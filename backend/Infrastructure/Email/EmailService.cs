using System.Net;
using System.Text;
using System.Text.Json;
using Application.Dtos.Email;
using Application.Interfaces.Email;
using Application.Interfaces.Restful;
using Domain.Constants;
using Domain.Helpers;
using Domain.Messages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Email;

/// <summary>
/// Sends transactional email through Resend's HTTPS API (https://api.resend.com) rather than
/// raw SMTP. Railway blocks outbound SMTP (ports 465/587/2525) on the Free/Trial/Hobby plans,
/// which made any SmtpClient-based sender (System.Net.Mail or MailKit) time out on connect
/// regardless of library — the connection never reaches Gmail. An HTTPS API call on port 443
/// is unaffected by that restriction.
/// </summary>
public class EmailService(
    IRestfulService restfulService,
    ILogger<EmailService> logger,
    IConfiguration configuration) : IEmailService
{
    private const string DefaultBaseUrl = "https://api.resend.com";

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

        var (resendConfig, isValid) = LoadResendConfig();
        if (resendConfig == null || !isValid) return EmailControllerMsg.Create.ConfigurationLoadFailed;

        try
        {
            var i = 0;
            var toEmailsChunk = request.ToEmails
                .GroupBy(_ => i++ / GlobalConstants.MaxBatchSize.MaxBatch100)
                .Select(s => s.ToList())
                .ToArray();

            foreach (var toEmails in toEmailsChunk)
            {
                var (statusCode, body) = await SendViaResendAsync(request, resendConfig, toEmails);

                if (statusCode is not (HttpStatusCode.OK or HttpStatusCode.Created))
                {
                    logger.LogError("Send email failed. Resend API returned {Status}: {Body}", statusCode, body);
                    return EmailControllerMsg.Create.Exception;
                }

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

    private Task<(HttpStatusCode, string)> SendViaResendAsync(
        SendEmailReqDto request, ResendConfig resendConfig, List<string> toEmails)
    {
        var payload = new Dictionary<string, object>
        {
            ["from"] = resendConfig.From,
            ["to"] = toEmails,
            ["subject"] = request.Subject,
            ["html"] = request.Body
        };

        if (request.CcEmails?.Count > 0) payload["cc"] = request.CcEmails;
        if (request.BccEmails?.Count > 0) payload["bcc"] = request.BccEmails;

        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        var headers = new Dictionary<string, string> { ["Authorization"] = $"Bearer {resendConfig.ApiKey}" };

        return restfulService.Post($"{resendConfig.BaseUrl}/emails", content, headers);
    }

    private (ResendConfig?, bool) LoadResendConfig()
    {
        var apiKey = configuration.GetSection(ConfigKeys.Resend.ApiKey).Value;
        var from = configuration.GetSection(ConfigKeys.Resend.From).Value;
        var baseUrl = configuration.GetSection(ConfigKeys.Resend.BaseUrl).Value;

        if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(from))
            return (null, false);

        var result = new ResendConfig
        {
            ApiKey = apiKey,
            From = from,
            BaseUrl = string.IsNullOrWhiteSpace(baseUrl) ? DefaultBaseUrl : baseUrl
        };

        return (result, true);
    }
}
