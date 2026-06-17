using Domain.Constants;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.BackgroundHandler.DbContext;

public static class DbInjection
{
    public static void AddJobStore(this IServiceCollection collection, IConfiguration configuration)
    {
        var connectionString = configuration.GetSection(ConfigKeys.Databases.Quartz).Value;
        if (string.IsNullOrEmpty(connectionString))
            throw new ArgumentNullException(nameof(connectionString), "Quartz database connection string is null");

        collection.AddDbContext<QuartzContext>(options =>
            options.UseNpgsql(connectionString));
    }

    public static void RunBackgroundMigration(this WebApplication app)
    {
        var autoMigration = app.Configuration.GetSection(ConfigKeys.AutoMigration).Get<bool>();
        if (!autoMigration) return;

        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<QuartzContext>();
        context.Database.Migrate();
    }
}
