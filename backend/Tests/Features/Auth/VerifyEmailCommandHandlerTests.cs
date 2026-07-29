using Application.Features.Auth.Commands.VerifyEmailCommand;
using Domain.Entities;
using Domain.Messages;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using NSubstitute;
using Tests.TestSupport;

namespace Tests.Features.Auth;

public class VerifyEmailCommandHandlerTests
{
    private readonly UserManager<User> _userManager = IdentityTestFactory.CreateUserManager();

    private VerifyEmailCommandHandler CreateHandler(Application.Interfaces.DataAccess.IWriteUnitOfWork writeUnitOfWork)
    {
        return new VerifyEmailCommandHandler(_userManager, writeUnitOfWork);
    }

    private static VerifyEmailCommand Command(string token = "verify-token") => new() { Token = token };

    [Fact]
    public async Task Handle_TokenNotFound_ReturnsTokenInvalidError()
    {
        var writeUnitOfWork = UnitOfWorkFake.CreateWrite();
        writeUnitOfWork.RegisterRepository<EmailVerificationToken>();
        var handler = CreateHandler(writeUnitOfWork);

        var result = await handler.Handle(Command(), CancellationToken.None);

        result.Should().Be(AuthControllerMsg.VerifyEmail.TokenInvalid);
    }

    [Fact]
    public async Task Handle_AlreadyConsumed_ReturnsAlreadyVerifiedError()
    {
        var token = new EmailVerificationToken
        {
            UserId = Guid.NewGuid(),
            Token = "verify-token",
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
            ConsumedAt = DateTimeOffset.UtcNow.AddMinutes(-5)
        };
        var writeUnitOfWork = UnitOfWorkFake.CreateWrite();
        writeUnitOfWork.RegisterRepository([token]);
        var handler = CreateHandler(writeUnitOfWork);

        var result = await handler.Handle(Command(), CancellationToken.None);

        result.Should().Be(AuthControllerMsg.VerifyEmail.AlreadyVerified);
    }

    [Fact]
    public async Task Handle_TokenExpired_ReturnsTokenExpiredError()
    {
        var token = new EmailVerificationToken
        {
            UserId = Guid.NewGuid(),
            Token = "verify-token",
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(-1)
        };
        var writeUnitOfWork = UnitOfWorkFake.CreateWrite();
        writeUnitOfWork.RegisterRepository([token]);
        var handler = CreateHandler(writeUnitOfWork);

        var result = await handler.Handle(Command(), CancellationToken.None);

        result.Should().Be(AuthControllerMsg.VerifyEmail.TokenExpired);
    }

    [Fact]
    public async Task Handle_UserNotFound_ReturnsTokenInvalidError()
    {
        var token = new EmailVerificationToken
        {
            UserId = Guid.NewGuid(),
            Token = "verify-token",
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1)
        };
        _userManager.FindByIdAsync(Arg.Any<string>()).Returns((User?)null);

        var writeUnitOfWork = UnitOfWorkFake.CreateWrite();
        writeUnitOfWork.RegisterRepository([token]);
        var handler = CreateHandler(writeUnitOfWork);

        var result = await handler.Handle(Command(), CancellationToken.None);

        result.Should().Be(AuthControllerMsg.VerifyEmail.TokenInvalid);
    }

    [Fact]
    public async Task Handle_ValidToken_ConfirmsEmailAndReturnsEmptyError()
    {
        var user = new User { Id = Guid.NewGuid(), FirstName = "Jane", EmailConfirmed = false };
        var token = new EmailVerificationToken
        {
            UserId = user.Id,
            Token = "verify-token",
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1)
        };
        _userManager.FindByIdAsync(Arg.Any<string>()).Returns(user);
        _userManager.UpdateAsync(Arg.Any<User>()).Returns(IdentityResult.Success);

        var writeUnitOfWork = UnitOfWorkFake.CreateWrite();
        writeUnitOfWork.RegisterRepository([token]);
        var handler = CreateHandler(writeUnitOfWork);

        var result = await handler.Handle(Command(), CancellationToken.None);

        result.Should().Be(string.Empty);
        user.EmailConfirmed.Should().BeTrue();
        token.ConsumedAt.Should().NotBeNull();
        await writeUnitOfWork.Received(1).SaveChanges();
    }
}
