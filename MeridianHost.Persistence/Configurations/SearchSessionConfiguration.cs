using MeridianHost.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MeridianHost.Persistence.Configurations;

public class SearchSessionConfiguration : IEntityTypeConfiguration<SearchSessionEntity>
{
    public void Configure(EntityTypeBuilder<SearchSessionEntity> builder)
    {
        builder.HasKey(x => x.Id);

        builder.HasOne(x => x.Target)
            .WithOne()
            .HasForeignKey<SearchSessionEntity>(x => x.Id)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(x => x.SearchStatus)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.StartedAt);
        
        builder.Property(x => x.FinishedAt);
    }
}