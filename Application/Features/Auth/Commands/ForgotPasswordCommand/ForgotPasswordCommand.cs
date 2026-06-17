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
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using System.Web;
using Application.Interfaces.Cqrs;

namespace Application.Features.Auth.Commands.ForgotPasswordCommand
{
    public record ForgotPasswordCommand : ICommand<string>
    {
        public required string Email { get; set; }
    }

    public class ResetPasswordCommandHandler(UserManager<User> userManager,
        IWriteUnitOfWork writeUnitOfWork,
        IEmailService emailService,
        IAuthShareService authShareService,
        IConfiguration configuration) : IRequestHandler<ForgotPasswordCommand, string>
    {
        public async Task<string> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
        {
            var userRepo = writeUnitOfWork.GetRepository<User>();

            var user = await userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                return AuthControllerMsg.ForgotPassword.EmailNotExist;
            }

            var resetPasswordToken = CommonHelper.GenerateBase64GuidToken();
            var expirationTime = DateTimeHelper.GetDt().AddHours(authShareService.ResetPasswordExpirationHours);

            user.ResetPasswordToken = resetPasswordToken;
            user.ResetPasswordExpiration = expirationTime;
            await userRepo.Update(user);
            await writeUnitOfWork.SaveChanges();

            var sendEmailError = await SendResetPasswordEmail(user, resetPasswordToken);
            if (!string.IsNullOrEmpty(sendEmailError))
            {
                return sendEmailError;
            }

            return string.Empty;
        }

        public async Task<string> SendResetPasswordEmail(User user, string token)
        {
            var emailTemplate = configuration.GetSection(ConfigKeys.Security.Email.ResetPasswordNotification).Get<EmailTemplateDto>();
            if (string.IsNullOrEmpty(user.Email)
                || emailTemplate == null
                || string.IsNullOrEmpty(emailTemplate.Path))
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
}
