using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Controllers
{
    [Authorize(Roles = "Reception")]
    public class FinesController : Controller
    {
        private readonly LibraryDbContext _context;

        public FinesController(LibraryDbContext context)
        {
            _context = context;
        }

        // GET: Fines
        public async Task<IActionResult> Index()
        {
            var fines = await _context.Fines
                .Include(f => f.Loan)
                .ToListAsync();

            foreach (var fine in fines)
            {
                if (fine.Loan != null)
                {
                    fine.Loan.Borrower = await _context.Borrowers
                        .FirstOrDefaultAsync(
                            b => b.Id == fine.Loan.BorrowerId);

                    fine.Loan.Item = await FindItem(
                        fine.Loan.ItemId);
                }
            }

            return View(fines);
        }

        // GET: Fines/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var fine = await _context.Fines
                .Include(f => f.Loan)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (fine == null)
                return NotFound();

            if (fine.Loan != null)
            {
                fine.Loan.Borrower = await _context.Borrowers
                    .FirstOrDefaultAsync(
                        b => b.Id == fine.Loan.BorrowerId);

                fine.Loan.Item = await FindItem(
                    fine.Loan.ItemId);
            }

            return View(fine);
        }

        // GET: Fines/Create
        public async Task<IActionResult> Create()
        {
            await LoadLoans();

            return View();
        }

        // POST: Fines/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("LoanId,Amount,IsPaid")] Fine fine)
        {
            if (ModelState.IsValid)
            {
                _context.Fines.Add(fine);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            await LoadLoans();

            return View(fine);
        }

        // GET: Fines/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var fine = await _context.Fines.FindAsync(id);

            if (fine == null)
                return NotFound();

            await LoadLoans();

            return View(fine);
        }

        // POST: Fines/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("Id,LoanId,Amount,IsPaid")] Fine fine)
        {
            if (id != fine.Id)
                return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(fine);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!FineExists(fine.Id))
                        return NotFound();

                    throw;
                }

                return RedirectToAction(nameof(Index));
            }

            await LoadLoans();

            return View(fine);
        }

        // GET: Fines/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var fine = await _context.Fines
                .Include(f => f.Loan)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (fine == null)
                return NotFound();

            if (fine.Loan != null)
            {
                fine.Loan.Borrower = await _context.Borrowers
                    .FirstOrDefaultAsync(
                        b => b.Id == fine.Loan.BorrowerId);

                fine.Loan.Item = await FindItem(
                    fine.Loan.ItemId);
            }

            return View(fine);
        }

        // POST: Fines/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var fine = await _context.Fines.FindAsync(id);

            if (fine != null)
            {
                _context.Fines.Remove(fine);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Fines/MarkAsPaid/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsPaid(int id)
        {
            var fine = await _context.Fines.FindAsync(id);

            if (fine == null)
                return NotFound();

            fine.IsPaid = true;

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

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

        private async Task LoadLoans()
        {
            var loans = await _context.Loans
                .Include(l => l.Borrower)
                .ToListAsync();

            foreach (var loan in loans)
            {
                loan.Item = await FindItem(loan.ItemId);
            }

            ViewBag.Loans = loans;
        }

        private bool FineExists(int id)
        {
            return _context.Fines.Any(f => f.Id == id);
        }
    }
}