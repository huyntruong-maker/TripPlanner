using Application.Features.Trips.Commands.MoveTripDestinationCommand;
using Domain.Entities;
using Domain.Messages;
using FluentAssertions;
using Tests.TestSupport;

namespace Tests.Features.Trips;

public class MoveTripDestinationCommandHandlerTests
{
    private static Trip NewTrip(Guid ownerId) => new() { Id = Guid.NewGuid(), Name = "Trip", UserId = ownerId };

    [Fact]
    public async Task Handle_TripNotFound_ReturnsNotFoundError()
    {
        var fixture = new TripFixture(NewTrip(Guid.NewGuid()));
        var writeUnitOfWork = UnitOfWorkFake.CreateWrite();
        fixture.Wire(writeUnitOfWork);
        var handler = new MoveTripDestinationCommandHandler(writeUnitOfWork);

        var (errorCode, tripDto) = await handler.Handle(
            new MoveTripDestinationCommand { TripId = Guid.NewGuid(), TripDestinationId = Guid.NewGuid(), UserId = Guid.NewGuid() },
            CancellationToken.None);

        errorCode.Should().Be(TripControllerMsg.NotFound);
        tripDto.Should().BeNull();
    }

    [Fact]
    public async Task Handle_TripOwnedByAnotherUser_ReturnsNotFoundError()
    {
        var ownerId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var trip = NewTrip(ownerId);
        var destination = new TripDestination { Id = Guid.NewGuid(), TripId = trip.Id, ProviderPlaceId = "p1", Name = "Place", Position = 1 };

        var fixture = new TripFixture(trip, destinations: [destination]);
        var writeUnitOfWork = UnitOfWorkFake.CreateWrite();
        fixture.Wire(writeUnitOfWork);
        var handler = new MoveTripDestinationCommandHandler(writeUnitOfWork);

        var (errorCode, tripDto) = await handler.Handle(
            new MoveTripDestinationCommand { TripId = trip.Id, TripDestinationId = destination.Id, UserId = otherUserId },
            CancellationToken.None);

        errorCode.Should().Be(TripControllerMsg.NotFound);
        tripDto.Should().BeNull();
    }

    [Fact]
    public async Task Handle_DestinationNotFound_ReturnsDestinationNotFoundError()
    {
        var ownerId = Guid.NewGuid();
        var trip = NewTrip(ownerId);

        var fixture = new TripFixture(trip);
        var writeUnitOfWork = UnitOfWorkFake.CreateWrite();
        fixture.Wire(writeUnitOfWork);
        var handler = new MoveTripDestinationCommandHandler(writeUnitOfWork);

        var (errorCode, tripDto) = await handler.Handle(
            new MoveTripDestinationCommand { TripId = trip.Id, TripDestinationId = Guid.NewGuid(), UserId = ownerId },
            CancellationToken.None);

        errorCode.Should().Be(TripControllerMsg.MoveDestination.DestinationNotFound);
        tripDto.Should().BeNull();
    }

    [Fact]
    public async Task Handle_TargetDayNotBelongingToTrip_ReturnsItineraryDayNotFoundError()
    {
        var ownerId = Guid.NewGuid();
        var trip = NewTrip(ownerId);
        var destination = new TripDestination { Id = Guid.NewGuid(), TripId = trip.Id, ProviderPlaceId = "p1", Name = "Place", Position = 1 };

        var fixture = new TripFixture(trip, destinations: [destination]);
        var writeUnitOfWork = UnitOfWorkFake.CreateWrite();
        fixture.Wire(writeUnitOfWork);
        var handler = new MoveTripDestinationCommandHandler(writeUnitOfWork);

        var (errorCode, tripDto) = await handler.Handle(
            new MoveTripDestinationCommand
            {
                TripId = trip.Id,
                TripDestinationId = destination.Id,
                UserId = ownerId,
                ItineraryDayId = Guid.NewGuid()
            },
            CancellationToken.None);

        errorCode.Should().Be(TripControllerMsg.MoveDestination.ItineraryDayNotFound);
        tripDto.Should().BeNull();
    }

