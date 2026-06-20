using Application.Common.Services;
using Application.Common.Validators;
using Application.Dtos.Base;
using Application.Interfaces.Cqrs;
using Application.Interfaces.DataAccess;
using Domain.Constants;
using Domain.Entities;
using Domain.Messages;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Users.Commands.UpdateUserCommand;

public record UpdateUserCommand : ICommand<string>
{
    public required Guid Id { get; set; }
    public required UpdateUserReqDto UpdateUserReqDto { get; set; }
}

public class UpdateUserCommandHandler(
    IWriteUnitOfWork unitOfWork,
    IUserContextService userContextService) : IRequestHandler<UpdateUserCommand, string>
{
    public async Task<string> Handle(UpdateUserCommand requestDto, CancellationToken cancellationToken)
    {
        var userContext = userContextService.GetCurrentUserContext();
        var request = requestDto.UpdateUserReqDto;

        var userRepo = unitOfWork.GetRepository<User>();

        var user = await userRepo.FindById(requestDto.Id);
        if (user == null)
        {
            return UserControllerMsg.Update.NotFound;
        }

        var validationResult = await ValidateUpdateUser(request, user, userRepo, userContext);
        if (!string.IsNullOrEmpty(validationResult))
        {
            return validationResult;
        }

        // Update role if it not current user
        if (userContext.UserId != user.Id && request.RoleIds.Length != 0)
        {
            var userRoleRepo = unitOfWork.GetRepository<UserRole>();

            // Ignore filter isDeleted for existing user roles
            var existingRoles = await (await userRoleRepo.QueryCondition(x => x.UserId == user.Id))
                                        .IgnoreQueryFilters().ToListAsync();
            var existingRoleIds = existingRoles.Select(x => x.RoleId).ToHashSet();
            var requestedRoleIds = request.RoleIds.ToHashSet();

            // Update Reactive user role again if it currently flag as delete
            var rolesToReactivate = existingRoles.Where(x =>
                            requestedRoleIds.Contains(x.RoleId)
                            && x.IsDeleted).ToList();
            if (rolesToReactivate.Any())
            {
                rolesToReactivate.ForEach(x => x.IsDeleted = false);
                await userRoleRepo.Update(rolesToReactivate);
            }

            // Add new role if have
            var newRoles = requestedRoleIds.Except(existingRoleIds)
                     .Select(roleId => new UserRole
                     {
                         UserId = user.Id,
                         RoleId = roleId
                     }).ToList();
            if (newRoles.Any())
            {
                await userRoleRepo.Add(newRoles);
            }

            // Deactivate role if remove from request
            var rolesToDelete = existingRoles.Where(x =>
                            !requestedRoleIds.Contains(x.RoleId)
                            && !x.IsDeleted).ToList();
            if (rolesToDelete.Any())
            {
                await userRoleRepo.Delete(rolesToDelete);
            }

            // If user role updated then remove all user session
            var isUserRolesUpdated = newRoles.Any() || rolesToReactivate.Any() || rolesToDelete.Any();
            if (isUserRolesUpdated)
            {
                var userTokenRepo = unitOfWork.GetRepository<UserToken>();
                var userSessions = (await userTokenRepo.QueryCondition(x => x.UserId == user.Id)).ToList();

                if (userSessions.Any())
                {
                    await userTokenRepo.Delete(userSessions);
                }
            }
        }

        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.Email = request.Email;
        user.UserName = request.UserName;
        user.PhoneNumber = request.PhoneNumber;
        user.NormalizedEmail = request.Email.ToUpper();
        user.NormalizedUserName = request.UserName.ToUpper();

        await userRepo.Update(user);
        await unitOfWork.SaveChanges();

        return string.Empty;
    }

    private async Task<string> ValidateUpdateUser(
        UpdateUserReqDto request
        , User user
        , IBaseWriteRepository<User> userRepo
        , CurrentUserContextDto userContext)
    {
        // Validate current user request api
        var userContextErrorCode = UserContextValidator.ValidateUserContext(userContext);
        if (!string.IsNullOrEmpty(userContextErrorCode))
        {
            return userContextErrorCode;
        }

        // Cannot update admin account
        if (user.Id == UserConstants.AdminId)
        {
            return UserControllerMsg.Update.CannotUpdateAdminAccount;
        }

        // Check request Email or UserName change is unique
        var isEmailChanged = request.Email != user.Email;
        var isUserNameChanged = request.UserName != user.UserName;
        if (isEmailChanged || isUserNameChanged)
        {
            var existingUser = await userRepo.Single(x =>
                    ((isEmailChanged && x.Email == request.Email) ||
                    (isUserNameChanged && x.UserName == request.UserName)) &&
                    x.Id != user.Id);

            if (existingUser != null)
            {
                return existingUser.Email == request.Email
                    ? UserControllerMsg.Update.DuplicatedEmail
                    : UserControllerMsg.Update.DuplicatedUserName;
            }
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
            return UserControllerMsg.Update.RoleNotFound;
        }

        // Update User role must not surpass User update them
        var maxCurrentUserRoleLevel = userContext.RolesLevel.Max();
        if (requestedRoles.Any(r => r.Level > maxCurrentUserRoleLevel))
        {
            return UserControllerMsg.Update.InvalidRolePermission;
        }

        return string.Empty;
    }
}