using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text.Json;
using MeridianHost.Application.SearchSessions.Execute.Providers;
using MeridianHost.Application.SearchSessions.Execute.Providers.Contracts;
using MeridianHost.Domain.SearchTargets;
using MeridianHost.Domain.SearchTargets.Enum;
using MeridianHost.Infrastructure.SearchSessions.Providers.Domain.Dns.Contracts;
using MeridianHost.Infrastructure.SearchSessions.Providers.Ip.Dnsbl.Contracts;

namespace MeridianHost.Infrastructure.SearchSessions.Providers.Ip.Dnsbl;

public class IpDnsblSearchProvider(HttpClient client) : ISearchProvider
{
    private readonly HttpClient _client = client;

    public string Key { get; } = "ip-dnsbl";

    public bool CanHandle(SearchTarget target)
        => target.TargetType == SearchTargetType.IpAddress;

    public async IAsyncEnumerable<RawSearchFinding> ReadAllAsync(
        SearchTarget target,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var rawIp = target.Value.Trim();
        if (!IPAddress.TryParse(rawIp, out var ipAddress) || ipAddress.AddressFamily != AddressFamily.InterNetwork)
            yield break;

        var bytes = ipAddress.GetAddressBytes();
        var reversedIp = $"{bytes[3]}.{bytes[2]}.{bytes[1]}.{bytes[0]}";
        var dnsblQuery = $"{reversedIp}.zen.spamhaus.org";

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"https://cloudflare-dns.com/dns-query?name={dnsblQuery}&type=A");

        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/dns-json"));

        DnsResponse? result = null;
        try
        {
            using var response = await _client.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
                yield break;

            result = await response.Content.ReadFromJsonAsync<DnsResponse>(cancellationToken);
        }
        catch
        {
            yield break;
        }

        var isListed = result?.Answers is not null && result.Answers.Count > 0;
        string? returnCode = isListed ? result!.Answers![0].Data : null;

        var (threatType, severity, details) = InterpretSpamhausCode(returnCode);

        var finding = new IpDnsblFinding(
            Ip: rawIp,
            IsListed: isListed,
            BlacklistProvider: "Spamhaus ZEN (SBL/CSS/XBL/PBL)",
            ReturnCode: returnCode,
            ThreatType: threatType,
            Severity: severity,
            Details: details);

        yield return new RawSearchFinding("ip-dnsbl_record", JsonSerializer.Serialize(finding));
    }

    private static (string ThreatType, string Severity, string Details) InterpretSpamhausCode(string? code)
    {
        return code switch
        {
            "127.0.0.2" => ("Direct Spam Source (SBL)", "Critical", "Verified direct spam source listed in Spamhaus SBL."),
            "127.0.0.3" => ("Snowshoe Spam (CSS)", "High", "Listed in Spamhaus CSS for snowshoe spam operations."),
            "127.0.0.4" or "127.0.0.5" or "127.0.0.6" or "127.0.0.7" => 
                ("Compromised Host / Botnet (XBL)", "Critical", "Host is infected with malware, trojans or acting as a botnet node (CBL/XBL)."),
            "127.0.0.10" or "127.0.0.11" => 
                ("End-User IP / Dynamic Pool (PBL)", "Low", "End-user residential IP or dynamic pool, should not act as public mail exchanger."),
            _ when code is not null => 
                ("Malicious Host", "High", $"Listed on Spamhaus with status code {code}."),
            _ => 
                ("Clean / Not Listed", "Secure", "IP is not listed on Spamhaus ZEN. Clean reputation.")
        };
    }
}
