using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.DataAccess.Configurations;

public class TripDestinationConfiguration : IEntityTypeConfiguration<TripDestination>
{
    public void Configure(EntityTypeBuilder<TripDestination> builder)
    {
        builder.HasKey(d => d.Id).HasName("TripDestinationId");

        builder.Property(d => d.ProviderPlaceId)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(d => d.Name)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(d => d.Category).HasMaxLength(100).IsRequired(false);
        builder.Property(d => d.ThumbnailUrl).HasMaxLength(1000).IsRequired(false);
        builder.Property(d => d.Lat).IsRequired();
        builder.Property(d => d.Lng).IsRequired();
        builder.Property(d => d.Position).IsRequired();

        builder.HasOne(d => d.Trip)
            .WithMany(t => t.TripDestinations)
            .HasForeignKey(d => d.TripId)
            .HasConstraintName("FK_TripDestinations_Trips")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(d => d.ItineraryDay)
            .WithMany(day => day.TripDestinations)
            .HasForeignKey(d => d.ItineraryDayId)
            .HasConstraintName("FK_TripDestinations_ItineraryDays")
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        // Supports efficient board-ordering queries (NFR-1: search ≤500ms)
        builder.HasIndex(d => new { d.TripId, d.ItineraryDayId, d.Position })
            .HasDatabaseName("IX_TripDestinations_TripId_ItineraryDayId_Position");
    }
}
