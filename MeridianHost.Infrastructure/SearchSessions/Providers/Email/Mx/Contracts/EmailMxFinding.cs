namespace MeridianHost.Infrastructure.SearchSessions.Providers.Email.Mx.Contracts;

public sealed record EmailMxFinding(
    string EmailDomain,
    string MailProviderName,
    List<string> MxServers,
    bool IsEnterprise,
    bool HasSpfProtection);
