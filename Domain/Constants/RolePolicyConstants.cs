namespace Domain.Constants;

public static class RolePolicyConstants
{
    public const string ClaimType = "RolePolicy";

    public static class SuperAdmin
    {
        public const string Name = "SuperAdmin";
        public const string DisplayName = "Super Admin";
        public static readonly Guid Id = UserConstants.Role.SuperAdmin;

        public static readonly string[] AllowedPermissions =
        [
            PermissionConstants.AssignRoles.ManageAssignRoles,
            PermissionConstants.Users.ViewUsers,
            PermissionConstants.Users.CreateUser,
            PermissionConstants.Users.UpdateUser,
            PermissionConstants.Users.DeactivateUser,
            PermissionConstants.Users.ResetPassUser
        ];
    }

    public struct SystemAdmin
    {
        public const string Name = "SystemAdmin";
        public const string DisplayName = "System Admin";
        public static readonly Guid Id = UserConstants.Role.SystemAdmin;

        public static readonly string[] AllowedPermissions =
        [
            PermissionConstants.Users.ViewUsers,
            PermissionConstants.Users.CreateUser,
            PermissionConstants.Users.UpdateUser,
            PermissionConstants.Users.DeactivateUser,
            PermissionConstants.Users.ResetPassUser,
        ];
    }
}