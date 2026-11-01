namespace MeridianHost.Infrastructure.SearchSessions.Providers.Email.Gravatar.Contracts;

public sealed record CleanGravatarResponse(
    string? Username,
    string? DisplayName,
    string? AboutMe,
    string? Location,
    string? ProfileUrl,
    string AvatarUrl);
