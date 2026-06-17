namespace Domain.Constants;

public static class PermissionConstants
{
    public static readonly string[] All =
    [
        AssignRoles.ManageAssignRoles,
        Users.ViewUsers,
        Users.CreateUser,
        Users.UpdateUser,
        Users.DeactivateUser,
        Users.ResetPassUser
    ];

    public struct AssignRoles
    {
        public const string ManageAssignRoles = "ManageAssignRoles";
    }

    public struct Users
    {
        public const string ViewUsers = "ViewUsers";
        public const string CreateUser = "CreateUser";
        public const string UpdateUser = "UpdateUser";
        public const string DeactivateUser = "DeactivateUser";
        public const string ResetPassUser = "ResetPassUser";
    }
}