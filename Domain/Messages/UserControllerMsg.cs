namespace Domain.Messages;

public static class UserControllerMsg
{
    public struct Get
    {
        public const string NotFound = "User.Get.NotFound";
        public const string Exception = "User.Get.Exception";
    }

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

    public struct Deactivate
    {
        public const string NotFound = "User.Deactivate.NotFound";
        public const string CannotDeactivateOwn = "User.Deactivate.CannotDeactivateOwn";
        public const string CannotDeactivateAdmin = "User.Deactivate.CannotDeactivateAdmin";
        public const string Exception = "User.Deactivate.Exception";
    }

    public struct Create
    {
        public const string UserNameAlreadyExisted = "User.Create.UserNameAlreadyExisted";
        public const string EmailAlreadyExisted = "User.Create.EmailAlreadyExisted";
        public const string InvalidEmailFormat = "User.Create.InvalidEmailFormat";
        public const string EmptyField = "User.Create.EmptyField";
        public const string PasswordNotStrongEnough = "User.Create.PasswordNotStrongEnough";
        public const string ConfirmPasswordNotMatch = "User.Create.ConfirmPasswordNotMatch";
        public const string RoleNotFound = "User.Create.RoleNotFound";
        public const string InvalidRolePermission = "User.Create.InvalidRolePermission";
        public const string Exception = "User.Create.Exception";
    }

    public struct Update
    {
        public const string NotFound = "User.Update.NotFound";
        public const string DuplicatedUserName = "User.Update.DuplicatedUserName";
        public const string DuplicatedEmail = "User.Update.DuplicatedEmail";
        public const string InvalidEmailFormat = "User.Update.InvalidEmailFormat";
        public const string EmptyField = "User.Update.EmptyField";
        public const string RoleNotFound = "User.Update.RoleNotFound";
        public const string InvalidRolePermission = "User.Update.InvalidRolePermission";
        public const string CannotUpdateAdminAccount = "User.Update.CannotUpdateAdminAccount";
        public const string Exception = "User.Update.Exception";
    }

    public struct ResetPassword
    {
        public const string NotFound = "User.ResetPassword.NotFound";
        public const string EmailNotExist = "User.ResetPassword.EmailNotExist";
        public const string SendEmailFailed = "User.ResetPassword.SendEmailFailed";
        public const string Exception = "User.ResetPassword.Exception";
    }
}