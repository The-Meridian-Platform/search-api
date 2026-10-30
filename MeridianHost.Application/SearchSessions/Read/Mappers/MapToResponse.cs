using MeridianHost.Application.SearchFindings.Mapper;
using MeridianHost.Application.SearchSessions.Read.Contracts;
using MeridianHost.Application.SearchTargets.Read.Mapper;
using MeridianHost.Domain.SearchSessions;

namespace MeridianHost.Application.SearchSessions.Read.Mappers;

public static class MapToResponse
{
    public static SearchSessionResponse ToResponse(this SearchSession searchSession)
        => new SearchSessionResponse(searchSession.Id, 
            searchSession.Target.ToResponse(),
            searchSession.Findings.Select(x => x.ToResponse()).ToList(),
            searchSession.SearchStatus,
            searchSession.CreatedAt,
            searchSession.StartedAt,
            searchSession.FinishedAt);
}