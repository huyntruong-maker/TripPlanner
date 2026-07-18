using Application.Dtos.Trips;
using Application.Features.Trips.Shared;
using Application.Interfaces.Cqrs;
using Application.Interfaces.DataAccess;
using Domain.Entities;
using Domain.Messages;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Trips.Commands.MoveTripDestinationCommand;

/// <summary>Moves/reorders a destination between itinerary days (or Saved Places) and repositions it (F3-US4/US5/US6).</summary>
public record MoveTripDestinationCommand : ICommand<(string, TripDto?)>
{
    public required Guid TripId { get; init; }

    public required Guid TripDestinationId { get; init; }

    public required Guid UserId { get; init; }

    /// <summary>Null moves the destination to "Saved Places".</summary>
    public Guid? ItineraryDayId { get; init; }

    /// <summary>1-based target position within the bucket; null/out-of-range appends at the end.</summary>
    public int? Position { get; init; }
}

public class MoveTripDestinationCommandHandler(IWriteUnitOfWork writeUnitOfWork)
    : IRequestHandler<MoveTripDestinationCommand, (string, TripDto?)>
{
    public async Task<(string, TripDto?)> Handle(MoveTripDestinationCommand request, CancellationToken cancellationToken)
    {
        var tripRepo = writeUnitOfWork.GetRepository<Trip>();

        // Verify the trip exists and belongs to the caller (NFR-6).
        var tripExists = await tripRepo.Any(t => t.Id == request.TripId && t.UserId == request.UserId);
        if (!tripExists)
            return (TripControllerMsg.NotFound, null);

        var destinationRepo = writeUnitOfWork.GetRepository<TripDestination>();

        var destination = await destinationRepo.Single(d => d.Id == request.TripDestinationId && d.TripId == request.TripId);
        if (destination is null)
            return (TripControllerMsg.MoveDestination.DestinationNotFound, null);

        // Verify the target day belongs to this trip — prevents cross-trip injection. Null targets Saved Places.
        if (request.ItineraryDayId is not null)
        {
            var dayRepo = writeUnitOfWork.GetRepository<ItineraryDay>();
            var dayExists = await dayRepo.Any(d => d.Id == request.ItineraryDayId && d.TripId == request.TripId);
            if (!dayExists)
                return (TripControllerMsg.MoveDestination.ItineraryDayNotFound, null);
        }

        var sourceDayId = destination.ItineraryDayId;
        var targetDayId = request.ItineraryDayId;
        var isSameBucket = sourceDayId == targetDayId;

        var sourceBucket = (await destinationRepo.QueryCondition(
                d => d.TripId == request.TripId && d.ItineraryDayId == sourceDayId))
            .OrderBy(d => d.Position)
            .ToList();

        var targetBucket = isSameBucket
            ? sourceBucket
            : (await destinationRepo.QueryCondition(
                    d => d.TripId == request.TripId && d.ItineraryDayId == targetDayId))
                .OrderBy(d => d.Position)
                .ToList();

        // Duplicate rule: the target bucket must not already contain this place (excluding the moved destination itself).
        var hasDuplicate = targetBucket.Any(d =>
            d.Id != destination.Id && d.ProviderPlaceId == destination.ProviderPlaceId);
        if (hasDuplicate)
            return (TripControllerMsg.MoveDestination.DuplicateInDay, null);

        sourceBucket.RemoveAll(d => d.Id == destination.Id);
        if (!isSameBucket)
            targetBucket.RemoveAll(d => d.Id == destination.Id);

        // Null, <1, or > bucket size appends at the end.
        var insertIndex = request.Position.HasValue &&
                           request.Position.Value >= 1 &&
                           request.Position.Value <= targetBucket.Count + 1
            ? request.Position.Value - 1
            : targetBucket.Count;

        targetBucket.Insert(insertIndex, destination);

        // Persist the bucket change explicitly — ReindexBucket below only touches rows whose Position changes,
        // but ItineraryDayId must always be saved when the destination moves between buckets.
        destination.ItineraryDayId = targetDayId;
        destination.UpdatedAt = DateTimeOffset.UtcNow;
        destination.UpdatedBy = request.UserId;
        await destinationRepo.Update(destination);

        await ReindexBucket(destinationRepo, targetBucket, request.UserId);
        if (!isSameBucket)
            await ReindexBucket(destinationRepo, sourceBucket, request.UserId);

        await writeUnitOfWork.SaveChanges();

        var trip = await tripRepo.Single(
            predicate: t => t.Id == request.TripId && t.UserId == request.UserId,
            include: q => q
                .Include(t => t.ItineraryDays.Where(d => !d.IsDeleted))
                    .ThenInclude(d => d.TripDestinations.Where(td => !td.IsDeleted))
                .Include(t => t.TripDestinations.Where(td => !td.IsDeleted && td.ItineraryDayId == null)));

        if (trip is null)
            return (TripControllerMsg.NotFound, null);

        return (string.Empty, TripDtoMapper.ToDetailDto(trip));
    }

    /// <summary>Reassigns contiguous 1-based positions in bucket order and persists changed rows.</summary>
    private static async Task ReindexBucket(
        IBaseWriteRepository<TripDestination> destinationRepo,
        IReadOnlyList<TripDestination> bucket,
        Guid userId)
    {
        for (var index = 0; index < bucket.Count; index++)
        {
            var item = bucket[index];
            var newPosition = index + 1;
            if (item.Position == newPosition)
                continue;

            item.Position = newPosition;
            item.UpdatedAt = DateTimeOffset.UtcNow;
            item.UpdatedBy = userId;
            await destinationRepo.Update(item);
        }
    }
}
