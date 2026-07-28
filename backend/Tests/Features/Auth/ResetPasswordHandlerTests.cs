using Application.Dtos.Email;
using Application.Features.Auth.Commands.ResetPasswordCommand;
using Application.Interfaces.Email;
using Domain.Constants;
using Domain.Entities;
using Domain.Messages;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Tests.TestSupport;

namespace Tests.Features.Auth;

public class ResetPasswordHandlerTests
{
    private readonly IEmailService _emailService = Substitute.For<IEmailService>();

    private ResetPasswordHandler CreateHandler(
        Application.Interfaces.DataAccess.IWriteUnitOfWork writeUnitOfWork,
        Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        return new ResetPasswordHandler(writeUnitOfWork, configuration, _emailService, NullLogger<ResetPasswordHandler>.Instance);
    }

    private static ResetPasswordCommand Command(string token = "reset-token") => new()
    {
        Token = token,
        NewPassword = "NewPass1!"
    };

    private static User NewUserWithToken(string token, DateTimeOffset? expiration = null) => new()
    {
        Id = Guid.NewGuid(),
        FirstName = "Jane",
        UserName = "jane@example.com",
        Email = "jane@example.com",
        ResetPasswordToken = token,
        ResetPasswordExpiration = expiration ?? DateTimeOffset.UtcNow.AddHours(1)
    };

    [Fact]
    public async Task Handle_TokenNotFound_ReturnsValidateTokenFailedError()
    {
        var writeUnitOfWork = UnitOfWorkFake.CreateWrite();
        writeUnitOfWork.RegisterRepository<User>();
        writeUnitOfWork.RegisterRepository<UserToken>();

        var handler = CreateHandler(writeUnitOfWork, ConfigurationTestFactory.Build());

        var result = await handler.Handle(Command(), CancellationToken.None);

        result.Should().Be(AuthControllerMsg.ResetPassword.ValidateTokenFailed);
    }

    [Fact]
    public async Task Handle_ExpiredToken_ReturnsValidateTokenFailedError()
    {
        var user = NewUserWithToken("reset-token", DateTimeOffset.UtcNow.AddHours(-1));
        var writeUnitOfWork = UnitOfWorkFake.CreateWrite();
        writeUnitOfWork.RegisterRepository([user]);
        writeUnitOfWork.RegisterRepository<UserToken>();

        var handler = CreateHandler(writeUnitOfWork, ConfigurationTestFactory.Build());

        var result = await handler.Handle(Command(), CancellationToken.None);

        result.Should().Be(AuthControllerMsg.ResetPassword.ValidateTokenFailed);
    }

    [Fact]
    public async Task Handle_ValidToken_UpdatesPasswordAndDeletesExistingSessions()
    {
        var user = NewUserWithToken("reset-token");
        var existingSession = new UserToken
        {
            UserId = user.Id,
            Value = "old-value",
            RefreshToken = "old-refresh",
            LoginProvider = GlobalConstants.JwtLoginToken,
            Name = "session-1"
        };
        _emailService.SendEmail(Arg.Any<SendEmailReqDto>()).Returns(string.Empty);

        var writeUnitOfWork = UnitOfWorkFake.CreateWrite();
        writeUnitOfWork.RegisterRepository([user]);
        writeUnitOfWork.RegisterRepository([existingSession]);
        var configuration = ConfigurationTestFactory.WithEmailTemplate(
            ConfigKeys.Security.Email.ResetPasswordSuccessNotification,
            "Password changed",
            "https://app.example.com");

        var handler = CreateHandler(writeUnitOfWork, configuration);
        var originalHash = user.PasswordHash;

        var result = await handler.Handle(Command(), CancellationToken.None);

        result.Should().Be(string.Empty);
        user.PasswordHash.Should().NotBe(originalHash);
        user.ResetPasswordToken.Should().BeEmpty();
        user.ResetPasswordExpiration.Should().BeNull();
        var tokenRepo = writeUnitOfWork.GetRepository<UserToken>();
        await tokenRepo.Received(1).Delete(Arg.Is<IEnumerable<UserToken>>(list => list.Contains(existingSession)));
        await writeUnitOfWork.Received(1).SaveChanges();
    }
}
