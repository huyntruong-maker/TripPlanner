using Application.Dtos.Destinations;
using Application.Features.Destinations.Queries.SearchLocationsQuery;
using Application.Interfaces.Providers;
using FluentAssertions;
using NSubstitute;

namespace Tests.Features.Destinations;

public class SearchLocationsQueryHandlerTests
{
    [Fact]
    public async Task Handle_ValidQuery_ReturnsItemsAndTotalCountFromProvider()
    {
        var geocodingProvider = Substitute.For<IGeocodingProvider>();
        var locations = new List<LocationDto>
        {
            new() { Name = "Paris", DisplayName = "Paris, France", Latitude = 48.8566, Longitude = 2.3522 },
            new() { Name = "Paris", DisplayName = "Paris, Texas, USA", Latitude = 33.6609, Longitude = -95.5555 }
        };
        geocodingProvider.SearchLocationsAsync("Paris", 5, Arg.Any<CancellationToken>()).Returns(locations);
        var handler = new SearchLocationsQueryHandler(geocodingProvider);

        var result = await handler.Handle(new SearchLocationsQuery { Query = "Paris" }, CancellationToken.None);

        result.Items.Should().BeEquivalentTo(locations);
        result.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task Handle_ProviderReturnsNoMatches_ReturnsEmptyResult()
    {
        var geocodingProvider = Substitute.For<IGeocodingProvider>();
        geocodingProvider.SearchLocationsAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<LocationDto>());
        var handler = new SearchLocationsQueryHandler(geocodingProvider);

        var result = await handler.Handle(new SearchLocationsQuery { Query = "Nowhereland" }, CancellationToken.None);

        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_CustomMaxResults_PassesMaxResultsToProvider()
    {
        var geocodingProvider = Substitute.For<IGeocodingProvider>();
        geocodingProvider.SearchLocationsAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<LocationDto>());
        var handler = new SearchLocationsQueryHandler(geocodingProvider);

        await handler.Handle(new SearchLocationsQuery { Query = "Hanoi", MaxResults = 10 }, CancellationToken.None);

        await geocodingProvider.Received(1).SearchLocationsAsync("Hanoi", 10, Arg.Any<CancellationToken>());
    }
}
