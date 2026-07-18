using Application.Dtos.Trips;
using Application.Features.Trips.Shared;
using Application.Interfaces.Cqrs;
using Application.Interfaces.DataAccess;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Trips.Queries.GetTripDetailQuery;

/// <summary>Full trip detail with ordered days, destinations, and Saved Places; null if not found or not owned (NFR-6).</summary>
public record GetTripDetailQuery : IQuery<TripDto?>
{
    public required Guid TripId { get; init; }

    public required Guid UserId { get; init; }
}

public class GetTripDetailQueryHandler(IReadUnitOfWork readUnitOfWork)
    : IRequestHandler<GetTripDetailQuery, TripDto?>
{
    public async Task<TripDto?> Handle(GetTripDetailQuery request, CancellationToken cancellationToken)
    {
        var trip = await readUnitOfWork.GetRepository<Trip>().Single(
            predicate: t => t.Id == request.TripId && t.UserId == request.UserId,
            include: q => q
                .Include(t => t.ItineraryDays.Where(d => !d.IsDeleted))
                    .ThenInclude(d => d.TripDestinations.Where(td => !td.IsDeleted))
                .Include(t => t.TripDestinations.Where(td => !td.IsDeleted && td.ItineraryDayId == null)));

        return trip is null ? null : TripDtoMapper.ToDetailDto(trip);
    }
}
