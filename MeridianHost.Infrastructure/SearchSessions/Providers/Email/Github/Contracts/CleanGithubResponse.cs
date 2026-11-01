namespace MeridianHost.Infrastructure.SearchSessions.Providers.Email.Github.Contracts;

public sealed record CleanGithubResponse(
    long Id,
    string Username,
    string ProfileId,
    string AvatarUrl,
    string AccountType);