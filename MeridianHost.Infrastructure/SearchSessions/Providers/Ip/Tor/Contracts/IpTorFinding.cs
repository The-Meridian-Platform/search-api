namespace MeridianHost.Infrastructure.SearchSessions.Providers.Ip.Tor.Contracts;

public sealed record IpTorFinding(
    string Ip,
    bool IsTorExitNode,
    string NetworkType,
    string Severity,
    string Summary);
