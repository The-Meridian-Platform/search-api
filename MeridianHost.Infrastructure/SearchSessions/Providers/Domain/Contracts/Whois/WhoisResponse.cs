using System.Text.Json.Serialization;
using MeridianHost.Infrastructure.SearchSessions.Providers.Domain.Contracts.Whois.Additional;

namespace MeridianHost.Infrastructure.SearchSessions.Providers.Domain.Contracts.Whois;

public sealed class WhoisResponse
{
    [JsonPropertyName("handle")]
    public string? Handle { get; set; }
    
    [JsonPropertyName("ldhName")]
    public string? LdhName { get; set; }
    
    [JsonPropertyName("status")]
    public List<string>? Status { get; set; }
    
    [JsonPropertyName("events")]
    public List<RdapEvent>? Events { get; set; }
    
    [JsonPropertyName("nameservers")]
    public List<RdapNameserver>? Nameservers { get; set; }
    
    [JsonPropertyName("entities")]
    public List<RdapEntity>? Entities { get; set; }
}