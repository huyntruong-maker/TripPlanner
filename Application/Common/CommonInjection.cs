using Application.Common.Behaviours;
using Application.Common.Services;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Common;

public static class CommonInjection
{
    public static void AddUserContext(this IServiceCollection collection)
    {
        collection.AddScoped<IUserContextService, UserContextService>();
    }

    public static void AddBehaviours(this IServiceCollection collection)
    {
        collection.AddScoped(typeof(IPipelineBehavior<,>), typeof(ReadAfterWriteBehaviour<,>));
    }
}