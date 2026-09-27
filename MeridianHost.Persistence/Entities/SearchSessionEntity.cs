using MeridianHost.Domain.Enums;

namespace MeridianHost.Persistence.Entities;

public sealed class SearchSessionEntity
{
    public Guid Id { get; set; }
    
    public SearchTargetEntity Target { get; set; }
    
    public Status SearchStatus { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? FinishedAt { get; set; }
}