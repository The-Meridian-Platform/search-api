using MeridianHost.Domain.SearchTargets.Enum;

namespace MeridianHost.Persistence.SearchTargets.Entities;

public sealed class SearchTargetEntity
{
    public Guid Id { get; set; }
    public string Value { get; set; } = null!;
    public SearchTargetType TargetType { get; set; }
}
