namespace Domain.Messages;

public class ShareControllerMsg
{
    public struct CurrentUserContext
    {
        public const string InvalidUser = "Share.UserContext.InvalidUser";
        public const string InvalidRole = "Share.UserContext.InvalidRole";
    }
}
