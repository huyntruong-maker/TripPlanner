using Application.Features.Auth.Shared;
using Application.Interfaces.DataAccess;
using Domain.Constants;
using Domain.Entities;
using Domain.Helpers;
using Domain.Messages;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Application.Interfaces.Cqrs;

namespace Application.Features.Auth.Commands.LoginCommand;

public record LoginCommand : ICommand<(string, LoginResultDto)>
{
    public string? Username { get; init; }

    public string? Password { get; init; }

    public required bool RememberMe { get; init; }
}

public class LoginCommandHandler(
    SignInManager<User> signInManager,
    UserManager<User> userManager,
    IAuthShareService authShareService,
    IWriteUnitOfWork writeUnitOfWork)
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
        var userToken = await userTokenRepo.Single(i => i.UserId == user.Id);

        if (userToken == null)
        {
            userToken = new UserToken
            {
                UserId = user.Id,
                RefreshToken = refreshToken,
                RefreshTokenExpiration = refreshExpiration,
                Value = token,
                LoginProvider = GlobalConstants.JwtLoginToken,
                Name = $"{user.UserName}_{DateTime.Now.ToFileTime()}",
                RememberMe = request.RememberMe
            };
            await writeUnitOfWork.GetRepository<UserToken>().Add(userToken);
        }
        else
        {
            userToken.Value = token;
            userToken.RefreshToken = refreshToken;
            userToken.RefreshTokenExpiration = refreshExpiration;
            userToken.RememberMe = request.RememberMe;
            await userTokenRepo.Update(userToken);
        }

        await writeUnitOfWork.SaveChanges();

        loginResult.Token = token;
        loginResult.RefreshToken = refreshToken;

        return (string.Empty, loginResult);
    }
}
