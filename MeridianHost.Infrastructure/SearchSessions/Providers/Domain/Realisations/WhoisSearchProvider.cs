using System.Net.Http.Headers;
using System.Text.Json;
using MeridianHost.Application.SearchSessions.Execute.Providers;
using MeridianHost.Application.SearchSessions.Execute.Providers.Contracts;
using MeridianHost.Domain.SearchTargets;
using MeridianHost.Domain.SearchTargets.Enum;
using MeridianHost.Infrastructure.SearchSessions.Providers.Domain.Contracts.Whois;

namespace MeridianHost.Infrastructure.SearchSessions.Providers.Domain.Realisations;

public class WhoisSearchProvider(HttpClient client) : ISearchProvider
{
    private readonly HttpClient _client = client;
    
    public string Key { get; } = "domain-whois";

    public bool CanHandle(SearchTarget target)
        => target.TargetType == SearchTargetType.Domain;

    public async IAsyncEnumerable<RawSearchFinding> ReadAllAsync(
        SearchTarget target, 
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"https://rdap.org/domain/{target.Value}");
        
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");

        WhoisResponse? result;

        try
        {
            using var response  = await _client.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
                yield break;
            
            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            
            result = JsonSerializer.Deserialize<WhoisResponse>(json);
            if (result is null)
                yield break;
        }
        catch
        {
            yield break;
        }

        yield return new RawSearchFinding("whois_record", JsonSerializer.Serialize(result));
    }
}