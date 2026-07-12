using Application.Features.Users.Commands.ChangeProfileCommand;
using Application.Features.Users.Queries.GetUserProfileQuery;
using AutoMapper;
using WebApi.Models.Requests.User;
using WebApi.Models.Responses.User;

namespace WebApi.Mappers;

public class UserMapper : Profile
{
    public UserMapper()
    {
        CreateMap<GetUserProfileDto, UserProfileRes>();
        CreateMap<ChangeProfileReq, ChangeProfileCommand>();
    }
}
