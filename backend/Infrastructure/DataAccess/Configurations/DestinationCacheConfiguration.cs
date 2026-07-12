using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.DataAccess.Configurations;

public class DestinationCacheConfiguration : IEntityTypeConfiguration<DestinationCache>
{
    public void Configure(EntityTypeBuilder<DestinationCache> builder)
    {
        builder.HasKey(c => c.ProviderPlaceId).HasName("DestinationCacheProviderPlaceId");

        builder.Property(c => c.ProviderPlaceId)
            .IsRequired()
            .HasMaxLength(256);

        // Store as jsonb on PostgreSQL for efficient JSON querying
        builder.Property(c => c.PayloadJson)
            .IsRequired()
            .HasColumnType("jsonb");

        builder.Property(c => c.FetchedAt).IsRequired();
    }
}
