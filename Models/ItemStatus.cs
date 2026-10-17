using System.ComponentModel.DataAnnotations;

namespace LibraryManagementSystem.Models;

public enum ItemStatus
{
    Available,
    Borrowed,
    Damaged,
    Destroyed,

    // Person 1: set automatically when a transfer is dispatched and cleared
    // when it is received or cancelled. Added at the end so the stored
    // integer values of the existing statuses do not change.
    [Display(Name = "In transit")]
    InTransit
}
