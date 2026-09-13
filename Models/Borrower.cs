namespace LibraryManagementSystem.Models;

public class Borrower
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    public List<Loan> Loans { get; set; } = new();
}
