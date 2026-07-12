using Application.Features.Auth.Commands.ChangePasswordCommand;
using Application.Features.Auth.Commands.ForgotPasswordCommand;
using Application.Features.Auth.Commands.LoginCommand;
using Application.Features.Auth.Commands.LogoutCommand;
using Application.Features.Auth.Commands.RefreshTokenCommand;
using Application.Features.Auth.Commands.RegisterCommand;
using Application.Features.Auth.Commands.ResetPasswordCommand;
using AutoMapper;
using WebApi.Models.Requests.Auth;
using WebApi.Models.Responses.Auth;

namespace WebApi.Mappers;

public class AuthMapper : Profile
{
    public AuthMapper()
    {
        CreateMap<LoginReq, LoginCommand>();
        CreateMap<LogoutReq, LogoutCommand>();
        CreateMap<RefreshTokenReq, RefreshTokenCommand>();

        CreateMap<LoginResultDto, LoginRes>();
        CreateMap<RefreshTokenDto, RefreshTokenRes>();

        CreateMap<ChangePasswordReq, ChangePasswordCommand>();

        CreateMap<ForgotPasswordReq, ForgotPasswordCommand>();
        CreateMap<ResetPasswordReq, ResetPasswordCommand>();

        CreateMap<RegisterReq, RegisterCommand>();
    }
}