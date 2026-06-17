using Application.Dtos.Base;
using Application.Dtos.Email;
using Application.Features.Auth.Shared;
using Application.Interfaces.DataAccess;
using Application.Interfaces.Email;
using Domain.Constants;
using Domain.Entities;
using Domain.Helpers;
using Domain.Messages;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Web;
using Application.Common.Services;
using Application.Common.Validators;
using Application.Interfaces.Cqrs;

namespace Application.Features.Users.Commands.ResetUserPasswordCommand;

public record ResetUserPasswordCommand : ICommand<string>
{
    public required Guid Id { get; set; }
}

public class ResetUserPasswordCommandHandler(
    IWriteUnitOfWork writeUnitOfWork,
    IUserContextService userContextService,
    IEmailService emailService,
    IAuthShareService authShareService,
    IConfiguration configuration,
    ILogger<ResetUserPasswordCommand> logger) : IRequestHandler<ResetUserPasswordCommand, string>
{
    public async Task<string> Handle(ResetUserPasswordCommand request, CancellationToken cancellationToken)
    {
        var userContext = userContextService.GetCurrentUserContext();
        var userContextErrorCode = UserContextValidator.ValidateUserContext(userContext);
        if (!string.IsNullOrEmpty(userContextErrorCode))
        {
            return userContextErrorCode;
        }

        var userRepo = writeUnitOfWork.GetRepository<User>();
        var user = await userRepo.Single(x => x.Id == request.Id);
        if (user == null)
        {
            return UserControllerMsg.ResetPassword.NotFound;
        }

        if (string.IsNullOrWhiteSpace(user.Email))
        {
            return UserControllerMsg.ResetPassword.EmailNotExist;
        }

        var resetPasswordToken = CommonHelper.GenerateBase64GuidToken();
        var expirationTime = DateTimeHelper.GetDt().AddHours(authShareService.ResetPasswordExpirationHours);

        user.ResetPasswordToken = resetPasswordToken;
        user.ResetPasswordExpiration = expirationTime;
        await userRepo.Update(user);

        // Clear all session for reset pass user
        var userTokenRepo = writeUnitOfWork.GetRepository<UserToken>();
        var userSessions = (await userTokenRepo.QueryCondition(x => x.UserId == user.Id)).ToList();
        await userTokenRepo.Delete(userSessions);

        await writeUnitOfWork.SaveChanges();

        var sendEmailError = await SendResetPasswordEmail(user, resetPasswordToken);
        if (!string.IsNullOrEmpty(sendEmailError))
        {
            logger.LogError("Failed to send reset password email for user {UserId}: {Error}", user.Id, sendEmailError);
            return sendEmailError;
        }

        return string.Empty;
    }

    public async Task<string> SendResetPasswordEmail(User user, string token)
    {
        var emailTemplate = configuration.GetSection(ConfigKeys.Security.Email.ResetPasswordNotification).Get<EmailTemplateDto>();
        if (string.IsNullOrWhiteSpace(user.Email)
            || emailTemplate == null
            || string.IsNullOrWhiteSpace(emailTemplate.Path)
            || string.IsNullOrWhiteSpace(emailTemplate.Url))
        {
            return AuthControllerMsg.ForgotPassword.SendEmailFailed;
        }

        var urlencodedToken = HttpUtility.UrlEncode(token);
        var dataBinding = new Dictionary<string, string>()
            {
                { "{{UserName}}", user.UserName! },
                { "{{Url}}", $"{emailTemplate.Url}?token={urlencodedToken}" },
            };

        var request = new SendEmailReqDto
        {
            ToEmails = [user.Email],
            Subject = string.IsNullOrEmpty(emailTemplate.Subject) ? "Reset your password." : emailTemplate.Subject,
            TemplatePath = emailTemplate.Path,
            DataBinding = dataBinding
        };

        return await emailService.SendEmail(request);
    }
}