    [Fact]
    public async Task Handle_DuplicateInTargetBucket_ReturnsDuplicateInDayError()
    {
        var ownerId = Guid.NewGuid();
        var trip = NewTrip(ownerId);
        var day1 = new ItineraryDay { Id = Guid.NewGuid(), TripId = trip.Id, Date = new DateOnly(2026, 1, 1), DayIndex = 1 };
        var day2 = new ItineraryDay { Id = Guid.NewGuid(), TripId = trip.Id, Date = new DateOnly(2026, 1, 2), DayIndex = 2 };

        var moving = new TripDestination { Id = Guid.NewGuid(), TripId = trip.Id, ItineraryDayId = day1.Id, ProviderPlaceId = "shared-place", Name = "A", Position = 1 };
        var blocking = new TripDestination { Id = Guid.NewGuid(), TripId = trip.Id, ItineraryDayId = day2.Id, ProviderPlaceId = "shared-place", Name = "A", Position = 1 };

        var fixture = new TripFixture(trip, [day1, day2], [moving, blocking]);
        var writeUnitOfWork = UnitOfWorkFake.CreateWrite();
        fixture.Wire(writeUnitOfWork);
        var handler = new MoveTripDestinationCommandHandler(writeUnitOfWork);

        var (errorCode, tripDto) = await handler.Handle(
            new MoveTripDestinationCommand
            {
                TripId = trip.Id,
                TripDestinationId = moving.Id,
                UserId = ownerId,
                ItineraryDayId = day2.Id
            },
            CancellationToken.None);

        errorCode.Should().Be(TripControllerMsg.MoveDestination.DuplicateInDay);
        tripDto.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ValidMoveToAnotherDay_ReindexesBothBucketsAndReturnsUpdatedTrip()
    {
        var ownerId = Guid.NewGuid();
        var trip = NewTrip(ownerId);
        var day1 = new ItineraryDay { Id = Guid.NewGuid(), TripId = trip.Id, Date = new DateOnly(2026, 1, 1), DayIndex = 1 };
        var day2 = new ItineraryDay { Id = Guid.NewGuid(), TripId = trip.Id, Date = new DateOnly(2026, 1, 2), DayIndex = 2 };

        var moving = new TripDestination { Id = Guid.NewGuid(), TripId = trip.Id, ItineraryDayId = day1.Id, ProviderPlaceId = "p1", Name = "A", Position = 1 };
        var remainingInDay1 = new TripDestination { Id = Guid.NewGuid(), TripId = trip.Id, ItineraryDayId = day1.Id, ProviderPlaceId = "p2", Name = "B", Position = 2 };
        var existingInDay2 = new TripDestination { Id = Guid.NewGuid(), TripId = trip.Id, ItineraryDayId = day2.Id, ProviderPlaceId = "p3", Name = "C", Position = 1 };

        var fixture = new TripFixture(trip, [day1, day2], [moving, remainingInDay1, existingInDay2]);
        var writeUnitOfWork = UnitOfWorkFake.CreateWrite();
        fixture.Wire(writeUnitOfWork);
        var handler = new MoveTripDestinationCommandHandler(writeUnitOfWork);

        var (errorCode, tripDto) = await handler.Handle(
            new MoveTripDestinationCommand
            {
                TripId = trip.Id,
                TripDestinationId = moving.Id,
                UserId = ownerId,
                ItineraryDayId = day2.Id,
                Position = 1
            },
            CancellationToken.None);

        errorCode.Should().Be(string.Empty);
        tripDto.Should().NotBeNull();
        moving.ItineraryDayId.Should().Be(day2.Id);
        moving.Position.Should().Be(1);
        existingInDay2.Position.Should().Be(2);
        remainingInDay1.Position.Should().Be(1);

        var resultDay1 = tripDto!.ItineraryDays.Single(d => d.Id == day1.Id);
        resultDay1.TripDestinations.Should().ContainSingle(d => d.Id == remainingInDay1.Id);

        var resultDay2 = tripDto.ItineraryDays.Single(d => d.Id == day2.Id);
        resultDay2.TripDestinations.Select(d => d.Id).Should().ContainInOrder(moving.Id, existingInDay2.Id);
    }

    [Fact]
    public async Task Handle_ValidReorderWithinSameDay_UpdatesPositionsOnly()
    {
        var ownerId = Guid.NewGuid();
        var trip = NewTrip(ownerId);
        var day1 = new ItineraryDay { Id = Guid.NewGuid(), TripId = trip.Id, Date = new DateOnly(2026, 1, 1), DayIndex = 1 };

        var first = new TripDestination { Id = Guid.NewGuid(), TripId = trip.Id, ItineraryDayId = day1.Id, ProviderPlaceId = "p1", Name = "A", Position = 1 };
        var second = new TripDestination { Id = Guid.NewGuid(), TripId = trip.Id, ItineraryDayId = day1.Id, ProviderPlaceId = "p2", Name = "B", Position = 2 };

        var fixture = new TripFixture(trip, [day1], [first, second]);
        var writeUnitOfWork = UnitOfWorkFake.CreateWrite();
        fixture.Wire(writeUnitOfWork);
        var handler = new MoveTripDestinationCommandHandler(writeUnitOfWork);

        var (errorCode, tripDto) = await handler.Handle(
            new MoveTripDestinationCommand
            {
                TripId = trip.Id,
                TripDestinationId = second.Id,
                UserId = ownerId,
                ItineraryDayId = day1.Id,
                Position = 1
            },
            CancellationToken.None);

        errorCode.Should().Be(string.Empty);
        second.Position.Should().Be(1);
        first.Position.Should().Be(2);

        var resultDay = tripDto!.ItineraryDays.Single(d => d.Id == day1.Id);
        resultDay.TripDestinations.Select(d => d.Id).Should().ContainInOrder(second.Id, first.Id);
    }
}
