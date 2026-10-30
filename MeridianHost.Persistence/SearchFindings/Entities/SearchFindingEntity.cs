using MeridianHost.Persistence.SearchSessions.Entities;

namespace MeridianHost.Persistence.SearchFindings.Entities;

public class SearchFindingEntity
{
    public Guid Id { get; set; }
    
    public SearchSessionEntity SearchSession { get; set; } = null!;
    public Guid SearchSessionId { get; set; }
    
    public string ProviderKey { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string DataJson { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}
