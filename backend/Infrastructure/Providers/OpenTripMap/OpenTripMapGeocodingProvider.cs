using System.Globalization;
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
/// Geocodes a free-text location query using the OpenTripMap Geoname API
/// (https://api.opentripmap.com/0.1/en/places/geoname).
/// </summary>
/// <remarks>
/// <para>
/// <b>Known provider limitation:</b> OpenTripMap's <c>/geoname</c> endpoint only ever returns a
/// single best-match place per call — it has no "list of candidates" mode. To still surface up to
/// several city/country candidates (F1-US2), this provider issues the primary lookup for the raw
/// query text, then separately resolves the coordinates of any *country* whose name textually
/// matches the query (using .NET's built-in <see cref="RegionInfo"/> table — not invented data —
/// purely to know which country names to try; the coordinates themselves always come from a real
/// <c>/geoname</c> call). This is a pragmatic workaround for a provider that cannot do multi-result
/// geocoding, not a genuine "search" API.
/// </para>
/// <para>
/// Results returned here are raw candidates: they are not deduplicated, ranked, or clamped — see
/// <see cref="IGeocodingProvider"/> remarks. That happens in the application layer.
/// </para>
/// </remarks>
public class OpenTripMapGeocodingProvider(
    IRestfulService restfulService,
    IConfiguration configuration,
    ILogger<OpenTripMapGeocodingProvider> logger) : IGeocodingProvider
{
    private string BaseUrl => configuration[ConfigKeys.Providers.OpenTripMap.BaseUrl]
                              ?? throw new InvalidOperationException($"Missing config: {ConfigKeys.Providers.OpenTripMap.BaseUrl}");

    private string ApiKey => configuration.GetSection(ConfigKeys.Providers.OpenTripMap.ApiKey).Value
                             ?? string.Empty;

    private static readonly Lazy<IReadOnlyList<string>> KnownCountryNames = new(BuildKnownCountryNames);

    public async Task<IReadOnlyList<LocationDto>> SearchLocationsAsync(
        string query,
        int maxResults = 5,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return [];

        var trimmedQuery = query.Trim();
        var candidates = new List<LocationDto>();

        // Primary: the direct best-match lookup for the raw query text (city or country).
        var primary = await GeocodeSingleAsync(trimmedQuery, cancellationToken);
        if (primary is not null)
            candidates.Add(primary);

        // Secondary: resolve any country names that textually match the query so both cities and
        // countries can be surfaced (F1-US2 business rule), bounded to maxResults extra lookups so
        // a short query (e.g. "a") can't fan out into dozens of HTTP calls.
        var countryNameMatches = FindMatchingCountryNames(trimmedQuery, Math.Clamp(maxResults, 1, 5))
            .Where(name => primary is null || !string.Equals(name, primary.Name, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (countryNameMatches.Count > 0)
        {
            var lookups = await Task.WhenAll(
                countryNameMatches.Select(name => GeocodeSingleAsync(name, cancellationToken)));

            candidates.AddRange(lookups.Where(location => location is not null)!);
        }

        return candidates;
    }

    private async Task<LocationDto?> GeocodeSingleAsync(string name, CancellationToken cancellationToken)
    {
        var url = $"{BaseUrl}/geoname?name={Uri.EscapeDataString(name)}&apikey={ApiKey}";

        HttpStatusCode statusCode;
        string body;
        try
        {
            (statusCode, body) = await restfulService.Get(url);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or IOException)
        {
            logger.LogWarning("[OpenTripMap] Geocoding request failed for '{Name}': {Ex}", name, ex.Message);
            return null;
        }

        if (statusCode != HttpStatusCode.OK || string.IsNullOrWhiteSpace(body))
        {
            logger.LogWarning("[OpenTripMap] Geocoding returned {Status} for query '{Name}'", statusCode, name);
            return null;
        }

        try
        {
            var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            // The geoname endpoint returns {"status":"OK","lon":...,"lat":...,"name":...,"country":...}
            // where "country" is an ISO 3166-1 alpha-2 code, not a display name.
            if (!root.TryGetProperty("name", out var nameProp) ||
                !root.TryGetProperty("lat", out var latProp) ||
                !root.TryGetProperty("lon", out var lonProp))
            {
                return null;
            }

            var placeName = nameProp.GetString() ?? string.Empty;
            var countryCode = root.TryGetProperty("country", out var countryProp) ? countryProp.GetString() : null;
            var countryName = ResolveCountryDisplayName(countryCode);

            // GeoNames (which /geoname wraps) also holds country-level records, where the place
            // name itself matches the resolved country name (e.g. name="France", country="FR").
            var isCountryLevelResult = !string.IsNullOrWhiteSpace(countryName)
                                        && string.Equals(countryName, placeName, StringComparison.OrdinalIgnoreCase);

            return new LocationDto
            {
                Name = placeName,
                DisplayName = isCountryLevelResult || string.IsNullOrWhiteSpace(countryName)
                    ? placeName
                    : $"{placeName}, {countryName}",
                Latitude = latProp.GetDouble(),
                Longitude = lonProp.GetDouble(),
                Country = string.IsNullOrWhiteSpace(countryName) ? countryCode : countryName,
                LocationType = isCountryLevelResult ? "country" : "city"
            };
        }
        catch (JsonException ex)
        {
            logger.LogError("[OpenTripMap] Failed to parse geocoding response for '{Name}': {Ex}", name, ex);
            return null;
        }
    }

    /// <summary>
    /// Returns the country names (from the .NET region table) whose display name contains
    /// <paramref name="query"/>, case-insensitively — e.g. "Unite" matches "United Kingdom" and
    /// "United States". Exact matches sort first; capped at <paramref name="maxMatches"/>.
    /// </summary>
    private static IReadOnlyList<string> FindMatchingCountryNames(string query, int maxMatches)
    {
        return KnownCountryNames.Value
            .Where(name => name.Contains(query, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(name => string.Equals(name, query, StringComparison.OrdinalIgnoreCase))
            .ThenBy(name => name.Length)
            .Take(maxMatches)
            .ToList();
    }

    private static string? ResolveCountryDisplayName(string? isoCode)
    {
        if (string.IsNullOrWhiteSpace(isoCode))
            return null;

        try
        {
            return new RegionInfo(isoCode.Trim().ToUpperInvariant()).EnglishName;
        }
        catch (ArgumentException)
        {
            // Unknown/unsupported region code — fall back to the raw provider value elsewhere.
            return null;
        }
    }

    /// <summary>
    /// Builds the set of known country display names from .NET's built-in culture/region data
    /// (<see cref="RegionInfo"/>) — a static, framework-maintained reference table, not invented
    /// or provider data. Used only to decide which country names are worth a follow-up
    /// <c>/geoname</c> lookup; the resulting coordinates always come from OpenTripMap.
    /// </summary>
    private static IReadOnlyList<string> BuildKnownCountryNames()
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var culture in CultureInfo.GetCultures(CultureTypes.SpecificCultures))
        {
            try
            {
                names.Add(new RegionInfo(culture.Name).EnglishName);
            }
            catch (ArgumentException)
            {
                // Some specific cultures (e.g. custom/synthetic ones) have no associated region.
            }
        }

        return names.ToList();
    }
}
