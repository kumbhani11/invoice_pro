using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvoicePro.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailAndTaxPercentage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Companies",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "TaxPercentage",
                table: "Products",
                type: "TEXT",
                nullable: false,
                defaultValue: 18m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "Email",         table: "Companies");
            migrationBuilder.DropColumn(name: "TaxPercentage", table: "Products");
        }
    }
}
