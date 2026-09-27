using MeridianHost.Domain.Models;
using MeridianHost.Persistence.Entities;

namespace MeridianHost.Persistence.Mappers;

public static class SearchSessionMapper
{
    public static SearchSession Map(this SearchSessionEntity entity)
        => SearchSession.Load(
            entity.Id,
            entity.Target.Map(),
            entity.SearchStatus,
            entity.CreatedAt,
            entity.StartedAt,
            entity.FinishedAt);

    public static SearchSessionEntity Map(this SearchSession domain)
        => new SearchSessionEntity
        {
            Id = domain.Id,
            Target = domain.Target.Map(),
            SearchStatus = domain.SearchStatus,
            CreatedAt = domain.CreatedAt,
            StartedAt = domain.StartedAt,
            FinishedAt = domain.FinishedAt
        };
}