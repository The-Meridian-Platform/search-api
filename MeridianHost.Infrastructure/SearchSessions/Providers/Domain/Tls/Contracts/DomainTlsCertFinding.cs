namespace MeridianHost.Infrastructure.SearchSessions.Providers.Domain.Tls.Contracts;

public sealed record DomainTlsCertFinding(
    string Domain,
    string Subject,
    string Issuer,
    DateTime ValidFrom,
    DateTime ValidTo,
    int DaysUntilExpiration,
    bool IsExpired,
    string Thumbprint,
    string SignatureAlgorithm,
    IReadOnlyList<string> SubjectAlternativeNames);
