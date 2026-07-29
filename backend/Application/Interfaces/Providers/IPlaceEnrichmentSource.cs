using Application.Dtos.Destinations;

namespace Application.Interfaces.Providers;

/// <summary>Finds a single place matching a name + coordinates, to enrich an attraction from another source. Returns null when no match is found (not an error).</summary>
public interface IPlaceEnrichmentSource
{
    Task<AttractionDto?> FindNearestMatchAsync(
        string name,
        double latitude,
        double longitude,
        CancellationToken cancellationToken = default);
}
