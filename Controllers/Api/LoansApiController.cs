using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Controllers.Api;

[ApiController]
[Route("api/loans")]
[Authorize(Roles = "Admin,Reception,Manager")]
public class LoansApiController : ControllerBase
{
    private readonly LibraryDbContext _context;

    public LoansApiController(LibraryDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetLoans(
        [FromQuery] LoanStatus? status,
        [FromQuery] bool overdueOnly = false,
        [FromQuery] int? branchId = null)
    {
        var today = DateTime.Today;

        var query = _context.Loans
            .AsNoTracking()
            .Include(l => l.Borrower)
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(l => l.Status == status.Value);
        }

        if (overdueOnly)
        {
            query = query.Where(l =>
                (l.Status == LoanStatus.Active && l.DueDate.Date < today) ||
                l.Status == LoanStatus.Overdue);
        }

        var itemLookup = await _context.Items
            .AsNoTracking()
            .ToDictionaryAsync(i => i.Id, i => i);

        var loans = await query
            .OrderByDescending(l => l.BorrowedDate)
            .ToListAsync();

        var response = loans.Select(l =>
        {
            itemLookup.TryGetValue(l.ItemId, out var item);

            return new
            {
                l.Id,
                l.ItemId,
                ItemTitle = item?.Title,
                LibraryCode = item?.LibraryCode,
                l.BorrowerId,
                BorrowerName = l.Borrower?.Name,
                BorrowerEmail = l.Borrower?.Email,
                l.BorrowedDate,
                l.DueDate,
                l.ReturnedDate,
                Status = l.Status.ToString(),
                IsOverdue = l.DueDate.Date < today && l.Status != LoanStatus.Returned,
                BranchId = branchId
            };
        });

        return Ok(response);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetLoanById(int id, [FromQuery] int? branchId = null)
    {
        var loan = await _context.Loans
            .AsNoTracking()
            .Include(l => l.Borrower)
            .FirstOrDefaultAsync(l => l.Id == id);

        if (loan == null)
        {
            return NotFound();
        }

        var item = await _context.Items
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == loan.ItemId);

        return Ok(new
        {
            loan.Id,
            loan.ItemId,
            ItemTitle = item?.Title,
            LibraryCode = item?.LibraryCode,
            loan.BorrowerId,
            BorrowerName = loan.Borrower?.Name,
            BorrowerEmail = loan.Borrower?.Email,
            loan.BorrowedDate,
            loan.DueDate,
            loan.ReturnedDate,
            Status = loan.Status.ToString(),
            BranchId = branchId
        });
    }
}
