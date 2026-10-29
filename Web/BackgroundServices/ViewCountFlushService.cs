using EShop.Application.Interfaces;

namespace EShop.Web.BackgroundServices;

public class ViewCountFlushService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ViewCountFlushService> _logger;

    public ViewCountFlushService(
        IServiceScopeFactory scopeFactory,
        ILogger<ViewCountFlushService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);

                using var scope = _scopeFactory.CreateScope();
                var tracker = scope.ServiceProvider.GetRequiredService<IProductViewTracker>();

                if (tracker.PendingCount > 0)
                {
                    _logger.LogInformation(
                        "Flushing {Count} pending view counts", tracker.PendingCount);
                    await tracker.FlushAsync();
                }
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ViewCount flush failed");
            }
        }
    }
}