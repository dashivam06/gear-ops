using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using gearOps.Infrastructure.Data;

#nullable disable

namespace gearOps.Infrastructure.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AppDbContext))]
    [Migration("20260513133000_AddSoftDeleteFields")]
    public partial class AddSoftDeleteFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP INDEX IF EXISTS ix_users_email;
                DROP INDEX IF EXISTS ix_users_phone;

                ALTER TABLE vendors
                    ADD COLUMN IF NOT EXISTS deleted_at timestamp with time zone,
                    ADD COLUMN IF NOT EXISTS is_deleted boolean NOT NULL DEFAULT false;

                ALTER TABLE vehicles
                    ADD COLUMN IF NOT EXISTS deleted_at timestamp with time zone,
                    ADD COLUMN IF NOT EXISTS is_deleted boolean NOT NULL DEFAULT false;

                ALTER TABLE users
                    ADD COLUMN IF NOT EXISTS deleted_at timestamp with time zone,
                    ADD COLUMN IF NOT EXISTS is_deleted boolean NOT NULL DEFAULT false;

                ALTER TABLE reviews
                    ADD COLUMN IF NOT EXISTS deleted_at timestamp with time zone,
                    ADD COLUMN IF NOT EXISTS is_deleted boolean NOT NULL DEFAULT false;

                ALTER TABLE parts
                    ADD COLUMN IF NOT EXISTS deleted_at timestamp with time zone,
                    ADD COLUMN IF NOT EXISTS is_deleted boolean NOT NULL DEFAULT false;

                CREATE UNIQUE INDEX IF NOT EXISTS ix_users_email
                    ON users (email)
                    WHERE is_deleted = false;

                CREATE UNIQUE INDEX IF NOT EXISTS ix_users_phone
                    ON users (phone)
                    WHERE is_deleted = false;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_users_email",
                table: "users");

            migrationBuilder.DropIndex(
                name: "ix_users_phone",
                table: "users");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "vendors");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "vendors");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "vehicles");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "vehicles");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "users");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "users");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "reviews");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "reviews");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "parts");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "parts");

            migrationBuilder.CreateIndex(
                name: "ix_users_email",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_phone",
                table: "users",
                column: "phone",
                unique: true);
        }
    }
}
