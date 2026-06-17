namespace WebApi.Models.Responses.Auth;

public class LoginRes
{
    public string? Token { get; set; }

    public string? RefreshToken { get; set; }

    public int AccessFailedCount { get; set; }

    public DateTime? LockoutEnd { get; set; }
}