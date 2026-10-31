using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using MeridianHost.Application.SearchSessions.Execute.Providers;
using MeridianHost.Application.SearchSessions.Execute.Providers.Contracts;
using MeridianHost.Domain.SearchTargets;
using MeridianHost.Domain.SearchTargets.Enum;
using MeridianHost.Infrastructure.SearchSessions.Providers.Domain.Http.Contracts;

namespace MeridianHost.Infrastructure.SearchSessions.Providers.Domain.Http;

public partial class HttpTechSearchProvider(HttpClient client) : ISearchProvider
{
    private readonly HttpClient _client = client;

    public string Key { get; } = "domain-http";

    public bool CanHandle(SearchTarget target)
        => target.TargetType == SearchTargetType.Domain;

    public async IAsyncEnumerable<RawSearchFinding> ReadAllAsync(
        SearchTarget target,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var schemes = new[] { "https", "http" };

        foreach (var scheme in schemes)
        {
            var url = $"{scheme}://{target.Value}";
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(5));

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));

            HttpResponseMessage? response = null;

            try
            {
                response = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cts.Token);
            }
            catch
            {
                continue;
            }

            using (response)
            {
                var statusCode = (int)response.StatusCode;
                var server = response.Headers.Server.ToString();
                if (string.IsNullOrWhiteSpace(server) && response.Headers.TryGetValues("Server", out var sValues))
                    server = string.Join(", ", sValues);

                string? poweredBy = null;
                if (response.Headers.TryGetValues("X-Powered-By", out var pValues))
                    poweredBy = string.Join(", ", pValues);

                var hasHsts = response.Headers.Contains("Strict-Transport-Security");
                var hasCsp = response.Headers.Contains("Content-Security-Policy");
                var hasXFrame = response.Headers.Contains("X-Frame-Options");
                var contentType = response.Content.Headers.ContentType?.ToString();

                string? title = null;
                try
                {
                    var stream = await response.Content.ReadAsStreamAsync(cts.Token);
                    var buffer = new byte[16384];
                    var bytesRead = await stream.ReadAsync(buffer, cts.Token);
                    var html = System.Text.Encoding.UTF8.GetString(buffer, 0, bytesRead);

                    var match = TitleRegex().Match(html);
                    if (match.Success)
                    {
                        title = match.Groups[1].Value.Trim();
                    }
                }
                catch
                {
                }

                var finding = new HttpTechFinding(
                    Url: url,
                    StatusCode: statusCode,
                    Server: string.IsNullOrWhiteSpace(server) ? null : server,
                    PoweredBy: poweredBy,
                    Title: title,
                    HasHsts: hasHsts,
                    HasCsp: hasCsp,
                    HasXFrameOptions: hasXFrame,
                    ContentType: contentType);

                yield return new RawSearchFinding("http_tech_record", JsonSerializer.Serialize(finding));
                yield break;
            }
        }
    }

    [GeneratedRegex(@"<title[^>]*>(.*?)</title>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex TitleRegex();
}
