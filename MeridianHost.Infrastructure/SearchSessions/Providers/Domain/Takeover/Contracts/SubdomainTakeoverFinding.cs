namespace MeridianHost.Infrastructure.SearchSessions.Providers.Domain.Takeover.Contracts;

public sealed class SubdomainTakeoverFinding
{
    public string Subdomain { get; set; } = string.Empty;
    public string Service { get; set; } = string.Empty;
    public string Cname { get; set; } = string.Empty;
    public string Severity { get; set; } = "Critical";
    public string FingerprintMatched { get; set; } = string.Empty;
    public string Remediation { get; set; } = string.Empty;
}
