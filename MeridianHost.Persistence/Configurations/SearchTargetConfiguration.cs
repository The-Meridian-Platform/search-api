using MeridianHost.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MeridianHost.Persistence.Configurations;

public class SearchTargetConfiguration : IEntityTypeConfiguration<SearchTargetEntity>
{
    public void Configure(EntityTypeBuilder<SearchTargetEntity> builder)
    {
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.Value)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.TargetType)
            .HasConversion<string>();
    }
}