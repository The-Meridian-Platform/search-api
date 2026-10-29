namespace MeridianHost.Application.SearchSessions.Execute.Providers.Contracts;

public sealed record SearchFinding(
    string Type,
    string DataJson);