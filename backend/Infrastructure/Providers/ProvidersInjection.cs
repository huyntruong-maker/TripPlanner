using Application.Interfaces.Caching;
using Application.Interfaces.Providers;
using Infrastructure.Providers.Caching;
using Infrastructure.Providers.Composite;
using Infrastructure.Providers.Foursquare;
using Infrastructure.Providers.OpenTripMap;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Providers;

public static class ProvidersInjection
{
    /// <summary>
    /// Registers the external provider clients and their caching decorators.
    /// <para>
    /// Resolution order:
    /// <list type="bullet">
    ///   <item>IGeocodingProvider → CachedGeocodingProvider wrapping OpenTripMapGeocodingProvider</item>
    ///   <item>IDestinationProvider → CachedDestinationProvider wrapping
    ///         FoursquareEnrichedDestinationProvider, which uses OpenTripMapDestinationProvider as
    ///         the primary POI source and FoursquareDestinationProvider to enrich categories/rating
    ///         (F1-US3), degrading gracefully to unenriched OpenTripMap results when Foursquare is
    ///         unavailable or not configured.</item>
    /// </list>
    /// </para>
    /// </summary>
    public static void AddProviders(this IServiceCollection collection)
    {
        // Raw provider implementations (transient — stateless HTTP clients).
        collection.AddTransient<OpenTripMapGeocodingProvider>();
        collection.AddTransient<OpenTripMapDestinationProvider>();
        collection.AddTransient<FoursquareDestinationProvider>();
        collection.AddTransient<FoursquareEnrichedDestinationProvider>();

        // IGeocodingProvider: cached wrapper over OpenTripMap.
        collection.AddScoped<IGeocodingProvider>(sp =>
        {
            var inner = sp.GetRequiredService<OpenTripMapGeocodingProvider>();
            var cache = sp.GetRequiredService<ICacheManager>();
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<CachedGeocodingProvider>>();
            return new CachedGeocodingProvider(inner, cache, logger);
        });

        // IDestinationProvider: cached wrapper over the OpenTripMap+Foursquare composite so
        // enriched results (not just the raw OpenTripMap ones) are what gets cached.
        collection.AddScoped<IDestinationProvider>(sp =>
        {
            var inner = sp.GetRequiredService<FoursquareEnrichedDestinationProvider>();
            var cache = sp.GetRequiredService<ICacheManager>();
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<CachedDestinationProvider>>();
            return new CachedDestinationProvider(inner, cache, logger);
        });
    }
}
