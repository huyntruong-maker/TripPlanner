using Application.Common.Services;
using Application.Dtos.Base;
using Application.Features.Users.Commands.ChangeProfileCommand;
using Domain.Constants;
using Domain.Entities;
using Domain.Messages;
using FluentAssertions;
using NSubstitute;
using Tests.TestSupport;

namespace Tests.Features.Users;

public class ChangeProfileHandlerTests
{
    private static ChangeProfileCommand Command(
        string userName = "jane.doe",
        string firstName = "Jane",
        string? lastName = "Doe",
        string email = "jane@example.com",
        string? phoneNumber = null) => new()
    {
        UserName = userName,
        FirstName = firstName,
        LastName = lastName,
        Email = email,
        PhoneNumber = phoneNumber
    };

    [Fact]
    public async Task Handle_UnauthenticatedUser_ReturnsInvalidUserError()
    {
        var writeUnitOfWork = UnitOfWorkFake.CreateWrite();
        writeUnitOfWork.RegisterRepository<User>();
        var userContextService = Substitute.For<IUserContextService>();
        userContextService.GetCurrentUserContext().Returns(new CurrentUserContextDto { UserId = Guid.Empty });
        var handler = new ChangeProfileHandler(writeUnitOfWork, userContextService);

        var result = await handler.Handle(Command(), CancellationToken.None);

        result.Should().Be(ShareControllerMsg.CurrentUserContext.InvalidUser);
    }

    [Fact]
    public async Task Handle_UserNotFound_ReturnsNotFoundError()
    {
        var writeUnitOfWork = UnitOfWorkFake.CreateWrite();
        writeUnitOfWork.RegisterRepository<User>();
        var userContextService = Substitute.For<IUserContextService>();
        userContextService.GetCurrentUserContext().Returns(new CurrentUserContextDto { UserId = Guid.NewGuid() });
        var handler = new ChangeProfileHandler(writeUnitOfWork, userContextService);

        var result = await handler.Handle(Command(), CancellationToken.None);

        result.Should().Be(UserControllerMsg.ChangeProfile.NotFound);
    }

    [Fact]
    public async Task Handle_AdminUser_ReturnsCannotChangeAdminProfileError()
    {
        var adminUser = new User { Id = UserConstants.AdminId, FirstName = "Admin", UserName = "admin", Email = "admin@example.com" };
        var writeUnitOfWork = UnitOfWorkFake.CreateWrite();
        writeUnitOfWork.RegisterRepository([adminUser]);
        var userContextService = Substitute.For<IUserContextService>();
        userContextService.GetCurrentUserContext().Returns(new CurrentUserContextDto { UserId = UserConstants.AdminId });
        var handler = new ChangeProfileHandler(writeUnitOfWork, userContextService);

        var result = await handler.Handle(Command(), CancellationToken.None);

        result.Should().Be(UserControllerMsg.ChangeProfile.CannotChangeAdminProfile);
    }

    [Fact]
    public async Task Handle_EmailAlreadyUsedByAnotherUser_ReturnsDuplicatedEmailError()
    {
        var currentUser = new User { Id = Guid.NewGuid(), FirstName = "Jane", UserName = "jane.doe", Email = "jane@example.com" };
        var otherUser = new User { Id = Guid.NewGuid(), FirstName = "John", UserName = "john.doe", Email = "taken@example.com" };
        var writeUnitOfWork = UnitOfWorkFake.CreateWrite();
        writeUnitOfWork.RegisterRepository([currentUser, otherUser]);
        var userContextService = Substitute.For<IUserContextService>();
        userContextService.GetCurrentUserContext().Returns(new CurrentUserContextDto { UserId = currentUser.Id });
        var handler = new ChangeProfileHandler(writeUnitOfWork, userContextService);

        var result = await handler.Handle(Command(email: "taken@example.com"), CancellationToken.None);

        result.Should().Be(UserControllerMsg.ChangeProfile.DuplicatedEmail);
    }

