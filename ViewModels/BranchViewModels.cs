using System.ComponentModel.DataAnnotations;

namespace LibraryManagementSystem.Models;

// Person 1: stock held by one branch, used on the Branches page and the
// Manager dashboard.
public class BranchInventorySummary
{
    public int BranchId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }

    public int TotalItems { get; set; }
    public int Available { get; set; }
    public int Borrowed { get; set; }
    public int Damaged { get; set; }
    public int Destroyed { get; set; }
    public int InTransit { get; set; }

    // Open transfers (Requested or In Transit)
    public int Outgoing { get; set; }
    public int Incoming { get; set; }
}

// Person 1: form used to request a transfer.
public class TransferCreateViewModel
{
    [Required(ErrorMessage = "Please enter the item's library code.")]
    [StringLength(20)]
    [Display(Name = "Library code")]
    public string LibraryCode { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Please choose a destination branch.")]
    [Display(Name = "Destination branch")]
    public int ToBranchId { get; set; }

    [StringLength(200)]
    [Display(Name = "Reason (optional)")]
    public string? Reason { get; set; }
}
