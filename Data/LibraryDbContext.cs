using LibraryManagementSystem.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Data
{
    public class LibraryDbContext : IdentityDbContext
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
        public DbSet<NotificationLog> NotificationLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<NotificationLog>(entity =>
            {
                entity.Property(n => n.Category)
                    .HasMaxLength(100);

                entity.Property(n => n.Severity)
                    .HasMaxLength(30);

                entity.Property(n => n.Title)
                    .HasMaxLength(200);

                entity.Property(n => n.EntityType)
                    .HasMaxLength(100);

                entity.Property(n => n.EventKey)
                    .HasMaxLength(120);

                entity.HasIndex(n => n.EventKey)
                    .IsUnique();
            });

            // Seed Book
            modelBuilder.Entity<Book>().HasData(
                new Book
                {
                    Id = 1,
                    Title = "The Hobbit",
                    Description = "A fantasy adventure novel about Bilbo Baggins.",
                    LibraryCode = "BK001",
                    Author = "J.R.R. Tolkien",
                    Genre = "Fantasy",
                    ISBN = "9780547928227",
                    Status = ItemStatus.Borrowed
                }
            );

            // Seed Toy
            modelBuilder.Entity<Toy>().HasData(
                new Toy
                {
                    Id = 2,
                    Title = "LEGO Classic Set",
                    Description = "A creative building set for children.",
                    LibraryCode = "TY001",
                    ToyType = "Building",
                    RecommendedAge = 8,
                    Status = ItemStatus.Available
                }
            );

            // Seed Music
            modelBuilder.Entity<Music>().HasData(
                new Music
                {
                    Id = 3,
                    Title = "Thriller",
                    Description = "The classic album by Michael Jackson.",
                    LibraryCode = "MU001",
                    Artist = "Michael Jackson",
                    Year = 1982,
                    Format = "CD",
                    Status = ItemStatus.Available
                }
            );

            // Seed Borrowers
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

            // Seed Loan
            modelBuilder.Entity<Loan>().HasData(
               new Loan
{
    Id = 1,
    ItemId = 1,
    BorrowerId = 1,
    BorrowedDate = new DateTime(2026, 9, 1),
    DueDate = new DateTime(2026, 9, 15),
    Status = LoanStatus.Active
}
            );

            
        }
    }
}