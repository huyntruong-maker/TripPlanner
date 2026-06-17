using System.ComponentModel.DataAnnotations;

namespace WebApi.Models.Requests.Auth
{
    public class ResetPasswordReq
    {
        [Required] public required string Token { get; set; }

        [Required] public required string NewPassword { get; set; }

        [Required] public required string ConfirmPassword { get; set; }
    }
}
