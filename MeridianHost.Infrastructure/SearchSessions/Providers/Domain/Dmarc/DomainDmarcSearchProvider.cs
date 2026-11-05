using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using MeridianHost.Application.SearchSessions.Execute.Providers;
using MeridianHost.Application.SearchSessions.Execute.Providers.Contracts;
using MeridianHost.Domain.SearchTargets;
using MeridianHost.Domain.SearchTargets.Enum;
using MeridianHost.Infrastructure.SearchSessions.Providers.Domain.Dmarc.Contracts;
using MeridianHost.Infrastructure.SearchSessions.Providers.Domain.Dns.Contracts;

namespace MeridianHost.Infrastructure.SearchSessions.Providers.Domain.Dmarc;

public partial class DomainDmarcSearchProvider(HttpClient client) : ISearchProvider
{
    private readonly HttpClient _client = client;

    public string Key { get; } = "domain-dmarc";

    public bool CanHandle(SearchTarget target)
        => target.TargetType == SearchTargetType.Domain;

    public async IAsyncEnumerable<RawSearchFinding> ReadAllAsync(
        SearchTarget target,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var domain = target.Value.Trim().ToLowerInvariant().TrimEnd('.');
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"https://cloudflare-dns.com/dns-query?name=_dmarc.{domain}&type=TXT");

        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/dns-json"));

        DnsResponse? result = null;
        try
        {
            using var response = await _client.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
                yield break;

            result = await response.Content.ReadFromJsonAsync<DnsResponse>(cancellationToken);
        }
        catch
        {
            yield break;
        }

        var dmarcRecord = result?.Answers?
            .Select(a => a.Data.Trim('"'))
            .FirstOrDefault(d => d.Contains("v=DMARC1", StringComparison.OrdinalIgnoreCase));

        DomainDmarcFinding finding;

        if (string.IsNullOrWhiteSpace(dmarcRecord))
        {
            finding = new DomainDmarcFinding(
                Domain: domain,
                HasDmarc: false,
                RawRecord: null,
                Policy: "none",
                IsSpoofable: true,
                Severity: "Critical",
                ReportEmail: null,
                Summary: "DMARC policy missing. Domain is vulnerable to email spoofing.");
        }
        else
        {
            var policyMatch = PolicyRegex().Match(dmarcRecord);
            var policy = policyMatch.Success ? policyMatch.Groups[1].Value.ToLowerInvariant() : "unknown";

            var emailMatch = RuaRegex().Match(dmarcRecord);
            var reportEmail = emailMatch.Success ? emailMatch.Groups[1].Value.Trim() : null;

            bool isSpoofable = policy is "none" or "unknown";
            string severity = policy switch
            {
                "reject" => "Secure",
                "quarantine" => "Moderate",
                _ => "Warning"
            };

            string summary = policy switch
            {
                "reject" => "Strict DMARC protection active (p=reject). Email spoofing rejected.",
                "quarantine" => "DMARC quarantine active (p=quarantine). Spoofed emails routed to spam.",
                _ => "DMARC monitoring only (p=none). Domain vulnerable to email spoofing."
            };

            finding = new DomainDmarcFinding(
                Domain: domain,
                HasDmarc: true,
                RawRecord: dmarcRecord,
                Policy: policy,
                IsSpoofable: isSpoofable,
                Severity: severity,
                ReportEmail: reportEmail,
                Summary: summary);
        }

        yield return new RawSearchFinding("domain-dmarc_record", JsonSerializer.Serialize(finding));
    }

    [GeneratedRegex(@"\bp=([a-zA-Z]+)", RegexOptions.IgnoreCase)]
    private static partial Regex PolicyRegex();

    [GeneratedRegex(@"mailto:([^\s,;]+)", RegexOptions.IgnoreCase)]
    private static partial Regex RuaRegex();
}
