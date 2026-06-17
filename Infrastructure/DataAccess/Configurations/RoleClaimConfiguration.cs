using Domain.Constants;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.DataAccess.Configurations;

public class RoleClaimConfiguration : IEntityTypeConfiguration<RoleClaim>
{
    public void Configure(EntityTypeBuilder<RoleClaim> builder)
    {
        builder.HasKey(userClaim => userClaim.Id).HasName("UserClaimId");

        builder
            .HasOne(roleClaim => roleClaim.Role)
            .WithMany(role => role.RoleClaims)
            .HasForeignKey(roleClaim => roleClaim.RoleId);

        builder.HasQueryFilter(i => !i.IsDeleted);

        var id = 1;
        foreach (var claimValue in RolePolicyConstants.SuperAdmin.AllowedPermissions)
        {
            builder.HasData(CreateRoleClaim(id++, RolePolicyConstants.SuperAdmin.Id, claimValue));
        }

        foreach (var claimValue in RolePolicyConstants.SystemAdmin.AllowedPermissions)
        {
            builder.HasData(CreateRoleClaim(id++, RolePolicyConstants.SystemAdmin.Id, claimValue));
        }
    }

    private static RoleClaim CreateRoleClaim(int id, Guid roleId, string claimValue) => new()
    {
        Id = id,
        RoleId = roleId,
        ClaimType = RolePolicyConstants.ClaimType,
        ClaimValue = claimValue,
        CreatedAt = new DateTime(2025, 3, 12, 0, 0, 0, 0, DateTimeKind.Utc).AddTicks(8363),
        UpdatedAt = new DateTime(2025, 3, 12, 0, 0, 0, 0, DateTimeKind.Utc).AddTicks(8363),
        CreatedBy = UserConstants.AdminId,
        UpdatedBy = UserConstants.AdminId,
    };
}