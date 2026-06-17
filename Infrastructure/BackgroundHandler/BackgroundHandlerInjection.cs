using System.Collections.Specialized;
using Domain.Constants;
using Infrastructure.BackgroundHandler.DbContext;
using Infrastructure.BackgroundHandler.HostedServices;
using Infrastructure.BackgroundHandler.QuartzJobs;
using Infrastructure.BackgroundHandler.Schedulers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using Quartz.Spi;

namespace Infrastructure.BackgroundHandler;

public static class BackgroundHandlerInjection
{
    public static void AddBackgroundHandler(this IServiceCollection collection, IConfiguration configuration)
    {
        collection.AddJobStore(configuration);
        collection.ConfigureSchedulerFactory(configuration);

        collection.AddScoped<IScheduleService, ScheduleService>();
        collection.AddSingleton<IJobFactory, JobFactory>();
        collection.AddJobs();

        collection.AddHostedServices();
    }

    private static void ConfigureSchedulerFactory(this IServiceCollection collection, IConfiguration configuration)
    {
        var connectionString = configuration.GetSection(ConfigKeys.Databases.Quartz).Value;
        if (string.IsNullOrEmpty(connectionString))
            throw new ArgumentNullException(nameof(connectionString), "Quartz database connection string is null");

        var properties = new NameValueCollection
        {
            ["quartz.scheduler.instanceName"] = "ClusteredScheduler",
            ["quartz.scheduler.instanceId"] = "AUTO",

            ["quartz.threadPool.type"] = "Quartz.Simpl.DefaultThreadPool, Quartz",
            ["quartz.threadPool.threadCount"] = "25",
            ["quartz.threadPool.threadPriority"] = "5",

            ["quartz.jobStore.useProperties"] = "true",
            ["quartz.jobStore.type"] = "Quartz.Impl.AdoJobStore.JobStoreTX, Quartz",
            ["quartz.jobStore.driverDelegateType"] = "Quartz.Impl.AdoJobStore.PostgreSQLDelegate, Quartz",
            ["quartz.jobStore.tablePrefix"] = "QRTZ_",
            ["quartz.jobStore.dataSource"] = "myDS",
            ["quartz.dataSource.myDS.connectionString"] = connectionString,
            ["quartz.dataSource.myDS.provider"] = "Npgsql",
            ["quartz.jobStore.clustered"] = "true",

            ["quartz.serializer.type"] = "newtonsoft"
        };

        var schedulerFactory = SchedulerBuilder.Create(properties).Build();

        collection.AddSingleton<ISchedulerFactory>(_ => schedulerFactory);
    }
}