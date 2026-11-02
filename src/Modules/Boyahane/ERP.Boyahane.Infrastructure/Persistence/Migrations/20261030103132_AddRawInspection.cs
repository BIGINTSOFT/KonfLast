using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Boyahane.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRawInspection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CustomerDecisionAccepted",
                schema: "Boyahane",
                table: "FabricRolls",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerDecisionBy",
                schema: "Boyahane",
                table: "FabricRolls",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerDecisionChannel",
                schema: "Boyahane",
                table: "FabricRolls",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerDecisionNote",
                schema: "Boyahane",
                table: "FabricRolls",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "CustomerDecisionOn",
                schema: "Boyahane",
                table: "FabricRolls",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerDecisionRecordedBy",
                schema: "Boyahane",
                table: "FabricRolls",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "InspectedAt",
                schema: "Boyahane",
                table: "FabricRolls",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InspectedBy",
                schema: "Boyahane",
                table: "FabricRolls",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "InspectedKilograms",
                schema: "Boyahane",
                table: "FabricRolls",
                type: "decimal(12,2)",
                precision: 12,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "InspectedMeters",
                schema: "Boyahane",
                table: "FabricRolls",
                type: "decimal(12,2)",
                precision: 12,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "InspectedWeightGsm",
                schema: "Boyahane",
                table: "FabricRolls",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "InspectedWidthCm",
                schema: "Boyahane",
                table: "FabricRolls",
                type: "decimal(6,2)",
                precision: 6,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InspectionNote",
                schema: "Boyahane",
                table: "FabricRolls",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsConditionallyApproved",
                schema: "Boyahane",
                table: "FabricRolls",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "DefectTypes",
                schema: "Boyahane",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
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
                    table.PrimaryKey("PK_DefectTypes", x => x.Id);
                    table.CheckConstraint("CK_DefectTypes_Category", "[Category] IN ('APPEARANCE', 'STRUCTURE', 'MEASURE')");
                });

            migrationBuilder.CreateTable(
                name: "FabricRollDefects",
                schema: "Boyahane",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RollId = table.Column<int>(type: "int", nullable: false),
                    DefectTypeId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    AtMeter = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: true),
                    Note = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FabricRollDefects", x => x.Id);
                    table.CheckConstraint("CK_FabricRollDefects_Quantity", "[Quantity] >= 1");
                    table.ForeignKey(
                        name: "FK_FabricRollDefects_DefectTypes_DefectTypeId",
                        column: x => x.DefectTypeId,
                        principalSchema: "Boyahane",
                        principalTable: "DefectTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FabricRollDefects_FabricRolls_RollId",
                        column: x => x.RollId,
                        principalSchema: "Boyahane",
                        principalTable: "FabricRolls",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DefectTypes_Code",
                schema: "Boyahane",
                table: "DefectTypes",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_FabricRollDefects_DefectTypeId",
                schema: "Boyahane",
                table: "FabricRollDefects",
                column: "DefectTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_FabricRollDefects_RollId",
                schema: "Boyahane",
                table: "FabricRollDefects",
                column: "RollId");

            // Yeni yetkiler; varsayılan olarak yalnızca Sistem Yöneticisine verilir, diğer roller rol ekranından yetkilendirilir.
            migrationBuilder.Sql(@"SET IDENTITY_INSERT [Core].[Permissions] ON;
EXEC(N'INSERT INTO [Core].[Permissions] ([Id], [Code], [Module], [Description])
SELECT v.[Id], v.[Code], v.[Module], v.[Description] FROM (VALUES
(1008, N''Boyahane.Inspection.View'', N''Boyahane'', N''Ham kontrolü görüntüleme''),
(1009, N''Boyahane.Inspection.Manage'', N''Boyahane'', N''Ham kontrol yapma, hata türlerini yönetme''),
(1010, N''Boyahane.Inspection.CustomerDecision'', N''Boyahane'', N''Şartlı top için müşteri kararını girme'')
) v([Id], [Code], [Module], [Description])
WHERE NOT EXISTS (SELECT 1 FROM [Core].[Permissions] p WHERE p.[Id] = v.[Id] OR p.[Code] = v.[Code])');
SET IDENTITY_INSERT [Core].[Permissions] OFF;");

            migrationBuilder.Sql(@"INSERT INTO [Core].[RolePermissions] ([RoleId], [PermissionId])
SELECT v.[RoleId], v.[PermissionId] FROM (VALUES
(1, 1008),
(1, 1009),
(1, 1010)
) v([RoleId], [PermissionId])
WHERE EXISTS (SELECT 1 FROM [Core].[Roles] r WHERE r.[Id] = v.[RoleId])
  AND EXISTS (SELECT 1 FROM [Core].[Permissions] p WHERE p.[Id] = v.[PermissionId])
  AND NOT EXISTS (SELECT 1 FROM [Core].[RolePermissions] rp WHERE rp.[RoleId] = v.[RoleId] AND rp.[PermissionId] = v.[PermissionId]);");

            // Başlangıç hata türleri; kullanıcı Tanımlar menüsünden değiştirebilir.
            migrationBuilder.Sql(@"INSERT INTO [Boyahane].[DefectTypes] ([Code], [Name], [Category], [IsActive], [IsDeleted], [CreatedAt], [CreatedBy])
SELECT v.[Code], v.[Name], v.[Category], 1, 0, SYSUTCDATETIME(), N'system' FROM (VALUES
(N'YAG-LEKE', N'Yağ lekesi', 'APPEARANCE'),
(N'KIR-LEKE', N'Kir lekesi', 'APPEARANCE'),
(N'DELIK', N'Delik', 'APPEARANCE'),
(N'CIZGI', N'Çizgi / barre', 'APPEARANCE'),
(N'IPLIK-KOPUK', N'İplik kopuğu', 'STRUCTURE'),
(N'KACIK', N'Kaçık', 'STRUCTURE'),
(N'ORGU-HATA', N'Örgü / dokuma hatası', 'STRUCTURE'),
(N'EN-FARK', N'En farkı', 'MEASURE'),
(N'GRAMAJ-FARK', N'Gramaj farkı', 'MEASURE')
) v([Code], [Name], [Category])
WHERE NOT EXISTS (SELECT 1 FROM [Boyahane].[DefectTypes] d WHERE d.[Code] = v.[Code]);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DELETE FROM [Core].[RolePermissions] WHERE [PermissionId] IN (1008, 1009, 1010);
DELETE FROM [Core].[Permissions] WHERE [Id] IN (1008, 1009, 1010);");

            migrationBuilder.DropTable(
                name: "FabricRollDefects",
                schema: "Boyahane");

            migrationBuilder.DropTable(
                name: "DefectTypes",
                schema: "Boyahane");

            migrationBuilder.DropColumn(
                name: "CustomerDecisionAccepted",
                schema: "Boyahane",
                table: "FabricRolls");

            migrationBuilder.DropColumn(
                name: "CustomerDecisionBy",
                schema: "Boyahane",
                table: "FabricRolls");

            migrationBuilder.DropColumn(
                name: "CustomerDecisionChannel",
                schema: "Boyahane",
                table: "FabricRolls");

            migrationBuilder.DropColumn(
                name: "CustomerDecisionNote",
                schema: "Boyahane",
                table: "FabricRolls");

            migrationBuilder.DropColumn(
                name: "CustomerDecisionOn",
                schema: "Boyahane",
                table: "FabricRolls");

            migrationBuilder.DropColumn(
                name: "CustomerDecisionRecordedBy",
                schema: "Boyahane",
                table: "FabricRolls");

            migrationBuilder.DropColumn(
                name: "InspectedAt",
                schema: "Boyahane",
                table: "FabricRolls");

            migrationBuilder.DropColumn(
                name: "InspectedBy",
                schema: "Boyahane",
                table: "FabricRolls");

            migrationBuilder.DropColumn(
                name: "InspectedKilograms",
                schema: "Boyahane",
                table: "FabricRolls");

            migrationBuilder.DropColumn(
                name: "InspectedMeters",
                schema: "Boyahane",
                table: "FabricRolls");

            migrationBuilder.DropColumn(
                name: "InspectedWeightGsm",
                schema: "Boyahane",
                table: "FabricRolls");

            migrationBuilder.DropColumn(
                name: "InspectedWidthCm",
                schema: "Boyahane",
                table: "FabricRolls");

            migrationBuilder.DropColumn(
                name: "InspectionNote",
                schema: "Boyahane",
                table: "FabricRolls");

            migrationBuilder.DropColumn(
                name: "IsConditionallyApproved",
                schema: "Boyahane",
                table: "FabricRolls");
        }
    }
}
