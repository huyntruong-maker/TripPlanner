using Application.Common.Utils;
using Application.Dtos.Destinations;
using Application.Interfaces.Cqrs;
using Application.Interfaces.Providers;
using MediatR;

namespace Application.Features.Destinations.Queries.SearchLocationsQuery;

/// <summary>
/// Returns up to <see cref="MaxResults"/> city/country results that match the free-text
/// <see cref="Query"/>. Results are ranked with exact matches first; partial and
/// case-insensitive matches are included. Empty query returns an empty list.
/// </summary>
public record SearchLocationsQuery : IQuery<LocationSearchResultDto>
{
    public required string Query { get; init; }

    public int MaxResults { get; init; } = 5;
}

public class SearchLocationsQueryHandler(IGeocodingProvider geocodingProvider)
    : IRequestHandler<SearchLocationsQuery, LocationSearchResultDto>
{
    public async Task<LocationSearchResultDto> Handle(
        SearchLocationsQuery request,
        CancellationToken cancellationToken)
    {
        var candidates = await geocodingProvider.SearchLocationsAsync(
            request.Query,
            request.MaxResults,
            cancellationToken);

        // The provider may return unranked/duplicate/over-the-limit candidates (see
        // IGeocodingProvider remarks) — dedup, exact-first ranking, and clamping is applied here
        // so the business rule (F1-US2: max 5, no duplicates, exact match first) is provider-agnostic.
        var items = LocationResultRanker.Rank(candidates, request.Query, request.MaxResults);

        return new LocationSearchResultDto
        {
            Items = items,
            TotalCount = items.Count
        };
    }
}
