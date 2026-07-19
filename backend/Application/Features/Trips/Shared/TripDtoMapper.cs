using Application.Dtos.Trips;
using Domain.Entities;

namespace Application.Features.Trips.Shared;

/// <summary>Maps a fully-loaded <see cref="Trip"/> (itinerary days + destinations) to its detail DTO.</summary>
public static class TripDtoMapper
{
    /// <summary>Requires trip.ItineraryDays.TripDestinations and the trip-level TripDestinations (Saved Places) to be already loaded/included.</summary>
    public static TripDto ToDetailDto(Trip trip)
    {
        var itineraryDays = trip.ItineraryDays
            .Where(d => !d.IsDeleted)
            .OrderBy(d => d.DayIndex)
            .Select(d => new ItineraryDayDto
            {
                Id = d.Id,
                Date = d.Date,
                DayIndex = d.DayIndex,
                TripDestinations = d.TripDestinations
                    .Where(td => !td.IsDeleted)
                    .OrderBy(td => td.Position)
                    .Select(ToDestinationDto)
                    .ToList()
            })
            .ToList();

        var savedPlaces = trip.TripDestinations
            .Where(td => !td.IsDeleted && td.ItineraryDayId == null)
            .OrderBy(td => td.Position)
            .Select(ToDestinationDto)
            .ToList();

        return new TripDto
        {
            Id = trip.Id,
            Name = trip.Name,
            StartDate = trip.StartDate,
            EndDate = trip.EndDate,
            CreatedAt = trip.CreatedAt,
            UpdatedAt = trip.UpdatedAt,
            ItineraryDays = itineraryDays,
            SavedPlaces = savedPlaces
        };
    }

    public static TripDestinationDto ToDestinationDto(TripDestination destination) => new()
    {
        Id = destination.Id,
        TripId = destination.TripId,
        ItineraryDayId = destination.ItineraryDayId,
        ProviderPlaceId = destination.ProviderPlaceId,
        Name = destination.Name,
        Category = destination.Category,
        ThumbnailUrl = destination.ThumbnailUrl,
        Lat = destination.Lat,
        Lng = destination.Lng,
        Position = destination.Position
    };
}
