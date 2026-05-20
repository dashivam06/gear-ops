using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace gearOps.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAppointmentIdToSalesInvoice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE sales_invoices
                    ADD COLUMN IF NOT EXISTS appointment_id integer,
                    ADD COLUMN IF NOT EXISTS invoice_type text;

                CREATE INDEX IF NOT EXISTS ix_sales_invoices_appointment_id
                    ON sales_invoices (appointment_id);

                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_constraint
                        WHERE conname = 'fk_sales_invoices_appointments_appointment_id'
                    ) THEN
                        ALTER TABLE sales_invoices
                            ADD CONSTRAINT fk_sales_invoices_appointments_appointment_id
                            FOREIGN KEY (appointment_id)
                            REFERENCES appointments (appointment_id)
                            ON DELETE SET NULL;
                    END IF;
                END $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_sales_invoices_appointments_appointment_id",
                table: "sales_invoices");

            migrationBuilder.DropIndex(
                name: "ix_sales_invoices_appointment_id",
                table: "sales_invoices");

            migrationBuilder.DropColumn(
                name: "appointment_id",
                table: "sales_invoices");

            migrationBuilder.DropColumn(
                name: "invoice_type",
                table: "sales_invoices");
        }
    }
}
