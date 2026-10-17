using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Services;

/// <summary>
/// Identifies a patron (Borrower) from details typed on a public screen
/// (kiosk or public reservation page). Member number = Borrower.Id and the
/// email address on file acts as a second check.
/// </summary>
public class PatronService
{
    private readonly LibraryDbContext _context;

    public PatronService(LibraryDbContext context)
    {
        _context = context;
    }

    public async Task<Borrower?> VerifyAsync(string? memberNumber, string? email)
    {
        if (!int.TryParse(memberNumber?.Trim(), out int id) ||
            string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        var borrower = await _context.Borrowers
            .FirstOrDefaultAsync(b => b.Id == id);

        if (borrower == null)
            return null;

        return string.Equals(
            borrower.Email.Trim(),
            email.Trim(),
            StringComparison.OrdinalIgnoreCase)
            ? borrower
            : null;
    }
}
