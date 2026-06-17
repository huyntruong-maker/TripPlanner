namespace Domain.Constants;

public static class UserConstants
{
    public static readonly Guid AdminId = new("bb9f6603-1c9f-4933-9f66-031c9fb933a5");
    public const string RolesLevelClaim = "RolesLevelClaim";
    public const string RolesClaim = "RolesClaim";

    public struct Password
    {
        public const string RegexPattern = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$";
    }

    public struct Role
    {
        public static readonly Guid SuperAdmin = new("0F34236C-5A49-44EE-8E61-2992B0308AB9");
        public static readonly Guid SystemAdmin = new("DE31CFFB-9AF8-41D0-B7D8-8FB1780F6560");
    }

    public struct RoleLevel
    {
        public const int SuperAdmin = 1024;
        public const int SystemAdmin = 512;
    }
}