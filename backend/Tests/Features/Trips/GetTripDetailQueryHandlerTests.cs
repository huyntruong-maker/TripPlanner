using Application.Features.Trips.Queries.GetTripDetailQuery;
using Domain.Entities;
using FluentAssertions;
using Tests.TestSupport;

namespace Tests.Features.Trips;

public class GetTripDetailQueryHandlerTests
{
    private static Trip NewTrip(Guid ownerId) => new() { Id = Guid.NewGuid(), Name = "Trip", UserId = ownerId };

    [Fact]
    public async Task Handle_TripNotFound_ReturnsNull()
    {
        var fixture = new TripFixture(NewTrip(Guid.NewGuid()));
        var readUnitOfWork = UnitOfWorkFake.CreateRead();
        fixture.Wire(readUnitOfWork);
        var handler = new GetTripDetailQueryHandler(readUnitOfWork);

        var result = await handler.Handle(
            new GetTripDetailQuery { TripId = Guid.NewGuid(), UserId = Guid.NewGuid() },
            CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_TripOwnedByAnotherUser_ReturnsNull()
    {
        var ownerId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var trip = NewTrip(ownerId);

        var fixture = new TripFixture(trip);
        var readUnitOfWork = UnitOfWorkFake.CreateRead();
        fixture.Wire(readUnitOfWork);
        var handler = new GetTripDetailQueryHandler(readUnitOfWork);

        var result = await handler.Handle(
            new GetTripDetailQuery { TripId = trip.Id, UserId = otherUserId },
            CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ValidRequest_ReturnsFullDetailWithDaysAndSavedPlaces()
    {
        var ownerId = Guid.NewGuid();
        var trip = NewTrip(ownerId);
        trip.StartDate = new DateOnly(2026, 1, 1);
        trip.EndDate = new DateOnly(2026, 1, 2);

        var day1 = new ItineraryDay { Id = Guid.NewGuid(), TripId = trip.Id, Date = new DateOnly(2026, 1, 1), DayIndex = 1 };
        var deletedDay = new ItineraryDay { Id = Guid.NewGuid(), TripId = trip.Id, Date = new DateOnly(2026, 1, 3), DayIndex = 2, IsDeleted = true };

        var scheduledDestination = new TripDestination
        {
            Id = Guid.NewGuid(),
            TripId = trip.Id,
            ItineraryDayId = day1.Id,
            ProviderPlaceId = "place-1",
            Name = "Eiffel Tower",
            Position = 1
        };
        var savedPlace = new TripDestination
        {
            Id = Guid.NewGuid(),
            TripId = trip.Id,
            ItineraryDayId = null,
            ProviderPlaceId = "place-2",
            Name = "Louvre",
            Position = 1
        };
        var deletedDestination = new TripDestination
        {
            Id = Guid.NewGuid(),
            TripId = trip.Id,
            ItineraryDayId = day1.Id,
            ProviderPlaceId = "place-3",
            Name = "Deleted Place",
            Position = 2,
            IsDeleted = true
        };

        var fixture = new TripFixture(trip, [day1, deletedDay], [scheduledDestination, savedPlace, deletedDestination]);
        var readUnitOfWork = UnitOfWorkFake.CreateRead();
        fixture.Wire(readUnitOfWork);
        var handler = new GetTripDetailQueryHandler(readUnitOfWork);

        var result = await handler.Handle(
            new GetTripDetailQuery { TripId = trip.Id, UserId = ownerId },
            CancellationToken.None);

        result.Should().NotBeNull();
        result!.ItineraryDays.Should().ContainSingle();
        result.ItineraryDays[0].TripDestinations.Should().ContainSingle(d => d.Id == scheduledDestination.Id);
        result.SavedPlaces.Should().ContainSingle(d => d.Id == savedPlace.Id);
    }
}
