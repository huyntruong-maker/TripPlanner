using Application.Dtos.Destinations;
using Application.Interfaces.Caching;
using Application.Interfaces.Providers;
using Domain.Constants;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Providers.Caching;

public class CachedGeocodingProvider(
    IGeocodingProvider inner,
    ICacheManager cacheManager,
    IConfiguration configuration,
    ILogger<CachedGeocodingProvider> logger) : IGeocodingProvider
{
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
        await cacheManager.SetData(cacheKey, result.ToList(), GetLocationTtl());
        return result;
    }

    private TimeSpan GetLocationTtl()
    {
        var hours = configuration.GetSection(ConfigKeys.Caching.Locations.TtlHours).Get<double?>();
        return TimeSpan.FromHours(hours ?? 1);
    }
}
