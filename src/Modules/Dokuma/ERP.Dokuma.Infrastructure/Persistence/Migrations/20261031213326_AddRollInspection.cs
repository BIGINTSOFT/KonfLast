using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Dokuma.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRollInspection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_FabricRolls_Status",
                schema: "Dokuma",
                table: "FabricRolls");

            migrationBuilder.AddColumn<string>(
                name: "CalculatedStatus",
                schema: "Dokuma",
                table: "FabricRolls",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "InspectedMeters",
                schema: "Dokuma",
                table: "FabricRolls",
                type: "decimal(12,2)",
                precision: 12,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "InspectedOn",
                schema: "Dokuma",
                table: "FabricRolls",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "InspectedWidthCm",
                schema: "Dokuma",
                table: "FabricRolls",
                type: "decimal(6,2)",
                precision: 6,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InspectionNotes",
                schema: "Dokuma",
                table: "FabricRolls",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OverrideNote",
                schema: "Dokuma",
                table: "FabricRolls",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PointsPer100M2",
                schema: "Dokuma",
                table: "FabricRolls",
                type: "decimal(8,2)",
                precision: 8,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TotalPoints",
                schema: "Dokuma",
                table: "FabricRolls",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "FirstQualityMaxPoints",
                schema: "Dokuma",
                table: "Customers",
                type: "decimal(8,2)",
                precision: 8,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SecondQualityMaxPoints",
                schema: "Dokuma",
                table: "Customers",
                type: "decimal(8,2)",
                precision: 8,
                scale: 2,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DefectTypes",
                schema: "Dokuma",
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
                schema: "Dokuma",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FirstQualityMaxPoints = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: false),
                    SecondQualityMaxPoints = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QualitySettings", x => x.Id);
                    table.CheckConstraint("CK_QualitySettings_Limits", "[FirstQualityMaxPoints] >= 0 AND [SecondQualityMaxPoints] >= [FirstQualityMaxPoints]");
                });

            migrationBuilder.CreateTable(
                name: "RollDefects",
                schema: "Dokuma",
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
                        principalSchema: "Dokuma",
                        principalTable: "DefectTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RollDefects_FabricRolls_RollId",
                        column: x => x.RollId,
                        principalSchema: "Dokuma",
                        principalTable: "FabricRolls",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_FabricRolls_CalculatedStatus",
                schema: "Dokuma",
                table: "FabricRolls",
                sql: "[CalculatedStatus] IS NULL OR [CalculatedStatus] IN ('PENDING_INSPECTION', 'FIRST_QUALITY', 'SECOND_QUALITY', 'REJECTED')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_FabricRolls_InspectedMeters",
                schema: "Dokuma",
                table: "FabricRolls",
                sql: "[InspectedMeters] IS NULL OR [InspectedMeters] > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_FabricRolls_InspectedWidthCm",
                schema: "Dokuma",
                table: "FabricRolls",
                sql: "[InspectedWidthCm] IS NULL OR [InspectedWidthCm] > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_FabricRolls_Status",
                schema: "Dokuma",
                table: "FabricRolls",
                sql: "[Status] IN ('PENDING_INSPECTION', 'FIRST_QUALITY', 'SECOND_QUALITY', 'REJECTED')");

            migrationBuilder.CreateIndex(
                name: "IX_DefectTypes_Code",
                schema: "Dokuma",
                table: "DefectTypes",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_RollDefects_DefectTypeId",
                schema: "Dokuma",
                table: "RollDefects",
                column: "DefectTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_RollDefects_RollId",
                schema: "Dokuma",
                table: "RollDefects",
                column: "RollId");

            // Başlangıç hata türleri ve genel kalite sınırları (100 m² başına puan); kullanıcı ekrandan değiştirir.
            migrationBuilder.Sql(@"EXEC(N'INSERT INTO [Dokuma].[DefectTypes] ([Code], [Name], [AlwaysFourPoints], [IsActive], [CreatedAt], [IsDeleted])
SELECT v.[Code], v.[Name], v.[AlwaysFourPoints], 1, SYSUTCDATETIME(), 0 FROM (VALUES
(N''ATKI-KACIGI'', N''Atkı kaçığı'', 0),
(N''COZGU-KOPUGU'', N''Çözgü kopuğu'', 0),
(N''CIFT-ATKI'', N''Çift atkı'', 0),
(N''SIKLIK-FARKI'', N''Atkı sıklık farkı'', 0),
(N''KALIN-YER'', N''Düğüm / kalın yer'', 0),
(N''LEKE'', N''Leke'', 0),
(N''KENAR'', N''Kenar hatası'', 0),
(N''DELIK'', N''Delik'', 1)
) v([Code], [Name], [AlwaysFourPoints])
WHERE NOT EXISTS (SELECT 1 FROM [Dokuma].[DefectTypes] d WHERE d.[Code] = v.[Code])');
EXEC(N'INSERT INTO [Dokuma].[QualitySettings] ([FirstQualityMaxPoints], [SecondQualityMaxPoints], [CreatedAt])
SELECT 20, 40, SYSUTCDATETIME() WHERE NOT EXISTS (SELECT 1 FROM [Dokuma].[QualitySettings])');");

            // Yeni yetkiler; varsayılan olarak yalnızca Sistem Yöneticisine verilir, diğer roller rol ekranından yetkilendirilir.
            migrationBuilder.Sql(@"SET IDENTITY_INSERT [Core].[Permissions] ON;
EXEC(N'INSERT INTO [Core].[Permissions] ([Id], [Code], [Module], [Description])
SELECT v.[Id], v.[Code], v.[Module], v.[Description] FROM (VALUES
(2018, N''Dokuma.Inspection.View'', N''Dokuma'', N''Ham kontrolü görüntüleme''),
(2019, N''Dokuma.Inspection.Manage'', N''Dokuma'', N''Ham kontrol yapma, hata türleri ve kalite sınırları''),
(2020, N''Dokuma.Inspection.Override'', N''Dokuma'', N''Hesaplanan kaliteyi gerekçeyle değiştirme'')
) v([Id], [Code], [Module], [Description])
WHERE NOT EXISTS (SELECT 1 FROM [Core].[Permissions] p WHERE p.[Id] = v.[Id] OR p.[Code] = v.[Code])');
SET IDENTITY_INSERT [Core].[Permissions] OFF;");

            migrationBuilder.Sql(@"INSERT INTO [Core].[RolePermissions] ([RoleId], [PermissionId])
SELECT v.[RoleId], v.[PermissionId] FROM (VALUES
(1, 2018), (1, 2019), (1, 2020)
) v([RoleId], [PermissionId])
WHERE EXISTS (SELECT 1 FROM [Core].[Roles] r WHERE r.[Id] = v.[RoleId])
  AND EXISTS (SELECT 1 FROM [Core].[Permissions] p WHERE p.[Id] = v.[PermissionId])
  AND NOT EXISTS (SELECT 1 FROM [Core].[RolePermissions] rp WHERE rp.[RoleId] = v.[RoleId] AND rp.[PermissionId] = v.[PermissionId]);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DELETE FROM [Core].[RolePermissions] WHERE [PermissionId] IN (2018, 2019, 2020);
DELETE FROM [Core].[Permissions] WHERE [Id] IN (2018, 2019, 2020);");

            migrationBuilder.DropTable(
                name: "QualitySettings",
                schema: "Dokuma");

            migrationBuilder.DropTable(
                name: "RollDefects",
                schema: "Dokuma");

            migrationBuilder.DropTable(
                name: "DefectTypes",
                schema: "Dokuma");

            migrationBuilder.DropCheckConstraint(
                name: "CK_FabricRolls_CalculatedStatus",
                schema: "Dokuma",
                table: "FabricRolls");

            migrationBuilder.DropCheckConstraint(
                name: "CK_FabricRolls_InspectedMeters",
                schema: "Dokuma",
                table: "FabricRolls");

            migrationBuilder.DropCheckConstraint(
                name: "CK_FabricRolls_InspectedWidthCm",
                schema: "Dokuma",
                table: "FabricRolls");

            migrationBuilder.DropCheckConstraint(
                name: "CK_FabricRolls_Status",
                schema: "Dokuma",
                table: "FabricRolls");

            migrationBuilder.DropColumn(
                name: "CalculatedStatus",
                schema: "Dokuma",
                table: "FabricRolls");

            migrationBuilder.DropColumn(
                name: "InspectedMeters",
                schema: "Dokuma",
                table: "FabricRolls");

            migrationBuilder.DropColumn(
                name: "InspectedOn",
                schema: "Dokuma",
                table: "FabricRolls");

            migrationBuilder.DropColumn(
                name: "InspectedWidthCm",
                schema: "Dokuma",
                table: "FabricRolls");

            migrationBuilder.DropColumn(
                name: "InspectionNotes",
                schema: "Dokuma",
                table: "FabricRolls");

            migrationBuilder.DropColumn(
                name: "OverrideNote",
                schema: "Dokuma",
                table: "FabricRolls");

            migrationBuilder.DropColumn(
                name: "PointsPer100M2",
                schema: "Dokuma",
                table: "FabricRolls");

            migrationBuilder.DropColumn(
                name: "TotalPoints",
                schema: "Dokuma",
                table: "FabricRolls");

            migrationBuilder.DropColumn(
                name: "FirstQualityMaxPoints",
                schema: "Dokuma",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "SecondQualityMaxPoints",
                schema: "Dokuma",
                table: "Customers");

            migrationBuilder.AddCheckConstraint(
                name: "CK_FabricRolls_Status",
                schema: "Dokuma",
                table: "FabricRolls",
                sql: "[Status] IN ('PENDING_INSPECTION')");
        }
    }
}
