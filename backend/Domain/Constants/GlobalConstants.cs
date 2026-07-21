namespace Domain.Constants;

public static class GlobalConstants
{
    public const string JwtLoginToken = "JwtLoginToken";

    public struct Claim
    {
        public const string UserId = "UserId";
        public const string FirstName = "FirstName";
        public const string LastName = "LastName";
        public const string Email = "Email";
    }

    public struct SwaggerConverterType
    {
        public const string String = "string";
        public const string TimeSpanExampleFormat = "00:00:00";
    }

    public struct Header
    {
        public const string AuthScheme = "Bearer";
        public const string Authorization = "Authorization";
    }

    public struct PageConfig
    {
        public const int Start = 1;
        public const int Length = 10;
        public const int MaxLength = 500;
    }

    public struct SortDirection
    {
        public const string Ascending = "asc";
        public const string Descending = "desc";
    }

    public struct MaxBatchSize
    {
        public const int MaxBatch100 = 100;
        public const int MaxBatch1000 = 1000;
        public const int MaxBatch10000 = 10000;
        public const int MaxBatch100000 = 100000;
        public const int MaxBatch1000000 = 1000000;
    }

    public struct MimeTypes
    {
        public const string TextPlain = "text/plain";
        public const string TextHtml = "text/html";
        public const string TextCss = "text/css";
        public const string TextJavaScript = "text/javascript";

        public const string ImageJpeg = "image/jpeg";
        public const string ImagePng = "image/png";
        public const string ImageGif = "image/gif";
        public const string ImageSvg = "image/svg+xml";

        public const string AudioMp3 = "audio/mpeg";
        public const string AudioOgg = "audio/ogg";

        public const string VideoMp4 = "video/mp4";
        public const string VideoWebm = "video/webm";

        public const string ApplicationOctetStream = "application/octet-stream";
        public const string ApplicationZip = "application/zip";
        public const string ApplicationXZip = "application/x-zip-compressed";
        public const string ApplicationRar = "application/x-rar-compressed";
        public const string ApplicationX = "application/x-compressed";
        public const string ApplicationXRar = "application/x-rar";
        public const string ApplicationVndRar = "application/vnd.rar";
        public const string ApplicationPdf = "application/pdf";
        public const string ApplicationExcel = "application/vnd.ms-excel";
        public const string ApplicationWord = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
    }

    public struct FileExtension
    {
        public const string Zip = ".zip";
        public const string Rar = ".rar";
    }

    public struct SmtpPort
    {
        public const int ImplicitTls = 465;
        public const int StartTls = 587;
    }
}