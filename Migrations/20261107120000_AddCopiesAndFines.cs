using System;
using Library.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Library.Migrations
{
    /// <summary>
    /// v1 -> v2: physical copies (book_copies), fines, loans.CopyId, reservations.CopyId.
    /// Existing data is converted with SQL inside this migration.
    /// </summary>
    [DbContext(typeof(AppDbContext))]
    [Migration("20261107120000_AddCopiesAndFines")]
    public partial class AddCopiesAndFines : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ---- 1. new tables / columns (new FK columns are nullable until the data is converted) ----
            migrationBuilder.CreateTable(
                name: "book_copies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BookId = table.Column<int>(type: "integer", nullable: false),
                    InventoryNumber = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Condition = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_book_copies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_book_copies_books_BookId",
                        column: x => x.BookId,
                        principalTable: "books",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.AddColumn<int>(
                name: "CopyId",
                table: "loans",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CopyId",
                table: "reservations",
                type: "integer",
                nullable: true);

            // ---- 2. data: one copy per unit of books.TotalCopies ----
            migrationBuilder.Sql(@"
INSERT INTO book_copies (""BookId"", ""InventoryNumber"", ""Status"", ""Condition"", ""CreatedAt"")
SELECT b.""Id"",
       ROW_NUMBER() OVER (ORDER BY b.""Id"", g.n),
       'AVAILABLE',
       'OK',
       now()
FROM books b
CROSS JOIN LATERAL generate_series(1, b.""TotalCopies"") AS g(n);
");

            // Books that were shrunk to 0 copies but still have loan history: keep one
            // out-of-circulation placeholder copy so the history can point to a copy.
            migrationBuilder.Sql(@"
INSERT INTO book_copies (""BookId"", ""InventoryNumber"", ""Status"", ""Condition"", ""CreatedAt"")
SELECT x.""BookId"",
       (SELECT COALESCE(MAX(""InventoryNumber""), 0) FROM book_copies) + ROW_NUMBER() OVER (ORDER BY x.""BookId""),
       'MAINTENANCE',
       'OK',
       now()
FROM (
    SELECT DISTINCT l.""BookId""
    FROM loans l
    WHERE NOT EXISTS (SELECT 1 FROM book_copies c WHERE c.""BookId"" = l.""BookId"")
) x;
");

            // ---- 3. data: spread ACTIVE loans over the copies of their book ----
            migrationBuilder.Sql(@"
WITH ranked_loans AS (
    SELECT ""Id"", ""BookId"",
           ROW_NUMBER() OVER (PARTITION BY ""BookId"" ORDER BY ""Id"") AS rn
    FROM loans
    WHERE ""Status"" = 'ACTIVE'
),
ranked_copies AS (
    SELECT ""Id"", ""BookId"",
           ROW_NUMBER() OVER (PARTITION BY ""BookId"" ORDER BY ""Id"") AS rn
    FROM book_copies
)
UPDATE loans l
SET ""CopyId"" = c.""Id""
FROM ranked_loans rl
JOIN ranked_copies c ON c.""BookId"" = rl.""BookId"" AND c.rn = rl.rn
WHERE l.""Id"" = rl.""Id"";
");

            migrationBuilder.Sql(@"
UPDATE book_copies
SET ""Status"" = 'LOANED'
WHERE ""Id"" IN (SELECT ""CopyId"" FROM loans WHERE ""CopyId"" IS NOT NULL AND ""Status"" = 'ACTIVE');
");

            // ---- 4. data: READY reservations hold one of the still free copies ----
            migrationBuilder.Sql(@"
WITH ranked_ready AS (
    SELECT ""Id"", ""BookId"",
           ROW_NUMBER() OVER (PARTITION BY ""BookId"" ORDER BY ""ExpiresAt"", ""Id"") AS rn
    FROM reservations
    WHERE ""Status"" = 'READY'
),
free_copies AS (
    SELECT ""Id"", ""BookId"",
           ROW_NUMBER() OVER (PARTITION BY ""BookId"" ORDER BY ""Id"") AS rn
    FROM book_copies
    WHERE ""Status"" = 'AVAILABLE'
)
UPDATE reservations r
SET ""CopyId"" = f.""Id""
FROM ranked_ready rr
JOIN free_copies f ON f.""BookId"" = rr.""BookId"" AND f.rn = rr.rn
WHERE r.""Id"" = rr.""Id"";
");

            migrationBuilder.Sql(@"
UPDATE book_copies
SET ""Status"" = 'RESERVED'
WHERE ""Id"" IN (SELECT ""CopyId"" FROM reservations WHERE ""CopyId"" IS NOT NULL AND ""Status"" = 'READY');
");

            // ---- 5. data: returned loans (history) point to any copy of their book ----
            migrationBuilder.Sql(@"
UPDATE loans l
SET ""CopyId"" = (SELECT MIN(c.""Id"") FROM book_copies c WHERE c.""BookId"" = l.""BookId"")
WHERE l.""CopyId"" IS NULL;
");

            // ---- 6. constraints on the converted data ----
            migrationBuilder.AlterColumn<int>(
                name: "CopyId",
                table: "loans",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "fines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LoanId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<int>(type: "integer", nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PaidAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_fines_loans_LoanId",
                        column: x => x.LoanId,
                        principalTable: "loans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_fines_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_book_copies_BookId",
                table: "book_copies",
                column: "BookId");

            migrationBuilder.CreateIndex(
                name: "IX_book_copies_InventoryNumber",
                table: "book_copies",
                column: "InventoryNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_loans_CopyId",
                table: "loans",
                column: "CopyId");

            migrationBuilder.CreateIndex(
                name: "IX_reservations_CopyId",
                table: "reservations",
                column: "CopyId");

            migrationBuilder.CreateIndex(
                name: "IX_fines_LoanId",
                table: "fines",
                column: "LoanId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fines_UserId",
                table: "fines",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_loans_book_copies_CopyId",
                table: "loans",
                column: "CopyId",
                principalTable: "book_copies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_reservations_book_copies_CopyId",
                table: "reservations",
                column: "CopyId",
                principalTable: "book_copies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // ---- 7. book_copies is now the source of truth: drop the counters ----
            migrationBuilder.DropCheckConstraint(
                name: "ck_books_available_le_total",
                table: "books");

            migrationBuilder.DropColumn(
                name: "AvailableCopies",
                table: "books");

            migrationBuilder.DropColumn(
                name: "TotalCopies",
                table: "books");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AvailableCopies",
                table: "books",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalCopies",
                table: "books",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Counters are rebuilt from the copies (lost/damaged/maintenance copies are not counted).
            migrationBuilder.Sql(@"
UPDATE books b
SET ""TotalCopies"" = (SELECT COUNT(*) FROM book_copies c
                       WHERE c.""BookId"" = b.""Id"" AND c.""Status"" IN ('AVAILABLE', 'LOANED', 'RESERVED')),
    ""AvailableCopies"" = (SELECT COUNT(*) FROM book_copies c
                           WHERE c.""BookId"" = b.""Id"" AND c.""Status"" = 'AVAILABLE');
");

            migrationBuilder.AddCheckConstraint(
                name: "ck_books_available_le_total",
                table: "books",
                sql: "\"AvailableCopies\" <= \"TotalCopies\"");

            migrationBuilder.DropForeignKey(
                name: "FK_loans_book_copies_CopyId",
                table: "loans");

            migrationBuilder.DropForeignKey(
                name: "FK_reservations_book_copies_CopyId",
                table: "reservations");

            migrationBuilder.DropTable(
                name: "fines");

            migrationBuilder.DropIndex(
                name: "IX_loans_CopyId",
                table: "loans");

            migrationBuilder.DropIndex(
                name: "IX_reservations_CopyId",
                table: "reservations");

            migrationBuilder.DropColumn(
                name: "CopyId",
                table: "loans");

            migrationBuilder.DropColumn(
                name: "CopyId",
                table: "reservations");

            migrationBuilder.DropTable(
                name: "book_copies");
        }
    }
}
