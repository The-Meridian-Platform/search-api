using System.Text.Json.Serialization;

namespace MeridianHost.Infrastructure.SearchSessions.Providers.Contracts;

public sealed class DnsResponse
{
    [JsonPropertyName("status")]
    public int Status { get; set; }
    
    [JsonPropertyName("answer")]
    public List<DnsAnswer>? Answers { get; set; }
}