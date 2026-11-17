namespace MeridianHost.Infrastructure.SearchSessions.Providers.Domain.JsLeaks.Contracts;

public sealed class JsSecretLeakFinding
{
    public string TargetDomain { get; set; } = string.Empty;
    public string ScriptUrl { get; set; } = string.Empty;
    public string SecretType { get; set; } = string.Empty;
    public string Severity { get; set; } = "High";
    public string MaskedMatch { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
