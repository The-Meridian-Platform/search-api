using System.Text.Json.Serialization;

namespace MeridianHost.Infrastructure.SearchSessions.Providers.Email.Github.Contracts.Additional;

public sealed class GithubResponse
{
    [JsonPropertyName("total_count")]
    public int TotalCount { get; set; }
    
    [JsonPropertyName("items")]
    public List<GithubUserItem> Items { get; set; } = [];
}