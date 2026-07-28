using System.Security.Claims;
using Application.Features.Auth.Commands.RefreshTokenCommand;
using Application.Features.Auth.Shared;
using Domain.Entities;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.Tokens;
using NSubstitute;
using Tests.TestSupport;

namespace Tests.Features.Auth;

public class RefreshTokenCommandHandlerTests
{
    private readonly UserManager<User> _userManager = IdentityTestFactory.CreateUserManager();
    private readonly SignInManager<User> _signInManager;
    private readonly IAuthShareService _authShareService = Substitute.For<IAuthShareService>();

    public RefreshTokenCommandHandlerTests()
    {
        _signInManager = IdentityTestFactory.CreateSignInManager(_userManager);
        _authShareService.ExpirationMinutes.Returns(30);
        _authShareService.RefreshExpirationDays.Returns(30);
        _authShareService.RefreshShortExpirationDays.Returns(1);
        _authShareService.RefreshTokeCommand.Returns("refresh-secret");
        _authShareService.Secret.Returns("access-secret");
    }

    private RefreshTokenCommandHandler CreateHandler(Application.Interfaces.DataAccess.IWriteUnitOfWork writeUnitOfWork)
    {
        return new RefreshTokenCommandHandler(
            _signInManager,
            _userManager,
            NullLogger<RefreshTokenCommandHandler>.Instance,
            _authShareService,
            writeUnitOfWork);
    }

    private static RefreshTokenCommand Command() => new() { Token = "access-token", RefreshToken = "refresh-token" };

    [Fact]
    public async Task Handle_AccessTokenStillValid_ReturnsUnsuccessfulResult()
    {
        var user = new User { Id = Guid.NewGuid(), FirstName = "Jane", LockoutEnabled = false };
        _authShareService.VerifyToken(Arg.Any<string>(), Arg.Any<string>()).Returns(user);

        var writeUnitOfWork = UnitOfWorkFake.CreateWrite();
        var handler = CreateHandler(writeUnitOfWork);

        var result = await handler.Handle(Command(), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Token.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ExpiredAccessTokenButInvalidRefreshToken_ReturnsUnsuccessfulResult()
    {
        _authShareService.VerifyToken(Arg.Any<string>(), Arg.Any<string>())
            .Returns<User?>(_ => throw new SecurityTokenExpiredException("expired"));
        _authShareService.VerifyUserToken(Arg.Any<string>(), Arg.Any<string>()).Returns((User?)null);

        var writeUnitOfWork = UnitOfWorkFake.CreateWrite();
        var handler = CreateHandler(writeUnitOfWork);

        var result = await handler.Handle(Command(), CancellationToken.None);

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ExpiredAccessTokenValidRefreshToken_ReturnsNewTokensAndUpdatesUserToken()
    {
        var user = new User { Id = Guid.NewGuid(), FirstName = "Jane" };
        var existingToken = new UserToken
        {
            UserId = user.Id,
            Value = "old-access",
            RefreshToken = "old-refresh",
            LoginProvider = Domain.Constants.GlobalConstants.JwtLoginToken,
            Name = "session-1",
            RememberMe = true
        };

        _authShareService.VerifyToken(Arg.Any<string>(), Arg.Any<string>())
            .Returns<User?>(_ => throw new SecurityTokenExpiredException("expired"));
        _authShareService.VerifyUserToken(Arg.Any<string>(), Arg.Any<string>()).Returns(user);
        _signInManager.CreateUserPrincipalAsync(Arg.Any<User>())
            .Returns(new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())])));
        _authShareService.GenerateToken(Arg.Any<IEnumerable<Claim>>(), Arg.Any<string>(), Arg.Any<DateTime>())
            .Returns("new-token");

        var writeUnitOfWork = UnitOfWorkFake.CreateWrite();
        writeUnitOfWork.RegisterRepository([existingToken]);
        var handler = CreateHandler(writeUnitOfWork);

        var result = await handler.Handle(Command(), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Token.Should().Be("new-token");
        result.RefreshToken.Should().Be("new-token");
        existingToken.Value.Should().Be("new-token");
        existingToken.RefreshToken.Should().Be("new-token");
        await writeUnitOfWork.Received(1).SaveChanges();
    }

    [Fact]
    public async Task Handle_ExpiredAccessTokenNoExistingUserToken_ReturnsNewTokensWithoutUpdatingRepo()
    {
        var user = new User { Id = Guid.NewGuid(), FirstName = "Jane" };

        _authShareService.VerifyToken(Arg.Any<string>(), Arg.Any<string>())
            .Returns<User?>(_ => throw new SecurityTokenExpiredException("expired"));
        _authShareService.VerifyUserToken(Arg.Any<string>(), Arg.Any<string>()).Returns(user);
        _signInManager.CreateUserPrincipalAsync(Arg.Any<User>())
            .Returns(new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())])));
        _authShareService.GenerateToken(Arg.Any<IEnumerable<Claim>>(), Arg.Any<string>(), Arg.Any<DateTime>())
            .Returns("new-token");

        var writeUnitOfWork = UnitOfWorkFake.CreateWrite();
        writeUnitOfWork.RegisterRepository<UserToken>();
        var handler = CreateHandler(writeUnitOfWork);

        var result = await handler.Handle(Command(), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Token.Should().Be("new-token");
    }
}
