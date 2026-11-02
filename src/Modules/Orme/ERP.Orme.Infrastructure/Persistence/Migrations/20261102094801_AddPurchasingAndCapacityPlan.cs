using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Orme.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPurchasingAndCapacityPlan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PurchaseOrderLineId",
                schema: "Orme",
                table: "YarnLots",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PlanSequence",
                schema: "Orme",
                table: "KnitOrders",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PlannedMachineId",
                schema: "Orme",
                table: "KnitOrders",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PurchaseOrders",
                schema: "Orme",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderNo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    SupplierId = table.Column<int>(type: "int", nullable: false),
                    OrderDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Currency = table.Column<string>(type: "char(3)", maxLength: 200, nullable: false, defaultValue: "TRY"),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseOrders", x => x.Id);
                    table.CheckConstraint("CK_PurchaseOrders_Status", "[Status] IN ('DRAFT', 'APPROVED', 'PARTIALLY_RECEIVED', 'RECEIVED', 'CLOSED', 'CANCELLED')");
                    table.ForeignKey(
                        name: "FK_PurchaseOrders_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalSchema: "Orme",
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseOrderLines",
                schema: "Orme",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    YarnId = table.Column<int>(type: "int", nullable: false),
                    Kilograms = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseOrderLines", x => x.Id);
                    table.CheckConstraint("CK_PurchaseOrderLines_Kilograms", "[Kilograms] > 0");
                    table.CheckConstraint("CK_PurchaseOrderLines_UnitPrice", "[UnitPrice] IS NULL OR [UnitPrice] >= 0");
                    table.ForeignKey(
                        name: "FK_PurchaseOrderLines_PurchaseOrders_OrderId",
                        column: x => x.OrderId,
                        principalSchema: "Orme",
                        principalTable: "PurchaseOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseOrderLines_Yarns_YarnId",
                        column: x => x.YarnId,
                        principalSchema: "Orme",
                        principalTable: "Yarns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_YarnLots_PurchaseOrderLineId",
                schema: "Orme",
                table: "YarnLots",
                column: "PurchaseOrderLineId",
                filter: "[PurchaseOrderLineId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_KnitOrders_PlannedMachineId_PlanSequence",
                schema: "Orme",
                table: "KnitOrders",
                columns: new[] { "PlannedMachineId", "PlanSequence" },
                filter: "[PlannedMachineId] IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_KnitOrders_PlanSequence",
                schema: "Orme",
                table: "KnitOrders",
                sql: "[PlanSequence] IS NULL OR [PlanSequence] >= 1");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderLines_OrderId",
                schema: "Orme",
                table: "PurchaseOrderLines",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderLines_YarnId",
                schema: "Orme",
                table: "PurchaseOrderLines",
                column: "YarnId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_OrderNo",
                schema: "Orme",
                table: "PurchaseOrders",
                column: "OrderNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_Status",
                schema: "Orme",
                table: "PurchaseOrders",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_SupplierId",
                schema: "Orme",
                table: "PurchaseOrders",
                column: "SupplierId");

            migrationBuilder.AddForeignKey(
                name: "FK_KnitOrders_Machines_PlannedMachineId",
                schema: "Orme",
                table: "KnitOrders",
                column: "PlannedMachineId",
                principalSchema: "Orme",
                principalTable: "Machines",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_YarnLots_PurchaseOrderLines_PurchaseOrderLineId",
                schema: "Orme",
                table: "YarnLots",
                column: "PurchaseOrderLineId",
                principalSchema: "Orme",
                principalTable: "PurchaseOrderLines",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql(@"UPDATE [Core].[Permissions] SET [Description] = N'Tedarikçileri, iplik satın alma siparişlerini ve iplik ihtiyaç planını görüntüleme' WHERE [Code] = N'Orme.Purchase.View';
UPDATE [Core].[Permissions] SET [Description] = N'Tedarikçi kartları, iplik satın alma siparişi ve teslim alma' WHERE [Code] = N'Orme.Purchase.Manage';");

            // Yeni yetkiler; varsayılan olarak yalnızca Sistem Yöneticisine verilir, diğer roller rol ekranından yetkilendirilir.
            migrationBuilder.Sql(@"SET IDENTITY_INSERT [Core].[Permissions] ON;
EXEC(N'INSERT INTO [Core].[Permissions] ([Id], [Code], [Module], [Description])
SELECT v.[Id], v.[Code], v.[Module], v.[Description] FROM (VALUES
(3030, N''Orme.Planning.View'', N''Orme'', N''Makine kapasite planını görüntüleme''),
(3031, N''Orme.Planning.Manage'', N''Orme'', N''Emirleri makine kuyruğuna alma, sırasını ve makinesini değiştirme'')
) v([Id], [Code], [Module], [Description])
WHERE NOT EXISTS (SELECT 1 FROM [Core].[Permissions] p WHERE p.[Id] = v.[Id] OR p.[Code] = v.[Code])');
SET IDENTITY_INSERT [Core].[Permissions] OFF;");

            migrationBuilder.Sql(@"INSERT INTO [Core].[RolePermissions] ([RoleId], [PermissionId])
SELECT v.[RoleId], v.[PermissionId] FROM (VALUES
(1, 3030), (1, 3031)
) v([RoleId], [PermissionId])
WHERE EXISTS (SELECT 1 FROM [Core].[Roles] r WHERE r.[Id] = v.[RoleId])
  AND EXISTS (SELECT 1 FROM [Core].[Permissions] p WHERE p.[Id] = v.[PermissionId])
  AND NOT EXISTS (SELECT 1 FROM [Core].[RolePermissions] rp WHERE rp.[RoleId] = v.[RoleId] AND rp.[PermissionId] = v.[PermissionId]);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DELETE FROM [Core].[RolePermissions] WHERE [PermissionId] IN (3030, 3031);
DELETE FROM [Core].[Permissions] WHERE [Id] IN (3030, 3031);");

            migrationBuilder.DropForeignKey(
                name: "FK_KnitOrders_Machines_PlannedMachineId",
                schema: "Orme",
                table: "KnitOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_YarnLots_PurchaseOrderLines_PurchaseOrderLineId",
                schema: "Orme",
                table: "YarnLots");

            migrationBuilder.DropTable(
                name: "PurchaseOrderLines",
                schema: "Orme");

            migrationBuilder.DropTable(
                name: "PurchaseOrders",
                schema: "Orme");

            migrationBuilder.DropIndex(
                name: "IX_YarnLots_PurchaseOrderLineId",
                schema: "Orme",
                table: "YarnLots");

            migrationBuilder.DropIndex(
                name: "IX_KnitOrders_PlannedMachineId_PlanSequence",
                schema: "Orme",
                table: "KnitOrders");

            migrationBuilder.DropCheckConstraint(
                name: "CK_KnitOrders_PlanSequence",
                schema: "Orme",
                table: "KnitOrders");

            migrationBuilder.DropColumn(
                name: "PurchaseOrderLineId",
                schema: "Orme",
                table: "YarnLots");

            migrationBuilder.DropColumn(
                name: "PlanSequence",
                schema: "Orme",
                table: "KnitOrders");

            migrationBuilder.DropColumn(
                name: "PlannedMachineId",
                schema: "Orme",
                table: "KnitOrders");
        }
    }
}
