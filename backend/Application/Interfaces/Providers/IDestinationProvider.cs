using Application.Dtos.Destinations;

namespace Application.Interfaces.Providers;

/// <summary>
/// Fetches points-of-interest (attractions) near a geographic coordinate.
/// Implementations must return results ranked by provider popularity/rating and
/// respect the configured page size ceiling (max 20 items per call).
/// </summary>
public interface IDestinationProvider
{
    /// <summary>
    /// Retrieves a page of attractions near the given coordinates.
    /// </summary>
    /// <param name="latitude">Latitude of the search centre.</param>
    /// <param name="longitude">Longitude of the search centre.</param>
    /// <param name="radiusMeters">Search radius in metres (default: 20 000 m = 20 km for a city).</param>
    /// <param name="page">1-based page index.</param>
    /// <param name="pageSize">Maximum number of items to return per page (capped at 20).</param>
    /// <param name="cancellationToken">Propagates cancellation to the underlying HTTP call.</param>
    /// <returns>
    /// A <see cref="AttractionSearchResultDto"/> containing the ranked page of attractions.
    /// Returns an empty result — never null — when no attractions are found.
    /// </returns>
    Task<AttractionSearchResultDto> GetAttractionsAsync(
        double latitude,
        double longitude,
        int radiusMeters = 20_000,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches the full detail record for a single attraction by its provider-specific ID.
    /// </summary>
    /// <param name="providerPlaceId">The provider-specific place identifier (e.g. OpenTripMap xid).</param>
    /// <param name="cancellationToken">Propagates cancellation to the underlying HTTP call.</param>
    /// <returns>
    /// An <see cref="AttractionDto"/> with all available fields populated,
    /// or <c>null</c> when the provider does not recognise the ID.
    /// </returns>
    Task<AttractionDto?> GetAttractionDetailAsync(
        string providerPlaceId,
        CancellationToken cancellationToken = default);
}
