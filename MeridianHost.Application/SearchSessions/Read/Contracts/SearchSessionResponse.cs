using MeridianHost.Application.SearchFindings.Contracts;
using MeridianHost.Application.SearchTargets.Read.Contracts;
using MeridianHost.Domain.SearchSessions.Enum;

namespace MeridianHost.Application.SearchSessions.Read.Contracts;

public sealed record SearchSessionResponse(
    Guid Id,
    SearchTargetResponse Target,
    List<SearchFindingResponse>? Finding,
    Status Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? FinishedAt);