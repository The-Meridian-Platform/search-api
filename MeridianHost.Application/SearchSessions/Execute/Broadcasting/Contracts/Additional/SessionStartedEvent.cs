namespace MeridianHost.Application.SearchSessions.Execute.Broadcasting.Contracts.Additional;

public sealed record SessionStartedEvent(
    Guid SessionId,
    string TargetValue,
    DateTimeOffset StartedAt) : SearchSessionEvent(SessionId);