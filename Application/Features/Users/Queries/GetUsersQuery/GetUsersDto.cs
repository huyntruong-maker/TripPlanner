using Application.Dtos.Base;

namespace Application.Features.Users.Queries.GetUsersQuery;

public class GetUsersDto
{
    public Guid Id { get; set; }

    public required string UserName { get; set; }

    public required string FirstName { get; set; }

    public string? LastName { get; set; }

    public required string Email { get; set; }

    public string? PhoneNumber { get; set; }

    public RoleDetailDto[] Roles { get; set; } = [];
}
