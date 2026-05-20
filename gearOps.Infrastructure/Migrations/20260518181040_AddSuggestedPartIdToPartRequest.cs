using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace gearOps.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSuggestedPartIdToPartRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "suggested_part_id",
                table: "part_requests",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "scheduled_notifications",
                columns: table => new
                {
                    notification_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    notification_type = table.Column<string>(type: "text", nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    schedule_time = table.Column<string>(type: "text", nullable: true),
                    low_stock_threshold = table.Column<int>(type: "integer", nullable: false),
                    overdue_days = table.Column<int>(type: "integer", nullable: false),
                    admin_email = table.Column<string>(type: "text", nullable: true),
                    last_run_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_run_status = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_scheduled_notifications", x => x.notification_id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_part_requests_suggested_part_id",
                table: "part_requests",
                column: "suggested_part_id");

            migrationBuilder.AddForeignKey(
                name: "fk_part_requests_parts_suggested_part_id",
                table: "part_requests",
                column: "suggested_part_id",
                principalTable: "parts",
                principalColumn: "part_id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_part_requests_parts_suggested_part_id",
                table: "part_requests");

            migrationBuilder.DropTable(
                name: "scheduled_notifications");

            migrationBuilder.DropIndex(
                name: "ix_part_requests_suggested_part_id",
                table: "part_requests");

            migrationBuilder.DropColumn(
                name: "suggested_part_id",
                table: "part_requests");
        }
    }
}
