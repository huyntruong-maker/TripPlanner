namespace Domain.Messages;

public static class StorageControllerMsg
{
    public struct Upload
    {
        public const string BucketNotFound = "Storage.Upload.BucketNotFound";
        public const string InvalidFileType = "Storage.Upload.InvalidFileType";
        public const string Failed = "Storage.Upload.Failed";
        public const string Exception = "Storage.Upload.Exception";
    }

    public struct Remove
    {
        public const string BucketNotFound = "Storage.Remove.BucketNotFound";
        public const string Exception = "Storage.Remove.Exception";
    }

    public struct Download
    {
        public const string BucketNotFound = "Storage.Download.BucketNotFound";
        public const string FileNotFound = "Storage.Download.FileNotFound";
        public const string Exception = "Storage.Download.Exception";
    }

    public struct GetPresignedUrl
    {
        public const string BucketNotFound = "Storage.GetPresignedUrl.BucketNotFound";
        public const string Exception = "Storage.GetPresignedUrl.Exception";
    }
}