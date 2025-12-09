using Byee.Server.Services.Interfaces;

namespace Byee.Server.Services;

public class CleanupBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CleanupBackgroundService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(15);

    public CleanupBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<CleanupBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Cleanup background service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var claimService = scope.ServiceProvider.GetRequiredService<IFileClaimService>();
                
                await claimService.CleanupStaleClaimsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during cleanup");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }
}
