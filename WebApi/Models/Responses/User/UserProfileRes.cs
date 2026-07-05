namespace WebApi.Models.Responses.User;

public class UserProfileRes
{
    public Guid Id { get; set; }

    public required string UserName { get; set; }

    public required string FirstName { get; set; }

    public string? LastName { get; set; }

    public required string Email { get; set; }

    public string? PhoneNumber { get; set; }
}
