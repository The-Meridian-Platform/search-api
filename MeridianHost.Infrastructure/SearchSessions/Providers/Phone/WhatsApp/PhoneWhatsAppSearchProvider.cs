using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text.Json;
using MeridianHost.Application.SearchSessions.Execute.Providers;
using MeridianHost.Application.SearchSessions.Execute.Providers.Contracts;
using MeridianHost.Domain.SearchTargets;
using MeridianHost.Domain.SearchTargets.Enum;
using MeridianHost.Domain.SearchTargets.ValueObjects;
using MeridianHost.Infrastructure.SearchSessions.Providers.Phone.WhatsApp.Contracts;

namespace MeridianHost.Infrastructure.SearchSessions.Providers.Phone.WhatsApp;

public class PhoneWhatsAppSearchProvider(HttpClient client) : ISearchProvider
{
    private readonly HttpClient _client = client;

    public string Key { get; } = "phone-whatsapp";

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

        var waUrl = $"https://wa.me/{digits}";
        var apiProbeUrl = $"https://api.whatsapp.com/send?phone={digits}";

        bool hasWa = false;
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(3.0));

        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, apiProbeUrl);
            req.Headers.UserAgent.Add(new ProductInfoHeaderValue("Mozilla", "5.0"));

            using var resp = await _client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, cts.Token);
            if (resp.IsSuccessStatusCode)
            {
                var body = await resp.Content.ReadAsStringAsync(cts.Token);
                if (body.Contains("action=send", StringComparison.OrdinalIgnoreCase) ||
                    body.Contains("Continue to Chat", StringComparison.OrdinalIgnoreCase) ||
                    body.Contains("Chat on WhatsApp", StringComparison.OrdinalIgnoreCase) ||
                    body.Contains("api.whatsapp.com/send", StringComparison.OrdinalIgnoreCase))
                {
                    hasWa = true;
                }
            }
        }
        catch
        {
        }

        var finding = new PhoneWhatsAppFinding(
            PhoneNumber: normalized,
            HasWhatsApp: hasWa,
            DirectChatUrl: waUrl,
            StatusDescription: hasWa
                ? "Active WhatsApp account detected with direct chat capability."
                : "Direct WhatsApp link available (account existence unverified).");

        yield return new RawSearchFinding("phone-whatsapp_record", JsonSerializer.Serialize(finding));
    }
}
