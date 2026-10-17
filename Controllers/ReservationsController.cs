using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using LibraryManagementSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Controllers
{
    // Staff view of the reservation queues.
    [Authorize(Roles = "Reception")]
    public class ReservationsController : Controller
    {
        private readonly LibraryDbContext _context;
        private readonly ReservationService _reservations;

        public ReservationsController(
            LibraryDbContext context,
            ReservationService reservations)
        {
            _context = context;
            _reservations = reservations;
        }

        // GET: Reservations
        public async Task<IActionResult> Index(bool showAll = false)
        {
            var query = _context.Reservations
                .AsNoTracking()
                .Include(r => r.Item)
                .Include(r => r.Borrower)
                .AsQueryable();

            if (!showAll)
            {
                query = query.Where(r =>
                    r.Status == ReservationStatus.Waiting ||
                    r.Status == ReservationStatus.Notified);
            }

            var list = await query
                .OrderBy(r => r.ItemId)
                .ThenBy(r => r.ReservedAt)
                .ThenBy(r => r.Id)
                .ToListAsync();

            ViewBag.ShowAll = showAll;

            return View(list);
        }

        // POST: Reservations/Cancel/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            bool cancelled = await _reservations.CancelAsync(id);

            TempData[cancelled ? "Success" : "Error"] = cancelled
                ? "Reservation cancelled."
                : "That reservation could not be cancelled.";

            return RedirectToAction(nameof(Index));
        }
    }
}
