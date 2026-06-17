using Application.Common.Services;
using Application.Common.Validators;
using Application.Dtos.Base;
using Application.Interfaces.Cqrs;
using Application.Interfaces.DataAccess;
using Domain.Entities;
using Domain.Messages;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Users.Queries.GetUserQuery;

public record GetUserQuery : IConsistentQuery<(string, GetUserDto?)>
{
    public Guid Id { get; init; }
}

public class GetUserQueryHandler(
    IReadUnitOfWork unitOfWork,
    IUserContextService userContextService) : IRequestHandler<GetUserQuery, (string, GetUserDto?)>
{
    public async Task<(string, GetUserDto?)> Handle(GetUserQuery request, CancellationToken cancellationToken)
    {
        var userContext = userContextService.GetCurrentUserContext();

        var userContextErrorCode = UserContextValidator.ValidateUserContext(userContext);
        if (!string.IsNullOrEmpty(userContextErrorCode))
        {
            return (userContextErrorCode, null);
        }

        var userQuery = await unitOfWork.GetRepository<User>().QueryCondition(x => x.Id == request.Id);
        var userRoleQuery = await unitOfWork.GetRepository<UserRole>().QueryCondition(x => x.UserId == request.Id);
        var roleQuery = await unitOfWork.GetRepository<Role>().QueryAll();

        var user = await (
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
            .GroupBy(gr => new { gr.Id, gr.UserName, gr.FirstName, gr.LastName, gr.Email, gr.PhoneNumber })
            .Select(gr => new GetUserDto
            {
                Id = gr.Key.Id,
                Email = gr.Key.Email,
                FirstName = gr.Key.FirstName,
                LastName = gr.Key.LastName,
                UserName = gr.Key.UserName,
                PhoneNumber = gr.Key.PhoneNumber,
                Roles = gr.Select(i => new RoleDetailDto()
                {
                    Id = i.RoleId,
                    DisplayName = i.RoleName
                }).ToArray()
            })
            .FirstOrDefaultAsync(cancellationToken: cancellationToken);

        if (user == null)
        {
            return (UserControllerMsg.Get.NotFound, null);
        }

        return (string.Empty, user);
    }
}