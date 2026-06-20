using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.DataAccess.Configurations;

public class TripConfiguration : IEntityTypeConfiguration<Trip>
{
    public void Configure(EntityTypeBuilder<Trip> builder)
    {
        builder.HasKey(t => t.Id).HasName("TripId");

        builder.Property(t => t.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(t => t.StartDate).IsRequired(false);
        builder.Property(t => t.EndDate).IsRequired(false);

        builder.HasOne(t => t.User)
            .WithMany(u => u.Trips)
            .HasForeignKey(t => t.UserId)
            .HasConstraintName("FK_Trips_Users")
            .OnDelete(DeleteBehavior.Cascade);

        // Optional: prevent a user from having two trips with the same name
        builder.HasIndex(t => new { t.UserId, t.Name })
            .HasDatabaseName("IX_Trips_UserId_Name")
            .IsUnique();
    }
}
