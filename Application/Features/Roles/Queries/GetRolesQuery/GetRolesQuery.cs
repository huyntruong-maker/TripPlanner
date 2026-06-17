using Application.Common.Services;
using Application.Common.Validators;
using Application.Dtos.Base;
using Application.Interfaces.Cqrs;
using Application.Interfaces.DataAccess;
using Domain.Entities;
using MediatR;

namespace Application.Features.Roles.Queries.GetRolesQuery;

public record GetRolesQuery : IQuery<(string, Pagination<GetRolesDto>?)>
{
    public required RolesSearchDto SearchDto { get; set; }
}

public class GetRolesQueryHandler(
    IReadUnitOfWork unitOfWork,
    IUserContextService userContextService) : IRequestHandler<GetRolesQuery, (string, Pagination<GetRolesDto>?)>
{
    public async Task<(string, Pagination<GetRolesDto>?)> Handle(GetRolesQuery request, CancellationToken cancellationToken)
    {
        var userContext = userContextService.GetCurrentUserContext();

        var userContextErrorCode = UserContextValidator.ValidateUserContext(userContext);
        if (!string.IsNullOrEmpty(userContextErrorCode))
        {
            return (userContextErrorCode, null);
        }

        var searchDto = request.SearchDto;
        var maxPermission = userContext.RolesLevel.Max();

        // Get roles equal or less than current user
        var roleQuery = await unitOfWork.GetRepository<Role>()
                .QueryCondition(x => x.Level <= maxPermission);

        if (!string.IsNullOrWhiteSpace(searchDto.Keyword))
        {
            var keyword = searchDto.Keyword.ToLower().Trim();
            roleQuery = roleQuery.Where(i => i.DisplayName.ToLower().Contains(keyword));
        }

        var queryResult = roleQuery.Select(i => new GetRolesDto
        {
            Id = i.Id,
            DisplayName = i.DisplayName,
        });

        return (string.Empty, new Pagination<GetRolesDto>(
            queryResult,
            searchDto.Start,
            searchDto.Length
        ));
    }
}
