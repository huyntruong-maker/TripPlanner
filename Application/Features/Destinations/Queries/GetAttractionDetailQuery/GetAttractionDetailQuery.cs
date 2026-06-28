using Application.Dtos.Destinations;
using Application.Interfaces.Cqrs;
using Application.Interfaces.Providers;
using MediatR;

namespace Application.Features.Destinations.Queries.GetAttractionDetailQuery;

/// <summary>
/// Returns full detail for a single attraction identified by its provider-specific ID.
/// Returns <c>null</c> when the provider does not recognise the ID (results in a 404 response).
/// Optional fields (description, photos, address, website, openingHours) may be null or empty
/// when the provider does not supply them — graceful partial data (F2-US1 business rule).
/// NFR-3: response within 2 s; the caching decorator handles repeated requests at 24-hour TTL.
/// </summary>
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
