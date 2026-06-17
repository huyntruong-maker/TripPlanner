using Domain.Constants;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Identity;

public static class Authorization
{
    public static void AddClaimsBasedAuthorization(this IServiceCollection collection)
    {
        collection.AddAuthorization(options =>
        {
            foreach (var permission in PermissionConstants.All)
                options.AddPolicy(permission, policy =>
                    policy.RequireClaim(RolePolicyConstants.ClaimType, permission));
        });
    }
}