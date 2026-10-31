using MeridianHost.Application.SearchSessions.Execute.Providers;
using MeridianHost.Application.SearchSessions.Execute.Queue;
using MeridianHost.Infrastructure.SearchSessions.Providers.Domain.Realisations;
using MeridianHost.Infrastructure.SearchSessions.Queue;
using MeridianHost.Infrastructure.SearchSessions.Workers;
using Microsoft.Extensions.DependencyInjection;

namespace MeridianHost.Infrastructure.DI;

public static class Injection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<ISearchWorkQueue, SearchWorkQueue>();

        services.AddHttpClient<DomainSearchProvider>();
        services.AddHttpClient<WhoisSearchProvider>();
        
        services.AddTransient<ISearchProvider>(sp => sp.GetRequiredService<DomainSearchProvider>());
        services.AddTransient<ISearchProvider>(sp => sp.GetRequiredService<WhoisSearchProvider>());

        services.AddHostedService<SearchExecutionWorker>();
        
        return services;
    }
}