using LibraryManagementSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Data
{
    public class LibraryDbContext : DbContext
    {
        public LibraryDbContext(DbContextOptions<LibraryDbContext> options)
            : base(options)
        {
        }

        public DbSet<Item> Items { get; set; }
        public DbSet<Book> Books { get; set; }
        public DbSet<Toy> Toys { get; set; }
        public DbSet<Music> Music { get; set; }
        public DbSet<Borrower> Borrowers { get; set; }
        public DbSet<Loan> Loans { get; set; }
        public DbSet<Fine> Fines { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Book>().HasData(
    new Book
    {
        Id = 1,
        Title = "The Hobbit",
        Author = "J.R.R. Tolkien",
        ISBN = "9780547928227",
        Status = ItemStatus.Available
    }
);

modelBuilder.Entity<Toy>().HasData(
    new Toy
    {
        Id = 2,
        Title = "LEGO Classic Set",
        RecommendedAge = "8",
        Status = ItemStatus.Available
    }
);

modelBuilder.Entity<Music>().HasData(
    new Music
    {
        Id = 3,
        Title = "Thriller",
        Artist = "Michael Jackson",
        Format = "CD",
        Status = ItemStatus.Available
    }
);

modelBuilder.Entity<Borrower>().HasData(
    new Borrower
    {
        Id = 1,
        Name = "John Smith",
        Email = "john.smith@example.com"
    },
    new Borrower
    {
        Id = 2,
        Name = "Jane Doe",
        Email = "jane.doe@example.com"
    }
);

modelBuilder.Entity<Loan>().HasData(
    new Loan
    {
        Id = 1,
        ItemId = 1,
        BorrowerId = 1,
        BorrowedDate = new DateTime(2026, 9, 1),
        Status = LoanStatus.Active
    }
);

modelBuilder.Entity<Fine>().HasData(
    new Fine
    {
        Id = 1,
        LoanId = 1,
        Amount = 5.00m,
        IsPaid = false
    }
);

        }
    }
}
