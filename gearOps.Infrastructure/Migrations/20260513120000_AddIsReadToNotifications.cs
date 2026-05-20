using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace gearOps.Infrastructure.Migrations
{
    /// <inheritdoc />
    [Migration("20260513120000_AddIsReadToNotifications")]
    public partial class AddIsReadToNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE notifications
                    ADD COLUMN IF NOT EXISTS is_read boolean NOT NULL DEFAULT false;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_read",
                table: "notifications");
        }
    }
}
