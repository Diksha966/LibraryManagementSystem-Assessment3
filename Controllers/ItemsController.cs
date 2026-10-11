using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ItemsController : Controller
    {
        private readonly LibraryDbContext _context;

        public ItemsController(LibraryDbContext context)
        {
            _context = context;
        }

        // GET: Items
        public async Task<IActionResult> Index()
        {
            // Query only concrete item types.
            // Item is abstract, so we do not query base Item records.
            var books = await _context.Books
                .AsNoTracking()
                .ToListAsync();

            var toys = await _context.Toys
                .AsNoTracking()
                .ToListAsync();

            var music = await _context.Music
                .AsNoTracking()
                .ToListAsync();

            var items = books.Cast<Item>()
                .Concat(toys.Cast<Item>())
                .Concat(music.Cast<Item>())
                .OrderBy(i => i.Title)
                .ToList();

            return View(items);
        }

        // GET: Items/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            Item? item = await FindItem(id.Value);

            if (item == null)
                return NotFound();

            return View(item);
        }

        // GET: Items/Create
        public IActionResult Create()
        {
            return View(new ItemFormViewModel());
        }

        // POST: Items/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ItemFormViewModel model)
        {
            ValidateItemType(model);

            if (!ModelState.IsValid)
                return View(model);

            Item item;

            switch (model.ItemType)
            {
                case "Book":
                    item = new Book
                    {
                        Title = model.Title,
                        Description = model.Description,
                        LibraryCode = model.LibraryCode,
                        Status = model.Status,
                        Author = model.Author,
                        Genre = model.Genre,
                        ISBN = model.ISBN
                    };
                    break;

                case "Toy":
                    item = new Toy
                    {
                        Title = model.Title,
                        Description = model.Description,
                        LibraryCode = model.LibraryCode,
                        Status = model.Status,
                        ToyType = model.ToyType,
                        RecommendedAge = model.RecommendedAge
                    };
                    break;

                case "Music":
                    item = new Music
                    {
                        Title = model.Title,
                        Description = model.Description,
                        LibraryCode = model.LibraryCode,
                        Status = model.Status,
                        Artist = model.Artist,
                        Year = model.Year,
                        Format = model.Format
                    };
                    break;

                default:
                    ModelState.AddModelError(
                        "ItemType",
                        "Please select a valid item type.");

                    return View(model);
            }

            _context.Items.Add(item);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // GET: Items/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            Item? item = await FindItem(id.Value);

            if (item == null)
                return NotFound();

            var model = new ItemFormViewModel
            {
                Id = item.Id,
                Title = item.Title,
                Description = item.Description,
                LibraryCode = item.LibraryCode,
                Status = item.Status
            };

            switch (item)
            {
                case Book book:
                    model.ItemType = "Book";
                    model.Author = book.Author;
                    model.Genre = book.Genre;
                    model.ISBN = book.ISBN;
                    break;

                case Toy toy:
                    model.ItemType = "Toy";
                    model.ToyType = toy.ToyType;
                    model.RecommendedAge = toy.RecommendedAge;
                    break;

                case Music music:
                    model.ItemType = "Music";
                    model.Artist = music.Artist;
                    model.Year = music.Year;
                    model.Format = music.Format;
                    break;
            }

            return View(model);
        }

        // POST: Items/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            ItemFormViewModel model)
        {
            if (id != model.Id)
                return NotFound();

            ValidateItemType(model);

            if (!ModelState.IsValid)
                return View(model);

            Item? item = await FindItem(id);

            if (item == null)
                return NotFound();

            item.Title = model.Title;
            item.Description = model.Description;
            item.LibraryCode = model.LibraryCode;
            item.Status = model.Status;

            switch (item)
            {
                case Book book:
                    book.Author = model.Author;
                    book.Genre = model.Genre;
                    book.ISBN = model.ISBN;
                    break;

                case Toy toy:
                    toy.ToyType = model.ToyType;
                    toy.RecommendedAge = model.RecommendedAge;
                    break;

                case Music music:
                    music.Artist = model.Artist;
                    music.Year = model.Year;
                    music.Format = model.Format;
                    break;
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // GET: Items/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            Item? item = await FindItem(id.Value);

            if (item == null)
                return NotFound();

            return View(item);
        }

        // POST: Items/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            Item? item = await FindItem(id);

            if (item == null)
                return NotFound();

            // Do not delete an item that has loan history.
            bool hasLoans = await _context.Loans
                .AnyAsync(l => l.ItemId == id);

            if (hasLoans)
            {
                TempData["Error"] =
                    "This item cannot be deleted because it has loan history.";

                return RedirectToAction(nameof(Index));
            }

            _context.Items.Remove(item);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        private async Task<Item?> FindItem(int id)
        {
            Item? book = await _context.Books
                .FirstOrDefaultAsync(b => b.Id == id);

            if (book != null)
                return book;

            Item? toy = await _context.Toys
                .FirstOrDefaultAsync(t => t.Id == id);

            if (toy != null)
                return toy;

            return await _context.Music
                .FirstOrDefaultAsync(m => m.Id == id);
        }

        private void ValidateItemType(ItemFormViewModel model)
        {
            if (model.ItemType == "Book")
            {
                if (string.IsNullOrWhiteSpace(model.Author))
                    ModelState.AddModelError(
                        "Author",
                        "Author is required for books.");

                if (string.IsNullOrWhiteSpace(model.Genre))
                    ModelState.AddModelError(
                        "Genre",
                        "Genre is required for books.");
            }

            if (model.ItemType == "Toy")
            {
                if (string.IsNullOrWhiteSpace(model.ToyType))
                    ModelState.AddModelError(
                        "ToyType",
                        "Toy type is required for toys.");
            }

            if (model.ItemType == "Music")
            {
                if (string.IsNullOrWhiteSpace(model.Artist))
                    ModelState.AddModelError(
                        "Artist",
                        "Artist is required for music.");
            }
        }
    }
}