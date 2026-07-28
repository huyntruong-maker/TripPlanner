using Application.Dtos.Destinations;
using Application.Features.Destinations.Queries.GetAttractionDetailQuery;
using Application.Interfaces.Providers;
using FluentAssertions;
using NSubstitute;

namespace Tests.Features.Destinations;

public class GetAttractionDetailQueryHandlerTests
{
    [Fact]
    public async Task Handle_AttractionNotFound_ReturnsNull()
    {
        var destinationProvider = Substitute.For<IDestinationProvider>();
        destinationProvider.GetAttractionDetailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((AttractionDto?)null);
        var handler = new GetAttractionDetailQueryHandler(destinationProvider);

        var result = await handler.Handle(
            new GetAttractionDetailQuery { ProviderPlaceId = "unknown-id" },
            CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_AttractionFound_MapsAllFieldsToDetailDto()
    {
        var destinationProvider = Substitute.For<IDestinationProvider>();
        var openingHours = new OpeningHoursDto { DisplayText = "Mon-Fri 09:00-17:00", IsOpenNow = true };
        var attraction = new AttractionDto
        {
            ProviderPlaceId = "xid-1",
            Name = "Eiffel Tower",
            Category = "cultural",
            Tags = ["landmark", "tower"],
            Description = "An iconic iron tower.",
            Photos = ["https://example.com/photo1.jpg"],
            Address = "Champ de Mars, Paris",
            Website = "https://www.toureiffel.paris",
            OpeningHours = openingHours,
            Rating = 9.2,
            Latitude = 48.8584,
            Longitude = 2.2945
        };
        destinationProvider.GetAttractionDetailAsync("xid-1", Arg.Any<CancellationToken>()).Returns(attraction);
        var handler = new GetAttractionDetailQueryHandler(destinationProvider);

        var result = await handler.Handle(
            new GetAttractionDetailQuery { ProviderPlaceId = "xid-1" },
            CancellationToken.None);

        result.Should().NotBeNull();
        result!.ProviderPlaceId.Should().Be(attraction.ProviderPlaceId);
        result.Name.Should().Be(attraction.Name);
        result.Category.Should().Be(attraction.Category);
        result.Tags.Should().BeEquivalentTo(attraction.Tags);
        result.Description.Should().Be(attraction.Description);
        result.Photos.Should().BeEquivalentTo(attraction.Photos);
        result.Address.Should().Be(attraction.Address);
        result.Website.Should().Be(attraction.Website);
        result.OpeningHours.Should().BeSameAs(openingHours);
        result.Rating.Should().Be(attraction.Rating);
        result.Latitude.Should().Be(attraction.Latitude);
        result.Longitude.Should().Be(attraction.Longitude);
    }

    [Fact]
    public async Task Handle_ValidRequest_PassesProviderPlaceIdToProvider()
    {
        var destinationProvider = Substitute.For<IDestinationProvider>();
        destinationProvider.GetAttractionDetailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((AttractionDto?)null);
        var handler = new GetAttractionDetailQueryHandler(destinationProvider);

        await handler.Handle(new GetAttractionDetailQuery { ProviderPlaceId = "fsq-42" }, CancellationToken.None);

        await destinationProvider.Received(1).GetAttractionDetailAsync("fsq-42", Arg.Any<CancellationToken>());
    }
}
