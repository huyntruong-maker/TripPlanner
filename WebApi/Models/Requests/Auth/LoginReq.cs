using System.ComponentModel.DataAnnotations;

namespace WebApi.Models.Requests.Auth;

public class LoginReq
{
    [MaxLength(100)] public string? Username { get; set; }

    [MaxLength(100)] public string? Password { get; set; }

    [Required] public bool RememberMe { get; set; }
}
