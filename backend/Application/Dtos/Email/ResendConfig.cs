namespace Application.Dtos.Email;

public class ResendConfig
{
    public required string ApiKey { get; set; }

    public required string From { get; set; }

    public required string BaseUrl { get; set; }
}
