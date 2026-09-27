using MeridianHost.Domain.Models;
using MeridianHost.Persistence.Entities;

namespace MeridianHost.Persistence.Mappers;

public static class SearchTargetMapper
{
    public static SearchTarget Map(this SearchTargetEntity entity)
        => SearchTarget.Load(
            entity.Id,
            entity.Value,
            entity.TargetType);

    public static SearchTargetEntity Map(this SearchTarget domain)
        => new SearchTargetEntity
        {
            Id = domain.Id,
            Value = domain.Value,
            TargetType = domain.TargetType
        };
}