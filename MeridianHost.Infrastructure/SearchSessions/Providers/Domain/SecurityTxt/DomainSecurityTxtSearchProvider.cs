using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text.Json;
using MeridianHost.Application.SearchSessions.Execute.Providers;
using MeridianHost.Application.SearchSessions.Execute.Providers.Contracts;
using MeridianHost.Domain.SearchTargets;
using MeridianHost.Domain.SearchTargets.Enum;
using MeridianHost.Infrastructure.SearchSessions.Providers.Domain.SecurityTxt.Contracts;

namespace MeridianHost.Infrastructure.SearchSessions.Providers.Domain.SecurityTxt;

public class DomainSecurityTxtSearchProvider(HttpClient client) : ISearchProvider
{
    private readonly HttpClient _client = client;

    public string Key { get; } = "domain-security-txt";

    public bool CanHandle(SearchTarget target)
        => target.TargetType == SearchTargetType.Domain;

    public async IAsyncEnumerable<RawSearchFinding> ReadAllAsync(
        SearchTarget target,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var domain = target.Value.Trim().ToLowerInvariant().TrimEnd('.');
        var paths = new[]
        {
            $"https://{domain}/.well-known/security.txt",
            $"https://{domain}/security.txt"
        };

        foreach (var path in paths)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, path);
            request.Headers.UserAgent.Add(new ProductInfoHeaderValue("Meridian-Platform", "1.0"));

            string text;
            try
            {
                using var response = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                if (!response.IsSuccessStatusCode)
                    continue;

                var contentType = response.Content.Headers.ContentType?.MediaType ?? string.Empty;
                if (contentType.Contains("html", StringComparison.OrdinalIgnoreCase))
                    continue;

                text = await response.Content.ReadAsStringAsync(cancellationToken);
            }
            catch
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(text) || !text.Contains("Contact:", StringComparison.OrdinalIgnoreCase))
                continue;

            var contacts = new List<string>();
            var securityEmails = new List<string>();
            string? policyUrl = null;
            string? ackUrl = null;
            string? hiringUrl = null;
            string? expires = null;

            using var reader = new StringReader(text);
            string? line;
            while ((line = reader.ReadLine()) is not null)
            {
                line = line.Trim();
                if (line.StartsWith('#') || string.IsNullOrEmpty(line))
                    continue;

                var separatorIndex = line.IndexOf(':');
                if (separatorIndex <= 0)
                    continue;

                var directive = line[..separatorIndex].Trim();
                var value = line[(separatorIndex + 1)..].Trim();

                if (string.Equals(directive, "Contact", StringComparison.OrdinalIgnoreCase))
                {
                    contacts.Add(value);
                    if (value.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase))
                    {
                        var email = value["mailto:".Length..].Trim();
                        if (!string.IsNullOrEmpty(email))
                            securityEmails.Add(email);
                    }
                }
                else if (string.Equals(directive, "Policy", StringComparison.OrdinalIgnoreCase))
                {
                    policyUrl = value;
                }
                else if (string.Equals(directive, "Acknowledgments", StringComparison.OrdinalIgnoreCase))
                {
                    ackUrl = value;
                }
                else if (string.Equals(directive, "Hiring", StringComparison.OrdinalIgnoreCase))
                {
                    hiringUrl = value;
                }
                else if (string.Equals(directive, "Expires", StringComparison.OrdinalIgnoreCase))
                {
                    expires = value;
                }
            }

            if (contacts.Count == 0 && securityEmails.Count == 0)
                continue;

            var finding = new DomainSecurityTxtFinding(
                Domain: domain,
                Url: path,
                Contacts: contacts,
                SecurityEmails: securityEmails,
                PolicyUrl: policyUrl,
                AcknowledgmentsUrl: ackUrl,
                HiringUrl: hiringUrl,
                Expires: expires);

            yield return new RawSearchFinding("domain-security-txt_record", JsonSerializer.Serialize(finding));
            yield break;
        }
    }
}
