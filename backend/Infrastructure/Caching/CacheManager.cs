using Application.Interfaces.Caching;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using StackExchange.Redis;
using static Domain.Constants.ConfigKeys;

namespace Infrastructure.Caching;

public class CacheManager(
    IConnectionMultiplexer? redis,
    ILogger<CacheManager> logger) : ICacheManager
{
    private readonly IDatabase? _redisDb = redis?.GetDatabase();

    public async Task<T?> GetData<T>(string key)
    {
        try
        {
            if (_redisDb == null) return default;

            var jsonData = await _redisDb.StringGetAsync(key);
            return !jsonData.IsNullOrEmpty
                ? JsonConvert.DeserializeObject<T>(jsonData!)
                : default;
        }
        catch (Exception ex)
        {
            logger.LogError("[GetData] Redis connection error: {ex}", ex);
            return default;
        }
    }

    public async Task<bool> SetData<T>(string key, T data, TimeSpan duration)
    {
        try
        {
            if (_redisDb == null) return false;

            var jsonData = JsonConvert.SerializeObject(data);
            var res = await _redisDb.StringSetAsync(key, jsonData, duration);
            return res;
        }
        catch (Exception ex)
        {
            logger.LogError("[SetData] Redis connection error: {ex}", ex);
            return false;
        }
    }

    public async Task<bool> RemoveCache(string key)
    {
        try
        {
            if (_redisDb == null) return false;

            var res = await _redisDb.KeyDeleteAsync(key);
            return res;
        }
        catch (Exception ex)
        {
            logger.LogError("[RemoveCache] Redis connection error: {ex}", ex);
            return false;
        }
    }

    public async Task RemoveByPatternAsync(string pattern)
    {
        try
        {
            if (_redisDb == null || redis == null) return;

            var server = redis.GetServer(redis.GetEndPoints().First());
            var keys = server.Keys(pattern: pattern);
            var errorKey = new List<string>();
            foreach (var key in keys)
            {
                await _redisDb.KeyDeleteAsync(key);
            }
        }
        catch (Exception ex)
        {
            logger.LogError("[RemoveByPatternAsync] Redis connection error: {ex}", ex);
        }
    }
}