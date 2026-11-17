namespace MeridianHost.Infrastructure.SearchSessions.Providers.Domain.SecurityAudit.Contracts;

public sealed class WebSecurityAuditFinding
{
    public string Target { get; set; } = string.Empty;
    public string AuditCategory { get; set; } = string.Empty;
    public string Severity { get; set; } = "Low";
    public string Issue { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Remediation { get; set; } = string.Empty;
}
