using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using MeridianHost.Application.SearchSessions.Execute.Providers;
using MeridianHost.Application.SearchSessions.Execute.Providers.Contracts;
using MeridianHost.Domain.SearchTargets;
using MeridianHost.Domain.SearchTargets.Enum;
using MeridianHost.Infrastructure.SearchSessions.Providers.Username.Telegram.Contracts;

namespace MeridianHost.Infrastructure.SearchSessions.Providers.Username.Telegram;

public partial class UsernameTelegramSearchProvider(HttpClient client) : ISearchProvider
{
    private readonly HttpClient _client = client;

    public string Key { get; } = "username-telegram";

    public bool CanHandle(SearchTarget target)
        => target.TargetType == SearchTargetType.Username;

    public async IAsyncEnumerable<RawSearchFinding> ReadAllAsync(
        SearchTarget target,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var rawUsername = target.Value.Trim().TrimStart('@');
        if (string.IsNullOrWhiteSpace(rawUsername))
            yield break;

        var url = $"https://t.me/{rawUsername}";
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.UserAgent.Add(new ProductInfoHeaderValue("Mozilla", "5.0"));

        string html;
        try
        {
            using var response = await _client.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
                yield break;

            html = await response.Content.ReadAsStringAsync(cancellationToken);
        }
        catch
        {
            yield break;
        }

        var titleMatch = TitleRegex().Match(html);
        if (!titleMatch.Success)
            yield break;

        var title = System.Net.WebUtility.HtmlDecode(titleMatch.Groups[1].Value.Trim());
        var descMatch = DescRegex().Match(html);
        var description = descMatch.Success
            ? System.Net.WebUtility.HtmlDecode(descMatch.Groups[1].Value.Trim())
            : null;

        var imageMatch = ImageRegex().Match(html);
        var avatarUrl = imageMatch.Success ? imageMatch.Groups[1].Value.Trim() : null;

        if (title.StartsWith("Telegram: Contact @", StringComparison.OrdinalIgnoreCase) &&
            (description is null || description.StartsWith("You can contact @", StringComparison.OrdinalIgnoreCase)) &&
            avatarUrl is null)
        {
            yield break;
        }

        var finding = new UsernameTelegramFinding(
            Username: rawUsername,
            Title: title,
            Description: description,
            AvatarUrl: avatarUrl,
            Url: url);

        yield return new RawSearchFinding("username-telegram_record", JsonSerializer.Serialize(finding));
    }

    [GeneratedRegex(@"<meta\s+property=""og:title""\s+content=""([^""]+)""", RegexOptions.IgnoreCase)]
    private static partial Regex TitleRegex();

    [GeneratedRegex(@"<meta\s+property=""og:description""\s+content=""([^""]+)""", RegexOptions.IgnoreCase)]
    private static partial Regex DescRegex();

    [GeneratedRegex(@"<meta\s+property=""og:image""\s+content=""([^""]+)""", RegexOptions.IgnoreCase)]
    private static partial Regex ImageRegex();
}
