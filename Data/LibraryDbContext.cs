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

        // Person 1: multi-branch inventory and inter-branch transfers
        public DbSet<Branch> Branches { get; set; }
        public DbSet<ItemTransfer> ItemTransfers { get; set; }

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

            // ---------- Person 1: branches and transfers ----------
            modelBuilder.Entity<Branch>(entity =>
            {
                entity.HasIndex(b => b.Code)
                    .IsUnique();
            });

            modelBuilder.Entity<Item>(entity =>
            {
                // Existing rows (and any code that does not set a branch)
                // are placed in branch 1, the Central Library.
                entity.Property(i => i.BranchId)
                    .HasDefaultValue(1);

                entity.HasOne(i => i.Branch)
                    .WithMany(b => b.Items)
                    .HasForeignKey(i => i.BranchId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<ItemTransfer>(entity =>
            {
                entity.HasOne(t => t.Item)
                    .WithMany()
                    .HasForeignKey(t => t.ItemId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(t => t.FromBranch)
                    .WithMany()
                    .HasForeignKey(t => t.FromBranchId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(t => t.ToBranch)
                    .WithMany()
                    .HasForeignKey(t => t.ToBranchId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(t => new { t.ItemId, t.Status });
            });

            modelBuilder.Entity<Branch>().HasData(
                new Branch
                {
                    Id = 1,
                    Code = "CEN",
                    Name = "Central Library",
                    Address = "1 Library Square",
                    IsActive = true
                },
                new Branch
                {
                    Id = 2,
                    Code = "NTH",
                    Name = "Northside Branch",
                    Address = "25 North Road",
                    IsActive = true
                }
            );

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
                    Status = ItemStatus.Borrowed,
                    BranchId = 1
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
                    Status = ItemStatus.Available,
                    BranchId = 1
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
                    Status = ItemStatus.Available,
                    BranchId = 2
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