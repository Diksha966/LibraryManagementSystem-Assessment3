using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using LibraryManagementSystem.Services.Branches;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Controllers
{
    // Person 1: inter-branch transfers. Branch staff (Reception) and Admin
    // can request, dispatch, receive and cancel transfers.
    [Authorize(Roles = "Admin,Reception")]
    public class TransfersController : Controller
    {
        private readonly LibraryDbContext _context;
        private readonly ITransferService _transferService;
        private readonly IBranchService _branchService;

        public TransfersController(
            LibraryDbContext context,
            ITransferService transferService,
            IBranchService branchService)
        {
            _context = context;
            _transferService = transferService;
            _branchService = branchService;
        }

        // GET: Transfers?status=Requested&branchId=2
        public async Task<IActionResult> Index(TransferStatus? status, int? branchId, bool openOnly = false)
        {
            var query = _context.ItemTransfers
                .AsNoTracking()
                .Include(t => t.Item)
                .Include(t => t.FromBranch)
                .Include(t => t.ToBranch)
                .AsQueryable();

            if (openOnly)
            {
                query = query.Where(t =>
                    t.Status == TransferStatus.Requested ||
                    t.Status == TransferStatus.InTransit);
            }
            else if (status.HasValue)
            {
                query = query.Where(t => t.Status == status.Value);
            }

            if (branchId.HasValue)
            {
                query = query.Where(t =>
                    t.FromBranchId == branchId.Value ||
                    t.ToBranchId == branchId.Value);
            }

            var transfers = await query
                .OrderBy(t => t.Status == TransferStatus.Received || t.Status == TransferStatus.Cancelled)
                .ThenByDescending(t => t.RequestedAt)
                .ToListAsync();

            ViewBag.Status = status;
            ViewBag.OpenOnly = openOnly;
            ViewBag.BranchId = branchId;
            ViewBag.Branches = new SelectList(
                await _branchService.GetAllBranchesAsync(), "Id", "Name", branchId);

            return View(transfers);
        }

        // GET: Transfers/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var transfer = await _context.ItemTransfers
                .AsNoTracking()
                .Include(t => t.Item)
                .Include(t => t.FromBranch)
                .Include(t => t.ToBranch)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (transfer == null)
                return NotFound();

            return View(transfer);
        }

        // GET: Transfers/Create?itemId=3
        public async Task<IActionResult> Create(int? itemId)
        {
            var model = new TransferCreateViewModel();

            if (itemId.HasValue)
            {
                var item = await _context.Items
                    .AsNoTracking()
                    .Include(i => i.Branch)
                    .FirstOrDefaultAsync(i => i.Id == itemId.Value);

                if (item != null)
                {
                    model.LibraryCode = item.LibraryCode;
                    ViewBag.Item = item;
                }
            }

            await PopulateBranchesAsync(model.ToBranchId);

            return View(model);
        }

        // POST: Transfers/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TransferCreateViewModel model)
        {
            Item? item = null;

            if (!string.IsNullOrWhiteSpace(model.LibraryCode))
            {
                var code = model.LibraryCode.Trim().ToUpper();

                item = await _context.Items
                    .Include(i => i.Branch)
                    .FirstOrDefaultAsync(i => i.LibraryCode.ToUpper() == code);

                if (item == null)
                {
                    ModelState.AddModelError(
                        nameof(model.LibraryCode),
                        "No item was found with that library code.");
                }
            }

            if (ModelState.IsValid && item != null)
            {
                var result = await _transferService.RequestAsync(
                    item.Id,
                    model.ToBranchId,
                    model.Reason,
                    CurrentUser());

                if (result.Success)
                {
                    TempData["Success"] = result.Message;
                    return RedirectToAction(nameof(Details), new { id = result.TransferId });
                }

                ModelState.AddModelError(string.Empty, result.Message);
            }

            ViewBag.Item = item;
            await PopulateBranchesAsync(model.ToBranchId);

            return View(model);
        }

        // POST: Transfers/Dispatch/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Dispatch(int id)
        {
            var result = await _transferService.DispatchAsync(id, CurrentUser());
            return AfterAction(id, result);
        }

        // POST: Transfers/Receive/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Receive(int id)
        {
            var result = await _transferService.ReceiveAsync(id, CurrentUser());
            return AfterAction(id, result);
        }

        // POST: Transfers/Cancel/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id, string? cancelReason)
        {
            var result = await _transferService.CancelAsync(id, cancelReason, CurrentUser());
            return AfterAction(id, result);
        }

        private IActionResult AfterAction(int id, TransferResult result)
        {
            TempData[result.Success ? "Success" : "Error"] = result.Message;
            return RedirectToAction(nameof(Details), new { id });
        }

        private async Task PopulateBranchesAsync(int? selected)
        {
            ViewBag.Branches = new SelectList(
                await _branchService.GetActiveBranchesAsync(), "Id", "Name", selected);
        }

        private string CurrentUser() =>
            User.Identity?.Name ?? "unknown";
    }
}
