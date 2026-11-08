using System.Collections.Frozen;
using System.Runtime.CompilerServices;
using System.Text.Json;
using MeridianHost.Application.SearchSessions.Execute.Providers;
using MeridianHost.Application.SearchSessions.Execute.Providers.Contracts;
using MeridianHost.Domain.SearchTargets;
using MeridianHost.Domain.SearchTargets.Enum;
using MeridianHost.Domain.SearchTargets.ValueObjects;
using MeridianHost.Infrastructure.SearchSessions.Providers.Phone.Carrier.Contracts;

namespace MeridianHost.Infrastructure.SearchSessions.Providers.Phone.Carrier;

public class PhoneCarrierGeoSearchProvider(HttpClient client) : ISearchProvider
{
    private readonly HttpClient _client = client;

    public string Key { get; } = "phone-carrier-geo";

    public bool CanHandle(SearchTarget target)
        => target.TargetType == SearchTargetType.PhoneNumber;

    public async IAsyncEnumerable<RawSearchFinding> ReadAllAsync(
        SearchTarget target,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await Task.Yield();

        var normalized = PhoneNumber.Normalize(target.Value);
        if (string.IsNullOrWhiteSpace(normalized))
            yield break;

        var finding = AnalyzePhone(target.Value, normalized);
        yield return new RawSearchFinding("phone-carrier_record", JsonSerializer.Serialize(finding));
    }

    private static PhoneCarrierFinding AnalyzePhone(string raw, string normalized)
    {
        var digits = normalized.TrimStart('+');

        var (country, countryCode, dialCodeLength) = ResolveCountry(digits);
        var localDigits = digits[dialCodeLength..];

        string carrier = "Standard Telecom Provider";
        string? region = null;
        string lineType = "Mobile";

        if (countryCode == "RU" || countryCode == "KZ")
        {
            if (localDigits.Length >= 3 && int.TryParse(localDigits[..3], out var defCode))
            {
                (carrier, region, lineType) = ResolveRuDef(defCode);
            }
        }
        else if (countryCode == "US" || countryCode == "CA")
        {
            lineType = "North American Numbering Plan (NANP)";
            carrier = "US/Canada Telecom Operator";
        }
        else if (countryCode == "GB")
        {
            lineType = localDigits.StartsWith('7') ? "Mobile" : "Geographic Landline";
            carrier = "UK Telecom Network";
        }
        else if (countryCode == "DE")
        {
            lineType = localDigits.StartsWith("15") || localDigits.StartsWith("16") || localDigits.StartsWith("17") ? "Mobile" : "Landline";
            carrier = "German Telecom Network";
        }

        string formatted = FormatInternational(countryCode, digits);

        return new PhoneCarrierFinding(
            RawPhoneNumber: raw,
            NormalizedNumber: normalized.StartsWith('+') ? normalized : $"+{normalized}",
            Country: country,
            CountryCode: countryCode,
            Region: region,
            Carrier: carrier,
            LineType: lineType,
            InternationalFormat: formatted,
            IsValid: digits.Length >= 7 && digits.Length <= 15);
    }

    private static (string Country, string Code, int DialLength) ResolveCountry(string digits)
    {
        for (int len = Math.Min(4, digits.Length); len >= 1; len--)
        {
            var prefix = digits[..len];
            if (CountryDialCodes.TryGetValue(prefix, out var info))
            {
                return (info.Country, info.Code, len);
            }
        }

        return ("International / Unknown", "XX", 0);
    }

