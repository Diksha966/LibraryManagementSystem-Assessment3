using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Controllers.Api;

[ApiController]
[Route("api/items")]
[Authorize(Roles = "Admin,Reception,Manager")]
public class ItemsApiController : ControllerBase
{
    private readonly LibraryDbContext _context;

    public ItemsApiController(LibraryDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetItems(
        [FromQuery] string? search,
        [FromQuery] ItemStatus? status,
        [FromQuery] int? branchId)
    {
        var query = _context.Items.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchTerm = search.Trim();
            query = query.Where(i =>
                i.Title.Contains(searchTerm) ||
                i.LibraryCode.Contains(searchTerm));
        }

        if (status.HasValue)
        {
            query = query.Where(i => i.Status == status.Value);
        }

        // Person 1: branch filter (contract agreed with Person 2).
        if (branchId.HasValue)
        {
            query = query.Where(i => i.BranchId == branchId.Value);
        }

        var items = await query
            .OrderBy(i => i.Title)
            .Select(i => new
            {
                i.Id,
                i.Title,
                i.LibraryCode,
                i.Description,
                Status = i.Status.ToString(),
                ItemType = i.GetType().Name,
                i.BranchId,
                BranchName = i.Branch != null ? i.Branch.Name : null
            })
            .ToListAsync();

        return Ok(items);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetItemById(int id, [FromQuery] int? branchId)
    {
        var item = await _context.Items
            .AsNoTracking()
            .Include(i => i.Branch)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (item == null || (branchId.HasValue && item.BranchId != branchId.Value))
        {
            return NotFound();
        }

        return Ok(new
        {
            item.Id,
            item.Title,
            item.LibraryCode,
            item.Description,
            Status = item.Status.ToString(),
            ItemType = item.GetType().Name,
            item.BranchId,
            BranchName = item.Branch?.Name
        });
    }
}
