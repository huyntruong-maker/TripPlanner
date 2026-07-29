namespace Domain.Constants;

public static class ConfigKeys
{
    public const string EnableSwagger = "EnableSwagger";
    public const string AutoMigration = "AutoMigration";

    public struct Databases
    {
        public const string ReadDatabase = "ConnectionStrings:ReadDatabase";
        public const string WriteDatabase = "ConnectionStrings:WriteDatabase";
        public const string ReadUniDatabase = "ConnectionStrings:ReadUniDatabase";
        public const string WriteUniDatabase = "ConnectionStrings:WriteUniDatabase";
        public const string Quartz = "ConnectionStrings:QuartzDatabase";
    }

    public struct Security
    {
        public struct Lockout
        {
            public const string MaxFailedAccessAttempts = "Security:Lockout:MaxFailedAccessAttempts";
            public const string DefaultLockoutMinutes = "Security:Lockout:DefaultLockoutMinutes";
        }

        public struct Jwt
        {
            public const string Secret = "Security:Jwt:Secret";
            public const string RefreshSecret = "Security:Jwt:RefreshSecret";
            public const string ExpirationMinutes = "Security:Jwt:ExpirationMinutes";
            public const string RefreshExpirationDays = "Security:Jwt:RefreshExpirationDays";
            public const string Issuer = "Security:Jwt:Issuer";
            public const string Audience = "Security:Jwt:Audience";
            public const string RefreshShortExpirationDays = "Security:Jwt:RefreshShortExpirationDays";
            public const string ResetPasswordExpirationHours = "Security:Jwt:ResetPasswordExpirationHours";
        }

        public struct Email
        {
            public const string ChangePasswordNotification = "Security:Email:ChangePasswordNotification";
            public const string ResetPasswordNotification = "Security:Email:ResetPasswordNotification";
            public const string ResetPasswordSuccessNotification = "Security:Email:ResetPasswordSuccessNotification";
            public const string EmailVerificationNotification = "Security:Email:EmailVerificationNotification";
        }
    }

    public struct MinIO
    {
        public const string Endpoint = "MinIO:Endpoint";
        public const string Region = "MinIO:Region";
        public const string Secure = "MinIO:Secure";
        public const string Bucket = "MinIO:Bucket";
        public const string AccessKey = "MinIO:AccessKey";
        public const string SecretKey = "MinIO:SecretKey";
    }

    public struct Redis
    {
        public const string Connection = "Redis:Connection";
        public const string InstanceName = "Redis:InstanceName";
        public const string ConnectRetry = "Redis:ConnectRetry";
        public const string ConnectTimeout = "Redis:ConnectTimeout";
    }

    public struct Email
    {
        /// <summary>Selects the active <c>IEmailSender</c>: "Smtp" (default) or "Brevo".</summary>
        public const string Provider = "Email:Provider";
    }

    public struct Smtp
    {
        public const string From = "Smtp:From";
        public const string Port = "Smtp:Port";
        public const string Username = "Smtp:Username";
        public const string Password = "Smtp:Password";
        public const string EnableSsl = "Smtp:EnableSsl";
        public const string Host = "Smtp:Host";
    }

    public struct Brevo
    {
        public const string ApiKey = "Brevo:ApiKey";
        public const string SenderEmail = "Brevo:SenderEmail";
        public const string SenderName = "Brevo:SenderName";
        public const string BaseUrl = "Brevo:BaseUrl";
    }

    public struct Replication
    {
        public const string ReadDelay = "Replication:ReadDelay";
    }

    public struct Providers
    {
        public struct OpenTripMap
        {
            public const string BaseUrl = "Providers:OpenTripMap:BaseUrl";
            public const string ApiKey = "Providers:OpenTripMap:ApiKey";
        }

        public struct Foursquare
        {
            public const string BaseUrl = "Providers:Foursquare:BaseUrl";
            public const string ApiKey = "Providers:Foursquare:ApiKey";

            /// <summary>Turns Foursquare enrichment on/off. Defaults to true.</summary>
            public const string EnableEnrichment = "Providers:Foursquare:EnableEnrichment";
        }

        public struct Nominatim
        {
            public const string BaseUrl = "Providers:Nominatim:BaseUrl";
        }

        public struct Geocoding
        {
            /// <summary>Selects the active <c>IGeocodingProvider</c>: "Nominatim" (default) or "OpenTripMap".</summary>
            public const string Provider = "Providers:Geocoding:Provider";
        }

        public struct Destination
        {
            /// <summary>Selects the active <c>IDestinationProvider</c>: "OpenTripMap" (default) or "Foursquare".</summary>
            public const string Provider = "Providers:Destination:Provider";
        }
    }

    public struct Caching
    {
        public struct Locations
        {
            /// <summary>Geocoding search-result cache TTL, in hours. Defaults to 1.</summary>
            public const string TtlHours = "Caching:Locations:TtlHours";
        }

        public struct Destinations
        {
            /// <summary>Attraction list cache TTL, in minutes. Defaults to 30.</summary>
            public const string AttractionListTtlMinutes = "Caching:Destinations:AttractionListTtlMinutes";

            /// <summary>Attraction detail cache TTL, in hours. Defaults to 24.</summary>
            public const string AttractionDetailTtlHours = "Caching:Destinations:AttractionDetailTtlHours";
        }
    }
}