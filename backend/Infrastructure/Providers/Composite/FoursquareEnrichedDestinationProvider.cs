using Application.Dtos.Destinations;
using Application.Interfaces.Providers;
using Domain.Constants;
using Infrastructure.Providers.Foursquare;
using Infrastructure.Providers.OpenTripMap;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Providers.Composite;

/// <summary>
/// Composite <see cref="IDestinationProvider"/>: OpenTripMap is the primary source of
/// points-of-interest near a city; Foursquare enriches each result with categories and
/// rating/popularity when a confident name+proximity match is found (F1-US3 note: "call
/// OpenTripMap for POIs near the city center, call Foursquare to enrich with categories and
/// reviews/rating").
/// </summary>
/// <remarks>
/// Enrichment degrades gracefully and never fails the request: if the Foursquare API key is not
/// configured, the Foursquare call errors, or it does not complete within
/// <see cref="EnrichmentTimeout"/>, the unenriched OpenTripMap results are returned and a warning
/// is logged. Foursquare is queried once per area (a single nearby search, not one call per POI)
/// and matched in memory to respect NFR-2 (attractions ≤ 1000 ms p95).
/// <para>
/// Detail lookups always delegate to OpenTripMap because every provider place ID surfaced by
/// <see cref="GetAttractionsAsync"/> is an OpenTripMap xid.
/// </para>
/// </remarks>
public class FoursquareEnrichedDestinationProvider(
    OpenTripMapDestinationProvider primaryProvider,
    FoursquareDestinationProvider enrichmentProvider,
    IConfiguration configuration,
    ILogger<FoursquareEnrichedDestinationProvider> logger) : IDestinationProvider
{
    // NFR-2's budget is 1000 ms p95 for the whole call; enrichment is optional, so it gets a
    // bounded slice and can never push a slow Foursquare response over that budget.
    private static readonly TimeSpan EnrichmentTimeout = TimeSpan.FromMilliseconds(700);

    // Two POIs farther apart than this are treated as different places even if their names
    // match (e.g. two same-named venues in different neighbourhoods).
    private const double MaxMatchDistanceMeters = 250;

    private const double EarthRadiusMeters = 6_371_000;

    public async Task<AttractionSearchResultDto> GetAttractionsAsync(
        double latitude,
        double longitude,
        int radiusMeters = 20_000,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var primaryResult = await primaryProvider.GetAttractionsAsync(
            latitude, longitude, radiusMeters, page, pageSize, cancellationToken);

        if (primaryResult.IsEmpty || !IsFoursquareConfigured())
            return primaryResult;

        var enrichmentCandidates = await TryFetchEnrichmentCandidatesAsync(
            latitude, longitude, radiusMeters, pageSize, cancellationToken);

        if (enrichmentCandidates.Count == 0)
            return primaryResult;

        foreach (var attraction in primaryResult.Items)
            Enrich(attraction, enrichmentCandidates);

        return primaryResult;
    }

    public Task<AttractionDto?> GetAttractionDetailAsync(
        string providerPlaceId,
        CancellationToken cancellationToken = default)
        => primaryProvider.GetAttractionDetailAsync(providerPlaceId, cancellationToken);

    private bool IsFoursquareConfigured()
    {
        var apiKey = configuration.GetSection(ConfigKeys.Providers.Foursquare.ApiKey).Value;
        return !string.IsNullOrWhiteSpace(apiKey);
    }

    private async Task<IReadOnlyList<AttractionDto>> TryFetchEnrichmentCandidatesAsync(
        double latitude,
        double longitude,
        int radiusMeters,
        int pageSize,
        CancellationToken cancellationToken)
    {
        Task<AttractionSearchResultDto> enrichmentTask;
        try
        {
            enrichmentTask = enrichmentProvider.GetAttractionsAsync(
                latitude, longitude, radiusMeters, page: 1, pageSize, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning("[Foursquare] Enrichment call could not start, returning unenriched results: {Ex}", ex.Message);
            return [];
        }

        var timeoutTask = Task.Delay(EnrichmentTimeout, cancellationToken);
        var completed = await Task.WhenAny(enrichmentTask, timeoutTask);

        if (completed == timeoutTask)
        {
            logger.LogWarning(
                "[Foursquare] Enrichment skipped: exceeded {TimeoutMs} ms budget, returning unenriched results",
                EnrichmentTimeout.TotalMilliseconds);

            // Best-effort: observe the still-running call's outcome so a later fault doesn't
            // surface as an unobserved task exception; its result is discarded either way.
            _ = enrichmentTask.ContinueWith(
                t => logger.LogWarning("[Foursquare] Late enrichment response ignored (timed out): {Ex}", t.Exception?.GetBaseException().Message),
                TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously);

            return [];
        }

        try
        {
            var result = await enrichmentTask;
            return result.Items;
        }
        catch (Exception ex)
        {
            // Enrichment is best-effort; any failure (network, auth, parsing) degrades to the
            // unenriched OpenTripMap result rather than failing the attractions request.
            logger.LogWarning("[Foursquare] Enrichment failed, returning unenriched results: {Ex}", ex.Message);
            return [];
        }
    }

    private static void Enrich(AttractionDto attraction, IReadOnlyList<AttractionDto> candidates)
    {
        var match = FindBestMatch(attraction, candidates);
        if (match is null)
            return;

        attraction.Category ??= match.Category;
        attraction.Rating ??= match.Rating;
        attraction.ThumbnailUrl ??= match.ThumbnailUrl;
        attraction.Tags = MergeTags(attraction.Tags, match.Tags);
    }

    private static AttractionDto? FindBestMatch(AttractionDto attraction, IReadOnlyList<AttractionDto> candidates)
    {
        var normalizedTarget = NormalizeName(attraction.Name);

        return candidates
            .Select(candidate => (candidate, distance: DistanceMeters(attraction.Latitude, attraction.Longitude, candidate.Latitude, candidate.Longitude)))
            .Where(pair => NormalizeName(pair.candidate.Name) == normalizedTarget && pair.distance <= MaxMatchDistanceMeters)
            .OrderBy(pair => pair.distance)
            .Select(pair => pair.candidate)
            .FirstOrDefault();
    }

    private static string NormalizeName(string name) =>
        new([.. name.Trim().ToLowerInvariant().Where(char.IsLetterOrDigit)]);

    private static IReadOnlyList<string> MergeTags(IReadOnlyList<string> primaryTags, IReadOnlyList<string> enrichmentTags)
    {
        var merged = new List<string>(primaryTags);

        foreach (var tag in enrichmentTags)
        {
            if (!merged.Any(existing => string.Equals(existing, tag, StringComparison.OrdinalIgnoreCase)))
                merged.Add(tag);
        }

        return merged;
    }

    private static double DistanceMeters(double lat1, double lon1, double lat2, double lon2)
    {
        var dLat = DegreesToRadians(lat2 - lat1);
        var dLon = DegreesToRadians(lon2 - lon1);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                + Math.Cos(DegreesToRadians(lat1)) * Math.Cos(DegreesToRadians(lat2))
                * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return EarthRadiusMeters * c;
    }

    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180;
}
