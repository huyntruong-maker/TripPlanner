using System.Net;
using System.Text.Json;
using Application.Dtos.Destinations;
using Application.Interfaces.Providers;
using Application.Interfaces.Restful;
using Domain.Constants;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Providers.OpenTripMap;

/// <summary>Fetches POIs via the OpenTripMap Radius API, enriched with detail-endpoint thumbnails.</summary>
public class OpenTripMapDestinationProvider(
    IRestfulService restfulService,
    IConfiguration configuration,
    ILogger<OpenTripMapDestinationProvider> logger) : IDestinationProvider
{
    private const int MaxPageSize = 20;

    private string BaseUrl => configuration[ConfigKeys.Providers.OpenTripMap.BaseUrl]
                              ?? throw new InvalidOperationException($"Missing config: {ConfigKeys.Providers.OpenTripMap.BaseUrl}");

    private string ApiKey => configuration.GetSection(ConfigKeys.Providers.OpenTripMap.ApiKey).Value
                             ?? string.Empty;

    public async Task<AttractionSearchResultDto> GetAttractionsAsync(
        double latitude,
        double longitude,
        int radiusMeters = 20_000,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var effectivePageSize = Math.Min(pageSize, MaxPageSize);
        var offset = (page - 1) * effectivePageSize;

        var url = $"{BaseUrl}/radius"
                  + $"?radius={radiusMeters}"
                  + $"&lon={longitude}&lat={latitude}"
                  + $"&limit={effectivePageSize}&offset={offset}"
                  + $"&rate=3"        // minimum rate — high-quality POIs only
                  + $"&format=json"
                  + $"&apikey={ApiKey}";

        HttpStatusCode statusCode;
        string body;
        try
        {
            (statusCode, body) = await restfulService.Get(url);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or IOException)
        {
            // Transient network failure — degrade to empty result rather than a 500.
            logger.LogWarning("[OpenTripMap] Radius search request failed: {Ex}", ex.Message);
            return new AttractionSearchResultDto();
        }

        if (statusCode != HttpStatusCode.OK || string.IsNullOrWhiteSpace(body))
        {
            logger.LogWarning("[OpenTripMap] Radius search returned {Status}", statusCode);
            return new AttractionSearchResultDto();
        }

        try
        {
            var docs = JsonDocument.Parse(body);
            var features = docs.RootElement.EnumerateArray().ToList();

            var attractions = features.Select(MapFeature).ToList();
            await EnrichThumbnailsAsync(attractions, cancellationToken);

            return new AttractionSearchResultDto
            {
                Items = attractions,
                TotalCount = attractions.Count
            };
        }
        catch (JsonException ex)
        {
            logger.LogError("[OpenTripMap] Failed to parse radius response: {Ex}", ex);
            return new AttractionSearchResultDto();
        }
    }

    // Radius API omits photos; fetch them from the detail endpoint in parallel, best-effort.
    private async Task EnrichThumbnailsAsync(List<AttractionDto> attractions, CancellationToken cancellationToken)
    {
        var tasks = attractions
            .Where(a => !string.IsNullOrWhiteSpace(a.ProviderPlaceId))
            .Select(a => EnrichThumbnailAsync(a, cancellationToken));

        await Task.WhenAll(tasks);
    }

    private async Task EnrichThumbnailAsync(AttractionDto attraction, CancellationToken cancellationToken)
    {
        try
        {
            var detail = await GetAttractionDetailAsync(attraction.ProviderPlaceId, cancellationToken);
            attraction.ThumbnailUrl = detail?.ThumbnailUrl;
        }
        catch (Exception ex)
        {
            logger.LogWarning("[OpenTripMap] Thumbnail enrichment failed for xid '{Xid}': {Ex}", attraction.ProviderPlaceId, ex.Message);
        }
    }

    public async Task<AttractionDto?> GetAttractionDetailAsync(
        string providerPlaceId,
        CancellationToken cancellationToken = default)
    {
        var url = $"{BaseUrl}/xid/{Uri.EscapeDataString(providerPlaceId)}?apikey={ApiKey}";

        HttpStatusCode statusCode;
        string body;
        try
        {
            (statusCode, body) = await restfulService.Get(url);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or IOException)
        {
            logger.LogWarning("[OpenTripMap] Detail fetch request failed for xid '{Xid}': {Ex}", providerPlaceId, ex.Message);
            return null;
        }

        if (statusCode == HttpStatusCode.NotFound)
            return null;

        if (statusCode != HttpStatusCode.OK || string.IsNullOrWhiteSpace(body))
        {
            logger.LogWarning("[OpenTripMap] Detail fetch returned {Status} for xid '{Xid}'", statusCode, providerPlaceId);
            return null;
        }

        try
        {
            var doc = JsonDocument.Parse(body);
            return MapDetail(doc.RootElement);
        }
        catch (JsonException ex)
        {
            logger.LogError("[OpenTripMap] Failed to parse detail response for '{Xid}': {Ex}", providerPlaceId, ex);
            return null;
        }
    }

    private static AttractionDto MapFeature(JsonElement feature)
    {
        var properties = feature.TryGetProperty("properties", out var p) ? p : feature;
        var geometry = feature.TryGetProperty("geometry", out var g) ? g : default;

        var name = properties.TryGetProperty("name", out var nameProp)
            ? nameProp.GetString() ?? "Unknown"
            : "Unknown";

        var xid = properties.TryGetProperty("xid", out var xidProp)
            ? xidProp.GetString() ?? string.Empty
            : string.Empty;

        var kinds = properties.TryGetProperty("kinds", out var kindsProp)
            ? kindsProp.GetString() ?? string.Empty
            : string.Empty;

        var tags = kinds.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        .ToList();

        var category = tags.FirstOrDefault();

        double lat = 0, lon = 0;
        if (geometry.ValueKind == JsonValueKind.Object
            && geometry.TryGetProperty("coordinates", out var coords)
            && coords.ValueKind == JsonValueKind.Array)
        {
            var coordArr = coords.EnumerateArray().ToArray();
            if (coordArr.Length >= 2)
            {
                lon = coordArr[0].GetDouble();
                lat = coordArr[1].GetDouble();
            }
        }

        return new AttractionDto
        {
            ProviderPlaceId = xid,
            Name = name,
            Category = category,
            Tags = tags,
            // Rating is a 0-10 scale (Foursquare reviews); OpenTripMap's "rate" is a 1-7 importance class, not a review score, so it's left null here.
            Rating = null,
            Latitude = lat,
            Longitude = lon
        };
    }

    private static AttractionDto? MapDetail(JsonElement root)
    {
        var xid = root.TryGetProperty("xid", out var xidProp) ? xidProp.GetString() : null;
        if (string.IsNullOrWhiteSpace(xid))
            return null;

        var name = root.TryGetProperty("name", out var nameProp)
            ? nameProp.GetString() ?? "Unknown"
            : "Unknown";

        var kinds = root.TryGetProperty("kinds", out var kindsProp)
            ? kindsProp.GetString() ?? string.Empty
            : string.Empty;

        var tags = kinds.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        .ToList();

        var category = tags.FirstOrDefault();

        double lat = 0, lon = 0;
        if (root.TryGetProperty("point", out var point))
        {
            if (point.TryGetProperty("lat", out var latProp)) lat = latProp.GetDouble();
            if (point.TryGetProperty("lon", out var lonProp)) lon = lonProp.GetDouble();
        }

        // Use Special:FilePath (not preview.source) since Wikimedia can 400 on fixed thumbnail widths.
        string? thumbnail = null;
        var photos = new List<string>();
        if (root.TryGetProperty("image", out var imageProp))
        {
            thumbnail = BuildWikimediaThumbnailUrl(imageProp.GetString());
            if (!string.IsNullOrWhiteSpace(thumbnail))
                photos.Add(thumbnail);
        }

        string? address = null;
        if (root.TryGetProperty("address", out var addr))
        {
            var parts = new List<string>();
            if (addr.TryGetProperty("road", out var road) && road.GetString() is { } r) parts.Add(r);
            if (addr.TryGetProperty("city", out var city) && city.GetString() is { } c) parts.Add(c);
            if (addr.TryGetProperty("country", out var country) && country.GetString() is { } co) parts.Add(co);
            address = parts.Count > 0 ? string.Join(", ", parts) : null;
        }

        // Description — OpenTripMap returns "wikipedia_extracts.text" on detail calls.
        string? description = null;
        if (root.TryGetProperty("wikipedia_extracts", out var extracts)
            && extracts.TryGetProperty("text", out var textProp))
        {
            description = textProp.GetString();
        }

        // Website — returned as "url" on OpenTripMap detail responses.
        string? website = null;
        if (root.TryGetProperty("url", out var urlProp))
            website = urlProp.GetString();

        // OpenTripMap does not provide structured opening hours.
        return new AttractionDto
        {
            ProviderPlaceId = xid,
            Name = name,
            Category = category,
            Tags = tags,
            // See MapFeature: Rating is Foursquare-only (0-10 review score); OpenTripMap's "rate" (1-7 importance class) must not leak into this field.
            Rating = null,
            Latitude = lat,
            Longitude = lon,
            ThumbnailUrl = thumbnail,
            Address = address,
            Description = description,
            Photos = photos,
            Website = website,
            OpeningHours = null
        };
    }

    private const int ThumbnailWidth = 400;
    private const string CommonsFileMarker = "/wiki/File:";

    private static string? BuildWikimediaThumbnailUrl(string? commonsFilePageUrl)
    {
        if (string.IsNullOrWhiteSpace(commonsFilePageUrl))
            return null;

        var markerIndex = commonsFilePageUrl.IndexOf(CommonsFileMarker, StringComparison.OrdinalIgnoreCase);
        if (markerIndex < 0)
            return null;

        var fileName = commonsFilePageUrl[(markerIndex + CommonsFileMarker.Length)..];
        return $"https://commons.wikimedia.org/wiki/Special:FilePath/{fileName}?width={ThumbnailWidth}";
    }
}
