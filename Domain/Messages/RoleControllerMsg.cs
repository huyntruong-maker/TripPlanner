namespace Domain.Messages;

public static class RoleControllerMsg
{
    public struct Get
    {
        public const string NotFound = "Role.Get.Notfound";
        public const string Exception = "Role.Get.Exception";
    }
}