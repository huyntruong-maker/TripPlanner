using System.Net;
using System.Text.Json;
using Application.Dtos.Destinations;
using Application.Interfaces.Providers;
using Application.Interfaces.Restful;
using Domain.Constants;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Providers.Foursquare;

/// <summary>Fetches attraction data (categories, ratings, photos) from the new Foursquare Places API
/// (<c>places-api.foursquare.com</c>).</summary>
public class FoursquareDestinationProvider(
    IRestfulService restfulService,
    IConfiguration configuration,
    ILogger<FoursquareDestinationProvider> logger) : IDestinationProvider
{
    private const int MaxPageSize = 20;

    private const string PlacesApiVersionHeaderName = "X-Places-Api-Version";
    private const string PlacesApiVersion = "2025-06-17";

    private const string CoreFields = "fsq_place_id,name,categories,latitude,longitude,location";
    private const string PremiumFields = "rating,photos,hours,description,website";
    private const string FullFields = CoreFields + "," + PremiumFields;

    // Set for the process lifetime once the account is found to have no premium API credits, so every
    // subsequent call requests core fields only instead of paying the round-trip cost of a failed premium call.
    private static int _premiumFieldsDisabled;

    private static bool IsPremiumFieldsDisabled =>
        Interlocked.CompareExchange(ref _premiumFieldsDisabled, 0, 0) == 1;

    private string BaseUrl => configuration.GetSection(ConfigKeys.Providers.Foursquare.BaseUrl).Value
                              ?? "https://places-api.foursquare.com";

    private string ApiKey => configuration.GetSection(ConfigKeys.Providers.Foursquare.ApiKey).Value
                             ?? string.Empty;

    private Dictionary<string, string> AuthHeaders => new()
    {
        ["Authorization"] = $"Bearer {ApiKey}",
        [PlacesApiVersionHeaderName] = PlacesApiVersion
    };

    public async Task<AttractionSearchResultDto> GetAttractionsAsync(
        double latitude,
        double longitude,
        int radiusMeters = 20_000,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var effectivePageSize = Math.Min(pageSize, MaxPageSize);

        string BuildUrl(string fields) =>
            $"{BaseUrl}/places/search"
            + $"?ll={latitude},{longitude}"
            + $"&radius={radiusMeters}"
            + $"&limit={effectivePageSize}"
            + $"&fields={fields}";

        var (statusCode, body) = await GetWithPremiumFallbackAsync(BuildUrl);
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

    /// <summary>
    /// Finds the Foursquare place nearest to <paramref name="latitude"/>/<paramref name="longitude"/> whose name
    /// best matches <paramref name="name"/>, used to enrich an OpenTripMap POI (which keeps its own <c>xid</c> as
    /// the public <c>ProviderPlaceId</c> — see <c>FoursquareEnrichedDestinationProvider</c>). Best-effort: returns
    /// null when disabled (no API key), not found, or on any request/parse failure.
    /// </summary>
    public async Task<AttractionDto?> FindNearestMatchAsync(
        string name,
        double latitude,
        double longitude,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(ApiKey))
            return null;

        const int matchRadiusMeters = 300;

        string BuildUrl(string fields) =>
            $"{BaseUrl}/places/search"
            + $"?query={Uri.EscapeDataString(name)}"
            + $"&ll={latitude},{longitude}"
            + $"&radius={matchRadiusMeters}"
            + "&limit=1"
            + $"&fields={fields}";

        var (statusCode, body) = await GetWithPremiumFallbackAsync(BuildUrl);
        if (statusCode != HttpStatusCode.OK || string.IsNullOrWhiteSpace(body))
        {
            logger.LogWarning("[Foursquare] Nearest-match search returned {Status} for '{Name}'", statusCode, name);
            return null;
        }

        try
        {
            var doc = JsonDocument.Parse(body);
            if (!doc.RootElement.TryGetProperty("results", out var results) || results.ValueKind != JsonValueKind.Array)
                return null;

            var first = results.EnumerateArray().FirstOrDefault();
            return first.ValueKind == JsonValueKind.Object ? MapPlace(first) : null;
        }
        catch (JsonException ex)
        {
            logger.LogError("[Foursquare] Failed to parse nearest-match response for '{Name}': {Ex}", name, ex);
            return null;
        }
    }

    public async Task<AttractionDto?> GetAttractionDetailAsync(
        string providerPlaceId,
        CancellationToken cancellationToken = default)
    {
        string BuildUrl(string fields) =>
            $"{BaseUrl}/places/{Uri.EscapeDataString(providerPlaceId)}"
            + $"?fields={fields}";

        var (statusCode, body) = await GetWithPremiumFallbackAsync(BuildUrl);

        if (statusCode == HttpStatusCode.NotFound)
            return null;

        if (statusCode != HttpStatusCode.OK || string.IsNullOrWhiteSpace(body))
        {
            logger.LogWarning("[Foursquare] Detail fetch returned {Status} for fsq_place_id '{Id}'", statusCode, providerPlaceId);
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

    /// <summary>
    /// Issues the request with the current field set (full when premium is still assumed available, core-only
    /// once it has been found to be disabled). If a full-field request fails with the account's "no premium
    /// credits" error, flips the process-lifetime flag, logs once, and immediately retries with core fields only
    /// so the caller still gets a usable (category/address) result instead of a hard failure.
    /// </summary>
    private async Task<(HttpStatusCode StatusCode, string Body)> GetWithPremiumFallbackAsync(Func<string, string> buildUrl)
    {
        var requestedFullFields = !IsPremiumFieldsDisabled;
        var fields = requestedFullFields ? FullFields : CoreFields;

        var (statusCode, body) = await restfulService.Get(buildUrl(fields), AuthHeaders);

        if (requestedFullFields && IsPremiumCreditError(statusCode, body))
        {
            LogPremiumFieldsDisabledOnce();
            (statusCode, body) = await restfulService.Get(buildUrl(CoreFields), AuthHeaders);
        }

        return (statusCode, body);
    }

    /// <summary>Defensive, best-effort detection of the Foursquare "no API credits remaining for Premium calls"
    /// error — matched on the response body rather than a specific status code since Foursquare has not
    /// documented a stable status code for this condition.</summary>
    private static bool IsPremiumCreditError(HttpStatusCode statusCode, string body) =>
        statusCode != HttpStatusCode.OK
        && !string.IsNullOrWhiteSpace(body)
        && body.Contains("credits", StringComparison.OrdinalIgnoreCase);

    private void LogPremiumFieldsDisabledOnce()
    {
        if (Interlocked.Exchange(ref _premiumFieldsDisabled, 1) == 0)
            logger.LogWarning(
                "[Foursquare] Premium fields (rating, photos, hours, description, website) are unavailable — " +
                "the account has no API credits remaining. Falling back to core fields (category/address) only " +
                "for the rest of this process's lifetime; add credits and restart the app to re-enable them.");
    }

    private static AttractionDto MapPlace(JsonElement place)
    {
        var fsqId = place.TryGetProperty("fsq_place_id", out var idProp)
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

        // Coordinates are top-level on the new Places API (no more nested "geocodes.main").
        double lat = 0, lon = 0;
        if (place.TryGetProperty("latitude", out var latProp) && latProp.ValueKind == JsonValueKind.Number)
            lat = latProp.GetDouble();
        if (place.TryGetProperty("longitude", out var lonProp) && lonProp.ValueKind == JsonValueKind.Number)
            lon = lonProp.GetDouble();

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
            if (location.TryGetProperty("formatted_address", out var formattedAddress)
                && formattedAddress.GetString() is { } formatted)
            {
                address = formatted;
            }
            else
            {
                var parts = new List<string>();
                if (location.TryGetProperty("address", out var addr) && addr.GetString() is { } a) parts.Add(a);
                if (location.TryGetProperty("locality", out var city) && city.GetString() is { } c) parts.Add(c);
                if (location.TryGetProperty("region", out var region) && region.GetString() is { } r) parts.Add(r);
                if (location.TryGetProperty("country", out var country) && country.GetString() is { } co) parts.Add(co);
                address = parts.Count > 0 ? string.Join(", ", parts) : null;
            }
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
