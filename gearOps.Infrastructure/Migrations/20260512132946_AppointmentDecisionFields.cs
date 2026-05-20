using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace gearOps.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AppointmentDecisionFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE appointments
                    ADD COLUMN IF NOT EXISTS approval_notes text,
                    ADD COLUMN IF NOT EXISTS approved_at timestamp with time zone,
                    ADD COLUMN IF NOT EXISTS approved_by_staff_id integer;

                CREATE INDEX IF NOT EXISTS ix_appointments_approved_by_staff_id
                    ON appointments (approved_by_staff_id);

                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_constraint
                        WHERE conname = 'fk_appointments_users_approved_by_staff_id'
                    ) THEN
                        ALTER TABLE appointments
                            ADD CONSTRAINT fk_appointments_users_approved_by_staff_id
                            FOREIGN KEY (approved_by_staff_id)
                            REFERENCES users (user_id)
                            ON DELETE SET NULL;
                    END IF;
                END $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_appointments_users_approved_by_staff_id",
                table: "appointments");

            migrationBuilder.DropIndex(
                name: "ix_appointments_approved_by_staff_id",
                table: "appointments");

            migrationBuilder.DropColumn(
                name: "approval_notes",
                table: "appointments");

            migrationBuilder.DropColumn(
                name: "approved_at",
                table: "appointments");

            migrationBuilder.DropColumn(
                name: "approved_by_staff_id",
                table: "appointments");
        }
    }
}
