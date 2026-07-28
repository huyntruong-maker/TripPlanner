using Application.Dtos.Email;
using Application.Features.Auth.Commands.ForgotPasswordCommand;
using Application.Features.Auth.Shared;
using Application.Interfaces.Email;
using Domain.Constants;
using Domain.Entities;
using Domain.Messages;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using NSubstitute;
using Tests.TestSupport;

namespace Tests.Features.Auth;

public class ForgotPasswordCommandHandlerTests
{
    private readonly IEmailService _emailService = Substitute.For<IEmailService>();
    private readonly IAuthShareService _authShareService = Substitute.For<IAuthShareService>();
    private readonly UserManager<User> _userManager = IdentityTestFactory.CreateUserManager();

    private ResetPasswordCommandHandler CreateHandler(
        Application.Interfaces.DataAccess.IWriteUnitOfWork writeUnitOfWork,
        Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        return new ResetPasswordCommandHandler(_userManager, writeUnitOfWork, _emailService, _authShareService, configuration);
    }

    private static ForgotPasswordCommand Command() => new() { Email = "user@example.com" };

    [Fact]
    public async Task Handle_UnknownEmail_ReturnsEmailNotExistError()
    {
        _userManager.FindByEmailAsync(Arg.Any<string>()).Returns((User?)null);
        var writeUnitOfWork = UnitOfWorkFake.CreateWrite();
        writeUnitOfWork.RegisterRepository<User>();
        var handler = CreateHandler(writeUnitOfWork, ConfigurationTestFactory.Build());

        var result = await handler.Handle(Command(), CancellationToken.None);

        result.Should().Be(AuthControllerMsg.ForgotPassword.EmailNotExist);
    }

    [Fact]
    public async Task Handle_KnownEmail_SetsResetTokenAndSendsEmail()
    {
        var user = new User { Id = Guid.NewGuid(), FirstName = "Jane", UserName = "user@example.com", Email = "user@example.com" };
        _userManager.FindByEmailAsync(Arg.Any<string>()).Returns(user);
        _authShareService.ResetPasswordExpirationHours.Returns(4);
        _emailService.SendEmail(Arg.Any<SendEmailReqDto>()).Returns(string.Empty);

        var writeUnitOfWork = UnitOfWorkFake.CreateWrite();
        writeUnitOfWork.RegisterRepository([user]);
        var configuration = ConfigurationTestFactory.WithEmailTemplate(
            ConfigKeys.Security.Email.ResetPasswordNotification,
            "Reset your password",
            "https://app.example.com/reset");

        var handler = CreateHandler(writeUnitOfWork, configuration);

        var result = await handler.Handle(Command(), CancellationToken.None);

        result.Should().Be(string.Empty);
        user.ResetPasswordToken.Should().NotBeNullOrEmpty();
        user.ResetPasswordExpiration.Should().NotBeNull();
        await writeUnitOfWork.Received(1).SaveChanges();
        await _emailService.Received(1).SendEmail(Arg.Any<SendEmailReqDto>());
    }

    [Fact]
    public async Task Handle_EmailTemplateMissing_ReturnsSendEmailFailedError()
    {
        var user = new User { Id = Guid.NewGuid(), FirstName = "Jane", UserName = "user@example.com", Email = "user@example.com" };
        _userManager.FindByEmailAsync(Arg.Any<string>()).Returns(user);
        _authShareService.ResetPasswordExpirationHours.Returns(4);

        var writeUnitOfWork = UnitOfWorkFake.CreateWrite();
        writeUnitOfWork.RegisterRepository([user]);
        var handler = CreateHandler(writeUnitOfWork, ConfigurationTestFactory.Build());

        var result = await handler.Handle(Command(), CancellationToken.None);

        result.Should().Be(AuthControllerMsg.ForgotPassword.SendEmailFailed);
    }
}
