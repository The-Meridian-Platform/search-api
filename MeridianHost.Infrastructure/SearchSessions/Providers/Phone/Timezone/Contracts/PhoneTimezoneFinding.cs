namespace MeridianHost.Infrastructure.SearchSessions.Providers.Phone.Timezone.Contracts;

public sealed record PhoneTimezoneFinding(
    string PhoneNumber,
    string TimezoneId,
    string UtcOffset,
    string CurrentLocalTime,
    bool IsBusinessHours,
    string TimeNotice);
