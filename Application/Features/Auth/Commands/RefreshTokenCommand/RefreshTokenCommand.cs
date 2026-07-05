using Application.Features.Auth.Shared;
using Application.Interfaces.DataAccess;
using Domain.Constants;
using Domain.Entities;
using Domain.Helpers;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using Application.Interfaces.Cqrs;

namespace Application.Features.Auth.Commands.RefreshTokenCommand;

public record RefreshTokenCommand : ICommand<RefreshTokenDto>
{
    public required string Token { get; set; }

    public required string RefreshToken { get; set; }

    public Guid DeviceUuid { get; set; }
}

public class RefreshTokenCommandHandler(
    SignInManager<User> signInManager,
    UserManager<User> userManager,
    ILogger<RefreshTokenCommandHandler> logger,
    IAuthShareService authShareService,
    IWriteUnitOfWork writeUnitOfWork)
    : IRequestHandler<RefreshTokenCommand, RefreshTokenDto>
{
    public async Task<RefreshTokenDto> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var result = new RefreshTokenDto();

        try
        {
            var user = await authShareService.VerifyToken(request.Token, authShareService.Secret);
            switch (user)
            {
                case null:
                case { LockoutEnabled: true, LockoutEnd: not null }:
                    return result;
                default:
                    logger.LogWarning("User {id} - {email} tries to get new token while it is still valid", user.Id,
                        user.Email);
                    return result;
            }
        }
        catch (SecurityTokenExpiredException)
        {
            var user = await authShareService.VerifyUserToken(request.DeviceUuid, request.Token, request.RefreshToken);
            if (user == null) return result;

            var principal = await signInManager.CreateUserPrincipalAsync(user);
            var userClaims = principal.Claims.ToList();

            var userTokenRepo = writeUnitOfWork.GetRepository<UserToken>();
            var userToken = await userTokenRepo.Single(i => i.UserId == user.Id && i.DeviceUuid == request.DeviceUuid);

            var tokenExpiration = DateTimeHelper.GetDt().AddMinutes(authShareService.ExpirationMinutes);
            var newToken = authShareService.GenerateToken(userClaims, authShareService.Secret, tokenExpiration);

            var refreshExpiration = DateTimeHelper.GetDt().AddDays(authShareService.RefreshShortExpirationDays);
            if (userToken != null && userToken.RememberMe)
            {
                refreshExpiration = DateTimeHelper.GetDt().AddDays(authShareService.RefreshExpirationDays);
            }
            var newRefreshToken = authShareService.GenerateToken(userClaims, authShareService.RefreshTokeCommand, refreshExpiration);

            result.Success = true;
            result.Token = newToken;
            result.RefreshToken = newRefreshToken;

            if (userToken != null)
            {
                userToken.Value = newToken;
                userToken.RefreshToken = newRefreshToken;
                userToken.RefreshTokenExpiration = refreshExpiration;
                await userTokenRepo.Update(userToken);
            }

            user.LockoutEnd = null;
            user.AccessFailedCount = 0;
            await userManager.UpdateAsync(user);
            await writeUnitOfWork.SaveChanges();
            return result;
        }
    }
}