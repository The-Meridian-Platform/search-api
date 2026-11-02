using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using MeridianHost.Application.SearchSessions.Execute.Providers;
using MeridianHost.Application.SearchSessions.Execute.Providers.Contracts;
using MeridianHost.Domain.SearchTargets;
using MeridianHost.Domain.SearchTargets.Enum;
using MeridianHost.Infrastructure.SearchSessions.Providers.Ip.Geo.Contracts;

namespace MeridianHost.Infrastructure.SearchSessions.Providers.Ip.Geo;

public class IpGeoSearchProvider(HttpClient client) : ISearchProvider
{
    private readonly HttpClient _client = client;

    public string Key { get; } = "ip-geo";

    public bool CanHandle(SearchTarget target)
        => target.TargetType == SearchTargetType.IpAddress;

    public async IAsyncEnumerable<RawSearchFinding> ReadAllAsync(
        SearchTarget target,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var ip = target.Value.Trim();
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"http://ip-api.com/json/{ip}?fields=status,message,country,countryCode,regionName,city,lat,lon,timezone,isp,org,as,mobile,proxy,hosting");

        IpApiResponse? result = null;
        try
        {
            using var response = await _client.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
                yield break;

            result = await response.Content.ReadFromJsonAsync<IpApiResponse>(cancellationToken);
        }
        catch
        {
            yield break;
        }

        if (result is null || !string.Equals(result.Status, "success", StringComparison.OrdinalIgnoreCase))
            yield break;

        var finding = new IpGeoFinding(
            Ip: ip,
            Country: result.Country,
            CountryCode: result.CountryCode,
            Region: result.RegionName,
            City: result.City,
            Latitude: result.Lat,
            Longitude: result.Lon,
            Timezone: result.Timezone,
            Isp: result.Isp,
            Org: result.Org,
            Asn: result.As,
            IsMobile: result.Mobile,
            IsProxy: result.Proxy,
            IsHosting: result.Hosting);

        yield return new RawSearchFinding("ip-geo_record", JsonSerializer.Serialize(finding));
    }
}
