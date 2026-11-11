using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text.Json;
using MeridianHost.Application.SearchSessions.Execute.Providers;
using MeridianHost.Application.SearchSessions.Execute.Providers.Contracts;
using MeridianHost.Domain.SearchTargets;
using MeridianHost.Domain.SearchTargets.Enum;
using MeridianHost.Infrastructure.SearchSessions.Providers.Username.Keybase.Contracts;

namespace MeridianHost.Infrastructure.SearchSessions.Providers.Username.Keybase;

public class UsernameKeybaseSearchProvider(HttpClient client) : ISearchProvider
{
    private readonly HttpClient _client = client;

    public string Key { get; } = "username-keybase";

    public bool CanHandle(SearchTarget target)
        => target.TargetType == SearchTargetType.Username;

    public async IAsyncEnumerable<RawSearchFinding> ReadAllAsync(
        SearchTarget target,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var username = target.Value.Trim().TrimStart('@');
        if (string.IsNullOrWhiteSpace(username))
            yield break;

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(3.5));

        UsernameKeybaseFinding? finding = null;

        try
        {
            var url = $"https://keybase.io/_/api/1.0/user/lookup.json?usernames={username}";
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.UserAgent.Add(new ProductInfoHeaderValue("Meridian-Platform", "1.0"));

            using var resp = await _client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, cts.Token);
            if (!resp.IsSuccessStatusCode)
                yield break;

            using var stream = await resp.Content.ReadAsStreamAsync(cts.Token);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cts.Token);
            var root = doc.RootElement;

            if (root.TryGetProperty("them", out var themArray) && themArray.ValueKind == JsonValueKind.Array && themArray.GetArrayLength() > 0)
            {
                var userObj = themArray[0];
                if (userObj.ValueKind == JsonValueKind.Object)
                {
                    string? fullName = null;
                    string? bio = null;
                    string? location = null;

                    if (userObj.TryGetProperty("profile", out var prof) && prof.ValueKind == JsonValueKind.Object)
                    {
                        fullName = prof.TryGetProperty("full_name", out var fn) ? fn.GetString() : null;
                        bio = prof.TryGetProperty("bio", out var b) ? b.GetString() : null;
                        location = prof.TryGetProperty("location", out var loc) ? loc.GetString() : null;
                    }

                    var proofs = new List<KeybaseProofItem>();
                    if (userObj.TryGetProperty("proofs_summary", out var proofsSum) &&
                        proofsSum.TryGetProperty("all", out var allProofs) &&
                        allProofs.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var p in allProofs.EnumerateArray())
                        {
                            var pType = p.TryGetProperty("proof_type", out var pt) ? pt.GetString() ?? "service" : "service";
                            var pName = p.TryGetProperty("nametag", out var pn) ? pn.GetString() ?? string.Empty : string.Empty;
                            var pUrl = p.TryGetProperty("service_url", out var pu) ? pu.GetString() ?? string.Empty : string.Empty;

                            if (!string.IsNullOrWhiteSpace(pName))
                            {
                                proofs.Add(new KeybaseProofItem(pType, pName, pUrl));
                            }
                        }
                    }

                    var crypto = new List<string>();
                    if (userObj.TryGetProperty("cryptocurrency_addresses", out var cryptoObj) && cryptoObj.ValueKind == JsonValueKind.Object)
                    {
                        foreach (var prop in cryptoObj.EnumerateObject())
                        {
                            if (prop.Value.ValueKind == JsonValueKind.Array)
                            {
                                foreach (var addr in prop.Value.EnumerateArray())
                                {
                                    var str = addr.TryGetProperty("address", out var a) ? a.GetString() : null;
                                    if (!string.IsNullOrWhiteSpace(str))
                                    {
                                        crypto.Add($"{prop.Name.ToUpperInvariant()}: {str}");
                                    }
                                }
                            }
                        }
                    }

                    finding = new UsernameKeybaseFinding(
                        Username: username,
                        Exists: true,
                        FullName: fullName,
                        Bio: bio,
                        Location: location,
                        ConnectedAccounts: proofs,
                        CryptoWallets: crypto);
                }
            }
        }
        catch
        {
        }

        if (finding is not null)
        {
            yield return new RawSearchFinding("username-keybase_record", JsonSerializer.Serialize(finding));
        }
    }
}
