using Application.Features.Trips.Queries.GetTripsQuery;
using Domain.Entities;
using FluentAssertions;
using Tests.TestSupport;

namespace Tests.Features.Trips;

public class GetTripsQueryHandlerTests
{
    [Fact]
    public async Task Handle_UserHasNoTrips_ReturnsEmptyList()
    {
        var readUnitOfWork = UnitOfWorkFake.CreateRead();
        readUnitOfWork.RegisterRepository<Trip>();
        var handler = new GetTripsQueryHandler(readUnitOfWork);

        var result = await handler.Handle(new GetTripsQuery { UserId = Guid.NewGuid() }, CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_UserHasTrips_ReturnsOnlyRequestingUsersTripsOrderedByCreatedAtDescending()
    {
        var requestingUserId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        var olderTrip = new Trip { Id = Guid.NewGuid(), Name = "Older Trip", UserId = requestingUserId, CreatedAt = DateTimeOffset.UtcNow.AddDays(-2) };
        var newerTrip = new Trip { Id = Guid.NewGuid(), Name = "Newer Trip", UserId = requestingUserId, CreatedAt = DateTimeOffset.UtcNow.AddDays(-1) };
        var otherUsersTrip = new Trip { Id = Guid.NewGuid(), Name = "Someone Else's Trip", UserId = otherUserId, CreatedAt = DateTimeOffset.UtcNow };

        var readUnitOfWork = UnitOfWorkFake.CreateRead();
        readUnitOfWork.RegisterRepository([olderTrip, newerTrip, otherUsersTrip]);
        var handler = new GetTripsQueryHandler(readUnitOfWork);

        var result = await handler.Handle(new GetTripsQuery { UserId = requestingUserId }, CancellationToken.None);

        result.Should().HaveCount(2);
        result.Select(t => t.Name).Should().ContainInOrder("Newer Trip", "Older Trip");
        result.Select(t => t.Id).Should().NotContain(otherUsersTrip.Id);
    }
}
