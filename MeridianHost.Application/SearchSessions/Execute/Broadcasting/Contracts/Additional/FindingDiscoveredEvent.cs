namespace MeridianHost.Application.SearchSessions.Execute.Broadcasting.Contracts.Additional;

public sealed record FindingDiscoveredEvent(
    Guid SessionId,
    Guid FindingId,
    string ProviderKey,
    string Type,
    string DataJson,
    DateTimeOffset CreatedAt) : SearchSessionEvent(SessionId);