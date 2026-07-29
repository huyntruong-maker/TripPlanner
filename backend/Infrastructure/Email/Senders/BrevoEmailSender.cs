using System.Net;
using System.Text;
using System.Text.Json;
using Application.Dtos.Email;
using Application.Interfaces.Email;
using Application.Interfaces.Restful;
using Domain.Constants;
using Domain.Messages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Email.Senders;

/// <summary>
/// Delivers email through Brevo's transactional HTTPS API. Because it is an ordinary HTTPS
/// request on port 443 it is unaffected by hosts that block outbound SMTP ports, and unlike
/// providers that require a verified domain, Brevo accepts a single OTP-verified sender
/// address — so it works without owning a domain.
/// </summary>
public class BrevoEmailSender(
    IRestfulService restfulService,
    ILogger<BrevoEmailSender> logger,
    IConfiguration configuration) : IEmailSender
{
    private const string DefaultBaseUrl = "https://api.brevo.com";
    private const string SendEmailPath = "/v3/smtp/email";
    private const string ApiKeyHeaderName = "api-key";

    public async Task<string> Send(SendEmailReqDto request, CancellationToken cancellationToken = default)
    {
        var (brevoConfig, isValid) = LoadBrevoConfig();
        if (brevoConfig == null || !isValid) return EmailControllerMsg.Create.ConfigurationLoadFailed;

        try
        {
            var (statusCode, body) = await restfulService.Post(
                $"{brevoConfig.BaseUrl}{SendEmailPath}",
                BuildContent(request, brevoConfig),
                new Dictionary<string, string> { [ApiKeyHeaderName] = brevoConfig.ApiKey });

            if (statusCode is not (HttpStatusCode.OK or HttpStatusCode.Created or HttpStatusCode.Accepted))
            {
                logger.LogError("Send email failed. Brevo API returned {Status}: {Body}", statusCode, body);
                return EmailControllerMsg.Create.Exception;
            }

            return string.Empty;
        }
        catch (Exception ex)
        {
            logger.LogError("Send email failed. Exception: {ex}", ex);

            return EmailControllerMsg.Create.Exception;
        }
    }

    private static StringContent BuildContent(SendEmailReqDto request, BrevoConfig brevoConfig)
    {
        var sender = new Dictionary<string, string> { ["email"] = brevoConfig.SenderEmail };
        if (!string.IsNullOrWhiteSpace(brevoConfig.SenderName)) sender["name"] = brevoConfig.SenderName;

        var payload = new Dictionary<string, object>
        {
            ["sender"] = sender,
            ["to"] = ToRecipients(request.ToEmails),
            ["subject"] = request.Subject,
            ["htmlContent"] = request.Body
        };

        if (request.CcEmails?.Count > 0) payload["cc"] = ToRecipients(request.CcEmails);
        if (request.BccEmails?.Count > 0) payload["bcc"] = ToRecipients(request.BccEmails);

        return new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
    }

    private static List<Dictionary<string, string>> ToRecipients(List<string> emails) =>
        emails.Select(email => new Dictionary<string, string> { ["email"] = email }).ToList();

    private (BrevoConfig?, bool) LoadBrevoConfig()
    {
        var apiKey = configuration.GetSection(ConfigKeys.Brevo.ApiKey).Value;
        var senderEmail = configuration.GetSection(ConfigKeys.Brevo.SenderEmail).Value;
        var senderName = configuration.GetSection(ConfigKeys.Brevo.SenderName).Value;
        var baseUrl = configuration.GetSection(ConfigKeys.Brevo.BaseUrl).Value;

        if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(senderEmail))
            return (null, false);

        var result = new BrevoConfig
        {
            ApiKey = apiKey,
            SenderEmail = senderEmail,
            SenderName = senderName,
            BaseUrl = string.IsNullOrWhiteSpace(baseUrl) ? DefaultBaseUrl : baseUrl
        };

        return (result, true);
    }
}
