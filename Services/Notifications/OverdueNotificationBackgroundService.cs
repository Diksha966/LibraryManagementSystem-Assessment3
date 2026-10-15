namespace LibraryManagementSystem.Services.Notifications;

public class OverdueNotificationBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OverdueNotificationBackgroundService> _logger;

    public OverdueNotificationBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<OverdueNotificationBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var notificationService = scope.ServiceProvider
                    .GetRequiredService<INotificationService>();

                int created = await notificationService
                    .CreateOverdueNotificationsAsync(stoppingToken);

                if (created > 0)
                {
                    _logger.LogInformation(
                        "Created {Count} overdue notification(s).",
                        created);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate overdue notifications.");
            }

            await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
        }
    }
}
