using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using MeridianHost.Application.SearchSessions.Execute.Providers;
using MeridianHost.Application.SearchSessions.Execute.Providers.Contracts;
using MeridianHost.Domain.SearchTargets;
using MeridianHost.Domain.SearchTargets.Enum;
using MeridianHost.Infrastructure.SearchSessions.Providers.Domain.Subdomains.Contracts;

namespace MeridianHost.Infrastructure.SearchSessions.Providers.Domain.Subdomains;

public partial class SubdomainSearchProvider(HttpClient client) : ISearchProvider
{
    private readonly HttpClient _client = client;

    [GeneratedRegex(@"<td>([a-zA-Z0-9\.\-_]+\.[a-zA-Z]{2,})</td>", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex RapidDnsSubdomainRegex();

    public string Key { get; } = "domain-subdomains";

    public bool CanHandle(SearchTarget target)
        => target.TargetType == SearchTargetType.Domain;

    public async IAsyncEnumerable<RawSearchFinding> ReadAllAsync(
        SearchTarget target,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var domain = target.Value.Trim().ToLowerInvariant();
        var uniqueSubdomains = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var certSpotterTask = QueryCertSpotterAsync(domain, cancellationToken);
        var rapidDnsTask = QueryRapidDnsAsync(domain, cancellationToken);
        var crtShTask = QueryCrtShAsync(domain, cancellationToken);

        await Task.WhenAll(certSpotterTask, rapidDnsTask, crtShTask);

        foreach (var finding in certSpotterTask.Result)
        {
            if (uniqueSubdomains.Add(finding.Subdomain))
            {
                yield return new RawSearchFinding("subdomain_record", JsonSerializer.Serialize(finding));
            }
        }

        foreach (var finding in rapidDnsTask.Result)
        {
            if (uniqueSubdomains.Add(finding.Subdomain))
            {
                yield return new RawSearchFinding("subdomain_record", JsonSerializer.Serialize(finding));
            }
        }

        foreach (var finding in crtShTask.Result)
        {
            if (uniqueSubdomains.Add(finding.Subdomain))
            {
                yield return new RawSearchFinding("subdomain_record", JsonSerializer.Serialize(finding));
            }
        }
    }

    private async Task<List<SubdomainFinding>> QueryCertSpotterAsync(string domain, CancellationToken ct)
    {
        var findings = new List<SubdomainFinding>();

        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(5));

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"https://api.certspotter.com/v1/issuances?domain={domain}&include_subdomains=true&expand=dns_names");

            request.Headers.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) Meridian/1.0");

            using var response = await _client.SendAsync(request, timeoutCts.Token);
            if (!response.IsSuccessStatusCode)
                return findings;

            var entries = await response.Content.ReadFromJsonAsync<List<CertSpotterEntry>>(timeoutCts.Token);
            if (entries is null)
                return findings;

            foreach (var entry in entries)
            {
                if (entry.DnsNames is null)
                    continue;

                foreach (var name in entry.DnsNames)
                {
                    var cleaned = CleanSubdomain(name, domain);
                    if (cleaned is not null)
                    {
                        findings.Add(new SubdomainFinding
                        {
                            Subdomain = cleaned,
                            Issuer = "CertSpotter (CT Log)",
                            ValidFrom = entry.NotBefore,
                            ValidTo = entry.NotAfter
                        });
                    }
                }
            }
        }
        catch
        {
        }

        return findings;
    }

    private async Task<List<SubdomainFinding>> QueryRapidDnsAsync(string domain, CancellationToken ct)
    {
        var findings = new List<SubdomainFinding>();

        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(5));

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"https://rapiddns.io/s/{domain}");

            request.Headers.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) Meridian/1.0");

            using var response = await _client.SendAsync(request, timeoutCts.Token);
            if (!response.IsSuccessStatusCode)
                return findings;

            var html = await response.Content.ReadAsStringAsync(timeoutCts.Token);
            var matches = RapidDnsSubdomainRegex().Matches(html);

            foreach (Match match in matches)
            {
                if (!match.Success)
                    continue;

                var raw = match.Groups[1].Value;
                var cleaned = CleanSubdomain(raw, domain);
                if (cleaned is not null)
                {
                    findings.Add(new SubdomainFinding
                    {
                        Subdomain = cleaned,
                        Issuer = "RapidDNS (Passive DNS)"
                    });
                }
            }
        }
        catch
        {
        }

        return findings;
    }

    private async Task<List<SubdomainFinding>> QueryCrtShAsync(string domain, CancellationToken ct)
    {
        var findings = new List<SubdomainFinding>();

        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(4));

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"https://crt.sh/?q=%.{domain}&output=json");

            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) Meridian/1.0");

            using var response = await _client.SendAsync(request, timeoutCts.Token);
            if (!response.IsSuccessStatusCode)
                return findings;

            var entries = await response.Content.ReadFromJsonAsync<List<CrtShEntry>>(timeoutCts.Token);
            if (entries is null)
                return findings;

            foreach (var entry in entries)
            {
                var rawNames = (entry.NameValue ?? entry.CommonName ?? string.Empty)
                    .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                foreach (var rawName in rawNames)
                {
                    var cleaned = CleanSubdomain(rawName, domain);
                    if (cleaned is not null)
                    {
                        findings.Add(new SubdomainFinding
                        {
                            Subdomain = cleaned,
                            Issuer = entry.IssuerName ?? "crt.sh (CT Log)",
                            ValidFrom = entry.NotBefore,
                            ValidTo = entry.NotAfter
                        });
                    }
                }
            }
        }
        catch
        {
        }

        return findings;
    }

    private static string? CleanSubdomain(string raw, string rootDomain)
    {
        var cleaned = raw.Trim().ToLowerInvariant();

        if (cleaned.StartsWith("*."))
        {
            cleaned = cleaned[2..];
        }

        if (cleaned.EndsWith('.'))
        {
            cleaned = cleaned.TrimEnd('.');
        }

        if (!cleaned.EndsWith(rootDomain, StringComparison.OrdinalIgnoreCase))
            return null;

        return cleaned;
    }
}
