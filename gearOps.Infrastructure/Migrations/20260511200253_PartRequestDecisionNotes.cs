using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace gearOps.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PartRequestDecisionNotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE part_requests
                    ADD COLUMN IF NOT EXISTS decision_note text,
                    ADD COLUMN IF NOT EXISTS reviewed_at timestamp with time zone,
                    ADD COLUMN IF NOT EXISTS reviewed_by_staff_id integer;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "decision_note",
                table: "part_requests");

            migrationBuilder.DropColumn(
                name: "reviewed_at",
                table: "part_requests");

            migrationBuilder.DropColumn(
                name: "reviewed_by_staff_id",
                table: "part_requests");
        }
    }
}
