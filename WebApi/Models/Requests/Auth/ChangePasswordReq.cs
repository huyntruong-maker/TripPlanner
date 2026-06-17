using System.ComponentModel.DataAnnotations;

namespace WebApi.Models.Requests.Auth;

public class ChangePasswordReq
{
    [MaxLength(100)] public required string OldPassword { get; set; }

    [MaxLength(100)] public required string NewPassword { get; set; }

    [MaxLength(100)] public required string ConfirmPassword { get; set; }

    public required Guid DeviceUuid { get; set; }
}