using Application.Interfaces.DataAccess;
using Domain.Constants;
using Infrastructure.DataAccess.DbContexts;
using Infrastructure.DataAccess.UnitOfWorks;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.DataAccess;

public static class DataAccessInjection
{
    public static void AddOrchestratorDatabases(this IServiceCollection collection, IConfiguration configuration)
    {
        collection.AddReadDbContext(configuration);
        collection.AddWriteDbContext(configuration);

        collection.AddScoped<IWriteUnitOfWork, WriteUnitOfWork>();
        collection.AddScoped<IReadUnitOfWork, ReadUnitOfWork>();
    }

    public static void RunMigration(this WebApplication app)
    {
        var autoMigration = app.Configuration.GetSection(ConfigKeys.AutoMigration).Get<bool>();
        if (!autoMigration) return;

        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<WriteDbContext>();
        context.Database.Migrate();
    }

    private static void AddReadDbContext(this IServiceCollection collection, IConfiguration configuration)
    {
        var connectionString = configuration.GetSection(ConfigKeys.Databases.ReadDatabase).Value;
        if (string.IsNullOrEmpty(connectionString))
            throw new ArgumentNullException(nameof(connectionString), "Read database connection string is null");

        collection.AddDbContext<ReadDbContext>(options =>
        {
            options.UseNpgsql(connectionString);
        });
    }

    private static void AddWriteDbContext(this IServiceCollection collection, IConfiguration configuration)
    {
        var connectionString = configuration.GetSection(ConfigKeys.Databases.WriteDatabase).Value;
        if (string.IsNullOrEmpty(connectionString))
            throw new ArgumentNullException(nameof(connectionString), "Write database connection string is null");

        collection.AddDbContext<WriteDbContext>(options =>
        {
            options.UseNpgsql(connectionString);
        });
    }
}
