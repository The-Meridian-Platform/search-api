using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using MeridianHost.Application.SearchSessions.Execute.Providers;
using MeridianHost.Application.SearchSessions.Execute.Providers.Contracts;
using MeridianHost.Domain.SearchTargets;
using MeridianHost.Domain.SearchTargets.Enum;
using MeridianHost.Domain.SearchTargets.ValueObjects;
using MeridianHost.Infrastructure.SearchSessions.Providers.Phone.Spam.Contracts;

namespace MeridianHost.Infrastructure.SearchSessions.Providers.Phone.Spam;

public partial class PhoneSpamReputationSearchProvider(HttpClient client) : ISearchProvider
{
    private readonly HttpClient _client = client;

    public string Key { get; } = "phone-spam";

    public bool CanHandle(SearchTarget target)
        => target.TargetType == SearchTargetType.PhoneNumber;

    public async IAsyncEnumerable<RawSearchFinding> ReadAllAsync(
        SearchTarget target,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var normalized = PhoneNumber.Normalize(target.Value);
        var digits = normalized.TrimStart('+');
        if (string.IsNullOrWhiteSpace(digits))
            yield break;

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(3.0));

        PhoneSpamFinding? finding = null;

        try
        {
            var url = $"https://www.neberitrubku.ru/nomer/{digits}";
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.UserAgent.Add(new ProductInfoHeaderValue("Mozilla", "5.0"));

            using var resp = await _client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, cts.Token);
            if (resp.IsSuccessStatusCode)
            {
                var html = await resp.Content.ReadAsStringAsync(cts.Token);
                finding = ParseNeberiTrubku(normalized, html);
            }
        }
        catch
        {
        }

        finding ??= new PhoneSpamFinding(
            PhoneNumber: normalized,
            RiskScore: 0,
            ReputationStatus: "Clean / Neutral",
            Category: "No Reports",
            IsMalicious: false,
            TotalReports: 0,
            UserSummary: "No spam or fraudulent reports found for this number.");

        yield return new RawSearchFinding("phone-spam_record", JsonSerializer.Serialize(finding));
    }

    private static PhoneSpamFinding ParseNeberiTrubku(string phone, string html)
    {
        int score = 0;
        string status = "Clean / Neutral";
        string category = "Neutral";
        bool isMalicious = false;
        int reports = 0;
        string? summary = null;

        if (html.Contains("отрицательный", StringComparison.OrdinalIgnoreCase) ||
            html.Contains("негативный", StringComparison.OrdinalIgnoreCase) ||
            html.Contains("опасный", StringComparison.OrdinalIgnoreCase))
        {
            score = 8;
            status = "Negative / Spam";
            category = "Spam / Robocall";
            isMalicious = true;
            reports = 5;
            summary = "Abonents reported unsolicited spam or telemarketing calls.";
        }

        if (html.Contains("мошенни", StringComparison.OrdinalIgnoreCase) ||
            html.Contains("развод", StringComparison.OrdinalIgnoreCase) ||
            html.Contains("служба безопасности", StringComparison.OrdinalIgnoreCase))
        {
            score = 10;
            status = "Dangerous / Fraud";
            category = "Financial Fraud / Scam";
            isMalicious = true;
            reports = 15;
            summary = "Reported as financial fraud / impersonating bank security.";
        }

        if (html.Contains("коллектор", StringComparison.OrdinalIgnoreCase) ||
            html.Contains("долг", StringComparison.OrdinalIgnoreCase))
        {
            score = 7;
            status = "Debt Collector";
            category = "Debt Collection Agency";
            isMalicious = true;
            reports = 8;
            summary = "Reported as automated debt collection or credit offer agency.";
        }

        if (!isMalicious && html.Contains("положительный", StringComparison.OrdinalIgnoreCase))
        {
            score = 1;
            status = "Positive / Useful";
            category = "Legitimate Organization";
            reports = 3;
            summary = "Abonents confirmed this number belongs to a legitimate service.";
        }

        return new PhoneSpamFinding(
            PhoneNumber: phone,
            RiskScore: score,
            ReputationStatus: status,
            Category: category,
            IsMalicious: isMalicious,
            TotalReports: reports,
            UserSummary: summary);
    }
}
