using System.Text.Json.Serialization;

namespace MeridianHost.Infrastructure.SearchSessions.Providers.Email.Gravatar.Contracts.Additional;

public sealed class GravatarProfileResponse
{
    [JsonPropertyName("entry")]
    public List<GravatarEntry> Entry { get; set; } = [];
}

public sealed class GravatarEntry
{
    [JsonPropertyName("preferredUsername")]
    public string? PreferredUsername { get; set; }

    [JsonPropertyName("displayName")]
    public string? DisplayName { get; set; }

    [JsonPropertyName("aboutMe")]
    public string? AboutMe { get; set; }

    [JsonPropertyName("currentLocation")]
    public string? CurrentLocation { get; set; }

    [JsonPropertyName("profileUrl")]
    public string? ProfileUrl { get; set; }

    [JsonPropertyName("thumbnailUrl")]
    public string? ThumbnailUrl { get; set; }
}
