using MeridianHost.Application.SearchTargets.Read.Contracts;
using MeridianHost.Domain.SearchTargets;

namespace MeridianHost.Application.SearchTargets.Read.Mapper;

public static class MapToResponse
{
    public static SearchTargetResponse ToResponse(this SearchTarget searchTarget)
        => new SearchTargetResponse(
            searchTarget.Id,
            searchTarget.Value,
            searchTarget.TargetType);
}