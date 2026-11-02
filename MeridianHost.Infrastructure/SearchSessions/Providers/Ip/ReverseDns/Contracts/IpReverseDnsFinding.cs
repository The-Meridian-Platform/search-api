namespace MeridianHost.Infrastructure.SearchSessions.Providers.Ip.ReverseDns.Contracts;

public sealed record IpReverseDnsFinding(
    string Ip,
    string ArpaQuery,
    IReadOnlyList<string> Hostnames);
