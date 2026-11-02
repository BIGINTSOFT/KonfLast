using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Boyahane.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFinalMeasureAndBillingUnit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BillingUnit",
                schema: "Boyahane",
                table: "OrderLines",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BillingUnit",
                schema: "Boyahane",
                table: "Customers",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "ORDER_UNIT");

            migrationBuilder.AddColumn<decimal>(
                name: "FinalKilograms",
                schema: "Boyahane",
                table: "BatchItems",
                type: "decimal(12,2)",
                precision: 12,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "FinalMeters",
                schema: "Boyahane",
                table: "BatchItems",
                type: "decimal(12,2)",
                precision: 12,
                scale: 2,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BillingUnit",
                schema: "Boyahane",
                table: "OrderLines");

            migrationBuilder.DropColumn(
                name: "BillingUnit",
                schema: "Boyahane",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "FinalKilograms",
                schema: "Boyahane",
                table: "BatchItems");

            migrationBuilder.DropColumn(
                name: "FinalMeters",
                schema: "Boyahane",
                table: "BatchItems");
        }
    }
}
