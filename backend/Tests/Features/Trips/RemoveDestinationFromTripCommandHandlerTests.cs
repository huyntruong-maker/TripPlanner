using Application.Features.Trips.Commands.RemoveDestinationFromTripCommand;
using Domain.Entities;
using Domain.Messages;
using FluentAssertions;
using Tests.TestSupport;

namespace Tests.Features.Trips;

public class RemoveDestinationFromTripCommandHandlerTests
{
    private static Trip NewTrip(Guid ownerId) => new() { Id = Guid.NewGuid(), Name = "Trip", UserId = ownerId };

    [Fact]
    public async Task Handle_TripNotFound_ReturnsNotFoundError()
    {
        var writeUnitOfWork = UnitOfWorkFake.CreateWrite();
        writeUnitOfWork.RegisterRepository<Trip>();
        writeUnitOfWork.RegisterRepository<TripDestination>();
        var handler = new RemoveDestinationFromTripCommandHandler(writeUnitOfWork);

        var (errorCode, success) = await handler.Handle(
            new RemoveDestinationFromTripCommand { TripId = Guid.NewGuid(), TripDestinationId = Guid.NewGuid(), UserId = Guid.NewGuid() },
            CancellationToken.None);

        errorCode.Should().Be(TripControllerMsg.NotFound);
        success.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_TripOwnedByAnotherUser_ReturnsNotFoundError()
    {
        var ownerId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var trip = NewTrip(ownerId);
        var destination = new TripDestination
        {
            Id = Guid.NewGuid(),
            TripId = trip.Id,
            ProviderPlaceId = "place-1",
            Name = "Eiffel Tower"
        };

        var writeUnitOfWork = UnitOfWorkFake.CreateWrite();
        writeUnitOfWork.RegisterRepository([trip]);
        writeUnitOfWork.RegisterRepository([destination]);
        var handler = new RemoveDestinationFromTripCommandHandler(writeUnitOfWork);

        var (errorCode, success) = await handler.Handle(
            new RemoveDestinationFromTripCommand { TripId = trip.Id, TripDestinationId = destination.Id, UserId = otherUserId },
            CancellationToken.None);

        errorCode.Should().Be(TripControllerMsg.NotFound);
        success.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_DestinationNotBelongingToTrip_ReturnsNotFoundError()
    {
        var ownerId = Guid.NewGuid();
        var trip = NewTrip(ownerId);

        var writeUnitOfWork = UnitOfWorkFake.CreateWrite();
        writeUnitOfWork.RegisterRepository([trip]);
        writeUnitOfWork.RegisterRepository<TripDestination>();
        var handler = new RemoveDestinationFromTripCommandHandler(writeUnitOfWork);

        var (errorCode, success) = await handler.Handle(
            new RemoveDestinationFromTripCommand { TripId = trip.Id, TripDestinationId = Guid.NewGuid(), UserId = ownerId },
            CancellationToken.None);

        errorCode.Should().Be(TripControllerMsg.NotFound);
        success.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ValidRequest_SoftDeletesDestinationAndReturnsTrue()
    {
        var ownerId = Guid.NewGuid();
        var trip = NewTrip(ownerId);
        var destination = new TripDestination
        {
            Id = Guid.NewGuid(),
            TripId = trip.Id,
            ProviderPlaceId = "place-1",
            Name = "Eiffel Tower"
        };

        var writeUnitOfWork = UnitOfWorkFake.CreateWrite();
        writeUnitOfWork.RegisterRepository([trip]);
        writeUnitOfWork.RegisterRepository([destination]);
        var handler = new RemoveDestinationFromTripCommandHandler(writeUnitOfWork);

        var (errorCode, success) = await handler.Handle(
            new RemoveDestinationFromTripCommand { TripId = trip.Id, TripDestinationId = destination.Id, UserId = ownerId },
            CancellationToken.None);

        errorCode.Should().Be(string.Empty);
        success.Should().BeTrue();
        destination.IsDeleted.Should().BeTrue();
    }
}
