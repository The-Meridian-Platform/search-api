using System.Collections.Frozen;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using MeridianHost.Application.SearchSessions.Execute.Providers;
using MeridianHost.Application.SearchSessions.Execute.Providers.Contracts;
using MeridianHost.Domain.SearchTargets;
using MeridianHost.Domain.SearchTargets.Enum;
using MeridianHost.Infrastructure.SearchSessions.Providers.Domain.Dns.Contracts;
using MeridianHost.Infrastructure.SearchSessions.Providers.Domain.Takeover.Contracts;

namespace MeridianHost.Infrastructure.SearchSessions.Providers.Domain.Takeover;

public class SubdomainTakeoverSearchProvider(HttpClient client) : ISearchProvider
{
    private readonly HttpClient _client = client;

    private sealed record TakeoverFingerprint(string Service, string CnamePattern, string BodyFingerprint);

    private static readonly TakeoverFingerprint[] Signatures =
    [
        new("GitHub Pages", "github.io", "There isn't a GitHub Pages site here"),
        new("Amazon S3", "s3.amazonaws.com", "The specified bucket does not exist"),
        new("Heroku", "herokuapp.com", "No such app"),
        new("Shopify", "myshopify.com", "Sorry, this shop is currently unavailable"),
        new("Ghost", "ghost.io", "The thing you were looking for is no longer here"),
        new("Surge.sh", "surge.sh", "project not found"),
        new("Fastly", "fastly.net", "Fastly error: unknown domain"),
        new("Zendesk", "zendesk.com", "Help Center Closed"),
        new("WordPress", "wordpress.com", "Do you want to register"),
        new("Tumblr", "domains.tumblr.com", "Whatever you were looking for doesn't exist"),
        new("Bitbucket", "bitbucket.io", "Repository not found")
    ];

    public string Key { get; } = "domain-subdomain-takeover";

    public bool CanHandle(SearchTarget target)
        => target.TargetType == SearchTargetType.Domain;

    public async IAsyncEnumerable<RawSearchFinding> ReadAllAsync(
        SearchTarget target,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var domain = target.Value.Trim().ToLowerInvariant();
        var cname = await ResolveCnameViaDohAsync(domain, cancellationToken);
        if (string.IsNullOrWhiteSpace(cname))
            yield break;

        foreach (var sig in Signatures)
        {
            if (!cname.Contains(sig.CnamePattern, StringComparison.OrdinalIgnoreCase))
                continue;

            var body = await FetchBodyPreviewAsync(domain, cancellationToken);
            if (body != null && body.Contains(sig.BodyFingerprint, StringComparison.OrdinalIgnoreCase))
            {
                var finding = new SubdomainTakeoverFinding
                {
                    Subdomain = domain,
                    Service = sig.Service,
                    Cname = cname,
                    Severity = "Critical",
                    FingerprintMatched = sig.BodyFingerprint,
                    Remediation = $"Удалите висящую CNAME-запись '{cname}' в DNS панели или зарегистрируйте сервис в {sig.Service}."
                };

                yield return new RawSearchFinding("subdomain_takeover_record", JsonSerializer.Serialize(finding));
            }
        }
    }

    private async Task<string?> ResolveCnameViaDohAsync(string domain, CancellationToken ct)
    {
        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(3));

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"https://cloudflare-dns.com/dns-query?name={domain}&type=CNAME");

            request.Headers.Add("Accept", "application/dns-json");

            using var response = await _client.SendAsync(request, timeoutCts.Token);
            if (!response.IsSuccessStatusCode)
                return null;

            var result = await response.Content.ReadFromJsonAsync<DnsResponse>(timeoutCts.Token);
            var record = result?.Answers?.FirstOrDefault(a => a.Type == 5);
            return record?.Data?.TrimEnd('.');
        }
        catch
        {
            return null;
        }
    }

    private async Task<string?> FetchBodyPreviewAsync(string domain, CancellationToken ct)
    {
        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(3));

            using var request = new HttpRequestMessage(HttpMethod.Get, $"http://{domain}");
            request.Headers.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) Meridian/1.0");

            using var response = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeoutCts.Token);
            var content = await response.Content.ReadAsStringAsync(timeoutCts.Token);
            return content.Length > 10000 ? content[..10000] : content;
        }
        catch
        {
            return null;
        }
    }
}
