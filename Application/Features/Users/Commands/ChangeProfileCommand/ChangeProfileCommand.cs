using Application.Common.Services;
using Application.Common.Validators;
using Application.Interfaces.Cqrs;
using Application.Interfaces.DataAccess;
using Domain.Constants;
using Domain.Entities;
using Domain.Messages;
using MediatR;

namespace Application.Features.Users.Commands.ChangeProfileCommand;

public record ChangeProfileCommand : ICommand<string>
{
    public required string UserName { get; set; }
    public required string FirstName { get; set; }
    public string? LastName { get; set; }
    public required string Email { get; set; }
    public string? PhoneNumber { get; set; }
}

public class ChangeProfileHandler(
    IWriteUnitOfWork unitOfWork,
    IUserContextService userContextService) : IRequestHandler<ChangeProfileCommand, string>
{
    public async Task<string> Handle(ChangeProfileCommand request, CancellationToken cancellationToken)
    {
        var userContext = userContextService.GetCurrentUserContext();
        var userContextErrorCode = UserContextValidator.ValidateUserContext(userContext);
        if (!string.IsNullOrEmpty(userContextErrorCode))
        {
            return userContextErrorCode;
        }

        var userRepo = unitOfWork.GetRepository<User>();
        var user = await userRepo.FindById(userContext.UserId);
        if (user == null)
        {
            return UserControllerMsg.ChangeProfile.NotFound;
        }

        var validationResult = await ValidateRequest(user, request, userRepo);
        if (!string.IsNullOrEmpty(validationResult))
        {
            return validationResult;
        }

        user.UserName = request.UserName;
        user.NormalizedUserName = request.UserName.ToUpper();
        user.Email = request.Email;
        user.NormalizedEmail = request.Email.ToUpper();
        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.PhoneNumber = request.PhoneNumber;

        await userRepo.Update(user);
        await unitOfWork.SaveChanges();

        return string.Empty;
    }

    private async Task<string> ValidateRequest(
        User user,
        ChangeProfileCommand request,
        IBaseWriteRepository<User> userRepo)
    {
        if (user.Id == UserConstants.AdminId)
        {
            return UserControllerMsg.ChangeProfile.CannotChangeAdminProfile;
        }

        var isEmailChanged = request.Email != user.Email;
        var isUserNameChanged = request.UserName != user.UserName;
        if (isEmailChanged || isUserNameChanged)
        {
            var existingUser = await userRepo.Single(x =>
                    ((isEmailChanged && x.Email == request.Email) ||
                    (isUserNameChanged && x.UserName == request.UserName)) &&
                    x.Id != user.Id);

            if (existingUser != null)
            {
                return existingUser.Email == request.Email
                    ? UserControllerMsg.ChangeProfile.DuplicatedEmail
                    : UserControllerMsg.ChangeProfile.DuplicatedUserName;
            }
        }

        return string.Empty;
    }
}
