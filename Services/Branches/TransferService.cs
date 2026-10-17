using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using LibraryManagementSystem.Services.Notifications;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Services.Branches;

public class TransferService : ITransferService
{
    private readonly LibraryDbContext _context;
    private readonly INotificationService _notificationService;
    private readonly ReservationService _reservations;

    public TransferService(
        LibraryDbContext context,
        INotificationService notificationService,
        ReservationService reservations)
    {
        _context = context;
        _notificationService = notificationService;
        _reservations = reservations;
    }

    public async Task<TransferResult> RequestAsync(
        int itemId,
        int toBranchId,
        string? reason,
        string user,
        CancellationToken cancellationToken = default)
    {
        var item = await _context.Items
            .FirstOrDefaultAsync(i => i.Id == itemId, cancellationToken);

        if (item == null)
            return TransferResult.Fail("That item could not be found.");

        // Rule: only an Available item can be transferred.
        if (item.Status != ItemStatus.Available)
        {
            return TransferResult.Fail(
                $"\"{item.Title}\" is {DescribeStatus(item.Status)} and cannot be transferred. " +
                "Only Available items can be transferred.");
        }

        // Rule: one open transfer per item.
        var openTransfer = await GetOpenTransferAsync(itemId, cancellationToken);

        if (openTransfer != null)
        {
            return TransferResult.Fail(
                $"\"{item.Title}\" already has an open transfer (#{openTransfer.Id}). " +
                "Receive or cancel it first.",
                openTransfer.Id);
        }

        var toBranch = await _context.Branches
            .FirstOrDefaultAsync(b => b.Id == toBranchId, cancellationToken);

        if (toBranch == null)
            return TransferResult.Fail("Please choose a destination branch.");

        if (!toBranch.IsActive)
            return TransferResult.Fail($"{toBranch.Name} is not active and cannot receive items.");

        if (toBranch.Id == item.BranchId)
            return TransferResult.Fail($"\"{item.Title}\" is already at {toBranch.Name}.");

        // Integration point (Person 3, reservations): when the Reservations
        // table is merged, refuse the request here if the item is being held
        // for the first patron in its reservation queue.

        var fromBranch = await _context.Branches
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == item.BranchId, cancellationToken);

        var transfer = new ItemTransfer
        {
            ItemId = item.Id,
            FromBranchId = item.BranchId,
            ToBranchId = toBranch.Id,
            Status = TransferStatus.Requested,
            Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim(),
            RequestedBy = user,
            RequestedAt = DateTime.Now
        };

        _context.ItemTransfers.Add(transfer);
        await _context.SaveChangesAsync(cancellationToken);

        await _notificationService.LogAsync(
            category: "Transfers",
            severity: "Info",
            title: "Transfer Requested",
            message: $"{item.Title} ({item.LibraryCode}) requested from {fromBranch?.Name} to {toBranch.Name} by {user}.",
            eventKey: $"transfer-requested-{transfer.Id}",
            entityType: "ItemTransfer",
            entityId: transfer.Id,
            branchId: transfer.FromBranchId,
            cancellationToken: cancellationToken);

