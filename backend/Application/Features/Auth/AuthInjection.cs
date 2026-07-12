using Application.Features.Auth.Shared;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Features.Auth;

public static class AuthInjection
{
    public static void AddAuthFeatures(this IServiceCollection collection)
    {
        collection.AddScoped<IAuthShareService, AuthShareService>();
    }
}