using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Services.Notifications;

public class NotificationService : INotificationService
{
    private readonly LibraryDbContext _context;

    public NotificationService(LibraryDbContext context)
    {
        _context = context;
    }

    public async Task LogAsync(
        string category,
        string severity,
        string title,
        string message,
        string? eventKey = null,
        string? entityType = null,
        int? entityId = null,
        int? branchId = null,
        CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(eventKey))
        {
            bool alreadyExists = await _context.NotificationLogs
                .AnyAsync(n => n.EventKey == eventKey, cancellationToken);

            if (alreadyExists)
            {
                return;
            }
        }

        var notification = new NotificationLog
        {
            Category = category,
            Severity = severity,
            Title = title,
            Message = message,
            EventKey = eventKey,
            EntityType = entityType,
            EntityId = entityId,
            BranchId = branchId,
            CreatedAtUtc = DateTime.UtcNow
        };

        _context.NotificationLogs.Add(notification);

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> CreateOverdueNotificationsAsync(
        CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;

        var overdueLoans = await _context.Loans
            .Where(l => l.Status == LoanStatus.Active && l.DueDate.Date < today)
            .ToListAsync(cancellationToken);

        if (overdueLoans.Count == 0)
        {
            return 0;
        }

        int created = 0;

        foreach (var loan in overdueLoans)
        {
            var item = await _context.Items
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.Id == loan.ItemId, cancellationToken);

            var borrower = await _context.Borrowers
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == loan.BorrowerId, cancellationToken);

            var eventKey = $"loan-overdue-{loan.Id}-{today:yyyyMMdd}";
            int daysOverdue = (today - loan.DueDate.Date).Days;

            await LogAsync(
                category: "Loans",
                severity: "Warning",
                title: "Loan Overdue",
                message: $"Loan #{loan.Id} for '{item?.Title ?? "Unknown Item"}' is {daysOverdue} day(s) overdue (borrower: {borrower?.Name ?? "Unknown"}).",
                eventKey: eventKey,
                entityType: "Loan",
                entityId: loan.Id,
                cancellationToken: cancellationToken);

            loan.Status = LoanStatus.Overdue;
            created++;
        }

        if (created > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        return created;
    }

    public async Task<List<NotificationLog>> GetRecentAsync(
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        return await _context.NotificationLogs
            .AsNoTracking()
            .OrderByDescending(n => n.CreatedAtUtc)
            .Take(take)
            .ToListAsync(cancellationToken);
    }
}
