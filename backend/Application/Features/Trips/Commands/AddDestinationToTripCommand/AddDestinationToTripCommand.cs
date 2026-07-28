using Application.Dtos.Trips;
using Application.Features.Trips.Shared;
using Application.Interfaces.Cqrs;
using Application.Interfaces.DataAccess;
using Domain.Entities;
using Domain.Messages;
using MediatR;

namespace Application.Features.Trips.Commands.AddDestinationToTripCommand;

public record AddDestinationToTripCommand : ICommand<(string, TripDestinationDto?)>
{
    public required Guid TripId { get; init; }

    public required Guid UserId { get; init; }

    /// <summary>Null means the destination is unscheduled ("Saved Places").</summary>
    public Guid? ItineraryDayId { get; init; }

    public required string ProviderPlaceId { get; init; }

    public required string Name { get; init; }

    public string? Category { get; init; }

    public string? ThumbnailUrl { get; init; }

    public double Lat { get; init; }

    public double Lng { get; init; }
}

public class AddDestinationToTripCommandHandler(IWriteUnitOfWork writeUnitOfWork)
    : IRequestHandler<AddDestinationToTripCommand, (string, TripDestinationDto?)>
{
    public async Task<(string, TripDestinationDto?)> Handle(AddDestinationToTripCommand request, CancellationToken cancellationToken)
    {
        var tripRepo = writeUnitOfWork.GetRepository<Trip>();

        // Verify the trip exists and belongs to the caller (NFR-6).
        var tripExists = await tripRepo.Any(t => t.Id == request.TripId && t.UserId == request.UserId);
        if (!tripExists)
            return (TripControllerMsg.NotFound, null);

        var destinationRepo = writeUnitOfWork.GetRepository<TripDestination>();

        // Null targets Saved Places; otherwise verify the day belongs to this trip (prevents cross-trip injection).
        if (request.ItineraryDayId is not null)
        {
            var dayRepo = writeUnitOfWork.GetRepository<ItineraryDay>();
            var dayExists = await dayRepo.Any(d => d.Id == request.ItineraryDayId && d.TripId == request.TripId);
            if (!dayExists)
                return (TripControllerMsg.AddDestination.ItineraryDayNotFound, null);

            // Duplicate rule: the same place cannot be added twice to the same day (F3-US3/US4).
            var duplicateExists = await destinationRepo.Any(d =>
                d.TripId == request.TripId &&
                d.ItineraryDayId == request.ItineraryDayId &&
                d.ProviderPlaceId == request.ProviderPlaceId);
            if (duplicateExists)
                return (TripControllerMsg.AddDestination.DuplicateInDay, null);
        }

        var existingInBucket = await destinationRepo.QueryCondition(
            d => d.TripId == request.TripId && d.ItineraryDayId == request.ItineraryDayId);
        var nextPosition = existingInBucket.Any() ? existingInBucket.Max(d => d.Position) + 1 : 1;

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

        return (string.Empty, TripDtoMapper.ToDestinationDto(destination));
    }
}
