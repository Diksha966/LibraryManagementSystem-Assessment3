using System.ComponentModel.DataAnnotations;

namespace LibraryManagementSystem.Models;

// Person 1: lifecycle of an inter-branch transfer.
// Requested -> InTransit -> Received, or Requested/InTransit -> Cancelled.
public enum TransferStatus
{
    Requested,

    [Display(Name = "In transit")]
    InTransit,

    Received,

    Cancelled
}
