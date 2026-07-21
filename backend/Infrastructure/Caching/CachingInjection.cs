using Application.Interfaces.Caching;
using Domain.Constants;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Infrastructure.Caching;

public static class CachingInjection
{
    public static void AddRedis(this IServiceCollection collection, IConfiguration configuration)
    {
        var connectionString = configuration.GetSection(ConfigKeys.Redis.Connection).Value;
        collection.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = connectionString;
            options.InstanceName = configuration.GetSection(ConfigKeys.Redis.InstanceName).Value;
            options.ConfigurationOptions = new ConfigurationOptions
            {
                ConnectRetry = configuration.GetSection(ConfigKeys.Redis.ConnectRetry).Get<int>(),
                ConnectTimeout = configuration.GetSection(ConfigKeys.Redis.ConnectTimeout).Get<int>(),
                AbortOnConnectFail = false
            };
        });

        collection.AddSingleton<IConnectionMultiplexer>(provider =>
        {
            IConnectionMultiplexer? connectionMultiplexer = null;
            try
            {
                connectionMultiplexer = ConnectionMultiplexer.Connect(connectionString ?? string.Empty);
            }
            catch (RedisConnectionException ex)
            {
                Console.WriteLine($"Could not connect to Redis: {ex.Message}");
            }
#pragma warning disable CS8603 // Possible null reference return.
            return connectionMultiplexer;
#pragma warning restore CS8603 // Possible null reference return.
        });

        collection.AddScoped<ICacheManager, CacheManager>();
    }
}