namespace Domain.Messages;

public class EmailControllerMsg
{
    public struct Create
    {
        public const string ValidateToEmailFailed = "Email.Create.ValidateToEmailFailed";
        public const string ValidateCcEmailFailed = "Email.Create.ValidateCcEmailFailed";
        public const string ValidateBccEmailFailed = "Email.Create.ValidateBccEmailFailed";
        public const string ConfigurationLoadFailed = "Email.Create.ConfigurationLoadFailed";
        public const string Exception = "Email.Create.Exception";
    }
}