namespace MeridianHost.Infrastructure.SearchSessions.Providers.Domain.SensitivePaths.Contracts;

public sealed record DiscoveredPathItem(
    string Path,
    string FullUrl,
    int StatusCode,
    string Category,
    string Severity,
    string Summary);

public sealed record DomainSensitivePathsFinding(
    string Domain,
    int TotalDiscovered,
    bool HasCriticalLeaks,
    IReadOnlyList<DiscoveredPathItem> Items);
