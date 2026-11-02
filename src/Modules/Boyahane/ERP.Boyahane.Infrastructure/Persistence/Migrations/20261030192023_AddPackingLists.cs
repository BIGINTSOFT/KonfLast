using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Boyahane.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPackingLists : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PackingLists",
                schema: "Boyahane",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PackingNo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    PackingDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PackingLists", x => x.Id);
                    table.CheckConstraint("CK_PackingLists_Status", "[Status] IN ('DRAFT', 'CONFIRMED', 'CANCELLED')");
                    table.ForeignKey(
                        name: "FK_PackingLists_Orders_OrderId",
                        column: x => x.OrderId,
                        principalSchema: "Boyahane",
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PackingItems",
                schema: "Boyahane",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PackingListId = table.Column<int>(type: "int", nullable: false),
                    PackageNo = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    BatchItemId = table.Column<int>(type: "int", nullable: false),
                    Kilograms = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    Meters = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: true),
                    BoxCount = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PackingItems", x => x.Id);
                    table.CheckConstraint("CK_PackingItems_BoxCount", "[BoxCount] >= 1");
                    table.CheckConstraint("CK_PackingItems_Kilograms", "[Kilograms] > 0");
                    table.ForeignKey(
                        name: "FK_PackingItems_BatchItems_BatchItemId",
                        column: x => x.BatchItemId,
                        principalSchema: "Boyahane",
                        principalTable: "BatchItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PackingItems_PackingLists_PackingListId",
                        column: x => x.PackingListId,
                        principalSchema: "Boyahane",
                        principalTable: "PackingLists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PackingItems_PackageNo",
                schema: "Boyahane",
                table: "PackingItems",
                column: "PackageNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PackingItems_PackingListId",
                schema: "Boyahane",
                table: "PackingItems",
                column: "PackingListId");

            migrationBuilder.CreateIndex(
                name: "UX_PackingItems_Active",
                schema: "Boyahane",
                table: "PackingItems",
                column: "BatchItemId",
                unique: true,
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_PackingLists_OrderId",
                schema: "Boyahane",
                table: "PackingLists",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_PackingLists_PackingNo",
                schema: "Boyahane",
                table: "PackingLists",
                column: "PackingNo",
                unique: true);

            // Yeni yetkiler Sistem Yöneticisi ve Boyahane Yöneticisine verilir; diğer roller rol ekranından yetkilendirilir.
            migrationBuilder.Sql(@"SET IDENTITY_INSERT [Core].[Permissions] ON;
EXEC(N'INSERT INTO [Core].[Permissions] ([Id], [Code], [Module], [Description])
SELECT v.[Id], v.[Code], v.[Module], v.[Description] FROM (VALUES
(1025, N''Boyahane.Packing.View'', N''Boyahane'', N''Çeki listelerini görüntüleme''),
(1026, N''Boyahane.Packing.Manage'', N''Boyahane'', N''Çeki listesi hazırlama ve onaylama'')
) v([Id], [Code], [Module], [Description])
WHERE NOT EXISTS (SELECT 1 FROM [Core].[Permissions] p WHERE p.[Id] = v.[Id] OR p.[Code] = v.[Code])');
SET IDENTITY_INSERT [Core].[Permissions] OFF;");

            migrationBuilder.Sql(@"INSERT INTO [Core].[RolePermissions] ([RoleId], [PermissionId])
SELECT r.[Id], p.[Id] FROM [Core].[Roles] r
CROSS JOIN [Core].[Permissions] p
WHERE r.[Code] IN (N'ADMIN', N'BOYAHANE_MANAGER') AND p.[Id] IN (1025, 1026)
  AND NOT EXISTS (SELECT 1 FROM [Core].[RolePermissions] rp WHERE rp.[RoleId] = r.[Id] AND rp.[PermissionId] = p.[Id]);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DELETE FROM [Core].[RolePermissions] WHERE [PermissionId] IN (1025, 1026);
DELETE FROM [Core].[Permissions] WHERE [Id] IN (1025, 1026);");

            migrationBuilder.DropTable(
                name: "PackingItems",
                schema: "Boyahane");

            migrationBuilder.DropTable(
                name: "PackingLists",
                schema: "Boyahane");
        }
    }
}
