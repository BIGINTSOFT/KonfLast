using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Konfeksiyon.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddManualReportAttribution : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "OperatorId",
                schema: "Konfeksiyon",
                table: "BundleOperationEvents",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<bool>(
                name: "CountsForPerformance",
                schema: "Konfeksiyon",
                table: "BundleOperationEvents",
                type: "bit",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CountsForPerformance",
                schema: "Konfeksiyon",
                table: "BundleOperationEvents");

            migrationBuilder.AlterColumn<int>(
                name: "OperatorId",
                schema: "Konfeksiyon",
                table: "BundleOperationEvents",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}
