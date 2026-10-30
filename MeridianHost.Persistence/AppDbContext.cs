using MeridianHost.Persistence.SearchFindings.Configurations;
using MeridianHost.Persistence.SearchFindings.Entities;
using MeridianHost.Persistence.SearchSessions.Configurations;
using MeridianHost.Persistence.SearchSessions.Entities;
using MeridianHost.Persistence.SearchTargets.Configurations;
using MeridianHost.Persistence.SearchTargets.Entities;
using Microsoft.EntityFrameworkCore;

namespace MeridianHost.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<SearchSessionEntity> SearchSessions { get; set; }
    public DbSet<SearchTargetEntity> SearchTargets { get; set; }
    public DbSet<SearchFindingEntity> SearchFindings { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new SearchTargetConfiguration());
        modelBuilder.ApplyConfiguration(new SearchSessionConfiguration());
        modelBuilder.ApplyConfiguration(new SearchFindingConfiguration());
        
        base.OnModelCreating(modelBuilder);
    }
}
