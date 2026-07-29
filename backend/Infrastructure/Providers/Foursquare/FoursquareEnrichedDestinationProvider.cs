using Application.Dtos.Destinations;
using Application.Interfaces.Providers;
using Domain.Constants;
using Infrastructure.Providers.OpenTripMap;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Providers.Foursquare;

/// <summary>
/// Runs whenever Providers:Destination:Provider is "OpenTripMap" (default) or unset. Wraps
/// OpenTripMapDestinationProvider and enriches its results using FoursquareDestinationProvider
/// (category/rating/photos/hours), matched by name + coordinates on every call rather than
/// persisting the match (OpenTripMap xid stays the public ProviderPlaceId). See
/// FoursquareDestinationProvider for its other, standalone use.
/// </summary>
public class FoursquareEnrichedDestinationProvider(
    OpenTripMapDestinationProvider openTripMap,
    FoursquareDestinationProvider foursquare,
    IConfiguration configuration,
    ILogger<FoursquareEnrichedDestinationProvider> logger) : IDestinationProvider
{
    // Logged once for the app's lifetime (not per request) when no API key is configured.
    private static int _hasLoggedDisabled;

    private bool IsEnabled
    {
        get
        {
            var hasApiKey = !string.IsNullOrWhiteSpace(configuration.GetSection(ConfigKeys.Providers.Foursquare.ApiKey).Value);
            var enrichmentEnabled = configuration.GetSection(ConfigKeys.Providers.Foursquare.EnableEnrichment).Get<bool?>() ?? true;
            return hasApiKey && enrichmentEnabled;
        }
    }

    public async Task<AttractionSearchResultDto> GetAttractionsAsync(
        double latitude,
        double longitude,
        int radiusMeters = 20_000,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await openTripMap.GetAttractionsAsync(latitude, longitude, radiusMeters, page, pageSize, cancellationToken);

        if (!IsEnabled)
        {
            LogDisabledOnce();
            return result;
        }

        if (result.Items.Count == 0)
            return result;

        // Concurrent, best-effort, per-item enrichment; NFR-2 trade-off accepted since CachedDestinationProvider's 30-min list TTL keeps repeat-query p95 within budget without per-call timeout support.
        var tasks = result.Items.Select(attraction => EnrichListItemAsync(attraction, cancellationToken));
        await Task.WhenAll(tasks);

        return result;
    }

    public async Task<AttractionDto?> GetAttractionDetailAsync(
        string providerPlaceId,
        CancellationToken cancellationToken = default)
    {
        var detail = await openTripMap.GetAttractionDetailAsync(providerPlaceId, cancellationToken);
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

    /// <summary>Rating always comes from Foursquare's 0-10 score (never OpenTripMap's 1-7 class); Foursquare's photo is preferred as the thumbnail, falling back to OpenTripMap's Wikimedia thumbnail when absent.</summary>
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
