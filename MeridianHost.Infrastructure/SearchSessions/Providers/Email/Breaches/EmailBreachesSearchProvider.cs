using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text.Json;
using MeridianHost.Application.SearchSessions.Execute.Providers;
using MeridianHost.Application.SearchSessions.Execute.Providers.Contracts;
using MeridianHost.Domain.SearchTargets;
using MeridianHost.Domain.SearchTargets.Enum;
using MeridianHost.Infrastructure.SearchSessions.Providers.Email.Breaches.Contracts;

namespace MeridianHost.Infrastructure.SearchSessions.Providers.Email.Breaches;

public class EmailBreachesSearchProvider(HttpClient client) : ISearchProvider
{
    private readonly HttpClient _client = client;

    public string Key { get; } = "email-breaches";

    public bool CanHandle(SearchTarget target)
        => target.TargetType == SearchTargetType.Email;

    public async IAsyncEnumerable<RawSearchFinding> ReadAllAsync(
        SearchTarget target,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var email = target.Value.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(email))
            yield break;

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(3.5));

        var breaches = new List<string>();

        try
        {
            var url = $"https://api.xposedornot.com/v1/check-email/{email}";
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.UserAgent.Add(new ProductInfoHeaderValue("Meridian-Platform", "1.0"));

            using var resp = await _client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, cts.Token);
            if (resp.IsSuccessStatusCode)
            {
                using var stream = await resp.Content.ReadAsStreamAsync(cts.Token);
                using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cts.Token);
                var root = doc.RootElement;

                if (root.TryGetProperty("breaches", out var breachesElem) && breachesElem.ValueKind == JsonValueKind.Array)
                {
                    foreach (var innerList in breachesElem.EnumerateArray())
                    {
                        if (innerList.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var item in innerList.EnumerateArray())
                            {
                                var name = item.GetString();
                                if (!string.IsNullOrWhiteSpace(name))
                                    breaches.Add(name);
                            }
                        }
                        else if (innerList.ValueKind == JsonValueKind.String)
                        {
                            var name = innerList.GetString();
                            if (!string.IsNullOrWhiteSpace(name))
                                breaches.Add(name);
                        }
                    }
                }
            }
        }
        catch
        {
        }

        bool hasBreaches = breaches.Count > 0;
        string severity = breaches.Count switch
        {
            0 => "Secure",
            < 3 => "Warning",
            _ => "Critical"
        };

        string summary = hasBreaches
            ? $"Compromised in {breaches.Count} public data breaches ({string.Join(", ", breaches.Take(4))}{(breaches.Count > 4 ? "..." : string.Empty)}). Credentials likely exposed."
            : "No known public breach records found for this email address.";

        var finding = new EmailBreachesFinding(
            Email: email,
            HasBreaches: hasBreaches,
            TotalBreaches: breaches.Count,
            Breaches: breaches,
            Severity: severity,
            Summary: summary);

        yield return new RawSearchFinding("email-breaches_record", JsonSerializer.Serialize(finding));
    }
}
