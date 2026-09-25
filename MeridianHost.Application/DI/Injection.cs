using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace MeridianHost.Application.DI;

public static class Injection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(Injection).Assembly);

        services.Scan(scan => scan
            .FromAssemblies()
            .AddClasses(classes => classes.Where(end => end.Name.EndsWith("Handler", StringComparison.Ordinal)))
            .AsImplementedInterfaces()
            .WithScopedLifetime());
        
        return services;
    }
}