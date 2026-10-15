using LibraryManagementSystem.Models;

namespace LibraryManagementSystem.Services.Notifications;

public interface INotificationService
{
    Task LogAsync(
        string category,
        string severity,
        string title,
        string message,
        string? eventKey = null,
        string? entityType = null,
        int? entityId = null,
        int? branchId = null,
        CancellationToken cancellationToken = default);

    Task<int> CreateOverdueNotificationsAsync(CancellationToken cancellationToken = default);

    Task<List<NotificationLog>> GetRecentAsync(
        int take = 50,
        CancellationToken cancellationToken = default);
}
