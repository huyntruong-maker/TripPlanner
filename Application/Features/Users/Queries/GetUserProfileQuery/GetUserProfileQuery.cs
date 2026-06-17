using Application.Common.Services;
using Application.Common.Validators;
using Application.Dtos.Base;
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
        var userRoleQuery = await unitOfWork.GetRepository<UserRole>().QueryCondition(x => x.UserId == userId);
        var roleQuery = await unitOfWork.GetRepository<Role>().QueryAll();

        var userProfile = await (
            from us in userQuery
            join ur in userRoleQuery on us.Id equals ur.UserId
            join ro in roleQuery on ur.RoleId equals ro.Id
            select new
            {
                us.Id,
                us.UserName,
                us.FirstName,
                us.LastName,
                us.Email,
                us.PhoneNumber,
                RoleId = ro.Id,
                RoleName = ro.DisplayName
            })
            .GroupBy(gr => new { gr.UserName, gr.FirstName, gr.LastName, gr.Email, gr.PhoneNumber, gr.Id })
            .Select(gr => new GetUserProfileDto()
            {
                Email = gr.Key.Email,
                FirstName = gr.Key.FirstName,
                LastName = gr.Key.LastName,
                UserName = gr.Key.UserName,
                PhoneNumber = gr.Key.PhoneNumber,
                Id = gr.Key.Id,
                Roles = gr.Select(i => new RoleDetailDto()
                {
                    Id = i.RoleId,
                    DisplayName = i.RoleName
                }).ToArray()
            })
            .FirstOrDefaultAsync();

        return userProfile != null
            ? (string.Empty, userProfile)
            : (UserControllerMsg.GetProfile.NotFound, null);
    }
}
