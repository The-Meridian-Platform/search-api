using MeridianHost.Domain.SearchSessions;
using MeridianHost.Persistence.SearchFindings.Mappers;
using MeridianHost.Persistence.SearchSessions.Entities;
using MeridianHost.Persistence.SearchTargets.Mappers;

namespace MeridianHost.Persistence.SearchSessions.Mappers;

public static class SearchSessionMapper
{
    public static SearchSession Map(this SearchSessionEntity entity)
        => SearchSession.Load(
            entity.Id,
            entity.Target.Map(),
            entity.Findings.Select(x => x.Map()).ToList(),
            entity.SearchStatus,
            entity.CreatedAt,
            entity.StartedAt,
            entity.FinishedAt);

    public static SearchSessionEntity Map(this SearchSession domain)
        => new SearchSessionEntity
        {
            Id = domain.Id,
            TargetId = domain.Target.Id,
            Target = domain.Target.Map(),
            Findings = domain.Findings.Select(x => x.Map()).ToList(),
            SearchStatus = domain.SearchStatus,
            CreatedAt = domain.CreatedAt,
            StartedAt = domain.StartedAt,
            FinishedAt = domain.FinishedAt
        };
}
