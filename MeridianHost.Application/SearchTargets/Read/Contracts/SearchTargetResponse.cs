using MeridianHost.Domain.SearchTargets.Enum;

namespace MeridianHost.Application.SearchTargets.Read.Contracts;

public record SearchTargetResponse(
    Guid Id,
    string Value,
    SearchTargetType TargetType);