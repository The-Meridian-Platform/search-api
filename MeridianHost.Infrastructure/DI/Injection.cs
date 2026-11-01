using MeridianHost.Application.SearchSessions.Execute.Broadcasting.Repositories;
using MeridianHost.Application.SearchSessions.Execute.Providers;
using MeridianHost.Application.SearchSessions.Execute.Queue;
using MeridianHost.Infrastructure.SearchSessions.Broadcasting;
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

        services.AddHttpClient();

        // Auto registration of services in DI
        var providerTypes = typeof(Injection).Assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } 
                        && typeof(ISearchProvider).IsAssignableFrom(t));

        foreach (var type in providerTypes)
        {
            services.AddTransient(type, sp =>
            {
                var factory = sp.GetRequiredService<IHttpClientFactory>();
                var httpClient = factory.CreateClient(type.Name);
                return ActivatorUtilities.CreateInstance(sp, type, httpClient);
            });

            services.AddTransient(typeof(ISearchProvider), sp => sp.GetRequiredService(type));
        }

        services.AddHostedService<SearchExecutionWorker>();

        services.AddGrpc();
        
        return services;
    }
}
