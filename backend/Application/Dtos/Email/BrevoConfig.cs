namespace Application.Dtos.Email;

public class BrevoConfig
{
    public required string ApiKey { get; set; }

    public required string SenderEmail { get; set; }

    public string? SenderName { get; set; }

    public required string BaseUrl { get; set; }
}
