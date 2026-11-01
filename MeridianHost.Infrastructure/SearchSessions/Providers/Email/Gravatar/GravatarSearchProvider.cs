using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MeridianHost.Application.SearchSessions.Execute.Providers;
using MeridianHost.Application.SearchSessions.Execute.Providers.Contracts;
using MeridianHost.Domain.SearchTargets;
using MeridianHost.Domain.SearchTargets.Enum;
using MeridianHost.Infrastructure.SearchSessions.Providers.Email.Gravatar.Contracts;
using MeridianHost.Infrastructure.SearchSessions.Providers.Email.Gravatar.Contracts.Additional;

namespace MeridianHost.Infrastructure.SearchSessions.Providers.Email.Gravatar;

public class GravatarSearchProvider(HttpClient client) : ISearchProvider
{
    private readonly HttpClient _client = client;

    public string Key { get; } = "email-gravatar";

    public bool CanHandle(SearchTarget target)
        => target.TargetType == SearchTargetType.Email;

    public async IAsyncEnumerable<RawSearchFinding> ReadAllAsync(
        SearchTarget target,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var normalizedEmail = target.Value.Trim().ToLowerInvariant();
        var hashBytes = MD5.HashData(Encoding.UTF8.GetBytes(normalizedEmail));
        var hash = Convert.ToHexStringLower(hashBytes);

        var avatarUrl = $"https://www.gravatar.com/avatar/{hash}";

        using var jsonRequest = new HttpRequestMessage(HttpMethod.Get, $"https://en.gravatar.com/{hash}.json");
        jsonRequest.Headers.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) Meridian/1.0");
        jsonRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        GravatarProfileResponse? profileResponse = null;

        try
        {
            using var response = await _client.SendAsync(jsonRequest, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                profileResponse = await response.Content.ReadFromJsonAsync<GravatarProfileResponse>(cancellationToken);
            }
        }
        catch
        {
        }

        if (profileResponse?.Entry is { Count: > 0 })
        {
            var entry = profileResponse.Entry[0];
            var clean = new CleanGravatarResponse(
                Username: entry.PreferredUsername,
                DisplayName: entry.DisplayName,
                AboutMe: entry.AboutMe,
                Location: entry.CurrentLocation,
                ProfileUrl: entry.ProfileUrl,
                AvatarUrl: entry.ThumbnailUrl ?? avatarUrl);

            yield return new RawSearchFinding("gravatar-email_record", JsonSerializer.Serialize(clean));
            yield break;
        }

        using var headRequest = new HttpRequestMessage(HttpMethod.Head, $"{avatarUrl}?d=404");
        headRequest.Headers.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) Meridian/1.0");

        bool avatarExists = false;
        try
        {
            using var headResponse = await _client.SendAsync(headRequest, cancellationToken);
            avatarExists = headResponse.IsSuccessStatusCode;
        }
        catch
        {
        }

        if (avatarExists)
        {
            var clean = new CleanGravatarResponse(
                Username: null,
                DisplayName: null,
                AboutMe: null,
                Location: null,
                ProfileUrl: null,
                AvatarUrl: avatarUrl);

            yield return new RawSearchFinding("gravatar-email_record", JsonSerializer.Serialize(clean));
        }
    }
}
