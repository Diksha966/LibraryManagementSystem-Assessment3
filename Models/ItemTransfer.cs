using System.ComponentModel.DataAnnotations;

namespace LibraryManagementSystem.Models;

// Person 1: one movement of one item from a source branch to a destination
// branch. Records are never deleted, so the full history of every move is kept.
public class ItemTransfer
{
    public int Id { get; set; }

    public int ItemId { get; set; }
    public Item? Item { get; set; }

    [Display(Name = "From branch")]
    public int FromBranchId { get; set; }
    public Branch? FromBranch { get; set; }

    [Display(Name = "To branch")]
    public int ToBranchId { get; set; }
    public Branch? ToBranch { get; set; }

    public TransferStatus Status { get; set; } = TransferStatus.Requested;

    [StringLength(200)]
    public string? Reason { get; set; }

    [StringLength(256)]
    [Display(Name = "Requested by")]
    public string RequestedBy { get; set; } = string.Empty;

    [Display(Name = "Requested")]
    public DateTime RequestedAt { get; set; } = DateTime.Now;

    [StringLength(256)]
    public string? DispatchedBy { get; set; }
    public DateTime? DispatchedAt { get; set; }

    [StringLength(256)]
    public string? ReceivedBy { get; set; }
    public DateTime? ReceivedAt { get; set; }

    [StringLength(256)]
    public string? CancelledBy { get; set; }
    public DateTime? CancelledAt { get; set; }

    [StringLength(200)]
    public string? CancelReason { get; set; }

    public bool IsOpen =>
        Status == TransferStatus.Requested ||
        Status == TransferStatus.InTransit;
}
