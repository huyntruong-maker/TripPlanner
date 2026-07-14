using Application.Dtos.Trips;
using Application.Interfaces.Cqrs;
using Application.Interfaces.DataAccess;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Trips.Queries.GetTripDetailQuery;

/// <summary>Full trip detail with ordered days and destinations; null if not found or not owned (NFR-6).</summary>
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
                    .ThenInclude(d => d.TripDestinations.Where(td => !td.IsDeleted)));

        if (trip is null)
            return null;

        var itineraryDays = trip.ItineraryDays
            .OrderBy(d => d.DayIndex)
            .Select(d => new ItineraryDayDto
            {
                Id = d.Id,
                Date = d.Date,
                DayIndex = d.DayIndex,
                TripDestinations = d.TripDestinations
                    .OrderBy(td => td.Position)
                    .Select(td => new TripDestinationDto
                    {
                        Id = td.Id,
                        TripId = td.TripId,
                        ItineraryDayId = td.ItineraryDayId,
                        ProviderPlaceId = td.ProviderPlaceId,
                        Name = td.Name,
                        Category = td.Category,
                        ThumbnailUrl = td.ThumbnailUrl,
                        Lat = td.Lat,
                        Lng = td.Lng,
                        Position = td.Position
                    })
                    .ToList()
            })
            .ToList();

        return new TripDto
        {
            Id = trip.Id,
            Name = trip.Name,
            StartDate = trip.StartDate,
            EndDate = trip.EndDate,
            CreatedAt = trip.CreatedAt,
            UpdatedAt = trip.UpdatedAt,
            ItineraryDays = itineraryDays
        };
    }
}
