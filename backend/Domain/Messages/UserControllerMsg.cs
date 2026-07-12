namespace Domain.Messages;

public static class UserControllerMsg
{
    public struct GetProfile
    {
        public const string NotFound = "User.GetProfile.NotFound";
        public const string Exception = "User.GetProfile.Exception";
    }

    public struct ChangeProfile
    {
        public const string NotFound = "User.ChangeProfile.NotFound";
        public const string Exception = "User.ChangeProfile.Exception";
        public const string DuplicatedUserName = "User.ChangeProfile.DuplicatedUserName";
        public const string DuplicatedEmail = "User.ChangeProfile.DuplicatedEmail";
        public const string InvalidEmailFormat = "User.ChangeProfile.InvalidEmailFormat";
        public const string EmptyField = "User.ChangeProfile.EmptyField";
        public const string Failed = "User.ChangeProfile.Failed";
        public const string CannotChangeAdminProfile = "User.ChangeProfile.CannotChangeAdminProfile";
    }
}