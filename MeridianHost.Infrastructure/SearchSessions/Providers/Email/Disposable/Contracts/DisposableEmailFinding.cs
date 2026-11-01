namespace MeridianHost.Infrastructure.SearchSessions.Providers.Email.Disposable.Contracts;

public sealed record DisposableEmailFinding(
    string Email,
    string Domain,
    bool IsDisposable,
    string Category);
