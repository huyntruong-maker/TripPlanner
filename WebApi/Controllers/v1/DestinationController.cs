using Application.Features.Destinations.Queries.GetAttractionsQuery;
using Application.Features.Destinations.Queries.SearchLocationsQuery;
using Asp.Versioning;
using AutoMapper;
using Domain.Messages;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using WebApi.Models.Responses.Base;

namespace WebApi.Controllers.v1;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/destinations")]
public class DestinationController(
    ILogger<DestinationController> logger,
    IMapper mapper) : BaseController(logger, mapper)
{
    /// <summary>
    /// Searches for cities or countries matching the given query text.
    /// Returns up to 5 ranked results; exact matches appear first.
    /// </summary>
    /// <param name="sender">Injected by ASP.NET Core endpoint DI per-request.</param>
    /// <param name="query">Partial or full city/country name (at least 1 character).</param>
    /// <param name="maxResults">Maximum number of results to return (default: 5, max: 5).</param>
    /// <param name="cancellationToken">Propagates request cancellation.</param>
    [HttpGet("locations/search")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ResultRes<Application.Dtos.Destinations.LocationSearchResultDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchLocations(
        ISender sender,
        [FromQuery] string? query,
        [FromQuery] int maxResults = 5,
        CancellationToken cancellationToken = default)
    {
        var response = new ResultRes<Application.Dtos.Destinations.LocationSearchResultDto>();

        try
        {
            response.Success = false;

            if (string.IsNullOrWhiteSpace(query))
            {
                response.ErrorCode = DestinationControllerMsg.SearchLocations.QueryRequired;
                return BadRequest(response);
            }

            var result = await sender.Send(new SearchLocationsQuery
            {
                Query = query.Trim(),
                MaxResults = Math.Clamp(maxResults, 1, 5)
            }, cancellationToken);

            response.Result = result;
            response.Success = true;
            return Ok(response);
        }
        catch (Exception ex)
        {
            response.ErrorCode = DestinationControllerMsg.SearchLocations.Exception;
            Logger.LogError("SearchLocations failed: {Ex}", ex);
            return InternalServerError(response);
        }
    }

    /// <summary>
    /// Returns a ranked, paginated list of attractions near the given coordinates.
    /// Default search radius is 20 km (city-level). Returns an empty list when no
    /// attractions are found rather than an error.
    /// </summary>
    /// <param name="sender">Injected by ASP.NET Core endpoint DI per-request.</param>
    /// <param name="latitude">Latitude of the search centre (required).</param>
    /// <param name="longitude">Longitude of the search centre (required).</param>
    /// <param name="radiusMeters">Search radius in metres (default: 20 000).</param>
    /// <param name="page">1-based page number (default: 1).</param>
    /// <param name="pageSize">Items per page (default: 20, max: 20).</param>
    /// <param name="cancellationToken">Propagates request cancellation.</param>
    [HttpGet("attractions")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ResultRes<Application.Dtos.Destinations.AttractionSearchResultDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAttractions(
        ISender sender,
        [FromQuery] double? latitude,
        [FromQuery] double? longitude,
        [FromQuery] int radiusMeters = 20_000,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var response = new ResultRes<Application.Dtos.Destinations.AttractionSearchResultDto>();

        try
        {
            response.Success = false;

            if (latitude is null)
            {
                response.ErrorCode = DestinationControllerMsg.GetAttractions.LatitudeRequired;
                return BadRequest(response);
            }

            if (longitude is null)
            {
                response.ErrorCode = DestinationControllerMsg.GetAttractions.LongitudeRequired;
                return BadRequest(response);
            }

            if (latitude < -90 || latitude > 90 || longitude < -180 || longitude > 180)
            {
                response.ErrorCode = DestinationControllerMsg.GetAttractions.InvalidCoordinates;
                return BadRequest(response);
            }

            var result = await sender.Send(new GetAttractionsQuery
            {
                Latitude = latitude.Value,
                Longitude = longitude.Value,
                RadiusMeters = Math.Max(1, radiusMeters),
                Page = Math.Max(1, page),
                PageSize = Math.Clamp(pageSize, 1, 20)
            }, cancellationToken);

            response.Result = result;
            response.Success = true;
            return Ok(response);
        }
        catch (Exception ex)
        {
            response.ErrorCode = DestinationControllerMsg.GetAttractions.Exception;
            Logger.LogError("GetAttractions failed: {Ex}", ex);
            return InternalServerError(response);
        }
    }
}
