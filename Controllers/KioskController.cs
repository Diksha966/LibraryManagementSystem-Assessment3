using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using LibraryManagementSystem.Services;
using LibraryManagementSystem.Services.Branches;
using LibraryManagementSystem.Services.Notifications;
using LibraryManagementSystem.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Controllers
{
    // Touch-friendly self-service kiosk. Public (no staff login), but every
    // borrowing action requires the patron to identify themselves first.
    public class KioskController : Controller
    {
        private const string BorrowerKey = "KioskBorrowerId";
        private const string LastActiveKey = "KioskLastActive";
        private const string FailedKey = "KioskFailedSignIns";
        private const int IdleSeconds = 120;
        private const int MaxFailedSignIns = 5;

        private readonly LibraryDbContext _context;
        private readonly PatronService _patrons;
        private readonly ReservationService _reservations;
        private readonly ITransferService _transfers;
        private readonly INotificationService _notifications;

        public KioskController(
            LibraryDbContext context,
            PatronService patrons,
            ReservationService reservations,
            ITransferService transfers,
            INotificationService notifications)
        {
            _context = context;
            _patrons = patrons;
            _reservations = reservations;
            _transfers = transfers;
            _notifications = notifications;
        }

        // GET: /Kiosk  (home screen)
        public IActionResult Index()
        {
            return View();
        }

        // GET: /Kiosk/SignIn
        [HttpGet]
        public IActionResult SignIn(string? next)
        {
            return View(new KioskSignInViewModel { Next = NormaliseNext(next) });
        }

        // POST: /Kiosk/SignIn
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SignIn(KioskSignInViewModel model)
        {
            model.Next = NormaliseNext(model.Next);

            if ((HttpContext.Session.GetInt32(FailedKey) ?? 0) >= MaxFailedSignIns)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Too many unsuccessful attempts. Please ask a member of staff for help.");

                return View(model);
            }

            if (!ModelState.IsValid)
                return View(model);

            var borrower = await _patrons.VerifyAsync(model.MemberNumber, model.Email);

            if (borrower == null)
            {
                HttpContext.Session.SetInt32(
                    FailedKey,
                    (HttpContext.Session.GetInt32(FailedKey) ?? 0) + 1);

                ModelState.AddModelError(
                    string.Empty,
                    "We could not match those details. Please check your member number and email address.");

                return View(model);
            }

            HttpContext.Session.Remove(FailedKey);
            HttpContext.Session.SetInt32(BorrowerKey, borrower.Id);
            Touch();

            return RedirectToAction(model.Next);
        }

        // GET: /Kiosk/Checkout
        [HttpGet]
        public async Task<IActionResult> Checkout()
        {
            var borrower = await GetSignedInBorrowerAsync();

            if (borrower == null)
                return RedirectToAction(nameof(SignIn), new { next = "Checkout" });

            return View(new KioskCheckoutViewModel { BorrowerName = borrower.Name });
        }

        // POST: /Kiosk/Checkout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(string? libraryCode)
        {
            var borrower = await GetSignedInBorrowerAsync();

            if (borrower == null)
                return RedirectToAction(nameof(SignIn), new { next = "Checkout" });

            var model = new KioskCheckoutViewModel { BorrowerName = borrower.Name };

            if (string.IsNullOrWhiteSpace(libraryCode))
            {
                model.Message = "Please scan or type the library code on the item.";
                return View(model);
            }

            string code = libraryCode.Trim().ToUpper();

            var item = await _context.Items
                .FirstOrDefaultAsync(i => i.LibraryCode.ToUpper() == code);

            if (item == null)
            {
                model.Message = "We could not find that code. Please try again or ask a member of staff.";
                return View(model);
            }

            if (item.Status != ItemStatus.Available)
            {
                model.Message = item.Status == ItemStatus.InTransit
                    ? "Sorry, \"" + item.Title + "\" is in transit between branches. You can reserve it from the Public Library search page."
                    : item.Status == ItemStatus.Borrowed || item.Status == ItemStatus.Damaged
                        ? "Sorry, \"" + item.Title + "\" is not available right now. You can reserve it from the Public Library search page."
                        : "Sorry, \"" + item.Title + "\" cannot be borrowed.";

                return View(model);
            }

            // Same rule as the reception desk: an item waiting to be sent to
            // another branch cannot be lent.
            if (await _transfers.HasOpenTransferAsync(item.Id))
            {
                model.Message = "Sorry, \"" + item.Title + "\" has been requested for transfer to another branch and cannot be borrowed right now.";
                return View(model);
            }

            if (!await _reservations.CanBorrowAsync(item.Id, borrower.Id))
            {
                model.Message = "Sorry, \"" + item.Title + "\" is being held for another member.";
                return View(model);
            }

            // Same rules as the reception desk (LoansController.Create).
            var loan = new Loan
            {
                ItemId = item.Id,
                BorrowerId = borrower.Id,
                BorrowedDate = DateTime.Now,
                DueDate = DateTime.Now.AddDays(14),
                ReturnedDate = null,
                Status = LoanStatus.Active
            };

            item.Status = ItemStatus.Borrowed;
            _context.Loans.Add(loan);
            await _context.SaveChangesAsync();

            await _reservations.FulfilAsync(item.Id, borrower.Id);

            // Same notification log entry as a borrow at the reception desk.
            await _notifications.LogAsync(
                category: "Loans",
                severity: "Info",
                title: "Item Borrowed",
                message: $"{borrower.Name} borrowed {item.Title} ({item.LibraryCode}) at the kiosk.",
                eventKey: $"loan-borrowed-{loan.Id}",
                entityType: "Loan",
                entityId: loan.Id,
                branchId: item.BranchId);

            model.Success = true;
            model.ItemTitle = item.Title;
            model.DueDate = loan.DueDate;
            model.Message = "You have borrowed \"" + item.Title + "\".";

            return View(model);
        }

        // GET: /Kiosk/Account
        public async Task<IActionResult> Account()
        {
            var borrower = await GetSignedInBorrowerAsync();

            if (borrower == null)
                return RedirectToAction(nameof(SignIn), new { next = "Account" });

            var model = new KioskAccountViewModel
            {
                Borrower = borrower,

                Loans = await _context.Loans
                    .AsNoTracking()
                    .Include(l => l.Item)
                    .Where(l => l.BorrowerId == borrower.Id &&
                        (l.Status == LoanStatus.Active ||
                         l.Status == LoanStatus.Overdue))
                    .OrderBy(l => l.DueDate)
                    .ToListAsync(),

                UnpaidFines = await _context.Fines
                    .AsNoTracking()
                    .Include(f => f.Loan).ThenInclude(l => l!.Item)
                    .Where(f => !f.IsPaid && f.Loan!.BorrowerId == borrower.Id)
                    .ToListAsync(),

                Reservations = await _reservations
                    .GetBorrowerReservationsAsync(borrower.Id)
            };

            return View(model);
        }

        // POST: /Kiosk/CancelReservation
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelReservation(int id)
        {
            var borrower = await GetSignedInBorrowerAsync();

            if (borrower == null)
                return RedirectToAction(nameof(SignIn), new { next = "Account" });

            await _reservations.CancelAsync(id, borrower.Id);

            return RedirectToAction(nameof(Account));
        }

        // GET: /Kiosk/Reset  (sign out and return to the home screen)
        public IActionResult Reset()
        {
            HttpContext.Session.Clear();
            return RedirectToAction(nameof(Index));
        }

        private static string NormaliseNext(string? next) =>
            next == "Account" ? "Account" : "Checkout";

        private void Touch()
        {
            HttpContext.Session.SetString(
                LastActiveKey,
                DateTime.UtcNow.Ticks.ToString());
        }

        // Returns the signed-in patron, or null if nobody is signed in or the
        // session has been idle too long (protects the next person at the kiosk).
        private async Task<Borrower?> GetSignedInBorrowerAsync()
        {
            int? id = HttpContext.Session.GetInt32(BorrowerKey);

            if (id == null)
                return null;

            bool expired = true;

            if (long.TryParse(HttpContext.Session.GetString(LastActiveKey), out long ticks))
            {
                var last = new DateTime(ticks, DateTimeKind.Utc);
                expired = DateTime.UtcNow - last > TimeSpan.FromSeconds(IdleSeconds);
            }

            if (expired)
            {
                HttpContext.Session.Clear();
                return null;
            }

            var borrower = await _context.Borrowers.FindAsync(id.Value);

            if (borrower == null)
            {
                HttpContext.Session.Clear();
                return null;
            }

            Touch();

            return borrower;
        }
    }
}
