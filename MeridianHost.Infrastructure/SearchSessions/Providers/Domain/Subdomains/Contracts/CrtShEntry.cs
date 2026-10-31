using System.Text.Json.Serialization;

namespace MeridianHost.Infrastructure.SearchSessions.Providers.Domain.Subdomains.Contracts;

public sealed class CrtShEntry
{
    [JsonPropertyName("name_value")]
    public string? NameValue { get; set; }

    [JsonPropertyName("common_name")]
    public string? CommonName { get; set; }

    [JsonPropertyName("issuer_name")]
    public string? IssuerName { get; set; }

    [JsonPropertyName("not_before")]
    public string? NotBefore { get; set; }

    [JsonPropertyName("not_after")]
    public string? NotAfter { get; set; }
}

public sealed class SubdomainFinding
{
    public string Subdomain { get; set; } = string.Empty;
    public string? Issuer { get; set; }
    public string? ValidFrom { get; set; }
    public string? ValidTo { get; set; }
}
