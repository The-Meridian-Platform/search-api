namespace MeridianHost.Infrastructure.SearchSessions.Providers.Email.Breaches.Contracts;

public sealed record EmailBreachesFinding(
    string Email,
    bool HasBreaches,
    int TotalBreaches,
    IReadOnlyList<string> Breaches,
    string Severity,
    string Summary);
