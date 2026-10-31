using System.Text.Json;
using System.Text.Json.Serialization;

namespace MeridianHost.Infrastructure.SearchSessions.Providers.Domain.Contracts.Whois.Additional;

public sealed class RdapEntity
{
    [JsonPropertyName("handle")]
    public string? Handle { get; set; }
    
    [JsonPropertyName("roles")]
    public List<string>? Roles { get; set; } // ["registrar"], ["abuse"]
    
    [JsonPropertyName("publicIds")]
    public List<RdapPublicId>? PublicIds { get; set; }
    
    [JsonPropertyName("vcardArray")]
    public JsonElement? VcardArray { get; set; } // Оставляем сырым, чтобы не писать костыли под vCard
}