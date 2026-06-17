using Application.Common.Services;
using Application.Common.Validators;
using Application.Dtos.Base;
using Application.Dtos.Email;
using Application.Interfaces.Cqrs;
using Application.Interfaces.DataAccess;
using Application.Interfaces.Email;
using Domain.Constants;
using Domain.Entities;
using Domain.Helpers;
using Domain.Messages;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Application.Features.Auth.Commands.ChangePasswordCommand;

public record ChangePasswordCommand : ICommand<string>
{
    public required string OldPassword { get; set; }

    public required string NewPassword { get; set; }

    public required string ConfirmPassword { get; set; }

    public Guid DeviceUuid { get; set; }
}

public class ChangePasswordHandler(
    UserManager<User> userManager,
    SignInManager<User> signInManager,
    IWriteUnitOfWork writeUnitOfWork,
    IConfiguration configuration,
    IEmailService emailService,
    ILogger<ChangePasswordCommand> logger,
    IUserContextService userContextService) : IRequestHandler<ChangePasswordCommand, string>
{
    public async Task<string> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        var userContext = userContextService.GetCurrentUserContext();
        var userContextErrorCode = UserContextValidator.ValidateUserContext(userContext);
        if (!string.IsNullOrEmpty(userContextErrorCode))
        {
            return userContextErrorCode;
        }

        var user = await userManager.FindByIdAsync(userContext.UserId.ToString());
        if (user == null)
        {
            return AuthControllerMsg.ChangePassword.UserNotFound;
        }

        if (string.IsNullOrEmpty(user.UserName))
        {
            return AuthControllerMsg.ChangePassword.EmptyUserName;
        }

        var validPassword = await this.ValidatePassword(user.UserName, request.OldPassword);
        if (!validPassword)
        {
            return AuthControllerMsg.ChangePassword.InvalidOldPassword;
        }

        if (!request.NewPassword.ValidatePasswordPolicy())
        {
            return AuthControllerMsg.ChangePassword.NewPasswordNotStrongEnough;
        }

        if (string.Equals(request.NewPassword, request.OldPassword, StringComparison.OrdinalIgnoreCase))
        {
            return AuthControllerMsg.ChangePassword.NewPassSameAsOldPass;
        }

        if (!request.NewPassword.Equals(request.ConfirmPassword))
        {
            return AuthControllerMsg.ChangePassword.PasswordsDoNotMatch;
        }

        user.PasswordHash = request.NewPassword.HashPassword();
        var result = await userManager.UpdateAsync(user);

        if (result.Succeeded)
        {
            var userTokenRepo = writeUnitOfWork.GetRepository<UserToken>();
            var userToken = (await userTokenRepo.QueryCondition(i => i.UserId == user.Id
                                                                     && i.DeviceUuid != request.DeviceUuid))
                .ToList();

            await userTokenRepo.Delete(userToken);
            await writeUnitOfWork.SaveChanges();

            await SendChangePassEmail(user);

            return string.Empty;
        }

        return AuthControllerMsg.ChangePassword.Failed;
    }

    private async Task<bool> ValidatePassword(string userName, string password)
    {
        var result = await signInManager.PasswordSignInAsync(userName, password, false, true);
        return result.Succeeded;
    }

    private async Task<string> SendChangePassEmail(User user)
    {
        if (string.IsNullOrEmpty(user.Email))
        {
            logger.LogWarning("SendNewDeviceEmail Failed. User {id} dont have email", user.Id);
            return string.Empty;
        }

        var emailTemplate = configuration.GetSection(ConfigKeys.Security.Email.ChangePasswordNotification)
            .Get<EmailTemplateDto>();

        if (emailTemplate == null || string.IsNullOrEmpty(emailTemplate.Path))
        {
            logger.LogWarning("Template or template path for send mail change password not found");
            return string.Empty;
        }

        var dataBinding = new Dictionary<string, string>
        {
          { "{{UserName}}", user.UserName! }
        };

        var request = new SendEmailReqDto
        {
            ToEmails = [user.Email],
            Subject = string.IsNullOrEmpty(emailTemplate.Subject) ? "Change password notification" : emailTemplate.Subject,
            TemplatePath = emailTemplate.Path,
            DataBinding = dataBinding
        };

        return await emailService.SendEmail(request);
    }
}
