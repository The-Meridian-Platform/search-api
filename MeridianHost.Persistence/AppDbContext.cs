using MeridianHost.Domain.Models;
using MeridianHost.Persistence.Configurations;
using MeridianHost.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace MeridianHost.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<SearchSessionEntity> SearchSessions { get; set; }
    public DbSet<SearchTargetEntity> SearchTargets { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new SearchTargetConfiguration());
        modelBuilder.ApplyConfiguration(new SearchSessionConfiguration());
        
        base.OnModelCreating(modelBuilder);
    }
}