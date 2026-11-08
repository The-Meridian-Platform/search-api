namespace MeridianHost.Infrastructure.SearchSessions.Providers.Phone.Carrier.Contracts;

public sealed record PhoneCarrierFinding(
    string RawPhoneNumber,
    string NormalizedNumber,
    string Country,
    string CountryCode,
    string? Region,
    string Carrier,
    string LineType,
    string InternationalFormat,
    bool IsValid);
