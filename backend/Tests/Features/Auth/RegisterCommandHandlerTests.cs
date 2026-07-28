using Application.Dtos.Email;
using Application.Features.Auth.Commands.RegisterCommand;
using Application.Interfaces.Email;
using Tests.TestSupport;
using Domain.Constants;
using Domain.Entities;
using Domain.Messages;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Tests.Features.Auth;

public class RegisterCommandHandlerTests
{
    private readonly UserManager<User> _userManager = IdentityTestFactory.CreateUserManager();
    private readonly IEmailService _emailService = Substitute.For<IEmailService>();

    private RegisterCommandHandler CreateHandler(
        Application.Interfaces.DataAccess.IWriteUnitOfWork writeUnitOfWork,
        Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        return new RegisterCommandHandler(
            _userManager,
            writeUnitOfWork,
            _emailService,
            configuration,
            NullLogger<RegisterCommandHandler>.Instance);
    }

    private static RegisterCommand ValidCommand() => new()
    {
        Email = "new.user@example.com",
        Password = "StrongPass1!",
        FirstName = "New",
        LastName = "User"
    };

    [Fact]
    public async Task Handle_EmailAlreadyTaken_ReturnsEmailTakenError()
    {
        _userManager.FindByEmailAsync(Arg.Any<string>()).Returns(new User { FirstName = "Existing" });
        var writeUnitOfWork = UnitOfWorkFake.CreateWrite();
        var configuration = ConfigurationTestFactory.Build();
        var handler = CreateHandler(writeUnitOfWork, configuration);

        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        result.Should().Be(AuthControllerMsg.Register.EmailTaken);
        await _userManager.DidNotReceive().CreateAsync(Arg.Any<User>(), Arg.Any<string>());
    }

    [Fact]
    public async Task Handle_CreateAsyncFails_ReturnsRegistrationFailedError()
    {
        _userManager.FindByEmailAsync(Arg.Any<string>()).Returns((User?)null);
        _userManager.CreateAsync(Arg.Any<User>(), Arg.Any<string>())
            .Returns(IdentityResult.Failed(new IdentityError { Code = "PasswordTooShort" }));
        var writeUnitOfWork = UnitOfWorkFake.CreateWrite();
        var configuration = ConfigurationTestFactory.Build();
        var handler = CreateHandler(writeUnitOfWork, configuration);

        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        result.Should().Be(AuthControllerMsg.Register.RegistrationFailed);
    }

    [Fact]
    public async Task Handle_NewEmail_CreatesUserAndVerificationTokenAndReturnsEmptyError()
    {
        _userManager.FindByEmailAsync(Arg.Any<string>()).Returns((User?)null);
        _userManager.CreateAsync(Arg.Any<User>(), Arg.Any<string>()).Returns(IdentityResult.Success);
        _emailService.SendEmail(Arg.Any<SendEmailReqDto>()).Returns(string.Empty);

        var writeUnitOfWork = UnitOfWorkFake.CreateWrite();
        var tokenStore = writeUnitOfWork.RegisterRepository<EmailVerificationToken>();
        var configuration = ConfigurationTestFactory.WithEmailTemplate(
            ConfigKeys.Security.Email.EmailVerificationNotification,
            "Verify your email",
            "https://app.example.com/verify");

        var handler = CreateHandler(writeUnitOfWork, configuration);

        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        result.Should().Be(string.Empty);
        tokenStore.Should().ContainSingle();
        await writeUnitOfWork.Received(1).SaveChanges();
        await _emailService.Received(1).SendEmail(Arg.Any<SendEmailReqDto>());
    }

    [Fact]
    public async Task Handle_EmailSendFails_StillReturnsEmptyError()
    {
        _userManager.FindByEmailAsync(Arg.Any<string>()).Returns((User?)null);
        _userManager.CreateAsync(Arg.Any<User>(), Arg.Any<string>()).Returns(IdentityResult.Success);
        _emailService.SendEmail(Arg.Any<SendEmailReqDto>()).Returns("smtp failure");

        var writeUnitOfWork = UnitOfWorkFake.CreateWrite();
        writeUnitOfWork.RegisterRepository<EmailVerificationToken>();
        var configuration = ConfigurationTestFactory.WithEmailTemplate(
            ConfigKeys.Security.Email.EmailVerificationNotification,
            "Verify your email",
            "https://app.example.com/verify");

        var handler = CreateHandler(writeUnitOfWork, configuration);

        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        result.Should().Be(string.Empty);
    }
}
