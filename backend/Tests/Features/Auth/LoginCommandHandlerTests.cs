using System.Security.Claims;
using Application.Features.Auth.Commands.LoginCommand;
using Application.Features.Auth.Shared;
using Domain.Constants;
using Domain.Entities;
using Domain.Messages;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using NSubstitute;
using Tests.TestSupport;

namespace Tests.Features.Auth;

public class LoginCommandHandlerTests
{
    private readonly UserManager<User> _userManager = IdentityTestFactory.CreateUserManager();
    private readonly SignInManager<User> _signInManager;
    private readonly IAuthShareService _authShareService = Substitute.For<IAuthShareService>();

    public LoginCommandHandlerTests()
    {
        _signInManager = IdentityTestFactory.CreateSignInManager(_userManager);
        _authShareService.MaxFailedAccessAttempts.Returns(5);
        _authShareService.ExpirationMinutes.Returns(30);
        _authShareService.RefreshExpirationDays.Returns(30);
        _authShareService.RefreshShortExpirationDays.Returns(1);
        _authShareService.RefreshTokeCommand.Returns("refresh-secret");
        _authShareService.Secret.Returns("access-secret");
    }

    private LoginCommandHandler CreateHandler(Application.Interfaces.DataAccess.IWriteUnitOfWork writeUnitOfWork)
    {
        return new LoginCommandHandler(_signInManager, _userManager, _authShareService, writeUnitOfWork);
    }

    private static LoginCommand Command(string username = "user@example.com", bool rememberMe = false) => new()
    {
        Username = username,
        Password = "StrongPass1!",
        RememberMe = rememberMe
    };

    private static User NewUser(int accessFailedCount = 0) => new()
    {
        Id = Guid.NewGuid(),
        UserName = "user@example.com",
        Email = "user@example.com",
        FirstName = "Jane",
        EmailConfirmed = true,
        AccessFailedCount = accessFailedCount
    };

    [Fact]
    public async Task Handle_UnknownUsername_ReturnsInvalidCredential()
    {
        _userManager.FindByEmailAsync(Arg.Any<string>()).Returns((User?)null);
        var writeUnitOfWork = UnitOfWorkFake.CreateWrite();
        var handler = CreateHandler(writeUnitOfWork);

        var (errorCode, result) = await handler.Handle(Command(), CancellationToken.None);

        errorCode.Should().Be(AuthControllerMsg.Login.InvalidCredential);
        result.Token.Should().BeNull();
    }

    [Fact]
    public async Task Handle_EmailNotConfirmed_ReturnsInActive()
    {
        var user = NewUser();
        user.EmailConfirmed = false;
        _userManager.FindByEmailAsync(Arg.Any<string>()).Returns(user);
        _signInManager.PasswordSignInAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<bool>())
            .Returns(SignInResult.Success);

        var writeUnitOfWork = UnitOfWorkFake.CreateWrite();
        var handler = CreateHandler(writeUnitOfWork);

        var (errorCode, _) = await handler.Handle(Command(), CancellationToken.None);

