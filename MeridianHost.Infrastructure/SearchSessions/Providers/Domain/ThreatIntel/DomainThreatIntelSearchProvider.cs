using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text.Json;
using MeridianHost.Application.SearchSessions.Execute.Providers;
using MeridianHost.Application.SearchSessions.Execute.Providers.Contracts;
using MeridianHost.Domain.SearchTargets;
using MeridianHost.Domain.SearchTargets.Enum;
using MeridianHost.Infrastructure.SearchSessions.Providers.Domain.ThreatIntel.Contracts;

namespace MeridianHost.Infrastructure.SearchSessions.Providers.Domain.ThreatIntel;

public class DomainThreatIntelSearchProvider(HttpClient client) : ISearchProvider
{
    private readonly HttpClient _client = client;

    public string Key { get; } = "domain-threat";

    public bool CanHandle(SearchTarget target)
        => target.TargetType == SearchTargetType.Domain;

    public async IAsyncEnumerable<RawSearchFinding> ReadAllAsync(
        SearchTarget target,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var domain = target.Value.Trim().ToLowerInvariant().TrimEnd('.');
        if (string.IsNullOrWhiteSpace(domain))
            yield break;

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(3.5));

        bool isThreat = false;
        string threatType = "Clean";
        int urlCount = 0;
        string severity = "Secure";
        string details = "No active malware distribution or phishing records found in Abuse.ch URLhaus.";

        try
        {
            var content = new FormUrlEncodedContent([new KeyValuePair<string, string>("host", domain)]);
            using var req = new HttpRequestMessage(HttpMethod.Post, "https://urlhaus-api.abuse.ch/v1/host/")
            {
                Content = content
            };
            req.Headers.UserAgent.Add(new ProductInfoHeaderValue("Meridian-Platform", "1.0"));

            using var resp = await _client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, cts.Token);
            if (resp.IsSuccessStatusCode)
            {
                using var stream = await resp.Content.ReadAsStreamAsync(cts.Token);
                using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cts.Token);
                var root = doc.RootElement;

                if (root.TryGetProperty("query_status", out var qStatus) &&
                    string.Equals(qStatus.GetString(), "ok", StringComparison.OrdinalIgnoreCase))
                {
                    isThreat = true;
                    threatType = root.TryGetProperty("threat", out var t) && !string.IsNullOrEmpty(t.GetString())
                        ? t.GetString()!
                        : "Malware Distribution";

                    urlCount = root.TryGetProperty("url_count", out var uc) ? uc.GetInt32() : 1;
                    severity = "Critical";
                    details = $"Domain is actively tracked by Abuse.ch for hosting {urlCount} malicious payload URLs ({threatType}).";
                }
            }
        }
        catch
        {
        }

        var finding = new DomainThreatFinding(
            Domain: domain,
            IsThreatDetected: isThreat,
            ThreatType: threatType,
            MaliciousUrlsCount: urlCount,
            Severity: severity,
            Details: details);

        yield return new RawSearchFinding("domain-threat_record", JsonSerializer.Serialize(finding));
    }
}
