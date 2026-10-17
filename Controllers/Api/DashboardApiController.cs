using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Controllers.Api;

[ApiController]
[Route("api/dashboard")]
[Authorize(Roles = "Admin,Manager")]
public class DashboardApiController : ControllerBase
{
    private readonly LibraryDbContext _context;

    public DashboardApiController(LibraryDbContext context)
    {
        _context = context;
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary([FromQuery] int? branchId = null)
    {
        var today = DateTime.Today;

        // Person 1: when branchId is given, every figure is limited to that
        // branch. Loans and fines belong to the branch that holds the item.
        var items = _context.Items.AsQueryable();
        var loans = _context.Loans.AsQueryable();
        var fines = _context.Fines.AsQueryable();
        var notifications = _context.NotificationLogs.AsQueryable();
        var transfers = _context.ItemTransfers.AsQueryable();

        if (branchId.HasValue)
        {
            int b = branchId.Value;

            items = items.Where(i => i.BranchId == b);
            loans = loans.Where(l => _context.Items.Any(i => i.Id == l.ItemId && i.BranchId == b));
            fines = fines.Where(f => _context.Loans.Any(l =>
                l.Id == f.LoanId && _context.Items.Any(i => i.Id == l.ItemId && i.BranchId == b)));
            notifications = notifications.Where(n => n.BranchId == b || n.BranchId == null);
            transfers = transfers.Where(t => t.FromBranchId == b || t.ToBranchId == b);
        }

        var payload = new
        {
            BranchId = branchId,
            Borrowing = new
            {
                TotalLoans = await loans.CountAsync(),
                ActiveLoans = await loans.CountAsync(l => l.Status == LoanStatus.Active && l.DueDate.Date >= today),
                ReturnedLoans = await loans.CountAsync(l => l.Status == LoanStatus.Returned),
                OverdueLoans = await loans.CountAsync(l =>
                    (l.Status == LoanStatus.Active && l.DueDate.Date < today) || l.Status == LoanStatus.Overdue)
            },
            Items = new
            {
                TotalItems = await items.CountAsync(),
                AvailableItems = await items.CountAsync(i => i.Status == ItemStatus.Available),
                BorrowedItems = await items.CountAsync(i => i.Status == ItemStatus.Borrowed),
                DamagedItems = await items.CountAsync(i => i.Status == ItemStatus.Damaged),
                DestroyedItems = await items.CountAsync(i => i.Status == ItemStatus.Destroyed),
                InTransitItems = await items.CountAsync(i => i.Status == ItemStatus.InTransit)
            },
            Transfers = new
            {
                Open = await transfers.CountAsync(t => t.Status == TransferStatus.Requested || t.Status == TransferStatus.InTransit),
                Requested = await transfers.CountAsync(t => t.Status == TransferStatus.Requested),
                InTransit = await transfers.CountAsync(t => t.Status == TransferStatus.InTransit),
                Received = await transfers.CountAsync(t => t.Status == TransferStatus.Received)
            },
            Fines = new
            {
                TotalFines = await fines.CountAsync(),
                PaidFines = await fines.CountAsync(f => f.IsPaid),
                UnpaidFines = await fines.CountAsync(f => !f.IsPaid),
                TotalFineAmount = await fines.SumAsync(f => (decimal?)f.Amount) ?? 0m,
                UnpaidFineAmount = await fines.Where(f => !f.IsPaid).SumAsync(f => (decimal?)f.Amount) ?? 0m
            },
            Notifications = new
            {
                Total = await notifications.CountAsync(),
                Warnings = await notifications.CountAsync(n => n.Severity == "Warning"),
                Errors = await notifications.CountAsync(n => n.Severity == "Error"),
                Latest = await notifications
                    .OrderByDescending(n => n.CreatedAtUtc)
                    .Take(10)
                    .Select(n => new
                    {
                        n.Id,
                        n.CreatedAtUtc,
                        n.Category,
                        n.Severity,
                        n.Title,
                        n.Message,
                        n.EntityType,
                        n.EntityId,
                        n.BranchId
                    })
                    .ToListAsync()
            }
        };

        return Ok(payload);
    }
}
