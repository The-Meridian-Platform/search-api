using System.Text.Json.Serialization;

namespace MeridianHost.Infrastructure.SearchSessions.Providers.Domain.Contracts.Whois.Additional;

public sealed class RdapNameserver
{
    [JsonPropertyName("ldhName")]
    public string LdhName { get; set; } = string.Empty;
}