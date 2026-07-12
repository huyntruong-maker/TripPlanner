using Application.Interfaces.DataAccess;
using Domain.Constants;
using Infrastructure.DataAccess.DbContexts;
using Infrastructure.DataAccess.UnitOfWorks;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;

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

        var connectionString = app.Configuration.GetSection(ConfigKeys.Databases.WriteDatabase).Value;
        if (string.IsNullOrEmpty(connectionString))
            throw new ArgumentNullException(nameof(connectionString), "Write database connection string is null");

        // EnableRetryOnFailure is incompatible with EF migrations (which use internal transactions).
        // Use a plain context without retry for the migration step.
        var options = new DbContextOptionsBuilder<WriteDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        using var context = new WriteDbContext(options);

        // The database may still be starting when the app boots (e.g. docker compose cold start),
        // so wait for it instead of crashing on the first refused connection.
        var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(RunMigration));
        const int maxConnectAttempts = 10;
        var retryDelay = TimeSpan.FromSeconds(3);

        for (var attempt = 1; ; attempt++)
        {
            try
            {
                context.Database.Migrate();
                return;
            }
            catch (NpgsqlException exception) when (attempt < maxConnectAttempts)
            {
                logger.LogWarning(
                    "Database is not ready yet ({Message}). Attempt {Attempt}/{MaxAttempts}, retrying in {DelaySeconds}s.",
                    exception.Message, attempt, maxConnectAttempts, retryDelay.TotalSeconds);
                Thread.Sleep(retryDelay);
            }
        }
    }

    private static void AddReadDbContext(this IServiceCollection collection, IConfiguration configuration)
    {
        var connectionString = configuration.GetSection(ConfigKeys.Databases.ReadDatabase).Value;
        if (string.IsNullOrEmpty(connectionString))
            throw new ArgumentNullException(nameof(connectionString), "Read database connection string is null");

        collection.AddDbContext<ReadDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsql => npgsql.EnableRetryOnFailure());
        });
    }

    private static void AddWriteDbContext(this IServiceCollection collection, IConfiguration configuration)
    {
        var connectionString = configuration.GetSection(ConfigKeys.Databases.WriteDatabase).Value;
        if (string.IsNullOrEmpty(connectionString))
            throw new ArgumentNullException(nameof(connectionString), "Write database connection string is null");

        collection.AddDbContext<WriteDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsql => npgsql.EnableRetryOnFailure());
        });
    }
}
