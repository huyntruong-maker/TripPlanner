using Application.Features.Auth.Commands.ChangePasswordCommand;
using Application.Features.Auth.Commands.ForgotPasswordCommand;
using Application.Features.Auth.Commands.LoginCommand;
using Application.Features.Auth.Commands.LogoutCommand;
using Application.Features.Auth.Commands.RefreshTokenCommand;
using Application.Features.Auth.Commands.RegisterCommand;
using Application.Features.Auth.Commands.ResetPasswordCommand;
using Application.Features.Auth.Commands.VerifyEmailCommand;
using Asp.Versioning;
using AutoMapper;
using Domain.Helpers;
using Domain.Messages;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using WebApi.Models.Requests.Auth;
using WebApi.Models.Responses.Auth;
using WebApi.Models.Responses.Base;

namespace WebApi.Controllers.v1;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/auth")]
public class AuthController(
    ILogger<AuthController> logger,
    IMapper mapper) : BaseController(logger, mapper)
{
    [HttpPost]
    [Route("login")]
    [ProducesResponseType(typeof(ResultRes<LoginRes>), StatusCodes.Status200OK)]
    [AllowAnonymous]
    public async Task<IActionResult> Login(ISender sender, LoginReq request)
    {
        var response = new ResultRes<LoginRes>();

        try
        {
            response.Success = false;
            request.TrimData(logger);

            if (string.IsNullOrWhiteSpace(request.Username)
                || string.IsNullOrWhiteSpace(request.Password))
            {
                response.ErrorCode = AuthControllerMsg.Login.EmptyField;
                return BadRequest(response);
            }

            if (string.IsNullOrWhiteSpace(request.DeviceUuid.ToString())
                || string.IsNullOrWhiteSpace(request.DeviceInfo)
                || string.IsNullOrWhiteSpace(request.LocationInfo))
            {
                response.ErrorCode = AuthControllerMsg.Login.InvalidCredential;
                return BadRequest(response);
            }

            var (errorCode, loginResult) = await sender.Send(Mapper.Map<LoginCommand>(request));
            response.Result = Mapper.Map<LoginRes>(loginResult);
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
            response.ErrorCode = AuthControllerMsg.Login.Exception;
            Logger.LogError("Login failed: {ex}", ex);
            return InternalServerError(response);
        }
    }

    [HttpPut]
    [Route("refresh")]
    [ProducesResponseType(typeof(ResultRes<RefreshTokenRes>), StatusCodes.Status200OK)]
    [AllowAnonymous]
    public async Task<IActionResult> RefreshToken(ISender sender, RefreshTokenReq refreshTokenReq)
    {
        var response = new ResultRes<RefreshTokenRes>();

        try
        {
            response.Success = false;
            refreshTokenReq.TrimData(logger);

            if (string.IsNullOrWhiteSpace(refreshTokenReq.Token) || string.IsNullOrWhiteSpace(refreshTokenReq.RefreshToken))
            {
                response.ErrorCode = AuthControllerMsg.RefreshToken.RequiredToken;
                return BadRequest(response);
            }

            var refreshResult = await sender.Send(Mapper.Map<RefreshTokenCommand>(refreshTokenReq));
            if (!refreshResult.Success)
            {
                response.ErrorCode = AuthControllerMsg.RefreshToken.Failed;
                return BadRequest(response);
            }

            response.Result = Mapper.Map<RefreshTokenRes>(refreshResult);
            response.Success = true;
            return Ok(response);
        }
        catch (Exception ex)
        {
            response.ErrorCode = AuthControllerMsg.RefreshToken.Exception;
            Logger.LogError("Refresh token failed: {ex}", ex);
            return InternalServerError(response);
        }
    }

    [HttpPut]
    [Route("logout")]
    [ProducesResponseType(typeof(ResultRes<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Logout(ISender sender, LogoutCommand logoutReq)
    {
        var response = new ResultRes<bool>();

        try
        {
            response.Success = false;
            logoutReq.TrimData(logger);

            if (string.IsNullOrWhiteSpace(logoutReq.Token) || string.IsNullOrWhiteSpace(logoutReq.RefreshToken))
            {
                response.ErrorCode = AuthControllerMsg.Logout.RequiredToken;
                return BadRequest(response);
            }

            response.Success = true;
            await sender.Send(Mapper.Map<LogoutCommand>(logoutReq));
        }
        catch (Exception ex)
        {
            Logger.LogError("Logout failed: {ex}", ex);
        }

        return Ok(response);
    }

    [HttpPut]
    [Route("change-password")]
    [ProducesResponseType(typeof(ResultRes<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ChangePassword(ISender sender, ChangePasswordReq request)
    {
        var response = new ResultRes<bool>();

        try
        {
            response.Success = false;
            request.TrimData(logger);

            if (string.IsNullOrWhiteSpace(request.OldPassword))
            {
                response.ErrorCode = AuthControllerMsg.ChangePassword.OldPasswordRequired;
                return BadRequest(response);
            }

            if (string.IsNullOrWhiteSpace(request.NewPassword))
            {
                response.ErrorCode = AuthControllerMsg.ChangePassword.NewPasswordRequired;
                return BadRequest(response);
            }

            var result = await sender.Send(Mapper.Map<ChangePasswordCommand>(request));
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
            Logger.LogError("Change password failed: {ex}", ex);
            response.ErrorCode = AuthControllerMsg.ChangePassword.Exception;
            return BadRequest(response);
        }
    }

    [HttpPost]
    [Route("forgot-password")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ResultRes<string>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ForgotPassword(ISender sender, ForgotPasswordReq request)
    {
        var response = new ResultRes<string>();

        try
        {
            response.Success = false;
            response.TrimData(logger);

            var errorCode = await sender.Send(Mapper.Map<ForgotPasswordCommand>(request));
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
            Logger.LogError("Request forgot password failed: {ex}", ex);
            response.ErrorCode = AuthControllerMsg.ForgotPassword.Exception;

            return BadRequest(response);
        }
    }

    [HttpPost]
    [Route("reset-password")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ResultRes<string>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ResetPassword(ISender sender, ResetPasswordReq request)
    {
        var response = new ResultRes<string>();

        try
        {
            response.Success = false;
            request.TrimData(logger);

            if (string.IsNullOrWhiteSpace(request.NewPassword))
            {
                response.ErrorCode = AuthControllerMsg.ResetPassword.NewPasswordRequired;
                return BadRequest(response);
            }

            if (!request.NewPassword.ValidatePasswordPolicy())
            {
                response.ErrorCode = AuthControllerMsg.ResetPassword.NewPasswordNotStrongEnough;
                return BadRequest(response);
            }

            if (!request.NewPassword.Equals(request.ConfirmPassword))
            {
                response.ErrorCode = AuthControllerMsg.ResetPassword.PasswordsDoNotMatch;
                return BadRequest(response);
            }

            var errorCode = await sender.Send(Mapper.Map<ResetPasswordCommand>(request));
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
            Logger.LogError("reset password failed: {ex}", ex);
            response.ErrorCode = AuthControllerMsg.ResetPassword.Exception;

            return BadRequest(response);
        }
    }

    [HttpPost]
    [Route("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ResultRes<string>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Register(ISender sender, RegisterReq request)
    {
        var response = new ResultRes<string>();

        try
        {
            response.Success = false;
            request.TrimData(logger);

            if (string.IsNullOrWhiteSpace(request.Email))
            {
                response.ErrorCode = AuthControllerMsg.Register.EmailRequired;
                return BadRequest(response);
            }

            if (!request.Email.IsValidEmail())
            {
                response.ErrorCode = AuthControllerMsg.Register.InvalidEmail;
                return BadRequest(response);
            }

            if (string.IsNullOrWhiteSpace(request.Password))
            {
                response.ErrorCode = AuthControllerMsg.Register.PasswordRequired;
                return BadRequest(response);
            }

            if (!request.Password.ValidatePasswordPolicy())
            {
                response.ErrorCode = AuthControllerMsg.Register.PasswordTooWeak;
                return BadRequest(response);
            }

            if (string.IsNullOrWhiteSpace(request.FirstName))
            {
                response.ErrorCode = AuthControllerMsg.Register.FirstNameRequired;
                return BadRequest(response);
            }

            var errorCode = await sender.Send(Mapper.Map<RegisterCommand>(request));
            if (!string.IsNullOrWhiteSpace(errorCode))
            {
                response.ErrorCode = errorCode;
                return BadRequest(response);
            }

            response.Success = true;
            return StatusCode(StatusCodes.Status201Created, response);
        }
        catch (Exception ex)
        {
            Logger.LogError("Register failed: {ex}", ex);
            response.ErrorCode = AuthControllerMsg.Register.Exception;
            return InternalServerError(response);
        }
    }

    [HttpGet]
    [Route("verify-email")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ResultRes<string>), StatusCodes.Status200OK)]
    public async Task<IActionResult> VerifyEmail(ISender sender, [FromQuery] string? token)
    {
        var response = new ResultRes<string>();

        try
        {
            response.Success = false;

            if (string.IsNullOrWhiteSpace(token))
            {
                response.ErrorCode = AuthControllerMsg.VerifyEmail.TokenRequired;
                return BadRequest(response);
            }

            var errorCode = await sender.Send(new VerifyEmailCommand { Token = token });
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
            // Token value is intentionally not logged.
            Logger.LogError("VerifyEmail failed: {ex}", ex);
            response.ErrorCode = AuthControllerMsg.VerifyEmail.Exception;
            return InternalServerError(response);
        }
    }
}