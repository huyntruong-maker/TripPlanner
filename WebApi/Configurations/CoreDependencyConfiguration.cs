using Application.Common;
using Application.Features.Auth;
using Application.MediatR;
using Infrastructure.Caching;
using Infrastructure.DataAccess;
using Infrastructure.Email;
using Infrastructure.Identity;
using Infrastructure.ObjectStorage;
using Infrastructure.Restful;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace WebApi.Configurations;

public static class CoreDependencyConfiguration
{
    public static void AddCoreDependencies(this IServiceCollection collection, IConfiguration configuration)
    {
        collection.AddHttpContextAccessor();
        collection.AddFeatures();
        collection.AddOrchestratorDatabases(configuration);
        collection.AddRestfulService();
        collection.AddObjectStorage(configuration);
        collection.AddRedis(configuration);
        collection.AddHealthChecks();
        collection.AddSecurity(configuration);
        collection.AddEmail();

        collection.AddMediatRConfig();

        collection.AddUserContext();
        collection.AddBehaviours();
    }

    private static void AddFeatures(this IServiceCollection collection)
    {
        collection.AddAuthFeatures();
    }
}