namespace MeridianHost.Infrastructure.SearchSessions.Providers.Ip.Geo.Contracts;

public sealed record IpGeoFinding(
    string Ip,
    string Country,
    string CountryCode,
    string Region,
    string City,
    double Latitude,
    double Longitude,
    string Timezone,
    string Isp,
    string Org,
    string Asn,
    bool IsMobile,
    bool IsProxy,
    bool IsHosting);
