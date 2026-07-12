using Application.Dtos.Trips;
using Application.Features.Trips.Commands.AddDestinationToTripCommand;
using Application.Features.Trips.Commands.CreateTripCommand;
using Application.Features.Trips.Commands.RemoveDestinationFromTripCommand;
using Application.Features.Trips.Commands.SetTripDatesCommand;
using Application.Features.Trips.Queries.GetTripDetailQuery;
using Application.Features.Trips.Queries.GetTripsQuery;
using Asp.Versioning;
using AutoMapper;
using Domain.Messages;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using WebApi.Models.Requests.Trip;
using WebApi.Models.Responses.Base;

namespace WebApi.Controllers.v1;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/trips")]
[Authorize]
public class TripsController(
    ILogger<TripsController> logger,
    IMapper mapper) : BaseController(logger, mapper)
{
    /// <summary>
    /// Returns the authenticated user's trip list. Empty list when the user has no trips (F3-US10).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ResultRes<IReadOnlyList<TripDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetTrips(
        ISender sender,
        CancellationToken cancellationToken = default)
    {
        var response = new ResultRes<IReadOnlyList<TripDto>>();

        try
        {
            response.Success = false;

            var userId = GetCurrentUserId();
            if (userId is null)
            {
                response.ErrorCode = TripControllerMsg.NotFound;
                return Unauthorized(response);
            }

            var result = await sender.Send(new GetTripsQuery { UserId = userId.Value }, cancellationToken);

            response.Result = result;
            response.Success = true;
            return Ok(response);
        }
        catch (Exception ex)
        {
            response.ErrorCode = TripControllerMsg.GetTrips.Exception;
            Logger.LogError("GetTrips failed: {Ex}", ex);
            return InternalServerError(response);
        }
    }

    /// <summary>
    /// Returns full detail for a single trip (itinerary days + destinations). 404 when the trip
    /// does not exist or does not belong to the caller (NFR-6 — no enumeration).
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ResultRes<TripDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResultRes<TripDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetTripDetail(
        ISender sender,
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        var response = new ResultRes<TripDto>();

        try
        {
            response.Success = false;

            var userId = GetCurrentUserId();
            if (userId is null)
            {
                response.ErrorCode = TripControllerMsg.NotFound;
                return Unauthorized(response);
            }

            var result = await sender.Send(new GetTripDetailQuery
            {
                TripId = id,
                UserId = userId.Value
            }, cancellationToken);

            if (result is null)
            {
                response.ErrorCode = TripControllerMsg.NotFound;
                return NotFound(response);
            }

            response.Result = result;
            response.Success = true;
            return Ok(response);
        }
        catch (Exception ex)
        {
            response.ErrorCode = TripControllerMsg.GetDetail.Exception;
            Logger.LogError("GetTripDetail failed for trip '{Id}': {Ex}", id, ex);
            return InternalServerError(response);
        }
    }

    /// <summary>
    /// Creates a new trip for the authenticated user (F3-US1). Trip name is required.
    /// The trip is created without dates; call PUT /trips/{id}/dates to set dates.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ResultRes<TripDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ResultRes<TripDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateTrip(
        ISender sender,
        [FromBody] CreateTripReq request,
        CancellationToken cancellationToken = default)
    {
        var response = new ResultRes<TripDto>();

        try
        {
            response.Success = false;

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                response.ErrorCode = TripControllerMsg.CreateTrip.NameRequired;
                return BadRequest(response);
            }

            var userId = GetCurrentUserId();
            if (userId is null)
            {
                response.ErrorCode = TripControllerMsg.NotFound;
                return Unauthorized(response);
            }

            var result = await sender.Send(new CreateTripCommand
            {
                Name = request.Name.Trim(),
                UserId = userId.Value
            }, cancellationToken);

            response.Result = result;
            response.Success = true;
            return StatusCode(StatusCodes.Status201Created, response);
        }
        catch (Exception ex)
        {
            response.ErrorCode = TripControllerMsg.CreateTrip.Exception;
            Logger.LogError("CreateTrip failed: {Ex}", ex);
            return InternalServerError(response);
        }
    }

    /// <summary>
    /// Sets or updates the trip's date range (F3-US2). Generates one ItineraryDay per calendar date.
    /// When the new range is shorter, scheduled destinations that lose their day become unscheduled
    /// (moved to Saved Places). The response includes a warning code when this occurs.
    /// </summary>
    [HttpPut("{id:guid}/dates")]
    [ProducesResponseType(typeof(ResultRes<TripDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResultRes<TripDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ResultRes<TripDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SetTripDates(
        ISender sender,
        [FromRoute] Guid id,
        [FromBody] SetTripDatesReq request,
        CancellationToken cancellationToken = default)
    {
        var response = new ResultRes<TripDto>();

        try
        {
            response.Success = false;

            if (request.StartDate is null)
            {
                response.ErrorCode = TripControllerMsg.SetDates.StartDateRequired;
                return BadRequest(response);
            }

            if (request.EndDate is null)
            {
                response.ErrorCode = TripControllerMsg.SetDates.EndDateRequired;
                return BadRequest(response);
            }

            if (request.StartDate.Value > request.EndDate.Value)
            {
                response.ErrorCode = TripControllerMsg.SetDates.InvalidDateRange;
                return BadRequest(response);
            }

            var userId = GetCurrentUserId();
            if (userId is null)
            {
                response.ErrorCode = TripControllerMsg.NotFound;
                return Unauthorized(response);
            }

            var setDatesResult = await sender.Send(new SetTripDatesCommand
            {
                TripId = id,
                UserId = userId.Value,
                StartDate = request.StartDate.Value,
                EndDate = request.EndDate.Value
            }, cancellationToken);

            if (setDatesResult is null)
            {
                response.ErrorCode = TripControllerMsg.NotFound;
                return NotFound(response);
            }

            response.Result = setDatesResult.Trip;
            response.Success = true;

            // Warn the caller when scheduled destinations were moved to Saved Places.
            if (setDatesResult.DestinationsUnscheduledCount > 0)
                response.ErrorCode = TripControllerMsg.DatesDestinationsUnscheduled;

            return Ok(response);
        }
        catch (Exception ex)
        {
            response.ErrorCode = TripControllerMsg.SetDates.Exception;
            Logger.LogError("SetTripDates failed for trip '{Id}': {Ex}", id, ex);
            return InternalServerError(response);
        }
    }

    /// <summary>
    /// Adds a destination to a specific itinerary day within the trip (F3-US3).
    /// The itinerary day must belong to this trip (prevents cross-trip injection).
    /// </summary>
    [HttpPost("{id:guid}/destinations")]
    [ProducesResponseType(typeof(ResultRes<TripDestinationDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ResultRes<TripDestinationDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ResultRes<TripDestinationDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> AddDestination(
        ISender sender,
        [FromRoute] Guid id,
        [FromBody] AddDestinationToTripReq request,
        CancellationToken cancellationToken = default)
    {
        var response = new ResultRes<TripDestinationDto>();

        try
        {
            response.Success = false;

            if (request.ItineraryDayId is null)
            {
                response.ErrorCode = TripControllerMsg.AddDestination.ItineraryDayIdRequired;
                return BadRequest(response);
            }

            if (string.IsNullOrWhiteSpace(request.ProviderPlaceId))
            {
                response.ErrorCode = TripControllerMsg.AddDestination.ProviderPlaceIdRequired;
                return BadRequest(response);
            }

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                response.ErrorCode = TripControllerMsg.AddDestination.NameRequired;
                return BadRequest(response);
            }

            var userId = GetCurrentUserId();
            if (userId is null)
            {
                response.ErrorCode = TripControllerMsg.NotFound;
                return Unauthorized(response);
            }

            var result = await sender.Send(new AddDestinationToTripCommand
            {
                TripId = id,
                UserId = userId.Value,
                ItineraryDayId = request.ItineraryDayId.Value,
                ProviderPlaceId = request.ProviderPlaceId.Trim(),
                Name = request.Name.Trim(),
                Category = request.Category?.Trim(),
                ThumbnailUrl = request.ThumbnailUrl?.Trim(),
                Lat = request.Lat,
                Lng = request.Lng
            }, cancellationToken);

            if (result is null)
            {
                // Null means either the trip was not found/owned, or the itinerary day does not
                // belong to this trip. Return 404 with NotFound to avoid leaking existence.
                response.ErrorCode = TripControllerMsg.NotFound;
                return NotFound(response);
            }

            response.Result = result;
            response.Success = true;
            return StatusCode(StatusCodes.Status201Created, response);
        }
        catch (Exception ex)
        {
            response.ErrorCode = TripControllerMsg.AddDestination.Exception;
            Logger.LogError("AddDestination failed for trip '{Id}': {Ex}", id, ex);
            return InternalServerError(response);
        }
    }

    /// <summary>
    /// Removes a destination from the trip (F3-US7). Both the trip and destination must belong to
    /// the authenticated user (NFR-6). Returns 404 when not found to prevent enumeration.
    /// </summary>
    [HttpDelete("{id:guid}/destinations/{tripDestinationId:guid}")]
    [ProducesResponseType(typeof(ResultRes<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResultRes<bool>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RemoveDestination(
        ISender sender,
        [FromRoute] Guid id,
        [FromRoute] Guid tripDestinationId,
        CancellationToken cancellationToken = default)
    {
        var response = new ResultRes<bool>();

        try
        {
            response.Success = false;

            var userId = GetCurrentUserId();
            if (userId is null)
            {
                response.ErrorCode = TripControllerMsg.NotFound;
                return Unauthorized(response);
            }

            var removed = await sender.Send(new RemoveDestinationFromTripCommand
            {
                TripId = id,
                TripDestinationId = tripDestinationId,
                UserId = userId.Value
            }, cancellationToken);

            if (!removed)
            {
                response.ErrorCode = TripControllerMsg.NotFound;
                return NotFound(response);
            }

            response.Result = true;
            response.Success = true;
            return Ok(response);
        }
        catch (Exception ex)
        {
            response.ErrorCode = TripControllerMsg.RemoveDestination.Exception;
            Logger.LogError("RemoveDestination failed for trip '{TripId}', destination '{DestId}': {Ex}",
                id, tripDestinationId, ex);
            return InternalServerError(response);
        }
    }

    /// <summary>
    /// Extracts the authenticated user's ID from the JWT NameIdentifier claim.
    /// Returns null only when the claim is absent (should not happen for [Authorize] endpoints).
    /// </summary>
    private Guid? GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var userId) ? userId : null;
    }
}
