using Application.Interfaces.DataAccess;
using Domain.Constants;
using Domain.Entities;
using Domain.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Application.Features.Auth.Shared;

public interface IAuthShareService
{
    Task<User?> VerifyToken(string token, string secret);
    string GenerateToken(IEnumerable<Claim> claims, string secret, DateTime expireDateTime);
    Task<User?> VerifyUserToken(Guid deviceUuid, string token, string refreshToken);

    int MaxFailedAccessAttempts { get; }
    int DefaultLockoutMinutes { get; }
    int ExpirationMinutes { get; }
    int RefreshExpirationDays { get; }
    int RefreshShortExpirationDays { get; }
    string RefreshTokeCommand { get; }
    string Secret { get; }
    int ResetPasswordExpirationHours { get; }
}

public class AuthShareService(
    UserManager<User> userManager,
    IConfiguration configuration,
    IReadUnitOfWork readUnitOfWork,
    ILogger<AuthShareService> logger) : IAuthShareService
{
    public int DefaultLockoutMinutes { get; } =
        configuration.GetSection(ConfigKeys.Security.Lockout.DefaultLockoutMinutes).Get<int>();

    public int ExpirationMinutes { get; } =
        configuration.GetSection(ConfigKeys.Security.Jwt.ExpirationMinutes).Get<int>();

    public int MaxFailedAccessAttempts { get; } =
        configuration.GetSection(ConfigKeys.Security.Lockout.MaxFailedAccessAttempts).Get<int>();

    public int RefreshExpirationDays { get; } =
        configuration.GetSection(ConfigKeys.Security.Jwt.RefreshExpirationDays).Get<int>();

    public int RefreshShortExpirationDays { get; } =
        configuration.GetSection(ConfigKeys.Security.Jwt.RefreshShortExpirationDays).Get<int>();

    public string RefreshTokeCommand { get; } =
        configuration.GetSection(ConfigKeys.Security.Jwt.RefreshSecret).Get<string>()!;

    public string Secret { get; } =
        configuration.GetSection(ConfigKeys.Security.Jwt.Secret).Get<string>()!;

    public int ResetPasswordExpirationHours { get; } =
        configuration.GetSection(ConfigKeys.Security.Jwt.ResetPasswordExpirationHours).Get<int>()!;

    public async Task<User?> VerifyToken(string token, string secret)
    {
        var tokenHandler = new JwtSecurityTokenHandler();

        var jwtToken = ReadJwtToken(token);

        var userId = jwtToken.Claims.GetUserId();

        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user == null) return null;

        var securityKey = secret.ToByteArray();

        tokenHandler.ValidateToken(token, new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(securityKey),
            ValidateIssuer = false,
            ValidateAudience = false,
            ClockSkew = TimeSpan.Zero
        }, out _);

        return user;
    }

    public string GenerateToken(IEnumerable<Claim> claims, string secret, DateTime expireDateTime)
    {
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expireDateTime,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.ASCII.GetBytes(secret)),
                SecurityAlgorithms.HmacSha512Signature),
            Audience = configuration.GetSection(ConfigKeys.Security.Jwt.Audience).Get<string>(),
            Issuer = configuration.GetSection(ConfigKeys.Security.Jwt.Issuer).Get<string>()
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);

        return tokenHandler.WriteToken(token);
    }

    public async Task<User?> VerifyUserToken(Guid deviceUuid, string token, string refreshToken)
    {
        var tokenContent = ReadJwtToken(token);
        var refreshTokenContent = ReadJwtToken(refreshToken);

        var userId = tokenContent.Claims.GetUserId();
        var refreshTokenUserId = refreshTokenContent.Claims.GetUserId();

        if (userId != refreshTokenUserId)
        {
            logger.LogWarning("User of both token aren't matched: {userId} - {refreshTokenUserId}", userId,
                refreshTokenUserId);
            return null;
        }

        var user = await VerifyToken(refreshToken, RefreshTokeCommand);
        if (user == null) return null;

        var userTokenRepo = readUnitOfWork.GetRepository<UserToken>();
        var userToken = await userTokenRepo.Single(i => i.UserId == user.Id && i.DeviceUuid == deviceUuid);
        if (userToken == null
            || userToken.RefreshToken != refreshToken
            || userToken.RefreshTokenExpiration < DateTimeHelper.GetDtOffset())
            return null;

        return user;
    }

    private static JwtSecurityToken ReadJwtToken(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        return jwtToken;
    }
}