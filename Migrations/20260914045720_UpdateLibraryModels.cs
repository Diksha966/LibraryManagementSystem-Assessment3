using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LibraryManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class UpdateLibraryModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Fines",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.AddColumn<DateTime>(
                name: "DueDate",
                table: "Loans",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(2026, 9, 15));

            migrationBuilder.AlterColumn<int>(
                name: "RecommendedAge",
                table: "Items",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Items",
                type: "TEXT",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Genre",
                table: "Items",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LibraryCode",
                table: "Items",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ToyType",
                table: "Items",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Year",
                table: "Items",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Phone",
                table: "Borrowers",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "Borrowers",
                keyColumn: "Id",
                keyValue: 1,
                column: "Phone",
                value: "");

            migrationBuilder.UpdateData(
                table: "Borrowers",
                keyColumn: "Id",
                keyValue: 2,
                column: "Phone",
                value: "");

            migrationBuilder.UpdateData(
                table: "Items",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Description", "Genre", "LibraryCode", "Status" },
                values: new object[] { "A fantasy adventure novel about Bilbo Baggins.", "Fantasy", "BK001", 1 });

            migrationBuilder.UpdateData(
                table: "Items",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Description", "LibraryCode", "RecommendedAge", "ToyType" },
                values: new object[] { "A creative building set for children.", "TY001", 8, "Building" });

            migrationBuilder.UpdateData(
                table: "Items",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "Description", "LibraryCode", "Year" },
                values: new object[] { "The classic album by Michael Jackson.", "MU001", 1982 });

            migrationBuilder.UpdateData(
                table: "Loans",
                keyColumn: "Id",
                keyValue: 1,
                column: "DueDate",
                value: new DateTime(2026, 9, 15, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DueDate",
                table: "Loans");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "Genre",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "LibraryCode",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "ToyType",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "Year",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "Phone",
                table: "Borrowers");

            migrationBuilder.AlterColumn<string>(
                name: "RecommendedAge",
                table: "Items",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.InsertData(
                table: "Fines",
                columns: new[] { "Id", "Amount", "IsPaid", "LoanId" },
                values: new object[] { 1, 5.00m, false, 1 });

            migrationBuilder.UpdateData(
                table: "Items",
                keyColumn: "Id",
                keyValue: 1,
                column: "Status",
                value: 0);

            migrationBuilder.UpdateData(
                table: "Items",
                keyColumn: "Id",
                keyValue: 2,
                column: "RecommendedAge",
                value: "8");
        }
    }
}
