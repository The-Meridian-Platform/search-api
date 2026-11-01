using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using MeridianHost.Application.SearchSessions.Execute.Providers;
using MeridianHost.Application.SearchSessions.Execute.Providers.Contracts;
using MeridianHost.Domain.SearchTargets;
using MeridianHost.Domain.SearchTargets.Enum;
using MeridianHost.Infrastructure.SearchSessions.Providers.Email.Github.Contracts;
using MeridianHost.Infrastructure.SearchSessions.Providers.Email.Github.Contracts.Additional;

namespace MeridianHost.Infrastructure.SearchSessions.Providers.Email.Github;

public class GithubSearchProvider(HttpClient client) : ISearchProvider
{
    private readonly HttpClient _client = client;
    
    public string Key { get; } = "email-github";

    public bool CanHandle(SearchTarget target)
        => target.TargetType == SearchTargetType.Email;

    public async IAsyncEnumerable<RawSearchFinding> ReadAllAsync(
        SearchTarget target, 
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"https://api.github.com/search/users?q={target.Value}+in:email");
        
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) Meridian/1.0");

        GithubResponse? result;

        try
        {
            using var response = await _client.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
                yield break;

            result = await response.Content.ReadFromJsonAsync<GithubResponse>(cancellationToken);
            if (result is null || result.Items.Count == 0)
                yield break;
        }
        catch 
        {
            yield break;
        }

        foreach (var user in result.Items)
        {
            var clean = new CleanGithubResponse(
                user.Id,
                user.Login,
                user.HtmlUrl,
                user.AvatarUrl,
                user.Type);
            
            yield return new RawSearchFinding("github-email_record", JsonSerializer.Serialize(clean));
        }
    }
}
