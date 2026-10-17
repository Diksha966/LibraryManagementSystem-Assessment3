using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using LibraryManagementSystem.Services;
using LibraryManagementSystem.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Controllers
{
    // Public page where a patron places a hold on a Borrowed or Damaged item.
    public class ReserveController : Controller
    {
        private readonly LibraryDbContext _context;
        private readonly PatronService _patrons;
        private readonly ReservationService _reservations;

        public ReserveController(
            LibraryDbContext context,
            PatronService patrons,
            ReservationService reservations)
        {
            _context = context;
            _patrons = patrons;
            _reservations = reservations;
        }

        // GET: /Reserve/Index/5
        [HttpGet]
        public async Task<IActionResult> Index(int id)
        {
            var model = await BuildModelAsync(id);

            if (model == null)
                return NotFound();

            return View(model);
        }

        // POST: /Reserve/Index/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(
            int id,
            string? memberNumber,
            string? email)
        {
            var model = await BuildModelAsync(id);

            if (model == null)
                return NotFound();

            model.MemberNumber = memberNumber;
            model.Email = email;

            if (string.IsNullOrWhiteSpace(memberNumber))
                ModelState.AddModelError("MemberNumber", "Please enter your member number.");

            if (string.IsNullOrWhiteSpace(email))
                ModelState.AddModelError("Email", "Please enter your email address.");

            if (!ModelState.IsValid)
                return View(model);

            var borrower = await _patrons.VerifyAsync(memberNumber, email);

            model.Submitted = true;

            if (borrower == null)
            {
                model.Success = false;
                model.Message = "We could not match those details. Please check your member number and email address.";
                return View(model);
            }

            var result = await _reservations.PlaceHoldAsync(id, borrower.Id);

            model.Success = result.Success;
            model.Message = result.Message;
            model.Position = result.Position;

            return View(model);
        }

        private async Task<ReserveViewModel?> BuildModelAsync(int itemId)
        {
            var item = await _context.Items
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.Id == itemId);

            if (item == null)
                return null;

            int queue = await _context.Reservations.CountAsync(r =>
                r.ItemId == itemId &&
                (r.Status == ReservationStatus.Waiting ||
                 r.Status == ReservationStatus.Notified));

            return new ReserveViewModel
            {
                ItemId = item.Id,
                ItemTitle = item.Title,
                ItemCode = item.LibraryCode,
                ItemStatus = item.Status,
                QueueLength = queue
            };
        }
    }
}
