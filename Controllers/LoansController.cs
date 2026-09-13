using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Controllers
{
    public class LoansController : Controller
    {
        private readonly LibraryDbContext _context;

        public LoansController(LibraryDbContext context)
        {
            _context = context;
        }

        // GET: Loans
        public async Task<IActionResult> Index()
        {
            var loans = await _context.Loans
                .Include(l => l.Item)
                .Include(l => l.Borrower)
                .OrderByDescending(l => l.BorrowedDate)
                .ToListAsync();

            return View(loans);
        }

        // GET: Loans/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var loan = await _context.Loans
                .Include(l => l.Item)
                .Include(l => l.Borrower)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (loan == null)
                return NotFound();

            return View(loan);
        }

        // GET: Loans/Create
        public IActionResult Create()
        {
            ViewBag.ItemId = new SelectList(
                _context.Items.Where(i => i.Status == ItemStatus.Available),
                "Id",
                "Title");

            ViewBag.BorrowerId = new SelectList(
                _context.Borrowers,
                "Id",
                "Name");

            return View();
        }

        // POST: Loans/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Loan loan)
        {
            var item = await _context.Items.FindAsync(loan.ItemId);
            var borrower = await _context.Borrowers.FindAsync(loan.BorrowerId);

            if (item == null)
                ModelState.AddModelError("ItemId", "Please select a valid item.");

            if (borrower == null)
                ModelState.AddModelError("BorrowerId", "Please select a valid borrower.");

            if (item != null && item.Status != ItemStatus.Available)
                ModelState.AddModelError("ItemId", "This item is not available.");

            if (ModelState.IsValid)
            {
                loan.BorrowedDate = DateTime.Now;
                loan.ReturnedDate = null;
                loan.Status = LoanStatus.Active;

                _context.Loans.Add(loan);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            ViewBag.ItemId = new SelectList(
                _context.Items.Where(i => i.Status == ItemStatus.Available),
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

        // GET: Loans/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var loan = await _context.Loans.FindAsync(id);

            if (loan == null)
                return NotFound();

            ViewBag.ItemId = new SelectList(_context.Items, "Id", "Title", loan.ItemId);
            ViewBag.BorrowerId = new SelectList(_context.Borrowers, "Id", "Name", loan.BorrowerId);

            return View(loan);
        }

        // POST: Loans/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Loan loan)
        {
            if (id != loan.Id)
                return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(loan);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!LoanExists(loan.Id))
                        return NotFound();

                    throw;
                }

                return RedirectToAction(nameof(Index));
            }

            ViewBag.ItemId = new SelectList(_context.Items, "Id", "Title", loan.ItemId);
            ViewBag.BorrowerId = new SelectList(_context.Borrowers, "Id", "Name", loan.BorrowerId);

            return View(loan);
        }

        // GET: Loans/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var loan = await _context.Loans
                .Include(l => l.Item)
                .Include(l => l.Borrower)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (loan == null)
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
            var loan = await _context.Loans.FindAsync(id);

            if (loan == null)
                return NotFound();

            if (loan.Status == LoanStatus.Active)
            {
                loan.Status = LoanStatus.Returned;
                loan.ReturnedDate = DateTime.Now;

                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool LoanExists(int id)
        {
            return _context.Loans.Any(l => l.Id == id);
        }
    }
}
