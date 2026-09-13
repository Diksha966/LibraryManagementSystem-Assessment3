using LibraryManagementSystem.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Controllers
{
    public class ItemsController : Controller
    {
        private readonly LibraryDbContext _context;

        public ItemsController(LibraryDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var items = await _context.Items
                .AsNoTracking()
                .ToListAsync();

            return View(items);
        }
    }
}
