using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using LibraryManagementSystem.Services.Branches;
using LibraryManagementSystem.Services.Notifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Controllers
{
    [Authorize(Roles = "Reception")]
    public class LoansController : Controller
    {
        private readonly LibraryDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly ITransferService _transferService;

        public LoansController(
            LibraryDbContext context,
            INotificationService notificationService,
            ITransferService transferService)
        {
            _context = context;
            _notificationService = notificationService;
            _transferService = transferService;
        }

        // GET: Loans
        public async Task<IActionResult> Index()
        {
            var loans = await _context.Loans
                .Include(l => l.Borrower)
                .OrderByDescending(l => l.BorrowedDate)
                .ToListAsync();

            foreach (var loan in loans)
            {
                loan.Item = await FindItem(loan.ItemId);
            }

            return View(loans);
        }

        // GET: Loans/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var loan = await _context.Loans
                .Include(l => l.Borrower)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (loan == null)
                return NotFound();

            loan.Item = await FindItem(loan.ItemId);

            if (loan.Item == null)
                return NotFound();

            return View(loan);
        }

        // GET: Loans/Create
        public IActionResult Create()
        {
            ViewBag.BorrowerId = new SelectList(
                _context.Borrowers,
                "Id",
                "Name");

            return View();
        }

        // POST: Loans/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            string? LibraryCode,
            Loan loan)
        {
            if (string.IsNullOrWhiteSpace(LibraryCode))
            {
                ModelState.AddModelError(
                    "LibraryCode",
                    "Please enter a library code.");
            }

            Item? item = null;

            if (!string.IsNullOrWhiteSpace(LibraryCode))
            {
                item = await FindItemByLibraryCode(
                    LibraryCode.Trim());

                if (item == null)
                {
                    ModelState.AddModelError(
                        "LibraryCode",
                        "No item was found with that library code.");
                }
                else if (item.Status == ItemStatus.InTransit)
                {
                    // Person 1: dispatched items are locked until received.
                    ModelState.AddModelError(
                        "LibraryCode",
                        "This item is in transit between branches and cannot be borrowed until it is received.");
                }
                else if (item.Status != ItemStatus.Available)
                {
                    ModelState.AddModelError(
                        "LibraryCode",
                        "This item is not available for borrowing.");
                }
                else if (await _transferService.HasOpenTransferAsync(item.Id))
                {
                    // Person 1: an item waiting to be dispatched is also locked.
                    ModelState.AddModelError(
                        "LibraryCode",
                        "This item has been requested for transfer to another branch. " +
                        "Cancel the transfer before lending it.");
                }
            }

            var borrower = await _context.Borrowers
                .FindAsync(loan.BorrowerId);

            if (borrower == null)
            {
                ModelState.AddModelError(
                    "BorrowerId",
                    "Please select a valid borrower.");
            }

            if (ModelState.IsValid && item != null)
            {
                loan.ItemId = item.Id;
                loan.BorrowedDate = DateTime.Now;
                loan.DueDate = DateTime.Now.AddDays(14);
                loan.ReturnedDate = null;
                loan.Status = LoanStatus.Active;

                item.Status = ItemStatus.Borrowed;

                _context.Loans.Add(loan);

                await _context.SaveChangesAsync();

                await _notificationService.LogAsync(
                    category: "Loans",
                    severity: "Info",
                    title: "Item Borrowed",
                    message: $"{borrower?.Name} borrowed {item.Title} ({item.LibraryCode}).",
                    eventKey: $"loan-borrowed-{loan.Id}",
                    entityType: "Loan",
                    entityId: loan.Id,
                    branchId: item.BranchId);

                return RedirectToAction(nameof(Index));
            }

            ViewBag.BorrowerId = new SelectList(
                _context.Borrowers,
                "Id",
                "Name",
                loan.BorrowerId);

            return View(loan);
        }

        // GET: Loans/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var loan = await _context.Loans.FindAsync(id);

            if (loan == null)
                return NotFound();

            ViewBag.ItemId = new SelectList(
                await GetAvailableItems(loan.ItemId),
                "Id",
                "Title",
                loan.ItemId);

            ViewBag.BorrowerId = new SelectList(
                _context.Borrowers,
                "Id",
                "Name",
                loan.BorrowerId);

            return View(loan);
        }

        // POST: Loans/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            Loan loan)
        {
            if (id != loan.Id)
                return NotFound();

            if (ModelState.IsValid)
            {
                var existingLoan = await _context.Loans
                    .FirstOrDefaultAsync(l => l.Id == id);

                if (existingLoan == null)
                    return NotFound();

                existingLoan.BorrowerId = loan.BorrowerId;
                existingLoan.ItemId = loan.ItemId;
                existingLoan.BorrowedDate = loan.BorrowedDate;
                existingLoan.DueDate = loan.DueDate;
                existingLoan.ReturnedDate = loan.ReturnedDate;
                existingLoan.Status = loan.Status;

                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            ViewBag.ItemId = new SelectList(
                await GetAvailableItems(loan.ItemId),
                "Id",
                "Title",
                loan.ItemId);

            ViewBag.BorrowerId = new SelectList(
                _context.Borrowers,
                "Id",
                "Name",
                loan.BorrowerId);

            return View(loan);
        }

        // GET: Loans/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var loan = await _context.Loans
                .Include(l => l.Borrower)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (loan == null)
                return NotFound();

            loan.Item = await FindItem(loan.ItemId);

            if (loan.Item == null)
                return NotFound();

            return View(loan);
        }

        // POST: Loans/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var loan = await _context.Loans.FindAsync(id);

            if (loan != null)
            {
                _context.Loans.Remove(loan);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Loans/Return/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Return(int id)
        {
            var loan = await _context.Loans
                .FirstOrDefaultAsync(l => l.Id == id);

            if (loan == null)
                return NotFound();

            if (loan.Status == LoanStatus.Active)
            {
                var item = await FindItem(loan.ItemId);
                bool fineCreated = false;
                decimal fineAmount = 0m;

                loan.Status = LoanStatus.Returned;
                loan.ReturnedDate = DateTime.Now;

                // Make the item available again.
                if (item != null)
                {
                    item.Status = ItemStatus.Available;
                }

                // Calculate a fine if the item was returned late.
                if (loan.ReturnedDate.Value.Date > loan.DueDate.Date)
                {
                    int lateDays =
                        (loan.ReturnedDate.Value.Date -
                         loan.DueDate.Date).Days;

                    fineAmount = lateDays * 2.00m;

                    var existingFine = await _context.Fines
                        .FirstOrDefaultAsync(
                            f => f.LoanId == loan.Id);

                    if (existingFine == null)
                    {
                        var fine = new Fine
                        {
                            LoanId = loan.Id,
                            Amount = fineAmount,
                            IsPaid = false
                        };

                        _context.Fines.Add(fine);
                        fineCreated = true;
                    }
                }

                await _context.SaveChangesAsync();

                await _notificationService.LogAsync(
                    category: "Loans",
                    severity: "Info",
                    title: "Item Returned",
                    message: $"Loan #{loan.Id} has been returned.",
                    eventKey: $"loan-returned-{loan.Id}",
                    entityType: "Loan",
                    entityId: loan.Id);

                if (fineCreated)
                {
                    await _notificationService.LogAsync(
                        category: "Fines",
                        severity: "Warning",
                        title: "Late Return Fine Generated",
                        message: $"Loan #{loan.Id} generated a fine of ${fineAmount:0.00}.",
                        eventKey: $"loan-fine-created-{loan.Id}",
                        entityType: "Loan",
                        entityId: loan.Id);
                }
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task<Item?> FindItemByLibraryCode(
    string libraryCode)
{
    libraryCode = libraryCode.Trim().ToUpper();

    var book = await _context.Books
        .FirstOrDefaultAsync(
            b => b.LibraryCode.ToUpper() == libraryCode);

    if (book != null)
        return book;

    var toy = await _context.Toys
        .FirstOrDefaultAsync(
            t => t.LibraryCode.ToUpper() == libraryCode);

    if (toy != null)
        return toy;

    return await _context.Music
        .FirstOrDefaultAsync(
            m => m.LibraryCode.ToUpper() == libraryCode);
}

        // Find a concrete item by ID.
        private async Task<Item?> FindItem(int id)
        {
            var book = await _context.Books
                .FirstOrDefaultAsync(b => b.Id == id);

            if (book != null)
                return book;

            var toy = await _context.Toys
                .FirstOrDefaultAsync(t => t.Id == id);

            if (toy != null)
                return toy;

            return await _context.Music
                .FirstOrDefaultAsync(m => m.Id == id);
        }

        // Get available items plus the item currently attached to the loan.
        private async Task<List<Item>> GetAvailableItems(
            int? currentItemId = null)
        {
            var books = await _context.Books
                .Where(b =>
                    b.Status == ItemStatus.Available ||
                    b.Id == currentItemId)
                .ToListAsync();

            var toys = await _context.Toys
                .Where(t =>
                    t.Status == ItemStatus.Available ||
                    t.Id == currentItemId)
                .ToListAsync();

            var music = await _context.Music
                .Where(m =>
                    m.Status == ItemStatus.Available ||
                    m.Id == currentItemId)
                .ToListAsync();

            return books.Cast<Item>()
                .Concat(toys.Cast<Item>())
                .Concat(music.Cast<Item>())
                .OrderBy(i => i.Title)
                .ToList();
        }

        private bool LoanExists(int id)
        {
            return _context.Loans.Any(l => l.Id == id);
        }
    }
}