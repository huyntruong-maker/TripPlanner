using Application.Dtos.Destinations;

namespace Application.Interfaces.Providers;

/// <summary>
/// Geocodes a free-text location query (city, country, region) into a list of matching
/// location candidates with coordinates.
/// </summary>
/// <remarks>
/// Implementations return raw candidates only — they do not need to deduplicate, rank, or clamp
/// the result set. That is a deliberately provider-agnostic responsibility of the caller (see
/// <c>Application.Common.Utils.LocationResultRanker</c>), because some providers (e.g. OpenTripMap's
/// <c>/geoname</c> endpoint) can only return a single best match per HTTP call and must combine
/// several calls to surface multiple candidates.
/// </remarks>
public interface IGeocodingProvider
{
    /// <summary>
    /// Searches for cities or countries matching <paramref name="query"/>.
    /// </summary>
    /// <param name="query">Partial or full city/country name (at least 1 character).</param>
    /// <param name="maxResults">Upper bound the caller intends to keep; providers may use this to
    /// bound the number of underlying lookups they perform, but are not required to return exactly
    /// this many candidates.</param>
    /// <param name="cancellationToken">Propagates cancellation to the underlying HTTP call(s).</param>
    /// <returns>
    /// An unranked, possibly-duplicate list of <see cref="LocationDto"/> candidates.
    /// Returns an empty list — never null — when no results are found.
    /// </returns>
    Task<IReadOnlyList<LocationDto>> SearchLocationsAsync(
        string query,
        int maxResults = 5,
        CancellationToken cancellationToken = default);
}
