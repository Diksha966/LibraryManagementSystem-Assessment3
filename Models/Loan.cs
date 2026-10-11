using System.ComponentModel.DataAnnotations;

namespace LibraryManagementSystem.Models;

public class Loan
{
    public int Id { get; set; }

    public int ItemId { get; set; }
    public Item? Item { get; set; }

    public int BorrowerId { get; set; }
    public Borrower? Borrower { get; set; }

    [DataType(DataType.Date)]
    public DateTime BorrowedDate { get; set; } = DateTime.Now;

    [Required]
    [DataType(DataType.Date)]
    public DateTime DueDate { get; set; } = DateTime.Now.AddDays(14);

    [DataType(DataType.Date)]
    public DateTime? ReturnedDate { get; set; }

    public LoanStatus Status { get; set; } = LoanStatus.Active;
}