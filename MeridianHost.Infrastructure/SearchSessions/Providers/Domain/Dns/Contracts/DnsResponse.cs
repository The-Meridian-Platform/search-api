using System.Text.Json.Serialization;

namespace MeridianHost.Infrastructure.SearchSessions.Providers.Domain.Dns.Contracts;

public sealed class DnsResponse
{
    [JsonPropertyName("status")]
    public int Status { get; set; }
    
    [JsonPropertyName("answer")]
    public List<DnsAnswer>? Answers { get; set; }
}