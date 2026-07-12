using Application.Dtos.Destinations;

namespace Application.Interfaces.Providers;

/// <summary>
/// Geocodes a free-text location query (city, country, region) into a ranked list
/// of matching locations with coordinates. Implementations must deduplicate results,
/// rank exact matches first, and apply case-insensitive partial matching.
/// </summary>
public interface IGeocodingProvider
{
    /// <summary>
    /// Searches for cities or countries matching <paramref name="query"/>.
    /// </summary>
    /// <param name="query">Partial or full city/country name (at least 1 character).</param>
    /// <param name="maxResults">Upper bound on the number of results returned (default: 5).</param>
    /// <param name="cancellationToken">Propagates cancellation to the underlying HTTP call.</param>
    /// <returns>
    /// An ordered list of <see cref="LocationDto"/> ranked by relevance; exact matches first.
    /// Returns an empty list — never null — when no results are found.
    /// </returns>
    Task<IReadOnlyList<LocationDto>> SearchLocationsAsync(
        string query,
        int maxResults = 5,
        CancellationToken cancellationToken = default);
}
