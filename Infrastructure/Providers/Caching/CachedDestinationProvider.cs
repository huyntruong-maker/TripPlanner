using Application.Dtos.Destinations;
using Application.Interfaces.Caching;
using Application.Interfaces.Providers;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Providers.Caching;

/// <summary>
/// Caching decorator around <see cref="IDestinationProvider"/>.
/// Attraction-list results are cached for 30 minutes; detail records for 24 hours.
/// Keys are namespaced by provider name; empty list results are never cached.
/// Cache misses fall through to the underlying provider transparently.
/// This satisfies NFR-1 (search ≤500 ms) and NFR-2 (attractions ≤1000 ms) for repeated queries.
/// </summary>
public class CachedDestinationProvider(
    IDestinationProvider inner,
    string providerName,
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
        // Keyed by provider: place IDs and result sets are provider-specific, so entries
        // written by one provider must not be served after Providers:Default is switched.
        var cacheKey = $"attractions:{providerName}:{latKey}:{lonKey}:{radiusMeters}:p{page}:s{pageSize}";

        var cached = await cacheManager.GetData<AttractionSearchResultDto>(cacheKey);
        if (cached is not null)
        {
            logger.LogDebug("[Cache HIT] attractions key={Key}", cacheKey);
            return cached;
        }

        var result = await inner.GetAttractionsAsync(latitude, longitude, radiusMeters, page, pageSize, cancellationToken);

        // Providers degrade transient network failures to an empty result, so an empty page
        // is indistinguishable from an outage — caching it would serve "no attractions"
        // for the full TTL. Genuinely empty areas just re-fetch, which is cheap.
        if (!result.IsEmpty)
            await cacheManager.SetData(cacheKey, result, AttractionListTtl);

        return result;
    }

    public async Task<AttractionDto?> GetAttractionDetailAsync(
        string providerPlaceId,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"attraction-detail:{providerName}:{providerPlaceId}";

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
