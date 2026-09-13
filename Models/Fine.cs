namespace LibraryManagementSystem.Models;

public class Fine
{
    public int Id { get; set; }

    public int LoanId { get; set; }
    public Loan? Loan { get; set; }

    public decimal Amount { get; set; }
    public bool IsPaid { get; set; }
}
