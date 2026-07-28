using Application.Features.Auth.Commands.LogoutCommand;
using Application.Features.Auth.Shared;
using Domain.Constants;
using Domain.Entities;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.Tokens;
using NSubstitute;
using Tests.TestSupport;

namespace Tests.Features.Auth;

public class LogoutCommandHandlerTests
{
    private readonly IAuthShareService _authShareService = Substitute.For<IAuthShareService>();

    private LogoutCommandHandler CreateHandler(Application.Interfaces.DataAccess.IWriteUnitOfWork writeUnitOfWork)
    {
        return new LogoutCommandHandler(NullLogger<LogoutCommandHandler>.Instance, _authShareService, writeUnitOfWork);
    }

    private static LogoutCommand Command() => new() { Token = "access-token", RefreshToken = "refresh-token" };

    [Fact]
    public async Task Handle_InvalidToken_ReturnsFalse()
    {
        _authShareService.VerifyToken(Arg.Any<string>(), Arg.Any<string>()).Returns((User?)null);
        var writeUnitOfWork = UnitOfWorkFake.CreateWrite();
        var handler = CreateHandler(writeUnitOfWork);

        var result = await handler.Handle(Command(), CancellationToken.None);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_TokenUserMismatch_ReturnsFalse()
    {
        var user = new User { Id = Guid.NewGuid(), FirstName = "Jane" };
        _authShareService.VerifyToken(Arg.Any<string>(), Arg.Any<string>()).Returns(user);
        _authShareService.VerifyUserToken(Arg.Any<string>(), Arg.Any<string>()).Returns((User?)null);
        var writeUnitOfWork = UnitOfWorkFake.CreateWrite();
        var handler = CreateHandler(writeUnitOfWork);

        var result = await handler.Handle(Command(), CancellationToken.None);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_SecurityTokenValidationException_ReturnsFalse()
    {
        _authShareService.VerifyToken(Arg.Any<string>(), Arg.Any<string>())
            .Returns<User?>(_ => throw new SecurityTokenValidationException("invalid"));
        var writeUnitOfWork = UnitOfWorkFake.CreateWrite();
        var handler = CreateHandler(writeUnitOfWork);

        var result = await handler.Handle(Command(), CancellationToken.None);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ValidTokens_DeletesUserTokenAndReturnsTrue()
    {
        var user = new User { Id = Guid.NewGuid(), FirstName = "Jane" };
        var userToken = new UserToken
        {
            UserId = user.Id,
            Value = "access-token",
            RefreshToken = "refresh-token",
            LoginProvider = GlobalConstants.JwtLoginToken,
            Name = "session-1"
        };

        _authShareService.VerifyToken(Arg.Any<string>(), Arg.Any<string>()).Returns(user);
        _authShareService.VerifyUserToken(Arg.Any<string>(), Arg.Any<string>()).Returns(user);

        var writeUnitOfWork = UnitOfWorkFake.CreateWrite();
        writeUnitOfWork.RegisterRepository([userToken]);
        var handler = CreateHandler(writeUnitOfWork);

        var result = await handler.Handle(Command(), CancellationToken.None);

        result.Should().BeTrue();
        var tokenRepo = writeUnitOfWork.GetRepository<UserToken>();
        await tokenRepo.Received(1).Delete(Arg.Any<object[]>());
        await writeUnitOfWork.Received(1).SaveChanges();
    }
}
