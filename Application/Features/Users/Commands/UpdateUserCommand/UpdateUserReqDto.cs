namespace Application.Features.Users.Commands.UpdateUserCommand;

public class UpdateUserReqDto
{
    public required string UserName { get; set; }

    public required string FirstName { get; set; }

    public string? LastName { get; set; }

    public required string Email { get; set; }

    public string? PhoneNumber { get; set; }

    public required Guid[] RoleIds { get; set; }
}