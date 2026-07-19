using Application.Dtos.Trips;
using Application.Features.Trips.Commands.AddDestinationToTripCommand;
using Application.Features.Trips.Commands.CreateTripCommand;
using Application.Features.Trips.Commands.MoveTripDestinationCommand;
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
    /// <summary>Returns the authenticated user's trip list; empty list when the user has no trips (F3-US10).</summary>
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

    /// <summary>Full trip detail (days + destinations); 404 when not found or not owned (NFR-6).</summary>
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

    /// <summary>Creates a trip without dates (F3-US1); use PUT /trips/{id}/dates to set them.</summary>
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

            var (errorCode, result) = await sender.Send(new CreateTripCommand
            {
                Name = request.Name.Trim(),
                UserId = userId.Value
            }, cancellationToken);

            if (!string.IsNullOrWhiteSpace(errorCode))
            {
                response.ErrorCode = errorCode;
                return BadRequest(response);
            }

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

    /// <summary>Sets the trip's date range (F3-US2); shortening it unschedules destinations that lose their day.</summary>
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

            var (errorCode, setDatesResult) = await sender.Send(new SetTripDatesCommand
            {
                TripId = id,
                UserId = userId.Value,
                StartDate = request.StartDate.Value,
                EndDate = request.EndDate.Value
            }, cancellationToken);

            if (!string.IsNullOrWhiteSpace(errorCode))
            {
                response.ErrorCode = errorCode;
                return NotFound(response);
            }

            response.Result = setDatesResult!.Trip;
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

    /// <summary>Adds a destination to the trip (F3-US3); a null <c>itineraryDayId</c> saves it to "Saved Places" (F3-US4).</summary>
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

            var (errorCode, result) = await sender.Send(new AddDestinationToTripCommand
            {
                TripId = id,
                UserId = userId.Value,
                ItineraryDayId = request.ItineraryDayId,
                ProviderPlaceId = request.ProviderPlaceId.Trim(),
                Name = request.Name.Trim(),
                Category = request.Category?.Trim(),
                ThumbnailUrl = request.ThumbnailUrl?.Trim(),
                Lat = request.Lat,
                Lng = request.Lng
            }, cancellationToken);

            if (!string.IsNullOrWhiteSpace(errorCode))
            {
                response.ErrorCode = errorCode;
                return errorCode == TripControllerMsg.AddDestination.DuplicateInDay
                    ? BadRequest(response)
                    : NotFound(response);
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

    /// <summary>Moves/reorders a destination between itinerary days or Saved Places (F3-US4/US5/US6); returns full trip detail.</summary>
    [HttpPut("{id:guid}/destinations/{tripDestinationId:guid}")]
    [ProducesResponseType(typeof(ResultRes<TripDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResultRes<TripDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ResultRes<TripDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> MoveDestination(
        ISender sender,
        [FromRoute] Guid id,
        [FromRoute] Guid tripDestinationId,
        [FromBody] MoveTripDestinationReq request,
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

            var (errorCode, result) = await sender.Send(new MoveTripDestinationCommand
            {
                TripId = id,
                TripDestinationId = tripDestinationId,
                UserId = userId.Value,
                ItineraryDayId = request.ItineraryDayId,
                Position = request.Position
            }, cancellationToken);

            if (!string.IsNullOrWhiteSpace(errorCode))
            {
                response.ErrorCode = errorCode;
                return errorCode == TripControllerMsg.MoveDestination.DuplicateInDay
                    ? BadRequest(response)
                    : NotFound(response);
            }

            response.Result = result;
            response.Success = true;
            return Ok(response);
        }
        catch (Exception ex)
        {
            response.ErrorCode = TripControllerMsg.MoveDestination.Exception;
            Logger.LogError("MoveDestination failed for trip '{TripId}', destination '{DestId}': {Ex}",
                id, tripDestinationId, ex);
            return InternalServerError(response);
        }
    }

    /// <summary>Removes a destination from the trip (F3-US7); 404 when not found/owned (NFR-6).</summary>
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

            var (errorCode, removed) = await sender.Send(new RemoveDestinationFromTripCommand
            {
                TripId = id,
                TripDestinationId = tripDestinationId,
                UserId = userId.Value
            }, cancellationToken);

            if (!removed)
            {
                response.ErrorCode = errorCode;
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

    /// <summary>Extracts the user ID from the JWT claim; null only if absent (unexpected for [Authorize]).</summary>
    private Guid? GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var userId) ? userId : null;
    }
}
