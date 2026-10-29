namespace MeridianHost.Application.SearchSessions.Execute.Providers.Contracts;

/// <summary>
/// A universal find
/// </summary>
/// <param name="Type"></param>
/// <param name="DataJson"></param>
public sealed record SearchFinding(
    string Type, // something kinda "avatar", "profile", etc
    string DataJson); // raw json