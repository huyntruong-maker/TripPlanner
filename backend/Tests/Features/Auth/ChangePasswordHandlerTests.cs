using Application.Common.Services;
using Application.Dtos.Base;
using Application.Dtos.Email;
using Application.Features.Auth.Commands.ChangePasswordCommand;
using Application.Interfaces.Email;
using Domain.Messages;
using Domain.Entities;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Tests.TestSupport;

namespace Tests.Features.Auth;

public class ChangePasswordHandlerTests
{
    private readonly UserManager<User> _userManager = IdentityTestFactory.CreateUserManager();
    private readonly SignInManager<User> _signInManager;
    private readonly IEmailService _emailService = Substitute.For<IEmailService>();
    private readonly IUserContextService _userContextService = Substitute.For<IUserContextService>();

    public ChangePasswordHandlerTests()
    {
        _signInManager = IdentityTestFactory.CreateSignInManager(_userManager);
    }

    private ChangePasswordHandler CreateHandler(Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        return new ChangePasswordHandler(
            _userManager,
            _signInManager,
            configuration,
            _emailService,
            NullLogger<ChangePasswordCommand>.Instance,
            _userContextService);
    }

    private static ChangePasswordCommand Command(string oldPassword = "OldPass1!", string newPassword = "NewPass1!", string? confirmPassword = null) => new()
    {
        OldPassword = oldPassword,
        NewPassword = newPassword,
        ConfirmPassword = confirmPassword ?? newPassword
    };

    [Fact]
    public async Task Handle_UnauthenticatedUser_ReturnsInvalidUserError()
    {
        _userContextService.GetCurrentUserContext().Returns(new CurrentUserContextDto { UserId = Guid.Empty });
        var handler = CreateHandler(ConfigurationTestFactory.Build());

        var result = await handler.Handle(Command(), CancellationToken.None);

        result.Should().Be(ShareControllerMsg.CurrentUserContext.InvalidUser);
    }

    [Fact]
    public async Task Handle_UserNotFound_ReturnsUserNotFoundError()
    {
        _userContextService.GetCurrentUserContext().Returns(new CurrentUserContextDto { UserId = Guid.NewGuid() });
        _userManager.FindByIdAsync(Arg.Any<string>()).Returns((User?)null);
        var handler = CreateHandler(ConfigurationTestFactory.Build());

        var result = await handler.Handle(Command(), CancellationToken.None);

        result.Should().Be(AuthControllerMsg.ChangePassword.UserNotFound);
    }

    [Fact]
    public async Task Handle_InvalidOldPassword_ReturnsInvalidOldPasswordError()
    {
        var user = new User { Id = Guid.NewGuid(), FirstName = "Jane", UserName = "jane@example.com" };
        _userContextService.GetCurrentUserContext().Returns(new CurrentUserContextDto { UserId = user.Id });
        _userManager.FindByIdAsync(Arg.Any<string>()).Returns(user);
        _signInManager.PasswordSignInAsync(Arg.Any<string>(), Arg.Any<string>(), false, true)
            .Returns(SignInResult.Failed);

        var handler = CreateHandler(ConfigurationTestFactory.Build());

        var result = await handler.Handle(Command(), CancellationToken.None);

        result.Should().Be(AuthControllerMsg.ChangePassword.InvalidOldPassword);
    }

    [Fact]
    public async Task Handle_WeakNewPassword_ReturnsNewPasswordNotStrongEnoughError()
    {
        var user = new User { Id = Guid.NewGuid(), FirstName = "Jane", UserName = "jane@example.com" };
        _userContextService.GetCurrentUserContext().Returns(new CurrentUserContextDto { UserId = user.Id });
        _userManager.FindByIdAsync(Arg.Any<string>()).Returns(user);
        _signInManager.PasswordSignInAsync(Arg.Any<string>(), Arg.Any<string>(), false, true)
            .Returns(SignInResult.Success);

        var handler = CreateHandler(ConfigurationTestFactory.Build());

        var result = await handler.Handle(Command(newPassword: "weak", confirmPassword: "weak"), CancellationToken.None);

        result.Should().Be(AuthControllerMsg.ChangePassword.NewPasswordNotStrongEnough);
    }

