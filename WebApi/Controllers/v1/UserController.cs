using Application.Features.Users.Commands.ChangeProfileCommand;
using Application.Features.Users.Commands.CreateUserCommand;
using Application.Features.Users.Commands.DeactivateUserCommand;
using Application.Features.Users.Commands.ResetUserPasswordCommand;
using Application.Features.Users.Commands.UpdateUserCommand;
using Application.Features.Users.Queries.GetUserProfileQuery;
using Application.Features.Users.Queries.GetUserQuery;
using Application.Features.Users.Queries.GetUsersQuery;
using Asp.Versioning;
using AutoMapper;
using Domain.Constants;
using Domain.Helpers;
using Domain.Messages;
using MediatR;
using Microsoft.AspNetCore.Authorization;
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
    [Authorize(Policy = PermissionConstants.Users.ViewUsers)]
    [ProducesResponseType(typeof(PaginationResultRes<List<UsersRes>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(ISender sender, [FromQuery] UsersSearchReq search)
    {
        var response = new PaginationResultRes<List<UsersRes>>();

        try
        {
            var (errorCode, result) = await sender.Send(new GetUsersQuery
            {
                SearchDto = Mapper.Map<UsersSearchDto>(search)
            });

            if (!string.IsNullOrWhiteSpace(errorCode))
            {
                response.Success = false;
                response.ErrorCode = errorCode;
                return BadRequest(response);
            }

            response.Result = Mapper.Map<List<UsersRes>>(result!.Items);
            response.Pagination = new PaginationRes
            {
                PageNumber = result.PageNumber,
                PageSize = result.PageSize,
                TotalPages = result.TotalPages,
                TotalItems = result.TotalItems,
            };

            response.Success = true;
            return Ok(response);
        }
        catch (Exception ex)
        {
            Logger.LogError("GetAll user failed {ex}", ex);
            response.ErrorCode = UserControllerMsg.Get.Exception;
            return InternalServerError(response);
        }
    }


    [HttpGet]
    [Route("{id}")]
    [Authorize(Policy = PermissionConstants.Users.ViewUsers)]
    [ProducesResponseType(typeof(ResultRes<UserRes>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(ISender sender, Guid id)
    {
        var response = new ResultRes<UserRes>();

        try
        {
            var (errorCode, result) = await sender.Send(new GetUserQuery
            {
                Id = id
            });

            if (!string.IsNullOrWhiteSpace(errorCode))
            {
                response.Success = false;
                response.ErrorCode = errorCode;
                return BadRequest(response);
            }

            response.Result = Mapper.Map<UserRes>(result);
            response.Success = true;
            return Ok(response);
        }
        catch (Exception ex)
        {
            Logger.LogError("Get user failed {id}: {ex}", id, ex);
            response.ErrorCode = UserControllerMsg.Get.Exception;
            return InternalServerError(response);
        }
    }

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

    [HttpDelete]
    [Route("{id}/deactivate")]
    [Authorize(Policy = PermissionConstants.Users.DeactivateUser)]
    [ProducesResponseType(typeof(ResultRes<string>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Deactivate(ISender sender, [FromRoute] Guid id)
    {
        var response = new ResultRes<string>();

        try
        {
            var errorCode = await sender.Send(new DeactivateUserCommand
            {
                Id = id
            });

            if (!string.IsNullOrWhiteSpace(errorCode))
            {
                response.Success = false;
                response.ErrorCode = errorCode;
                return BadRequest(response);
            }

            response.Success = true;
            return Ok(response);
        }
        catch (Exception ex)
        {
            Logger.LogError("Deactivate user failed {ex}", ex);
            response.ErrorCode = UserControllerMsg.Deactivate.Exception;
            return InternalServerError(response);
        }
    }


    [HttpPost]
    [Authorize(Policy = PermissionConstants.Users.CreateUser)]
    [ProducesResponseType(typeof(ResultRes<string>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Create(ISender sender, [FromBody] CreateUserReq request)
    {
        var response = new ResultRes<string>();

        try
        {
            response.Success = false;
            request.TrimData(logger);

            if (string.IsNullOrWhiteSpace(request.UserName)
                || string.IsNullOrWhiteSpace(request.FirstName)
                || string.IsNullOrWhiteSpace(request.Email)
                || string.IsNullOrWhiteSpace(request.Password)
                || request.RoleIds.Length == 0)
            {
                response.ErrorCode = UserControllerMsg.Create.EmptyField;
                return BadRequest(response);
            }

            if (!request.Password.ValidatePasswordPolicy())
            {
                response.ErrorCode = UserControllerMsg.Create.PasswordNotStrongEnough;
                return BadRequest(response);
            }

            if (!request.Password.Equals(request.ConfirmPassword))
            {
                response.ErrorCode = UserControllerMsg.Create.ConfirmPasswordNotMatch;
                return BadRequest(response);
            }

            if (!request.Email.IsValidEmail())
            {
                response.ErrorCode = UserControllerMsg.Create.InvalidEmailFormat;
                return BadRequest(response);
            }

            var errorCode = await sender.Send(new CreateUserCommand
            {
                CreateUserReqDto = Mapper.Map<CreateUserReqDto>(request)
            });

            if (!string.IsNullOrWhiteSpace(errorCode))
            {
                response.ErrorCode = errorCode;
                return BadRequest(response);
            }

            response.Success = true;
            return Ok(response);
        }
        catch (Exception ex)
        {
            Logger.LogError("Create user failed {ex}", ex);
            response.ErrorCode = UserControllerMsg.Create.Exception;
            return InternalServerError(response);
        }
    }

    [HttpPut]
    [Route("{id}")]
    [Authorize(Policy = PermissionConstants.Users.UpdateUser)]
    [ProducesResponseType(typeof(ResultRes<string>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(ISender sender, [FromRoute] Guid id, [FromBody] UpdateUserReq request)
    {
        var response = new ResultRes<string>();

        try
        {
            response.Success = false;
            request.TrimData(logger);

            if (string.IsNullOrWhiteSpace(request.UserName)
                || string.IsNullOrWhiteSpace(request.FirstName)
                || string.IsNullOrWhiteSpace(request.Email)
                || request.RoleIds.Length == 0)
            {
                response.ErrorCode = UserControllerMsg.Update.EmptyField;
                return BadRequest(response);
            }

            if (!request.Email.IsValidEmail())
            {
                response.ErrorCode = UserControllerMsg.Update.InvalidEmailFormat;
                return BadRequest(response);
            }

            var errorCode = await sender.Send(new UpdateUserCommand
            {
                Id = id,
                UpdateUserReqDto = Mapper.Map<UpdateUserReqDto>(request)
            });

            if (!string.IsNullOrWhiteSpace(errorCode))
            {
                response.ErrorCode = errorCode;
                return BadRequest(response);
            }

            response.Success = true;
            return Ok(response);
        }
        catch (Exception ex)
        {
            Logger.LogError("Update user failed {ex}", ex);
            response.ErrorCode = UserControllerMsg.Update.Exception;
            return InternalServerError(response);
        }
    }

    [HttpPost]
    [Route("{id}/reset-password")]
    [Authorize(Policy = PermissionConstants.Users.ResetPassUser)]
    [ProducesResponseType(typeof(ResultRes<string>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ResetPassword(ISender sender, [FromRoute] Guid id)
    {
        var response = new ResultRes<string>();

        try
        {
            var errorCode = await sender.Send(new ResetUserPasswordCommand
            {
                Id = id
            });

            if (!string.IsNullOrWhiteSpace(errorCode))
            {
                response.Success = false;
                response.ErrorCode = errorCode;
                return BadRequest(response);
            }

            response.Success = true;
            return Ok(response);
        }
        catch (Exception ex)
        {
            Logger.LogError("Reset password failed for user {id}: {ex}", id, ex);
            response.ErrorCode = UserControllerMsg.ResetPassword.Exception;
            return InternalServerError(response);
        }
    }
}