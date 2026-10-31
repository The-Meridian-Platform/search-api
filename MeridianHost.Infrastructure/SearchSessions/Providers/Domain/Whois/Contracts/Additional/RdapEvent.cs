using System.Text.Json.Serialization;

namespace MeridianHost.Infrastructure.SearchSessions.Providers.Domain.Contracts.Whois.Additional;

public sealed class RdapEvent
{
    [JsonPropertyName("eventAction")]
    public string EventAction { get; set; } = string.Empty; // "registration", "expiration", "last changed"
    
    [JsonPropertyName("eventDate")]
    public DateTimeOffset? EventDate { get; set; }
}