using Application.Interfaces.Caching;
using Application.Interfaces.Providers;
using Domain.Constants;
using Infrastructure.Providers.Caching;
using Infrastructure.Providers.Foursquare;
using Infrastructure.Providers.OpenTripMap;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Providers;

public static class ProvidersInjection
{
    /// <summary>
    /// Registers the external provider clients and their caching decorators.
    /// <para>
    /// Resolution order:
    /// <list type="bullet">
    ///   <item>IGeocodingProvider → CachedGeocodingProvider wrapping OpenTripMapGeocodingProvider</item>
    ///   <item>IDestinationProvider → CachedDestinationProvider wrapping the keyed provider selected
    ///         by the <c>Providers:Default</c> configuration entry (see <see cref="ProviderNames"/>;
    ///         falls back to OpenTripMap when unset)</item>
    /// </list>
    /// </para>
    /// </summary>
    public static void AddProviders(this IServiceCollection collection)
    {
        // Raw provider implementations (transient — stateless HTTP clients).
        collection.AddTransient<OpenTripMapGeocodingProvider>();

        // Destination provider strategies, keyed by well-known name.
        collection.AddKeyedTransient<IDestinationProvider, OpenTripMapDestinationProvider>(ProviderNames.OpenTripMap);
        collection.AddKeyedTransient<IDestinationProvider, FoursquareDestinationProvider>(ProviderNames.Foursquare);

        // IGeocodingProvider: cached wrapper over OpenTripMap.
        collection.AddScoped<IGeocodingProvider>(sp =>
        {
            var inner = sp.GetRequiredService<OpenTripMapGeocodingProvider>();
            var cache = sp.GetRequiredService<ICacheManager>();
            var logger = sp.GetRequiredService<ILogger<CachedGeocodingProvider>>();
            return new CachedGeocodingProvider(inner, cache, logger);
        });

        // IDestinationProvider: cached wrapper over the configured strategy.
        collection.AddScoped<IDestinationProvider>(sp =>
        {
            var configuredName = sp.GetRequiredService<IConfiguration>()[ConfigKeys.Providers.Default]
                                 ?? ProviderNames.OpenTripMap;
            if (!ProviderNames.All.Contains(configuredName))
            {
                throw new InvalidOperationException(
                    $"Unknown destination provider '{configuredName}' in config '{ConfigKeys.Providers.Default}'. "
                    + $"Valid values: {string.Join(", ", ProviderNames.All)}.");
            }

            var inner = sp.GetRequiredKeyedService<IDestinationProvider>(configuredName);
            var cache = sp.GetRequiredService<ICacheManager>();
            var logger = sp.GetRequiredService<ILogger<CachedDestinationProvider>>();
            return new CachedDestinationProvider(inner, configuredName, cache, logger);
        });
    }
}
