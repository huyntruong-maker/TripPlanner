using Application.Features.Trips.Commands.SetTripDatesCommand;
using Domain.Entities;
using Domain.Messages;
using FluentAssertions;
using Tests.TestSupport;

namespace Tests.Features.Trips;

public class SetTripDatesCommandHandlerTests
{
    private static Trip NewTrip(Guid ownerId) => new() { Id = Guid.NewGuid(), Name = "Trip", UserId = ownerId };

    [Fact]
    public async Task Handle_TripNotFound_ReturnsNotFoundError()
    {
        var fixture = new TripFixture(NewTrip(Guid.NewGuid()));
        var writeUnitOfWork = UnitOfWorkFake.CreateWrite();
        fixture.Wire(writeUnitOfWork);
        var handler = new SetTripDatesCommandHandler(writeUnitOfWork);

        var (errorCode, result) = await handler.Handle(
            new SetTripDatesCommand
            {
                TripId = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                StartDate = new DateOnly(2026, 1, 1),
                EndDate = new DateOnly(2026, 1, 2)
            },
            CancellationToken.None);

        errorCode.Should().Be(TripControllerMsg.NotFound);
        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_TripOwnedByAnotherUser_ReturnsNotFoundError()
    {
        var ownerId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var trip = NewTrip(ownerId);

        var fixture = new TripFixture(trip);
        var writeUnitOfWork = UnitOfWorkFake.CreateWrite();
        fixture.Wire(writeUnitOfWork);
        var handler = new SetTripDatesCommandHandler(writeUnitOfWork);

        var (errorCode, result) = await handler.Handle(
            new SetTripDatesCommand
            {
                TripId = trip.Id,
                UserId = otherUserId,
                StartDate = new DateOnly(2026, 1, 1),
                EndDate = new DateOnly(2026, 1, 2)
            },
            CancellationToken.None);

        errorCode.Should().Be(TripControllerMsg.NotFound);
        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_NoExistingDays_CreatesOneDayPerDateInRange()
    {
        var ownerId = Guid.NewGuid();
        var trip = NewTrip(ownerId);

        var fixture = new TripFixture(trip);
        var writeUnitOfWork = UnitOfWorkFake.CreateWrite();
        fixture.Wire(writeUnitOfWork);
        var handler = new SetTripDatesCommandHandler(writeUnitOfWork);

        var (errorCode, result) = await handler.Handle(
            new SetTripDatesCommand
            {
                TripId = trip.Id,
                UserId = ownerId,
                StartDate = new DateOnly(2026, 1, 1),
                EndDate = new DateOnly(2026, 1, 3)
            },
            CancellationToken.None);

        errorCode.Should().Be(string.Empty);
        result.Should().NotBeNull();
        result!.Trip.ItineraryDays.Should().HaveCount(3);
        result.Trip.ItineraryDays.Select(d => d.DayIndex).Should().ContainInOrder(1, 2, 3);
        result.DestinationsUnscheduledCount.Should().Be(0);
        fixture.Days.Should().HaveCount(3);
    }

    [Fact]
    public async Task Handle_ShorteningRange_SoftDeletesRemovedDaysAndUnschedulesTheirDestinations()
    {
        var ownerId = Guid.NewGuid();
        var trip = NewTrip(ownerId);
        var day1 = new ItineraryDay { Id = Guid.NewGuid(), TripId = trip.Id, Date = new DateOnly(2026, 1, 1), DayIndex = 1 };
        var day2 = new ItineraryDay { Id = Guid.NewGuid(), TripId = trip.Id, Date = new DateOnly(2026, 1, 2), DayIndex = 2 };
        var destinationOnDay2 = new TripDestination
        {
            TripId = trip.Id,
            ItineraryDayId = day2.Id,
            ProviderPlaceId = "place-1",
            Name = "Louvre",
            Position = 1
        };

        var fixture = new TripFixture(trip, [day1, day2], [destinationOnDay2]);
        var writeUnitOfWork = UnitOfWorkFake.CreateWrite();
        fixture.Wire(writeUnitOfWork);
        var handler = new SetTripDatesCommandHandler(writeUnitOfWork);

        var (errorCode, result) = await handler.Handle(
            new SetTripDatesCommand
            {
                TripId = trip.Id,
                UserId = ownerId,
                StartDate = new DateOnly(2026, 1, 1),
                EndDate = new DateOnly(2026, 1, 1)
            },
            CancellationToken.None);

        errorCode.Should().Be(string.Empty);
        result!.DestinationsUnscheduledCount.Should().Be(1);
        result.Trip.ItineraryDays.Should().HaveCount(1);
        result.Trip.ItineraryDays.Should().ContainSingle(d => d.Id == day1.Id);
        result.Trip.ItineraryDays.Should().NotContain(d => d.Id == day2.Id);
        day2.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ExtendingRange_KeepsExistingDaysAndAddsNewOnes()
    {
        var ownerId = Guid.NewGuid();
        var trip = NewTrip(ownerId);
        var day1 = new ItineraryDay { Id = Guid.NewGuid(), TripId = trip.Id, Date = new DateOnly(2026, 1, 1), DayIndex = 1 };

        var fixture = new TripFixture(trip, [day1]);
        var writeUnitOfWork = UnitOfWorkFake.CreateWrite();
        fixture.Wire(writeUnitOfWork);
        var handler = new SetTripDatesCommandHandler(writeUnitOfWork);

        var (errorCode, result) = await handler.Handle(
            new SetTripDatesCommand
            {
                TripId = trip.Id,
                UserId = ownerId,
                StartDate = new DateOnly(2026, 1, 1),
                EndDate = new DateOnly(2026, 1, 2)
            },
            CancellationToken.None);

        errorCode.Should().Be(string.Empty);
        result!.Trip.ItineraryDays.Should().HaveCount(2);
        result.Trip.ItineraryDays.Should().Contain(d => d.Id == day1.Id);
        day1.IsDeleted.Should().BeFalse();
    }
}
