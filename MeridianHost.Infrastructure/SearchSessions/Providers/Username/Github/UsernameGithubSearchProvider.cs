using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using MeridianHost.Application.SearchSessions.Execute.Providers;
using MeridianHost.Application.SearchSessions.Execute.Providers.Contracts;
using MeridianHost.Domain.SearchTargets;
using MeridianHost.Domain.SearchTargets.Enum;
using MeridianHost.Infrastructure.SearchSessions.Providers.Username.Github.Contracts;

namespace MeridianHost.Infrastructure.SearchSessions.Providers.Username.Github;

public class UsernameGithubSearchProvider(HttpClient client) : ISearchProvider
{
    private readonly HttpClient _client = client;

    public string Key { get; } = "username-github";

    public bool CanHandle(SearchTarget target)
        => target.TargetType == SearchTargetType.Username;

    public async IAsyncEnumerable<RawSearchFinding> ReadAllAsync(
        SearchTarget target,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var username = target.Value.Trim();
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"https://api.github.com/users/{username}");

        request.Headers.UserAgent.Add(new ProductInfoHeaderValue("Meridian-Platform", "1.0"));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));

        GithubUserProfileResponse? profile = null;
        try
        {
            using var response = await _client.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
                yield break;

            profile = await response.Content.ReadFromJsonAsync<GithubUserProfileResponse>(cancellationToken);
        }
        catch
        {
            yield break;
        }

        if (profile is null || string.IsNullOrWhiteSpace(profile.Login))
            yield break;

        var finding = new UsernameGithubFinding(
            Id: profile.Id,
            Username: profile.Login,
            Name: profile.Name,
            ProfileUrl: string.IsNullOrWhiteSpace(profile.HtmlUrl) ? $"https://github.com/{profile.Login}" : profile.HtmlUrl,
            AvatarUrl: profile.AvatarUrl,
            Bio: profile.Bio,
            Company: profile.Company,
            Location: profile.Location,
            Email: profile.Email,
            Blog: profile.Blog,
            Twitter: profile.TwitterUsername,
            PublicRepos: profile.PublicRepos,
            Followers: profile.Followers);

        yield return new RawSearchFinding("username-github_record", JsonSerializer.Serialize(finding));
    }
}