    [Fact]
    public async Task Handle_NewPasswordSameAsOld_ReturnsNewPassSameAsOldPassError()
    {
        var user = new User { Id = Guid.NewGuid(), FirstName = "Jane", UserName = "jane@example.com" };
        _userContextService.GetCurrentUserContext().Returns(new CurrentUserContextDto { UserId = user.Id });
        _userManager.FindByIdAsync(Arg.Any<string>()).Returns(user);
        _signInManager.PasswordSignInAsync(Arg.Any<string>(), Arg.Any<string>(), false, true)
            .Returns(SignInResult.Success);

        var handler = CreateHandler(ConfigurationTestFactory.Build());

        var result = await handler.Handle(Command(oldPassword: "SamePass1!", newPassword: "SamePass1!"), CancellationToken.None);

        result.Should().Be(AuthControllerMsg.ChangePassword.NewPassSameAsOldPass);
    }

    [Fact]
    public async Task Handle_PasswordsDoNotMatch_ReturnsPasswordsDoNotMatchError()
    {
        var user = new User { Id = Guid.NewGuid(), FirstName = "Jane", UserName = "jane@example.com" };
        _userContextService.GetCurrentUserContext().Returns(new CurrentUserContextDto { UserId = user.Id });
        _userManager.FindByIdAsync(Arg.Any<string>()).Returns(user);
        _signInManager.PasswordSignInAsync(Arg.Any<string>(), Arg.Any<string>(), false, true)
            .Returns(SignInResult.Success);

        var handler = CreateHandler(ConfigurationTestFactory.Build());

        var result = await handler.Handle(Command(newPassword: "NewPass1!", confirmPassword: "Different1!"), CancellationToken.None);

        result.Should().Be(AuthControllerMsg.ChangePassword.PasswordsDoNotMatch);
    }

    [Fact]
    public async Task Handle_ValidRequest_UpdatesPasswordAndReturnsEmptyError()
    {
        var user = new User { Id = Guid.NewGuid(), FirstName = "Jane", UserName = "jane@example.com", Email = "jane@example.com" };
        _userContextService.GetCurrentUserContext().Returns(new CurrentUserContextDto { UserId = user.Id });
        _userManager.FindByIdAsync(Arg.Any<string>()).Returns(user);
        _signInManager.PasswordSignInAsync(Arg.Any<string>(), Arg.Any<string>(), false, true)
            .Returns(SignInResult.Success);
        _userManager.UpdateAsync(Arg.Any<User>()).Returns(IdentityResult.Success);
        _emailService.SendEmail(Arg.Any<SendEmailReqDto>()).Returns(string.Empty);

        var handler = CreateHandler(ConfigurationTestFactory.Build());

        var result = await handler.Handle(Command(), CancellationToken.None);

        result.Should().Be(string.Empty);
        await _emailService.Received(1).SendEmail(Arg.Any<SendEmailReqDto>());
    }

    [Fact]
    public async Task Handle_UpdateAsyncFails_ReturnsFailedError()
    {
        var user = new User { Id = Guid.NewGuid(), FirstName = "Jane", UserName = "jane@example.com" };
        _userContextService.GetCurrentUserContext().Returns(new CurrentUserContextDto { UserId = user.Id });
        _userManager.FindByIdAsync(Arg.Any<string>()).Returns(user);
        _signInManager.PasswordSignInAsync(Arg.Any<string>(), Arg.Any<string>(), false, true)
            .Returns(SignInResult.Success);
        _userManager.UpdateAsync(Arg.Any<User>())
            .Returns(IdentityResult.Failed(new IdentityError { Code = "ConcurrencyFailure" }));

        var handler = CreateHandler(ConfigurationTestFactory.Build());

        var result = await handler.Handle(Command(), CancellationToken.None);

        result.Should().Be(AuthControllerMsg.ChangePassword.Failed);
    }
}
