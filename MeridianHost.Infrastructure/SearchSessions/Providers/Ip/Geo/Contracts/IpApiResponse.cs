using System.Text.Json.Serialization;

namespace MeridianHost.Infrastructure.SearchSessions.Providers.Ip.Geo.Contracts;

public sealed class IpApiResponse
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("country")]
    public string Country { get; set; } = string.Empty;

    [JsonPropertyName("countryCode")]
    public string CountryCode { get; set; } = string.Empty;

    [JsonPropertyName("regionName")]
    public string RegionName { get; set; } = string.Empty;

    [JsonPropertyName("city")]
    public string City { get; set; } = string.Empty;

    [JsonPropertyName("lat")]
    public double Lat { get; set; }

    [JsonPropertyName("lon")]
    public double Lon { get; set; }

    [JsonPropertyName("timezone")]
    public string Timezone { get; set; } = string.Empty;

    [JsonPropertyName("isp")]
    public string Isp { get; set; } = string.Empty;

    [JsonPropertyName("org")]
    public string Org { get; set; } = string.Empty;

    [JsonPropertyName("as")]
    public string As { get; set; } = string.Empty;

    [JsonPropertyName("mobile")]
    public bool Mobile { get; set; }

    [JsonPropertyName("proxy")]
    public bool Proxy { get; set; }

    [JsonPropertyName("hosting")]
    public bool Hosting { get; set; }
}
