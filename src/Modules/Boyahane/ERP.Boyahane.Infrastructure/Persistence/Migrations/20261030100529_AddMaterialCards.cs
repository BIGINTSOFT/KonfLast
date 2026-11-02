using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Boyahane.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMaterialCards : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Fabrics",
                schema: "Boyahane",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FabricCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    FabricType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Structure = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Composition = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    RawWidthCm = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: false),
                    RawWeightGsm = table.Column<int>(type: "int", nullable: false),
                    FinishedWidthCm = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: true),
                    FinishedWeightGsm = table.Column<int>(type: "int", nullable: true),
                    ShrinkageTolerancePercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    DefaultUnit = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
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
                    table.PrimaryKey("PK_Fabrics", x => x.Id);
                    table.CheckConstraint("CK_Fabrics_DefaultUnit", "[DefaultUnit] IN ('METER', 'KILOGRAM')");
                    table.CheckConstraint("CK_Fabrics_FabricType", "[FabricType] IN ('KNIT', 'WOVEN')");
                    table.CheckConstraint("CK_Fabrics_RawWeightGsm", "[RawWeightGsm] > 0");
                    table.CheckConstraint("CK_Fabrics_RawWidthCm", "[RawWidthCm] > 0");
                    table.CheckConstraint("CK_Fabrics_ShrinkageTolerancePercent", "[ShrinkageTolerancePercent] IS NULL OR [ShrinkageTolerancePercent] BETWEEN 0 AND 100");
                });

            migrationBuilder.CreateTable(
                name: "Yarns",
                schema: "Boyahane",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    YarnCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CountValue = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: false),
                    CountSystem = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Plies = table.Column<int>(type: "int", nullable: false),
                    Composition = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Twist = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
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
                    table.PrimaryKey("PK_Yarns", x => x.Id);
                    table.CheckConstraint("CK_Yarns_CountSystem", "[CountSystem] IN ('NE', 'NM', 'DTEX')");
                    table.CheckConstraint("CK_Yarns_CountValue", "[CountValue] > 0");
                    table.CheckConstraint("CK_Yarns_Plies", "[Plies] >= 1");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Fabrics_FabricCode",
                schema: "Boyahane",
                table: "Fabrics",
                column: "FabricCode",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Yarns_YarnCode",
                schema: "Boyahane",
                table: "Yarns",
                column: "YarnCode",
                unique: true,
                filter: "[IsDeleted] = 0");

            // Yeni yetkiler; varsayılan olarak yalnızca Sistem Yöneticisine verilir, diğer roller rol ekranından yetkilendirilir.
            migrationBuilder.Sql(@"SET IDENTITY_INSERT [Core].[Permissions] ON;
EXEC(N'INSERT INTO [Core].[Permissions] ([Id], [Code], [Module], [Description])
SELECT v.[Id], v.[Code], v.[Module], v.[Description] FROM (VALUES
(1004, N''Boyahane.Material.View'', N''Boyahane'', N''Kumaş ve iplik kartlarını görüntüleme''),
(1005, N''Boyahane.Material.Manage'', N''Boyahane'', N''Kumaş ve iplik kartlarını oluşturma ve düzenleme'')
) v([Id], [Code], [Module], [Description])
WHERE NOT EXISTS (SELECT 1 FROM [Core].[Permissions] p WHERE p.[Id] = v.[Id] OR p.[Code] = v.[Code])');
SET IDENTITY_INSERT [Core].[Permissions] OFF;");

            migrationBuilder.Sql(@"INSERT INTO [Core].[RolePermissions] ([RoleId], [PermissionId])
SELECT v.[RoleId], v.[PermissionId] FROM (VALUES
(1, 1004),
(1, 1005)
) v([RoleId], [PermissionId])
WHERE EXISTS (SELECT 1 FROM [Core].[Roles] r WHERE r.[Id] = v.[RoleId])
  AND EXISTS (SELECT 1 FROM [Core].[Permissions] p WHERE p.[Id] = v.[PermissionId])
  AND NOT EXISTS (SELECT 1 FROM [Core].[RolePermissions] rp WHERE rp.[RoleId] = v.[RoleId] AND rp.[PermissionId] = v.[PermissionId]);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DELETE FROM [Core].[RolePermissions] WHERE [PermissionId] IN (1004, 1005);
DELETE FROM [Core].[Permissions] WHERE [Id] IN (1004, 1005);");

            migrationBuilder.DropTable(
                name: "Fabrics",
                schema: "Boyahane");

            migrationBuilder.DropTable(
                name: "Yarns",
                schema: "Boyahane");
        }
    }
}
