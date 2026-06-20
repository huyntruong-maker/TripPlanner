using System.ComponentModel.DataAnnotations;

namespace WebApi.Models.Requests.Auth;

public class RegisterReq
{
    [MaxLength(256)]
    public string? Email { get; set; }

    [MaxLength(256)]
    public string? Password { get; set; }

    [MaxLength(100)]
    public string? FirstName { get; set; }

    [MaxLength(100)]
    public string? LastName { get; set; }
}
