namespace WebApi.Models.Requests.Auth;

public class RefreshTokenReq
{
    public string? Token { get; set; }

    public string? RefreshToken { get; set; }
}
