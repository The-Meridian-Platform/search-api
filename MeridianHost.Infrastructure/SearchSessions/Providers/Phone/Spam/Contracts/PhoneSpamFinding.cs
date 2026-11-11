namespace MeridianHost.Infrastructure.SearchSessions.Providers.Phone.Spam.Contracts;

public sealed record PhoneSpamFinding(
    string PhoneNumber,
    int RiskScore,
    string ReputationStatus,
    string Category,
    bool IsMalicious,
    int TotalReports,
    string? UserSummary);
