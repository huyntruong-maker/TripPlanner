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

        // Photos — build full URLs from prefix+size+suffix; collect all available.
        string? thumbnail = null;
        var photoUrls = new List<string>();
        if (place.TryGetProperty("photos", out var photosEl))
        {
            foreach (var photo in photosEl.EnumerateArray())
            {
                if (photo.ValueKind == JsonValueKind.Object
                    && photo.TryGetProperty("prefix", out var prefix)
                    && photo.TryGetProperty("suffix", out var suffix))
                {
                    var photoUrl = $"{prefix.GetString()}300x300{suffix.GetString()}";
                    photoUrls.Add(photoUrl);
                }
            }

            thumbnail = photoUrls.FirstOrDefault();
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

        // Description — Foursquare returns "description" directly on detail calls.
        string? description = null;
        if (place.TryGetProperty("description", out var descProp))
            description = descProp.GetString();

        // Website — Foursquare returns "website" on detail calls.
        string? website = null;
        if (place.TryGetProperty("website", out var websiteProp))
            website = websiteProp.GetString();

        // Opening hours — Foursquare returns "hours" with "display", "regular", and "open_now".
        OpeningHoursDto? openingHours = null;
        if (place.TryGetProperty("hours", out var hours))
        {
            var weekdayText = new List<string>();
            if (hours.TryGetProperty("regular", out var regular))
            {
                foreach (var day in regular.EnumerateArray())
                {
                    if (day.TryGetProperty("open", out var openTime)
                        && day.TryGetProperty("close", out var closeTime)
                        && day.TryGetProperty("day", out var dayNum))
                    {
                        var dayName = dayNum.GetInt32() switch
                        {
                            1 => "Monday",
                            2 => "Tuesday",
                            3 => "Wednesday",
                            4 => "Thursday",
                            5 => "Friday",
                            6 => "Saturday",
                            7 => "Sunday",
                            _ => "Unknown"
                        };
                        weekdayText.Add($"{dayName}: {openTime.GetString()} – {closeTime.GetString()}");
                    }
                }
            }

            string? displayText = null;
            if (hours.TryGetProperty("display", out var displayProp))
                displayText = displayProp.GetString();

            bool? isOpenNow = null;
            if (hours.TryGetProperty("open_now", out var openNowProp)
                && (openNowProp.ValueKind == JsonValueKind.True || openNowProp.ValueKind == JsonValueKind.False))
            {
                isOpenNow = openNowProp.GetBoolean();
            }

            openingHours = new OpeningHoursDto
            {
                DisplayText = displayText,
                WeekdayText = weekdayText,
                IsOpenNow = isOpenNow
            };
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
            Address = address,
            Description = description,
            Photos = photoUrls,
            Website = website,
            OpeningHours = openingHours
        };
    }
}
