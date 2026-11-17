using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using MeridianHost.Application.SearchSessions.Execute.Providers;
using MeridianHost.Application.SearchSessions.Execute.Providers.Contracts;
using MeridianHost.Domain.SearchTargets;
using MeridianHost.Domain.SearchTargets.Enum;
using MeridianHost.Infrastructure.SearchSessions.Providers.Domain.JsLeaks.Contracts;

namespace MeridianHost.Infrastructure.SearchSessions.Providers.Domain.JsLeaks;

public partial class DomainJsSecretsSearchProvider(HttpClient client) : ISearchProvider
{
    private readonly HttpClient _client = client;

    [GeneratedRegex(@"<script[^>]+src=[""']([^""']+\.js[^""']*)[""']", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex ScriptTagRegex();

    [GeneratedRegex(@"AIzaSy[a-zA-Z0-9_\-]{33}", RegexOptions.Compiled)]
    private static partial Regex GoogleApiKeyRegex();

    [GeneratedRegex(@"gh[pousr]_[a-zA-Z0-9]{36,255}", RegexOptions.Compiled)]
    private static partial Regex GitHubTokenRegex();

    [GeneratedRegex(@"AKIA[0-9A-Z]{16}", RegexOptions.Compiled)]
    private static partial Regex AwsAccessKeyRegex();

    [GeneratedRegex(@"sk_live_[0-9a-zA-Z]{24}", RegexOptions.Compiled)]
    private static partial Regex StripeSecretKeyRegex();

    [GeneratedRegex(@"xox[baprs]-[0-9a-zA-Z]{10,48}", RegexOptions.Compiled)]
    private static partial Regex SlackTokenRegex();

    public string Key { get; } = "domain-js-secrets";

    public bool CanHandle(SearchTarget target)
        => target.TargetType == SearchTargetType.Domain;

    public async IAsyncEnumerable<RawSearchFinding> ReadAllAsync(
        SearchTarget target,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var domain = target.Value.Trim();
        var html = await FetchHtmlAsync(domain, cancellationToken);
        if (string.IsNullOrWhiteSpace(html))
            yield break;

        var scriptUrls = ExtractScriptUrls(html, domain);
        var seenSecrets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var scriptUrl in scriptUrls.Take(4))
        {
            var jsContent = await FetchScriptContentAsync(scriptUrl, cancellationToken);
            if (string.IsNullOrWhiteSpace(jsContent))
                continue;

            foreach (var finding in ScanContentForSecrets(domain, scriptUrl, jsContent, seenSecrets))
            {
                yield return new RawSearchFinding("js_secret_leak_record", JsonSerializer.Serialize(finding));
            }
        }
    }

    private async Task<string?> FetchHtmlAsync(string domain, CancellationToken ct)
    {
        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(4));

            using var request = new HttpRequestMessage(HttpMethod.Get, $"https://{domain}");
            request.Headers.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) Meridian/1.0");

            using var response = await _client.SendAsync(request, timeoutCts.Token);
            if (!response.IsSuccessStatusCode)
                return null;

            return await response.Content.ReadAsStringAsync(timeoutCts.Token);
        }
        catch
        {
            return null;
        }
    }

    private async Task<string?> FetchScriptContentAsync(string url, CancellationToken ct)
    {
        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(4));

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) Meridian/1.0");

            using var response = await _client.SendAsync(request, timeoutCts.Token);
            if (!response.IsSuccessStatusCode)
                return null;

            var content = await response.Content.ReadAsStringAsync(timeoutCts.Token);
            return content.Length > 200000 ? content[..200000] : content;
        }
        catch
        {
            return null;
        }
    }

    private static List<string> ExtractScriptUrls(string html, string domain)
    {
        var list = new List<string>();
        var matches = ScriptTagRegex().Matches(html);

        foreach (Match match in matches)
        {
            if (!match.Success)
                continue;

            var rawSrc = match.Groups[1].Value.Trim();
            if (rawSrc.StartsWith("//"))
            {
                list.Add($"https:{rawSrc}");
            }
            else if (rawSrc.StartsWith('/'))
            {
                list.Add($"https://{domain}{rawSrc}");
            }
            else if (rawSrc.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                     rawSrc.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                list.Add(rawSrc);
            }
        }

        return list;
    }

    private static IEnumerable<JsSecretLeakFinding> ScanContentForSecrets(
        string domain,
        string scriptUrl,
        string content,
        HashSet<string> seen)
    {
        foreach (Match m in GoogleApiKeyRegex().Matches(content))
        {
            if (m.Success && seen.Add(m.Value))
            {
                yield return BuildFinding(domain, scriptUrl, "Google API Key", "Medium", m.Value);
            }
        }

        foreach (Match m in GitHubTokenRegex().Matches(content))
        {
            if (m.Success && seen.Add(m.Value))
            {
                yield return BuildFinding(domain, scriptUrl, "GitHub Personal Token", "Critical", m.Value);
            }
        }

        foreach (Match m in AwsAccessKeyRegex().Matches(content))
        {
            if (m.Success && seen.Add(m.Value))
            {
                yield return BuildFinding(domain, scriptUrl, "AWS Access Key ID", "Critical", m.Value);
            }
        }

        foreach (Match m in StripeSecretKeyRegex().Matches(content))
        {
            if (m.Success && seen.Add(m.Value))
            {
                yield return BuildFinding(domain, scriptUrl, "Stripe Live Secret Key", "Critical", m.Value);
            }
        }

        foreach (Match m in SlackTokenRegex().Matches(content))
        {
            if (m.Success && seen.Add(m.Value))
            {
                yield return BuildFinding(domain, scriptUrl, "Slack API Token", "High", m.Value);
            }
        }
    }

    private static JsSecretLeakFinding BuildFinding(
        string domain,
        string scriptUrl,
        string secretType,
        string severity,
        string rawSecret)
    {
        var masked = rawSecret.Length > 8
            ? $"{rawSecret[..4]}...{rawSecret[^4..]}"
            : "****";

        return new JsSecretLeakFinding
        {
            TargetDomain = domain,
            ScriptUrl = scriptUrl,
            SecretType = secretType,
            Severity = severity,
            MaskedMatch = masked,
            Description = $"В бандле обнаружен жестко закодированный секрет: {secretType} ({masked})"
        };
    }
}
