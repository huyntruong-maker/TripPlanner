using System.ComponentModel.DataAnnotations;

namespace WebApi.Models.Requests.User;

public class CreateUserReq
{
    [MaxLength(100)]
    public required string UserName { get; set; }

    [MaxLength(100)]
    public required string Password { get; set; }

    [MaxLength(100)]
    public required string ConfirmPassword { get; set; }

    [MaxLength(100)]
    public required string FirstName { get; set; }

    [MaxLength(100)]
    public string? LastName { get; set; }

    [MaxLength(100)]
    public required string Email { get; set; }

    [MaxLength(20)]
    public string? PhoneNumber { get; set; }

    public required Guid[] RoleIds { get; set; }
}