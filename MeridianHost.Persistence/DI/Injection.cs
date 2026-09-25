using Microsoft.Extensions.DependencyInjection;

namespace MeridianHost.Persistence.DI;

public static class Injection
{
    public static IServiceCollection AddPersistence(this IServiceCollection services)
    {
        return services;
    }
}