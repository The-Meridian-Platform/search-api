using MeridianHost.Application.SearchSessions.Execute.Broadcasting.Repositories;
using MeridianHost.Application.SearchSessions.Execute.Providers;
using MeridianHost.Application.SearchSessions.Execute.Queue;
using MeridianHost.Infrastructure.SearchSessions.Broadcasting;
using MeridianHost.Infrastructure.SearchSessions.Providers.Domain.Dns;
using MeridianHost.Infrastructure.SearchSessions.Providers.Domain.Http;
using MeridianHost.Infrastructure.SearchSessions.Providers.Domain.Subdomains;
using MeridianHost.Infrastructure.SearchSessions.Providers.Domain.Whois;
using MeridianHost.Infrastructure.SearchSessions.Queue;
using MeridianHost.Infrastructure.SearchSessions.Workers;
using Microsoft.Extensions.DependencyInjection;

namespace MeridianHost.Infrastructure.DI;

public static class Injection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<ISearchWorkQueue, SearchWorkQueue>();
        services.AddSingleton<ISearchSessionBroadcaster, SearchSessionBroadcaster>();

        services.AddHttpClient<DnsSearchProvider>();
        services.AddHttpClient<WhoisSearchProvider>();
        services.AddHttpClient<SubdomainSearchProvider>();
        services.AddHttpClient<HttpTechSearchProvider>();
        
        services.AddTransient<ISearchProvider>(sp => sp.GetRequiredService<DnsSearchProvider>());
        services.AddTransient<ISearchProvider>(sp => sp.GetRequiredService<WhoisSearchProvider>());
        services.AddTransient<ISearchProvider>(sp => sp.GetRequiredService<SubdomainSearchProvider>());
        services.AddTransient<ISearchProvider>(sp => sp.GetRequiredService<HttpTechSearchProvider>());

        services.AddHostedService<SearchExecutionWorker>();

        services.AddGrpc();
        
        return services;
    }
}
