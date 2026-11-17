using System.Runtime.CompilerServices;
using System.Text.Json;
using MeridianHost.Application.SearchSessions.Execute.Providers;
using MeridianHost.Application.SearchSessions.Execute.Providers.Contracts;
using MeridianHost.Domain.SearchTargets;
using MeridianHost.Domain.SearchTargets.Enum;
using MeridianHost.Infrastructure.SearchSessions.Providers.Domain.SecurityAudit.Contracts;

namespace MeridianHost.Infrastructure.SearchSessions.Providers.Domain.SecurityAudit;

public class DomainSecurityAuditSearchProvider(HttpClient client) : ISearchProvider
{
    private readonly HttpClient _client = client;

    public string Key { get; } = "domain-security-audit";

    public bool CanHandle(SearchTarget target)
        => target.TargetType == SearchTargetType.Domain;

    public async IAsyncEnumerable<RawSearchFinding> ReadAllAsync(
        SearchTarget target,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var domain = target.Value.Trim();
        var uri = new Uri($"https://{domain}");

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(5));

        HttpResponseMessage? response = null;
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, uri);
            request.Headers.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) Meridian/1.0");
            request.Headers.Add("Origin", "https://evil-attacker.com");

            response = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeoutCts.Token);
        }
        catch
        {
            try
            {
                using var httpReq = new HttpRequestMessage(HttpMethod.Get, $"http://{domain}");
                httpReq.Headers.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) Meridian/1.0");
                httpReq.Headers.Add("Origin", "https://evil-attacker.com");
                response = await _client.SendAsync(httpReq, HttpCompletionOption.ResponseHeadersRead, timeoutCts.Token);
            }
            catch
            {
                yield break;
            }
        }

        if (response == null)
            yield break;

        using (response)
        {
            var headers = response.Headers;

            if (headers.TryGetValues("Access-Control-Allow-Origin", out var allowOrigins))
            {
                var allowOriginVal = string.Join(",", allowOrigins).Trim();
                headers.TryGetValues("Access-Control-Allow-Credentials", out var allowCreds);
                var allowCredsVal = string.Join(",", allowCreds ?? []).Trim();

                if (allowOriginVal.Equals("https://evil-attacker.com", StringComparison.OrdinalIgnoreCase) &&
                    allowCredsVal.Equals("true", StringComparison.OrdinalIgnoreCase))
                {
                    yield return CreateFinding(domain, "CORS", "Critical",
                        "Критическая уязвимость CORS (Arbitrary Origin with Credentials)",
                        "Сервер доверяет любому произвольному Origin и разрешает передачу учетных данных (куки, Authorization). Возможна кража сессий пользователей через внешний скрипт.",
                        "Запретить отражение заголовка Origin при Access-Control-Allow-Credentials: true.");
                }
                else if (allowOriginVal == "*")
                {
                    yield return CreateFinding(domain, "CORS", "Low",
                        "Открытый заголовок CORS (*)",
                        "API разрешает запросы с любых доменов в интернете (Access-Control-Allow-Origin: *).",
                        "Убедитесь, что эндпоинт не возвращает конфиденциальные персональные данные.");
                }
            }

            if (!headers.Contains("Strict-Transport-Security"))
            {
                yield return CreateFinding(domain, "Headers", "Medium",
                    "Отсутствует заголовок HSTS (Strict-Transport-Security)",
                    "Домен не принуждает браузеры использовать исключительно HTTPS. Пользователи уязвимы к атакам SSL-Stripping и перехвату трафика в незащищенных Wi-Fi сетях.",
                    "Добавить заголовок: Strict-Transport-Security: max-age=31536000; includeSubDomains; preload");
            }

            if (!headers.Contains("X-Frame-Options") && !headers.Contains("Content-Security-Policy"))
            {
                yield return CreateFinding(domain, "Headers", "Medium",
                    "Уязвимость к Clickjacking (отсутствуют X-Frame-Options и frame-ancestors)",
                    "Сайт может быть встроен в невидимый iframe на вредоносном ресурсе для кражи кликов и действий авторизованных пользователей.",
                    "Установить заголовок X-Frame-Options: DENY или frame-ancestors 'none' в Content-Security-Policy.");
            }

            if (!headers.Contains("Content-Security-Policy"))
            {
                yield return CreateFinding(domain, "Headers", "Low",
                    "Отсутствует Content-Security-Policy (CSP)",
                    "Нет ограничений на выполнение сторонних скриптов, что повышает риск успешной эксплуатации XSS (Cross-Site Scripting).",
                    "Сформировать и настроить строгую политику CSP (script-src, object-src, default-src).");
            }

            if (!headers.Contains("X-Content-Type-Options"))
            {
                yield return CreateFinding(domain, "Headers", "Low",
                    "Отсутствует X-Content-Type-Options: nosniff",
                    "Браузер может попытаться угадать MIME-тип ответа (MIME Sniffing), что может привести к исполнению скриптов под видом картинок или текста.",
                    "Установить заголовок: X-Content-Type-Options: nosniff");
            }
        }
    }

    private static RawSearchFinding CreateFinding(
        string target,
        string category,
        string severity,
        string issue,
        string description,
        string remediation)
    {
        var finding = new WebSecurityAuditFinding
        {
            Target = target,
            AuditCategory = category,
            Severity = severity,
            Issue = issue,
            Description = description,
            Remediation = remediation
        };

        return new RawSearchFinding("web_security_audit_record", JsonSerializer.Serialize(finding));
    }
}
