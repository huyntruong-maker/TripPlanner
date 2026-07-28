using Application.Dtos.Destinations;
using Application.Interfaces.Cqrs;
using Application.Interfaces.Providers;
using MediatR;

namespace Application.Features.Destinations.Queries.GetAttractionsQuery;

/// <summary>PageSize is capped at 20 by the provider regardless of the value requested here.</summary>
public record GetAttractionsQuery : IQuery<AttractionSearchResultDto>
{
    public required double Latitude { get; init; }

    public required double Longitude { get; init; }

    public int RadiusMeters { get; init; } = 20_000;

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 20;
}

public class GetAttractionsQueryHandler(IDestinationProvider destinationProvider)
    : IRequestHandler<GetAttractionsQuery, AttractionSearchResultDto>
{
    public async Task<AttractionSearchResultDto> Handle(
        GetAttractionsQuery request,
        CancellationToken cancellationToken)
    {
        return await destinationProvider.GetAttractionsAsync(
            request.Latitude,
            request.Longitude,
            request.RadiusMeters,
            request.Page,
            request.PageSize,
            cancellationToken);
    }
}
