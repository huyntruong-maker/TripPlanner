using Application.Common.Services;
using Application.Common.Validators;
using Application.Dtos.Base;
using Application.Interfaces.Cqrs;
using Application.Interfaces.DataAccess;
using Domain.Entities;
using Domain.Helpers;
using Domain.Messages;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Users.Commands.CreateUserCommand;

public record CreateUserCommand : ICommand<string>
{
    public required CreateUserReqDto CreateUserReqDto { get; set; }
}

public class CreateUserCommandHandler(
    IWriteUnitOfWork unitOfWork,
    IUserContextService userContextService) : IRequestHandler<CreateUserCommand, string>
{
    public async Task<string> Handle(CreateUserCommand requestDto, CancellationToken cancellationToken)
    {
        var userContext = userContextService.GetCurrentUserContext();
        var request = requestDto.CreateUserReqDto;

        var userRepo = unitOfWork.GetRepository<User>();

        var validationResult = await ValidateCreateUser(request, userRepo, userContext);
        if (!string.IsNullOrEmpty(validationResult))
        {
            return validationResult;
        }

        var userRoles = request.RoleIds.Select(roleId => new UserRole { RoleId = roleId }).ToList();

        var user = new User
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            UserName = request.UserName,
            NormalizedUserName = request.UserName.ToUpper(),
            PasswordHash = request.Password.HashPassword(),
            Email = request.Email,
            NormalizedEmail = request.Email.ToUpper(),
            PhoneNumber = request.PhoneNumber,
            UserRoles = userRoles,
            SecurityStamp = Guid.NewGuid().ToString(),
            LockoutEnabled = true,
        };

        await userRepo.Add(user);
        await unitOfWork.SaveChanges();

        return string.Empty;
    }

    private async Task<string> ValidateCreateUser(
        CreateUserReqDto request
        , IBaseWriteRepository<User> userRepo
        , CurrentUserContextDto userContext)
    {
        // Validate current user request api
        var userContextErrorCode = UserContextValidator.ValidateUserContext(userContext);
        if (!string.IsNullOrEmpty(userContextErrorCode))
        {
            return userContextErrorCode;
        }

        // Check if Email or UserName already eixst
        var existingUser = await userRepo.Single(x => (x.Email == request.Email) || (x.UserName == request.UserName));
        if (existingUser != null)
        {
            if (existingUser.Email == request.Email) return UserControllerMsg.Create.EmailAlreadyExisted;
            if (existingUser.UserName == request.UserName) return UserControllerMsg.Create.UserNameAlreadyExisted;
        }

        // Get roles data from request role
        var requestedRoles = await (await unitOfWork.GetRepository<Role>()
            .QueryCondition(r => request.RoleIds.Contains(r.Id)))
            .Select(x => new { x.Id, x.Level })
            .ToListAsync();

        // Check if request roles exit
        var existingRoleIds = requestedRoles.Select(r => r.Id).ToHashSet();
        if (request.RoleIds.Except(existingRoleIds).Any())
        {
            return UserControllerMsg.Create.RoleNotFound;
        }

        // Create User role must not surpass User create them
        var maxCurrentUserRoleLevel = userContext.RolesLevel.Max();
        if (requestedRoles.Any(r => r.Level > maxCurrentUserRoleLevel))
        {
            return UserControllerMsg.Create.InvalidRolePermission;
        }

        return string.Empty;
    }
}
