using Application.Features.Roles.Queries.GetRolesQuery;
using AutoMapper;
using WebApi.Models.Requests.Role;
using WebApi.Models.Responses.Role;

namespace WebApi.Mappers;

public class RoleMapper : Profile
{
    public RoleMapper()
    {
        CreateMap<GetRolesDto, RolesRes>();
        CreateMap<RoleSearchReq, RolesSearchDto>();
    }
}
