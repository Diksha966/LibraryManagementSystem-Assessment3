namespace LibraryManagementSystem.Models;

public class Loan
{
    public int Id { get; set; }

    public int ItemId { get; set; }
    public Item? Item { get; set; }

    public int BorrowerId { get; set; }
    public Borrower? Borrower { get; set; }

    public DateTime BorrowedDate { get; set; } = DateTime.Now;
    public DateTime? ReturnedDate { get; set; }

    public LoanStatus Status { get; set; } = LoanStatus.Active;
}