        return TransferResult.Ok(
            $"Transfer #{transfer.Id} requested: \"{item.Title}\" from {fromBranch?.Name} to {toBranch.Name}.",
            transfer.Id);
    }

    public async Task<TransferResult> DispatchAsync(
        int transferId,
        string user,
        CancellationToken cancellationToken = default)
    {
        var transfer = await LoadAsync(transferId, cancellationToken);

        if (transfer?.Item == null)
            return TransferResult.Fail("That transfer could not be found.");

        if (transfer.Status != TransferStatus.Requested)
        {
            return TransferResult.Fail(
                $"Only a Requested transfer can be dispatched. This transfer is {transfer.Status}.",
                transfer.Id);
        }

        // The item may have changed since the request (for example marked Damaged).
        if (transfer.Item.Status != ItemStatus.Available)
        {
            return TransferResult.Fail(
                $"\"{transfer.Item.Title}\" is now {DescribeStatus(transfer.Item.Status)}, so it cannot be dispatched. " +
                "Cancel this transfer or fix the item first.",
                transfer.Id);
        }

        transfer.Status = TransferStatus.InTransit;
        transfer.DispatchedBy = user;
        transfer.DispatchedAt = DateTime.Now;

        // Locks the item: every borrow path already refuses items that are not Available.
        transfer.Item.Status = ItemStatus.InTransit;

        await _context.SaveChangesAsync(cancellationToken);

        await _notificationService.LogAsync(
            category: "Transfers",
            severity: "Info",
            title: "Transfer Dispatched",
            message: $"{transfer.Item.Title} ({transfer.Item.LibraryCode}) left {transfer.FromBranch?.Name} for {transfer.ToBranch?.Name}.",
            eventKey: $"transfer-dispatched-{transfer.Id}",
            entityType: "ItemTransfer",
            entityId: transfer.Id,
            branchId: transfer.FromBranchId,
            cancellationToken: cancellationToken);

        return TransferResult.Ok(
            $"Transfer #{transfer.Id} dispatched. \"{transfer.Item.Title}\" is now in transit to {transfer.ToBranch?.Name}.",
            transfer.Id);
    }

    public async Task<TransferResult> ReceiveAsync(
        int transferId,
        string user,
        CancellationToken cancellationToken = default)
    {
        var transfer = await LoadAsync(transferId, cancellationToken);

        if (transfer?.Item == null)
            return TransferResult.Fail("That transfer could not be found.");

        if (transfer.Status != TransferStatus.InTransit)
        {
            return TransferResult.Fail(
                $"Only an In Transit transfer can be received. This transfer is {transfer.Status}.",
                transfer.Id);
        }

        transfer.Status = TransferStatus.Received;
        transfer.ReceivedBy = user;
        transfer.ReceivedAt = DateTime.Now;

        // The only place an item's branch changes.
        transfer.Item.BranchId = transfer.ToBranchId;
        transfer.Item.Status = ItemStatus.Available;

        await _context.SaveChangesAsync(cancellationToken);

        await _notificationService.LogAsync(
            category: "Transfers",
            severity: "Info",
            title: "Transfer Received",
            message: $"{transfer.Item.Title} ({transfer.Item.LibraryCode}) received at {transfer.ToBranch?.Name}.",
            eventKey: $"transfer-received-{transfer.Id}",
            entityType: "ItemTransfer",
            entityId: transfer.Id,
            branchId: transfer.ToBranchId,
            cancellationToken: cancellationToken);

        // Person 3 (reservations): the item is Available at its new branch,
        // so the first person in its reservation queue is notified.
        await _reservations.NotifyNextAsync(transfer.ItemId);

        return TransferResult.Ok(
            $"Transfer #{transfer.Id} received. \"{transfer.Item.Title}\" is now Available at {transfer.ToBranch?.Name}.",
            transfer.Id);
    }

    public async Task<TransferResult> CancelAsync(
        int transferId,
        string? reason,
        string user,
        CancellationToken cancellationToken = default)
    {
        var transfer = await LoadAsync(transferId, cancellationToken);

        if (transfer?.Item == null)
            return TransferResult.Fail("That transfer could not be found.");

        if (transfer.Status != TransferStatus.Requested &&
            transfer.Status != TransferStatus.InTransit)
        {
            return TransferResult.Fail(
                $"This transfer is already {transfer.Status} and cannot be cancelled.",
                transfer.Id);
        }

        bool wasInTransit = transfer.Status == TransferStatus.InTransit;

        transfer.Status = TransferStatus.Cancelled;
        transfer.CancelledBy = user;
        transfer.CancelledAt = DateTime.Now;
        transfer.CancelReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();

        // An item that had already left goes back to the source branch.
        // Its BranchId never changed, so only the status is restored.
        if (wasInTransit && transfer.Item.Status == ItemStatus.InTransit)
        {
            transfer.Item.Status = ItemStatus.Available;
        }

        await _context.SaveChangesAsync(cancellationToken);

        await _notificationService.LogAsync(
            category: "Transfers",
            severity: "Warning",
            title: "Transfer Cancelled",
            message: $"Transfer #{transfer.Id} of {transfer.Item.Title} ({transfer.Item.LibraryCode}) was cancelled by {user}" +
                     (transfer.CancelReason == null ? "." : $": {transfer.CancelReason}"),
            eventKey: $"transfer-cancelled-{transfer.Id}",
            entityType: "ItemTransfer",
            entityId: transfer.Id,
            branchId: transfer.FromBranchId,
            cancellationToken: cancellationToken);

        // Person 3 (reservations): a cancelled in-transit item is Available again.
        if (wasInTransit && transfer.Item.Status == ItemStatus.Available)
        {
            await _reservations.NotifyNextAsync(transfer.ItemId);
        }

        return TransferResult.Ok(
            wasInTransit
                ? $"Transfer #{transfer.Id} cancelled. \"{transfer.Item.Title}\" returns to {transfer.FromBranch?.Name} and is Available again."
                : $"Transfer #{transfer.Id} cancelled. \"{transfer.Item.Title}\" stays at {transfer.FromBranch?.Name}.",
            transfer.Id);
    }

    public Task<bool> HasOpenTransferAsync(
        int itemId,
        CancellationToken cancellationToken = default)
    {
        return _context.ItemTransfers.AnyAsync(t =>
            t.ItemId == itemId &&
            (t.Status == TransferStatus.Requested || t.Status == TransferStatus.InTransit),
            cancellationToken);
    }

    public Task<ItemTransfer?> GetOpenTransferAsync(
        int itemId,
        CancellationToken cancellationToken = default)
    {
        return _context.ItemTransfers
            .Include(t => t.ToBranch)
            .FirstOrDefaultAsync(t =>
                t.ItemId == itemId &&
                (t.Status == TransferStatus.Requested || t.Status == TransferStatus.InTransit),
                cancellationToken);
    }

    private Task<ItemTransfer?> LoadAsync(int transferId, CancellationToken cancellationToken)
    {
        return _context.ItemTransfers
            .Include(t => t.Item)
            .Include(t => t.FromBranch)
            .Include(t => t.ToBranch)
            .FirstOrDefaultAsync(t => t.Id == transferId, cancellationToken);
    }

    private static string DescribeStatus(ItemStatus status) => status switch
    {
        ItemStatus.Borrowed => "on loan",
        ItemStatus.Damaged => "marked Damaged",
        ItemStatus.Destroyed => "marked Destroyed",
        ItemStatus.InTransit => "already in transit",
        _ => status.ToString()
    };
}
