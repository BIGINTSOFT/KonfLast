using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Boyahane.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OrderLineId",
                schema: "Boyahane",
                table: "FabricRolls",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Orders",
                schema: "Boyahane",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderNo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    OrderDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
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
                    table.CheckConstraint("CK_Orders_Ownership", "[Ownership] IN ('COMMISSION', 'OWN_GOODS')");
                    table.CheckConstraint("CK_Orders_Status", "[Status] IN ('DRAFT', 'CONFIRMED', 'CANCELLED')");
                    table.ForeignKey(
                        name: "FK_Orders_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "Boyahane",
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OrderLines",
                schema: "Boyahane",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    ProcessType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    FabricId = table.Column<int>(type: "int", nullable: true),
                    YarnId = table.Column<int>(type: "int", nullable: true),
                    ColorName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ColorCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Quantity = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(12,4)", precision: 12, scale: 4, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderLines", x => x.Id);
                    table.CheckConstraint("CK_OrderLines_Item", "([FabricId] IS NOT NULL AND [YarnId] IS NULL) OR ([FabricId] IS NULL AND [YarnId] IS NOT NULL)");
                    table.CheckConstraint("CK_OrderLines_ProcessType", "[ProcessType] IN ('FABRIC_DYEING', 'YARN_DYEING', 'FINISHING', 'PRINTING')");
                    table.CheckConstraint("CK_OrderLines_Quantity", "[Quantity] > 0");
                    table.CheckConstraint("CK_OrderLines_Unit", "[Unit] IN ('METER', 'KILOGRAM')");
                    table.ForeignKey(
                        name: "FK_OrderLines_Fabrics_FabricId",
                        column: x => x.FabricId,
                        principalSchema: "Boyahane",
                        principalTable: "Fabrics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrderLines_Orders_OrderId",
                        column: x => x.OrderId,
                        principalSchema: "Boyahane",
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrderLines_Yarns_YarnId",
                        column: x => x.YarnId,
                        principalSchema: "Boyahane",
                        principalTable: "Yarns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FabricRolls_OrderLineId",
                schema: "Boyahane",
                table: "FabricRolls",
                column: "OrderLineId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderLines_FabricId",
                schema: "Boyahane",
                table: "OrderLines",
                column: "FabricId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderLines_OrderId",
                schema: "Boyahane",
                table: "OrderLines",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderLines_YarnId",
                schema: "Boyahane",
                table: "OrderLines",
                column: "YarnId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_CustomerId",
                schema: "Boyahane",
                table: "Orders",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_OrderDate",
                schema: "Boyahane",
                table: "Orders",
                column: "OrderDate");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_OrderNo",
                schema: "Boyahane",
                table: "Orders",
                column: "OrderNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_Status",
                schema: "Boyahane",
                table: "Orders",
                column: "Status");

            migrationBuilder.AddForeignKey(
                name: "FK_FabricRolls_OrderLines_OrderLineId",
                schema: "Boyahane",
                table: "FabricRolls",
                column: "OrderLineId",
                principalSchema: "Boyahane",
                principalTable: "OrderLines",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // Yeni yetkiler; varsayılan olarak yalnızca Sistem Yöneticisine verilir, diğer roller rol ekranından yetkilendirilir.
            migrationBuilder.Sql(@"SET IDENTITY_INSERT [Core].[Permissions] ON;
EXEC(N'INSERT INTO [Core].[Permissions] ([Id], [Code], [Module], [Description])
SELECT v.[Id], v.[Code], v.[Module], v.[Description] FROM (VALUES
(1011, N''Boyahane.Order.View'', N''Boyahane'', N''Boyahane siparişlerini görüntüleme''),
(1012, N''Boyahane.Order.Manage'', N''Boyahane'', N''Boyahane siparişlerini oluşturma, onaylama ve iptal etme'')
) v([Id], [Code], [Module], [Description])
WHERE NOT EXISTS (SELECT 1 FROM [Core].[Permissions] p WHERE p.[Id] = v.[Id] OR p.[Code] = v.[Code])');
SET IDENTITY_INSERT [Core].[Permissions] OFF;");

            migrationBuilder.Sql(@"INSERT INTO [Core].[RolePermissions] ([RoleId], [PermissionId])
SELECT v.[RoleId], v.[PermissionId] FROM (VALUES
(1, 1011),
(1, 1012)
) v([RoleId], [PermissionId])
WHERE EXISTS (SELECT 1 FROM [Core].[Roles] r WHERE r.[Id] = v.[RoleId])
  AND EXISTS (SELECT 1 FROM [Core].[Permissions] p WHERE p.[Id] = v.[PermissionId])
  AND NOT EXISTS (SELECT 1 FROM [Core].[RolePermissions] rp WHERE rp.[RoleId] = v.[RoleId] AND rp.[PermissionId] = v.[PermissionId]);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DELETE FROM [Core].[RolePermissions] WHERE [PermissionId] IN (1011, 1012);
DELETE FROM [Core].[Permissions] WHERE [Id] IN (1011, 1012);");

            migrationBuilder.DropForeignKey(
                name: "FK_FabricRolls_OrderLines_OrderLineId",
                schema: "Boyahane",
                table: "FabricRolls");

            migrationBuilder.DropTable(
                name: "OrderLines",
                schema: "Boyahane");

            migrationBuilder.DropTable(
                name: "Orders",
                schema: "Boyahane");

            migrationBuilder.DropIndex(
                name: "IX_FabricRolls_OrderLineId",
                schema: "Boyahane",
                table: "FabricRolls");

            migrationBuilder.DropColumn(
                name: "OrderLineId",
                schema: "Boyahane",
                table: "FabricRolls");
        }
    }
}
