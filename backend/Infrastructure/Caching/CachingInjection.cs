using Application.Interfaces.Caching;
using Domain.Constants;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Infrastructure.Caching;

public static class CachingInjection
{
    public static void AddRedis(this IServiceCollection collection, IConfiguration configuration)
    {
        var connectionString = configuration.GetSection(ConfigKeys.Redis.Connection).Value ?? string.Empty;
        var redisOptions = ConfigurationOptions.Parse(connectionString);
        redisOptions.ConnectRetry = configuration.GetSection(ConfigKeys.Redis.ConnectRetry).Get<int>();
        redisOptions.ConnectTimeout = configuration.GetSection(ConfigKeys.Redis.ConnectTimeout).Get<int>();
        redisOptions.AbortOnConnectFail = false;

        collection.AddStackExchangeRedisCache(options =>
        {
            options.InstanceName = configuration.GetSection(ConfigKeys.Redis.InstanceName).Value;
            options.ConfigurationOptions = redisOptions;
        });

        collection.AddSingleton<IConnectionMultiplexer>(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<IConnectionMultiplexer>>();

            // AbortOnConnectFail = false means Connect() returns a multiplexer that keeps
            // retrying in the background instead of throwing/staying null forever when the
            // very first attempt at startup fails (e.g. Redis not reachable yet).
            var connectionMultiplexer = ConnectionMultiplexer.Connect(redisOptions);

            connectionMultiplexer.ConnectionFailed += (_, args) =>
                logger.LogWarning("Redis connection failed: {FailureType} - {Exception}", args.FailureType, args.Exception);

            connectionMultiplexer.ConnectionRestored += (_, _) =>
                logger.LogInformation("Redis connection restored");

            return connectionMultiplexer;
        });

        collection.AddScoped<ICacheManager, CacheManager>();
    }
}