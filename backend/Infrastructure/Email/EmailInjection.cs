using Application.Interfaces.Email;
using Domain.Constants;
using Infrastructure.Email.Senders;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Email;

public static class EmailInjection
{
    public static void AddEmail(this IServiceCollection collection)
    {
        // Transport strategies (transient — stateless).
        collection.AddTransient<SmtpEmailSender>();
        collection.AddTransient<BrevoEmailSender>();

        // IEmailSender: chosen by Email:Provider ("Smtp" default — direct SMTP, fine locally;
        // "Brevo" — HTTPS API, needed where outbound SMTP ports are blocked).
        collection.AddScoped<IEmailSender>(sp =>
        {
            var configuration = sp.GetRequiredService<IConfiguration>();
            var logger = sp.GetRequiredService<ILogger<EmailService>>();
            var providerName = configuration[ConfigKeys.Email.Provider];

            return providerName?.Trim().ToLowerInvariant() switch
            {
                "brevo" => sp.GetRequiredService<BrevoEmailSender>(),
                "smtp" or null or "" => sp.GetRequiredService<SmtpEmailSender>(),
                _ => LogUnknownProviderAndFallback(logger, providerName!, sp.GetRequiredService<SmtpEmailSender>())
            };
        });

        collection.AddScoped<IEmailService, EmailService>();
    }

    /// <summary>Logs an unrecognized <c>Email:Provider</c> value and falls back to SMTP.</summary>
    private static IEmailSender LogUnknownProviderAndFallback(
        ILogger<EmailService> logger,
        string providerName,
        IEmailSender fallback)
    {
        logger.LogWarning(
            "[Email] Unknown Email:Provider value '{ProviderName}'; falling back to Smtp.",
            providerName);
        return fallback;
    }
}
