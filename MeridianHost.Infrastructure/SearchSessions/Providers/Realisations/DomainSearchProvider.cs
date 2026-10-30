using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using MeridianHost.Application.SearchSessions.Execute.Providers;
using MeridianHost.Application.SearchSessions.Execute.Providers.Contracts;
using MeridianHost.Domain.SearchTargets;
using MeridianHost.Domain.SearchTargets.Enum;
using MeridianHost.Infrastructure.SearchSessions.Providers.Contracts;

namespace MeridianHost.Infrastructure.SearchSessions.Providers.Realisations;

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
        string[] recordTypes = ["A", "AAAA", "MX", "TXT", "NS", "CNAME"];

        var tasks = recordTypes
            .Select(type => FetchRecordsAsync(target.Value, type, cancellationToken))
            .ToArray();

        var results = await Task.WhenAll(tasks);

        foreach (var findingList in results)
        {
            foreach (var finding in findingList)
            {
                yield return finding;
            }
        }
    }

    private async Task<List<SearchFinding>> FetchRecordsAsync(
        string domain, 
        string recordType, 
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"https://cloudflare-dns.com/dns-query?name={domain}&type={recordType}");

        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/dns-json"));

        try
        {
            using var response = await _client.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return [];

            var result = await response.Content.ReadFromJsonAsync<DnsResponse>(cancellationToken);
            if (result?.Answers is null || result.Answers.Count == 0)
                return [];

            return result.Answers
                .Select(answer => new SearchFinding("dns_record", JsonSerializer.Serialize(answer)))
                .ToList();
        }
        catch
        {
            return [];
        }
    }
}
