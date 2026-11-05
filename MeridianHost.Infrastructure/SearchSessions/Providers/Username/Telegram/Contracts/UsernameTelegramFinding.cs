namespace MeridianHost.Infrastructure.SearchSessions.Providers.Username.Telegram.Contracts;

public sealed record UsernameTelegramFinding(
    string Username,
    string Title,
    string? Description,
    string? AvatarUrl,
    string Url);
