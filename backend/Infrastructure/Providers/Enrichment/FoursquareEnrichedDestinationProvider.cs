using Application.Dtos.Destinations;
using Application.Interfaces.Providers;
using Domain.Constants;
using Infrastructure.Providers.Foursquare;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Providers.Enrichment;

/// <summary>
/// Enriches OpenTripMap attractions with Foursquare categories/reviews/photos/hours (PDF requirement:
/// "Call Foursquare to enrich with categories and reviews"). Composes an inner <see cref="IDestinationProvider"/>
/// (OpenTripMap) with <see cref="FoursquareDestinationProvider"/>; the OpenTripMap <c>xid</c> remains the public
/// <c>ProviderPlaceId</c> — Foursquare is matched by name + coordinates on every call rather than persisted.
/// </summary>
public class FoursquareEnrichedDestinationProvider(
    IDestinationProvider inner,
    FoursquareDestinationProvider foursquare,
    IConfiguration configuration,
    ILogger<FoursquareEnrichedDestinationProvider> logger) : IDestinationProvider
{
    // Logged once for the app's lifetime (not per request) when no API key is configured.
    private static int _hasLoggedDisabled;

    private bool IsEnabled =>
        !string.IsNullOrWhiteSpace(configuration.GetSection(ConfigKeys.Providers.Foursquare.ApiKey).Value);

    public async Task<AttractionSearchResultDto> GetAttractionsAsync(
        double latitude,
        double longitude,
        int radiusMeters = 20_000,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await inner.GetAttractionsAsync(latitude, longitude, radiusMeters, page, pageSize, cancellationToken);

        if (!IsEnabled)
        {
            LogDisabledOnce();
            return result;
        }

        if (result.Items.Count == 0)
            return result;

        // Concurrent, best-effort, per-item enrichment. NFR-2 trade-off: IRestfulService has no per-call
        // timeout hook today, so a slow Foursquare response can extend overall latency; we accept this
        // because the outermost CachedDestinationProvider absorbs repeat queries (30-min list TTL), keeping
        // p95 within budget for the common "same area searched again" case rather than adding new timeout
        // infrastructure for this change.
        var tasks = result.Items.Select(attraction => EnrichListItemAsync(attraction, cancellationToken));
        await Task.WhenAll(tasks);

        return result;
    }

    public async Task<AttractionDto?> GetAttractionDetailAsync(
        string providerPlaceId,
        CancellationToken cancellationToken = default)
    {
        var detail = await inner.GetAttractionDetailAsync(providerPlaceId, cancellationToken);
        if (detail is null)
            return null;

        if (!IsEnabled)
        {
            LogDisabledOnce();
            return detail;
        }

        try
        {
            var match = await foursquare.FindNearestMatchAsync(detail.Name, detail.Latitude, detail.Longitude, cancellationToken);
            if (match is not null)
                ApplyDetailEnrichment(detail, match);
        }
        catch (Exception ex)
        {
            logger.LogWarning("[Foursquare enrich] Detail enrichment failed for xid '{Xid}': {Ex}", providerPlaceId, ex.Message);
        }

        return detail;
    }

    private async Task EnrichListItemAsync(AttractionDto attraction, CancellationToken cancellationToken)
    {
        try
        {
            var match = await foursquare.FindNearestMatchAsync(attraction.Name, attraction.Latitude, attraction.Longitude, cancellationToken);
            if (match is not null)
                ApplyListEnrichment(attraction, match);
        }
        catch (Exception ex)
        {
            logger.LogWarning("[Foursquare enrich] List enrichment failed for '{Name}': {Ex}", attraction.Name, ex.Message);
        }
    }

    /// <summary>Rating always comes from Foursquare's 0-10 score (or stays null) — never OpenTripMap's 1-7 class.
    /// Foursquare's photo is preferred as the thumbnail (unique per venue); OpenTripMap's Wikimedia thumbnail,
    /// set later by <c>OpenTripMapDestinationProvider</c>'s own enrichment, is kept as the fallback when
    /// Foursquare has no photo.</summary>
    private static void ApplyListEnrichment(AttractionDto attraction, AttractionDto match)
    {
        attraction.Rating = match.Rating;

        if (!string.IsNullOrWhiteSpace(match.ThumbnailUrl))
            attraction.ThumbnailUrl = match.ThumbnailUrl;

        if (string.IsNullOrWhiteSpace(attraction.Category) && !string.IsNullOrWhiteSpace(match.Category))
            attraction.Category = match.Category;
    }

    private static void ApplyDetailEnrichment(AttractionDto detail, AttractionDto match)
    {
        detail.Rating = match.Rating;

        if (!string.IsNullOrWhiteSpace(match.ThumbnailUrl))
            detail.ThumbnailUrl = match.ThumbnailUrl;

        if (string.IsNullOrWhiteSpace(detail.Category) && !string.IsNullOrWhiteSpace(match.Category))
            detail.Category = match.Category;

        if (detail.OpeningHours is null && match.OpeningHours is not null)
            detail.OpeningHours = match.OpeningHours;

        if (string.IsNullOrWhiteSpace(detail.Website) && !string.IsNullOrWhiteSpace(match.Website))
            detail.Website = match.Website;

        if (string.IsNullOrWhiteSpace(detail.Description) && !string.IsNullOrWhiteSpace(match.Description))
            detail.Description = match.Description;

        // Append unique Foursquare photos after OpenTripMap's own.
        if (match.Photos.Count == 0)
            return;

        var merged = new List<string>(detail.Photos);
        foreach (var photo in match.Photos)
            if (!merged.Contains(photo, StringComparer.OrdinalIgnoreCase))
                merged.Add(photo);

        detail.Photos = merged;
    }

    private void LogDisabledOnce()
    {
        if (Interlocked.Exchange(ref _hasLoggedDisabled, 1) == 0)
            logger.LogInformation("[Foursquare enrich] Providers:Foursquare:ApiKey is not configured; skipping Foursquare enrichment.");
    }
}
