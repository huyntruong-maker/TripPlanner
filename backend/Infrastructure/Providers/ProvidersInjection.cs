using Application.Interfaces.Caching;
using Application.Interfaces.Providers;
using Domain.Constants;
using Infrastructure.Providers.Caching;
using Infrastructure.Providers.Foursquare;
using Infrastructure.Providers.Nominatim;
using Infrastructure.Providers.OpenTripMap;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Providers;

public static class ProvidersInjection
{
    /// <summary>Registers the external provider clients and their caching decorators.</summary>
    public static void AddProviders(this IServiceCollection collection)
    {
        // Raw provider implementations (transient — stateless HTTP clients).
        collection.AddTransient<NominatimGeocodingProvider>();
        collection.AddTransient<OpenTripMapGeocodingProvider>();
        collection.AddTransient<OpenTripMapDestinationProvider>();
        collection.AddTransient<FoursquareDestinationProvider>();

        // IGeocodingProvider: cached wrapper over the provider selected by Providers:Geocoding:Provider
        // ("Nominatim" default — up to 5 ranked results, F1-US2; "OpenTripMap" — single best match only).
        collection.AddScoped<IGeocodingProvider>(sp =>
        {
            var configuration = sp.GetRequiredService<IConfiguration>();
            var logger = sp.GetRequiredService<ILogger<CachedGeocodingProvider>>();
            var providerName = configuration[ConfigKeys.Providers.Geocoding.Provider];

            IGeocodingProvider inner = providerName?.Trim().ToLowerInvariant() switch
            {
                "opentripmap" => sp.GetRequiredService<OpenTripMapGeocodingProvider>(),
                "nominatim" or null or "" => sp.GetRequiredService<NominatimGeocodingProvider>(),
                _ => LogUnknownProviderAndFallback(logger, providerName!, sp.GetRequiredService<NominatimGeocodingProvider>())
            };

            return new CachedGeocodingProvider(inner, sp.GetRequiredService<ICacheManager>(), logger);
        });

        // IDestinationProvider: cached wrapper over OpenTripMap.
        // Foursquare is registered separately for direct injection in enrichment scenarios.
        collection.AddScoped<IDestinationProvider>(sp =>
        {
            var inner = sp.GetRequiredService<OpenTripMapDestinationProvider>();
            var cache = sp.GetRequiredService<ICacheManager>();
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<CachedDestinationProvider>>();
            return new CachedDestinationProvider(inner, cache, logger);
        });
    }

    /// <summary>Logs an unrecognized <c>Providers:Geocoding:Provider</c> value and falls back to Nominatim.</summary>
    private static IGeocodingProvider LogUnknownProviderAndFallback(
        ILogger<CachedGeocodingProvider> logger,
        string providerName,
        IGeocodingProvider fallback)
    {
        logger.LogWarning(
            "[Geocoding] Unknown Providers:Geocoding:Provider value '{ProviderName}'; falling back to Nominatim.",
            providerName);
        return fallback;
    }
}
