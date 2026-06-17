using Domain.Constants;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.DataAccess.Configurations;

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.HasKey(role => role.Id).HasName("RoleId");
        builder.Property(role => role.ConcurrencyStamp).IsConcurrencyToken();
        builder.Property(role => role.Name).HasMaxLength(256);
        builder.Property(role => role.NormalizedName).HasMaxLength(256);

        builder.HasData(
            CreateRole(UserConstants.Role.SuperAdmin, RolePolicyConstants.SuperAdmin.Name, RolePolicyConstants.SuperAdmin.DisplayName, UserConstants.RoleLevel.SuperAdmin),
            CreateRole(UserConstants.Role.SystemAdmin, RolePolicyConstants.SystemAdmin.Name, RolePolicyConstants.SystemAdmin.DisplayName, UserConstants.RoleLevel.SystemAdmin)
        );

        builder.HasQueryFilter(i => !i.IsDeleted);
    }

    private static Role CreateRole(Guid id, string code, string displayName, int level) => new()
    {
        Id = id,
        Name = code,
        NormalizedName = code,
        DisplayName = displayName,
        ConcurrencyStamp = "6AAFFB84-E49A-468D-9153-2DA282AC0CDA",
        CreatedAt = new DateTime(2025, 3, 12, 0, 0, 0, 0, DateTimeKind.Utc).AddTicks(8363),
        UpdatedAt = new DateTime(2025, 3, 12, 0, 0, 0, 0, DateTimeKind.Utc).AddTicks(8363),
        CreatedBy = UserConstants.AdminId,
        UpdatedBy = UserConstants.AdminId,
        Level = level
    };
}