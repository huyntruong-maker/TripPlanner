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
    }
}