using System.Linq.Expressions;
using Application.Interfaces.DataAccess;
using Domain.Entities;
using Microsoft.EntityFrameworkCore.Query;
using NSubstitute;

namespace Tests.TestSupport;

/// <summary>
/// Backs a single Trip aggregate with live ItineraryDay/TripDestination stores so that
/// handler mutations (Add/FK changes) are reflected the next time the Trip is re-queried
/// with Include, mirroring how a fresh EF query would reflect current row state.
/// </summary>
public sealed class TripFixture
{
    public Trip Trip { get; }

    public List<ItineraryDay> Days { get; }

    public List<TripDestination> Destinations { get; }

    private readonly IBaseWriteRepository<Trip> _tripRepo = Substitute.For<IBaseWriteRepository<Trip>>();

    public TripFixture(Trip trip, List<ItineraryDay>? days = null, List<TripDestination>? destinations = null)
    {
        Trip = trip;
        Days = days ?? [];
        Destinations = destinations ?? [];

        Trip.ItineraryDays = Days;
        Trip.TripDestinations = Destinations;

        ConfigureTripRepo();
    }

    public void Wire(IWriteUnitOfWork writeUnitOfWork)
    {
        writeUnitOfWork.GetRepository<Trip>().Returns(_tripRepo);
        writeUnitOfWork.RegisterRepository(Days, ResyncDayDestinations);
        writeUnitOfWork.RegisterRepository(Destinations);
    }

    public void Wire(IReadUnitOfWork readUnitOfWork)
    {
        readUnitOfWork.GetRepository<Trip>().Returns(_tripRepo);
        readUnitOfWork.RegisterRepository(Days, ResyncDayDestinations);
        readUnitOfWork.RegisterRepository(Destinations);
    }

    private void ConfigureTripRepo()
    {
        _tripRepo.Single(
                Arg.Any<Expression<Func<Trip, bool>>?>(),
                Arg.Any<Func<IQueryable<Trip>, IOrderedQueryable<Trip>>?>(),
                Arg.Any<Func<IQueryable<Trip>, IIncludableQueryable<Trip, object>>?>(),
                Arg.Any<bool>())
            .Returns(callInfo =>
            {
                ResyncDayDestinations();
                var predicate = callInfo.ArgAt<Expression<Func<Trip, bool>>?>(0);
                var matches = predicate is null || predicate.Compile()(Trip);
                return Task.FromResult(matches ? Trip : null);
            });

        _tripRepo.Any(Arg.Any<Expression<Func<Trip, bool>>>())
            .Returns(callInfo =>
            {
                var predicate = callInfo.ArgAt<Expression<Func<Trip, bool>>>(0);
                return Task.FromResult(predicate.Compile()(Trip));
            });

        _tripRepo.QueryCondition(Arg.Any<Expression<Func<Trip, bool>>>(), Arg.Any<bool>())
            .Returns(callInfo =>
            {
                var predicate = callInfo.ArgAt<Expression<Func<Trip, bool>>>(0);
                return Task.FromResult(new[] { Trip }.AsQueryable().Where(predicate));
            });

        _tripRepo.Add(Arg.Any<Trip>()).Returns(Task.CompletedTask);
        _tripRepo.Update(Arg.Any<Trip>()).Returns(Task.CompletedTask);
    }

    private void ResyncDayDestinations()
    {
        foreach (var day in Days)
        {
            day.TripDestinations = Destinations.Where(d => d.ItineraryDayId == day.Id).ToList();
        }
    }
}
