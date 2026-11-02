using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Dokuma.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWarpingAndBeams : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WarpOrders",
                schema: "Dokuma",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WarpNo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    OrderLineId = table.Column<int>(type: "int", nullable: false),
                    PlannedDate = table.Column<DateOnly>(type: "date", nullable: false),
                    PlannedBeamCount = table.Column<int>(type: "int", nullable: false),
                    MetersPerBeam = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    Ends = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CompletedOn = table.Column<DateOnly>(type: "date", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WarpOrders", x => x.Id);
                    table.CheckConstraint("CK_WarpOrders_Ends", "[Ends] >= 1");
                    table.CheckConstraint("CK_WarpOrders_MetersPerBeam", "[MetersPerBeam] > 0");
                    table.CheckConstraint("CK_WarpOrders_PlannedBeamCount", "[PlannedBeamCount] >= 1");
                    table.CheckConstraint("CK_WarpOrders_Status", "[Status] IN ('OPEN', 'COMPLETED', 'CANCELLED')");
                    table.ForeignKey(
                        name: "FK_WarpOrders_OrderLines_OrderLineId",
                        column: x => x.OrderLineId,
                        principalSchema: "Dokuma",
                        principalTable: "OrderLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Beams",
                schema: "Dokuma",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BeamNo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    WarpOrderId = table.Column<int>(type: "int", nullable: false),
                    Meters = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    Ends = table.Column<int>(type: "int", nullable: false),
                    SizingMode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    IsSized = table.Column<bool>(type: "bit", nullable: false),
                    SizingSupplierId = table.Column<int>(type: "int", nullable: true),
                    SentToSizingOn = table.Column<DateOnly>(type: "date", nullable: true),
                    SizedOn = table.Column<DateOnly>(type: "date", nullable: true),
                    DrawingMethod = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    ReadyOn = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Beams", x => x.Id);
                    table.CheckConstraint("CK_Beams_DrawingMethod", "[DrawingMethod] IS NULL OR [DrawingMethod] IN ('DRAWING', 'TYING')");
                    table.CheckConstraint("CK_Beams_Ends", "[Ends] >= 1");
                    table.CheckConstraint("CK_Beams_Meters", "[Meters] > 0");
                    table.CheckConstraint("CK_Beams_SizingMode", "[SizingMode] IN ('NONE', 'IN_HOUSE', 'COMMISSION')");
                    table.CheckConstraint("CK_Beams_Status", "[Status] IN ('WARPED', 'AT_SIZING', 'READY', 'ON_LOOM', 'FINISHED')");
                    table.ForeignKey(
                        name: "FK_Beams_Suppliers_SizingSupplierId",
                        column: x => x.SizingSupplierId,
                        principalSchema: "Dokuma",
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Beams_WarpOrders_WarpOrderId",
                        column: x => x.WarpOrderId,
                        principalSchema: "Dokuma",
                        principalTable: "WarpOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WarpIssues",
                schema: "Dokuma",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WarpOrderId = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_WarpIssues", x => x.Id);
                    table.CheckConstraint("CK_WarpIssues_Kilograms", "[Kilograms] > 0");
                    table.ForeignKey(
                        name: "FK_WarpIssues_WarpOrders_WarpOrderId",
                        column: x => x.WarpOrderId,
                        principalSchema: "Dokuma",
                        principalTable: "WarpOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WarpIssues_YarnLots_YarnLotId",
                        column: x => x.YarnLotId,
                        principalSchema: "Dokuma",
                        principalTable: "YarnLots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Beams_BeamNo",
                schema: "Dokuma",
                table: "Beams",
                column: "BeamNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Beams_SizingSupplierId",
                schema: "Dokuma",
                table: "Beams",
                column: "SizingSupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_Beams_Status",
                schema: "Dokuma",
                table: "Beams",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Beams_WarpOrderId",
                schema: "Dokuma",
                table: "Beams",
                column: "WarpOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_WarpIssues_WarpOrderId",
                schema: "Dokuma",
                table: "WarpIssues",
                column: "WarpOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_WarpIssues_YarnLotId",
                schema: "Dokuma",
                table: "WarpIssues",
                column: "YarnLotId");

            migrationBuilder.CreateIndex(
                name: "IX_WarpOrders_OrderLineId",
                schema: "Dokuma",
                table: "WarpOrders",
                column: "OrderLineId");

            migrationBuilder.CreateIndex(
                name: "IX_WarpOrders_WarpNo",
                schema: "Dokuma",
                table: "WarpOrders",
                column: "WarpNo",
                unique: true);

            // Yeni yetkiler; varsayılan olarak yalnızca Sistem Yöneticisine verilir, diğer roller rol ekranından yetkilendirilir.
            migrationBuilder.Sql(@"SET IDENTITY_INSERT [Core].[Permissions] ON;
EXEC(N'INSERT INTO [Core].[Permissions] ([Id], [Code], [Module], [Description])
SELECT v.[Id], v.[Code], v.[Module], v.[Description] FROM (VALUES
(2014, N''Dokuma.Warp.View'', N''Dokuma'', N''Çözgü emirlerini ve leventleri görüntüleme''),
(2015, N''Dokuma.Warp.Manage'', N''Dokuma'', N''Çözgü emri, çözgüye iplik verme, levent, haşıl ve tahar / düğüm'')
) v([Id], [Code], [Module], [Description])
WHERE NOT EXISTS (SELECT 1 FROM [Core].[Permissions] p WHERE p.[Id] = v.[Id] OR p.[Code] = v.[Code])');
SET IDENTITY_INSERT [Core].[Permissions] OFF;");

            migrationBuilder.Sql(@"INSERT INTO [Core].[RolePermissions] ([RoleId], [PermissionId])
SELECT v.[RoleId], v.[PermissionId] FROM (VALUES
(1, 2014), (1, 2015)
) v([RoleId], [PermissionId])
WHERE EXISTS (SELECT 1 FROM [Core].[Roles] r WHERE r.[Id] = v.[RoleId])
  AND EXISTS (SELECT 1 FROM [Core].[Permissions] p WHERE p.[Id] = v.[PermissionId])
  AND NOT EXISTS (SELECT 1 FROM [Core].[RolePermissions] rp WHERE rp.[RoleId] = v.[RoleId] AND rp.[PermissionId] = v.[PermissionId]);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DELETE FROM [Core].[RolePermissions] WHERE [PermissionId] IN (2014, 2015);
DELETE FROM [Core].[Permissions] WHERE [Id] IN (2014, 2015);");

            migrationBuilder.DropTable(
                name: "Beams",
                schema: "Dokuma");

            migrationBuilder.DropTable(
                name: "WarpIssues",
                schema: "Dokuma");

            migrationBuilder.DropTable(
                name: "WarpOrders",
                schema: "Dokuma");
        }
    }
}
