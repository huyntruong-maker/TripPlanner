namespace Application.Dtos.Base;

public class CurrentUserContextDto
{
    public Guid UserId { get; set; }

    public Guid[] RoleIds { get; set; } = [];

    public int[] RolesLevel { get; set; } = [];
}