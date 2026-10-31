namespace MeridianHost.Infrastructure.SearchSessions.Providers.Domain.Http.Contracts;

public sealed record HttpTechFinding(
    string Url,
    int StatusCode,
    string? Server,
    string? PoweredBy,
    string? Title,
    bool HasHsts,
    bool HasCsp,
    bool HasXFrameOptions,
    string? ContentType);
