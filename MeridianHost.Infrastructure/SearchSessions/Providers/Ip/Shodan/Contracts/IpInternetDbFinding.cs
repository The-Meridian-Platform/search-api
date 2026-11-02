namespace MeridianHost.Infrastructure.SearchSessions.Providers.Ip.Shodan.Contracts;

public sealed record IpInternetDbFinding(
    string Ip,
    IReadOnlyList<int> Ports,
    IReadOnlyList<string> Hostnames,
    IReadOnlyList<string> Tags,
    IReadOnlyList<string> Cpes,
    IReadOnlyList<string> Vulnerabilities);
