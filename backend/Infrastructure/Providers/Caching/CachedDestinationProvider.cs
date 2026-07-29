using Application.Dtos.Destinations;
using Application.Interfaces.Caching;
using Application.Interfaces.Providers;
using Domain.Constants;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Providers.Caching;

public class CachedDestinationProvider(
    IDestinationProvider inner,
    ICacheManager cacheManager,
    IConfiguration configuration,
    ILogger<CachedDestinationProvider> logger) : IDestinationProvider
{
    public async Task<AttractionSearchResultDto> GetAttractionsAsync(
        double latitude,
        double longitude,
        int radiusMeters = 20_000,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
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
        await cacheManager.SetData(cacheKey, result, GetAttractionListTtl());
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
            await cacheManager.SetData(cacheKey, result, GetAttractionDetailTtl());

        return result;
    }

    private TimeSpan GetAttractionListTtl()
    {
        var minutes = configuration.GetSection(ConfigKeys.Caching.Destinations.AttractionListTtlMinutes).Get<double?>();
        return TimeSpan.FromMinutes(minutes ?? 30);
    }

    private TimeSpan GetAttractionDetailTtl()
    {
        var hours = configuration.GetSection(ConfigKeys.Caching.Destinations.AttractionDetailTtlHours).Get<double?>();
        return TimeSpan.FromHours(hours ?? 24);
    }
}
