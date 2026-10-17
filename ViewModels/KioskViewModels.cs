using System.ComponentModel.DataAnnotations;
using LibraryManagementSystem.Models;
using LibraryManagementSystem.Services;

namespace LibraryManagementSystem.ViewModels;

public class KioskSignInViewModel
{
    [Required(ErrorMessage = "Please enter your member number.")]
    [Display(Name = "Member number")]
    public string? MemberNumber { get; set; }

    [Required(ErrorMessage = "Please enter your email address.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    [Display(Name = "Email address")]
    public string? Email { get; set; }

    public string? Next { get; set; }
}

public class KioskCheckoutViewModel
{
    public string BorrowerName { get; set; } = string.Empty;
    public string? Message { get; set; }
    public bool Success { get; set; }
    public string? ItemTitle { get; set; }
    public DateTime? DueDate { get; set; }
}

public class KioskAccountViewModel
{
    public Borrower Borrower { get; set; } = new();
    public List<Loan> Loans { get; set; } = new();
    public List<Fine> UnpaidFines { get; set; } = new();
    public List<ReservationInfo> Reservations { get; set; } = new();
}

public class ReserveViewModel
{
    public int ItemId { get; set; }
    public string ItemTitle { get; set; } = string.Empty;
    public string ItemCode { get; set; } = string.Empty;
    public ItemStatus ItemStatus { get; set; }
    public int QueueLength { get; set; }

    [Required(ErrorMessage = "Please enter your member number.")]
    [Display(Name = "Member number")]
    public string? MemberNumber { get; set; }

    [Required(ErrorMessage = "Please enter your email address.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    [Display(Name = "Email address")]
    public string? Email { get; set; }

    // Result of a submitted request
    public bool Submitted { get; set; }
    public bool Success { get; set; }
    public string? Message { get; set; }
    public int Position { get; set; }
}
