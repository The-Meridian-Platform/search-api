using System.Text.Json.Serialization;

namespace MeridianHost.Infrastructure.SearchSessions.Providers.Email.Github.Contracts.Additional;

public sealed class GithubUserItem
{
    [JsonPropertyName("id")]
    public long Id { get; set; }
    
    [JsonPropertyName("login")]
    public string Login { get; set; } = string.Empty;
    
    [JsonPropertyName("html_url")]
    public string HtmlUrl { get; set; } = string.Empty;
    
    [JsonPropertyName("avatar_url")]
    public string AvatarUrl { get; set; } = string.Empty;
    
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty; // "User" or "Organization"
    
    [JsonPropertyName("site_admin")]
    public bool SiteAdmin { get; set; }
}