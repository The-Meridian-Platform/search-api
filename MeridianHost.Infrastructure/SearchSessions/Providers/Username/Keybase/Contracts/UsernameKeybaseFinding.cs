namespace MeridianHost.Infrastructure.SearchSessions.Providers.Username.Keybase.Contracts;

public sealed record KeybaseProofItem(
    string ProofType,
    string NameOnPlatform,
    string ServiceUrl);

public sealed record UsernameKeybaseFinding(
    string Username,
    bool Exists,
    string? FullName,
    string? Bio,
    string? Location,
    IReadOnlyList<KeybaseProofItem> ConnectedAccounts,
    IReadOnlyList<string> CryptoWallets);
