namespace MeridianHost.Infrastructure.SearchSessions.Providers.Domain.Dmarc.Contracts;

public sealed record DomainDmarcFinding(
    string Domain,
    bool HasDmarc,
    string? RawRecord,
    string Policy,
    bool IsSpoofable,
    string Severity,
    string? ReportEmail,
    string Summary);
