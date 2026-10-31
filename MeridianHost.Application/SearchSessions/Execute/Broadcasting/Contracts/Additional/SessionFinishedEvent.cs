namespace MeridianHost.Application.SearchSessions.Execute.Broadcasting.Contracts.Additional;

public sealed record SessionFinishedEvent(
    Guid SessionId,
    string Status,
    int TotalFindings,
    DateTimeOffset FinishedAt) : SearchSessionEvent(SessionId);