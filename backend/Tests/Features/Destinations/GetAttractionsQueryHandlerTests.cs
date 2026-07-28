using Application.Dtos.Destinations;
using Application.Features.Destinations.Queries.GetAttractionsQuery;
using Application.Interfaces.Providers;
using FluentAssertions;
using NSubstitute;

namespace Tests.Features.Destinations;

public class GetAttractionsQueryHandlerTests
{
    [Fact]
    public async Task Handle_ValidRequest_ReturnsResultFromProvider()
    {
        var destinationProvider = Substitute.For<IDestinationProvider>();
        var expectedResult = new AttractionSearchResultDto
        {
            Items =
            [
                new AttractionDto { ProviderPlaceId = "xid-1", Name = "Eiffel Tower" }
            ],
            TotalCount = 1
        };
        destinationProvider.GetAttractionsAsync(48.8566, 2.3522, 20_000, 1, 20, Arg.Any<CancellationToken>())
            .Returns(expectedResult);
        var handler = new GetAttractionsQueryHandler(destinationProvider);

        var result = await handler.Handle(
            new GetAttractionsQuery { Latitude = 48.8566, Longitude = 2.3522 },
            CancellationToken.None);

        result.Should().BeSameAs(expectedResult);
    }

    [Fact]
    public async Task Handle_ProviderReturnsNoAttractions_ReturnsEmptyResult()
    {
        var destinationProvider = Substitute.For<IDestinationProvider>();
        destinationProvider.GetAttractionsAsync(
                Arg.Any<double>(), Arg.Any<double>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new AttractionSearchResultDto());
        var handler = new GetAttractionsQueryHandler(destinationProvider);

        var result = await handler.Handle(
            new GetAttractionsQuery { Latitude = 0, Longitude = 0 },
            CancellationToken.None);

        result.IsEmpty.Should().BeTrue();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_CustomPagingAndRadius_PassesParametersToProvider()
    {
        var destinationProvider = Substitute.For<IDestinationProvider>();
        destinationProvider.GetAttractionsAsync(
                Arg.Any<double>(), Arg.Any<double>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new AttractionSearchResultDto());
        var handler = new GetAttractionsQueryHandler(destinationProvider);

        await handler.Handle(
            new GetAttractionsQuery
            {
                Latitude = 10.5,
                Longitude = 20.5,
                RadiusMeters = 5_000,
                Page = 3,
                PageSize = 10
            },
            CancellationToken.None);

        await destinationProvider.Received(1).GetAttractionsAsync(10.5, 20.5, 5_000, 3, 10, Arg.Any<CancellationToken>());
    }
}
