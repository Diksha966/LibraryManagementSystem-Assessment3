using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Controllers
{
    [Authorize(Roles = "Manager")]
    public class ManagerController : Controller
    {
        private readonly LibraryDbContext _context;

        public ManagerController(LibraryDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Use today's date to calculate overdue loans
            // based on the actual due date.
            var today = DateTime.Today;

            // Borrowing statistics
            var totalLoans = await _context.Loans
                .CountAsync();

            var activeLoans = await _context.Loans
                .CountAsync(l =>
                    l.Status == LoanStatus.Active &&
                    l.DueDate.Date >= today);

            var returnedLoans = await _context.Loans
                .CountAsync(l =>
                    l.Status == LoanStatus.Returned);

            var overdueLoans = await _context.Loans
                .CountAsync(l =>
                    (l.Status == LoanStatus.Active &&
                     l.DueDate.Date < today) ||
                    l.Status == LoanStatus.Overdue);

            // Item status statistics
            var totalItems = await _context.Items
                .CountAsync();

            var availableItems = await _context.Items
                .CountAsync(i =>
                    i.Status == ItemStatus.Available);

            var borrowedItems = await _context.Items
                .CountAsync(i =>
                    i.Status == ItemStatus.Borrowed);

            var damagedItems = await _context.Items
                .CountAsync(i =>
                    i.Status == ItemStatus.Damaged);

            var destroyedItems = await _context.Items
                .CountAsync(i =>
                    i.Status == ItemStatus.Destroyed);

            // Fine statistics
            var totalFines = await _context.Fines
                .CountAsync();

            var paidFines = await _context.Fines
                .CountAsync(f => f.IsPaid);

            var unpaidFines = await _context.Fines
                .CountAsync(f => !f.IsPaid);

            var totalFineAmount = await _context.Fines
                .SumAsync(f => (decimal?)f.Amount) ?? 0m;

            var unpaidFineAmount = await _context.Fines
                .Where(f => !f.IsPaid)
                .SumAsync(f => (decimal?)f.Amount) ?? 0m;

            // Notification statistics
            var totalNotifications = await _context.NotificationLogs
                .CountAsync();

            var warningNotifications = await _context.NotificationLogs
                .CountAsync(n => n.Severity == "Warning");

            var errorNotifications = await _context.NotificationLogs
                .CountAsync(n => n.Severity == "Error");

            var recentNotifications = await _context.NotificationLogs
                .OrderByDescending(n => n.CreatedAtUtc)
                .Take(10)
                .ToListAsync();

            // Pass statistics to the dashboard view
            ViewBag.TotalLoans = totalLoans;
            ViewBag.ActiveLoans = activeLoans;
            ViewBag.ReturnedLoans = returnedLoans;
            ViewBag.OverdueLoans = overdueLoans;

            ViewBag.TotalItems = totalItems;
            ViewBag.AvailableItems = availableItems;
            ViewBag.BorrowedItems = borrowedItems;
            ViewBag.DamagedItems = damagedItems;
            ViewBag.DestroyedItems = destroyedItems;

            ViewBag.TotalFines = totalFines;
            ViewBag.PaidFines = paidFines;
            ViewBag.UnpaidFines = unpaidFines;
            ViewBag.TotalFineAmount = totalFineAmount;
            ViewBag.UnpaidFineAmount = unpaidFineAmount;

            ViewBag.TotalNotifications = totalNotifications;
            ViewBag.WarningNotifications = warningNotifications;
            ViewBag.ErrorNotifications = errorNotifications;
            ViewBag.RecentNotifications = recentNotifications;

            return View();
        }
    }
}