using Application.Interfaces.Restful;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Restful;

public static class RestfulInjection
{
    public static void AddRestfulService(this IServiceCollection collection)
    {
        collection.AddHttpClient();
        collection.AddTransient<IRestfulService, RestfulService>();
    }
}