using Application.Dtos.Trips;
using Application.Interfaces.Cqrs;
using Application.Interfaces.DataAccess;
using Domain.Entities;
using Domain.Messages;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Trips.Commands.SetTripDatesCommand;

/// <summary>Shortening soft-deletes days and unschedules their destinations (SetNull cascade) rather than deleting them.</summary>
public record SetTripDatesCommand : ICommand<(string, SetTripDatesResult?)>
{
    public required Guid TripId { get; init; }

    public required Guid UserId { get; init; }

    public required DateOnly StartDate { get; init; }

    public required DateOnly EndDate { get; init; }
}

public class SetTripDatesResult
{
    public required TripDto Trip { get; init; }

    public int DestinationsUnscheduledCount { get; init; }
}

public class SetTripDatesCommandHandler(IWriteUnitOfWork writeUnitOfWork)
    : IRequestHandler<SetTripDatesCommand, (string, SetTripDatesResult?)>
{
    public async Task<(string, SetTripDatesResult?)> Handle(SetTripDatesCommand request, CancellationToken cancellationToken)
    {
        var tripRepo = writeUnitOfWork.GetRepository<Trip>();
        var dayRepo = writeUnitOfWork.GetRepository<ItineraryDay>();

        // Ownership is verified via the UserId filter here (NFR-6).
        var trip = await tripRepo.Single(
            predicate: t => t.Id == request.TripId && t.UserId == request.UserId,
            include: q => q.Include(t => t.ItineraryDays));

        if (trip is null)
            return (TripControllerMsg.NotFound, null);

        var newDates = EnumerateDates(request.StartDate, request.EndDate).ToList();

        var existingDays = trip.ItineraryDays
            .Where(d => !d.IsDeleted)
            .OrderBy(d => d.DayIndex)
            .ToList();

        var existingDates = existingDays.Select(d => d.Date).ToHashSet();
        var newDatesSet = newDates.ToHashSet();

        var daysToRemove = existingDays.Where(d => !newDatesSet.Contains(d.Date)).ToList();

        // Must count before the soft-delete below runs, since that cascades to unschedule these destinations.
        var destinationRepo = writeUnitOfWork.GetRepository<TripDestination>();
        var dayIdsToRemove = daysToRemove.Select(d => d.Id).ToHashSet();
        int unscheduledCount = 0;

        if (dayIdsToRemove.Count > 0)
        {
            var allDestinations = await destinationRepo.QueryCondition(
                d => d.TripId == request.TripId && d.ItineraryDayId != null);
            unscheduledCount = allDestinations
                .Count(d => d.ItineraryDayId.HasValue && dayIdsToRemove.Contains(d.ItineraryDayId.Value));
        }

        foreach (var day in daysToRemove)
        {
            day.IsDeleted = true;
            day.UpdatedAt = DateTimeOffset.UtcNow;
            day.UpdatedBy = request.UserId;
            await dayRepo.Update(day);
        }

        var datesToAdd = newDates.Where(date => !existingDates.Contains(date)).ToList();
        var newDayEntities = new List<ItineraryDay>();

        foreach (var (date, index) in newDates.Select((d, i) => (d, i + 1)))
        {
            if (datesToAdd.Contains(date))
            {
                var newDay = new ItineraryDay
                {
                    TripId = request.TripId,
                    Date = date,
                    DayIndex = index,
                    CreatedBy = request.UserId,
                    UpdatedBy = request.UserId
                };
                newDayEntities.Add(newDay);
                await dayRepo.Add(newDay);
            }
            else
            {
                var existingDay = existingDays.First(d => d.Date == date);
                if (existingDay.DayIndex != index)
                {
                    existingDay.DayIndex = index;
                    existingDay.UpdatedAt = DateTimeOffset.UtcNow;
                    existingDay.UpdatedBy = request.UserId;
                    await dayRepo.Update(existingDay);
                }
            }
        }

        trip.StartDate = request.StartDate;
        trip.EndDate = request.EndDate;
        trip.UpdatedAt = DateTimeOffset.UtcNow;
        trip.UpdatedBy = request.UserId;
        await tripRepo.Update(trip);

        await writeUnitOfWork.SaveChanges();

        // Re-query rather than reuse tracked entities so newly generated IDs are included.
        var updatedDays = (await dayRepo.QueryCondition(d => d.TripId == request.TripId))
            .OrderBy(d => d.DayIndex)
            .Select(d => new ItineraryDayDto
            {
                Id = d.Id,
                Date = d.Date,
                DayIndex = d.DayIndex
            })
            .ToList();

        var tripDto = new TripDto
        {
            Id = trip.Id,
            Name = trip.Name,
            StartDate = trip.StartDate,
            EndDate = trip.EndDate,
            CreatedAt = trip.CreatedAt,
            UpdatedAt = trip.UpdatedAt,
            ItineraryDays = updatedDays
        };

        var setDatesResult = new SetTripDatesResult
        {
            Trip = tripDto,
            DestinationsUnscheduledCount = unscheduledCount
        };

        return (string.Empty, setDatesResult);
    }

    private static IEnumerable<DateOnly> EnumerateDates(DateOnly start, DateOnly end)
    {
        for (var date = start; date <= end; date = date.AddDays(1))
            yield return date;
    }
}
