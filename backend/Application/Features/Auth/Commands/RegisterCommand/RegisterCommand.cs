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
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Web;

namespace Application.Features.Auth.Commands.RegisterCommand;

public record RegisterCommand : ICommand<string>
{
    public string? Email { get; init; }
    public string? Password { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
}

public class RegisterCommandHandler(
    UserManager<User> userManager,
    IWriteUnitOfWork writeUnitOfWork,
    IEmailService emailService,
    IConfiguration configuration,
    ILogger<RegisterCommandHandler> logger) : IRequestHandler<RegisterCommand, string>
{
    private const int VerificationTokenExpiryHours = 24;

    public async Task<string> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var existingUser = await userManager.FindByEmailAsync(request.Email!);
        if (existingUser != null)
        {
            // Generic response — prevents email enumeration.
            return AuthControllerMsg.Register.EmailTaken;
        }

        var user = new User
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName!,
            LastName = request.LastName ?? string.Empty
        };

        var createResult = await userManager.CreateAsync(user, request.Password!);
        if (!createResult.Succeeded)
        {
            logger.LogWarning("User registration failed. Errors: {Errors}",
                string.Join(", ", createResult.Errors.Select(e => e.Code)));
            return AuthControllerMsg.Register.RegistrationFailed;
        }

        var token = CommonHelper.GenerateBase64GuidToken();
        var verificationToken = new EmailVerificationToken
        {
            UserId = user.Id,
            Token = token,
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(VerificationTokenExpiryHours)
        };

        var tokenRepo = writeUnitOfWork.GetRepository<EmailVerificationToken>();
        await tokenRepo.Add(verificationToken);
        await writeUnitOfWork.SaveChanges();

        var sendError = await SendVerificationEmail(user, token);
        if (!string.IsNullOrEmpty(sendError))
        {
            logger.LogWarning("Email verification send failed for user {UserId}: {Error}", user.Id, sendError);
            // Registration succeeded; email failure is non-blocking (user can request resend later).
        }

        return string.Empty;
    }

    private async Task<string> SendVerificationEmail(User user, string token)
    {
        var emailTemplate = configuration
            .GetSection(ConfigKeys.Security.Email.EmailVerificationNotification)
            .Get<EmailTemplateDto>();

        if (string.IsNullOrEmpty(user.Email)
            || emailTemplate == null
            || string.IsNullOrEmpty(emailTemplate.Url))
        {
            return "EmailVerificationNotification template missing or user email absent.";
        }

        var urlEncodedToken = HttpUtility.UrlEncode(token);
        var verifyUrl = $"{emailTemplate.Url}?token={urlEncodedToken}";

        var sendRequest = new SendEmailReqDto
        {
            ToEmails = [user.Email],
            Subject = string.IsNullOrEmpty(emailTemplate.Subject)
                ? EmailTemplates.VerificationSubject
                : emailTemplate.Subject,
            Body = EmailTemplates.BuildVerificationEmail(user.FirstName, verifyUrl, VerificationTokenExpiryHours)
        };

        return await emailService.SendEmail(sendRequest);
    }
}
