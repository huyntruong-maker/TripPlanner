using Application.Interfaces.Cqrs;
using Application.Interfaces.DataAccess;
using Domain.Entities;
using Domain.Messages;
using MediatR;

namespace Application.Features.Trips.Commands.RemoveDestinationFromTripCommand;

/// <summary>Soft-deletes a destination from a trip owned by the caller (NFR-6); NotFound code if not found.</summary>
public record RemoveDestinationFromTripCommand : ICommand<(string, bool)>
{
    public required Guid TripId { get; init; }

    public required Guid TripDestinationId { get; init; }

    public required Guid UserId { get; init; }
}

public class RemoveDestinationFromTripCommandHandler(IWriteUnitOfWork writeUnitOfWork)
    : IRequestHandler<RemoveDestinationFromTripCommand, (string, bool)>
{
    public async Task<(string, bool)> Handle(RemoveDestinationFromTripCommand request, CancellationToken cancellationToken)
    {
        var tripRepo = writeUnitOfWork.GetRepository<Trip>();

        // Verify trip ownership before touching any destination (NFR-6).
        var tripExists = await tripRepo.Any(t => t.Id == request.TripId && t.UserId == request.UserId);
        if (!tripExists)
            return (TripControllerMsg.NotFound, false);

        var destinationRepo = writeUnitOfWork.GetRepository<TripDestination>();

        // Verify the destination belongs to this trip — prevents cross-trip deletion.
        var destination = await destinationRepo.Single(
            d => d.Id == request.TripDestinationId && d.TripId == request.TripId);

        if (destination is null)
            return (TripControllerMsg.NotFound, false);

        destination.IsDeleted = true;
        destination.UpdatedAt = DateTimeOffset.UtcNow;
        destination.UpdatedBy = request.UserId;
        await destinationRepo.Update(destination);
        await writeUnitOfWork.SaveChanges();

        return (string.Empty, true);
    }
}
