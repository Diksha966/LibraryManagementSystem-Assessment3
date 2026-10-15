using LibraryManagementSystem.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Controllers.Api;

[ApiController]
[Route("api/notifications")]
[Authorize(Roles = "Admin,Manager")]
public class NotificationsApiController : ControllerBase
{
    private readonly LibraryDbContext _context;

    public NotificationsApiController(LibraryDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetNotifications(
        [FromQuery] string? severity,
        [FromQuery] string? category,
        [FromQuery] int take = 100,
        [FromQuery] int? branchId = null)
    {
        take = Math.Clamp(take, 1, 500);

        var query = _context.NotificationLogs
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(severity))
        {
            var severityFilter = severity.Trim();
            query = query.Where(n => n.Severity == severityFilter);
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            var categoryFilter = category.Trim();
            query = query.Where(n => n.Category == categoryFilter);
        }

        if (branchId.HasValue)
        {
            query = query.Where(n => n.BranchId == branchId.Value || n.BranchId == null);
        }

        var notifications = await query
            .OrderByDescending(n => n.CreatedAtUtc)
            .Take(take)
            .Select(n => new
            {
                n.Id,
                n.CreatedAtUtc,
                n.Category,
                n.Severity,
                n.Title,
                n.Message,
                n.EntityType,
                n.EntityId,
                n.EventKey,
                n.BranchId
            })
            .ToListAsync();

        return Ok(notifications);
    }
}
