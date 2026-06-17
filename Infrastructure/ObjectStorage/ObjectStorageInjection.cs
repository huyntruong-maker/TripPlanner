using Application.Interfaces.Storage;
using Domain.Constants;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Minio;

namespace Infrastructure.ObjectStorage;

public static class ObjectStorageInjection
{
    public static void AddObjectStorage(this IServiceCollection collection, IConfiguration configuration)
    {
        var endpoint = configuration.GetSection(ConfigKeys.MinIO.Endpoint).Value;
        var accessKey = configuration.GetSection(ConfigKeys.MinIO.AccessKey).Value;
        var secretKey = configuration.GetSection(ConfigKeys.MinIO.SecretKey).Value;
        var region = configuration.GetSection(ConfigKeys.MinIO.Region).Value;
        var secure = configuration.GetSection(ConfigKeys.MinIO.Secure).Get<bool>();

        collection.AddMinio(configureClient => configureClient
            .WithEndpoint(endpoint)
            .WithCredentials(accessKey, secretKey)
            .WithSSL(secure)
            .WithRegion(region)
            .Build());
        
        collection.AddScoped<IStorageService, StorageService>();
    }
}