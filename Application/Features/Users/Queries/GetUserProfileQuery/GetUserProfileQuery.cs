using Application.Common.Services;
using Application.Common.Validators;
using Application.Interfaces.Cqrs;
using Application.Interfaces.DataAccess;
using Domain.Entities;
using Domain.Messages;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Users.Queries.GetUserProfileQuery;

public record GetUserProfileQuery : IConsistentQuery<(string, GetUserProfileDto?)>;

public class GetUserProfileHandler(
    IReadUnitOfWork unitOfWork,
    IUserContextService userContextService) : IRequestHandler<GetUserProfileQuery, (string, GetUserProfileDto?)>
{
    public async Task<(string, GetUserProfileDto?)> Handle(GetUserProfileQuery request, CancellationToken cancellationToken)
    {
        var userContext = userContextService.GetCurrentUserContext();
        var userContextErrorCode = UserContextValidator.ValidateUserContext(userContext);
        if (!string.IsNullOrEmpty(userContextErrorCode))
        {
            return (userContextErrorCode, null);
        }

        var userId = userContext.UserId;

        var userQuery = await unitOfWork.GetRepository<User>().QueryCondition(x => x.Id == userId);

        var userProfile = await userQuery
            .Select(us => new GetUserProfileDto
            {
                Id = us.Id,
                UserName = us.UserName!,
                FirstName = us.FirstName,
                LastName = us.LastName,
                Email = us.Email!,
                PhoneNumber = us.PhoneNumber
            })
            .FirstOrDefaultAsync(cancellationToken);

        return userProfile != null
            ? (string.Empty, userProfile)
            : (UserControllerMsg.GetProfile.NotFound, null);
    }
}
