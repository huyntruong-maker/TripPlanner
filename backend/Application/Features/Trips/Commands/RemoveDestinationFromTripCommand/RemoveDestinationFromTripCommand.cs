using Application.Interfaces.Cqrs;
using Application.Interfaces.DataAccess;
using Domain.Entities;
using MediatR;

namespace Application.Features.Trips.Commands.RemoveDestinationFromTripCommand;

/// <summary>
/// Soft-deletes a <see cref="TripDestination"/> from a trip.
/// Both the trip and the destination must belong to the authenticated user — verified via trip ownership (NFR-6).
/// Returns true when successfully removed, false when the trip or destination was not found.
/// </summary>
public record RemoveDestinationFromTripCommand : ICommand<bool>
{
    public required Guid TripId { get; init; }

    public required Guid TripDestinationId { get; init; }

    public required Guid UserId { get; init; }
}

public class RemoveDestinationFromTripCommandHandler(IWriteUnitOfWork writeUnitOfWork)
    : IRequestHandler<RemoveDestinationFromTripCommand, bool>
{
    public async Task<bool> Handle(RemoveDestinationFromTripCommand request, CancellationToken cancellationToken)
    {
        var tripRepo = writeUnitOfWork.GetRepository<Trip>();

        // Verify trip ownership before touching any destination (NFR-6).
        var tripExists = await tripRepo.Any(t => t.Id == request.TripId && t.UserId == request.UserId);
        if (!tripExists)
            return false;

        var destinationRepo = writeUnitOfWork.GetRepository<TripDestination>();

        // Verify the destination belongs to this trip — prevents cross-trip deletion.
        var destination = await destinationRepo.Single(
            d => d.Id == request.TripDestinationId && d.TripId == request.TripId);

        if (destination is null)
            return false;

        destination.IsDeleted = true;
        destination.UpdatedAt = DateTimeOffset.UtcNow;
        destination.UpdatedBy = request.UserId;
        await destinationRepo.Update(destination);
        await writeUnitOfWork.SaveChanges();

        return true;
    }
}
