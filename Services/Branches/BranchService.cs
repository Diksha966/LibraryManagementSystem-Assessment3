using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Services.Branches;

public interface IBranchService
{
    Task<List<Branch>> GetActiveBranchesAsync(CancellationToken cancellationToken = default);

    Task<List<Branch>> GetAllBranchesAsync(CancellationToken cancellationToken = default);

    Task<List<BranchInventorySummary>> GetInventorySummaryAsync(CancellationToken cancellationToken = default);
}

// Person 1: read-only branch queries shared by several controllers.
public class BranchService : IBranchService
{
    private readonly LibraryDbContext _context;

    public BranchService(LibraryDbContext context)
    {
        _context = context;
    }

    public Task<List<Branch>> GetActiveBranchesAsync(CancellationToken cancellationToken = default)
    {
        return _context.Branches
            .AsNoTracking()
            .Where(b => b.IsActive)
            .OrderBy(b => b.Name)
            .ToListAsync(cancellationToken);
    }

    public Task<List<Branch>> GetAllBranchesAsync(CancellationToken cancellationToken = default)
    {
        return _context.Branches
            .AsNoTracking()
            .OrderBy(b => b.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<BranchInventorySummary>> GetInventorySummaryAsync(
        CancellationToken cancellationToken = default)
    {
        var branches = await GetAllBranchesAsync(cancellationToken);

        var stock = await _context.Items
            .AsNoTracking()
            .GroupBy(i => new { i.BranchId, i.Status })
            .Select(g => new { g.Key.BranchId, g.Key.Status, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var openTransfers = await _context.ItemTransfers
            .AsNoTracking()
            .Where(t => t.Status == TransferStatus.Requested || t.Status == TransferStatus.InTransit)
            .Select(t => new { t.FromBranchId, t.ToBranchId })
            .ToListAsync(cancellationToken);

        int Count(int branchId, ItemStatus status) =>
            stock.Where(s => s.BranchId == branchId && s.Status == status).Sum(s => s.Count);

        return branches.Select(b => new BranchInventorySummary
        {
            BranchId = b.Id,
            Code = b.Code,
            Name = b.Name,
            IsActive = b.IsActive,
            TotalItems = stock.Where(s => s.BranchId == b.Id).Sum(s => s.Count),
            Available = Count(b.Id, ItemStatus.Available),
            Borrowed = Count(b.Id, ItemStatus.Borrowed),
            Damaged = Count(b.Id, ItemStatus.Damaged),
            Destroyed = Count(b.Id, ItemStatus.Destroyed),
            InTransit = Count(b.Id, ItemStatus.InTransit),
            Outgoing = openTransfers.Count(t => t.FromBranchId == b.Id),
            Incoming = openTransfers.Count(t => t.ToBranchId == b.Id)
        }).ToList();
    }
}