    private static (string Carrier, string? Region, string LineType) ResolveRuDef(int def)
    {
        return def switch
        {
            >= 910 and <= 919 or >= 980 and <= 989 => ("МТС (MTS)", "Федеральный оператор", "Mobile"),
            >= 920 and <= 929 or >= 930 and <= 939 => ("МегаФон (MegaFon)", "Федеральный оператор", "Mobile"),
            >= 960 and <= 969 or 903 or 905 or 906 => ("Билайн (Beeline / VimpelCom)", "Федеральный оператор", "Mobile"),
            900 or 901 or 902 or 904 or 908 or 950 or 951 or 952 or 953 or 977 or 999 => 
                ("Tele2 / Ростелеком / MVNO (T-Mobile/Sber/Yota)", "Федеральный MVNO оператор", "Mobile"),
            >= 495 and <= 499 => ("Стационарная связь", "Москва / Московская область", "Landline"),
            812 => ("Стационарная связь", "Санкт-Петербург / Ленинградская область", "Landline"),
            800 => ("Toll-Free (Бесплатный вызов 8-800)", "Российская Федерация", "Toll-Free"),
            _ => ("Российский оператор связи", null, "Mobile/Fixed")
        };
    }

    private static string FormatInternational(string countryCode, string digits)
    {
        if (countryCode == "RU" && digits.Length == 11)
        {
            return $"+{digits[0]} ({digits[1..4]}) {digits[4..7]}-{digits[7..9]}-{digits[9..11]}";
        }

        if ((countryCode == "US" || countryCode == "CA") && digits.Length == 11)
        {
            return $"+{digits[0]} ({digits[1..4]}) {digits[4..7]}-{digits[7..11]}";
        }

        return $"+{digits}";
    }

    private static readonly FrozenDictionary<string, (string Country, string Code)> CountryDialCodes = new Dictionary<string, (string, string)>
    {
        { "1", ("United States / Canada", "US") },
        { "7", ("Russia / Kazakhstan", "RU") },
        { "20", ("Egypt", "EG") },
        { "27", ("South Africa", "ZA") },
        { "30", ("Greece", "GR") },
        { "31", ("Netherlands", "NL") },
        { "32", ("Belgium", "BE") },
        { "33", ("France", "FR") },
        { "34", ("Spain", "ES") },
        { "36", ("Hungary", "HU") },
        { "39", ("Italy", "IT") },
        { "40", ("Romania", "RO") },
        { "41", ("Switzerland", "CH") },
        { "43", ("Austria", "AT") },
        { "44", ("United Kingdom", "GB") },
        { "45", ("Denmark", "DK") },
        { "46", ("Sweden", "SE") },
        { "47", ("Norway", "NO") },
        { "48", ("Poland", "PL") },
        { "49", ("Germany", "DE") },
        { "51", ("Peru", "PE") },
        { "52", ("Mexico", "MX") },
        { "54", ("Argentina", "AR") },
        { "55", ("Brazil", "BR") },
        { "60", ("Malaysia", "MY") },
        { "61", ("Australia", "AU") },
        { "62", ("Indonesia", "ID") },
        { "63", ("Philippines", "PH") },
        { "64", ("New Zealand", "NZ") },
        { "65", ("Singapore", "SG") },
        { "66", ("Thailand", "TH") },
        { "81", ("Japan", "JP") },
        { "82", ("South Korea", "KR") },
        { "84", ("Vietnam", "VN") },
        { "86", ("China", "CN") },
        { "90", ("Turkey", "TR") },
        { "91", ("India", "IN") },
        { "92", ("Pakistan", "PK") },
        { "93", ("Afghanistan", "AF") },
        { "94", ("Sri Lanka", "LK") },
        { "95", ("Myanmar", "MM") },
        { "98", ("Iran", "IR") },
        { "212", ("Morocco", "MA") },
        { "213", ("Algeria", "DZ") },
        { "216", ("Tunisia", "TN") },
        { "351", ("Portugal", "PT") },
        { "352", ("Luxembourg", "LU") },
        { "353", ("Ireland", "IE") },
        { "354", ("Iceland", "IS") },
        { "358", ("Finland", "FI") },
        { "370", ("Lithuania", "LT") },
        { "371", ("Latvia", "LV") },
        { "372", ("Estonia", "EE") },
        { "375", ("Belarus", "BY") },
        { "380", ("Ukraine", "UA") },
        { "381", ("Serbia", "RS") },
        { "420", ("Czech Republic", "CZ") },
        { "971", ("United Arab Emirates", "AE") },
        { "972", ("Israel", "IL") }
    }.ToFrozenDictionary();
}
