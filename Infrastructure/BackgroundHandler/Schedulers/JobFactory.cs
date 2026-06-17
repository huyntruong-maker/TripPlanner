using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;
using Quartz.Spi;

namespace Infrastructure.BackgroundHandler.Schedulers;

public class JobFactory(
    ILogger<JobFactory> logger,
    IServiceScopeFactory serviceScopeFactory) : IJobFactory
{
    /// <summary>
    ///     Add transient for IJob
    /// </summary>
    public IJob NewJob(TriggerFiredBundle bundle, IScheduler scheduler)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var job = scope.ServiceProvider.GetService(bundle.JobDetail.JobType);
        return (IJob)job!;
    }

    public void ReturnJob(IJob job)
    {
        logger.LogTrace("Job {jobName} complete!", job.GetType());
    }
}