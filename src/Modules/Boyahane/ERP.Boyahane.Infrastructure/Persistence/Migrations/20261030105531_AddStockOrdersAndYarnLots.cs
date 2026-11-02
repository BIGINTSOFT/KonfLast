using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Boyahane.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStockOrdersAndYarnLots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "CustomerId",
                schema: "Boyahane",
                table: "Orders",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.CreateTable(
                name: "YarnLots",
                schema: "Boyahane",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LotNo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ReceiptDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CustomerId = table.Column<int>(type: "int", nullable: true),
                    YarnId = table.Column<int>(type: "int", nullable: false),
                    DespatchNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CustomerLotNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PackageType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    PackageCount = table.Column<int>(type: "int", nullable: false),
                    Kilograms = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    OrderLineId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_YarnLots", x => x.Id);
                    table.CheckConstraint("CK_YarnLots_Kilograms", "[Kilograms] > 0");
                    table.CheckConstraint("CK_YarnLots_PackageCount", "[PackageCount] >= 1");
                    table.CheckConstraint("CK_YarnLots_PackageType", "[PackageType] IN ('BOBBIN', 'HANK')");
                    table.ForeignKey(
                        name: "FK_YarnLots_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "Boyahane",
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_YarnLots_OrderLines_OrderLineId",
                        column: x => x.OrderLineId,
                        principalSchema: "Boyahane",
                        principalTable: "OrderLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_YarnLots_Yarns_YarnId",
                        column: x => x.YarnId,
                        principalSchema: "Boyahane",
                        principalTable: "Yarns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_YarnLots_CustomerId",
                schema: "Boyahane",
                table: "YarnLots",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_YarnLots_LotNo",
                schema: "Boyahane",
                table: "YarnLots",
                column: "LotNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_YarnLots_OrderLineId",
                schema: "Boyahane",
                table: "YarnLots",
                column: "OrderLineId");

            migrationBuilder.CreateIndex(
                name: "IX_YarnLots_YarnId",
                schema: "Boyahane",
                table: "YarnLots",
                column: "YarnId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "YarnLots",
                schema: "Boyahane");

            migrationBuilder.AlterColumn<int>(
                name: "CustomerId",
                schema: "Boyahane",
                table: "Orders",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}
