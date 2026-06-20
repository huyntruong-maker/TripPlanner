using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.DataAccess.Configurations;

public class ItineraryDayConfiguration : IEntityTypeConfiguration<ItineraryDay>
{
    public void Configure(EntityTypeBuilder<ItineraryDay> builder)
    {
        builder.HasKey(d => d.Id).HasName("ItineraryDayId");

        builder.Property(d => d.Date).IsRequired();
        builder.Property(d => d.DayIndex).IsRequired();

        builder.HasOne(d => d.Trip)
            .WithMany(t => t.ItineraryDays)
            .HasForeignKey(d => d.TripId)
            .HasConstraintName("FK_ItineraryDays_Trips")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(d => !d.IsDeleted);

        // Enforce a single ItineraryDay per (trip, day-index) pair
        builder.HasIndex(d => new { d.TripId, d.DayIndex })
            .HasDatabaseName("IX_ItineraryDays_TripId_DayIndex")
            .IsUnique();
    }
}
