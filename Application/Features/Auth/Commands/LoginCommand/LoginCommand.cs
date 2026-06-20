using Application.Dtos.Base;
using Application.Dtos.Email;
using Application.Features.Auth.Shared;
using Application.Interfaces.DataAccess;
using Application.Interfaces.Email;
using Domain.Constants;
using Domain.Entities;
using Domain.Helpers;
using Domain.Messages;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using Application.Interfaces.Cqrs;

namespace Application.Features.Auth.Commands.LoginCommand;

public record LoginCommand : ICommand<(string, LoginResultDto)>
{
    public string? Username { get; init; }

    public string? Password { get; init; }

    public required Guid DeviceUuid { get; init; }

    public required bool RememberMe { get; init; }

    public required string DeviceInfo { get; set; }

    public required string LocationInfo { get; set; }
}

public class LoginCommandHandler(
    SignInManager<User> signInManager,
    UserManager<User> userManager,
    IAuthShareService authShareService,
    IWriteUnitOfWork writeUnitOfWork,
    IEmailService emailService,
    IConfiguration configuration,
    ILogger<LoginCommandHandler> logger)
    : IRequestHandler<LoginCommand, (string, LoginResultDto)>
{
    public async Task<(string, LoginResultDto)> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var loginResult = new LoginResultDto();

        // Accept email in the username field: try email lookup first, then fall back to username.
        User? user = null;
        if (!string.IsNullOrWhiteSpace(request.Username) && request.Username.Contains('@'))
        {
            user = await userManager.FindByEmailAsync(request.Username);
        }

        user ??= await userManager.FindByNameAsync(request.Username!);
        if (user == null) return (AuthControllerMsg.Login.InvalidCredential, loginResult);

        var result = await signInManager.PasswordSignInAsync(request.Username!, request.Password!, true, true);

        if (!result.Succeeded)
        {
            if (result.IsLockedOut)
            {
                loginResult.LockoutEnd = user.LockoutEnd?.AddHours(7).DateTime;
                loginResult.AccessFailedCount = authShareService.MaxFailedAccessAttempts;
                return (AuthControllerMsg.Login.LockedOut, loginResult);
            }

            loginResult.AccessFailedCount = user.AccessFailedCount;

            if (user.AccessFailedCount + 1 == authShareService.MaxFailedAccessAttempts)
                return (AuthControllerMsg.Login.WillBeLockedOut, loginResult);

            if (user.AccessFailedCount == authShareService.MaxFailedAccessAttempts)
            {
                var lockoutTime = DateTimeHelper.GetDtOffset().AddMinutes(authShareService.DefaultLockoutMinutes);
                await userManager.SetLockoutEndDateAsync(user, lockoutTime);
                loginResult.LockoutEnd = user.LockoutEnd?.AddHours(7).DateTime;
                loginResult.AccessFailedCount = authShareService.MaxFailedAccessAttempts;
                return (AuthControllerMsg.Login.LockedOut, loginResult);
            }

            return (AuthControllerMsg.Login.InvalidCredential, loginResult);
        }

        user.LockoutEnd = null;
        user.AccessFailedCount = 0;

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

        var roleLevel = roleData.Select(r => r.Level).ToArray();
        var roleIds = roleData.Select(r => r.Id).ToArray();

        userClaims.AddRange(roleLevel.Select(permission =>
            new Claim(UserConstants.RolesLevelClaim, permission.ToString())));

        userClaims.AddRange(roleIds.Select(roleId =>
            new Claim(UserConstants.RolesClaim, roleId.ToString())));

        var token = authShareService.GenerateToken(
            userClaims,
            authShareService.Secret,
            DateTimeHelper.GetDt().AddMinutes(authShareService.ExpirationMinutes));

        var refreshExpiration = request.RememberMe
            ? DateTimeHelper.GetDt().AddDays(authShareService.RefreshExpirationDays)
            : DateTimeHelper.GetDt().AddDays(authShareService.RefreshShortExpirationDays);

        var refreshToken = authShareService.GenerateToken(
            userClaims,
            authShareService.RefreshTokeCommand,
            refreshExpiration);

        await userManager.UpdateAsync(user);

        var userTokenRepo = writeUnitOfWork.GetRepository<UserToken>();
        var userToken = await userTokenRepo.Single(i => i.DeviceUuid == request.DeviceUuid && i.UserId == user.Id);

        if (userToken == null)
        {
            userToken = new UserToken
            {
                UserId = user.Id,
                DeviceUuid = request.DeviceUuid,
                RefreshToken = refreshToken,
                RefreshTokenExpiration = refreshExpiration,
                Value = token,
                LoginProvider = GlobalConstants.JwtLoginToken,
                Name = $"{user.UserName}_{DateTime.Now.ToFileTime()}",
                RememberMe = request.RememberMe,
                DeviceInfo = request.DeviceInfo,
                LocationInfo = request.LocationInfo
            };
            await writeUnitOfWork.GetRepository<UserToken>().Add(userToken);

            await SendNewDeviceEmail(user, userToken, request);
        }
        else
        {
            userToken.Value = token;
            userToken.RefreshToken = refreshToken;
            userToken.RefreshTokenExpiration = refreshExpiration;
            await userTokenRepo.Update(userToken);
        }

        await writeUnitOfWork.SaveChanges();

        loginResult.Token = token;
        loginResult.RefreshToken = refreshToken;

        return (string.Empty, loginResult);
    }

    private async Task<string> SendNewDeviceEmail(User user, UserToken userToken, LoginCommand loginRequest)
    {
        if (string.IsNullOrEmpty(user.Email))
        {
            logger.LogWarning("SendNewDeviceEmail Failed. User {id} dont have email", user.Id);
            return string.Empty;
        }

        var emailTemplate = configuration.GetSection(ConfigKeys.Security.Email.NewDeviceNotification)
            .Get<EmailTemplateDto>();

        if (emailTemplate == null || string.IsNullOrEmpty(emailTemplate.Path))
        {
            logger.LogWarning("Template or template path for send email new device login not found");
            return string.Empty;
        }

        var dataBinding = new Dictionary<string, string>
        {
            { "{{UserName}}", user.UserName! },
            { "{{DeviceId}}", userToken.DeviceUuid.ToString() },
            { "{{LoginTime}}", DateTimeOffset.UtcNow.ToLocalTime().ToString(DateTimeFormats.DateTime4)},
            { "{{DeviceInfo}}", loginRequest.DeviceInfo },
            { "{{LocationInfo}}", loginRequest.LocationInfo },
        };

        var request = new SendEmailReqDto
        {
            ToEmails = [user.Email],
            Subject = string.IsNullOrEmpty(emailTemplate.Subject) ? "New device login alert" : emailTemplate.Subject,
            TemplatePath = emailTemplate.Path,
            DataBinding = dataBinding
        };

        return await emailService.SendEmail(request);
    }
}