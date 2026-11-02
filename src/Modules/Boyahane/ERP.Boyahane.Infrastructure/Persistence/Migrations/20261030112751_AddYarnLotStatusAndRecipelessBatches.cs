using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Boyahane.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddYarnLotStatusAndRecipelessBatches : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RejectionNote",
                schema: "Boyahane",
                table: "YarnLots",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                schema: "Boyahane",
                table: "YarnLots",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "APPROVED");

            migrationBuilder.AlterColumn<int>(
                name: "RecipeTrialId",
                schema: "Boyahane",
                table: "Batches",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<decimal>(
                name: "LiquorRatio",
                schema: "Boyahane",
                table: "Batches",
                type: "decimal(6,2)",
                precision: 6,
                scale: 2,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(6,2)",
                oldPrecision: 6,
                oldScale: 2);

            migrationBuilder.AddCheckConstraint(
                name: "CK_YarnLots_Status",
                schema: "Boyahane",
                table: "YarnLots",
                sql: "[Status] IN ('APPROVED', 'REJECTED')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_YarnLots_Status",
                schema: "Boyahane",
                table: "YarnLots");

            migrationBuilder.DropColumn(
                name: "RejectionNote",
                schema: "Boyahane",
                table: "YarnLots");

            migrationBuilder.DropColumn(
                name: "Status",
                schema: "Boyahane",
                table: "YarnLots");

            migrationBuilder.AlterColumn<int>(
                name: "RecipeTrialId",
                schema: "Boyahane",
                table: "Batches",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "LiquorRatio",
                schema: "Boyahane",
                table: "Batches",
                type: "decimal(6,2)",
                precision: 6,
                scale: 2,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(6,2)",
                oldPrecision: 6,
                oldScale: 2,
                oldNullable: true);
        }
    }
}
