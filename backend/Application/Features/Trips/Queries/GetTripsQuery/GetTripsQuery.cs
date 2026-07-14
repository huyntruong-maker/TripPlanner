using Application.Dtos.Trips;
using Application.Interfaces.Cqrs;
using Application.Interfaces.DataAccess;
using Domain.Entities;
using MediatR;

namespace Application.Features.Trips.Queries.GetTripsQuery;

/// <summary>Lists the authenticated user's trips (top-level fields only); empty list if none.</summary>
public record GetTripsQuery : IQuery<IReadOnlyList<TripDto>>
{
    public required Guid UserId { get; init; }
}

public class GetTripsQueryHandler(IReadUnitOfWork readUnitOfWork)
    : IRequestHandler<GetTripsQuery, IReadOnlyList<TripDto>>
{
    public async Task<IReadOnlyList<TripDto>> Handle(GetTripsQuery request, CancellationToken cancellationToken)
    {
        var trips = await readUnitOfWork.GetRepository<Trip>()
            .QueryCondition(t => t.UserId == request.UserId);

        return trips
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new TripDto
            {
                Id = t.Id,
                Name = t.Name,
                StartDate = t.StartDate,
                EndDate = t.EndDate,
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt
            })
            .ToList();
    }
}
