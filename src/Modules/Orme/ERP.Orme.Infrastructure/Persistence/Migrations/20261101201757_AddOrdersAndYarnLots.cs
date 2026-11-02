using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Orme.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOrdersAndYarnLots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Orders",
                schema: "Orme",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderNo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    OrderDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CustomerId = table.Column<int>(type: "int", nullable: true),
                    CustomerOrderNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Ownership = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: true),
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
                    table.PrimaryKey("PK_Orders", x => x.Id);
                    table.CheckConstraint("CK_Orders_CommissionCustomer", "[Ownership] <> 'COMMISSION' OR [CustomerId] IS NOT NULL");
                    table.CheckConstraint("CK_Orders_Ownership", "[Ownership] IN ('COMMISSION', 'OWN_GOODS')");
                    table.CheckConstraint("CK_Orders_Status", "[Status] IN ('DRAFT', 'CONFIRMED', 'CANCELLED')");
                    table.ForeignKey(
                        name: "FK_Orders_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "Orme",
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "YarnLots",
                schema: "Orme",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LotNo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ReceiptDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CustomerId = table.Column<int>(type: "int", nullable: true),
                    SupplierId = table.Column<int>(type: "int", nullable: true),
                    YarnId = table.Column<int>(type: "int", nullable: false),
                    DespatchNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ProducerLotNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PackageCount = table.Column<int>(type: "int", nullable: false),
                    Kilograms = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
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
                    table.ForeignKey(
                        name: "FK_YarnLots_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "Orme",
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_YarnLots_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalSchema: "Orme",
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_YarnLots_Yarns_YarnId",
                        column: x => x.YarnId,
                        principalSchema: "Orme",
                        principalTable: "Yarns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OrderLines",
                schema: "Orme",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    FabricId = table.Column<int>(type: "int", nullable: false),
                    Kilograms = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderLines", x => x.Id);
                    table.CheckConstraint("CK_OrderLines_Kilograms", "[Kilograms] > 0");
                    table.CheckConstraint("CK_OrderLines_UnitPrice", "[UnitPrice] IS NULL OR [UnitPrice] >= 0");
                    table.ForeignKey(
                        name: "FK_OrderLines_Fabrics_FabricId",
                        column: x => x.FabricId,
                        principalSchema: "Orme",
                        principalTable: "Fabrics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrderLines_Orders_OrderId",
                        column: x => x.OrderId,
                        principalSchema: "Orme",
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrderLines_FabricId",
                schema: "Orme",
                table: "OrderLines",
                column: "FabricId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderLines_OrderId",
                schema: "Orme",
                table: "OrderLines",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_CustomerId",
                schema: "Orme",
                table: "Orders",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_OrderDate",
                schema: "Orme",
                table: "Orders",
                column: "OrderDate");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_OrderNo",
                schema: "Orme",
                table: "Orders",
                column: "OrderNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_YarnLots_CustomerId",
                schema: "Orme",
                table: "YarnLots",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_YarnLots_LotNo",
                schema: "Orme",
                table: "YarnLots",
                column: "LotNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_YarnLots_SupplierId",
                schema: "Orme",
                table: "YarnLots",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_YarnLots_YarnId_CustomerId",
                schema: "Orme",
                table: "YarnLots",
                columns: new[] { "YarnId", "CustomerId" });

            // Yeni yetkiler; varsayılan olarak yalnızca Sistem Yöneticisine verilir, diğer roller rol ekranından yetkilendirilir.
            migrationBuilder.Sql(@"SET IDENTITY_INSERT [Core].[Permissions] ON;
EXEC(N'INSERT INTO [Core].[Permissions] ([Id], [Code], [Module], [Description])
SELECT v.[Id], v.[Code], v.[Module], v.[Description] FROM (VALUES
(3010, N''Orme.Order.View'', N''Orme'', N''Örme siparişlerini ve iplik ihtiyacını görüntüleme''),
(3011, N''Orme.Order.Manage'', N''Orme'', N''Örme siparişlerini oluşturma, onaylama ve iptal etme''),
(3012, N''Orme.Receipt.View'', N''Orme'', N''İplik girişlerini görüntüleme''),
(3013, N''Orme.Receipt.Manage'', N''Orme'', N''İplik girişi yapma, düzenleme ve silme'')
) v([Id], [Code], [Module], [Description])
WHERE NOT EXISTS (SELECT 1 FROM [Core].[Permissions] p WHERE p.[Id] = v.[Id] OR p.[Code] = v.[Code])');
SET IDENTITY_INSERT [Core].[Permissions] OFF;");

            migrationBuilder.Sql(@"INSERT INTO [Core].[RolePermissions] ([RoleId], [PermissionId])
SELECT v.[RoleId], v.[PermissionId] FROM (VALUES
(1, 3010), (1, 3011), (1, 3012), (1, 3013)
) v([RoleId], [PermissionId])
WHERE EXISTS (SELECT 1 FROM [Core].[Roles] r WHERE r.[Id] = v.[RoleId])
  AND EXISTS (SELECT 1 FROM [Core].[Permissions] p WHERE p.[Id] = v.[PermissionId])
  AND NOT EXISTS (SELECT 1 FROM [Core].[RolePermissions] rp WHERE rp.[RoleId] = v.[RoleId] AND rp.[PermissionId] = v.[PermissionId]);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DELETE FROM [Core].[RolePermissions] WHERE [PermissionId] BETWEEN 3010 AND 3013;
DELETE FROM [Core].[Permissions] WHERE [Id] BETWEEN 3010 AND 3013;");

            migrationBuilder.DropTable(
                name: "OrderLines",
                schema: "Orme");

            migrationBuilder.DropTable(
                name: "YarnLots",
                schema: "Orme");

            migrationBuilder.DropTable(
                name: "Orders",
                schema: "Orme");
        }
    }
}
