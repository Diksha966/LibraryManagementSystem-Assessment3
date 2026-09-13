using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Controllers
{
    public class BorrowersController : Controller
    {
        private readonly LibraryDbContext _context;

        public BorrowersController(LibraryDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var borrowers = await _context.Borrowers
                .OrderBy(b => b.Name)
                .ToListAsync();

            return View(borrowers);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var borrower = await _context.Borrowers
                .FirstOrDefaultAsync(b => b.Id == id);

            if (borrower == null)
                return NotFound();

            return View(borrower);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("Name,Email")] Borrower borrower)
        {
            if (!ModelState.IsValid)
                return View(borrower);

            _context.Borrowers.Add(borrower);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var borrower = await _context.Borrowers.FindAsync(id);

            if (borrower == null)
                return NotFound();

            return View(borrower);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("Id,Name,Email")] Borrower borrower)
        {
            if (id != borrower.Id)
                return NotFound();

            if (!ModelState.IsValid)
                return View(borrower);

            _context.Update(borrower);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var borrower = await _context.Borrowers
                .FirstOrDefaultAsync(b => b.Id == id);

            if (borrower == null)
                return NotFound();

            return View(borrower);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var borrower = await _context.Borrowers.FindAsync(id);

            if (borrower != null)
            {
                _context.Borrowers.Remove(borrower);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
