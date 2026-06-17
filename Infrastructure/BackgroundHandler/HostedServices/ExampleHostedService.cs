using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.BackgroundHandler.HostedServices;

public class ExampleHostedService(
    ILogger<ExampleHostedService> logger,
    IConfiguration configuration) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (true)
        {
            logger.LogInformation("Example hosted service executed at: {date:yyyy-MM-ddd}", DateTime.UtcNow);
            await Task.Delay(10000, stoppingToken); 
        }
    }
}