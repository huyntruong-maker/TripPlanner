namespace WebApi.Models.Requests.Auth;

public class LogoutReq
{
    public string? Token { get; set; }

    public string? RefreshToken { get; set; }
}
