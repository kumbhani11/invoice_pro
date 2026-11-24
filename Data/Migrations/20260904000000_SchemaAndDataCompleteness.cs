using InvoicePro.Data.SQLite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvoicePro.Data.Migrations
{
    [DbContext(typeof(BillingDbContext))]
    [Migration("20260904000000_SchemaAndDataCompleteness")]
    public partial class SchemaAndDataCompleteness : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ShippingName",
                table: "Invoices",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ShippingAddress",
                table: "Invoices",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ShippingGSTIN",
                table: "Invoices",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ShippingState",
                table: "Invoices",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ShippingStateCode",
                table: "Invoices",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "TaxRate",
                table: "InvoiceItems",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            // Use raw SQL so we can use IF NOT EXISTS — safe on DBs that already have this index
            migrationBuilder.Sql(
                "CREATE UNIQUE INDEX IF NOT EXISTS IX_Invoices_InvoiceNumber ON Invoices(InvoiceNumber);");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Invoices_InvoiceNumber",
                table: "Invoices");

            migrationBuilder.DropColumn(name: "TaxRate", table: "InvoiceItems");
            migrationBuilder.DropColumn(name: "ShippingName", table: "Invoices");
            migrationBuilder.DropColumn(name: "ShippingAddress", table: "Invoices");
            migrationBuilder.DropColumn(name: "ShippingGSTIN", table: "Invoices");
            migrationBuilder.DropColumn(name: "ShippingState", table: "Invoices");
            migrationBuilder.DropColumn(name: "ShippingStateCode", table: "Invoices");
        }
    }
}
