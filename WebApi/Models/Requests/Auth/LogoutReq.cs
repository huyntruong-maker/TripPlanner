using System.ComponentModel.DataAnnotations;

namespace WebApi.Models.Requests.Auth;

public class LogoutReq
{
    public string? Token { get; set; }

    public string? RefreshToken { get; set; }

    [Required] public required Guid DeviceUuid { get; set; }
}