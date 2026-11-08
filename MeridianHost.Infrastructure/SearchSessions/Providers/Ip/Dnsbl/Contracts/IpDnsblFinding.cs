namespace MeridianHost.Infrastructure.SearchSessions.Providers.Ip.Dnsbl.Contracts;

public sealed record IpDnsblFinding(
    string Ip,
    bool IsListed,
    string BlacklistProvider,
    string? ReturnCode,
    string ThreatType,
    string Severity,
    string Details);
