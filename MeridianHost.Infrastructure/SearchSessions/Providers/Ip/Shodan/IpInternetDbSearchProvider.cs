using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using MeridianHost.Application.SearchSessions.Execute.Providers;
using MeridianHost.Application.SearchSessions.Execute.Providers.Contracts;
using MeridianHost.Domain.SearchTargets;
using MeridianHost.Domain.SearchTargets.Enum;
using MeridianHost.Infrastructure.SearchSessions.Providers.Ip.Shodan.Contracts;

namespace MeridianHost.Infrastructure.SearchSessions.Providers.Ip.Shodan;

public class IpInternetDbSearchProvider(HttpClient client) : ISearchProvider
{
    private readonly HttpClient _client = client;

    public string Key { get; } = "ip-shodan-db";

    public bool CanHandle(SearchTarget target)
        => target.TargetType == SearchTargetType.IpAddress;

    public async IAsyncEnumerable<RawSearchFinding> ReadAllAsync(
        SearchTarget target,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var ip = target.Value.Trim();
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"https://internetdb.shodan.io/{ip}");

        request.Headers.UserAgent.Add(new ProductInfoHeaderValue("Meridian-Platform", "1.0"));

        InternetDbResponse? result = null;
        try
        {
            using var response = await _client.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
                yield break;

            result = await response.Content.ReadFromJsonAsync<InternetDbResponse>(cancellationToken);
        }
        catch
        {
            yield break;
        }

        if (result is null)
            yield break;

        var finding = new IpInternetDbFinding(
            Ip: string.IsNullOrWhiteSpace(result.Ip) ? ip : result.Ip,
            Ports: result.Ports ?? [],
            Hostnames: result.Hostnames ?? [],
            Tags: result.Tags ?? [],
            Cpes: result.Cpes ?? [],
            Vulnerabilities: result.Vulns ?? []);

        yield return new RawSearchFinding("ip-shodan_record", JsonSerializer.Serialize(finding));
    }
}
