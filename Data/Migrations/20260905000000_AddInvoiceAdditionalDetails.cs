using System;
using InvoicePro.Data.SQLite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvoicePro.Data.Migrations
{
    [DbContext(typeof(BillingDbContext))]
    [Migration("20260905000000_AddInvoiceAdditionalDetails")]
    public partial class AddInvoiceAdditionalDetails : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReverseCharge", table: "Invoices", type: "TEXT", nullable: false, defaultValue: "No");

            migrationBuilder.AddColumn<string>(
                name: "TransportName", table: "Invoices", type: "TEXT", nullable: false, defaultValue: "Mahendra");

            migrationBuilder.AddColumn<DateTime>(
                name: "DateOfSupply", table: "Invoices", type: "TEXT", nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<string>(
                name: "PlaceOfSupply", table: "Invoices", type: "TEXT", nullable: false, defaultValue: "Mumbai");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "ReverseCharge", table: "Invoices");
            migrationBuilder.DropColumn(name: "TransportName", table: "Invoices");
            migrationBuilder.DropColumn(name: "DateOfSupply", table: "Invoices");
            migrationBuilder.DropColumn(name: "PlaceOfSupply", table: "Invoices");
        }
    }
}
