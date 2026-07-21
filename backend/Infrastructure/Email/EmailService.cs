using Application.Dtos.Email;
using Application.Interfaces.Email;
using Domain.Constants;
using Domain.Helpers;
using Domain.Messages;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace Infrastructure.Email;

public class EmailService(
    ILogger<EmailService> logger,
    IConfiguration configuration) : IEmailService
{
    private const int SmtpTimeoutMilliseconds = 15000;

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

        var (smtpConfig, isValid) = LoadSmtpConfig();
        if (smtpConfig == null || !isValid) return EmailControllerMsg.Create.ConfigurationLoadFailed;

        try
        {
            using var smtpClient = new SmtpClient
            {
                Timeout = SmtpTimeoutMilliseconds
            };

            var secureSocketOptions = ResolveSecureSocketOptions(smtpConfig);
            await smtpClient.ConnectAsync(smtpConfig.Host, smtpConfig.Port, secureSocketOptions);
            await smtpClient.AuthenticateAsync(smtpConfig.Username, smtpConfig.Password);

            var i = 0;
            var toEmailsChunk = request.ToEmails
                .GroupBy(_ => i++ / GlobalConstants.MaxBatchSize.MaxBatch100)
                .Select(s => s.ToList())
                .ToArray();

            foreach (var toEmails in toEmailsChunk)
            {
                var mimeMessage = BuildMimeMessage(request, smtpConfig, toEmails);

                await smtpClient.SendAsync(mimeMessage);

                logger.LogInformation("Send email to {emails} successfully", string.Join(", ", toEmails));
            }

            await smtpClient.DisconnectAsync(quit: true);

            return string.Empty;
        }
        catch (Exception ex)
        {
            logger.LogError("Send email failed. Exception: {ex}", ex);

            return EmailControllerMsg.Create.Exception;
        }
    }

    private static SecureSocketOptions ResolveSecureSocketOptions(SmtpConfig smtpConfig)
    {
        if (!smtpConfig.EnableSsl) return SecureSocketOptions.None;

        return smtpConfig.Port == GlobalConstants.SmtpPort.ImplicitTls
            ? SecureSocketOptions.SslOnConnect
            : SecureSocketOptions.StartTls;
    }

    private static MimeMessage BuildMimeMessage(SendEmailReqDto request, SmtpConfig smtpConfig, List<string> toEmails)
    {
        var mimeMessage = new MimeMessage
        {
            Subject = request.Subject
        };

        mimeMessage.From.Add(MailboxAddress.Parse(smtpConfig.From));
        toEmails.ForEach(email => mimeMessage.To.Add(MailboxAddress.Parse(email)));

        if (request.CcEmails?.Count > 0)
            request.CcEmails.ForEach(email => mimeMessage.Cc.Add(MailboxAddress.Parse(email)));

        if (request.BccEmails?.Count > 0)
            request.BccEmails.ForEach(email => mimeMessage.Bcc.Add(MailboxAddress.Parse(email)));

        mimeMessage.Body = new BodyBuilder { HtmlBody = request.Body }.ToMessageBody();

        return mimeMessage;
    }

    private (SmtpConfig?, bool) LoadSmtpConfig()
    {
        var smtpFromEmail = configuration.GetSection(ConfigKeys.Smtp.From).Value;
        var smtpUserName = configuration.GetSection(ConfigKeys.Smtp.Username).Value;
        var smtpPassword = configuration.GetSection(ConfigKeys.Smtp.Password).Value;
        var smtpHost = configuration.GetSection(ConfigKeys.Smtp.Host).Value;
        var smtpPort = configuration.GetSection(ConfigKeys.Smtp.Port).Get<int?>();
        var smtpEnableSsl = configuration.GetSection(ConfigKeys.Smtp.EnableSsl).Get<bool?>();

        if (string.IsNullOrWhiteSpace(smtpFromEmail)
            || string.IsNullOrWhiteSpace(smtpUserName)
            || string.IsNullOrWhiteSpace(smtpPassword)
            || string.IsNullOrWhiteSpace(smtpHost)
            || smtpPort == null)
            return (null, false);

        var result = new SmtpConfig
        {
            From = smtpFromEmail,
            Username = smtpUserName,
            Host = smtpHost,
            Port = smtpPort.Value,
            Password = smtpPassword,
            EnableSsl = smtpEnableSsl ?? true
        };

        return (result, true);
    }
}