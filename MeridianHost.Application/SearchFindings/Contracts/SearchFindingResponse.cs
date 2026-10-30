namespace MeridianHost.Application.SearchFindings.Contracts;

public sealed record SearchFindingResponse(
    Guid Id,
    string ProviderKey,
    string Type,
    string DataJson,
    DateTimeOffset CreatedAt);