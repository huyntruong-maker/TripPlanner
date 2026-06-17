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

            var roleQuery = await writeUnitOfWork.GetRepository<Role>().QueryAll();
            var userRoleQuery = await writeUnitOfWork.GetRepository<UserRole>().QueryAll();
            var roleClaimQuery = await writeUnitOfWork.GetRepository<RoleClaim>().QueryAll();

            var authorizationClaims = (from ur in userRoleQuery
                                       join r in roleQuery on ur.RoleId equals r.Id
                                       join rc in roleClaimQuery on r.Id equals rc.RoleId
                                       where ur.UserId == user.Id
                                       select rc.ClaimValue).Distinct().ToArray();

            var roleData = (from ur in userRoleQuery
                           join r in roleQuery on ur.RoleId equals r.Id
                           where ur.UserId == user.Id
                           select new { r.Id, r.Level }).Distinct().ToArray();

            userClaims.AddRange(authorizationClaims.Select(ac => new Claim(RolePolicyConstants.ClaimType, ac))
                .ToList());

            // Add role level claims
            var roleLevel = roleData.Select(r => r.Level).ToArray();
            var roleIds = roleData.Select(r => r.Id).ToArray();

            userClaims.AddRange(roleLevel.Select(permission =>
                new Claim(UserConstants.RolesLevelClaim, permission.ToString())));

            userClaims.AddRange(roleIds.Select(roleId =>
                new Claim(UserConstants.RolesClaim, roleId.ToString())));

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