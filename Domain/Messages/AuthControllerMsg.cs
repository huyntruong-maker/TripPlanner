namespace Domain.Messages;

public static class AuthControllerMsg
{
    public struct Login
    {
        public const string InvalidCredential = "Auth.Login.InvalidCredential";
        public const string WillBeLockedOut = "Auth.Login.WillBeLockedOut";
        public const string LockedOut = "Auth.Login.LockedOut";
        public const string InActive = "Auth.Login.InActive";
        public const string EmptyField = "Auth.Login.EmptyField";
        public const string Exception = "Auth.Login.Exception";
    }

    public struct RefreshToken
    {
        public const string RequiredToken = "Auth.RefreshToken.RequiredToken";
        public const string Failed = "Auth.RefreshToken.Failed";
        public const string Exception = "Auth.RefreshToken.Exception";
    }

    public struct Logout
    {
        public const string RequiredToken = "Auth.Logout.RequiredToken";
    }

    public struct ChangePassword
    {
        public const string EmptyUserName = "Auth.ChangePassword.EmptyUserName";
        public const string UserNotFound = "Auth.ChangePassword.UserNotFound";
        public const string InvalidOldPassword = "Auth.ChangePassword.InvalidOldPassword";
        public const string OldPasswordRequired = "Auth.ChangePassword.OldPasswordRequired";
        public const string NewPasswordRequired = "Auth.ChangePassword.NewPasswordRequired";
        public const string NewPasswordNotStrongEnough = "Auth.ChangePassword.NewPasswordNotStrongEnough";
        public const string PasswordsDoNotMatch = "Auth.ChangePassword.PasswordsDoNotMatch";
        public const string NewPassSameAsOldPass = "Auth.ChangePassword.NewPassSameAsOldPass";
        public const string Exception = "Auth.ChangePassword.Exception";
        public const string Failed = "Auth.ChangePassword.Failed";
    }

    public struct ForgotPassword
    {
        public const string EmailNotExist = "Auth.ForgotPassword.EmailNotExist";
        public const string SendEmailFailed = "Auth.ForgotPassword.SendEmailFailed";
        public const string Exception = "Auth.ForgotPassword.Exception";
    }

    public struct ResetPassword
    {
        public const string ValidateTokenFailed = "Auth.ResetPassword.ValidateTokenFailed";
        public const string Exception = "Auth.ResetPassword.Exception";
        public const string NewPasswordRequired = "Auth.ResetPassword.NewPasswordRequired";
        public const string NewPasswordNotStrongEnough = "Auth.ResetPassword.NewPasswordNotStrongEnough";
        public const string PasswordsDoNotMatch = "Auth.ResetPassword.PasswordsDoNotMatch";
        public const string SendEmailFailed = "Auth.ResetPassword.SendEmailFailed";
    }
}