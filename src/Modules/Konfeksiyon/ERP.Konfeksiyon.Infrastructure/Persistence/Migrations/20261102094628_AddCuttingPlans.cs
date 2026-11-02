using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Konfeksiyon.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCuttingPlans : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CuttingPlanId",
                schema: "Konfeksiyon",
                table: "Bundles",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CuttingPlans",
                schema: "Konfeksiyon",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlanNo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ProductionOrderId = table.Column<int>(type: "int", nullable: false),
                    ColorId = table.Column<int>(type: "int", nullable: false),
                    MarkerLength = table.Column<decimal>(type: "decimal(9,3)", precision: 9, scale: 3, nullable: false),
                    EndAllowance = table.Column<decimal>(type: "decimal(9,3)", precision: 9, scale: 3, nullable: false),
                    PlannedPlies = table.Column<int>(type: "int", nullable: false),
                    ActualPlies = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CutAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CutBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CuttingPlans", x => x.Id);
                    table.CheckConstraint("CK_CuttingPlans_ActualPlies", "[ActualPlies] IS NULL OR [ActualPlies] > 0");
                    table.CheckConstraint("CK_CuttingPlans_EndAllowance", "[EndAllowance] >= 0");
                    table.CheckConstraint("CK_CuttingPlans_MarkerLength", "[MarkerLength] > 0");
                    table.CheckConstraint("CK_CuttingPlans_PlannedPlies", "[PlannedPlies] > 0");
                    table.CheckConstraint("CK_CuttingPlans_Status", "[Status] IN ('DRAFT', 'CUT', 'CANCELLED')");
                    table.ForeignKey(
                        name: "FK_CuttingPlans_Colors_ColorId",
                        column: x => x.ColorId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "Colors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CuttingPlans_ProductionOrders_ProductionOrderId",
                        column: x => x.ProductionOrderId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "ProductionOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CuttingPlanLots",
                schema: "Konfeksiyon",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CuttingPlanId = table.Column<int>(type: "int", nullable: false),
                    StockLotId = table.Column<int>(type: "int", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    UsedQuantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CuttingPlanLots", x => x.Id);
                    table.CheckConstraint("CK_CuttingPlanLots_UsedQuantity", "[UsedQuantity] IS NULL OR [UsedQuantity] >= 0");
                    table.ForeignKey(
                        name: "FK_CuttingPlanLots_CuttingPlans_CuttingPlanId",
                        column: x => x.CuttingPlanId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "CuttingPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CuttingPlanLots_StockLots_StockLotId",
                        column: x => x.StockLotId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "StockLots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CuttingPlanSizes",
                schema: "Konfeksiyon",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CuttingPlanId = table.Column<int>(type: "int", nullable: false),
                    SizeId = table.Column<int>(type: "int", nullable: false),
                    Ratio = table.Column<int>(type: "int", nullable: false),
                    CutQuantity = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CuttingPlanSizes", x => x.Id);
                    table.CheckConstraint("CK_CuttingPlanSizes_CutQuantity", "[CutQuantity] IS NULL OR [CutQuantity] >= 0");
                    table.CheckConstraint("CK_CuttingPlanSizes_Ratio", "[Ratio] > 0");
                    table.ForeignKey(
                        name: "FK_CuttingPlanSizes_CuttingPlans_CuttingPlanId",
                        column: x => x.CuttingPlanId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "CuttingPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CuttingPlanSizes_Sizes_SizeId",
                        column: x => x.SizeId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "Sizes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bundles_CuttingPlanId",
                schema: "Konfeksiyon",
                table: "Bundles",
                column: "CuttingPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_CuttingPlanLots_CuttingPlanId_StockLotId",
                schema: "Konfeksiyon",
                table: "CuttingPlanLots",
                columns: new[] { "CuttingPlanId", "StockLotId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CuttingPlanLots_StockLotId",
                schema: "Konfeksiyon",
                table: "CuttingPlanLots",
                column: "StockLotId");

            migrationBuilder.CreateIndex(
                name: "IX_CuttingPlans_ColorId",
                schema: "Konfeksiyon",
                table: "CuttingPlans",
                column: "ColorId");

            migrationBuilder.CreateIndex(
                name: "IX_CuttingPlans_PlanNo",
                schema: "Konfeksiyon",
                table: "CuttingPlans",
                column: "PlanNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CuttingPlans_ProductionOrderId_Status",
                schema: "Konfeksiyon",
                table: "CuttingPlans",
                columns: new[] { "ProductionOrderId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_CuttingPlans_Status",
                schema: "Konfeksiyon",
                table: "CuttingPlans",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_CuttingPlanSizes_CuttingPlanId_SizeId",
                schema: "Konfeksiyon",
                table: "CuttingPlanSizes",
                columns: new[] { "CuttingPlanId", "SizeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CuttingPlanSizes_SizeId",
                schema: "Konfeksiyon",
                table: "CuttingPlanSizes",
                column: "SizeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Bundles_CuttingPlans_CuttingPlanId",
                schema: "Konfeksiyon",
                table: "Bundles",
                column: "CuttingPlanId",
                principalSchema: "Konfeksiyon",
                principalTable: "CuttingPlans",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // Yeni yetkiler; varsayılan olarak yalnızca Sistem Yöneticisine verilir, diğer roller rol ekranından yetkilendirilir.
            migrationBuilder.Sql(@"SET IDENTITY_INSERT [Core].[Permissions] ON;
EXEC(N'INSERT INTO [Core].[Permissions] ([Id], [Code], [Module], [Description])
SELECT v.[Id], v.[Code], v.[Module], v.[Description] FROM (VALUES
(97, N''CuttingPlan.View'', N''CuttingPlan'', N''Kesim planlarını görüntüleme''),
(98, N''CuttingPlan.Manage'', N''CuttingPlan'', N''Kesim planı oluşturma ve kesim kaydı'')
) v([Id], [Code], [Module], [Description])
WHERE NOT EXISTS (SELECT 1 FROM [Core].[Permissions] p WHERE p.[Id] = v.[Id] OR p.[Code] = v.[Code])');
SET IDENTITY_INSERT [Core].[Permissions] OFF;");

            migrationBuilder.Sql(@"INSERT INTO [Core].[RolePermissions] ([RoleId], [PermissionId])
SELECT v.[RoleId], v.[PermissionId] FROM (VALUES
(1, 97), (1, 98)
) v([RoleId], [PermissionId])
WHERE EXISTS (SELECT 1 FROM [Core].[Roles] r WHERE r.[Id] = v.[RoleId])
  AND EXISTS (SELECT 1 FROM [Core].[Permissions] p WHERE p.[Id] = v.[PermissionId])
  AND NOT EXISTS (SELECT 1 FROM [Core].[RolePermissions] rp WHERE rp.[RoleId] = v.[RoleId] AND rp.[PermissionId] = v.[PermissionId]);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DELETE FROM [Core].[RolePermissions] WHERE [PermissionId] IN (97, 98);
DELETE FROM [Core].[Permissions] WHERE [Id] IN (97, 98);");

            migrationBuilder.DropForeignKey(
                name: "FK_Bundles_CuttingPlans_CuttingPlanId",
                schema: "Konfeksiyon",
                table: "Bundles");

            migrationBuilder.DropTable(
                name: "CuttingPlanLots",
                schema: "Konfeksiyon");

            migrationBuilder.DropTable(
                name: "CuttingPlanSizes",
                schema: "Konfeksiyon");

            migrationBuilder.DropTable(
                name: "CuttingPlans",
                schema: "Konfeksiyon");

            migrationBuilder.DropIndex(
                name: "IX_Bundles_CuttingPlanId",
                schema: "Konfeksiyon",
                table: "Bundles");

            migrationBuilder.DropColumn(
                name: "CuttingPlanId",
                schema: "Konfeksiyon",
                table: "Bundles");
        }
    }
}
