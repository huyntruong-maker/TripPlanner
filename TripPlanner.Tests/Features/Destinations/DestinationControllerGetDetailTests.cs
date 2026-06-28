using Application.Dtos.Destinations;
using Application.Features.Destinations.Queries.GetAttractionDetailQuery;
using AutoMapper;
using Domain.Messages;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using WebApi.Controllers.v1;
using WebApi.Models.Responses.Base;
using FluentAssertions;

namespace TripPlanner.Tests.Features.Destinations;

public class DestinationControllerGetDetailTests
{
    private readonly ISender _sender = Substitute.For<ISender>();
    private readonly DestinationController _controller;

    public DestinationControllerGetDetailTests()
    {
        var logger = Substitute.For<ILogger<DestinationController>>();
        var mapper = Substitute.For<IMapper>();
        _controller = new DestinationController(logger, mapper);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetDestinationDetail_NullOrWhitespaceProviderPlaceId_ReturnsBadRequest(string? placeId)
    {
        // Act
        var actionResult = await _controller.GetDestinationDetail(_sender, placeId, CancellationToken.None);

        // Assert
        var badRequest = actionResult.Should().BeOfType<BadRequestObjectResult>().Subject;
        var response = badRequest.Value.Should().BeOfType<ResultRes<DestinationDetailDto>>().Subject;
        response.Success.Should().BeFalse();
        response.ErrorCode.Should().Be(DestinationControllerMsg.GetDetail.ProviderPlaceIdRequired);
        await _sender.DidNotReceive().Send(Arg.Any<GetAttractionDetailQuery>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetDestinationDetail_ProviderReturnsNull_ReturnsNotFound()
    {
        // Arrange
        const string placeId = "UNKNOWN_XID";
        _sender.Send(Arg.Any<GetAttractionDetailQuery>(), Arg.Any<CancellationToken>())
               .Returns((DestinationDetailDto?)null);

        // Act
        var actionResult = await _controller.GetDestinationDetail(_sender, placeId, CancellationToken.None);

        // Assert
        var notFound = actionResult.Should().BeOfType<NotFoundObjectResult>().Subject;
        var response = notFound.Value.Should().BeOfType<ResultRes<DestinationDetailDto>>().Subject;
        response.Success.Should().BeFalse();
        response.ErrorCode.Should().Be(DestinationControllerMsg.GetDetail.NotFound);
    }

    [Fact]
    public async Task GetDestinationDetail_ProviderThrows_ReturnsInternalServerError()
    {
        // Arrange
        const string placeId = "W123456";
        _sender.Send(Arg.Any<GetAttractionDetailQuery>(), Arg.Any<CancellationToken>())
               .Throws(new InvalidOperationException("provider unavailable"));

        // Act
        var actionResult = await _controller.GetDestinationDetail(_sender, placeId, CancellationToken.None);

        // Assert
        var statusResult = actionResult.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
        var response = statusResult.Value.Should().BeOfType<ResultRes<DestinationDetailDto>>().Subject;
        response.Success.Should().BeFalse();
        response.ErrorCode.Should().Be(DestinationControllerMsg.GetDetail.Exception);
    }

    [Fact]
    public async Task GetDestinationDetail_ProviderReturnsDetail_Returns200WithPopulatedResult()
    {
        // Arrange
        const string placeId = "W123456";
        var detail = new DestinationDetailDto
        {
            ProviderPlaceId = placeId,
            Name = "Eiffel Tower",
            Category = "cultural",
            Tags = ["landmark"],
            Description = "Famous iron tower.",
            Photos = ["https://cdn.example.com/photo.jpg"],
            Address = "Paris, France",
            Website = "https://toureiffel.paris",
            OpeningHours = new OpeningHoursDto { DisplayText = "Daily 09:00-23:00" },
            Rating = 9.5,
            Latitude = 48.8584,
            Longitude = 2.2945
        };
        _sender.Send(Arg.Any<GetAttractionDetailQuery>(), Arg.Any<CancellationToken>())
               .Returns(detail);

        // Act
        var actionResult = await _controller.GetDestinationDetail(_sender, placeId, CancellationToken.None);

        // Assert
        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ResultRes<DestinationDetailDto>>().Subject;
        response.Success.Should().BeTrue();
        response.ErrorCode.Should().BeNull();
        response.Result.Should().NotBeNull();
        response.Result!.Name.Should().Be("Eiffel Tower");
        response.Result.Photos.Should().HaveCount(1);
        response.Result.OpeningHours!.DisplayText.Should().Be("Daily 09:00-23:00");
    }

    [Fact]
    public async Task GetDestinationDetail_ProviderReturnsPartialDetail_Returns200WithNullOptionalFields()
    {
        // Arrange — destination exists but has no optional data (graceful partial data / F2-US1 rule)
        const string placeId = "W000001";
        var detail = new DestinationDetailDto
        {
            ProviderPlaceId = placeId,
            Name = "Mystery Ruin",
            Latitude = 10.0,
            Longitude = 20.0
        };
        _sender.Send(Arg.Any<GetAttractionDetailQuery>(), Arg.Any<CancellationToken>())
               .Returns(detail);

        // Act
        var actionResult = await _controller.GetDestinationDetail(_sender, placeId, CancellationToken.None);

        // Assert
        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ResultRes<DestinationDetailDto>>().Subject;
        response.Success.Should().BeTrue();
        response.Result!.Description.Should().BeNull();
        response.Result.Photos.Should().BeEmpty();
        response.Result.OpeningHours.Should().BeNull();
        response.Result.Address.Should().BeNull();
        response.Result.Website.Should().BeNull();
    }
}