        errorCode.Should().Be(AuthControllerMsg.Login.InActive);
    }

    [Fact]
    public async Task Handle_LockedOut_ReturnsLockedOutWithLockoutEnd()
    {
        var user = NewUser();
        user.LockoutEnd = DateTimeOffset.UtcNow.AddMinutes(10);
        _userManager.FindByEmailAsync(Arg.Any<string>()).Returns(user);
        _signInManager.PasswordSignInAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<bool>())
            .Returns(SignInResult.LockedOut);

        var writeUnitOfWork = UnitOfWorkFake.CreateWrite();
        var handler = CreateHandler(writeUnitOfWork);

        var (errorCode, result) = await handler.Handle(Command(), CancellationToken.None);

        errorCode.Should().Be(AuthControllerMsg.Login.LockedOut);
        result.AccessFailedCount.Should().Be(5);
    }

    [Fact]
    public async Task Handle_InvalidPasswordBelowThreshold_ReturnsInvalidCredential()
    {
        var user = NewUser(accessFailedCount: 1);
        _userManager.FindByEmailAsync(Arg.Any<string>()).Returns(user);
        _signInManager.PasswordSignInAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<bool>())
            .Returns(SignInResult.Failed);

        var writeUnitOfWork = UnitOfWorkFake.CreateWrite();
        var handler = CreateHandler(writeUnitOfWork);

        var (errorCode, _) = await handler.Handle(Command(), CancellationToken.None);

        errorCode.Should().Be(AuthControllerMsg.Login.InvalidCredential);
    }

    [Fact]
    public async Task Handle_InvalidPasswordOneAttemptFromLockout_ReturnsWillBeLockedOut()
    {
        var user = NewUser(accessFailedCount: 4);
        _userManager.FindByEmailAsync(Arg.Any<string>()).Returns(user);
        _signInManager.PasswordSignInAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<bool>())
            .Returns(SignInResult.Failed);

        var writeUnitOfWork = UnitOfWorkFake.CreateWrite();
        var handler = CreateHandler(writeUnitOfWork);

        var (errorCode, _) = await handler.Handle(Command(), CancellationToken.None);

        errorCode.Should().Be(AuthControllerMsg.Login.WillBeLockedOut);
    }

    [Fact]
    public async Task Handle_ValidCredentialsNoExistingToken_ReturnsTokensAndCreatesUserToken()
    {
        var user = NewUser();
        _userManager.FindByEmailAsync(Arg.Any<string>()).Returns(user);
        _signInManager.PasswordSignInAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<bool>())
            .Returns(SignInResult.Success);
        _signInManager.CreateUserPrincipalAsync(Arg.Any<User>())
            .Returns(new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())])));
        _authShareService.GenerateToken(Arg.Any<IEnumerable<Claim>>(), Arg.Any<string>(), Arg.Any<DateTime>())
            .Returns("generated-token");

        var writeUnitOfWork = UnitOfWorkFake.CreateWrite();
        var tokenStore = writeUnitOfWork.RegisterRepository<UserToken>();
        var handler = CreateHandler(writeUnitOfWork);

        var (errorCode, result) = await handler.Handle(Command(), CancellationToken.None);

        errorCode.Should().Be(string.Empty);
        result.Token.Should().Be("generated-token");
        result.RefreshToken.Should().Be("generated-token");
        tokenStore.Should().ContainSingle(t => t.UserId == user.Id);
        await writeUnitOfWork.Received(1).SaveChanges();
    }

    [Fact]
    public async Task Handle_ValidCredentialsExistingToken_UpdatesExistingUserToken()
    {
        var user = NewUser();
        var existingToken = new UserToken
        {
            UserId = user.Id,
            RefreshToken = "old-refresh",
            Value = "old-value",
            LoginProvider = GlobalConstants.JwtLoginToken,
            Name = "existing"
        };

        _userManager.FindByEmailAsync(Arg.Any<string>()).Returns(user);
        _signInManager.PasswordSignInAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<bool>())
            .Returns(SignInResult.Success);
        _signInManager.CreateUserPrincipalAsync(Arg.Any<User>())
            .Returns(new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())])));
        _authShareService.GenerateToken(Arg.Any<IEnumerable<Claim>>(), Arg.Any<string>(), Arg.Any<DateTime>())
            .Returns("new-token");

        var writeUnitOfWork = UnitOfWorkFake.CreateWrite();
        var tokenStore = writeUnitOfWork.RegisterRepository([existingToken]);
        var handler = CreateHandler(writeUnitOfWork);

        var (errorCode, result) = await handler.Handle(Command(), CancellationToken.None);

        errorCode.Should().Be(string.Empty);
        tokenStore.Should().ContainSingle();
        existingToken.Value.Should().Be("new-token");
        existingToken.RefreshToken.Should().Be("new-token");
    }
}
