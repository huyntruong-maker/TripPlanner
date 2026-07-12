using System.ComponentModel.DataAnnotations;

namespace WebApi.Models.Requests.Auth
{
    public class ForgotPasswordReq
    {
        [Required] public required string Email { get; set; }
    }
}
