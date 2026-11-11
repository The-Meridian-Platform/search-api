namespace MeridianHost.Infrastructure.SearchSessions.Providers.Domain.ThreatIntel.Contracts;

public sealed record DomainThreatFinding(
    string Domain,
    bool IsThreatDetected,
    string ThreatType,
    int MaliciousUrlsCount,
    string Severity,
    string Details);
