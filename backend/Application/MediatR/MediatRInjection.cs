using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Application.MediatR;

public static class MediatRInjection
{
    public static void AddMediatRConfig(this IServiceCollection collection)
    {
        collection.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
        });
    }
}