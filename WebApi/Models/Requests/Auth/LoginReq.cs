using System.ComponentModel.DataAnnotations;

namespace WebApi.Models.Requests.Auth;

public class LoginReq
{
    [MaxLength(100)] public string? Username { get; set; }

    [MaxLength(100)] public string? Password { get; set; }

    [Required] public required Guid DeviceUuid { get; set; }

    [Required] public bool RememberMe { get; set; }

    [MaxLength(250)] public required string DeviceInfo { get; set; }

    [MaxLength(150)] public required string LocationInfo { get; set; }
}