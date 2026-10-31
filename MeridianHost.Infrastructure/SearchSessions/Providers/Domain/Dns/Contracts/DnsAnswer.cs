using System.Text.Json.Serialization;

namespace MeridianHost.Infrastructure.SearchSessions.Providers.Domain.Dns.Contracts;

public sealed class DnsAnswer
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty; // шоб компилятор не ругался за Null
    
    [JsonPropertyName("type")]
    public int Type { get; set; }
    
    [JsonPropertyName("TTL")]
    public int Ttl { get; set; }
    
    [JsonPropertyName("data")]
    public string Data { get; set; } = string.Empty; // и тут компилятор не ругайся пж
}