using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Boyahane.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFinalControlsAndGoodsReturns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SaleCustomerId",
                schema: "Boyahane",
                table: "PackingLists",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FinalOverrideBy",
                schema: "Boyahane",
                table: "BatchItems",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FinalOverrideNote",
                schema: "Boyahane",
                table: "BatchItems",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "GoodsReturns",
                schema: "Boyahane",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReturnNo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CustomerId = table.Column<int>(type: "int", nullable: true),
                    ReturnDate = table.Column<DateOnly>(type: "date", nullable: false),
                    VehiclePlate = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    DriverName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DeliveryAddress = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoodsReturns", x => x.Id);
                    table.CheckConstraint("CK_GoodsReturns_Kind", "[Kind] IN ('CUSTOMER_RETURN', 'SCRAP')");
                    table.CheckConstraint("CK_GoodsReturns_Status", "[Status] IN ('DRAFT', 'CONFIRMED', 'CANCELLED')");
                    table.ForeignKey(
                        name: "FK_GoodsReturns_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "Boyahane",
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GoodsReturnItems",
                schema: "Boyahane",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReturnId = table.Column<int>(type: "int", nullable: false),
                    RollId = table.Column<int>(type: "int", nullable: true),
                    YarnLotId = table.Column<int>(type: "int", nullable: true),
                    ItemNo = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CardName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Kilograms = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    Meters = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoodsReturnItems", x => x.Id);
                    table.CheckConstraint("CK_GoodsReturnItems_Item", "([RollId] IS NOT NULL AND [YarnLotId] IS NULL) OR ([RollId] IS NULL AND [YarnLotId] IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_GoodsReturnItems_FabricRolls_RollId",
                        column: x => x.RollId,
                        principalSchema: "Boyahane",
                        principalTable: "FabricRolls",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GoodsReturnItems_GoodsReturns_ReturnId",
                        column: x => x.ReturnId,
                        principalSchema: "Boyahane",
                        principalTable: "GoodsReturns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GoodsReturnItems_YarnLots_YarnLotId",
                        column: x => x.YarnLotId,
                        principalSchema: "Boyahane",
                        principalTable: "YarnLots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PackingLists_SaleCustomerId",
                schema: "Boyahane",
                table: "PackingLists",
                column: "SaleCustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReturnItems_ReturnId",
                schema: "Boyahane",
                table: "GoodsReturnItems",
                column: "ReturnId");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReturnItems_RollId",
                schema: "Boyahane",
                table: "GoodsReturnItems",
                column: "RollId");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReturnItems_YarnLotId",
                schema: "Boyahane",
                table: "GoodsReturnItems",
                column: "YarnLotId");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReturns_CustomerId",
                schema: "Boyahane",
                table: "GoodsReturns",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReturns_ReturnNo",
                schema: "Boyahane",
                table: "GoodsReturns",
                column: "ReturnNo",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_PackingLists_Customers_SaleCustomerId",
                schema: "Boyahane",
                table: "PackingLists",
                column: "SaleCustomerId",
                principalSchema: "Boyahane",
                principalTable: "Customers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // Final kalite kararı: Mal Kabul ve Kalite ile yöneticiler. Ölçü sınırı aşımı: yöneticiler. Planlama ofisten final veremez (kullanıcı onayı 2026-10-31).
            migrationBuilder.Sql(@"SET IDENTITY_INSERT [Core].[Permissions] ON;
EXEC(N'INSERT INTO [Core].[Permissions] ([Id], [Code], [Module], [Description])
SELECT v.[Id], v.[Code], v.[Module], v.[Description] FROM (VALUES
(1037, N''Boyahane.Batch.FinalQuality'', N''Boyahane'', N''Final kalite kararını ofisten girme''),
(1038, N''Boyahane.Batch.FinalOverride'', N''Boyahane'', N''Çıkış ölçüsü sınırını gerekçeyle aşma'')
) v([Id], [Code], [Module], [Description])
WHERE NOT EXISTS (SELECT 1 FROM [Core].[Permissions] p WHERE p.[Id] = v.[Id] OR p.[Code] = v.[Code])');
SET IDENTITY_INSERT [Core].[Permissions] OFF;");

            migrationBuilder.Sql(@"INSERT INTO [Core].[RolePermissions] ([RoleId], [PermissionId])
SELECT r.[Id], p.[Id] FROM (VALUES
(N'ADMIN', 1037), (N'ADMIN', 1038),
(N'BOYAHANE_MANAGER', 1037), (N'BOYAHANE_MANAGER', 1038),
(N'BOYAHANE_RECEIVING', 1037)
) v([RoleCode], [PermissionId])
JOIN [Core].[Roles] r ON r.[Code] = v.[RoleCode]
JOIN [Core].[Permissions] p ON p.[Id] = v.[PermissionId]
WHERE NOT EXISTS (SELECT 1 FROM [Core].[RolePermissions] rp WHERE rp.[RoleId] = r.[Id] AND rp.[PermissionId] = p.[Id]);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DELETE FROM [Core].[RolePermissions] WHERE [PermissionId] IN (1037, 1038);
DELETE FROM [Core].[Permissions] WHERE [Id] IN (1037, 1038);");

            migrationBuilder.DropForeignKey(
                name: "FK_PackingLists_Customers_SaleCustomerId",
                schema: "Boyahane",
                table: "PackingLists");

            migrationBuilder.DropTable(
                name: "GoodsReturnItems",
                schema: "Boyahane");

            migrationBuilder.DropTable(
                name: "GoodsReturns",
                schema: "Boyahane");

            migrationBuilder.DropIndex(
                name: "IX_PackingLists_SaleCustomerId",
                schema: "Boyahane",
                table: "PackingLists");

            migrationBuilder.DropColumn(
                name: "SaleCustomerId",
                schema: "Boyahane",
                table: "PackingLists");

            migrationBuilder.DropColumn(
                name: "FinalOverrideBy",
                schema: "Boyahane",
                table: "BatchItems");

            migrationBuilder.DropColumn(
                name: "FinalOverrideNote",
                schema: "Boyahane",
                table: "BatchItems");
        }
    }
}
