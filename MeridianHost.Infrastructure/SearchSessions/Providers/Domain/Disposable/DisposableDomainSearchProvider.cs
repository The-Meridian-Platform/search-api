using System.Collections.Frozen;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json;
using MeridianHost.Application.SearchSessions.Execute.Providers;
using MeridianHost.Application.SearchSessions.Execute.Providers.Contracts;
using MeridianHost.Domain.SearchTargets;
using MeridianHost.Domain.SearchTargets.Enum;
using MeridianHost.Infrastructure.SearchSessions.Providers.Domain.Disposable.Contracts;

namespace MeridianHost.Infrastructure.SearchSessions.Providers.Domain.Disposable;

public class DisposableDomainSearchProvider(HttpClient client) : ISearchProvider
{
    private readonly HttpClient _client = client;
    private static readonly IdnMapping Idn = new();

    private static readonly FrozenSet<string> DisposableDomains = new[]
    {
        "10minutemail.com", "10minutemail.net", "tempmail.com", "temp-mail.org", "temp-mail.io",
        "guerrillamail.com", "guerrillamail.net", "guerrillamail.org", "guerrillamailblock.com",
        "mailinator.com", "yopmail.com", "yopmail.fr", "yopmail.net", "trashmail.com", "trashmail.net",
        "dispostable.com", "sharklasers.com", "grr.la", "pokemail.net", "fakemailgenerator.com",
        "throwawaymail.com", "getairmail.com", "mohmal.com", "crazymailing.com", "dropmail.me",
        "nada.ltd", "burnermail.io", "mytemp.email", "tempail.com", "disposablemail.com",
        "inboxkitten.com", "generator.email", "mailcatch.com", "minutemail.com", "emailondeck.com",
        "getnada.com", "abovethecurve.com", "airmail.cc", "anonaddy.com", "anonymbox.com",
        "binkmail.com", "bobmail.info", "chacuo.net", "chacuo.org", "courrieltmp.com",
        "cust.in", "dayrep.com", "discard.email", "discardmail.com", "disposable.com",
        "dodgeit.com", "drdrb.net", "dumpmail.de", "e4ward.com", "emailinfive.com",
        "emailtemporal.org", "fakemail.net", "fastmail.fm", "filzmail.com", "flemail.ru",
        "gishpuppy.com", "gustr.com", "harakirimail.com", "incognitomail.org", "jetable.org",
        "kasmail.com", "klzlk.com", "mail-temporaire.fr", "mailblocks.com", "mailnesia.com",
        "mailnull.com", "meltmail.com", "mytrashmail.com", "noclickemail.com", "nomail.xl.cx",
        "nospam.ze.tc", "nowmymail.com", "oneoffmail.com", "pookmail.com", "privacy.net",
        "quickinbox.com", "rcpt.at", "rmqkr.net", "safetymail.info", "saynotospams.com",
        "shortmail.net", "slopsbox.com", "sogetthis.com", "soodonims.com", "spambob.com",
        "spambog.com", "spambox.us", "spamcannon.com", "spamcon.org", "spamcorptastic.com",
        "spamcowboy.com", "spamday.com", "spamex.com", "spamfree24.org", "spamgourmet.com",
        "spamhole.com", "spaml.com", "spammotel.com", "spamspot.com", "spamtrap.ro",
        "suremail.info", "tafmail.com", "teleworm.us", "tempinbox.com", "temporaryemail.net",
        "temporaryinbox.com", "trash-mail.com", "trashmail.at", "trashmail.me", "trashmailer.com",
        "trbvm.com", "uroid.com", "veryrealemail.com", "wegwerfadresse.de", "wegwerfemail.de",
        "whyspam.me", "willselfdestruct.com", "zoemail.org", "0-mail.com", "0815.ru"
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    public string Key { get; } = "domain-disposable";

    public bool CanHandle(SearchTarget target)
        => target.TargetType == SearchTargetType.Domain;

    public async IAsyncEnumerable<RawSearchFinding> ReadAllAsync(
        SearchTarget target,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await Task.Yield();

        var domain = target.Value.Trim().ToLowerInvariant().TrimEnd('.');
        if (string.IsNullOrWhiteSpace(domain))
            yield break;

        var (isSuspicious, reason) = AnalyzeDomain(domain);

        var finding = new DisposableDomainFinding(
            Domain: domain,
            IsDisposable: isSuspicious,
            Reason: reason);

        yield return new RawSearchFinding("domain-disposable_record", JsonSerializer.Serialize(finding));
    }

    private static (bool IsSuspicious, string Reason) AnalyzeDomain(string domain)
    {
        if (DisposableDomains.Contains(domain))
            return (true, "Known disposable or temporary domain service");

        string unicodeDomain = domain;
        if (domain.Contains("xn--", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                unicodeDomain = Idn.GetUnicode(domain);
            }
            catch
            {
                return (true, "Invalid Punycode: IDN decoding failed");
            }
        }

        var labels = unicodeDomain.Split('.');
        foreach (var label in labels)
        {
            if (string.IsNullOrEmpty(label))
                continue;

            bool hasLatin = false;
            bool hasCyrillic = false;

            foreach (var ch in label)
            {
                if (ch == '-')
                    continue;

                if ((ch >= 'a' && ch <= 'z') || (ch >= 'A' && ch <= 'Z'))
                {
                    hasLatin = true;
                }
                else if ((ch >= 'а' && ch <= 'я') || (ch >= 'А' && ch <= 'Я') || ch == 'ё' || ch == 'Ё')
                {
                    hasCyrillic = true;
                }
                else if (char.IsDigit(ch))
                {
                    continue;
                }
                else
                {
                    return (true, $"Suspicious or invisible character detected: '{ch}' (U+{(int)ch:X4})");
                }

                if (hasLatin && hasCyrillic)
                {
                    return (true, "IDN Homograph attack detected: mixed Latin and Cyrillic scripts");
                }
            }
        }

        return (false, "Legitimate domain (passed disposable and homograph checks)");
    }
}
