namespace Application.Features.Auth.Commands.RefreshTokenCommand;

public class RefreshTokenDto
{
    public string? Token { get; set; }

    public string? RefreshToken { get; set; }

    public bool Success { get; set; }
}