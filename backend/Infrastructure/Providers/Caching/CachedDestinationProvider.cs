using Application.Dtos.Destinations;
using Application.Interfaces.Caching;
using Application.Interfaces.Providers;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Providers.Caching;

/// <summary>Caches attraction lists (30 min) and details (24 h) to meet NFR-1/NFR-2 latency targets.</summary>
public class CachedDestinationProvider(
    IDestinationProvider inner,
    ICacheManager cacheManager,
    ILogger<CachedDestinationProvider> logger) : IDestinationProvider
{
    private static readonly TimeSpan AttractionListTtl = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan AttractionDetailTtl = TimeSpan.FromHours(24);

    public async Task<AttractionSearchResultDto> GetAttractionsAsync(
        double latitude,
        double longitude,
        int radiusMeters = 20_000,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        // Round coordinates to 4 decimal places (~11 m precision) for stable cache keys.
        var latKey = Math.Round(latitude, 4);
        var lonKey = Math.Round(longitude, 4);
        var cacheKey = $"attractions:{latKey}:{lonKey}:{radiusMeters}:p{page}:s{pageSize}";

        var cached = await cacheManager.GetData<AttractionSearchResultDto>(cacheKey);
        if (cached is not null)
        {
            logger.LogDebug("[Cache HIT] attractions key={Key}", cacheKey);
            return cached;
        }

        var result = await inner.GetAttractionsAsync(latitude, longitude, radiusMeters, page, pageSize, cancellationToken);
        await cacheManager.SetData(cacheKey, result, AttractionListTtl);
        return result;
    }

    public async Task<AttractionDto?> GetAttractionDetailAsync(
        string providerPlaceId,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"attraction-detail:{providerPlaceId}";

        var cached = await cacheManager.GetData<AttractionDto>(cacheKey);
        if (cached is not null)
        {
            logger.LogDebug("[Cache HIT] attraction-detail key={Key}", cacheKey);
            return cached;
        }

        var result = await inner.GetAttractionDetailAsync(providerPlaceId, cancellationToken);
        if (result is not null)
            await cacheManager.SetData(cacheKey, result, AttractionDetailTtl);

        return result;
    }
}
