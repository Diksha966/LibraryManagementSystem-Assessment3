using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace LibraryManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class SeedInitialData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Borrowers",
                columns: new[] { "Id", "Email", "Name" },
                values: new object[,]
                {
                    { 1, "john.smith@example.com", "John Smith" },
                    { 2, "jane.doe@example.com", "Jane Doe" }
                });

            migrationBuilder.InsertData(
                table: "Items",
                columns: new[] { "Id", "Author", "Discriminator", "ISBN", "Status", "Title" },
                values: new object[] { 1, "J.R.R. Tolkien", "Book", "9780547928227", 0, "The Hobbit" });

            migrationBuilder.InsertData(
                table: "Items",
                columns: new[] { "Id", "Discriminator", "RecommendedAge", "Status", "Title" },
                values: new object[] { 2, "Toy", "8", 0, "LEGO Classic Set" });

            migrationBuilder.InsertData(
                table: "Items",
                columns: new[] { "Id", "Artist", "Discriminator", "Format", "Status", "Title" },
                values: new object[] { 3, "Michael Jackson", "Music", "CD", 0, "Thriller" });

            migrationBuilder.InsertData(
                table: "Loans",
                columns: new[] { "Id", "BorrowedDate", "BorrowerId", "ItemId", "ReturnedDate", "Status" },
                values: new object[] { 1, new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 1, 1, null, 0 });

            migrationBuilder.InsertData(
                table: "Fines",
                columns: new[] { "Id", "Amount", "IsPaid", "LoanId" },
                values: new object[] { 1, 5.00m, false, 1 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Borrowers",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Fines",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Items",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Items",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Loans",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Borrowers",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Items",
                keyColumn: "Id",
                keyValue: 1);
        }
    }
}
