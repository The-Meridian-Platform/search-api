namespace MeridianHost.Infrastructure.SearchSessions.Providers.Username.Github.Contracts;

public sealed record UsernameGithubFinding(
    long Id,
    string Username,
    string? Name,
    string ProfileUrl,
    string AvatarUrl,
    string? Bio,
    string? Company,
    string? Location,
    string? Email,
    string? Blog,
    string? Twitter,
    int PublicRepos,
    int Followers);
