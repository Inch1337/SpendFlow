using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpendFlow.Api.Migrations
{
    /// <inheritdoc />
    public partial class UseDateOnlyAndUtcTimestamps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Preserve the UTC calendar date regardless of the database session time zone.
            migrationBuilder.Sql("""
                ALTER TABLE "Expenses"
                ALTER COLUMN "Date" TYPE date
                USING ("Date" AT TIME ZONE 'UTC')::date;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Up discards the original time; rollback restores midnight UTC.
            migrationBuilder.Sql("""
                ALTER TABLE "Expenses"
                ALTER COLUMN "Date" TYPE timestamp with time zone
                USING ("Date"::timestamp AT TIME ZONE 'UTC');
                """);
        }
    }
}
