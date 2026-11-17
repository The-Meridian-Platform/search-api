using System.Text.Json.Serialization;

namespace MeridianHost.Infrastructure.SearchSessions.Providers.Ip.Epss.Contracts;

public sealed class EpssApiResponse
{
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("data")]
    public List<EpssItemDto>? Data { get; set; }
}

public sealed class EpssItemDto
{
    [JsonPropertyName("cve")]
    public string Cve { get; set; } = string.Empty;

    [JsonPropertyName("epss")]
    public string Epss { get; set; } = string.Empty;

    [JsonPropertyName("percentile")]
    public string Percentile { get; set; } = string.Empty;

    [JsonPropertyName("date")]
    public string? Date { get; set; }
}

public sealed class VulnerabilityIntelFinding
{
    public string TargetIp { get; set; } = string.Empty;
    public string Cve { get; set; } = string.Empty;
    public double EpssScore { get; set; }
    public double Percentile { get; set; }
    public bool IsKnownExploited { get; set; }
    public string Severity { get; set; } = "Medium";
    public string Description { get; set; } = string.Empty;
}
