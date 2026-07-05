using Application.Features.Users.Commands.ChangeProfileCommand;
using Application.Features.Users.Queries.GetUserProfileQuery;
using Asp.Versioning;
using AutoMapper;
using Domain.Helpers;
using Domain.Messages;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using WebApi.Models.Requests.User;
using WebApi.Models.Responses.Base;
using WebApi.Models.Responses.User;

namespace WebApi.Controllers.v1;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/users")]
public class UserController(
    ILogger<UserController> logger,
    IMapper mapper) : BaseController(logger, mapper)
{
    [HttpGet]
    [Route("profile")]
    [ProducesResponseType(typeof(ResultRes<UserProfileRes>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProfile(ISender sender)
    {
        var response = new ResultRes<UserProfileRes>();

        try
        {
            var (errorCode, result) = await sender.Send(new GetUserProfileQuery());

            if (!string.IsNullOrWhiteSpace(errorCode))
            {
                response.Success = false;
                response.ErrorCode = errorCode;
                return BadRequest(response);
            }

            response.Result = Mapper.Map<UserProfileRes>(result);
            response.Success = true;
            return Ok(response);
        }
        catch (Exception ex)
        {
            Logger.LogError("Get user profile failed {ex}", ex);
            response.ErrorCode = UserControllerMsg.GetProfile.Exception;
            return InternalServerError(response);
        }
    }

    [HttpPut]
    [Route("profile")]
    [ProducesResponseType(typeof(ResultRes<string>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ChangeProfile(ISender sender, ChangeProfileReq request)
    {
        var response = new ResultRes<string>();
        try
        {
            response.Success = false;
            request.TrimData(logger);

            if (string.IsNullOrWhiteSpace(request.UserName)
                || string.IsNullOrWhiteSpace(request.FirstName)
                || string.IsNullOrWhiteSpace(request.Email))
            {
                response.ErrorCode = UserControllerMsg.ChangeProfile.EmptyField;
                return BadRequest(response);
            }

            if (!request.Email.IsValidEmail())
            {
                response.ErrorCode = UserControllerMsg.ChangeProfile.InvalidEmailFormat;
                return BadRequest(response);
            }

            var result = await sender.Send(Mapper.Map<ChangeProfileCommand>(request));
            if (!string.IsNullOrWhiteSpace(result))
            {
                response.ErrorCode = result;
                return BadRequest(response);
            }

            response.Success = true;
            return Ok(response);
        }
        catch (Exception ex)
        {
            Logger.LogError("Change user profile failed {ex}", ex);
            response.ErrorCode = UserControllerMsg.ChangeProfile.Exception;
            return InternalServerError(response);
        }
    }
}
