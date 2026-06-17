using Domain.Constants;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.DataAccess.Configurations;

public class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.HasKey(userRole => new { userRole.UserId, userRole.RoleId });

        builder
            .HasOne(userRole => userRole.Role)
            .WithMany(role => role.UserRoles)
            .HasForeignKey(userRole => userRole.RoleId);

        builder
            .HasOne(userRole => userRole.User)
            .WithMany(user => user.UserRoles)
            .HasForeignKey(userRole => userRole.UserId);

        builder.HasQueryFilter(i => !i.IsDeleted);

        builder.HasData(new UserRole
        {
            UserId = UserConstants.AdminId,
            RoleId = UserConstants.Role.SuperAdmin,
            CreatedAt = new DateTime(2025, 3, 12, 0, 0, 0, DateTimeKind.Utc).AddTicks(8363),
            UpdatedAt = new DateTime(2025, 3, 12, 0, 0, 0, DateTimeKind.Utc).AddTicks(8363),
            CreatedBy = UserConstants.AdminId,
            UpdatedBy = UserConstants.AdminId
        });
    }
}