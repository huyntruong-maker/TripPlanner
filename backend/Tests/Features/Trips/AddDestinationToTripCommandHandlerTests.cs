using Application.Features.Trips.Commands.AddDestinationToTripCommand;
using Domain.Entities;
using Domain.Messages;
using FluentAssertions;
using Tests.TestSupport;

namespace Tests.Features.Trips;

public class AddDestinationToTripCommandHandlerTests
{
    private static Trip NewTrip(Guid ownerId) => new() { Id = Guid.NewGuid(), Name = "Trip", UserId = ownerId };

    private static AddDestinationToTripCommand Command(Guid tripId, Guid userId, Guid? itineraryDayId = null) => new()
    {
        TripId = tripId,
        UserId = userId,
        ItineraryDayId = itineraryDayId,
        ProviderPlaceId = "place-1",
        Name = "Eiffel Tower",
        Lat = 48.8584,
        Lng = 2.2945
    };

    [Fact]
    public async Task Handle_TripNotFound_ReturnsNotFoundError()
    {
        var writeUnitOfWork = UnitOfWorkFake.CreateWrite();
        writeUnitOfWork.RegisterRepository<Trip>();
        writeUnitOfWork.RegisterRepository<ItineraryDay>();
        writeUnitOfWork.RegisterRepository<TripDestination>();
        var handler = new AddDestinationToTripCommandHandler(writeUnitOfWork);

        var (errorCode, destination) = await handler.Handle(Command(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        errorCode.Should().Be(TripControllerMsg.NotFound);
        destination.Should().BeNull();
    }

    [Fact]
    public async Task Handle_TripOwnedByAnotherUser_ReturnsNotFoundError()
    {
        var ownerId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var trip = NewTrip(ownerId);

        var writeUnitOfWork = UnitOfWorkFake.CreateWrite();
        writeUnitOfWork.RegisterRepository([trip]);
        writeUnitOfWork.RegisterRepository<ItineraryDay>();
        writeUnitOfWork.RegisterRepository<TripDestination>();
        var handler = new AddDestinationToTripCommandHandler(writeUnitOfWork);

        var (errorCode, destination) = await handler.Handle(Command(trip.Id, otherUserId), CancellationToken.None);

        errorCode.Should().Be(TripControllerMsg.NotFound);
        destination.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ItineraryDayNotBelongingToTrip_ReturnsItineraryDayNotFoundError()
    {
        var ownerId = Guid.NewGuid();
        var trip = NewTrip(ownerId);
        var otherTrip = NewTrip(Guid.NewGuid());
        var dayInOtherTrip = new ItineraryDay { Id = Guid.NewGuid(), TripId = otherTrip.Id, Date = new DateOnly(2026, 1, 1), DayIndex = 1 };

        var writeUnitOfWork = UnitOfWorkFake.CreateWrite();
        writeUnitOfWork.RegisterRepository([trip]);
        writeUnitOfWork.RegisterRepository([dayInOtherTrip]);
        writeUnitOfWork.RegisterRepository<TripDestination>();
        var handler = new AddDestinationToTripCommandHandler(writeUnitOfWork);

        var (errorCode, destination) = await handler.Handle(
            Command(trip.Id, ownerId, itineraryDayId: dayInOtherTrip.Id),
            CancellationToken.None);

        errorCode.Should().Be(TripControllerMsg.AddDestination.ItineraryDayNotFound);
        destination.Should().BeNull();
    }

    [Fact]
    public async Task Handle_DuplicateInSameDay_ReturnsDuplicateInDayError()
    {
        var ownerId = Guid.NewGuid();
        var trip = NewTrip(ownerId);
        var day = new ItineraryDay { Id = Guid.NewGuid(), TripId = trip.Id, Date = new DateOnly(2026, 1, 1), DayIndex = 1 };
        var existing = new TripDestination
        {
            TripId = trip.Id,
            ItineraryDayId = day.Id,
            ProviderPlaceId = "place-1",
            Name = "Eiffel Tower",
            Position = 1
        };

        var writeUnitOfWork = UnitOfWorkFake.CreateWrite();
        writeUnitOfWork.RegisterRepository([trip]);
        writeUnitOfWork.RegisterRepository([day]);
        writeUnitOfWork.RegisterRepository([existing]);
        var handler = new AddDestinationToTripCommandHandler(writeUnitOfWork);

        var (errorCode, destination) = await handler.Handle(
            Command(trip.Id, ownerId, itineraryDayId: day.Id),
            CancellationToken.None);

        errorCode.Should().Be(TripControllerMsg.AddDestination.DuplicateInDay);
        destination.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ValidRequestWithNullDay_AddsToSavedPlaces()
    {
        var ownerId = Guid.NewGuid();
        var trip = NewTrip(ownerId);

        var writeUnitOfWork = UnitOfWorkFake.CreateWrite();
        writeUnitOfWork.RegisterRepository([trip]);
        writeUnitOfWork.RegisterRepository<ItineraryDay>();
        var destinationStore = writeUnitOfWork.RegisterRepository<TripDestination>();
        var handler = new AddDestinationToTripCommandHandler(writeUnitOfWork);

        var (errorCode, destination) = await handler.Handle(Command(trip.Id, ownerId), CancellationToken.None);

        errorCode.Should().Be(string.Empty);
        destination.Should().NotBeNull();
        destination!.ItineraryDayId.Should().BeNull();
        destination.Position.Should().Be(1);
        destinationStore.Should().ContainSingle();
    }

    [Fact]
    public async Task Handle_ValidRequestToDayWithExistingDestinations_AppendsNextPosition()
    {
        var ownerId = Guid.NewGuid();
        var trip = NewTrip(ownerId);
        var day = new ItineraryDay { Id = Guid.NewGuid(), TripId = trip.Id, Date = new DateOnly(2026, 1, 1), DayIndex = 1 };
        var existing = new TripDestination
        {
            TripId = trip.Id,
            ItineraryDayId = day.Id,
            ProviderPlaceId = "other-place",
            Name = "Louvre",
            Position = 1
        };

        var writeUnitOfWork = UnitOfWorkFake.CreateWrite();
        writeUnitOfWork.RegisterRepository([trip]);
        writeUnitOfWork.RegisterRepository([day]);
        var destinationStore = writeUnitOfWork.RegisterRepository([existing]);
        var handler = new AddDestinationToTripCommandHandler(writeUnitOfWork);

        var (errorCode, destination) = await handler.Handle(
            Command(trip.Id, ownerId, itineraryDayId: day.Id),
            CancellationToken.None);

        errorCode.Should().Be(string.Empty);
        destination!.Position.Should().Be(2);
        destinationStore.Should().HaveCount(2);
    }
}
