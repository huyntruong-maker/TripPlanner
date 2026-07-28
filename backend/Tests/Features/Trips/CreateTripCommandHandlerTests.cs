using Application.Features.Trips.Commands.CreateTripCommand;
using Domain.Entities;
using FluentAssertions;
using NSubstitute;
using Tests.TestSupport;

namespace Tests.Features.Trips;

public class CreateTripCommandHandlerTests
{
    [Fact]
    public async Task Handle_ValidRequest_CreatesTripAndReturnsEmptyError()
    {
        var writeUnitOfWork = UnitOfWorkFake.CreateWrite();
        var tripStore = writeUnitOfWork.RegisterRepository<Trip>();
        var handler = new CreateTripCommandHandler(writeUnitOfWork);
        var userId = Guid.NewGuid();

        var (errorCode, tripDto) = await handler.Handle(
            new CreateTripCommand { Name = "Summer Vacation", UserId = userId },
            CancellationToken.None);

        errorCode.Should().Be(string.Empty);
        tripDto.Name.Should().Be("Summer Vacation");
        tripStore.Should().ContainSingle();
        await writeUnitOfWork.Received(1).SaveChanges();
    }

    [Fact]
    public async Task Handle_ValidRequest_AssignsTripToRequestingUserOnly()
    {
        var writeUnitOfWork = UnitOfWorkFake.CreateWrite();
        var tripStore = writeUnitOfWork.RegisterRepository<Trip>();
        var handler = new CreateTripCommandHandler(writeUnitOfWork);
        var requestingUserId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        await handler.Handle(new CreateTripCommand { Name = "My Trip", UserId = requestingUserId }, CancellationToken.None);

        var createdTrip = tripStore.Should().ContainSingle().Subject;
        createdTrip.UserId.Should().Be(requestingUserId);
        createdTrip.UserId.Should().NotBe(otherUserId);
        createdTrip.CreatedBy.Should().Be(requestingUserId);
    }
}
