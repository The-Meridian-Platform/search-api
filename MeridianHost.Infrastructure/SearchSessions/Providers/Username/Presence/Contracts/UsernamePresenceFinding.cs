namespace MeridianHost.Infrastructure.SearchSessions.Providers.Username.Presence.Contracts;

public sealed record UsernamePresenceFinding(
    string Username,
    string Platform,
    string Url,
    bool Exists,
    string? Details);
