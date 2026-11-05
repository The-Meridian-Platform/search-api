namespace MeridianHost.Infrastructure.SearchSessions.Providers.Domain.SecurityTxt.Contracts;

public sealed record DomainSecurityTxtFinding(
    string Domain,
    string Url,
    IReadOnlyList<string> Contacts,
    IReadOnlyList<string> SecurityEmails,
    string? PolicyUrl,
    string? AcknowledgmentsUrl,
    string? HiringUrl,
    string? Expires);
