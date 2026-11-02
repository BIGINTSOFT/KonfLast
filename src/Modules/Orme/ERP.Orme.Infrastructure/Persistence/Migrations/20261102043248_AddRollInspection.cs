using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Orme.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRollInspection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CalculatedStatus",
                schema: "Orme",
                table: "FabricRolls",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "InspectedMeters",
                schema: "Orme",
                table: "FabricRolls",
                type: "decimal(12,2)",
                precision: 12,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "InspectedMetersEstimated",
                schema: "Orme",
                table: "FabricRolls",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateOnly>(
                name: "InspectedOn",
                schema: "Orme",
                table: "FabricRolls",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "InspectedWeightGsm",
                schema: "Orme",
                table: "FabricRolls",
                type: "decimal(7,2)",
                precision: 7,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "InspectedWidthCm",
                schema: "Orme",
                table: "FabricRolls",
                type: "decimal(6,2)",
                precision: 6,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InspectionNotes",
                schema: "Orme",
                table: "FabricRolls",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OverrideNote",
                schema: "Orme",
                table: "FabricRolls",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PointsPer100M2",
                schema: "Orme",
                table: "FabricRolls",
                type: "decimal(8,2)",
                precision: 8,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SpecDeviation",
                schema: "Orme",
                table: "FabricRolls",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TotalPoints",
                schema: "Orme",
                table: "FabricRolls",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DefectTypes",
                schema: "Orme",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AlwaysFourPoints = table.Column<bool>(type: "bit", nullable: false),
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
                });

            migrationBuilder.CreateTable(
                name: "QualitySettings",
                schema: "Orme",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FirstQualityMaxPoints = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: false),
                    SecondQualityMaxPoints = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: false),
                    WidthTolerancePercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    WeightTolerancePercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QualitySettings", x => x.Id);
                    table.CheckConstraint("CK_QualitySettings_Limits", "[FirstQualityMaxPoints] >= 0 AND [SecondQualityMaxPoints] >= [FirstQualityMaxPoints]");
                    table.CheckConstraint("CK_QualitySettings_Tolerances", "[WidthTolerancePercent] BETWEEN 0 AND 100 AND [WeightTolerancePercent] BETWEEN 0 AND 100");
                });

            migrationBuilder.CreateTable(
                name: "RollDefects",
                schema: "Orme",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RollId = table.Column<int>(type: "int", nullable: false),
                    DefectTypeId = table.Column<int>(type: "int", nullable: false),
                    AtMeter = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    LengthCm = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: false),
                    Points = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RollDefects", x => x.Id);
                    table.CheckConstraint("CK_RollDefects_AtMeter", "[AtMeter] >= 0");
                    table.CheckConstraint("CK_RollDefects_LengthCm", "[LengthCm] > 0");
                    table.CheckConstraint("CK_RollDefects_Points", "[Points] BETWEEN 1 AND 4");
                    table.ForeignKey(
                        name: "FK_RollDefects_DefectTypes_DefectTypeId",
                        column: x => x.DefectTypeId,
                        principalSchema: "Orme",
                        principalTable: "DefectTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RollDefects_FabricRolls_RollId",
                        column: x => x.RollId,
                        principalSchema: "Orme",
                        principalTable: "FabricRolls",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_FabricRolls_CalculatedStatus",
                schema: "Orme",
                table: "FabricRolls",
                sql: "[CalculatedStatus] IS NULL OR [CalculatedStatus] IN ('PENDING_INSPECTION', 'FIRST_QUALITY', 'SECOND_QUALITY', 'REJECTED')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_FabricRolls_InspectedMeters",
                schema: "Orme",
                table: "FabricRolls",
                sql: "[InspectedMeters] IS NULL OR [InspectedMeters] > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_FabricRolls_InspectedWeightGsm",
                schema: "Orme",
                table: "FabricRolls",
                sql: "[InspectedWeightGsm] IS NULL OR [InspectedWeightGsm] > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_FabricRolls_InspectedWidthCm",
                schema: "Orme",
                table: "FabricRolls",
                sql: "[InspectedWidthCm] IS NULL OR [InspectedWidthCm] > 0");

            migrationBuilder.CreateIndex(
                name: "IX_DefectTypes_Code",
                schema: "Orme",
                table: "DefectTypes",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_RollDefects_DefectTypeId",
                schema: "Orme",
                table: "RollDefects",
                column: "DefectTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_RollDefects_RollId",
                schema: "Orme",
                table: "RollDefects",
                column: "RollId");

            // Başlangıç tanımları; hata türleri ve sınırlar ekrandan değiştirilebilir.
            migrationBuilder.Sql(@"EXEC(N'INSERT INTO [Orme].[DefectTypes] ([Code], [Name], [AlwaysFourPoints], [IsActive], [CreatedAt], [IsDeleted])
SELECT v.[Code], v.[Name], v.[AlwaysFourPoints], 1, SYSUTCDATETIME(), 0 FROM (VALUES
(N''IGNE-CIZGISI'', N''İğne çizgisi'', 0),
(N''ILMEK-KACIGI'', N''İlmek kaçığı'', 0),
(N''BARE'', N''Bare (enine çizgi)'', 0),
(N''KALIN-YER'', N''Düğüm / kalın yer'', 0),
(N''IPLIK-KOPUGU'', N''İplik kopuğu'', 0),
(N''YAG-LEKESI'', N''Yağ lekesi'', 0),
(N''LEKE'', N''Leke'', 0),
(N''DELIK'', N''Delik'', 1)
) v([Code], [Name], [AlwaysFourPoints])
WHERE NOT EXISTS (SELECT 1 FROM [Orme].[DefectTypes] d WHERE d.[Code] = v.[Code])');
EXEC(N'INSERT INTO [Orme].[QualitySettings] ([FirstQualityMaxPoints], [SecondQualityMaxPoints], [WidthTolerancePercent], [WeightTolerancePercent], [CreatedAt])
SELECT 20, 40, 3, 5, SYSUTCDATETIME() WHERE NOT EXISTS (SELECT 1 FROM [Orme].[QualitySettings])');");

            // Yeni yetkiler; varsayılan olarak yalnızca Sistem Yöneticisine verilir, diğer roller rol ekranından yetkilendirilir.
            migrationBuilder.Sql(@"SET IDENTITY_INSERT [Core].[Permissions] ON;
EXEC(N'INSERT INTO [Core].[Permissions] ([Id], [Code], [Module], [Description])
SELECT v.[Id], v.[Code], v.[Module], v.[Description] FROM (VALUES
(3016, N''Orme.Inspection.View'', N''Orme'', N''Ham kontrolü görüntüleme''),
(3017, N''Orme.Inspection.Manage'', N''Orme'', N''Ham kontrol yapma, hata türleri ve kalite sınırları''),
(3018, N''Orme.Inspection.Override'', N''Orme'', N''Hesaplanan kaliteyi gerekçeyle değiştirme'')
) v([Id], [Code], [Module], [Description])
WHERE NOT EXISTS (SELECT 1 FROM [Core].[Permissions] p WHERE p.[Id] = v.[Id] OR p.[Code] = v.[Code])');
SET IDENTITY_INSERT [Core].[Permissions] OFF;");

            migrationBuilder.Sql(@"INSERT INTO [Core].[RolePermissions] ([RoleId], [PermissionId])
SELECT v.[RoleId], v.[PermissionId] FROM (VALUES
(1, 3016), (1, 3017), (1, 3018)
) v([RoleId], [PermissionId])
WHERE EXISTS (SELECT 1 FROM [Core].[Roles] r WHERE r.[Id] = v.[RoleId])
  AND EXISTS (SELECT 1 FROM [Core].[Permissions] p WHERE p.[Id] = v.[PermissionId])
  AND NOT EXISTS (SELECT 1 FROM [Core].[RolePermissions] rp WHERE rp.[RoleId] = v.[RoleId] AND rp.[PermissionId] = v.[PermissionId]);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DELETE FROM [Core].[RolePermissions] WHERE [PermissionId] BETWEEN 3016 AND 3018;
DELETE FROM [Core].[Permissions] WHERE [Id] BETWEEN 3016 AND 3018;");

            migrationBuilder.DropTable(
                name: "QualitySettings",
                schema: "Orme");

            migrationBuilder.DropTable(
                name: "RollDefects",
                schema: "Orme");

            migrationBuilder.DropTable(
                name: "DefectTypes",
                schema: "Orme");

            migrationBuilder.DropCheckConstraint(
                name: "CK_FabricRolls_CalculatedStatus",
                schema: "Orme",
                table: "FabricRolls");

            migrationBuilder.DropCheckConstraint(
                name: "CK_FabricRolls_InspectedMeters",
                schema: "Orme",
                table: "FabricRolls");

            migrationBuilder.DropCheckConstraint(
                name: "CK_FabricRolls_InspectedWeightGsm",
                schema: "Orme",
                table: "FabricRolls");

            migrationBuilder.DropCheckConstraint(
                name: "CK_FabricRolls_InspectedWidthCm",
                schema: "Orme",
                table: "FabricRolls");

            migrationBuilder.DropColumn(
                name: "CalculatedStatus",
                schema: "Orme",
                table: "FabricRolls");

            migrationBuilder.DropColumn(
                name: "InspectedMeters",
                schema: "Orme",
                table: "FabricRolls");

            migrationBuilder.DropColumn(
                name: "InspectedMetersEstimated",
                schema: "Orme",
                table: "FabricRolls");

            migrationBuilder.DropColumn(
                name: "InspectedOn",
                schema: "Orme",
                table: "FabricRolls");

            migrationBuilder.DropColumn(
                name: "InspectedWeightGsm",
                schema: "Orme",
                table: "FabricRolls");

            migrationBuilder.DropColumn(
                name: "InspectedWidthCm",
                schema: "Orme",
                table: "FabricRolls");

            migrationBuilder.DropColumn(
                name: "InspectionNotes",
                schema: "Orme",
                table: "FabricRolls");

            migrationBuilder.DropColumn(
                name: "OverrideNote",
                schema: "Orme",
                table: "FabricRolls");

            migrationBuilder.DropColumn(
                name: "PointsPer100M2",
                schema: "Orme",
                table: "FabricRolls");

            migrationBuilder.DropColumn(
                name: "SpecDeviation",
                schema: "Orme",
                table: "FabricRolls");

            migrationBuilder.DropColumn(
                name: "TotalPoints",
                schema: "Orme",
                table: "FabricRolls");
        }
    }
}
