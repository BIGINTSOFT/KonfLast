using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Dokuma.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFloorAndReports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ClientRequestId",
                schema: "Dokuma",
                table: "FabricRolls",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CutByOperatorId",
                schema: "Dokuma",
                table: "FabricRolls",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Operators",
                schema: "Dokuma",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Role = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CardUid = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
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
                    table.PrimaryKey("PK_Operators", x => x.Id);
                    table.CheckConstraint("CK_Operators_Role", "[Role] IN ('WEAVER', 'SUPERVISOR', 'MAINTENANCE')");
                });

            migrationBuilder.CreateTable(
                name: "RfidCards",
                schema: "Dokuma",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Uid = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Label = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
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
                    table.PrimaryKey("PK_RfidCards", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StopReasons",
                schema: "Dokuma",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_StopReasons", x => x.Id);
                    table.CheckConstraint("CK_StopReasons_Category", "[Category] IN ('STOP', 'BREAKDOWN', 'PLANNED')");
                });

            migrationBuilder.CreateTable(
                name: "Tablets",
                schema: "Dokuma",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TabletCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    LoomId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    DeviceIdentifier = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DeviceKeyHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    LastSeenAt = table.Column<DateTime>(type: "datetime2", nullable: true),
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
                    table.PrimaryKey("PK_Tablets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tablets_Looms_LoomId",
                        column: x => x.LoomId,
                        principalSchema: "Dokuma",
                        principalTable: "Looms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LoomStops",
                schema: "Dokuma",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LoomId = table.Column<int>(type: "int", nullable: false),
                    BeamId = table.Column<int>(type: "int", nullable: true),
                    ReasonId = table.Column<int>(type: "int", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StartedBy = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    EndedBy = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    TabletId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoomStops", x => x.Id);
                    table.CheckConstraint("CK_LoomStops_Category", "[Category] IN ('STOP', 'BREAKDOWN', 'PLANNED')");
                    table.ForeignKey(
                        name: "FK_LoomStops_Beams_BeamId",
                        column: x => x.BeamId,
                        principalSchema: "Dokuma",
                        principalTable: "Beams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LoomStops_Looms_LoomId",
                        column: x => x.LoomId,
                        principalSchema: "Dokuma",
                        principalTable: "Looms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LoomStops_StopReasons_ReasonId",
                        column: x => x.ReasonId,
                        principalSchema: "Dokuma",
                        principalTable: "StopReasons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TabletSessions",
                schema: "Dokuma",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TabletId = table.Column<int>(type: "int", nullable: false),
                    OperatorId = table.Column<int>(type: "int", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndReason = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TabletSessions", x => x.Id);
                    table.CheckConstraint("CK_TabletSessions_EndReason", "[EndReason] IN ('LOGOUT', 'TAKEN_OVER', 'MOVED_AWAY', 'CLOSED_BY_OFFICE')");
                    table.ForeignKey(
                        name: "FK_TabletSessions_Operators_OperatorId",
                        column: x => x.OperatorId,
                        principalSchema: "Dokuma",
                        principalTable: "Operators",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TabletSessions_Tablets_TabletId",
                        column: x => x.TabletId,
                        principalSchema: "Dokuma",
                        principalTable: "Tablets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FabricRolls_CutByOperatorId",
                schema: "Dokuma",
                table: "FabricRolls",
                column: "CutByOperatorId");

            migrationBuilder.CreateIndex(
                name: "UX_FabricRolls_ClientRequest",
                schema: "Dokuma",
                table: "FabricRolls",
                column: "ClientRequestId",
                unique: true,
                filter: "[ClientRequestId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_LoomStops_BeamId",
                schema: "Dokuma",
                table: "LoomStops",
                column: "BeamId");

            migrationBuilder.CreateIndex(
                name: "IX_LoomStops_LoomId_StartedAt",
                schema: "Dokuma",
                table: "LoomStops",
                columns: new[] { "LoomId", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_LoomStops_ReasonId",
                schema: "Dokuma",
                table: "LoomStops",
                column: "ReasonId");

            migrationBuilder.CreateIndex(
                name: "UX_LoomStops_Open",
                schema: "Dokuma",
                table: "LoomStops",
                column: "LoomId",
                unique: true,
                filter: "[EndedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Operators_Code",
                schema: "Dokuma",
                table: "Operators",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UX_Operators_CardUid",
                schema: "Dokuma",
                table: "Operators",
                column: "CardUid",
                unique: true,
                filter: "[CardUid] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_RfidCards_Uid",
                schema: "Dokuma",
                table: "RfidCards",
                column: "Uid",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_StopReasons_Code",
                schema: "Dokuma",
                table: "StopReasons",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Tablets_TabletCode",
                schema: "Dokuma",
                table: "Tablets",
                column: "TabletCode",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UX_Tablets_Device",
                schema: "Dokuma",
                table: "Tablets",
                column: "DeviceIdentifier",
                unique: true,
                filter: "[DeviceIdentifier] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UX_Tablets_Loom",
                schema: "Dokuma",
                table: "Tablets",
                column: "LoomId",
                unique: true,
                filter: "[LoomId] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UX_TabletSessions_Open",
                schema: "Dokuma",
                table: "TabletSessions",
                column: "TabletId",
                unique: true,
                filter: "[EndedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "UX_TabletSessions_OperatorOpen",
                schema: "Dokuma",
                table: "TabletSessions",
                column: "OperatorId",
                unique: true,
                filter: "[EndedAt] IS NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_FabricRolls_Operators_CutByOperatorId",
                schema: "Dokuma",
                table: "FabricRolls",
                column: "CutByOperatorId",
                principalSchema: "Dokuma",
                principalTable: "Operators",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // Yeni yetkiler; varsayılan olarak yalnızca Sistem Yöneticisine verilir, diğer roller rol ekranından yetkilendirilir.
            migrationBuilder.Sql(@"SET IDENTITY_INSERT [Core].[Permissions] ON;
EXEC(N'INSERT INTO [Core].[Permissions] ([Id], [Code], [Module], [Description])
SELECT v.[Id], v.[Code], v.[Module], v.[Description] FROM (VALUES
(2029, N''Dokuma.Floor.View'', N''Dokuma'', N''Dokuma personelini, kartları, tabletleri ve oturumları görüntüleme''),
(2030, N''Dokuma.Floor.Manage'', N''Dokuma'', N''Personel, kart, tablet ve duruş nedeni tanımlama; ofisten duruş ve oturum işlemleri''),
(2031, N''Dokuma.Report.View'', N''Dokuma'', N''Tezgâh verimi, sipariş takibi ve kalite raporları'')
) v([Id], [Code], [Module], [Description])
WHERE NOT EXISTS (SELECT 1 FROM [Core].[Permissions] p WHERE p.[Id] = v.[Id] OR p.[Code] = v.[Code])');
SET IDENTITY_INSERT [Core].[Permissions] OFF;");

            migrationBuilder.Sql(@"INSERT INTO [Core].[RolePermissions] ([RoleId], [PermissionId])
SELECT v.[RoleId], v.[PermissionId] FROM (VALUES
(1, 2029), (1, 2030), (1, 2031)
) v([RoleId], [PermissionId])
WHERE EXISTS (SELECT 1 FROM [Core].[Roles] r WHERE r.[Id] = v.[RoleId])
  AND EXISTS (SELECT 1 FROM [Core].[Permissions] p WHERE p.[Id] = v.[PermissionId])
  AND NOT EXISTS (SELECT 1 FROM [Core].[RolePermissions] rp WHERE rp.[RoleId] = v.[RoleId] AND rp.[PermissionId] = v.[PermissionId]);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DELETE FROM [Core].[RolePermissions] WHERE [PermissionId] BETWEEN 2029 AND 2031;
DELETE FROM [Core].[Permissions] WHERE [Id] BETWEEN 2029 AND 2031;");

            migrationBuilder.DropForeignKey(
                name: "FK_FabricRolls_Operators_CutByOperatorId",
                schema: "Dokuma",
                table: "FabricRolls");

            migrationBuilder.DropTable(
                name: "LoomStops",
                schema: "Dokuma");

            migrationBuilder.DropTable(
                name: "RfidCards",
                schema: "Dokuma");

            migrationBuilder.DropTable(
                name: "TabletSessions",
                schema: "Dokuma");

            migrationBuilder.DropTable(
                name: "StopReasons",
                schema: "Dokuma");

            migrationBuilder.DropTable(
                name: "Operators",
                schema: "Dokuma");

            migrationBuilder.DropTable(
                name: "Tablets",
                schema: "Dokuma");

            migrationBuilder.DropIndex(
                name: "IX_FabricRolls_CutByOperatorId",
                schema: "Dokuma",
                table: "FabricRolls");

            migrationBuilder.DropIndex(
                name: "UX_FabricRolls_ClientRequest",
                schema: "Dokuma",
                table: "FabricRolls");

            migrationBuilder.DropColumn(
                name: "ClientRequestId",
                schema: "Dokuma",
                table: "FabricRolls");

            migrationBuilder.DropColumn(
                name: "CutByOperatorId",
                schema: "Dokuma",
                table: "FabricRolls");
        }
    }
}
