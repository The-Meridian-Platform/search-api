using System.Net.Security;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using MeridianHost.Application.SearchSessions.Execute.Providers;
using MeridianHost.Application.SearchSessions.Execute.Providers.Contracts;
using MeridianHost.Domain.SearchTargets;
using MeridianHost.Domain.SearchTargets.Enum;
using MeridianHost.Infrastructure.SearchSessions.Providers.Domain.Tls.Contracts;

namespace MeridianHost.Infrastructure.SearchSessions.Providers.Domain.Tls;

public class DomainTlsCertSearchProvider(HttpClient client) : ISearchProvider
{
    private readonly HttpClient _client = client;

    public string Key { get; } = "domain-tls";

    public bool CanHandle(SearchTarget target)
        => target.TargetType == SearchTargetType.Domain;

    public async IAsyncEnumerable<RawSearchFinding> ReadAllAsync(
        SearchTarget target,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var domain = target.Value.Trim().ToLowerInvariant().TrimEnd('.');
        if (string.IsNullOrWhiteSpace(domain))
            yield break;

        DomainTlsCertFinding? finding = null;

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(3.5));

            using var tcpClient = new TcpClient();
            await tcpClient.ConnectAsync(domain, 443, cts.Token);

            await using var sslStream = new SslStream(
                tcpClient.GetStream(),
                false,
                (_, _, _, _) => true);

            var sslOptions = new SslClientAuthenticationOptions
            {
                TargetHost = domain
            };

            await sslStream.AuthenticateAsClientAsync(sslOptions, cts.Token);

            if (sslStream.RemoteCertificate is null)
                yield break;

            using var cert = new X509Certificate2(sslStream.RemoteCertificate);

            var sans = new List<string>();
            foreach (var ext in cert.Extensions)
            {
                if (ext is X509SubjectAlternativeNameExtension sanExt)
                {
                    foreach (var dnsName in sanExt.EnumerateDnsNames())
                    {
                        if (!string.IsNullOrWhiteSpace(dnsName))
                            sans.Add(dnsName);
                    }
                }
            }

            var now = DateTime.UtcNow;
            var validTo = cert.NotAfter.ToUniversalTime();
            var validFrom = cert.NotBefore.ToUniversalTime();
            var daysUntil = (int)Math.Ceiling((validTo - now).TotalDays);

            finding = new DomainTlsCertFinding(
                Domain: domain,
                Subject: cert.Subject,
                Issuer: cert.Issuer,
                ValidFrom: validFrom,
                ValidTo: validTo,
                DaysUntilExpiration: daysUntil,
                IsExpired: now > validTo,
                Thumbprint: cert.Thumbprint,
                SignatureAlgorithm: cert.SignatureAlgorithm.FriendlyName ?? cert.SignatureAlgorithm.Value ?? "Unknown",
                SubjectAlternativeNames: sans.Distinct(StringComparer.OrdinalIgnoreCase).ToList());
        }
        catch
        {
            yield break;
        }

        if (finding is not null)
        {
            yield return new RawSearchFinding("domain-tls_record", JsonSerializer.Serialize(finding));
        }
    }
}
