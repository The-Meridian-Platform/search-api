using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text.Json;
using MeridianHost.Application.SearchSessions.Execute.Providers;
using MeridianHost.Application.SearchSessions.Execute.Providers.Contracts;
using MeridianHost.Domain.SearchTargets;
using MeridianHost.Domain.SearchTargets.Enum;
using MeridianHost.Infrastructure.SearchSessions.Providers.Username.Presence.Contracts;

namespace MeridianHost.Infrastructure.SearchSessions.Providers.Username.Presence;

public class UsernamePresenceSearchProvider(HttpClient client) : ISearchProvider
{
    private readonly HttpClient _client = client;

    public string Key { get; } = "username-presence";

    public bool CanHandle(SearchTarget target)
        => target.TargetType == SearchTargetType.Username;

    public async IAsyncEnumerable<RawSearchFinding> ReadAllAsync(
        SearchTarget target,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var username = target.Value.Trim().TrimStart('@');
        if (string.IsNullOrWhiteSpace(username))
            yield break;

        var tasks = new[]
        {
            CheckRedditAsync(username, cancellationToken),
            CheckSteamAsync(username, cancellationToken),
            CheckGitLabAsync(username, cancellationToken),
            CheckHackerNewsAsync(username, cancellationToken)
        };

        var results = await Task.WhenAll(tasks);

        foreach (var finding in results)
        {
            if (finding is not null && finding.Exists)
            {
                yield return new RawSearchFinding("username-presence_record", JsonSerializer.Serialize(finding));
            }
        }
    }

    private async Task<UsernamePresenceFinding?> CheckRedditAsync(string username, CancellationToken ct)
    {
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, $"https://www.reddit.com/user/{username}/about.json");
            req.Headers.UserAgent.Add(new ProductInfoHeaderValue("Meridian-Platform", "1.0"));

            using var resp = await _client.SendAsync(req, ct);
            if (!resp.IsSuccessStatusCode)
                return null;

            using var doc = await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
            if (doc.RootElement.TryGetProperty("data", out var data) &&
                data.TryGetProperty("name", out var name) &&
                !string.IsNullOrWhiteSpace(name.GetString()))
            {
                var totalKarma = data.TryGetProperty("total_karma", out var karma) ? karma.GetInt32().ToString() : null;
                var details = totalKarma is not null ? $"Karma: {totalKarma}" : "Active Profile";

                return new UsernamePresenceFinding(
                    Username: username,
                    Platform: "Reddit",
                    Url: $"https://www.reddit.com/user/{username}",
                    Exists: true,
                    Details: details);
            }
        }
        catch
        {
        }

        return null;
    }

    private async Task<UsernamePresenceFinding?> CheckSteamAsync(string username, CancellationToken ct)
    {
        try
        {
            var url = $"https://steamcommunity.com/id/{username}";
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.UserAgent.Add(new ProductInfoHeaderValue("Mozilla", "5.0"));

            using var resp = await _client.SendAsync(req, ct);
            if (!resp.IsSuccessStatusCode)
                return null;

            var body = await resp.Content.ReadAsStringAsync(ct);
            if (!body.Contains("The specified profile could not be found", StringComparison.OrdinalIgnoreCase))
            {
                return new UsernamePresenceFinding(
                    Username: username,
                    Platform: "Steam",
                    Url: url,
                    Exists: true,
                    Details: "Public Steam Community Profile");
            }
        }
        catch
        {
        }

        return null;
    }

    private async Task<UsernamePresenceFinding?> CheckGitLabAsync(string username, CancellationToken ct)
    {
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, $"https://gitlab.com/api/v4/users?username={username}");
            req.Headers.UserAgent.Add(new ProductInfoHeaderValue("Meridian-Platform", "1.0"));

            using var resp = await _client.SendAsync(req, ct);
            if (!resp.IsSuccessStatusCode)
                return null;

            using var doc = await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
            if (doc.RootElement.ValueKind == JsonValueKind.Array && doc.RootElement.GetArrayLength() > 0)
            {
                var first = doc.RootElement[0];
                var name = first.TryGetProperty("name", out var n) ? n.GetString() : null;

                return new UsernamePresenceFinding(
                    Username: username,
                    Platform: "GitLab",
                    Url: $"https://gitlab.com/{username}",
                    Exists: true,
                    Details: name is not null ? $"Name: {name}" : "GitLab Profile");
            }
        }
        catch
        {
        }

        return null;
    }

    private async Task<UsernamePresenceFinding?> CheckHackerNewsAsync(string username, CancellationToken ct)
    {
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, $"https://hacker-news.firebaseio.com/v0/user/{username}.json");
            req.Headers.UserAgent.Add(new ProductInfoHeaderValue("Meridian-Platform", "1.0"));

            using var resp = await _client.SendAsync(req, ct);
            if (!resp.IsSuccessStatusCode)
                return null;

            using var doc = await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
            if (doc.RootElement.ValueKind == JsonValueKind.Object &&
                doc.RootElement.TryGetProperty("id", out var id) &&
                !string.IsNullOrWhiteSpace(id.GetString()))
            {
                var karma = doc.RootElement.TryGetProperty("karma", out var k) ? k.GetInt32().ToString() : null;
                var details = karma is not null ? $"Karma: {karma}" : "HackerNews Account";

                return new UsernamePresenceFinding(
                    Username: username,
                    Platform: "HackerNews",
                    Url: $"https://news.ycombinator.com/user?id={username}",
                    Exists: true,
                    Details: details);
            }
        }
        catch
        {
        }

        return null;
    }
}
