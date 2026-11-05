using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using MeridianHost.Application.SearchSessions.Execute.Providers;
using MeridianHost.Application.SearchSessions.Execute.Providers.Contracts;
using MeridianHost.Domain.SearchTargets;
using MeridianHost.Domain.SearchTargets.Enum;
using MeridianHost.Infrastructure.SearchSessions.Providers.Username.SshKeys.Contracts;

namespace MeridianHost.Infrastructure.SearchSessions.Providers.Username.SshKeys;

public partial class UsernameSshKeysSearchProvider(HttpClient client) : ISearchProvider
{
    private readonly HttpClient _client = client;

    public string Key { get; } = "username-ssh-keys";

    public bool CanHandle(SearchTarget target)
        => target.TargetType == SearchTargetType.Username;

    public async IAsyncEnumerable<RawSearchFinding> ReadAllAsync(
        SearchTarget target,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var username = target.Value.Trim().TrimStart('@');
        if (string.IsNullOrWhiteSpace(username))
            yield break;

        using var request = new HttpRequestMessage(HttpMethod.Get, $"https://github.com/{username}.keys");
        request.Headers.UserAgent.Add(new ProductInfoHeaderValue("Meridian-Platform", "1.0"));

        string text;
        try
        {
            using var response = await _client.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
                yield break;

            text = await response.Content.ReadAsStringAsync(cancellationToken);
        }
        catch
        {
            yield break;
        }

        if (string.IsNullOrWhiteSpace(text))
            yield break;

        var keyItems = new List<SshKeyItem>();
        var emails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var hostnames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        using var reader = new StringReader(text);
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            line = line.Trim();
            if (string.IsNullOrEmpty(line) || line.StartsWith('#'))
                continue;

            var parts = line.Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
                continue;

            var keyType = parts[0];
            var keyBase64 = parts[1];
            var comment = parts.Length > 2 ? parts[2].Trim() : null;

            string fingerprint = string.Empty;
            try
            {
                var keyBytes = Convert.FromBase64String(keyBase64);
                var hash = SHA256.HashData(keyBytes);
                fingerprint = "SHA256:" + Convert.ToBase64String(hash).TrimEnd('=');
            }
            catch
            {
                continue;
            }

            if (!string.IsNullOrEmpty(comment))
            {
                var emailMatch = EmailRegex().Match(comment);
                if (emailMatch.Success)
                {
                    emails.Add(emailMatch.Value);
                }
                else if (comment.Contains('@') || comment.Contains(".local", StringComparison.OrdinalIgnoreCase))
                {
                    hostnames.Add(comment);
                }
            }

            keyItems.Add(new SshKeyItem(keyType, fingerprint, comment));
        }

        if (keyItems.Count == 0)
            yield break;

        var finding = new UsernameSshKeysFinding(
            Username: username,
            Keys: keyItems,
            DiscoveredEmails: emails.ToList(),
            DiscoveredHostnames: hostnames.ToList());

        yield return new RawSearchFinding("username-ssh-keys_record", JsonSerializer.Serialize(finding));
    }

    [GeneratedRegex(@"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}", RegexOptions.IgnoreCase)]
    private static partial Regex EmailRegex();
}
