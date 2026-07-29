using System.Net;

namespace Application.Common.Email;

// Inline styles only (no <style> blocks): many email clients strip them, breaking rendering.
public static class EmailTemplates
{
    public const string VerificationSubject = "Verify your email address";
    public const string ResetPasswordSubject = "Reset your password";
    public const string ResetPasswordSuccessSubject = "Your password has been reset";
    public const string ChangePasswordSubject = "Your password was changed";

    private const string BrandColor = "#00236f";
    private const string PageBackground = "#f8f9ff";
    private const string CardBorder = "#e5eeff";
    private const string BodyTextColor = "#0b1c30";
    private const string MutedTextColor = "#444651";

    private const string HeadingStyle =
        "margin:0 0 16px;font-size:22px;line-height:28px;font-weight:700;color:" + BodyTextColor + ";";

    private const string ParagraphStyle =
        "margin:0 0 16px;font-size:15px;line-height:24px;color:" + BodyTextColor + ";";

    public static string BuildVerificationEmail(string firstName, string verifyUrl, int expiryHours)
    {
        var body = $"""
            <h1 style="{HeadingStyle}">Verify your email address</h1>
            <p style="{ParagraphStyle}">Hello {Encode(firstName)},</p>
            <p style="{ParagraphStyle}">
                Thanks for creating a TripPlanner account. Your account won't be usable until you
                verify this email address.
            </p>
            {BuildButton("Verify email", verifyUrl)}
            {BuildFallbackLink(verifyUrl)}
            <p style="{ParagraphStyle}">This link will expire in <strong>{expiryHours} hours</strong>.</p>
            """;

        return BuildLayout(
            "Verify your email address",
            body,
            "You received this because an account was created with this email. If this wasn't you, ignore this message.");
    }

    public static string BuildResetPasswordEmail(string userName, string resetUrl, int expiryHours)
    {
        var expiryLabel = expiryHours == 1 ? "1 hour" : $"{expiryHours} hours";

        var body = $"""
            <h1 style="{HeadingStyle}">Reset your password</h1>
            <p style="{ParagraphStyle}">Hello {Encode(userName)},</p>
            <p style="{ParagraphStyle}">
                We received a request to reset the password for your TripPlanner account. Click the
                button below to choose a new one.
            </p>
            {BuildButton("Reset password", resetUrl)}
            {BuildFallbackLink(resetUrl)}
            <p style="{ParagraphStyle}">This link will expire in <strong>{expiryLabel}</strong>.</p>
            """;

        return BuildLayout(
            "Reset your password",
            body,
            "You received this because a password reset was requested for your account. If this wasn't you, ignore this message and your password will stay the same.");
    }

    public static string BuildChangePasswordNotificationEmail(string userName)
    {
        var body = $"""
            <h1 style="{HeadingStyle}">Your password was changed</h1>
            <p style="{ParagraphStyle}">Hello {Encode(userName)},</p>
            <p style="{ParagraphStyle}">
                The password for your TripPlanner account was just changed. Use your new password the
                next time you log in.
            </p>
            """;

        return BuildLayout(
            "Your password was changed",
            body,
            "You received this because your account's password changed. If you didn't make this change, reset your password immediately.");
    }

    public static string BuildResetPasswordSuccessEmail(string userName)
    {
        var body = $"""
            <h1 style="{HeadingStyle}">Your password has been reset</h1>
            <p style="{ParagraphStyle}">Hello {Encode(userName)},</p>
            <p style="{ParagraphStyle}">
                Your TripPlanner password was successfully reset. You can now log in with your new
                password.
            </p>
            """;

        return BuildLayout(
            "Your password has been reset",
            body,
            "You received this because your account's password was reset. If you didn't make this change, contact support immediately.");
    }

    private static string BuildLayout(string title, string bodyHtml, string footerText) => $"""
        <!DOCTYPE html>
        <html lang="en">
        <head>
        <meta charset="UTF-8" />
        <meta name="viewport" content="width=device-width, initial-scale=1.0" />
        <title>{title}</title>
        </head>
        <body style="margin:0;padding:0;background-color:{PageBackground};font-family:'Segoe UI',Helvetica,Arial,sans-serif;">
        <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="background-color:{PageBackground};padding:32px 16px;">
        <tr>
        <td align="center">
        <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="max-width:560px;background-color:#ffffff;border-radius:12px;overflow:hidden;">
        <tr>
        <td style="background-color:{BrandColor};padding:24px 32px;">
        <span style="font-size:20px;font-weight:700;color:#ffffff;letter-spacing:-0.01em;">TripPlanner</span>
        </td>
        </tr>
        <tr>
        <td style="padding:32px;">
        {bodyHtml}
        </td>
        </tr>
        <tr>
        <td style="padding:20px 32px;background-color:{PageBackground};border-top:1px solid {CardBorder};">
        <p style="margin:0;font-size:12px;line-height:18px;color:{MutedTextColor};">{footerText}</p>
        </td>
        </tr>
        </table>
        </td>
        </tr>
        </table>
        </body>
        </html>
        """;

    private static string BuildButton(string label, string url) => $"""
        <table role="presentation" cellpadding="0" cellspacing="0" style="margin:8px 0 20px;">
        <tr>
        <td style="border-radius:8px;background-color:{BrandColor};">
        <a href="{Encode(url)}" style="display:inline-block;padding:12px 28px;font-size:16px;font-weight:600;color:#ffffff;text-decoration:none;border-radius:8px;">{Encode(label)}</a>
        </td>
        </tr>
        </table>
        """;

    private static string BuildFallbackLink(string url) => $"""
        <p style="margin:0 0 20px;font-size:13px;line-height:20px;color:{MutedTextColor};">
        If the button doesn't work, copy this link into your browser:<br />
        <a href="{Encode(url)}" style="color:{BrandColor};word-break:break-all;">{Encode(url)}</a>
        </p>
        """;

    private static string Encode(string value) => WebUtility.HtmlEncode(value);
}
