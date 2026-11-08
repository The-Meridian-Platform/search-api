namespace MeridianHost.Infrastructure.SearchSessions.Providers.Domain.Wayback.Contracts;

public sealed record DomainWaybackFinding(
    string Domain,
    bool HasSnapshots,
    string? FirstSeenDate,
    string? LatestSnapshotUrl,
    string? LatestSnapshotTimestamp,
    int? EstimatedAgeYears);
