using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using MeridianHost.Application.SearchSessions.Execute.Providers;
using MeridianHost.Application.SearchSessions.Execute.Providers.Contracts;
using MeridianHost.Domain.SearchTargets;
using MeridianHost.Domain.SearchTargets.Enum;
using MeridianHost.Infrastructure.SearchSessions.Providers.Domain.Dns.Contracts;
using MeridianHost.Infrastructure.SearchSessions.Providers.Email.Mx.Contracts;

namespace MeridianHost.Infrastructure.SearchSessions.Providers.Email.Mx;

public class EmailMxSearchProvider(HttpClient client) : ISearchProvider
{
    private readonly HttpClient _client = client;

    public string Key { get; } = "email-mx";

    public bool CanHandle(SearchTarget target)
        => target.TargetType == SearchTargetType.Email;

    public async IAsyncEnumerable<RawSearchFinding> ReadAllAsync(
        SearchTarget target,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var parts = target.Value.Trim().Split('@');
        if (parts.Length != 2)
            yield break;

        var domain = parts[1].ToLowerInvariant();

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"https://cloudflare-dns.com/dns-query?name={domain}&type=MX");

        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/dns-json"));

        DnsResponse? result;

        try
        {
            using var response = await _client.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
                yield break;

            result = await response.Content.ReadFromJsonAsync<DnsResponse>(cancellationToken);
            if (result?.Answers is null || result.Answers.Count == 0)
                yield break;
        }
        catch
        {
            yield break;
        }

        var mxServers = result.Answers
            .Select(a => a.Data.Trim().TrimEnd('.'))
            .ToList();

        var (providerName, isEnterprise) = DetectMailProvider(mxServers);

        var finding = new EmailMxFinding(
            EmailDomain: domain,
            MailProviderName: providerName,
            MxServers: mxServers,
            IsEnterprise: isEnterprise,
            HasSpfProtection: false);

        yield return new RawSearchFinding("mx-email_record", JsonSerializer.Serialize(finding));
    }

    private static (string Name, bool IsEnterprise) DetectMailProvider(List<string> mxServers)
    {
        var text = string.Join(" ", mxServers).ToLowerInvariant();

        if (text.Contains("google") || text.Contains("aspmx"))
            return ("Google Workspace / Gmail", true);

        if (text.Contains("outlook") || text.Contains("microsoft"))
            return ("Microsoft 365 / Exchange", true);

        if (text.Contains("protonmail") || text.Contains("proton.me"))
            return ("ProtonMail (End-to-End Encrypted)", false);

        if (text.Contains("yandex"))
            return ("Yandex 360", true);

        if (text.Contains("mail.ru") || text.Contains("mxs.mail.ru"))
            return ("VK / Mail.ru", false);

        if (text.Contains("zoho"))
            return ("Zoho Mail", true);

        if (text.Contains("icloud") || text.Contains("apple"))
            return ("Apple iCloud Mail", false);

        return ("Custom / Self-Hosted Mail Server", false);
    }
}
