using Application.Common.Email;
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
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Application.Features.Auth.Commands.ResetPasswordCommand
{
    public record ResetPasswordCommand : ICommand<string>
    {
        public required string Token { get; set; }

        public required string NewPassword { get; set; }
    }

    public class ResetPasswordHandler(
        IWriteUnitOfWork writeUnitOfWork,
        IConfiguration configuration,
        IEmailService emailService,
        ILogger<ResetPasswordHandler> logger) : IRequestHandler<ResetPasswordCommand, string>
    {
        public async Task<string> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
        {
            var userTokenRepo = writeUnitOfWork.GetRepository<UserToken>();
            var userRepo = writeUnitOfWork.GetRepository<User>();

            var user = (await userRepo.QueryCondition(u => u.ResetPasswordToken == request.Token)).FirstOrDefault();
            if (user == null
                || user.ResetPasswordExpiration < DateTimeHelper.GetDtOffset())
            {
                return AuthControllerMsg.ResetPassword.ValidateTokenFailed;
            }

            user.PasswordHash = request.NewPassword.HashPassword();
            user.ResetPasswordToken = string.Empty;
            user.ResetPasswordExpiration = null;
            await userRepo.Update(user);

            var userSessions = (await userTokenRepo.QueryCondition(x => x.UserId == user.Id)).ToList();
            await userTokenRepo.Delete(userSessions);

            await writeUnitOfWork.SaveChanges();

            var sendEmailError = await SendResetPasswordSuccessEmail(user);
            if (!string.IsNullOrEmpty(sendEmailError))
            {
                logger.LogWarning("Send notification reset password failed: {sendEmailError}", sendEmailError);
            }

            return string.Empty;
        }

        public async Task<string> SendResetPasswordSuccessEmail(User user)
        {
            if (string.IsNullOrEmpty(user.Email))
            {
                return AuthControllerMsg.ResetPassword.SendEmailFailed;
            }

            var emailTemplate = configuration.GetSection(ConfigKeys.Security.Email.ResetPasswordSuccessNotification).Get<EmailTemplateDto>();

            var request = new SendEmailReqDto
            {
                ToEmails = [user.Email],
                Subject = string.IsNullOrEmpty(emailTemplate?.Subject) ? EmailTemplates.ResetPasswordSuccessSubject : emailTemplate.Subject,
                Body = EmailTemplates.BuildResetPasswordSuccessEmail(user.UserName!)
            };

            return await emailService.SendEmail(request);
        }
    }
}
