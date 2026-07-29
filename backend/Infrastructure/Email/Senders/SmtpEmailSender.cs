using System.Net;
using System.Net.Mail;
using Application.Dtos.Email;
using Application.Interfaces.Email;
using Domain.Constants;
using Domain.Messages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Email.Senders;

/// <summary>
/// Delivers email over raw SMTP. Works for local development, but note that some hosts
/// (Railway on Free/Trial/Hobby, for one) block outbound SMTP ports 25/465/587/2525, in which
/// case <see cref="SmtpClient"/> times out while opening the socket and no mail is sent.
/// Switch <c>Email:Provider</c> to an HTTPS-based sender in that environment.
/// </summary>
public class SmtpEmailSender(
    ILogger<SmtpEmailSender> logger,
    IConfiguration configuration) : IEmailSender
{
    public async Task<string> Send(SendEmailReqDto request, CancellationToken cancellationToken = default)
    {
        var (smtpConfig, isValid) = LoadSmtpConfig();
        if (smtpConfig == null || !isValid) return EmailControllerMsg.Create.ConfigurationLoadFailed;

        try
        {
            using var mailMessage = BuildMailMessage(request, smtpConfig);
            using var smtpClient = new SmtpClient(smtpConfig.Host)
            {
                Port = smtpConfig.Port,
                Credentials = new NetworkCredential(smtpConfig.Username, smtpConfig.Password),
                EnableSsl = smtpConfig.EnableSsl
            };

            await smtpClient.SendMailAsync(mailMessage, cancellationToken);

            return string.Empty;
        }
        catch (Exception ex)
        {
            logger.LogError("Send email failed. Exception: {ex}", ex);

            return EmailControllerMsg.Create.Exception;
        }
    }

    private static MailMessage BuildMailMessage(SendEmailReqDto request, SmtpConfig smtpConfig)
    {
        var mailMessage = new MailMessage
        {
            From = new MailAddress(smtpConfig.From),
            Subject = request.Subject,
            Body = request.Body,
            IsBodyHtml = true
        };

        request.ToEmails.ForEach(mailMessage.To.Add);

        if (request.CcEmails?.Count > 0) request.CcEmails.ForEach(mailMessage.CC.Add);

        if (request.BccEmails?.Count > 0) request.BccEmails.ForEach(mailMessage.Bcc.Add);

        return mailMessage;
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
