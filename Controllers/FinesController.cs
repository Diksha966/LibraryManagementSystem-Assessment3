using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Controllers
{
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
                    .ThenInclude(l => l.Borrower)
                .Include(f => f.Loan)
                    .ThenInclude(l => l.Item)
                .ToListAsync();

            return View(fines);
        }

        // GET: Fines/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var fine = await _context.Fines
                .Include(f => f.Loan)
                    .ThenInclude(l => l.Borrower)
                .Include(f => f.Loan)
                    .ThenInclude(l => l.Item)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (fine == null)
                return NotFound();

            return View(fine);
        }

        // GET: Fines/Create
        public async Task<IActionResult> Create()
        {
            ViewBag.Loans = await _context.Loans
                .Include(l => l.Borrower)
                .Include(l => l.Item)
                .ToListAsync();

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

            ViewBag.Loans = await _context.Loans
                .Include(l => l.Borrower)
                .Include(l => l.Item)
                .ToListAsync();

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

            ViewBag.Loans = await _context.Loans
                .Include(l => l.Borrower)
                .Include(l => l.Item)
                .ToListAsync();

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

            ViewBag.Loans = await _context.Loans
                .Include(l => l.Borrower)
                .Include(l => l.Item)
                .ToListAsync();

            return View(fine);
        }

        // GET: Fines/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var fine = await _context.Fines
                .Include(f => f.Loan)
                    .ThenInclude(l => l.Borrower)
                .Include(f => f.Loan)
                    .ThenInclude(l => l.Item)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (fine == null)
                return NotFound();

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

        private bool FineExists(int id)
        {
            return _context.Fines.Any(f => f.Id == id);
        }
    }
}
