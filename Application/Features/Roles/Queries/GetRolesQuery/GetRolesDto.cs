namespace Application.Features.Roles.Queries.GetRolesQuery;

public class GetRolesDto
{
    public Guid Id { get; set; }

    public string DisplayName { get; set; } = string.Empty;
}