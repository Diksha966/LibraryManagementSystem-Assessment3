using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using LibraryManagementSystem.Services.Branches;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ItemsController : Controller
    {
        private readonly LibraryDbContext _context;
        private readonly IBranchService _branchService;
        private readonly ITransferService _transferService;

        public ItemsController(
            LibraryDbContext context,
            IBranchService branchService,
            ITransferService transferService)
        {
            _context = context;
            _branchService = branchService;
            _transferService = transferService;
        }

        // GET: Items?branchId=2&status=Available
        public async Task<IActionResult> Index(int? branchId, ItemStatus? status)
        {
            // Query only concrete item types.
            // Item is abstract, so we do not query base Item records.
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

            // Person 1: filter the inventory by branch and status.
            if (branchId.HasValue)
                items = items.Where(i => i.BranchId == branchId.Value);

            if (status.HasValue)
                items = items.Where(i => i.Status == status.Value);

            ViewBag.BranchId = branchId;
            ViewBag.Status = status;
            ViewBag.Branches = new SelectList(
                await _branchService.GetAllBranchesAsync(), "Id", "Name", branchId);

            ViewBag.OpenTransferItemIds = (await _context.ItemTransfers
                .AsNoTracking()
                .Where(t => t.Status == TransferStatus.Requested || t.Status == TransferStatus.InTransit)
                .Select(t => t.ItemId)
                .ToListAsync())
                .ToHashSet();

            return View(items.OrderBy(i => i.Title).ToList());
        }

        // GET: Items/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            Item? item = await FindItem(id.Value);

            if (item == null)
                return NotFound();

            // Person 1: transfer history for this item.
            ViewBag.Transfers = await _context.ItemTransfers
                .AsNoTracking()
                .Include(t => t.FromBranch)
                .Include(t => t.ToBranch)
                .Where(t => t.ItemId == item.Id)
                .OrderByDescending(t => t.RequestedAt)
                .ToListAsync();

            return View(item);
        }

        // GET: Items/Create
        public async Task<IActionResult> Create()
        {
            var branches = await _branchService.GetActiveBranchesAsync();

            var model = new ItemFormViewModel
            {
                BranchId = branches.FirstOrDefault()?.Id ?? 0
            };

            await PopulateBranchesAsync(model.BranchId);

            return View(model);
        }

        // POST: Items/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ItemFormViewModel model)
        {
            ValidateItemType(model);

            // Person 1: new items must go to an active branch, and the
            // In transit status is only ever set by a transfer.
            bool branchOk = await _context.Branches
                .AnyAsync(b => b.Id == model.BranchId && b.IsActive);

            if (!branchOk)
            {
                ModelState.AddModelError(
                    nameof(model.BranchId),
                    "Please choose an active branch.");
            }

            if (model.Status == ItemStatus.InTransit)
            {
                ModelState.AddModelError(
                    nameof(model.Status),
                    "In transit is set automatically when a transfer is dispatched.");
            }

            if (!ModelState.IsValid)
            {
                await PopulateBranchesAsync(model.BranchId);
                return View(model);
            }

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

                    await PopulateBranchesAsync(model.BranchId);
                    return View(model);
            }

            item.BranchId = model.BranchId;

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
                Status = item.Status,
                BranchId = item.BranchId,
                BranchName = item.Branch?.Name
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

            Item? item = await FindItem(id);

            if (item == null)
                return NotFound();

            // Person 1: the branch is read-only here (it changes only through
            // a transfer), and status changes are blocked while a transfer is open.
            model.BranchId = item.BranchId;
            model.BranchName = item.Branch?.Name;
            ModelState.Remove(nameof(model.BranchId));

            var openTransfer = await _transferService.GetOpenTransferAsync(item.Id);

            if (openTransfer != null && model.Status != item.Status)
            {
                ModelState.AddModelError(
                    nameof(model.Status),
                    $"This item has an open transfer (#{openTransfer.Id}) to {openTransfer.ToBranch?.Name}. " +
                    "Receive or cancel the transfer before changing its status.");
            }
            else if (model.Status == ItemStatus.InTransit && item.Status != ItemStatus.InTransit)
            {
                ModelState.AddModelError(
                    nameof(model.Status),
                    "In transit is set automatically when a transfer is dispatched.");
            }

            if (!ModelState.IsValid)
                return View(model);

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

            // Person 1: keep transfer history intact.
            bool hasTransfers = await _context.ItemTransfers
                .AnyAsync(t => t.ItemId == id);

            if (hasTransfers)
            {
                TempData["Error"] =
                    "This item cannot be deleted because it has transfer history.";

                return RedirectToAction(nameof(Index));
            }

            _context.Items.Remove(item);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        private async Task<Item?> FindItem(int id)
        {
            Item? book = await _context.Books
                .Include(b => b.Branch)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (book != null)
                return book;

            Item? toy = await _context.Toys
                .Include(t => t.Branch)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (toy != null)
                return toy;

            return await _context.Music
                .Include(m => m.Branch)
                .FirstOrDefaultAsync(m => m.Id == id);
        }

        private async Task PopulateBranchesAsync(int? selected)
        {
            ViewBag.Branches = new SelectList(
                await _branchService.GetActiveBranchesAsync(), "Id", "Name", selected);
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