using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using LibraryManagementSystem.Services.Branches;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Controllers
{
    // Person 1: branch records and per-branch inventory.
    // Admin and Manager can view; only Admin can create or change branches.
    [Authorize(Roles = "Admin,Manager")]
    public class BranchesController : Controller
    {
        private readonly LibraryDbContext _context;
        private readonly IBranchService _branchService;

        public BranchesController(
            LibraryDbContext context,
            IBranchService branchService)
        {
            _context = context;
            _branchService = branchService;
        }

        // GET: Branches
        public async Task<IActionResult> Index()
        {
            var summary = await _branchService.GetInventorySummaryAsync();
            return View(summary);
        }

        // GET: Branches/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var branch = await _context.Branches
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == id);

            if (branch == null)
                return NotFound();

            ViewBag.Items = await _context.Items
                .AsNoTracking()
                .Where(i => i.BranchId == branch.Id)
                .OrderBy(i => i.Title)
                .ToListAsync();

            ViewBag.Transfers = await _context.ItemTransfers
                .AsNoTracking()
                .Include(t => t.Item)
                .Include(t => t.FromBranch)
                .Include(t => t.ToBranch)
                .Where(t => t.FromBranchId == branch.Id || t.ToBranchId == branch.Id)
                .OrderByDescending(t => t.RequestedAt)
                .Take(20)
                .ToListAsync();

            return View(branch);
        }

        // GET: Branches/Create
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View(new Branch { IsActive = true });
        }

        // POST: Branches/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(
            [Bind("Code,Name,Address,IsActive")] Branch branch)
        {
            branch.Code = (branch.Code ?? string.Empty).Trim().ToUpperInvariant();
            branch.Name = (branch.Name ?? string.Empty).Trim();

            await ValidateUniqueCodeAsync(branch);

            if (!ModelState.IsValid)
                return View(branch);

            _context.Branches.Add(branch);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Branch {branch.Name} ({branch.Code}) was created.";

            return RedirectToAction(nameof(Index));
        }

        // GET: Branches/Edit/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var branch = await _context.Branches.FindAsync(id);

            if (branch == null)
                return NotFound();

            return View(branch);
        }

        // POST: Branches/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("Id,Code,Name,Address,IsActive")] Branch model)
        {
            if (id != model.Id)
                return NotFound();

            var branch = await _context.Branches.FindAsync(id);

            if (branch == null)
                return NotFound();

            model.Code = (model.Code ?? string.Empty).Trim().ToUpperInvariant();
            model.Name = (model.Name ?? string.Empty).Trim();

            await ValidateUniqueCodeAsync(model);

            // A branch cannot be closed while it still holds stock or has
            // transfers on the way in or out.
            if (branch.IsActive && !model.IsActive)
            {
                bool holdsItems = await _context.Items
                    .AnyAsync(i => i.BranchId == id);

                bool hasOpenTransfers = await _context.ItemTransfers
                    .AnyAsync(t =>
                        (t.FromBranchId == id || t.ToBranchId == id) &&
                        (t.Status == TransferStatus.Requested || t.Status == TransferStatus.InTransit));

                if (holdsItems || hasOpenTransfers)
                {
                    ModelState.AddModelError(
                        nameof(Branch.IsActive),
                        "This branch still holds items or has open transfers. " +
                        "Transfer its items to another branch before deactivating it.");
                }
            }

            if (!ModelState.IsValid)
                return View(model);

            branch.Code = model.Code;
            branch.Name = model.Name;
            branch.Address = model.Address;
            branch.IsActive = model.IsActive;

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Branch {branch.Name} was updated.";

            return RedirectToAction(nameof(Index));
        }

        private async Task ValidateUniqueCodeAsync(Branch branch)
        {
            if (string.IsNullOrWhiteSpace(branch.Code))
                return;

            bool codeTaken = await _context.Branches
                .AnyAsync(b => b.Id != branch.Id && b.Code.ToUpper() == branch.Code);

            if (codeTaken)
            {
                ModelState.AddModelError(
                    nameof(Branch.Code),
                    "Another branch already uses this code.");
            }
        }
    }
}
