using LibraryManagementSystem.Data;
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
    }
}
