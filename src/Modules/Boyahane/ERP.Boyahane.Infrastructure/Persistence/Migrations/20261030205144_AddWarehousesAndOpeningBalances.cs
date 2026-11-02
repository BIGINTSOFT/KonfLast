using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Boyahane.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWarehousesAndOpeningBalances : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Payments_Method",
                schema: "Boyahane",
                table: "Payments");

            migrationBuilder.AddColumn<int>(
                name: "LocationId",
                schema: "Boyahane",
                table: "YarnLots",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LocationId",
                schema: "Boyahane",
                table: "PackingItems",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "OpeningAmount",
                schema: "Boyahane",
                table: "Invoices",
                type: "decimal(16,2)",
                precision: 16,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LocationId",
                schema: "Boyahane",
                table: "FabricRolls",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsOpening",
                schema: "Boyahane",
                table: "FabricReceipts",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "LocationId",
                schema: "Boyahane",
                table: "ChemicalLots",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Warehouses",
                schema: "Boyahane",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
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
                    table.PrimaryKey("PK_Warehouses", x => x.Id);
                    table.CheckConstraint("CK_Warehouses_Type", "[Type] IN ('RAW_FABRIC', 'CHEMICAL', 'FINISHED_GOODS', 'QUARANTINE')");
                });

            migrationBuilder.CreateTable(
                name: "WarehouseLocations",
                schema: "Boyahane",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WarehouseId = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Zone = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Rack = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Bin = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_WarehouseLocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WarehouseLocations_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalSchema: "Boyahane",
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LocationMovements",
                schema: "Boyahane",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GoodsType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    GoodsId = table.Column<int>(type: "int", nullable: false),
                    GoodsNo = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    FromLocationId = table.Column<int>(type: "int", nullable: true),
                    ToLocationId = table.Column<int>(type: "int", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocationMovements", x => x.Id);
                    table.CheckConstraint("CK_LocationMovements_GoodsType", "[GoodsType] IN ('FABRIC_ROLL', 'YARN_LOT', 'CHEMICAL_LOT', 'PACKAGE')");
                    table.CheckConstraint("CK_LocationMovements_Type", "[Type] IN ('ADDRESS_CHANGE', 'TRANSFER')");
                    table.ForeignKey(
                        name: "FK_LocationMovements_WarehouseLocations_FromLocationId",
                        column: x => x.FromLocationId,
                        principalSchema: "Boyahane",
                        principalTable: "WarehouseLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LocationMovements_WarehouseLocations_ToLocationId",
                        column: x => x.ToLocationId,
                        principalSchema: "Boyahane",
                        principalTable: "WarehouseLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_YarnLots_LocationId",
                schema: "Boyahane",
                table: "YarnLots",
                column: "LocationId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Payments_Method",
                schema: "Boyahane",
                table: "Payments",
                sql: "[Method] IN ('CASH', 'BANK_TRANSFER', 'CHECK', 'OPENING')");

            migrationBuilder.CreateIndex(
                name: "IX_PackingItems_LocationId",
                schema: "Boyahane",
                table: "PackingItems",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_FabricRolls_LocationId",
                schema: "Boyahane",
                table: "FabricRolls",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_ChemicalLots_LocationId",
                schema: "Boyahane",
                table: "ChemicalLots",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_LocationMovements_FromLocationId",
                schema: "Boyahane",
                table: "LocationMovements",
                column: "FromLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_LocationMovements_GoodsNo",
                schema: "Boyahane",
                table: "LocationMovements",
                column: "GoodsNo");

            migrationBuilder.CreateIndex(
                name: "IX_LocationMovements_ToLocationId",
                schema: "Boyahane",
                table: "LocationMovements",
                column: "ToLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseLocations_WarehouseId_Code",
                schema: "Boyahane",
                table: "WarehouseLocations",
                columns: new[] { "WarehouseId", "Code" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Warehouses_Code",
                schema: "Boyahane",
                table: "Warehouses",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.AddForeignKey(
                name: "FK_ChemicalLots_WarehouseLocations_LocationId",
                schema: "Boyahane",
                table: "ChemicalLots",
                column: "LocationId",
                principalSchema: "Boyahane",
                principalTable: "WarehouseLocations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FabricRolls_WarehouseLocations_LocationId",
                schema: "Boyahane",
                table: "FabricRolls",
                column: "LocationId",
                principalSchema: "Boyahane",
                principalTable: "WarehouseLocations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PackingItems_WarehouseLocations_LocationId",
                schema: "Boyahane",
                table: "PackingItems",
                column: "LocationId",
                principalSchema: "Boyahane",
                principalTable: "WarehouseLocations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_YarnLots_WarehouseLocations_LocationId",
                schema: "Boyahane",
                table: "YarnLots",
                column: "LocationId",
                principalSchema: "Boyahane",
                principalTable: "WarehouseLocations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // Başlangıç depoları ve genel adresleri; mevcut mal türüne göre genel adrese yazılır.
            migrationBuilder.Sql(@"EXEC(N'
INSERT INTO [Boyahane].[Warehouses] ([Code], [Name], [Type], [IsActive], [IsDeleted], [CreatedAt]) VALUES
(N''HAM'', N''Ham Kumaş ve İplik Deposu'', N''RAW_FABRIC'', 1, 0, SYSUTCDATETIME()),
(N''KIMYA'', N''Boya ve Kimyasal Deposu'', N''CHEMICAL'', 1, 0, SYSUTCDATETIME()),
(N''MAMUL'', N''Mamul Deposu'', N''FINISHED_GOODS'', 1, 0, SYSUTCDATETIME()),
(N''KARANTINA'', N''Karantina ve Red Deposu'', N''QUARANTINE'', 1, 0, SYSUTCDATETIME());
INSERT INTO [Boyahane].[WarehouseLocations] ([WarehouseId], [Code], [Zone], [Description], [IsDefault], [IsActive], [IsDeleted], [CreatedAt])
SELECT w.[Id], N''GENEL'', N''GENEL'', N''Adres seçilmeyen mal'', 1, 1, 0, SYSUTCDATETIME() FROM [Boyahane].[Warehouses] w;
DECLARE @ham int = (SELECT l.[Id] FROM [Boyahane].[WarehouseLocations] l JOIN [Boyahane].[Warehouses] w ON w.[Id] = l.[WarehouseId] WHERE w.[Code] = N''HAM'');
DECLARE @kimya int = (SELECT l.[Id] FROM [Boyahane].[WarehouseLocations] l JOIN [Boyahane].[Warehouses] w ON w.[Id] = l.[WarehouseId] WHERE w.[Code] = N''KIMYA'');
DECLARE @mamul int = (SELECT l.[Id] FROM [Boyahane].[WarehouseLocations] l JOIN [Boyahane].[Warehouses] w ON w.[Id] = l.[WarehouseId] WHERE w.[Code] = N''MAMUL'');
UPDATE [Boyahane].[FabricRolls] SET [LocationId] = @ham;
UPDATE [Boyahane].[YarnLots] SET [LocationId] = @ham;
UPDATE [Boyahane].[ChemicalLots] SET [LocationId] = @kimya;
UPDATE i SET [LocationId] = @mamul FROM [Boyahane].[PackingItems] i JOIN [Boyahane].[PackingLists] p ON p.[Id] = i.[PackingListId] WHERE i.[IsActive] = 1 AND p.[Status] = N''CONFIRMED'';
');");

            // Depo yetkileri artık adresleri de kapsıyor.
            migrationBuilder.Sql(@"UPDATE [Core].[Permissions] SET [Description] = N'Depoyu, adresleri ve boya-kimyasal stoğunu görüntüleme' WHERE [Id] = 1031;
UPDATE [Core].[Permissions] SET [Description] = N'Depo ve adres tanımları, mal taşıma, boya-kimyasal girişi ve sayım düzeltmesi' WHERE [Id] = 1032;");

            // Kullanıcı onayı (2026-10-30): cari yetkileri Satış rolüne.
            migrationBuilder.Sql(@"INSERT INTO [Core].[RolePermissions] ([RoleId], [PermissionId])
SELECT r.[Id], p.[Id] FROM [Core].[Roles] r
CROSS JOIN [Core].[Permissions] p
WHERE r.[Code] = N'BOYAHANE_SALES' AND p.[Id] IN (1035, 1036)
  AND NOT EXISTS (SELECT 1 FROM [Core].[RolePermissions] rp WHERE rp.[RoleId] = r.[Id] AND rp.[PermissionId] = p.[Id]);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DELETE rp FROM [Core].[RolePermissions] rp JOIN [Core].[Roles] r ON r.[Id] = rp.[RoleId]
WHERE r.[Code] = N'BOYAHANE_SALES' AND rp.[PermissionId] IN (1035, 1036);");

            migrationBuilder.DropForeignKey(
                name: "FK_ChemicalLots_WarehouseLocations_LocationId",
                schema: "Boyahane",
                table: "ChemicalLots");

            migrationBuilder.DropForeignKey(
                name: "FK_FabricRolls_WarehouseLocations_LocationId",
                schema: "Boyahane",
                table: "FabricRolls");

            migrationBuilder.DropForeignKey(
                name: "FK_PackingItems_WarehouseLocations_LocationId",
                schema: "Boyahane",
                table: "PackingItems");

            migrationBuilder.DropForeignKey(
                name: "FK_YarnLots_WarehouseLocations_LocationId",
                schema: "Boyahane",
                table: "YarnLots");

            migrationBuilder.DropTable(
                name: "LocationMovements",
                schema: "Boyahane");

            migrationBuilder.DropTable(
                name: "WarehouseLocations",
                schema: "Boyahane");

            migrationBuilder.DropTable(
                name: "Warehouses",
                schema: "Boyahane");

            migrationBuilder.DropIndex(
                name: "IX_YarnLots_LocationId",
                schema: "Boyahane",
                table: "YarnLots");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Payments_Method",
                schema: "Boyahane",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_PackingItems_LocationId",
                schema: "Boyahane",
                table: "PackingItems");

            migrationBuilder.DropIndex(
                name: "IX_FabricRolls_LocationId",
                schema: "Boyahane",
                table: "FabricRolls");

            migrationBuilder.DropIndex(
                name: "IX_ChemicalLots_LocationId",
                schema: "Boyahane",
                table: "ChemicalLots");

            migrationBuilder.DropColumn(
                name: "LocationId",
                schema: "Boyahane",
                table: "YarnLots");

            migrationBuilder.DropColumn(
                name: "LocationId",
                schema: "Boyahane",
                table: "PackingItems");

            migrationBuilder.DropColumn(
                name: "OpeningAmount",
                schema: "Boyahane",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "LocationId",
                schema: "Boyahane",
                table: "FabricRolls");

            migrationBuilder.DropColumn(
                name: "IsOpening",
                schema: "Boyahane",
                table: "FabricReceipts");

            migrationBuilder.DropColumn(
                name: "LocationId",
                schema: "Boyahane",
                table: "ChemicalLots");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Payments_Method",
                schema: "Boyahane",
                table: "Payments",
                sql: "[Method] IN ('CASH', 'BANK_TRANSFER', 'CHECK')");
        }
    }
}
