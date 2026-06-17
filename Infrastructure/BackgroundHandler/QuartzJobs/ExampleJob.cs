using Microsoft.Extensions.Logging;
using Quartz;

namespace Infrastructure.BackgroundHandler.QuartzJobs;

public class ExampleJob(ILogger<ExampleJob> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        while (true)
        {
            logger.LogInformation("Example job executed at: {date:yyyy-MM-ddd}", DateTime.UtcNow);
            await Task.Delay(10000);
        }
    }
}