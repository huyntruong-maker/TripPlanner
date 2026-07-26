using Application.Dtos.Email;
using Application.Interfaces.Email;
using Domain.Constants;
using Domain.Helpers;
using Domain.Messages;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Email;

/// <summary>
/// Validates and batches outgoing mail, then delegates delivery to the configured
/// <see cref="IEmailSender"/> transport. Adding a provider means adding an
/// <see cref="IEmailSender"/> implementation — this class does not change.
/// </summary>
public class EmailService(
    IEmailSender emailSender,
    ILogger<EmailService> logger) : IEmailService
{
    public async Task<string> SendEmail(SendEmailReqDto request)
    {
        if (request.ToEmails.Count < 1 || request.ToEmails.Any(email => !email.IsValidEmail()))
        {
            logger.LogError("Send email failed. ValidateToEmailFailed: {request}", request);
            return EmailControllerMsg.Create.ValidateToEmailFailed;
        }

        if (request.CcEmails != null && request.CcEmails.Any(email => !email.IsValidEmail()))
        {
            logger.LogError("Send email failed. ValidateCcEmailFailed: {request}", request);
            return EmailControllerMsg.Create.ValidateCcEmailFailed;
        }

        if (request.BccEmails != null && request.BccEmails.Any(email => !email.IsValidEmail()))
        {
            logger.LogError("Send email failed. ValidateBccEmailFailed: {request}", request);
            return EmailControllerMsg.Create.ValidateBccEmailFailed;
        }

        var i = 0;
        var toEmailsChunk = request.ToEmails
            .GroupBy(_ => i++ / GlobalConstants.MaxBatchSize.MaxBatch100)
            .Select(s => s.ToList())
            .ToArray();

        foreach (var toEmails in toEmailsChunk)
        {
            var error = await emailSender.Send(BuildBatchRequest(request, toEmails));
            if (!string.IsNullOrEmpty(error)) return error;

            logger.LogInformation("Send email to {emails} successfully", string.Join(", ", toEmails));
        }

        return string.Empty;
    }

    private static SendEmailReqDto BuildBatchRequest(SendEmailReqDto request, List<string> toEmails) => new()
    {
        ToEmails = toEmails,
        CcEmails = request.CcEmails,
        BccEmails = request.BccEmails,
        Subject = request.Subject,
        Body = request.Body
    };
}
