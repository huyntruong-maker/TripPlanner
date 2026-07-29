using System.Net;
using System.Text.Json;
using Application.Dtos.Destinations;
using Application.Interfaces.Providers;
using Application.Interfaces.Restful;
using Domain.Constants;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Providers.OpenTripMap;

public class OpenTripMapGeocodingProvider(
    IRestfulService restfulService,
    IConfiguration configuration,
    ILogger<OpenTripMapGeocodingProvider> logger) : IGeocodingProvider
{
    private string BaseUrl => configuration[ConfigKeys.Providers.OpenTripMap.BaseUrl]
                              ?? throw new InvalidOperationException($"Missing config: {ConfigKeys.Providers.OpenTripMap.BaseUrl}");

    private string ApiKey => configuration.GetSection(ConfigKeys.Providers.OpenTripMap.ApiKey).Value
                             ?? string.Empty;

    public async Task<IReadOnlyList<LocationDto>> SearchLocationsAsync(
        string query,
        int maxResults = 5,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return [];

        // Geoname endpoint returns a single best-match location; wrap it in a list.
        var url = $"{BaseUrl}/geoname?name={Uri.EscapeDataString(query)}&apikey={ApiKey}";

        var (statusCode, body) = await restfulService.Get(url);
        if (statusCode != HttpStatusCode.OK || string.IsNullOrWhiteSpace(body))
        {
            logger.LogWarning("[OpenTripMap] Geocoding returned {Status} for query '{Query}'", statusCode, query);
            return [];
        }

        try
        {
            var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            // The geoname endpoint returns {"status":"OK","lon":...,"lat":...,"name":...,"country":...}
            if (!root.TryGetProperty("name", out var nameProp) ||
                !root.TryGetProperty("lat", out var latProp) ||
                !root.TryGetProperty("lon", out var lonProp))
            {
                return [];
            }

            var name = nameProp.GetString() ?? string.Empty;
            var country = root.TryGetProperty("country", out var countryProp)
                ? countryProp.GetString()
                : null;

            var location = new LocationDto
            {
                Name = name,
                DisplayName = string.IsNullOrWhiteSpace(country) ? name : $"{name}, {country}",
                Latitude = latProp.GetDouble(),
                Longitude = lonProp.GetDouble(),
                Country = country,
                LocationType = "city"
            };

            return [location];
        }
        catch (JsonException ex)
        {
            logger.LogError("[OpenTripMap] Failed to parse geocoding response: {Ex}", ex);
            return [];
        }
    }
}
