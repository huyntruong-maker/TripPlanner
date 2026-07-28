using Application.Features.Trips.Shared;
using Domain.Entities;
using FluentAssertions;

namespace Tests.Features.Trips;

public class TripDtoMapperTests
{
    private static Trip NewTrip() => new()
    {
        Id = Guid.NewGuid(),
        Name = "Summer Vacation",
        StartDate = new DateOnly(2026, 1, 1),
        EndDate = new DateOnly(2026, 1, 3),
        CreatedAt = DateTimeOffset.UtcNow.AddDays(-1),
        UpdatedAt = DateTimeOffset.UtcNow
    };

    [Fact]
    public void ToDetailDto_MapsTripLevelFields()
    {
        var trip = NewTrip();

        var result = TripDtoMapper.ToDetailDto(trip);

        result.Id.Should().Be(trip.Id);
        result.Name.Should().Be(trip.Name);
        result.StartDate.Should().Be(trip.StartDate);
        result.EndDate.Should().Be(trip.EndDate);
        result.CreatedAt.Should().Be(trip.CreatedAt);
        result.UpdatedAt.Should().Be(trip.UpdatedAt);
    }

    [Fact]
    public void ToDetailDto_ExcludesDeletedItineraryDays()
    {
        var trip = NewTrip();
        var activeDay = new ItineraryDay { Id = Guid.NewGuid(), TripId = trip.Id, DayIndex = 1 };
        var deletedDay = new ItineraryDay { Id = Guid.NewGuid(), TripId = trip.Id, DayIndex = 2, IsDeleted = true };
        trip.ItineraryDays = [activeDay, deletedDay];
        trip.TripDestinations = [];

        var result = TripDtoMapper.ToDetailDto(trip);

        result.ItineraryDays.Should().ContainSingle(d => d.Id == activeDay.Id);
    }

    [Fact]
    public void ToDetailDto_OrdersItineraryDaysByDayIndexAscending()
    {
        var trip = NewTrip();
        var day2 = new ItineraryDay { Id = Guid.NewGuid(), TripId = trip.Id, DayIndex = 2 };
        var day1 = new ItineraryDay { Id = Guid.NewGuid(), TripId = trip.Id, DayIndex = 1 };
        trip.ItineraryDays = [day2, day1];
        trip.TripDestinations = [];

        var result = TripDtoMapper.ToDetailDto(trip);

        result.ItineraryDays.Select(d => d.Id).Should().ContainInOrder(day1.Id, day2.Id);
    }

    [Fact]
    public void ToDetailDto_ExcludesDeletedDestinationsFromItineraryDay()
    {
        var trip = NewTrip();
        var day = new ItineraryDay { Id = Guid.NewGuid(), TripId = trip.Id, DayIndex = 1 };
        var activeDestination = new TripDestination
        {
            Id = Guid.NewGuid(), TripId = trip.Id, ItineraryDayId = day.Id, ProviderPlaceId = "p1", Name = "Museum", Position = 1
        };
        var deletedDestination = new TripDestination
        {
            Id = Guid.NewGuid(), TripId = trip.Id, ItineraryDayId = day.Id, ProviderPlaceId = "p2", Name = "Closed Place", Position = 2, IsDeleted = true
        };
        day.TripDestinations = [activeDestination, deletedDestination];
        trip.ItineraryDays = [day];
        trip.TripDestinations = [];

        var result = TripDtoMapper.ToDetailDto(trip);

        result.ItineraryDays.Single().TripDestinations.Should().ContainSingle(d => d.Id == activeDestination.Id);
    }

    [Fact]
    public void ToDetailDto_OrdersDestinationsWithinDayByPositionAscending()
    {
        var trip = NewTrip();
        var day = new ItineraryDay { Id = Guid.NewGuid(), TripId = trip.Id, DayIndex = 1 };
        var second = new TripDestination { Id = Guid.NewGuid(), TripId = trip.Id, ItineraryDayId = day.Id, ProviderPlaceId = "p1", Name = "Second", Position = 2 };
        var first = new TripDestination { Id = Guid.NewGuid(), TripId = trip.Id, ItineraryDayId = day.Id, ProviderPlaceId = "p2", Name = "First", Position = 1 };
        day.TripDestinations = [second, first];
        trip.ItineraryDays = [day];
        trip.TripDestinations = [];

        var result = TripDtoMapper.ToDetailDto(trip);

        result.ItineraryDays.Single().TripDestinations.Select(d => d.Id).Should().ContainInOrder(first.Id, second.Id);
    }

