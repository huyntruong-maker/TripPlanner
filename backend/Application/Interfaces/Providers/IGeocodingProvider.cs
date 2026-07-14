using Application.Dtos.Destinations;

namespace Application.Interfaces.Providers;

/// <summary>Geocodes a free-text city/country query into a ranked, deduplicated list of locations.</summary>
public interface IGeocodingProvider
{
    /// <summary>Searches for matching cities/countries; never null, empty list if none found.</summary>
    Task<IReadOnlyList<LocationDto>> SearchLocationsAsync(
        string query,
        int maxResults = 5,
        CancellationToken cancellationToken = default);
}
