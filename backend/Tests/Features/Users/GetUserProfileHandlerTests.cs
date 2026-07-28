using Application.Common.Services;
using Application.Dtos.Base;
using Application.Features.Users.Queries.GetUserProfileQuery;
using Domain.Entities;
using Domain.Messages;
using FluentAssertions;
using NSubstitute;
using Tests.TestSupport;

namespace Tests.Features.Users;

public class GetUserProfileHandlerTests
{
    [Fact]
    public async Task Handle_UnauthenticatedUser_ReturnsInvalidUserError()
    {
        var readUnitOfWork = UnitOfWorkFake.CreateRead();
        readUnitOfWork.RegisterRepository<User>();
        var userContextService = Substitute.For<IUserContextService>();
        userContextService.GetCurrentUserContext().Returns(new CurrentUserContextDto { UserId = Guid.Empty });
        var handler = new GetUserProfileHandler(readUnitOfWork, userContextService);

        var (errorCode, profile) = await handler.Handle(new GetUserProfileQuery(), CancellationToken.None);

        errorCode.Should().Be(ShareControllerMsg.CurrentUserContext.InvalidUser);
        profile.Should().BeNull();
    }

    [Fact]
    public async Task Handle_UserNotFound_ReturnsNotFoundError()
    {
        var readUnitOfWork = UnitOfWorkFake.CreateRead();
        readUnitOfWork.RegisterRepository<User>();
        var userContextService = Substitute.For<IUserContextService>();
        userContextService.GetCurrentUserContext().Returns(new CurrentUserContextDto { UserId = Guid.NewGuid() });
        var handler = new GetUserProfileHandler(readUnitOfWork, userContextService);

        var (errorCode, profile) = await handler.Handle(new GetUserProfileQuery(), CancellationToken.None);

        errorCode.Should().Be(UserControllerMsg.GetProfile.NotFound);
        profile.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ValidRequest_ReturnsRequestingUsersOwnProfile()
    {
        var currentUser = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Jane",
            LastName = "Doe",
            UserName = "jane.doe",
            Email = "jane@example.com",
            PhoneNumber = "555-0100"
        };
        var readUnitOfWork = UnitOfWorkFake.CreateRead();
        readUnitOfWork.RegisterRepository([currentUser]);
        var userContextService = Substitute.For<IUserContextService>();
        userContextService.GetCurrentUserContext().Returns(new CurrentUserContextDto { UserId = currentUser.Id });
        var handler = new GetUserProfileHandler(readUnitOfWork, userContextService);

        var (errorCode, profile) = await handler.Handle(new GetUserProfileQuery(), CancellationToken.None);

        errorCode.Should().Be(string.Empty);
        profile.Should().NotBeNull();
        profile!.Id.Should().Be(currentUser.Id);
        profile.UserName.Should().Be(currentUser.UserName);
        profile.FirstName.Should().Be(currentUser.FirstName);
        profile.LastName.Should().Be(currentUser.LastName);
        profile.Email.Should().Be(currentUser.Email);
        profile.PhoneNumber.Should().Be(currentUser.PhoneNumber);
    }

    [Fact]
    public async Task Handle_MultipleUsersExist_ReturnsOnlyRequestingUsersProfileNotOthers()
    {
        var currentUser = new User { Id = Guid.NewGuid(), FirstName = "Jane", UserName = "jane.doe", Email = "jane@example.com" };
        var otherUser = new User { Id = Guid.NewGuid(), FirstName = "John", UserName = "john.doe", Email = "john@example.com" };
        var readUnitOfWork = UnitOfWorkFake.CreateRead();
        readUnitOfWork.RegisterRepository([currentUser, otherUser]);
        var userContextService = Substitute.For<IUserContextService>();
        userContextService.GetCurrentUserContext().Returns(new CurrentUserContextDto { UserId = currentUser.Id });
        var handler = new GetUserProfileHandler(readUnitOfWork, userContextService);

        var (_, profile) = await handler.Handle(new GetUserProfileQuery(), CancellationToken.None);

        profile.Should().NotBeNull();
        profile!.Id.Should().Be(currentUser.Id);
        profile.Id.Should().NotBe(otherUser.Id);
        profile.UserName.Should().Be(currentUser.UserName);
    }
}
