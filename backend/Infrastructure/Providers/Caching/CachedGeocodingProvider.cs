using Application.Dtos.Destinations;
using Application.Interfaces.Caching;
using Application.Interfaces.Providers;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Providers.Caching;

/// <summary>Caches location search results for 1 hour to meet NFR-1 (≤500 ms for 95% of requests).</summary>
public class CachedGeocodingProvider(
    IGeocodingProvider inner,
    ICacheManager cacheManager,
    ILogger<CachedGeocodingProvider> logger) : IGeocodingProvider
{
    private static readonly TimeSpan LocationTtl = TimeSpan.FromHours(1);

    public async Task<IReadOnlyList<LocationDto>> SearchLocationsAsync(
        string query,
        int maxResults = 5,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return [];

        var cacheKey = $"locations:{query.ToLowerInvariant().Trim()}:{maxResults}";

        var cached = await cacheManager.GetData<List<LocationDto>>(cacheKey);
        if (cached is not null)
        {
            logger.LogDebug("[Cache HIT] locations key={Key}", cacheKey);
            return cached;
        }

        var result = await inner.SearchLocationsAsync(query, maxResults, cancellationToken);
        await cacheManager.SetData(cacheKey, result.ToList(), LocationTtl);
        return result;
    }
}
