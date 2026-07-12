using Application.Dtos.Destinations;

namespace Application.Common.Utils;

/// <summary>
/// Provider-agnostic ranking, deduplication, and clamping for geocoding candidates (F1-US2).
/// Kept separate from any specific <c>IGeocodingProvider</c> implementation because some providers
/// combine several underlying lookups to build the candidate set (see
/// <c>OpenTripMapGeocodingProvider</c>) and can return unranked, duplicate, or over-the-limit results.
/// </summary>
public static class LocationResultRanker
{
    /// <summary>
    /// Deduplicates by name+country (case-insensitive), ranks exact-name matches first, then
    /// alphabetically, and clamps to <paramref name="maxResults"/> (itself clamped to [1, 5] per
    /// the F1-US2 business rule).
    /// </summary>
    public static IReadOnlyList<LocationDto> Rank(
        IEnumerable<LocationDto> candidates,
        string query,
        int maxResults)
    {
        var trimmedQuery = query.Trim();
        var clampedMaxResults = Math.Clamp(maxResults, 1, 5);

        var deduplicated = candidates
            .GroupBy(
                location => (
                    Name: location.Name.Trim().ToLowerInvariant(),
                    Country: location.Country?.Trim().ToLowerInvariant() ?? string.Empty),
                (_, group) => group.First())
            .ToList();

        return deduplicated
            .OrderByDescending(location => string.Equals(location.Name, trimmedQuery, StringComparison.OrdinalIgnoreCase))
            .ThenBy(location => location.Name, StringComparer.OrdinalIgnoreCase)
            .Take(clampedMaxResults)
            .ToList();
    }
}
