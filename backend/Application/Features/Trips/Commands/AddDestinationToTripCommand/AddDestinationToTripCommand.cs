using Application.Dtos.Trips;
using Application.Interfaces.Cqrs;
using Application.Interfaces.DataAccess;
using Domain.Entities;
using MediatR;

namespace Application.Features.Trips.Commands.AddDestinationToTripCommand;

/// <summary>
/// Adds a destination to a specific itinerary day within a trip.
/// Because drag-drop scheduling (F3-US4) is out of MVP, the caller must supply
/// <see cref="ItineraryDayId"/> explicitly (the user picks a day from the UI).
/// </summary>
public record AddDestinationToTripCommand : ICommand<TripDestinationDto?>
{
    public required Guid TripId { get; init; }

    public required Guid UserId { get; init; }

    public required Guid ItineraryDayId { get; init; }

    public required string ProviderPlaceId { get; init; }

    public required string Name { get; init; }

    public string? Category { get; init; }

    public string? ThumbnailUrl { get; init; }

    public double Lat { get; init; }

    public double Lng { get; init; }
}

public class AddDestinationToTripCommandHandler(IWriteUnitOfWork writeUnitOfWork)
    : IRequestHandler<AddDestinationToTripCommand, TripDestinationDto?>
{
    public async Task<TripDestinationDto?> Handle(AddDestinationToTripCommand request, CancellationToken cancellationToken)
    {
        var tripRepo = writeUnitOfWork.GetRepository<Trip>();

        // Verify the trip exists and belongs to the caller (NFR-6).
        var tripExists = await tripRepo.Any(t => t.Id == request.TripId && t.UserId == request.UserId);
        if (!tripExists)
            return null;

        // Verify the itinerary day belongs to this trip — prevents cross-trip injection.
        var dayRepo = writeUnitOfWork.GetRepository<ItineraryDay>();
        var dayExists = await dayRepo.Any(d => d.Id == request.ItineraryDayId && d.TripId == request.TripId);
        if (!dayExists)
            return null;

        // Compute the next position within the day.
        var destinationRepo = writeUnitOfWork.GetRepository<TripDestination>();
        var existingInDay = await destinationRepo.QueryCondition(
            d => d.TripId == request.TripId && d.ItineraryDayId == request.ItineraryDayId);
        var nextPosition = existingInDay.Any() ? existingInDay.Max(d => d.Position) + 1 : 1;

        var destination = new TripDestination
        {
            TripId = request.TripId,
            ItineraryDayId = request.ItineraryDayId,
            ProviderPlaceId = request.ProviderPlaceId,
            Name = request.Name,
            Category = request.Category,
            ThumbnailUrl = request.ThumbnailUrl,
            Lat = request.Lat,
            Lng = request.Lng,
            Position = nextPosition,
            CreatedBy = request.UserId,
            UpdatedBy = request.UserId
        };

        await destinationRepo.Add(destination);
        await writeUnitOfWork.SaveChanges();

        return new TripDestinationDto
        {
            Id = destination.Id,
            TripId = destination.TripId,
            ItineraryDayId = destination.ItineraryDayId,
            ProviderPlaceId = destination.ProviderPlaceId,
            Name = destination.Name,
            Category = destination.Category,
            ThumbnailUrl = destination.ThumbnailUrl,
            Lat = destination.Lat,
            Lng = destination.Lng,
            Position = destination.Position
        };
    }
}
