using Application.Dtos.Trips;
using Application.Interfaces.Cqrs;
using Application.Interfaces.DataAccess;
using Domain.Entities;
using MediatR;

namespace Application.Features.Trips.Commands.CreateTripCommand;

/// <summary>Dates are set in a separate step (F3-US2).</summary>
public record CreateTripCommand : ICommand<(string, TripDto)>
{
    public required string Name { get; init; }

    /// <summary>Set from JWT claims in the controller, never from the request body.</summary>
    public required Guid UserId { get; init; }
}

public class CreateTripCommandHandler(IWriteUnitOfWork writeUnitOfWork)
    : IRequestHandler<CreateTripCommand, (string, TripDto)>
{
    public async Task<(string, TripDto)> Handle(CreateTripCommand request, CancellationToken cancellationToken)
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

        var tripDto = new TripDto
        {
            Id = trip.Id,
            Name = trip.Name,
            StartDate = trip.StartDate,
            EndDate = trip.EndDate,
            CreatedAt = trip.CreatedAt,
            UpdatedAt = trip.UpdatedAt
        };

        return (string.Empty, tripDto);
    }
}
