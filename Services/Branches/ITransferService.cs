using LibraryManagementSystem.Models;

namespace LibraryManagementSystem.Services.Branches;

public record TransferResult(bool Success, string Message, int? TransferId = null)
{
    public static TransferResult Ok(string message, int? transferId = null) =>
        new(true, message, transferId);

    public static TransferResult Fail(string message, int? transferId = null) =>
        new(false, message, transferId);
}

// Person 1: all business rules for inter-branch transfers live here so that
// every screen (and any future API or kiosk code) applies the same rules.
public interface ITransferService
{
    // Staff at the item's current branch ask for it to be moved.
    Task<TransferResult> RequestAsync(
        int itemId,
        int toBranchId,
        string? reason,
        string user,
        CancellationToken cancellationToken = default);

    // The item leaves the source branch: transfer and item become In Transit.
    Task<TransferResult> DispatchAsync(
        int transferId,
        string user,
        CancellationToken cancellationToken = default);

    // The destination branch confirms arrival: the item changes branch
    // and becomes Available there.
    Task<TransferResult> ReceiveAsync(
        int transferId,
        string user,
        CancellationToken cancellationToken = default);

    // Stop a Requested or In Transit transfer. The item stays at (or returns
    // to) the source branch and the record is kept for the history.
    Task<TransferResult> CancelAsync(
        int transferId,
        string? reason,
        string user,
        CancellationToken cancellationToken = default);

    Task<bool> HasOpenTransferAsync(
        int itemId,
        CancellationToken cancellationToken = default);

    Task<ItemTransfer?> GetOpenTransferAsync(
        int itemId,
        CancellationToken cancellationToken = default);
}
