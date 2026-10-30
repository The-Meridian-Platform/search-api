using MeridianHost.Persistence.SearchFindings.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MeridianHost.Persistence.SearchFindings.Configurations;

public class SearchFindingConfiguration : IEntityTypeConfiguration<SearchFindingEntity>
{
    public void Configure(EntityTypeBuilder<SearchFindingEntity> builder)
    {
        builder.HasKey(x => x.Id);
        
        builder.HasOne(x => x.SearchSession)
            .WithMany(s => s.Findings)
            .HasForeignKey(x => x.SearchSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(x => x.ProviderKey)
            .HasMaxLength(128)
            .IsRequired();
        
        builder.Property(x => x.Type)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(x => x.DataJson)
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();
    }
}
