namespace Domain.Messages
{
    public static class TenantControllerMsg
    {
        public struct Create
        {
            public const string NameAlreadyUsed = "Tenant.Create.NameAlreadyUsed";
            public const string Exception = "Tenant.Create.Exception";
            public const string UserEmailAlreadyUsed = "Tenant.Create.UserEmailAlreadyUsed";
            public const string UserPasswordRequired = "Tenant.Create.UserPasswordRequired";
            public const string UserPasswordNotStrongEnough = "Tenant.Create.UserPasswordNotStrongEnough";
            public const string UserPasswordsDoNotMatch = "Tenant.Create.UserPasswordsDoNotMatch";
            public const string InValidUserEmail = "Tenant.Create.InValidUserEmail";
            public const string UserNameRequired = "Tenant.Create.UserNameRequired";
            public const string UserNameAlreadyUsed = "Tenant.Create.UserNameAlreadyUsed";
        }

        public struct Update
        {
            public const string TenantNotFound = "Tenant.Update.NotFound";
            public const string NameUsed = "Tenant.Update.NameAlreadyUsed";
            public const string IsDefaultTenant = "Tenant.Update.IsDefaultTenant";
            public const string Exception = "Tenant.Update.Exception";
        }

        public struct Deactivate
        {
            public const string TenantNotFound = "Tenant.Deactivate.NotFound";
            public const string IsDefaultTenant = "Tenant.Deactivate.IsDefaultTenant";
            public const string Exception = "Tenant.Deactivate.Exception";
        }

        public struct Get
        {
            public const string TenantNotFound = "Tenant.Get.NotFound";
            public const string Exception = "Tenant.Get.Exception";
        }
    }
}