    [Fact]
    public async Task Handle_UserNameAlreadyUsedByAnotherUser_ReturnsDuplicatedUserNameError()
    {
        var currentUser = new User { Id = Guid.NewGuid(), FirstName = "Jane", UserName = "jane.doe", Email = "jane@example.com" };
        var otherUser = new User { Id = Guid.NewGuid(), FirstName = "John", UserName = "taken.name", Email = "john@example.com" };
        var writeUnitOfWork = UnitOfWorkFake.CreateWrite();
        writeUnitOfWork.RegisterRepository([currentUser, otherUser]);
        var userContextService = Substitute.For<IUserContextService>();
        userContextService.GetCurrentUserContext().Returns(new CurrentUserContextDto { UserId = currentUser.Id });
        var handler = new ChangeProfileHandler(writeUnitOfWork, userContextService);

        var result = await handler.Handle(Command(userName: "taken.name"), CancellationToken.None);

        result.Should().Be(UserControllerMsg.ChangeProfile.DuplicatedUserName);
    }

    [Fact]
    public async Task Handle_EmailAndUserNameUnchanged_SkipsDuplicateCheckAndUpdatesOtherFields()
    {
        var currentUser = new User { Id = Guid.NewGuid(), FirstName = "Jane", UserName = "jane.doe", Email = "jane@example.com" };
        var writeUnitOfWork = UnitOfWorkFake.CreateWrite();
        writeUnitOfWork.RegisterRepository([currentUser]);
        var userContextService = Substitute.For<IUserContextService>();
        userContextService.GetCurrentUserContext().Returns(new CurrentUserContextDto { UserId = currentUser.Id });
        var handler = new ChangeProfileHandler(writeUnitOfWork, userContextService);

        var result = await handler.Handle(
            Command(userName: "jane.doe", email: "jane@example.com", firstName: "Janet"),
            CancellationToken.None);

        result.Should().Be(string.Empty);
        currentUser.FirstName.Should().Be("Janet");
    }

    [Fact]
    public async Task Handle_ValidRequest_UpdatesProfileAndReturnsEmptyError()
    {
        var currentUser = new User { Id = Guid.NewGuid(), FirstName = "Jane", UserName = "jane.doe", Email = "jane@example.com" };
        var writeUnitOfWork = UnitOfWorkFake.CreateWrite();
        writeUnitOfWork.RegisterRepository([currentUser]);
        var userContextService = Substitute.For<IUserContextService>();
        userContextService.GetCurrentUserContext().Returns(new CurrentUserContextDto { UserId = currentUser.Id });
        var handler = new ChangeProfileHandler(writeUnitOfWork, userContextService);

        var result = await handler.Handle(
            Command(userName: "jane.smith", email: "jane.smith@example.com", phoneNumber: "555-0100"),
            CancellationToken.None);

        result.Should().Be(string.Empty);
        currentUser.UserName.Should().Be("jane.smith");
        currentUser.Email.Should().Be("jane.smith@example.com");
        currentUser.PhoneNumber.Should().Be("555-0100");
        await writeUnitOfWork.Received(1).SaveChanges();
    }

    [Fact]
    public async Task Handle_ValidRequest_OnlyUpdatesRequestingUsersOwnProfile()
    {
        var currentUser = new User { Id = Guid.NewGuid(), FirstName = "Jane", UserName = "jane.doe", Email = "jane@example.com" };
        var otherUser = new User { Id = Guid.NewGuid(), FirstName = "John", UserName = "john.doe", Email = "john@example.com" };
        var writeUnitOfWork = UnitOfWorkFake.CreateWrite();
        writeUnitOfWork.RegisterRepository([currentUser, otherUser]);
        var userContextService = Substitute.For<IUserContextService>();
        userContextService.GetCurrentUserContext().Returns(new CurrentUserContextDto { UserId = currentUser.Id });
        var handler = new ChangeProfileHandler(writeUnitOfWork, userContextService);

        await handler.Handle(Command(firstName: "Janet"), CancellationToken.None);

        currentUser.FirstName.Should().Be("Janet");
        otherUser.FirstName.Should().Be("John");
        otherUser.UserName.Should().Be("john.doe");
        otherUser.Email.Should().Be("john@example.com");
    }
}
