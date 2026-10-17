using System.Globalization;
using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Services;

public class ReportTable
{
    public string Key { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; } = DateTime.Now;
    public List<(string Label, string Value)> Summary { get; } = new();
    public string[] Columns { get; set; } = Array.Empty<string>();
    public List<string[]> Rows { get; } = new();
}

/// <summary>
/// Builds the three manager reports as plain tables so the same data can be
/// previewed on screen and exported to CSV or PDF.
/// </summary>
public class ReportService
{
    public const string Borrowing = "borrowing";
    public const string Fines = "fines";
    public const string Inventory = "inventory";

    private readonly LibraryDbContext _context;

    public ReportService(LibraryDbContext context)
    {
        _context = context;
    }

    public Task<ReportTable?> BuildAsync(string? key)
    {
        return (key ?? string.Empty).ToLowerInvariant() switch
        {
            Borrowing => BuildBorrowingAsync(),
            Fines => BuildFinesAsync(),
            Inventory => BuildInventoryAsync(),
            _ => Task.FromResult<ReportTable?>(null)
        };
    }

    private static string Date(DateTime? d) =>
        d.HasValue ? d.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : "";

    private static string Money(decimal d) =>
        d.ToString("0.00", CultureInfo.InvariantCulture);

    private async Task<ReportTable?> BuildBorrowingAsync()
    {
        var today = DateTime.Today;

        var loans = await _context.Loans
            .AsNoTracking()
            .Include(l => l.Item)
            .Include(l => l.Borrower)
            .OrderByDescending(l => l.BorrowedDate)
            .ToListAsync();

        string StatusOf(Loan l) =>
            (l.Status == LoanStatus.Active && l.DueDate.Date < today) ||
            l.Status == LoanStatus.Overdue
                ? "Overdue"
                : l.Status.ToString();

        var report = new ReportTable
        {
            Key = Borrowing,
            Title = "Borrowing Statistics Report",
            Columns = new[]
            {
                "Loan", "Item", "Code", "Borrower",
                "Borrowed", "Due", "Returned", "Status"
            }
        };

        report.Summary.Add(("Total loans", loans.Count.ToString()));
        report.Summary.Add(("Active (not overdue)",
            loans.Count(l => StatusOf(l) == "Active").ToString()));
        report.Summary.Add(("Overdue",
            loans.Count(l => StatusOf(l) == "Overdue").ToString()));
        report.Summary.Add(("Returned",
            loans.Count(l => l.Status == LoanStatus.Returned).ToString()));
        report.Summary.Add(("Lost",
            loans.Count(l => l.Status == LoanStatus.Lost).ToString()));

        foreach (var l in loans)
        {
            report.Rows.Add(new[]
            {
                l.Id.ToString(),
                l.Item?.Title ?? "(unknown item)",
                l.Item?.LibraryCode ?? "",
                l.Borrower?.Name ?? "(unknown borrower)",
                Date(l.BorrowedDate),
                Date(l.DueDate),
                Date(l.ReturnedDate),
                StatusOf(l)
            });
        }

        return report;
    }

    private async Task<ReportTable?> BuildFinesAsync()
    {
        var fines = await _context.Fines
            .AsNoTracking()
            .Include(f => f.Loan).ThenInclude(l => l!.Item)
            .Include(f => f.Loan).ThenInclude(l => l!.Borrower)
            .OrderBy(f => f.Id)
            .ToListAsync();

        decimal total = fines.Sum(f => f.Amount);
        decimal collected = fines.Where(f => f.IsPaid).Sum(f => f.Amount);
        decimal outstanding = total - collected;

        var report = new ReportTable
        {
            Key = Fines,
            Title = "Fine Revenue Audit",
            Columns = new[]
            {
                "Fine", "Loan", "Borrower", "Item", "Amount ($)", "Status"
            }
        };

        report.Summary.Add(("Fines issued", fines.Count.ToString()));
        report.Summary.Add(("Total assessed ($)", Money(total)));
        report.Summary.Add(("Collected ($)", Money(collected)));
        report.Summary.Add(("Outstanding ($)", Money(outstanding)));
        report.Summary.Add(("Collection rate",
            total == 0
                ? "n/a"
                : (collected / total * 100m).ToString("0.0", CultureInfo.InvariantCulture) + "%"));

        foreach (var f in fines)
        {
            report.Rows.Add(new[]
            {
                f.Id.ToString(),
                f.LoanId.ToString(),
                f.Loan?.Borrower?.Name ?? "(unknown borrower)",
                f.Loan?.Item?.Title ?? "(unknown item)",
                Money(f.Amount),
                f.IsPaid ? "Paid" : "Unpaid"
            });
        }

        return report;
    }

    private async Task<ReportTable?> BuildInventoryAsync()
    {
        var items = await _context.Items
            .AsNoTracking()
            .Include(i => i.Branch)
            .OrderBy(i => i.LibraryCode)
            .ToListAsync();

        var loanCounts = await _context.Loans
            .GroupBy(l => l.ItemId)
            .Select(g => new { ItemId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ItemId, x => x.Count);

        int activeHolds = await _context.Reservations.CountAsync(r =>
            r.Status == ReservationStatus.Waiting ||
            r.Status == ReservationStatus.Notified);

        int available = items.Count(i => i.Status == ItemStatus.Available);
        int borrowed = items.Count(i => i.Status == ItemStatus.Borrowed);
        int damaged = items.Count(i => i.Status == ItemStatus.Damaged);
        int destroyed = items.Count(i => i.Status == ItemStatus.Destroyed);
        int inTransit = items.Count(i => i.Status == ItemStatus.InTransit);

        var report = new ReportTable
        {
            Key = Inventory,
            Title = "Inventory Health Report",
            Columns = new[]
            {
                "Code", "Title", "Type", "Branch", "Status", "Times borrowed"
            }
        };

        report.Summary.Add(("Total items", items.Count.ToString()));
        report.Summary.Add(("Available", available.ToString()));
        report.Summary.Add(("Borrowed", borrowed.ToString()));
        report.Summary.Add(("Damaged", damaged.ToString()));
        report.Summary.Add(("Destroyed", destroyed.ToString()));
        report.Summary.Add(("In transit", inTransit.ToString()));
        report.Summary.Add(("Needing attention (damaged + destroyed)",
            (damaged + destroyed).ToString()));
        report.Summary.Add(("Availability rate",
            items.Count == 0
                ? "n/a"
                : ((decimal)available / items.Count * 100m)
                    .ToString("0.0", CultureInfo.InvariantCulture) + "%"));
        report.Summary.Add(("Active reservations", activeHolds.ToString()));

        foreach (var i in items)
        {
            string type = i switch
            {
                Book => "Book",
                Toy => "Toy",
                Music => "Music",
                _ => "Item"
            };

            report.Rows.Add(new[]
            {
                i.LibraryCode,
                i.Title,
                type,
                i.Branch?.Name ?? "",
                i.Status == ItemStatus.InTransit ? "In transit" : i.Status.ToString(),
                loanCounts.TryGetValue(i.Id, out int c) ? c.ToString() : "0"
            });
        }

        return report;
    }
}
