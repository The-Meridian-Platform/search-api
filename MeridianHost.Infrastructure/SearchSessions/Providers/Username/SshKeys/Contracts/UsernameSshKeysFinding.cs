namespace MeridianHost.Infrastructure.SearchSessions.Providers.Username.SshKeys.Contracts;

public sealed record SshKeyItem(
    string KeyType,
    string FingerprintSha256,
    string? Comment);

public sealed record UsernameSshKeysFinding(
    string Username,
    IReadOnlyList<SshKeyItem> Keys,
    IReadOnlyList<string> DiscoveredEmails,
    IReadOnlyList<string> DiscoveredHostnames);
