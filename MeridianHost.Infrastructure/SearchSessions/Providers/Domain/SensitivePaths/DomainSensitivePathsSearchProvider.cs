using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text.Json;
using MeridianHost.Application.SearchSessions.Execute.Providers;
using MeridianHost.Application.SearchSessions.Execute.Providers.Contracts;
using MeridianHost.Domain.SearchTargets;
using MeridianHost.Domain.SearchTargets.Enum;
using MeridianHost.Infrastructure.SearchSessions.Providers.Domain.SensitivePaths.Contracts;

namespace MeridianHost.Infrastructure.SearchSessions.Providers.Domain.SensitivePaths;

public class DomainSensitivePathsSearchProvider(HttpClient client) : ISearchProvider
{
    private readonly HttpClient _client = client;

    public string Key { get; } = "domain-sensitive";

    public bool CanHandle(SearchTarget target)
        => target.TargetType == SearchTargetType.Domain;

    public async IAsyncEnumerable<RawSearchFinding> ReadAllAsync(
        SearchTarget target,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var domain = target.Value.Trim().ToLowerInvariant().TrimEnd('.');
        if (string.IsNullOrWhiteSpace(domain))
            yield break;

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(3.0));

        var tasks = new[]
        {
            ProbeEnvAsync(domain, cts.Token),
            ProbeGitHeadAsync(domain, cts.Token),
            ProbeRobotsAsync(domain, cts.Token),
            ProbeSitemapAsync(domain, cts.Token),
            ProbeSwaggerAsync(domain, cts.Token),
            ProbeActuatorAsync(domain, cts.Token)
        };

        var results = await Task.WhenAll(tasks);
        var discoveredItems = results.Where(r => r is not null).Select(r => r!).ToList();

        if (discoveredItems.Count == 0)
            yield break;

        bool hasCritical = discoveredItems.Any(i => string.Equals(i.Severity, "Critical", StringComparison.OrdinalIgnoreCase));

        var finding = new DomainSensitivePathsFinding(
            Domain: domain,
            TotalDiscovered: discoveredItems.Count,
            HasCriticalLeaks: hasCritical,
            Items: discoveredItems);

