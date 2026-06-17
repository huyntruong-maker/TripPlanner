using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.BackgroundHandler.QuartzJobs;

public static class JobInjection
{
    public static void AddJobs(this IServiceCollection collection)
    {
        collection.AddTransient<ExampleJob>();
    }
}