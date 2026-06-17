using Application.Common.Services;
using Application.Common.Validators;
using Application.Interfaces.Cqrs;
using Application.Interfaces.DataAccess;
using Domain.Constants;
using Domain.Entities;
using Domain.Messages;
using MediatR;

namespace Application.Features.Users.Commands.DeactivateUserCommand;

public record DeactivateUserCommand : ICommand<string>
{
    public Guid Id { get; init; }
}

public class DeactivateCommandHandler(
    IWriteUnitOfWork unitOfWork,
    IUserContextService userContextService) : IRequestHandler<DeactivateUserCommand, string>
{
    public async Task<string> Handle(DeactivateUserCommand request, CancellationToken cancellationToken)
    {
        var userContext = userContextService.GetCurrentUserContext();

        var userContextErrorCode = UserContextValidator.ValidateUserContext(userContext);
        if (!string.IsNullOrEmpty(userContextErrorCode))
        {
            return userContextErrorCode;
        }

        if (userContext.UserId == request.Id) return UserControllerMsg.Deactivate.CannotDeactivateOwn;

        var userRepo = unitOfWork.GetRepository<User>();
        var user = await userRepo.FindById(request.Id);

        if (user == null) return UserControllerMsg.Deactivate.NotFound;

        if (user.Id == UserConstants.AdminId) return UserControllerMsg.Deactivate.CannotDeactivateAdmin;

        await userRepo.Delete(user);
        await unitOfWork.SaveChanges();

        return string.Empty;
    }
}