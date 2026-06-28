using System.Net;
using System.Text.Json;
using Application.Dtos.Destinations;
using Application.Interfaces.Providers;
using Application.Interfaces.Restful;
using Domain.Constants;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Providers.OpenTripMap;

/// <summary>
/// Fetches points-of-interest using the OpenTripMap Radius API and enriches each result
/// with its detail endpoint to obtain rating and thumbnail when available.
/// </summary>
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

        var (statusCode, body) = await restfulService.Get(url);
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

    public async Task<AttractionDto?> GetAttractionDetailAsync(
        string providerPlaceId,
        CancellationToken cancellationToken = default)
    {
        var url = $"{BaseUrl}/xid/{Uri.EscapeDataString(providerPlaceId)}?apikey={ApiKey}";
        var (statusCode, body) = await restfulService.Get(url);

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

    // -------------------------------------------------------------------------
    // Private mapping helpers
    // -------------------------------------------------------------------------

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

        double? rate = properties.TryGetProperty("rate", out var rateProp) && rateProp.ValueKind == JsonValueKind.Number
            ? rateProp.GetDouble()
            : null;

        return new AttractionDto
        {
            ProviderPlaceId = xid,
            Name = name,
            Category = category,
            Tags = tags,
            Rating = rate,
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

        // Thumbnail — kept for list-compat; also added as the first Photos entry.
        string? thumbnail = null;
        var photos = new List<string>();
        if (root.TryGetProperty("preview", out var preview)
            && preview.TryGetProperty("source", out var sourceProp))
        {
            thumbnail = sourceProp.GetString();
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

        double? rate = root.TryGetProperty("rate", out var rateProp) && rateProp.ValueKind == JsonValueKind.Number
            ? rateProp.GetDouble()
            : null;

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
            Rating = rate,
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
}
