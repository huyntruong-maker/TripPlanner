namespace Domain.Constants;

public static class UserConstants
{
    public static readonly Guid AdminId = new("bb9f6603-1c9f-4933-9f66-031c9fb933a5");

    public struct Password
    {
        public const string RegexPattern = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$";
    }
}