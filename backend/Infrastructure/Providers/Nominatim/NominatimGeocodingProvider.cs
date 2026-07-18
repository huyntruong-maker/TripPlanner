using System.Globalization;
using System.Net;
using System.Text.Json;
using Application.Dtos.Destinations;
using Application.Interfaces.Providers;
using Application.Interfaces.Restful;
using Domain.Constants;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Providers.Nominatim;

/// <summary>Geocodes a free-text location query using the OpenStreetMap Nominatim search API.</summary>
public class NominatimGeocodingProvider(
    IRestfulService restfulService,
    IConfiguration configuration,
    ILogger<NominatimGeocodingProvider> logger) : IGeocodingProvider
{
    /// <summary>addresstype/type values kept as city- or country-like results.</summary>
    private static readonly HashSet<string> AllowedLocationKinds = new(StringComparer.OrdinalIgnoreCase)
    {
        "city", "town", "village", "municipality", "country", "state", "province"
    };

    /// <summary>Nominatim's usage policy requires a descriptive User-Agent on every request.</summary>
    private static readonly Dictionary<string, string> RequestHeaders = new() { ["User-Agent"] = "TripPlanner/1.0" };

    private string BaseUrl => configuration[ConfigKeys.Providers.Nominatim.BaseUrl]
                              ?? throw new InvalidOperationException($"Missing config: {ConfigKeys.Providers.Nominatim.BaseUrl}");

    public async Task<IReadOnlyList<LocationDto>> SearchLocationsAsync(
        string query,
        int maxResults = 5,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return [];

        var url = $"{BaseUrl}/search?q={Uri.EscapeDataString(query)}&format=jsonv2&limit=10&accept-language=en";

        var (statusCode, body) = await restfulService.Get(url, RequestHeaders);
        if (statusCode != HttpStatusCode.OK || string.IsNullOrWhiteSpace(body))
        {
            logger.LogWarning("[Nominatim] Geocoding returned {Status} for query '{Query}'", statusCode, query);
            return [];
        }

        try
        {
            var doc = JsonDocument.Parse(body);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
                return [];

            var candidates = new List<LocationDto>();
            foreach (var item in doc.RootElement.EnumerateArray())
            {
                var location = ParseLocation(item);
                if (location is not null)
                    candidates.Add(location);
            }

            return RankAndDedupe(candidates, query, maxResults);
        }
        catch (JsonException ex)
        {
            logger.LogError("[Nominatim] Failed to parse geocoding response: {Ex}", ex);
            return [];
        }
    }

    /// <summary>Maps a single jsonv2 search result to a <see cref="LocationDto"/>; null when it fails validation or the filter.</summary>
    private static LocationDto? ParseLocation(JsonElement item)
    {
        if (!item.TryGetProperty("name", out var nameProp) ||
            !item.TryGetProperty("display_name", out var displayNameProp) ||
            !item.TryGetProperty("lat", out var latProp) ||
            !item.TryGetProperty("lon", out var lonProp))
        {
            return null;
        }

        var name = nameProp.GetString();
        var displayName = displayNameProp.GetString();
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(displayName))
            return null;

        var addressType = item.TryGetProperty("addresstype", out var addressTypeProp) ? addressTypeProp.GetString() : null;
        var type = item.TryGetProperty("type", out var typeProp) ? typeProp.GetString() : null;
        var kind = addressType ?? type;

        if (string.IsNullOrWhiteSpace(kind) || !AllowedLocationKinds.Contains(kind))
            return null;

        if (!double.TryParse(latProp.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var lat) ||
            !double.TryParse(lonProp.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var lon))
        {
            return null;
        }

        var isCountry = string.Equals(kind, "country", StringComparison.OrdinalIgnoreCase);
        var country = LastCommaSegment(displayName);

        return new LocationDto
        {
            Name = name,
            DisplayName = isCountry ? name : $"{name}, {country}",
            Latitude = lat,
            Longitude = lon,
            Country = country,
            LocationType = isCountry ? "country" : "city"
        };
    }

    private static string? LastCommaSegment(string displayName)
    {
        var segments = displayName.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        return segments.Length > 0 ? segments[^1] : null;
    }

    /// <summary>Dedupes by (name, country) case-insensitively, then ranks exact-name matches first, preserving provider order otherwise.</summary>
    private static IReadOnlyList<LocationDto> RankAndDedupe(List<LocationDto> candidates, string query, int maxResults)
    {
        var seen = new HashSet<(string Name, string Country)>();
        var deduped = new List<LocationDto>();

        foreach (var candidate in candidates)
        {
            var key = (candidate.Name.ToLowerInvariant(), (candidate.Country ?? string.Empty).ToLowerInvariant());
            if (seen.Add(key))
                deduped.Add(candidate);
        }

        // OrderByDescending is a stable sort, so provider order is preserved within each rank group.
        var ranked = deduped
            .OrderByDescending(location => string.Equals(location.Name, query, StringComparison.OrdinalIgnoreCase))
            .ToList();

        return ranked.Take(maxResults).ToList();
    }
}
