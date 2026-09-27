using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MeridianHost.Persistence.DI;

public static class Injection
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"));
#if DEBUG
            options.EnableSensitiveDataLogging();
#endif
        });

        services.Scan(scan => scan
            .FromAssemblies()
            .AddClasses(classes => classes.Where(type => type.Name.EndsWith("Repository")))
            .AsImplementedInterfaces()
            .WithScopedLifetime());

        return services;
    }
}