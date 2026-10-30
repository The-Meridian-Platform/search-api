using MeridianHost.Domain.SearchFindings;
using MeridianHost.Persistence.SearchFindings.Entities;

namespace MeridianHost.Persistence.SearchFindings.Mappers;

public static class SearchFindingMapper
{
    public static SearchFinding Map(this SearchFindingEntity entity)
        => SearchFinding.Load(
            entity.Id,
            entity.SearchSessionId,
            entity.ProviderKey,
            entity.Type,
            entity.DataJson,
            entity.CreatedAt);

    public static SearchFindingEntity Map(this SearchFinding domain)
        => new SearchFindingEntity
        {
            Id = domain.Id,
            SearchSessionId = domain.SearchSessionId,
            ProviderKey = domain.ProviderKey,
            Type = domain.Type,
            DataJson = domain.DataJson,
            CreatedAt = domain.CreatedAt,
        };
}