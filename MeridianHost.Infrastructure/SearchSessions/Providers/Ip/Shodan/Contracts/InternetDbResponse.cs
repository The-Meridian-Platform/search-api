using System.Text.Json.Serialization;

namespace MeridianHost.Infrastructure.SearchSessions.Providers.Ip.Shodan.Contracts;

public sealed class InternetDbResponse
{
    [JsonPropertyName("ip")]
    public string Ip { get; set; } = string.Empty;

    [JsonPropertyName("ports")]
    public List<int>? Ports { get; set; }

    [JsonPropertyName("hostnames")]
    public List<string>? Hostnames { get; set; }

    [JsonPropertyName("tags")]
    public List<string>? Tags { get; set; }

    [JsonPropertyName("cpes")]
    public List<string>? Cpes { get; set; }

    [JsonPropertyName("vulns")]
    public List<string>? Vulns { get; set; }
}
