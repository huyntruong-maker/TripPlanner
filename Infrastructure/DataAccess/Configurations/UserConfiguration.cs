using Domain.Constants;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.DataAccess.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(role => role.Id).HasName("UserId");
        builder.HasIndex(user => user.NormalizedUserName).HasDatabaseName("UserNameIndex").IsUnique();
        builder.HasIndex(user => user.NormalizedEmail).HasDatabaseName("EmailIndex");
        builder.HasIndex(user => user.PhoneNumber).HasDatabaseName("PhoneNumberIndex");
        builder.Property(user => user.ConcurrencyStamp).IsConcurrencyToken().HasMaxLength(100);
        builder.Property(user => user.SecurityStamp).HasMaxLength(100);
        builder.Property(user => user.PhoneNumber).HasMaxLength(20);
        builder.Property(user => user.UserName).HasMaxLength(256);
        builder.Property(user => user.NormalizedUserName).HasMaxLength(256);
        builder.Property(user => user.Email).HasMaxLength(256);
        builder.Property(user => user.NormalizedEmail).HasMaxLength(256);

        builder.HasQueryFilter(i => !i.IsDeleted);

        builder.HasData(
            new User
            {
                Id = UserConstants.AdminId,
                UserName = "admin",
                PasswordHash =
                    "AQAAAAEAACcQAAAAEELKNErj+EBVy3yZwAI32HSAQILEj5UAOooOEHTMPYU/yp0E28xNH1BjU/SEBw8kuA==", // Admin!1234
                LockoutEnabled = true,
                ConcurrencyStamp = "616f1653-48e9-4a6f-81b3-1bdd52e565b5",
                NormalizedUserName = "ADMIN",
                SecurityStamp = "ZY5BGSWBARTE74T6ZLO7WKKMMILBEB2E",
                CreatedAt = new DateTime(2023, 10, 09, 0, 0, 0, 0, DateTimeKind.Utc).AddTicks(8363),
                UpdatedAt = new DateTime(2023, 10, 09, 0, 0, 0, 0, DateTimeKind.Utc).AddTicks(8363),
                CreatedBy = Guid.Empty,
                UpdatedBy = Guid.Empty,
                FirstName = "Admin",
            });
    }
}