    [Fact]
    public void ToDetailDto_SeparatesUnscheduledDestinationsIntoSavedPlaces()
    {
        var trip = NewTrip();
        var day = new ItineraryDay { Id = Guid.NewGuid(), TripId = trip.Id, DayIndex = 1 };
        var scheduled = new TripDestination { Id = Guid.NewGuid(), TripId = trip.Id, ItineraryDayId = day.Id, ProviderPlaceId = "p1", Name = "Scheduled", Position = 1 };
        var saved = new TripDestination { Id = Guid.NewGuid(), TripId = trip.Id, ItineraryDayId = null, ProviderPlaceId = "p2", Name = "Saved", Position = 1 };
        day.TripDestinations = [scheduled];
        trip.ItineraryDays = [day];
        trip.TripDestinations = [scheduled, saved];

        var result = TripDtoMapper.ToDetailDto(trip);

        result.SavedPlaces.Should().ContainSingle(d => d.Id == saved.Id);
        result.SavedPlaces.Should().NotContain(d => d.Id == scheduled.Id);
    }

    [Fact]
    public void ToDetailDto_ExcludesDeletedSavedPlaces()
    {
        var trip = NewTrip();
        var activeSaved = new TripDestination { Id = Guid.NewGuid(), TripId = trip.Id, ItineraryDayId = null, ProviderPlaceId = "p1", Name = "Active Saved", Position = 1 };
        var deletedSaved = new TripDestination { Id = Guid.NewGuid(), TripId = trip.Id, ItineraryDayId = null, ProviderPlaceId = "p2", Name = "Deleted Saved", Position = 2, IsDeleted = true };
        trip.ItineraryDays = [];
        trip.TripDestinations = [activeSaved, deletedSaved];

        var result = TripDtoMapper.ToDetailDto(trip);

        result.SavedPlaces.Should().ContainSingle(d => d.Id == activeSaved.Id);
    }

    [Fact]
    public void ToDetailDto_OrdersSavedPlacesByPositionAscending()
    {
        var trip = NewTrip();
        var second = new TripDestination { Id = Guid.NewGuid(), TripId = trip.Id, ItineraryDayId = null, ProviderPlaceId = "p1", Name = "Second", Position = 2 };
        var first = new TripDestination { Id = Guid.NewGuid(), TripId = trip.Id, ItineraryDayId = null, ProviderPlaceId = "p2", Name = "First", Position = 1 };
        trip.ItineraryDays = [];
        trip.TripDestinations = [second, first];

        var result = TripDtoMapper.ToDetailDto(trip);

        result.SavedPlaces.Select(d => d.Id).Should().ContainInOrder(first.Id, second.Id);
    }

    [Fact]
    public void ToDetailDto_NoItineraryDaysOrDestinations_ReturnsEmptyCollections()
    {
        var trip = NewTrip();
        trip.ItineraryDays = [];
        trip.TripDestinations = [];

        var result = TripDtoMapper.ToDetailDto(trip);

        result.ItineraryDays.Should().BeEmpty();
        result.SavedPlaces.Should().BeEmpty();
    }

    [Fact]
    public void ToDestinationDto_MapsAllFields()
    {
        var destination = new TripDestination
        {
            Id = Guid.NewGuid(),
            TripId = Guid.NewGuid(),
            ItineraryDayId = Guid.NewGuid(),
            ProviderPlaceId = "xid-1",
            Name = "Eiffel Tower",
            Category = "cultural",
            ThumbnailUrl = "https://example.com/thumb.jpg",
            Lat = 48.8584,
            Lng = 2.2945,
            Position = 3
        };

        var result = TripDtoMapper.ToDestinationDto(destination);

        result.Id.Should().Be(destination.Id);
        result.TripId.Should().Be(destination.TripId);
        result.ItineraryDayId.Should().Be(destination.ItineraryDayId);
        result.ProviderPlaceId.Should().Be(destination.ProviderPlaceId);
        result.Name.Should().Be(destination.Name);
        result.Category.Should().Be(destination.Category);
        result.ThumbnailUrl.Should().Be(destination.ThumbnailUrl);
        result.Lat.Should().Be(destination.Lat);
        result.Lng.Should().Be(destination.Lng);
        result.Position.Should().Be(destination.Position);
    }
}
