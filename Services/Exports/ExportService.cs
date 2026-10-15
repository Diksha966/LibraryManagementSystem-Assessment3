using System.Globalization;
using System.Text;
using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Services.Exports;

public class ExportService : IExportService
{
    private readonly LibraryDbContext _context;

    public ExportService(LibraryDbContext context)
    {
        _context = context;
    }

    public async Task<byte[]> BuildLoansCsvAsync(CancellationToken cancellationToken = default)
    {
        var loans = await _context.Loans
            .AsNoTracking()
            .Include(l => l.Borrower)
            .OrderByDescending(l => l.BorrowedDate)
            .ToListAsync(cancellationToken);

        var itemLookup = await _context.Items
            .AsNoTracking()
            .ToDictionaryAsync(i => i.Id, i => i, cancellationToken);

        var sb = new StringBuilder();
        sb.AppendLine("LoanId,ItemTitle,LibraryCode,BorrowerName,BorrowerEmail,BorrowedDate,DueDate,ReturnedDate,Status");

        foreach (var loan in loans)
        {
            itemLookup.TryGetValue(loan.ItemId, out var item);

            sb.AppendLine(string.Join(",",
                Csv(loan.Id.ToString(CultureInfo.InvariantCulture)),
                Csv(item?.Title ?? string.Empty),
                Csv(item?.LibraryCode ?? string.Empty),
                Csv(loan.Borrower?.Name ?? string.Empty),
                Csv(loan.Borrower?.Email ?? string.Empty),
                Csv(loan.BorrowedDate.ToString("yyyy-MM-dd")),
                Csv(loan.DueDate.ToString("yyyy-MM-dd")),
                Csv(loan.ReturnedDate?.ToString("yyyy-MM-dd") ?? string.Empty),
                Csv(loan.Status.ToString())));
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    public async Task<byte[]> BuildNotificationsCsvAsync(CancellationToken cancellationToken = default)
    {
        var notifications = await _context.NotificationLogs
            .AsNoTracking()
            .OrderByDescending(n => n.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var sb = new StringBuilder();
        sb.AppendLine("Id,CreatedAtUtc,Category,Severity,Title,Message,EntityType,EntityId,EventKey,BranchId");

        foreach (var notification in notifications)
        {
            sb.AppendLine(string.Join(",",
                Csv(notification.Id.ToString(CultureInfo.InvariantCulture)),
                Csv(notification.CreatedAtUtc.ToString("yyyy-MM-dd HH:mm:ss")),
                Csv(notification.Category),
                Csv(notification.Severity),
                Csv(notification.Title),
                Csv(notification.Message),
                Csv(notification.EntityType ?? string.Empty),
                Csv(notification.EntityId?.ToString(CultureInfo.InvariantCulture) ?? string.Empty),
                Csv(notification.EventKey ?? string.Empty),
                Csv(notification.BranchId?.ToString(CultureInfo.InvariantCulture) ?? string.Empty)));
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    public async Task<byte[]> BuildDashboardPdfAsync(CancellationToken cancellationToken = default)
    {
        var today = DateTime.Today;

        int totalLoans = await _context.Loans.CountAsync(cancellationToken);
        int activeLoans = await _context.Loans
            .CountAsync(l => l.Status == LoanStatus.Active && l.DueDate.Date >= today, cancellationToken);
        int overdueLoans = await _context.Loans
            .CountAsync(l => (l.Status == LoanStatus.Active && l.DueDate.Date < today) || l.Status == LoanStatus.Overdue, cancellationToken);
        int totalItems = await _context.Items.CountAsync(cancellationToken);
        int totalFines = await _context.Fines.CountAsync(cancellationToken);
        decimal totalFineAmount = await _context.Fines
            .SumAsync(f => (decimal?)f.Amount, cancellationToken) ?? 0m;
        int totalNotifications = await _context.NotificationLogs.CountAsync(cancellationToken);

        var lines = new List<string>
        {
            "Library Management System - Dashboard Export",
            $"Generated (UTC): {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}",
            string.Empty,
            "Borrowing Statistics",
            $"Total Loans: {totalLoans}",
            $"Active Loans: {activeLoans}",
            $"Overdue Loans: {overdueLoans}",
            string.Empty,
            "Item Statistics",
            $"Total Items: {totalItems}",
            string.Empty,
            "Fine Statistics",
            $"Total Fines: {totalFines}",
            $"Total Fine Amount: ${totalFineAmount:0.00}",
            string.Empty,
            "Notification Statistics",
            $"Total Notifications: {totalNotifications}"
        };

        return SimplePdfBuilder.BuildSinglePageTextPdf(lines);
    }

    private static string Csv(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }
}
