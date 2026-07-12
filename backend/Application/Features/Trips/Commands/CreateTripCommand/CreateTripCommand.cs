using Application.Dtos.Trips;
using Application.Interfaces.Cqrs;
using Application.Interfaces.DataAccess;
using Domain.Entities;
using MediatR;

namespace Application.Features.Trips.Commands.CreateTripCommand;

/// <summary>
/// Creates a new trip owned by the authenticated user.
/// The trip is created without dates — the user sets dates in a separate step (F3-US2).
/// </summary>
public record CreateTripCommand : ICommand<TripDto>
{
    public required string Name { get; init; }

    /// <summary>The authenticated user's ID — set from JWT claims in the controller, never from the request body.</summary>
    public required Guid UserId { get; init; }
}

public class CreateTripCommandHandler(IWriteUnitOfWork writeUnitOfWork)
    : IRequestHandler<CreateTripCommand, TripDto>
{
    public async Task<TripDto> Handle(CreateTripCommand request, CancellationToken cancellationToken)
    {
        var trip = new Trip
        {
            Name = request.Name,
            UserId = request.UserId,
            CreatedBy = request.UserId,
            UpdatedBy = request.UserId
        };

        await writeUnitOfWork.GetRepository<Trip>().Add(trip);
        await writeUnitOfWork.SaveChanges();

        return new TripDto
        {
            Id = trip.Id,
            Name = trip.Name,
            StartDate = trip.StartDate,
            EndDate = trip.EndDate,
            CreatedAt = trip.CreatedAt,
            UpdatedAt = trip.UpdatedAt
        };
    }
}
