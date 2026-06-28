using Application.Dtos.Destinations;
using Application.Features.Destinations.Queries.GetAttractionDetailQuery;
using Application.Interfaces.Providers;
using FluentAssertions;
using NSubstitute;

namespace TripPlanner.Tests.Features.Destinations;

public class GetAttractionDetailQueryHandlerTests
{
    private readonly IDestinationProvider _provider = Substitute.For<IDestinationProvider>();
    private readonly GetAttractionDetailQueryHandler _handler;

    public GetAttractionDetailQueryHandlerTests()
    {
        _handler = new GetAttractionDetailQueryHandler(_provider);
    }

    [Fact]
    public async Task Handle_ProviderReturnsFullData_ReturnsMappedDestinationDetailDto()
    {
        // Arrange
        const string placeId = "W123456";
        var attraction = new AttractionDto
        {
            ProviderPlaceId = placeId,
            Name = "Eiffel Tower",
            Category = "cultural",
            Tags = ["cultural", "landmark"],
            Description = "Famous iron lattice tower in Paris.",
            Photos = ["https://cdn.example.com/photo1.jpg", "https://cdn.example.com/photo2.jpg"],
            Address = "Champ de Mars, Paris, France",
            Website = "https://toureiffel.paris",
            OpeningHours = new OpeningHoursDto
            {
                DisplayText = "Daily 09:00-23:00",
                WeekdayText = ["Monday: 09:00 – 23:00"],
                IsOpenNow = true
            },
            Rating = 9.5,
            Latitude = 48.8584,
            Longitude = 2.2945
        };
        _provider.GetAttractionDetailAsync(placeId, Arg.Any<CancellationToken>())
                 .Returns(attraction);

        // Act
        var result = await _handler.Handle(new GetAttractionDetailQuery { ProviderPlaceId = placeId }, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.ProviderPlaceId.Should().Be(placeId);
        result.Name.Should().Be("Eiffel Tower");
        result.Category.Should().Be("cultural");
        result.Tags.Should().BeEquivalentTo(["cultural", "landmark"]);
        result.Description.Should().Be("Famous iron lattice tower in Paris.");
        result.Photos.Should().HaveCount(2);
        result.Address.Should().Be("Champ de Mars, Paris, France");
        result.Website.Should().Be("https://toureiffel.paris");
        result.OpeningHours.Should().NotBeNull();
        result.OpeningHours!.DisplayText.Should().Be("Daily 09:00-23:00");
        result.OpeningHours.IsOpenNow.Should().BeTrue();
        result.Rating.Should().Be(9.5);
        result.Latitude.Should().Be(48.8584);
        result.Longitude.Should().Be(2.2945);
    }

    [Fact]
    public async Task Handle_ProviderReturnsNull_ReturnsNull()
    {
        // Arrange
        const string placeId = "UNKNOWN_XID";
        _provider.GetAttractionDetailAsync(placeId, Arg.Any<CancellationToken>())
                 .Returns((AttractionDto?)null);

        // Act
        var result = await _handler.Handle(new GetAttractionDetailQuery { ProviderPlaceId = placeId }, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ProviderReturnsPartialData_OptionalFieldsAreNullOrEmpty()
    {
        // Arrange — only required fields populated; all optional fields absent (graceful partial data)
        const string placeId = "W000001";
        var attraction = new AttractionDto
        {
            ProviderPlaceId = placeId,
            Name = "Unknown Ruin",
            Category = null,
            Tags = [],
            Description = null,
            Photos = [],
            Address = null,
            Website = null,
            OpeningHours = null,
            Rating = null,
            Latitude = 10.0,
            Longitude = 20.0
        };
        _provider.GetAttractionDetailAsync(placeId, Arg.Any<CancellationToken>())
                 .Returns(attraction);

        // Act
        var result = await _handler.Handle(new GetAttractionDetailQuery { ProviderPlaceId = placeId }, CancellationToken.None);

        // Assert — the DTO is returned with optional fields null/empty rather than throwing
        result.Should().NotBeNull();
        result!.Name.Should().Be("Unknown Ruin");
        result.Category.Should().BeNull();
        result.Tags.Should().BeEmpty();
        result.Description.Should().BeNull();
        result.Photos.Should().BeEmpty();
        result.Address.Should().BeNull();
        result.Website.Should().BeNull();
        result.OpeningHours.Should().BeNull();
        result.Rating.Should().BeNull();
    }
}
