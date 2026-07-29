using Application.Dtos.Destinations;
using Application.Interfaces.Cqrs;
using Application.Interfaces.Providers;
using MediatR;

namespace Application.Features.Destinations.Queries.GetAttractionDetailQuery;

/// <summary>Null when not recognised (results in 404). NFR-3: 24h cache TTL.</summary>
public record GetAttractionDetailQuery : IQuery<DestinationDetailDto?>
{
    public required string ProviderPlaceId { get; init; }
}

public class GetAttractionDetailQueryHandler(IDestinationProvider destinationProvider)
    : IRequestHandler<GetAttractionDetailQuery, DestinationDetailDto?>
{
    public async Task<DestinationDetailDto?> Handle(
        GetAttractionDetailQuery request,
        CancellationToken cancellationToken)
    {
        var attraction = await destinationProvider.GetAttractionDetailAsync(
            request.ProviderPlaceId,
            cancellationToken);

        if (attraction is null)
            return null;

        return new DestinationDetailDto
        {
            ProviderPlaceId = attraction.ProviderPlaceId,
            Name = attraction.Name,
            Category = attraction.Category,
            Tags = attraction.Tags,
            Description = attraction.Description,
            Photos = attraction.Photos,
            Address = attraction.Address,
            Website = attraction.Website,
            OpeningHours = attraction.OpeningHours,
            Rating = attraction.Rating,
            Latitude = attraction.Latitude,
            Longitude = attraction.Longitude
        };
    }
}
