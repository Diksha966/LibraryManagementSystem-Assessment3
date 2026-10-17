using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Controllers
{
    public class PublicController : Controller
    {
        private readonly LibraryDbContext _context;

        public PublicController(LibraryDbContext context)
        {
            _context = context;
        }

        // Public library search
        public async Task<IActionResult> Index(string? search, int? branchId)
        {
            var books = await _context.Books
                .AsNoTracking()
                .Include(b => b.Branch)
                .ToListAsync();

            var toys = await _context.Toys
                .AsNoTracking()
                .Include(t => t.Branch)
                .ToListAsync();

            var music = await _context.Music
                .AsNoTracking()
                .Include(m => m.Branch)
                .ToListAsync();

            var items = books.Cast<Item>()
                .Concat(toys.Cast<Item>())
                .Concat(music.Cast<Item>());

            if (!string.IsNullOrWhiteSpace(search))
{
    search = search.Trim();

    var searchUpper = search.ToUpper();

    items = items.Where(i =>
        i.Title.ToUpper().Contains(searchUpper) ||
        i.LibraryCode.ToUpper().Contains(searchUpper));
}  
            // Person 1: patrons can limit results to one branch.
            if (branchId.HasValue)
                items = items.Where(i => i.BranchId == branchId.Value);

            var results = items
                .OrderBy(i => i.Title)
                .ToList();

            ViewBag.Search = search;
            ViewBag.BranchId = branchId;
            ViewBag.Branches = new SelectList(
                await _context.Branches
                    .AsNoTracking()
                    .Where(b => b.IsActive)
                    .OrderBy(b => b.Name)
                    .ToListAsync(),
                "Id", "Name", branchId);

            return View(results);
        }

        // Public item details
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            Item? item = await FindItem(id.Value);

            if (item == null)
                return NotFound();

            return View(item);
        }

        private async Task<Item?> FindItem(int id)
        {
            var book = await _context.Books
                .AsNoTracking()
                .Include(b => b.Branch)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (book != null)
                return book;

            var toy = await _context.Toys
                .AsNoTracking()
                .Include(t => t.Branch)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (toy != null)
                return toy;

            return await _context.Music
                .AsNoTracking()
                .Include(m => m.Branch)
                .FirstOrDefaultAsync(m => m.Id == id);
        }
    }
}