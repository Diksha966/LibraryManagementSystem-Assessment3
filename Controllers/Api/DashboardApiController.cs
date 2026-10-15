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

        var payload = new
        {
            BranchId = branchId,
            Borrowing = new
            {
                TotalLoans = await _context.Loans.CountAsync(),
                ActiveLoans = await _context.Loans.CountAsync(l => l.Status == LoanStatus.Active && l.DueDate.Date >= today),
                ReturnedLoans = await _context.Loans.CountAsync(l => l.Status == LoanStatus.Returned),
                OverdueLoans = await _context.Loans.CountAsync(l =>
                    (l.Status == LoanStatus.Active && l.DueDate.Date < today) || l.Status == LoanStatus.Overdue)
            },
            Items = new
            {
                TotalItems = await _context.Items.CountAsync(),
                AvailableItems = await _context.Items.CountAsync(i => i.Status == ItemStatus.Available),
                BorrowedItems = await _context.Items.CountAsync(i => i.Status == ItemStatus.Borrowed),
                DamagedItems = await _context.Items.CountAsync(i => i.Status == ItemStatus.Damaged),
                DestroyedItems = await _context.Items.CountAsync(i => i.Status == ItemStatus.Destroyed)
            },
            Fines = new
            {
                TotalFines = await _context.Fines.CountAsync(),
                PaidFines = await _context.Fines.CountAsync(f => f.IsPaid),
                UnpaidFines = await _context.Fines.CountAsync(f => !f.IsPaid),
                TotalFineAmount = await _context.Fines.SumAsync(f => (decimal?)f.Amount) ?? 0m,
                UnpaidFineAmount = await _context.Fines.Where(f => !f.IsPaid).SumAsync(f => (decimal?)f.Amount) ?? 0m
            },
            Notifications = new
            {
                Total = await _context.NotificationLogs.CountAsync(),
                Warnings = await _context.NotificationLogs.CountAsync(n => n.Severity == "Warning"),
                Errors = await _context.NotificationLogs.CountAsync(n => n.Severity == "Error"),
                Latest = await _context.NotificationLogs
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
