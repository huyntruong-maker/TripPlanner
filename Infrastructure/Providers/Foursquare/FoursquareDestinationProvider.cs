using System.Net;
using System.Text.Json;
using Application.Dtos.Destinations;
using Application.Interfaces.Providers;
using Application.Interfaces.Restful;
using Domain.Constants;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Providers.Foursquare;

/// <summary>
/// Fetches and enriches attraction data using the Foursquare Places API v3.
/// Used as an enrichment source: categories, ratings, and photos.
/// </summary>
public class FoursquareDestinationProvider(
    IRestfulService restfulService,
    IConfiguration configuration,
    ILogger<FoursquareDestinationProvider> logger) : IDestinationProvider
{
    private const int MaxPageSize = 20;

    private string BaseUrl => configuration.GetSection(ConfigKeys.Providers.Foursquare.BaseUrl).Value
                              ?? "https://api.foursquare.com/v3/places";

    private string ApiKey => configuration.GetSection(ConfigKeys.Providers.Foursquare.ApiKey).Value
                             ?? string.Empty;

    private Dictionary<string, string> AuthHeader => new() { ["Authorization"] = ApiKey };

    public async Task<AttractionSearchResultDto> GetAttractionsAsync(
        double latitude,
        double longitude,
        int radiusMeters = 20_000,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var effectivePageSize = Math.Min(pageSize, MaxPageSize);

        var url = $"{BaseUrl}/search"
                  + $"?ll={latitude},{longitude}"
                  + $"&radius={radiusMeters}"
                  + $"&limit={effectivePageSize}"
                  + "&fields=fsq_id,name,categories,rating,geocodes,location,photos";

        var (statusCode, body) = await restfulService.Get(url, AuthHeader);
        if (statusCode != HttpStatusCode.OK || string.IsNullOrWhiteSpace(body))
        {
            logger.LogWarning("[Foursquare] Nearby search returned {Status}", statusCode);
            return new AttractionSearchResultDto();
        }

        try
        {
            var doc = JsonDocument.Parse(body);
            if (!doc.RootElement.TryGetProperty("results", out var results))
                return new AttractionSearchResultDto();

            var attractions = results.EnumerateArray().Select(MapPlace).ToList();
            return new AttractionSearchResultDto
            {
                Items = attractions,
                TotalCount = attractions.Count
            };
        }
        catch (JsonException ex)
        {
            logger.LogError("[Foursquare] Failed to parse nearby response: {Ex}", ex);
            return new AttractionSearchResultDto();
        }
    }

    public async Task<AttractionDto?> GetAttractionDetailAsync(
        string providerPlaceId,
        CancellationToken cancellationToken = default)
    {
        var url = $"{BaseUrl}/{Uri.EscapeDataString(providerPlaceId)}"
                  + "?fields=fsq_id,name,categories,rating,geocodes,location,photos,description,hours";

        var (statusCode, body) = await restfulService.Get(url, AuthHeader);

        if (statusCode == HttpStatusCode.NotFound)
            return null;

        if (statusCode != HttpStatusCode.OK || string.IsNullOrWhiteSpace(body))
        {
            logger.LogWarning("[Foursquare] Detail fetch returned {Status} for fsq_id '{Id}'", statusCode, providerPlaceId);
            return null;
        }

        try
        {
            var doc = JsonDocument.Parse(body);
            return MapPlace(doc.RootElement);
        }
        catch (JsonException ex)
        {
            logger.LogError("[Foursquare] Failed to parse detail response for '{Id}': {Ex}", providerPlaceId, ex);
            return null;
        }
    }

    // -------------------------------------------------------------------------
    // Private mapping helpers
    // -------------------------------------------------------------------------

    private static AttractionDto MapPlace(JsonElement place)
    {
        var fsqId = place.TryGetProperty("fsq_id", out var idProp)
            ? idProp.GetString() ?? string.Empty
            : string.Empty;

        var name = place.TryGetProperty("name", out var nameProp)
            ? nameProp.GetString() ?? "Unknown"
            : "Unknown";

        // Categories — first entry is primary, rest are tags.
        var tags = new List<string>();
        string? category = null;
        if (place.TryGetProperty("categories", out var categories))
        {
            foreach (var cat in categories.EnumerateArray())
            {
                if (cat.TryGetProperty("name", out var catName) && catName.GetString() is { } cn)
                    tags.Add(cn);
            }
            category = tags.FirstOrDefault();
        }

        double? rating = place.TryGetProperty("rating", out var ratingProp) && ratingProp.ValueKind == JsonValueKind.Number
            ? ratingProp.GetDouble()
            : null;

        double lat = 0, lon = 0;
        if (place.TryGetProperty("geocodes", out var geocodes)
            && geocodes.TryGetProperty("main", out var main))
        {
            if (main.TryGetProperty("latitude", out var latProp)) lat = latProp.GetDouble();
            if (main.TryGetProperty("longitude", out var lonProp)) lon = lonProp.GetDouble();
        }

        string? thumbnail = null;
        if (place.TryGetProperty("photos", out var photos))
        {
            var firstPhoto = photos.EnumerateArray().FirstOrDefault();
            if (firstPhoto.ValueKind == JsonValueKind.Object
                && firstPhoto.TryGetProperty("prefix", out var prefix)
                && firstPhoto.TryGetProperty("suffix", out var suffix))
            {
                thumbnail = $"{prefix.GetString()}300x300{suffix.GetString()}";
            }
        }

        string? address = null;
        if (place.TryGetProperty("location", out var location))
        {
            var parts = new List<string>();
            if (location.TryGetProperty("address", out var addr) && addr.GetString() is { } a) parts.Add(a);
            if (location.TryGetProperty("locality", out var city) && city.GetString() is { } c) parts.Add(c);
            if (location.TryGetProperty("country", out var country) && country.GetString() is { } co) parts.Add(co);
            address = parts.Count > 0 ? string.Join(", ", parts) : null;
        }

        return new AttractionDto
        {
            ProviderPlaceId = fsqId,
            Name = name,
            Category = category,
            Tags = tags,
            Rating = rating,
            Latitude = lat,
            Longitude = lon,
            ThumbnailUrl = thumbnail,
            Address = address
        };
    }
}
