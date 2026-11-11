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
using MeridianHost.Infrastructure.SearchSessions.Providers.Ip.Tor.Contracts;

namespace MeridianHost.Infrastructure.SearchSessions.Providers.Ip.Tor;

public class IpTorExitNodeSearchProvider(HttpClient client) : ISearchProvider
{
    private readonly HttpClient _client = client;

    public string Key { get; } = "ip-tor";

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
        var torDnsQuery = $"{reversedIp}.dnsel.torproject.org";

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"https://cloudflare-dns.com/dns-query?name={torDnsQuery}&type=A");

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

        bool isTor = result?.Answers is not null && result.Answers.Any(a => a.Data == "127.0.0.2");

        var finding = new IpTorFinding(
            Ip: rawIp,
            IsTorExitNode: isTor,
            NetworkType: isTor ? "The Onion Router (Tor) Exit Node" : "Standard Internet Host",
            Severity: isTor ? "Warning" : "Secure",
            Summary: isTor
                ? "Host identified as an active Tor network exit relay. Traffic from this IP is anonymized."
                : "Host is not part of the public Tor exit network.");

        yield return new RawSearchFinding("ip-tor_record", JsonSerializer.Serialize(finding));
    }
}
