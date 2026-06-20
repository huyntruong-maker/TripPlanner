using Application.Common.Services;
using Application.Common.Validators;
using Application.Dtos.Base;
using Application.Interfaces.Cqrs;
using Application.Interfaces.DataAccess;
using Domain.Entities;
using Domain.Helpers;
using MediatR;

namespace Application.Features.Users.Queries.GetUsersQuery;

public record GetUsersQuery : IConsistentQuery<(string, Pagination<GetUsersDto>?)>
{
    public required UsersSearchDto SearchDto { get; set; }
}

public class GetUsersQueryHandler(
    IReadUnitOfWork unitOfWork,
    IUserContextService userContextService) : IRequestHandler<GetUsersQuery, (string, Pagination<GetUsersDto>?)>
{
    public async Task<(string, Pagination<GetUsersDto>?)> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        var userContext = userContextService.GetCurrentUserContext();

        var userContextErrorCode = UserContextValidator.ValidateUserContext(userContext);
        if (!string.IsNullOrEmpty(userContextErrorCode))
        {
            return (userContextErrorCode, null);
        }

        var userQuery = await unitOfWork.GetRepository<User>().QueryAll();
        var userRoleQuery = await unitOfWork.GetRepository<UserRole>().QueryAll();
        var roleQuery = await unitOfWork.GetRepository<Role>().QueryAll();

        var searchDto = request.SearchDto;

        if (!string.IsNullOrWhiteSpace(searchDto.Keyword))
        {
            var keyword = searchDto.Keyword.ToLower().Trim();
            userQuery = userQuery.Where(i =>
                (!string.IsNullOrEmpty(i.UserName) && i.UserName.ToLower().Contains(keyword)) ||
                (!string.IsNullOrEmpty(i.FirstName) && i.FirstName.ToLower().Contains(keyword)) ||
                (!string.IsNullOrEmpty(i.LastName) && i.LastName.ToLower().Contains(keyword)) ||
                (!string.IsNullOrEmpty(i.Email) && i.Email.ToLower().Contains(keyword))
            );
        }

        userQuery = string.IsNullOrWhiteSpace(searchDto.Column)
                    ? userQuery.OrderBy(i => i.UserName)
                    : userQuery.ApplySorting(searchDto.Column, searchDto.Ascending);

        var queryResult = (
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
            .Select(gr => new GetUsersDto
            {
                Id = gr.Key.Id,
                Email = gr.Key.Email,
                FirstName = gr.Key.FirstName,
                LastName = gr.Key.LastName,
                UserName = gr.Key.UserName,
                PhoneNumber = gr.Key.PhoneNumber,
                Roles = gr.Select(i => new RoleDetailDto
                {
                    Id = i.RoleId,
                    DisplayName = i.RoleName
                }).ToArray()
            });

        return (string.Empty, new Pagination<GetUsersDto>(
            queryResult,
            searchDto.Start,
            searchDto.Length
        ));
    }
}
