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
using MeridianHost.Infrastructure.SearchSessions.Providers.Ip.ReverseDns.Contracts;

namespace MeridianHost.Infrastructure.SearchSessions.Providers.Ip.ReverseDns;

public class IpReverseDnsSearchProvider(HttpClient client) : ISearchProvider
{
    private readonly HttpClient _client = client;

    public string Key { get; } = "ip-rdns";

    public bool CanHandle(SearchTarget target)
        => target.TargetType == SearchTargetType.IpAddress;

    public async IAsyncEnumerable<RawSearchFinding> ReadAllAsync(
        SearchTarget target,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var rawIp = target.Value.Trim();
        if (!IPAddress.TryParse(rawIp, out var ipAddress))
            yield break;

        var arpaName = ToArpaDomain(ipAddress);
        if (string.IsNullOrEmpty(arpaName))
            yield break;

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"https://cloudflare-dns.com/dns-query?name={arpaName}&type=PTR");

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

        if (result?.Answers is null || result.Answers.Count == 0)
            yield break;

        var hostnames = result.Answers
            .Select(a => a.Data.TrimEnd('.'))
            .Where(h => !string.IsNullOrWhiteSpace(h))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (hostnames.Count == 0)
            yield break;

        var finding = new IpReverseDnsFinding(
            Ip: rawIp,
            ArpaQuery: arpaName,
            Hostnames: hostnames);

        yield return new RawSearchFinding("ip-rdns_record", JsonSerializer.Serialize(finding));
    }

    private static string ToArpaDomain(IPAddress ip)
    {
        var bytes = ip.GetAddressBytes();

        if (ip.AddressFamily == AddressFamily.InterNetwork)
        {
            return $"{bytes[3]}.{bytes[2]}.{bytes[1]}.{bytes[0]}.in-addr.arpa";
        }

        if (ip.AddressFamily == AddressFamily.InterNetworkV6)
        {
            var sb = new System.Text.StringBuilder(72);
            for (int i = bytes.Length - 1; i >= 0; i--)
            {
                sb.Append((bytes[i] & 0x0F).ToString("x"));
                sb.Append('.');
                sb.Append(((bytes[i] >> 4) & 0x0F).ToString("x"));
                sb.Append('.');
            }
            sb.Append("ip6.arpa");
            return sb.ToString();
        }

        return string.Empty;
    }
}