        yield return new RawSearchFinding("domain-sensitive_record", JsonSerializer.Serialize(finding));
    }

    private async Task<DiscoveredPathItem?> ProbeEnvAsync(string domain, CancellationToken ct)
    {
        try
        {
            var url = $"https://{domain}/.env";
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.UserAgent.Add(new ProductInfoHeaderValue("Meridian-Platform", "1.0"));

            using var resp = await _client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
            if (!resp.IsSuccessStatusCode)
                return null;

            var ctype = resp.Content.Headers.ContentType?.MediaType ?? string.Empty;
            if (ctype.Contains("html", StringComparison.OrdinalIgnoreCase))
                return null;

            var body = await resp.Content.ReadAsStringAsync(ct);
            if (string.IsNullOrWhiteSpace(body))
                return null;

            if (body.Contains("DB_PASSWORD", StringComparison.OrdinalIgnoreCase) ||
                body.Contains("APP_KEY", StringComparison.OrdinalIgnoreCase) ||
                body.Contains("SECRET", StringComparison.OrdinalIgnoreCase) ||
                body.Contains("API_KEY", StringComparison.OrdinalIgnoreCase) ||
                body.Contains("PORT=", StringComparison.OrdinalIgnoreCase) ||
                body.Contains("DATABASE_URL", StringComparison.OrdinalIgnoreCase))
            {
                return new DiscoveredPathItem(
                    Path: "/.env",
                    FullUrl: url,
                    StatusCode: (int)resp.StatusCode,
                    Category: "Environment Secrets / Config",
                    Severity: "Critical",
                    Summary: "Exposed environment configuration file containing database passwords or API keys.");
            }
        }
        catch
        {
        }

        return null;
    }

    private async Task<DiscoveredPathItem?> ProbeGitHeadAsync(string domain, CancellationToken ct)
    {
        try
        {
            var url = $"https://{domain}/.git/HEAD";
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.UserAgent.Add(new ProductInfoHeaderValue("Meridian-Platform", "1.0"));

            using var resp = await _client.SendAsync(req, ct);
            if (!resp.IsSuccessStatusCode)
                return null;

            var body = await resp.Content.ReadAsStringAsync(ct);
            if (!string.IsNullOrWhiteSpace(body) && body.TrimStart().StartsWith("ref: refs/", StringComparison.OrdinalIgnoreCase))
            {
                return new DiscoveredPathItem(
                    Path: "/.git/HEAD",
                    FullUrl: url,
                    StatusCode: (int)resp.StatusCode,
                    Category: "Exposed Source Repository",
                    Severity: "Critical",
                    Summary: "Exposed .git directory. Full source code and commit history can be downloaded.");
            }
        }
        catch
        {
        }

        return null;
    }

    private async Task<DiscoveredPathItem?> ProbeRobotsAsync(string domain, CancellationToken ct)
    {
        try
        {
            var url = $"https://{domain}/robots.txt";
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.UserAgent.Add(new ProductInfoHeaderValue("Meridian-Platform", "1.0"));

            using var resp = await _client.SendAsync(req, ct);
            if (!resp.IsSuccessStatusCode)
                return null;

            var body = await resp.Content.ReadAsStringAsync(ct);
            if (!string.IsNullOrWhiteSpace(body) &&
                (body.Contains("User-agent:", StringComparison.OrdinalIgnoreCase) || body.Contains("Disallow:", StringComparison.OrdinalIgnoreCase)))
            {
                return new DiscoveredPathItem(
                    Path: "/robots.txt",
                    FullUrl: url,
                    StatusCode: (int)resp.StatusCode,
                    Category: "Robots Crawler Policy",
                    Severity: "Info",
                    Summary: "Public robots.txt file discovered. May reveal hidden administration endpoints.");
            }
        }
        catch
        {
        }

        return null;
    }

    private async Task<DiscoveredPathItem?> ProbeSitemapAsync(string domain, CancellationToken ct)
    {
        try
        {
            var url = $"https://{domain}/sitemap.xml";
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.UserAgent.Add(new ProductInfoHeaderValue("Meridian-Platform", "1.0"));

            using var resp = await _client.SendAsync(req, ct);
            if (!resp.IsSuccessStatusCode)
                return null;

            var body = await resp.Content.ReadAsStringAsync(ct);
            if (!string.IsNullOrWhiteSpace(body) && (body.Contains("<urlset", StringComparison.OrdinalIgnoreCase) || body.Contains("<sitemapindex", StringComparison.OrdinalIgnoreCase)))
            {
                return new DiscoveredPathItem(
                    Path: "/sitemap.xml",
                    FullUrl: url,
                    StatusCode: (int)resp.StatusCode,
                    Category: "XML Sitemap",
                    Severity: "Info",
                    Summary: "Sitemap XML map available, indexing public website structure.");
            }
        }
        catch
        {
        }

        return null;
    }

    private async Task<DiscoveredPathItem?> ProbeSwaggerAsync(string domain, CancellationToken ct)
    {
        try
        {
            var endpoints = new[] { "/swagger/v1/swagger.json", "/swagger.json", "/api-docs" };
            foreach (var ep in endpoints)
            {
                var url = $"https://{domain}{ep}";
                using var req = new HttpRequestMessage(HttpMethod.Get, url);
                req.Headers.UserAgent.Add(new ProductInfoHeaderValue("Meridian-Platform", "1.0"));

                using var resp = await _client.SendAsync(req, ct);
                if (!resp.IsSuccessStatusCode)
                    continue;

                var body = await resp.Content.ReadAsStringAsync(ct);
                if (!string.IsNullOrWhiteSpace(body) && (body.Contains("\"openapi\":", StringComparison.OrdinalIgnoreCase) || body.Contains("\"swagger\":", StringComparison.OrdinalIgnoreCase)))
                {
                    return new DiscoveredPathItem(
                        Path: ep,
                        FullUrl: url,
                        StatusCode: (int)resp.StatusCode,
                        Category: "OpenAPI / Swagger Spec",
                        Severity: "Warning",
                        Summary: "Exposed API specification allowing reconnaissance of internal backend endpoints.");
                }
            }
        }
        catch
        {
        }

        return null;
    }

    private async Task<DiscoveredPathItem?> ProbeActuatorAsync(string domain, CancellationToken ct)
    {
        try
        {
            var url = $"https://{domain}/actuator/health";
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.UserAgent.Add(new ProductInfoHeaderValue("Meridian-Platform", "1.0"));

            using var resp = await _client.SendAsync(req, ct);
            if (!resp.IsSuccessStatusCode)
                return null;

            var body = await resp.Content.ReadAsStringAsync(ct);
            if (!string.IsNullOrWhiteSpace(body) && body.Contains("\"status\":", StringComparison.OrdinalIgnoreCase))
            {
                return new DiscoveredPathItem(
                    Path: "/actuator/health",
                    FullUrl: url,
                    StatusCode: (int)resp.StatusCode,
                    Category: "Spring Boot Actuator",
                    Severity: "Warning",
                    Summary: "Exposed Spring Boot Actuator health endpoint revealing backend status.");
            }
        }
        catch
        {
        }

        return null;
    }
}
