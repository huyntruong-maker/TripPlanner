using Domain.Constants;
using Domain.Entities;
using Infrastructure.DataAccess.DbContexts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Identity;

public static class DbIdentity
{
    public static void AddDbIdentity(this IServiceCollection collection, IConfiguration configuration)
    {
        collection.AddDefaultIdentity<User>(options =>
            {
                options.Lockout.MaxFailedAccessAttempts = configuration
                    .GetSection(ConfigKeys.Security.Lockout.MaxFailedAccessAttempts).Get<int>();
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(configuration
                    .GetSection(ConfigKeys.Security.Lockout.DefaultLockoutMinutes).Get<int>());
            })
            .AddEntityFrameworkStores<WriteDbContext>();
    }
}