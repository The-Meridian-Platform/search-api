using MeridianHost.Domain.SearchTargets.Enum;

namespace MeridianHost.Application.SearchSessions.Create.Contracts;

public sealed record CreateSearchSessionRequest(string Value, SearchTargetType? TargetType);