using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Dokuma.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWeavingAndRolls : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "FinishedOn",
                schema: "Dokuma",
                table: "Beams",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "LeftoverMeters",
                schema: "Dokuma",
                table: "Beams",
                type: "decimal(12,2)",
                precision: 12,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LoomId",
                schema: "Dokuma",
                table: "Beams",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "MountedOn",
                schema: "Dokuma",
                table: "Beams",
                type: "date",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "FabricRolls",
                schema: "Dokuma",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RollNo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    BeamId = table.Column<int>(type: "int", nullable: false),
                    LoomId = table.Column<int>(type: "int", nullable: false),
                    CutOn = table.Column<DateOnly>(type: "date", nullable: false),
                    Meters = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    Kilograms = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FabricRolls", x => x.Id);
                    table.CheckConstraint("CK_FabricRolls_Kilograms", "[Kilograms] IS NULL OR [Kilograms] > 0");
                    table.CheckConstraint("CK_FabricRolls_Meters", "[Meters] > 0");
                    table.CheckConstraint("CK_FabricRolls_Status", "[Status] IN ('PENDING_INSPECTION')");
                    table.ForeignKey(
                        name: "FK_FabricRolls_Beams_BeamId",
                        column: x => x.BeamId,
                        principalSchema: "Dokuma",
                        principalTable: "Beams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FabricRolls_Looms_LoomId",
                        column: x => x.LoomId,
                        principalSchema: "Dokuma",
                        principalTable: "Looms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WeftIssues",
                schema: "Dokuma",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BeamId = table.Column<int>(type: "int", nullable: false),
                    YarnLotId = table.Column<int>(type: "int", nullable: false),
                    IssuedOn = table.Column<DateOnly>(type: "date", nullable: false),
                    Kilograms = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeftIssues", x => x.Id);
                    table.CheckConstraint("CK_WeftIssues_Kilograms", "[Kilograms] > 0");
                    table.ForeignKey(
                        name: "FK_WeftIssues_Beams_BeamId",
                        column: x => x.BeamId,
                        principalSchema: "Dokuma",
                        principalTable: "Beams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WeftIssues_YarnLots_YarnLotId",
                        column: x => x.YarnLotId,
                        principalSchema: "Dokuma",
                        principalTable: "YarnLots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "UX_Beams_LoomOnLoom",
                schema: "Dokuma",
                table: "Beams",
                column: "LoomId",
                unique: true,
                filter: "[Status] = 'ON_LOOM'");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Beams_LeftoverMeters",
                schema: "Dokuma",
                table: "Beams",
                sql: "[LeftoverMeters] IS NULL OR [LeftoverMeters] >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_FabricRolls_BeamId",
                schema: "Dokuma",
                table: "FabricRolls",
                column: "BeamId");

            migrationBuilder.CreateIndex(
                name: "IX_FabricRolls_LoomId",
                schema: "Dokuma",
                table: "FabricRolls",
                column: "LoomId");

            migrationBuilder.CreateIndex(
                name: "IX_FabricRolls_RollNo",
                schema: "Dokuma",
                table: "FabricRolls",
                column: "RollNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FabricRolls_Status",
                schema: "Dokuma",
                table: "FabricRolls",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_WeftIssues_BeamId",
                schema: "Dokuma",
                table: "WeftIssues",
                column: "BeamId");

            migrationBuilder.CreateIndex(
                name: "IX_WeftIssues_YarnLotId",
                schema: "Dokuma",
                table: "WeftIssues",
                column: "YarnLotId");

            migrationBuilder.AddForeignKey(
                name: "FK_Beams_Looms_LoomId",
                schema: "Dokuma",
                table: "Beams",
                column: "LoomId",
                principalSchema: "Dokuma",
                principalTable: "Looms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // Yeni yetkiler; varsayılan olarak yalnızca Sistem Yöneticisine verilir, diğer roller rol ekranından yetkilendirilir.
            migrationBuilder.Sql(@"SET IDENTITY_INSERT [Core].[Permissions] ON;
EXEC(N'INSERT INTO [Core].[Permissions] ([Id], [Code], [Module], [Description])
SELECT v.[Id], v.[Code], v.[Module], v.[Description] FROM (VALUES
(2016, N''Dokuma.Weaving.View'', N''Dokuma'', N''Tezgâh panosunu, dokunan leventleri ve topları görüntüleme''),
(2017, N''Dokuma.Weaving.Manage'', N''Dokuma'', N''Leventi tezgâha bağlama ve bitirme, atkıya iplik verme, top kesme'')
) v([Id], [Code], [Module], [Description])
WHERE NOT EXISTS (SELECT 1 FROM [Core].[Permissions] p WHERE p.[Id] = v.[Id] OR p.[Code] = v.[Code])');
SET IDENTITY_INSERT [Core].[Permissions] OFF;");

            migrationBuilder.Sql(@"INSERT INTO [Core].[RolePermissions] ([RoleId], [PermissionId])
SELECT v.[RoleId], v.[PermissionId] FROM (VALUES
(1, 2016), (1, 2017)
) v([RoleId], [PermissionId])
WHERE EXISTS (SELECT 1 FROM [Core].[Roles] r WHERE r.[Id] = v.[RoleId])
  AND EXISTS (SELECT 1 FROM [Core].[Permissions] p WHERE p.[Id] = v.[PermissionId])
  AND NOT EXISTS (SELECT 1 FROM [Core].[RolePermissions] rp WHERE rp.[RoleId] = v.[RoleId] AND rp.[PermissionId] = v.[PermissionId]);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DELETE FROM [Core].[RolePermissions] WHERE [PermissionId] IN (2016, 2017);
DELETE FROM [Core].[Permissions] WHERE [Id] IN (2016, 2017);");

            migrationBuilder.DropForeignKey(
                name: "FK_Beams_Looms_LoomId",
                schema: "Dokuma",
                table: "Beams");

            migrationBuilder.DropTable(
                name: "FabricRolls",
                schema: "Dokuma");

            migrationBuilder.DropTable(
                name: "WeftIssues",
                schema: "Dokuma");

            migrationBuilder.DropIndex(
                name: "UX_Beams_LoomOnLoom",
                schema: "Dokuma",
                table: "Beams");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Beams_LeftoverMeters",
                schema: "Dokuma",
                table: "Beams");

            migrationBuilder.DropColumn(
                name: "FinishedOn",
                schema: "Dokuma",
                table: "Beams");

            migrationBuilder.DropColumn(
                name: "LeftoverMeters",
                schema: "Dokuma",
                table: "Beams");

            migrationBuilder.DropColumn(
                name: "LoomId",
                schema: "Dokuma",
                table: "Beams");

            migrationBuilder.DropColumn(
                name: "MountedOn",
                schema: "Dokuma",
                table: "Beams");
        }
    }
}
