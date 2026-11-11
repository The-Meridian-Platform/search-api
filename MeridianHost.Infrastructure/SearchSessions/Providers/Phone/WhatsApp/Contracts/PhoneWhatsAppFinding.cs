namespace MeridianHost.Infrastructure.SearchSessions.Providers.Phone.WhatsApp.Contracts;

public sealed record PhoneWhatsAppFinding(
    string PhoneNumber,
    bool HasWhatsApp,
    string DirectChatUrl,
    string StatusDescription);
