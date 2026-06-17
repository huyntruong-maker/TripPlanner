using Application.Dtos.Email;
using Application.Interfaces.Email;
using Domain.Constants;
using Domain.Helpers;
using Domain.Messages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Mail;

namespace Infrastructure.Email;

public class EmailService(
    ILogger<EmailService> logger,
    IConfiguration configuration) : IEmailService
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

        var (smtpConfig, isValid) = LoadSmtpConfig();
        if (smtpConfig == null || !isValid) return EmailControllerMsg.Create.ConfigurationLoadFailed;

        var mailMessage = new MailMessage
        {
            From = new MailAddress(smtpConfig.From),
            Subject = request.Subject,
            Body = BindingDataToTemplate(request.TemplatePath, request.DataBinding),
            IsBodyHtml = true
        };

        if (request.CcEmails?.Count > 0) request.CcEmails.ForEach(mailMessage.CC.Add);

        if (request.BccEmails?.Count > 0) request.BccEmails.ForEach(mailMessage.Bcc.Add);

        try
        {
            var smtpClient = new SmtpClient(smtpConfig.Host)
            {
                Port = smtpConfig.Port,
                Credentials = new NetworkCredential(smtpConfig.Username, smtpConfig.Password),
                EnableSsl = smtpConfig.EnableSsl
            };

            var i = 0;
            var toEmailsChunk = request.ToEmails
                .GroupBy(_ => i++ / GlobalConstants.MaxBatchSize.MaxBatch100)
                .Select(s => s.ToList())
                .ToArray();

            foreach (var toEmails in toEmailsChunk)
            {
                toEmails.ForEach(mailMessage.To.Add);

                await smtpClient.SendMailAsync(mailMessage);

                logger.LogInformation("Send email to {emails} successfully", string.Join(", ", toEmails));
            }

            return string.Empty;
        }
        catch (Exception ex)
        {
            logger.LogError("Send email failed. Exception: {ex}", ex);

            return EmailControllerMsg.Create.Exception;
        }
    }

    private string BindingDataToTemplate(string templatePath, Dictionary<string, string>? dataBinding)
    {
        var bodyContent = File.ReadAllText(templatePath);

        if (dataBinding != null)
        {
            foreach (var item in dataBinding)
            {
                bodyContent = bodyContent.Replace(item.Key, item.Value);
            }
        }

        return bodyContent;
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