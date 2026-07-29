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
    public static void AddProviders(this IServiceCollection collection)
    {
        collection.AddTransient<NominatimGeocodingProvider>();
        collection.AddTransient<OpenTripMapGeocodingProvider>();
        collection.AddTransient<OpenTripMapDestinationProvider>();
        collection.AddTransient<FoursquareDestinationProvider>();

        collection.AddScoped<IGeocodingProvider>(sp =>
        {
            var configuration = sp.GetRequiredService<IConfiguration>();
            var logger = sp.GetRequiredService<ILogger<CachedGeocodingProvider>>();
            var providerName = configuration[ConfigKeys.Providers.Geocoding.Provider];

            IGeocodingProvider primary = providerName?.Trim().ToLowerInvariant() switch
            {
                "opentripmap" => sp.GetRequiredService<OpenTripMapGeocodingProvider>(),
                "nominatim" or null or "" => sp.GetRequiredService<NominatimGeocodingProvider>(),
                _ => LogUnknownGeocodingProviderAndFallback(logger, providerName!, sp.GetRequiredService<NominatimGeocodingProvider>())
            };

            return new CachedGeocodingProvider(primary, sp.GetRequiredService<ICacheManager>(), configuration, logger);
        });

        collection.AddScoped<IDestinationProvider>(sp =>
        {
            var configuration = sp.GetRequiredService<IConfiguration>();
            var destinationLogger = sp.GetRequiredService<ILogger<CachedDestinationProvider>>();
            var providerName = configuration[ConfigKeys.Providers.Destination.Provider];

            IDestinationProvider primary = providerName?.Trim().ToLowerInvariant() switch
            {
                "foursquare" => sp.GetRequiredService<FoursquareDestinationProvider>(),
                "opentripmap" or null or "" => BuildFoursquareEnriched(sp, configuration),
                _ => LogUnknownDestinationProviderAndFallback(destinationLogger, providerName!, BuildFoursquareEnriched(sp, configuration))
            };

            return new CachedDestinationProvider(primary, sp.GetRequiredService<ICacheManager>(), configuration, destinationLogger);
        });
    }

    private static IDestinationProvider BuildFoursquareEnriched(IServiceProvider sp, IConfiguration configuration)
    {
        var openTripMap = sp.GetRequiredService<OpenTripMapDestinationProvider>();
        var foursquare = sp.GetRequiredService<FoursquareDestinationProvider>();
        var logger = sp.GetRequiredService<ILogger<FoursquareEnrichedDestinationProvider>>();
        return new FoursquareEnrichedDestinationProvider(openTripMap, foursquare, configuration, logger);
    }

    private static IGeocodingProvider LogUnknownGeocodingProviderAndFallback(
        ILogger<CachedGeocodingProvider> logger,
        string providerName,
        IGeocodingProvider fallback)
    {
        logger.LogWarning("[Geocoding] Unknown Providers:Geocoding:Provider value '{ProviderName}'; falling back to Nominatim.", providerName);
        return fallback;
    }

    private static IDestinationProvider LogUnknownDestinationProviderAndFallback(
        ILogger<CachedDestinationProvider> logger,
        string providerName,
        IDestinationProvider fallback)
    {
        logger.LogWarning("[Destination] Unknown Providers:Destination:Provider value '{ProviderName}'; falling back to OpenTripMap.", providerName);
        return fallback;
    }
}
