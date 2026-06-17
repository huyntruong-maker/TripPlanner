namespace Application.Dtos.Email;

public class SmtpConfig
{
    public required string Host { get; set; }

    public required int Port { get; set; }

    public required string Username { get; set; }

    public required string Password { get; set; }

    public bool EnableSsl { get; set; }

    public required string From { get; set; }
}