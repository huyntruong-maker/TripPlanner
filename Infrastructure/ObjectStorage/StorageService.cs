using Application.Dtos.Storage;
using Application.Interfaces.Storage;
using Domain.Constants;
using Domain.Messages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Minio;
using Minio.DataModel;
using Minio.DataModel.Args;
using Minio.Exceptions;

namespace Infrastructure.ObjectStorage;

public class StorageService(
    IConfiguration configuration,
    IMinioClient minioClient,
    ILogger<StorageService> logger) : IStorageService
{
    private readonly string _bucket = configuration[ConfigKeys.MinIO.Bucket] ??
                                      throw new ArgumentNullException(nameof(_bucket), "Bucket is null");

    public async Task<(bool, string)> UploadFile(UploadFileReqDto requestDto)
    {
        var fileExtension = Path.GetExtension(requestDto.File.FileName);
        var fileName = Path.GetFileNameWithoutExtension(requestDto.File.FileName);

        try
        {
            await using var fileStream = requestDto.File.OpenReadStream();
            var bucketExistsArgs = new BucketExistsArgs().WithBucket(_bucket);
            var isExistBucket = await minioClient.BucketExistsAsync(bucketExistsArgs).ConfigureAwait(false);
            if (!isExistBucket) return (false, StorageControllerMsg.Upload.BucketNotFound);

            fileName = string.Concat(fileName, "_", Guid.NewGuid().ToString().Replace("-", ""), fileExtension);

            var putObjectArgs = new PutObjectArgs()
                .WithBucket(_bucket)
                .WithStreamData(fileStream)
                .WithObject(fileName)
                .WithObjectSize(fileStream.Length)
                .WithContentType(ContentType.Get(fileName))
                .WithHeaders(requestDto.Metadata);

            var result = await minioClient.PutObjectAsync(putObjectArgs).ConfigureAwait(false);
            if (result.Etag == null)
            {
                logger.LogError("Upload file {fileName} failed", requestDto.File.FileName);

                return (false, StorageControllerMsg.Upload.Failed);
            }

            logger.LogInformation("Upload file {fileName} Successfully", requestDto.File.FileName);

            return (true, fileName);
        }
        catch (MinioException ex)
        {
            logger.LogError("Upload file failed, MinioException: {ex}", ex);
            return (false, StorageControllerMsg.Upload.Exception);
        }
        catch (Exception ex)
        {
            logger.LogError("Upload file failed, Exception: {ex}", ex);
            return (false, StorageControllerMsg.Upload.Exception);
        }
    }

    public async Task<string> Remove(string fileName)
    {
        try
        {
            var bucketExistsArgs = new BucketExistsArgs().WithBucket(_bucket);
            var isExistBucket = await minioClient.BucketExistsAsync(bucketExistsArgs).ConfigureAwait(false);
            if (!isExistBucket) return StorageControllerMsg.Remove.BucketNotFound;

            var removeObjectArgs = new RemoveObjectArgs()
                .WithBucket(_bucket)
                .WithObject(fileName);
            await minioClient.RemoveObjectAsync(removeObjectArgs).ConfigureAwait(false);

            return string.Empty;
        }
        catch (MinioException ex)
        {
            logger.LogError("Remove file failed, MinioException: {ex}", ex);
            return StorageControllerMsg.Remove.Exception;
        }
        catch (Exception ex)
        {
            logger.LogError("Remove file failed, Exception: {ex}", ex);
            return StorageControllerMsg.Remove.Exception;
        }
    }

    public async Task<(string, DownloadFileDto? fileDto)> DownloadFileStream(string fileName)
    {
        var memoryStream = new MemoryStream();

        try
        {
            var bucketExistsArgs = new BucketExistsArgs().WithBucket(_bucket);
            var isExistBucket = await minioClient.BucketExistsAsync(bucketExistsArgs).ConfigureAwait(false);
            if (!isExistBucket) return (StorageControllerMsg.Download.BucketNotFound, null);

            var statObjectArgs = new StatObjectArgs().WithBucket(_bucket).WithObject(fileName);
            ObjectStat? statObject;

            try
            {
                statObject = await minioClient.StatObjectAsync(statObjectArgs).ConfigureAwait(false);
            }
            catch (ObjectNotFoundException)
            {
                logger.LogWarning("File {fileName} not found", fileName);
                return (StorageControllerMsg.Download.FileNotFound, null);
            }

            await minioClient.GetObjectAsync(new GetObjectArgs()
                             .WithBucket(_bucket)
                             .WithObject(fileName)
                             .WithCallbackStream((stream, cancellationToken) => stream.CopyToAsync(memoryStream, cancellationToken)))
                             .ConfigureAwait(false);

            memoryStream.Position = 0;

            return (string.Empty, new DownloadFileDto()
            {
                Stream = memoryStream,
                ContentType = statObject.ContentType,
                FileName = fileName
            });
        }
        catch (Exception ex)
        {
            await memoryStream.DisposeAsync();

            logger.LogError("Download file failed, Exception: {ex}", ex);
            return (StorageControllerMsg.Download.Exception, null);
        }
    }

    public async Task<(string, string?)> GetPresignedUrl(string fileName, int time = 1)
    {
        try
        {
            var bucketExistsArgs = new BucketExistsArgs().WithBucket(_bucket);
            var isExistBucket = await minioClient.BucketExistsAsync(bucketExistsArgs).ConfigureAwait(false);
            if (!isExistBucket) return (StorageControllerMsg.GetPresignedUrl.BucketNotFound, string.Empty);

            var args = new PresignedGetObjectArgs()
                       .WithBucket(_bucket)
                       .WithObject(fileName)
                       .WithExpiry(time);

            return (string.Empty, await minioClient.PresignedGetObjectAsync(args));
        }
        catch (Exception ex)
        {
            logger.LogError("Error streaming file from MinIO: {ex}", ex);
            return (StorageControllerMsg.GetPresignedUrl.Exception, null);
        }
    }

    public async Task<(bool, string)> AppendTextToFile(string fileName, string[] text)
    {
        if (text.Length == 0)
        {
            logger.LogWarning("No text provided to append to file {fileName}", fileName);
            return (true, string.Empty);
        }

        var memoryStream = new MemoryStream();
        try
        {
            var bucketExistsArgs = new BucketExistsArgs().WithBucket(_bucket);
            var isExistBucket = await minioClient.BucketExistsAsync(bucketExistsArgs).ConfigureAwait(false);
            if (!isExistBucket) return (false, StorageControllerMsg.Upload.BucketNotFound);

            var fileExists = false;
            try
            {
                var statObjectArgs = new StatObjectArgs().WithBucket(_bucket).WithObject(fileName);
                var statObject = await minioClient.StatObjectAsync(statObjectArgs).ConfigureAwait(false);

                if (!statObject.ContentType.Equals(GlobalConstants.MimeTypes.TextPlain))
                {
                    logger.LogError("File {fileName} is not a text file", fileName);
                    return (false, StorageControllerMsg.Upload.InvalidFileType);
                }

                await minioClient.GetObjectAsync(new GetObjectArgs()
                                                 .WithBucket(_bucket)
                                                 .WithObject(fileName)
                                                 .WithCallbackStream((stream, cancellationToken) =>
                                                     stream.CopyToAsync(memoryStream, cancellationToken))
                ).ConfigureAwait(false);

                fileExists = true;
            }
            catch (ObjectNotFoundException)
            {
                logger.LogInformation("File {fileName} not found, creating a new one", fileName);
            }
            catch (Exception ex)
            {
                logger.LogError("Error reading existing file {fileName}: {ex}", fileName, ex);
                return (false, StorageControllerMsg.Upload.Exception);
            }

            if (fileExists)
            {
                memoryStream.Position = memoryStream.Length;
            }

            await using (var writer = new StreamWriter(memoryStream, System.Text.Encoding.UTF8, leaveOpen: true))
            {
                if (fileExists && memoryStream.Length > 0)
                {
                    memoryStream.Position = memoryStream.Length - 1;
                    var lastByte = new byte[1];
                    _ = await memoryStream.ReadAsync(lastByte.AsMemory(0, 1));
                    memoryStream.Position = memoryStream.Length;

                    if (lastByte[0] != '\n')
                    {
                        await writer.WriteLineAsync();
                    }
                }

                foreach (var line in text)
                {
                    await writer.WriteLineAsync(line);
                }

                await writer.FlushAsync();
            }

            memoryStream.Position = 0;
            var putObjectArgs = new PutObjectArgs()
                                .WithBucket(_bucket)
                                .WithStreamData(memoryStream)
                                .WithObject(fileName)
                                .WithObjectSize(memoryStream.Length)
                                .WithContentType(GlobalConstants.MimeTypes.TextPlain);

            var result = await minioClient.PutObjectAsync(putObjectArgs).ConfigureAwait(false);
            if (result.Etag != null) return (true, string.Empty);

            logger.LogError("Failed to append text to file {fileName}", fileName);
            return (false, StorageControllerMsg.Upload.Failed);
        }
        catch (MinioException ex)
        {
            logger.LogError("Failed to append text, MinioException: {ex}", ex);
            return (false, StorageControllerMsg.Upload.Exception);
        }
        catch (Exception ex)
        {
            logger.LogError("Failed to append text, Exception: {ex}", ex);
            return (false, StorageControllerMsg.Upload.Exception);
        }
        finally
        {
            await memoryStream.DisposeAsync();
        }
    }
}