using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using MeridianHost.Application.SearchSessions.Execute.Providers;
using MeridianHost.Application.SearchSessions.Execute.Providers.Contracts;
using MeridianHost.Domain.SearchTargets;
using MeridianHost.Domain.SearchTargets.Enum;
using MeridianHost.Infrastructure.SearchSessions.Providers.Domain.Wayback.Contracts;

namespace MeridianHost.Infrastructure.SearchSessions.Providers.Domain.Wayback;

public class DomainWaybackSearchProvider(HttpClient client) : ISearchProvider
{
    private readonly HttpClient _client = client;

    public string Key { get; } = "domain-wayback";

    public bool CanHandle(SearchTarget target)
        => target.TargetType == SearchTargetType.Domain;

    public async IAsyncEnumerable<RawSearchFinding> ReadAllAsync(
        SearchTarget target,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var domain = target.Value.Trim().ToLowerInvariant().TrimEnd('.');
        if (string.IsNullOrWhiteSpace(domain))
            yield break;

        var availableTask = CheckAvailableAsync(domain, cancellationToken);
        var earliestTask = CheckEarliestAsync(domain, cancellationToken);

        await Task.WhenAll(availableTask, earliestTask);

        var (hasSnapshot, latestUrl, latestTs) = await availableTask;
        var earliestTs = await earliestTask;

        if (!hasSnapshot && string.IsNullOrEmpty(earliestTs))
            yield break;

        string? firstSeenFormatted = null;
        int? estimatedAge = null;

        var primaryTs = earliestTs ?? latestTs;
        if (!string.IsNullOrEmpty(primaryTs) && primaryTs.Length >= 8)
        {
            if (DateTime.TryParseExact(
                    primaryTs[..8],
                    "yyyyMMdd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var parsedDate))
            {
                firstSeenFormatted = parsedDate.ToString("yyyy-MM-dd");
                estimatedAge = Math.Max(0, DateTime.UtcNow.Year - parsedDate.Year);
            }
        }

        var finding = new DomainWaybackFinding(
            Domain: domain,
            HasSnapshots: hasSnapshot,
            FirstSeenDate: firstSeenFormatted,
            LatestSnapshotUrl: latestUrl,
            LatestSnapshotTimestamp: latestTs,
            EstimatedAgeYears: estimatedAge);

        yield return new RawSearchFinding("domain-wayback_record", JsonSerializer.Serialize(finding));
    }

    private async Task<(bool HasSnapshot, string? LatestUrl, string? LatestTs)> CheckAvailableAsync(string domain, CancellationToken ct)
    {
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, $"https://archive.org/wayback/available?url={domain}");
            req.Headers.UserAgent.Add(new ProductInfoHeaderValue("Meridian-Platform", "1.0"));

            using var resp = await _client.SendAsync(req, ct);
            if (!resp.IsSuccessStatusCode)
                return (false, null, null);

            using var doc = await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
            if (doc.RootElement.TryGetProperty("archived_snapshots", out var snapshots) &&
                snapshots.TryGetProperty("closest", out var closest) &&
                closest.TryGetProperty("available", out var avail) &&
                avail.GetBoolean())
            {
                var url = closest.TryGetProperty("url", out var u) ? u.GetString() : null;
                var ts = closest.TryGetProperty("timestamp", out var t) ? t.GetString() : null;
                return (true, url, ts);
            }
        }
        catch
        {
        }

        return (false, null, null);
    }

    private async Task<string?> CheckEarliestAsync(string domain, CancellationToken ct)
    {
        try
        {
            using var req = new HttpRequestMessage(
                HttpMethod.Get,
                $"https://web.archive.org/cdx/search/cdx?url={domain}&output=json&limit=1&fl=timestamp");
            req.Headers.UserAgent.Add(new ProductInfoHeaderValue("Meridian-Platform", "1.0"));

            using var resp = await _client.SendAsync(req, ct);
            if (!resp.IsSuccessStatusCode)
                return null;

            using var doc = await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
            if (doc.RootElement.ValueKind == JsonValueKind.Array && doc.RootElement.GetArrayLength() > 1)
            {
                var row = doc.RootElement[1];
                if (row.ValueKind == JsonValueKind.Array && row.GetArrayLength() > 0)
                {
                    return row[0].GetString();
                }
            }
        }
        catch
        {
        }

        return null;
    }
}
