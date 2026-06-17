using Application.Features.Users.Commands.ChangeProfileCommand;
using Application.Features.Users.Commands.CreateUserCommand;
using Application.Features.Users.Commands.UpdateUserCommand;
using Application.Features.Users.Queries.GetUserProfileQuery;
using Application.Features.Users.Queries.GetUserQuery;
using Application.Features.Users.Queries.GetUsersQuery;
using AutoMapper;
using Domain.Entities;
using WebApi.Models.Requests.User;
using WebApi.Models.Responses.User;

namespace WebApi.Mappers;

public class UserMapper : Profile
{
    public UserMapper()
    {
        CreateMap<User, GetUserDto>();
        CreateMap<GetUserDto, UserRes>();

        CreateMap<GetUserProfileDto, UserProfileRes>();
        CreateMap<ChangeProfileReq, ChangeProfileCommand>();

        CreateMap<GetUsersDto, UsersRes>();
        CreateMap<UsersSearchReq, UsersSearchDto>();

        CreateMap<CreateUserReq, CreateUserReqDto>();

        CreateMap<UpdateUserReq, UpdateUserReqDto>();
    }
}