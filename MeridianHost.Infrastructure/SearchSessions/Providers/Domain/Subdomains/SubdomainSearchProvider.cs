using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using MeridianHost.Application.SearchSessions.Execute.Providers;
using MeridianHost.Application.SearchSessions.Execute.Providers.Contracts;
using MeridianHost.Domain.SearchTargets;
using MeridianHost.Domain.SearchTargets.Enum;
using MeridianHost.Infrastructure.SearchSessions.Providers.Domain.Subdomains.Contracts;

namespace MeridianHost.Infrastructure.SearchSessions.Providers.Domain.Subdomains;

public class SubdomainSearchProvider(HttpClient client) : ISearchProvider
{
    private readonly HttpClient _client = client;

    public string Key { get; } = "domain-subdomains";

    public bool CanHandle(SearchTarget target)
        => target.TargetType == SearchTargetType.Domain;

    public async IAsyncEnumerable<RawSearchFinding> ReadAllAsync(
        SearchTarget target,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"https://crt.sh/?q=%.{target.Value}&output=json");

        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) Meridian/1.0");

        List<CrtShEntry>? entries;

        try
        {
            using var response = await _client.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
                yield break;

            entries = await response.Content.ReadFromJsonAsync<List<CrtShEntry>>(cancellationToken);
            if (entries is null || entries.Count == 0)
                yield break;
        }
        catch
        {
            yield break;
        }

        var uniqueSubdomains = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in entries)
        {
            var rawNames = (entry.NameValue ?? entry.CommonName ?? string.Empty)
                .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            foreach (var rawName in rawNames)
            {
                var cleaned = rawName.Trim().ToLowerInvariant();

                if (cleaned.StartsWith("*."))
                {
                    cleaned = cleaned[2..];
                }

                if (!cleaned.EndsWith(target.Value, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (uniqueSubdomains.Add(cleaned))
                {
                    var finding = new SubdomainFinding
                    {
                        Subdomain = cleaned,
                        Issuer = entry.IssuerName,
                        ValidFrom = entry.NotBefore,
                        ValidTo = entry.NotAfter
                    };

                    yield return new RawSearchFinding("subdomain_record", JsonSerializer.Serialize(finding));
                }
            }
        }
    }
}
