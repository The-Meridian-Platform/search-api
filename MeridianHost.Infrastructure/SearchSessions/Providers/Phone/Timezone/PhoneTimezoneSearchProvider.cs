using System.Collections.Frozen;
using System.Runtime.CompilerServices;
using System.Text.Json;
using MeridianHost.Application.SearchSessions.Execute.Providers;
using MeridianHost.Application.SearchSessions.Execute.Providers.Contracts;
using MeridianHost.Domain.SearchTargets;
using MeridianHost.Domain.SearchTargets.Enum;
using MeridianHost.Domain.SearchTargets.ValueObjects;
using MeridianHost.Infrastructure.SearchSessions.Providers.Phone.Timezone.Contracts;

namespace MeridianHost.Infrastructure.SearchSessions.Providers.Phone.Timezone;

public class PhoneTimezoneSearchProvider(HttpClient client) : ISearchProvider
{
    private readonly HttpClient _client = client;

    public string Key { get; } = "phone-timezone";

    public bool CanHandle(SearchTarget target)
        => target.TargetType == SearchTargetType.PhoneNumber;

    public async IAsyncEnumerable<RawSearchFinding> ReadAllAsync(
        SearchTarget target,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await Task.Yield();

        var normalized = PhoneNumber.Normalize(target.Value);
        var digits = normalized.TrimStart('+');
        if (string.IsNullOrWhiteSpace(digits))
            yield break;

        var timezoneId = ResolveTimezone(digits);
        var (offset, localTimeStr, isBusinessHours, notice) = CalculateTimeInfo(timezoneId);

        var finding = new PhoneTimezoneFinding(
            PhoneNumber: normalized,
            TimezoneId: timezoneId,
            UtcOffset: offset,
            CurrentLocalTime: localTimeStr,
            IsBusinessHours: isBusinessHours,
            TimeNotice: notice);

        yield return new RawSearchFinding("phone-timezone_record", JsonSerializer.Serialize(finding));
    }

    private static string ResolveTimezone(string digits)
    {
        if (digits.StartsWith('7'))
        {
            if (digits.Length >= 4 && int.TryParse(digits[1..4], out var code))
            {
                return code switch
                {
                    >= 341 and <= 349 => "Russian Standard Time", // UTC+5 Yekaterinburg
                    >= 381 and <= 385 => "N. Central Asia Standard Time", // UTC+7 Novosibirsk
                    >= 391 and <= 395 => "North Asia Standard Time", // UTC+7 Krasnoyarsk
                    >= 421 and <= 424 => "Vladivostok Standard Time", // UTC+10 Vladivostok
                    _ => "Russian Standard Time" // Default MSK UTC+3
                };
            }

            return "Russian Standard Time";
        }

        if (digits.StartsWith('1'))
            return "Eastern Standard Time"; // US / Canada East

        if (digits.StartsWith("44"))
            return "GMT Standard Time"; // UK

        if (digits.StartsWith("49") || digits.StartsWith("33"))
            return "W. Europe Standard Time"; // Germany / France

        if (digits.StartsWith("81"))
            return "Tokyo Standard Time"; // Japan

        if (digits.StartsWith("86"))
            return "China Standard Time"; // China

        if (digits.StartsWith("971"))
            return "Arabian Standard Time"; // UAE

        return "UTC";
    }

    private static (string Offset, string LocalTime, bool IsBusinessHours, string Notice) CalculateTimeInfo(string tzId)
    {
        try
        {
            var tz = TimeZoneInfo.FindSystemTimeZoneById(tzId);
            var nowLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
            var offset = tz.GetUtcOffset(DateTime.UtcNow);
            var offsetStr = (offset >= TimeSpan.Zero ? "+" : "-") + offset.ToString(@"hh\:mm");

            var hour = nowLocal.Hour;
            bool isBusiness = hour is >= 9 and < 19;
            string notice = isBusiness
                ? "Active business hours in subscriber's region."
                : "Night / Non-working hours in subscriber's region.";

            return ($"UTC{offsetStr}", nowLocal.ToString("yyyy-MM-dd HH:mm:ss"), isBusiness, notice);
        }
        catch
        {
            var nowUtc = DateTime.UtcNow;
            return ("UTC+00:00", nowUtc.ToString("yyyy-MM-dd HH:mm:ss"), true, "UTC standard reference time.");
        }
    }
}
