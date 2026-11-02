namespace MeridianHost.Infrastructure.SearchSessions.Providers.Domain.Disposable.Contracts;

public sealed record DisposableDomainFinding(
    string Domain,
    bool IsDisposable,
    string Reason);