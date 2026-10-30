using MeridianHost.Application.SearchFindings.Contracts;
using MeridianHost.Domain.SearchFindings;

namespace MeridianHost.Application.SearchFindings.Mapper;

public static class SearchFindingMapper
{
    public static SearchFindingResponse ToResponse(this SearchFinding searchFinding)
        => new SearchFindingResponse(
            searchFinding.Id,
            searchFinding.ProviderKey,
            searchFinding.Type,
            searchFinding.DataJson,
            searchFinding.CreatedAt);
}