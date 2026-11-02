using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Boyahane.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPurchasing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "MinStockKilograms",
                schema: "Boyahane",
                table: "DyeChemicals",
                type: "decimal(14,3)",
                precision: 14,
                scale: 3,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PurchaseOrderLineId",
                schema: "Boyahane",
                table: "ChemicalLots",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SupplierId",
                schema: "Boyahane",
                table: "ChemicalLots",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Suppliers",
                schema: "Boyahane",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TaxNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    LeadTimeDays = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Suppliers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseOrders",
                schema: "Boyahane",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderNo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    SupplierId = table.Column<int>(type: "int", nullable: false),
                    OrderDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ExpectedDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
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
                    table.CheckConstraint("CK_PurchaseOrders_Status", "[Status] IN ('DRAFT', 'CONFIRMED', 'PARTIALLY_RECEIVED', 'RECEIVED', 'CLOSED', 'CANCELLED')");
                    table.ForeignKey(
                        name: "FK_PurchaseOrders_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalSchema: "Boyahane",
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseOrderLines",
                schema: "Boyahane",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    DyeChemicalId = table.Column<int>(type: "int", nullable: false),
                    Kilograms = table.Column<decimal>(type: "decimal(14,3)", precision: 14, scale: 3, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(14,4)", precision: 14, scale: 4, nullable: true),
                    ReceivedKilograms = table.Column<decimal>(type: "decimal(14,3)", precision: 14, scale: 3, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseOrderLines", x => x.Id);
                    table.CheckConstraint("CK_PurchaseOrderLines_Kilograms", "[Kilograms] > 0");
                    table.ForeignKey(
                        name: "FK_PurchaseOrderLines_DyeChemicals_DyeChemicalId",
                        column: x => x.DyeChemicalId,
                        principalSchema: "Boyahane",
                        principalTable: "DyeChemicals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseOrderLines_PurchaseOrders_OrderId",
                        column: x => x.OrderId,
                        principalSchema: "Boyahane",
                        principalTable: "PurchaseOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChemicalLots_PurchaseOrderLineId",
                schema: "Boyahane",
                table: "ChemicalLots",
                column: "PurchaseOrderLineId");

            migrationBuilder.CreateIndex(
                name: "IX_ChemicalLots_SupplierId",
                schema: "Boyahane",
                table: "ChemicalLots",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderLines_DyeChemicalId",
                schema: "Boyahane",
                table: "PurchaseOrderLines",
                column: "DyeChemicalId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderLines_OrderId",
                schema: "Boyahane",
                table: "PurchaseOrderLines",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_OrderNo",
                schema: "Boyahane",
                table: "PurchaseOrders",
                column: "OrderNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_SupplierId",
                schema: "Boyahane",
                table: "PurchaseOrders",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_Code",
                schema: "Boyahane",
                table: "Suppliers",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.AddForeignKey(
                name: "FK_ChemicalLots_PurchaseOrderLines_PurchaseOrderLineId",
                schema: "Boyahane",
                table: "ChemicalLots",
                column: "PurchaseOrderLineId",
                principalSchema: "Boyahane",
                principalTable: "PurchaseOrderLines",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ChemicalLots_Suppliers_SupplierId",
                schema: "Boyahane",
                table: "ChemicalLots",
                column: "SupplierId",
                principalSchema: "Boyahane",
                principalTable: "Suppliers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // Satın alma yetkileri Sistem Yöneticisi ve Boyahane Yöneticisine; diğer roller rol ekranından yetkilendirilir.
            migrationBuilder.Sql(@"SET IDENTITY_INSERT [Core].[Permissions] ON;
EXEC(N'INSERT INTO [Core].[Permissions] ([Id], [Code], [Module], [Description])
SELECT v.[Id], v.[Code], v.[Module], v.[Description] FROM (VALUES
(1033, N''Boyahane.Purchase.View'', N''Boyahane'', N''Satın alma siparişlerini ve tedarikçileri görüntüleme''),
(1034, N''Boyahane.Purchase.Manage'', N''Boyahane'', N''Satın alma siparişi, teslim alma, tedarikçi kartları'')
) v([Id], [Code], [Module], [Description])
WHERE NOT EXISTS (SELECT 1 FROM [Core].[Permissions] p WHERE p.[Id] = v.[Id] OR p.[Code] = v.[Code])');
SET IDENTITY_INSERT [Core].[Permissions] OFF;");

            migrationBuilder.Sql(@"INSERT INTO [Core].[RolePermissions] ([RoleId], [PermissionId])
SELECT r.[Id], p.[Id] FROM [Core].[Roles] r
CROSS JOIN [Core].[Permissions] p
WHERE r.[Code] IN (N'ADMIN', N'BOYAHANE_MANAGER') AND p.[Id] IN (1033, 1034)
  AND NOT EXISTS (SELECT 1 FROM [Core].[RolePermissions] rp WHERE rp.[RoleId] = r.[Id] AND rp.[PermissionId] = p.[Id]);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DELETE FROM [Core].[RolePermissions] WHERE [PermissionId] IN (1033, 1034);
DELETE FROM [Core].[Permissions] WHERE [Id] IN (1033, 1034);");

            migrationBuilder.DropForeignKey(
                name: "FK_ChemicalLots_PurchaseOrderLines_PurchaseOrderLineId",
                schema: "Boyahane",
                table: "ChemicalLots");

            migrationBuilder.DropForeignKey(
                name: "FK_ChemicalLots_Suppliers_SupplierId",
                schema: "Boyahane",
                table: "ChemicalLots");

            migrationBuilder.DropTable(
                name: "PurchaseOrderLines",
                schema: "Boyahane");

            migrationBuilder.DropTable(
                name: "PurchaseOrders",
                schema: "Boyahane");

            migrationBuilder.DropTable(
                name: "Suppliers",
                schema: "Boyahane");

            migrationBuilder.DropIndex(
                name: "IX_ChemicalLots_PurchaseOrderLineId",
                schema: "Boyahane",
                table: "ChemicalLots");

            migrationBuilder.DropIndex(
                name: "IX_ChemicalLots_SupplierId",
                schema: "Boyahane",
                table: "ChemicalLots");

            migrationBuilder.DropColumn(
                name: "MinStockKilograms",
                schema: "Boyahane",
                table: "DyeChemicals");

            migrationBuilder.DropColumn(
                name: "PurchaseOrderLineId",
                schema: "Boyahane",
                table: "ChemicalLots");

            migrationBuilder.DropColumn(
                name: "SupplierId",
                schema: "Boyahane",
                table: "ChemicalLots");
        }
    }
}
