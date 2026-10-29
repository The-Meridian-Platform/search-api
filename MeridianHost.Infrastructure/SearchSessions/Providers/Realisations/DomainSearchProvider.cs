using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using MeridianHost.Application.SearchSessions.Execute.Providers;
using MeridianHost.Application.SearchSessions.Execute.Providers.Contracts;
using MeridianHost.Domain.SearchTargets;
using MeridianHost.Domain.SearchTargets.Enum;
using MeridianHost.Infrastructure.SearchSessions.Providers.Contracts;

namespace MeridianHost.Infrastructure.SearchSessions.Providers.Realisations;

/// <summary>
/// Domain search provider
/// </summary>
/// <param name="client"></param>
public class DomainSearchProvider(HttpClient client) : ISearchProvider
{
    private readonly HttpClient _client = client;

    public string Key { get; } = "domain-dns";

    public bool CanHandle(SearchTarget target)
        => target.TargetType == SearchTargetType.Domain;

    public async IAsyncEnumerable<SearchFinding> ReadAllAsync(
        SearchTarget target, 
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        string[] recordTypes = new[] { "A", "AAAA", "MX", "TXT", "NS", "CNAME"};

        foreach (var recordType in recordTypes)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"https://cloudflare-dns.com/dns-query?name={target.Value}&type={recordType}");
            
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/dns-json"));
            
            DnsResponse? result;

            try
            {
                using var response = await _client.SendAsync(request, cancellationToken);
                if (!response.IsSuccessStatusCode)
                    continue;

                result = await response.Content.ReadFromJsonAsync<DnsResponse>(cancellationToken);
            }

            catch { continue; }
            
            if (result?.Answers is null || result?.Answers.Count == 0) continue;

            foreach (var answer in result!.Answers)
                yield return new SearchFinding("dns_record", JsonSerializer.Serialize(answer));
        }
    }
    
}