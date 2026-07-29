using Application.Dtos.Destinations;
using Application.Interfaces.Cqrs;
using Application.Interfaces.Providers;
using MediatR;

namespace Application.Features.Destinations.Queries.SearchLocationsQuery;

/// <summary>Exact-name matches are ranked first by the provider.</summary>
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
