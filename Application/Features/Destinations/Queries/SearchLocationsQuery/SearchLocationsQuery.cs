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
        var items = await geocodingProvider.SearchLocationsAsync(
            request.Query,
            request.MaxResults,
            cancellationToken);

        return new LocationSearchResultDto
        {
            Items = items,
            TotalCount = items.Count
        };
    }
}
