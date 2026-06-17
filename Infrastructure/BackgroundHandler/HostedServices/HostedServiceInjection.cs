using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.BackgroundHandler.HostedServices;

public static class HostedServiceInjection
{
    public static void AddHostedServices(this IServiceCollection collection)
    {
        collection.AddHostedService<ExampleHostedService>();
    }
}