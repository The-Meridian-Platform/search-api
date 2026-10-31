using System.Text.Json.Serialization;

namespace MeridianHost.Infrastructure.SearchSessions.Providers.Domain.Contracts.Whois.Additional;

public sealed class RdapPublicId
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }
    
    [JsonPropertyName("identifier")]
    public string? Identifier { get; set; }
}