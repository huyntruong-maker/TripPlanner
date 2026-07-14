using Application.Dtos.Destinations;

namespace Application.Interfaces.Providers;

/// <summary>Fetches attractions near a coordinate, ranked by popularity/rating (max 20 per page).</summary>
public interface IDestinationProvider
{
    /// <summary>Retrieves a page of attractions near the given coordinates; never null.</summary>
    Task<AttractionSearchResultDto> GetAttractionsAsync(
        double latitude,
        double longitude,
        int radiusMeters = 20_000,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    /// <summary>Fetches full attraction detail by provider-specific ID; null if not recognised.</summary>
    Task<AttractionDto?> GetAttractionDetailAsync(
        string providerPlaceId,
        CancellationToken cancellationToken = default);
}
