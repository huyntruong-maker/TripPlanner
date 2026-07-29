using Application.Dtos.Destinations;

namespace Application.Interfaces.Providers;

/// <summary>Fetches attractions near a coordinate, ranked by popularity/rating (max 20 per page).</summary>
public interface IDestinationProvider
{
    Task<AttractionSearchResultDto> GetAttractionsAsync(
        double latitude,
        double longitude,
        int radiusMeters = 20_000,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    /// <summary>Null if not recognised (not an error).</summary>
    Task<AttractionDto?> GetAttractionDetailAsync(
        string providerPlaceId,
        CancellationToken cancellationToken = default);
}
