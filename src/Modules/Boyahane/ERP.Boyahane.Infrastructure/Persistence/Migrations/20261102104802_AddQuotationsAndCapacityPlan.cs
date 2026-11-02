using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Boyahane.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddQuotationsAndCapacityPlan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PlanSequence",
                schema: "Boyahane",
                table: "Batches",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CostSettings",
                schema: "Boyahane",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Currency = table.Column<string>(type: "char(3)", maxLength: 200, nullable: false, defaultValue: "TRY"),
                    DefaultMachineHourlyCost = table.Column<decimal>(type: "decimal(12,4)", precision: 12, scale: 4, nullable: false),
                    LaborCostPerMinute = table.Column<decimal>(type: "decimal(12,4)", precision: 12, scale: 4, nullable: false),
                    DefaultLiquorRatio = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: false),
                    DefaultBatchKilograms = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    OverheadPercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    FirePercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    MarginPercent = table.Column<decimal>(type: "decimal(7,2)", precision: 7, scale: 2, nullable: false),
                    ChemicalCostMethod = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    FabricDyeingMinutes = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    YarnDyeingMinutes = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    FinishingMinutes = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    PrintingMinutes = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CostSettings", x => x.Id);
                    table.CheckConstraint("CK_CostSettings_Costs", "[DefaultMachineHourlyCost] >= 0 AND [LaborCostPerMinute] >= 0");
                    table.CheckConstraint("CK_CostSettings_Defaults", "[DefaultLiquorRatio] > 0 AND [DefaultBatchKilograms] > 0");
                    table.CheckConstraint("CK_CostSettings_Method", "[ChemicalCostMethod] IN ('LAST_LOT', 'WEIGHTED_AVERAGE')");
                    table.CheckConstraint("CK_CostSettings_Minutes", "[FabricDyeingMinutes] > 0 AND [YarnDyeingMinutes] > 0 AND [FinishingMinutes] > 0 AND [PrintingMinutes] > 0");
                    table.CheckConstraint("CK_CostSettings_Percents", "[OverheadPercent] BETWEEN 0 AND 100 AND [FirePercent] BETWEEN 0 AND 50 AND [MarginPercent] BETWEEN -100 AND 1000");
                });

            migrationBuilder.CreateTable(
                name: "MachineCostRates",
                schema: "Boyahane",
                columns: table => new
                {
                    MachineId = table.Column<int>(type: "int", nullable: false),
                    HourlyCost = table.Column<decimal>(type: "decimal(12,4)", precision: 12, scale: 4, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MachineCostRates", x => x.MachineId);
                    table.CheckConstraint("CK_MachineCostRates_HourlyCost", "[HourlyCost] >= 0");
                    table.ForeignKey(
                        name: "FK_MachineCostRates_Machines_MachineId",
                        column: x => x.MachineId,
                        principalSchema: "Boyahane",
                        principalTable: "Machines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Quotations",
                schema: "Boyahane",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    QuotationNo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    QuotationDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ValidUntil = table.Column<DateOnly>(type: "date", nullable: false),
                    CustomerId = table.Column<int>(type: "int", nullable: true),
                    ProspectName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Ownership = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Currency = table.Column<string>(type: "char(3)", maxLength: 200, nullable: false, defaultValue: "TRY"),
                    DocumentLanguage = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DecidedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DecidedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DecisionNote = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    OrderId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Quotations", x => x.Id);
                    table.CheckConstraint("CK_Quotations_DocumentLanguage", "[DocumentLanguage] IN ('TURKISH', 'ENGLISH')");
                    table.CheckConstraint("CK_Quotations_Ownership", "[Ownership] IN ('COMMISSION', 'OWN_GOODS')");
                    table.CheckConstraint("CK_Quotations_Party", "[CustomerId] IS NOT NULL OR [ProspectName] IS NOT NULL");
                    table.CheckConstraint("CK_Quotations_Status", "[Status] IN ('DRAFT', 'SENT', 'ACCEPTED', 'REJECTED', 'EXPIRED')");
                    table.CheckConstraint("CK_Quotations_ValidUntil", "[ValidUntil] >= [QuotationDate]");
                    table.ForeignKey(
                        name: "FK_Quotations_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "Boyahane",
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Quotations_Orders_OrderId",
                        column: x => x.OrderId,
                        principalSchema: "Boyahane",
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "QuotationLines",
                schema: "Boyahane",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    QuotationId = table.Column<int>(type: "int", nullable: false),
                    ProcessType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    FabricId = table.Column<int>(type: "int", nullable: true),
                    YarnId = table.Column<int>(type: "int", nullable: true),
                    ColorName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ColorCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ColorCardId = table.Column<int>(type: "int", nullable: true),
                    PatternId = table.Column<int>(type: "int", nullable: true),
                    ColorwayId = table.Column<int>(type: "int", nullable: true),
                    Quantity = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    RecipeTrialId = table.Column<int>(type: "int", nullable: true),
                    MachineId = table.Column<int>(type: "int", nullable: true),
                    BatchKilogramsInput = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: true),
                    LiquorRatioInput = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: true),
                    ProcessMinutesInput = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: true),
                    FirePercentInput = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    OverheadPercentInput = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    MarginPercentInput = table.Column<decimal>(type: "decimal(7,2)", precision: 7, scale: 2, nullable: true),
                    UnitPriceInput = table.Column<decimal>(type: "decimal(12,4)", precision: 12, scale: 4, nullable: true),
                    BatchKilograms = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    LiquorRatio = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: true),
                    ProcessMinutes = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    MinutesSource = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    KilogramsPerMeter = table.Column<decimal>(type: "decimal(10,4)", precision: 10, scale: 4, nullable: true),
                    ChemicalCost = table.Column<decimal>(type: "decimal(14,4)", precision: 14, scale: 4, nullable: false),
                    PrintCost = table.Column<decimal>(type: "decimal(14,4)", precision: 14, scale: 4, nullable: false),
                    MachineCost = table.Column<decimal>(type: "decimal(14,4)", precision: 14, scale: 4, nullable: false),
                    LaborCost = table.Column<decimal>(type: "decimal(14,4)", precision: 14, scale: 4, nullable: false),
                    FirePercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    FireCost = table.Column<decimal>(type: "decimal(14,4)", precision: 14, scale: 4, nullable: false),
                    OverheadPercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    OverheadCost = table.Column<decimal>(type: "decimal(14,4)", precision: 14, scale: 4, nullable: false),
                    UnitCost = table.Column<decimal>(type: "decimal(14,4)", precision: 14, scale: 4, nullable: false),
                    MarginPercent = table.Column<decimal>(type: "decimal(9,2)", precision: 9, scale: 2, nullable: true),
                    UnitPrice = table.Column<decimal>(type: "decimal(12,4)", precision: 12, scale: 4, nullable: false),
                    CostWarning = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuotationLines", x => x.Id);
                    table.CheckConstraint("CK_QuotationLines_BatchKilograms", "[BatchKilograms] > 0");
                    table.CheckConstraint("CK_QuotationLines_Item", "([FabricId] IS NOT NULL AND [YarnId] IS NULL) OR ([FabricId] IS NULL AND [YarnId] IS NOT NULL)");
                    table.CheckConstraint("CK_QuotationLines_MinutesSource", "[MinutesSource] IN ('MANUAL', 'ROUTE', 'DEFAULT')");
                    table.CheckConstraint("CK_QuotationLines_ProcessType", "[ProcessType] IN ('FABRIC_DYEING', 'YARN_DYEING', 'FINISHING', 'PRINTING')");
                    table.CheckConstraint("CK_QuotationLines_Quantity", "[Quantity] > 0");
                    table.CheckConstraint("CK_QuotationLines_Unit", "[Unit] IN ('METER', 'KILOGRAM')");
                    table.ForeignKey(
                        name: "FK_QuotationLines_ColorCards_ColorCardId",
                        column: x => x.ColorCardId,
                        principalSchema: "Boyahane",
                        principalTable: "ColorCards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QuotationLines_Fabrics_FabricId",
                        column: x => x.FabricId,
                        principalSchema: "Boyahane",
                        principalTable: "Fabrics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QuotationLines_LabTrials_RecipeTrialId",
                        column: x => x.RecipeTrialId,
                        principalSchema: "Boyahane",
                        principalTable: "LabTrials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QuotationLines_Machines_MachineId",
                        column: x => x.MachineId,
                        principalSchema: "Boyahane",
                        principalTable: "Machines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QuotationLines_PatternColorways_ColorwayId",
                        column: x => x.ColorwayId,
                        principalSchema: "Boyahane",
                        principalTable: "PatternColorways",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QuotationLines_Patterns_PatternId",
                        column: x => x.PatternId,
                        principalSchema: "Boyahane",
                        principalTable: "Patterns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QuotationLines_Quotations_QuotationId",
                        column: x => x.QuotationId,
                        principalSchema: "Boyahane",
                        principalTable: "Quotations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QuotationLines_Yarns_YarnId",
                        column: x => x.YarnId,
                        principalSchema: "Boyahane",
                        principalTable: "Yarns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_QuotationLines_ColorCardId",
                schema: "Boyahane",
                table: "QuotationLines",
                column: "ColorCardId");

            migrationBuilder.CreateIndex(
                name: "IX_QuotationLines_ColorwayId",
                schema: "Boyahane",
                table: "QuotationLines",
                column: "ColorwayId");

            migrationBuilder.CreateIndex(
                name: "IX_QuotationLines_FabricId",
                schema: "Boyahane",
                table: "QuotationLines",
                column: "FabricId");

            migrationBuilder.CreateIndex(
                name: "IX_QuotationLines_MachineId",
                schema: "Boyahane",
                table: "QuotationLines",
                column: "MachineId");

            migrationBuilder.CreateIndex(
                name: "IX_QuotationLines_PatternId",
                schema: "Boyahane",
                table: "QuotationLines",
                column: "PatternId");

            migrationBuilder.CreateIndex(
                name: "IX_QuotationLines_QuotationId",
                schema: "Boyahane",
                table: "QuotationLines",
                column: "QuotationId");

            migrationBuilder.CreateIndex(
                name: "IX_QuotationLines_RecipeTrialId",
                schema: "Boyahane",
                table: "QuotationLines",
                column: "RecipeTrialId");

            migrationBuilder.CreateIndex(
                name: "IX_QuotationLines_YarnId",
                schema: "Boyahane",
                table: "QuotationLines",
                column: "YarnId");

            migrationBuilder.CreateIndex(
                name: "IX_Quotations_CustomerId",
                schema: "Boyahane",
                table: "Quotations",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Quotations_OrderId",
                schema: "Boyahane",
                table: "Quotations",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_Quotations_QuotationDate",
                schema: "Boyahane",
                table: "Quotations",
                column: "QuotationDate");

            migrationBuilder.CreateIndex(
                name: "IX_Quotations_QuotationNo",
                schema: "Boyahane",
                table: "Quotations",
                column: "QuotationNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Quotations_Status",
                schema: "Boyahane",
                table: "Quotations",
                column: "Status");

            // Yeni yetkiler; varsayılan olarak yalnızca Sistem Yöneticisine verilir, diğer roller rol ekranından yetkilendirilir.
            migrationBuilder.Sql(@"SET IDENTITY_INSERT [Core].[Permissions] ON;
EXEC(N'INSERT INTO [Core].[Permissions] ([Id], [Code], [Module], [Description])
SELECT v.[Id], v.[Code], v.[Module], v.[Description] FROM (VALUES
(1039, N''Boyahane.Quotation.View'', N''Boyahane'', N''Fiyat tekliflerini, ön maliyeti ve maliyet parametrelerini görüntüleme''),
(1040, N''Boyahane.Quotation.Manage'', N''Boyahane'', N''Teklif hazırlama, gönderme, kabul / ret ve maliyet parametreleri'')
) v([Id], [Code], [Module], [Description])
WHERE NOT EXISTS (SELECT 1 FROM [Core].[Permissions] p WHERE p.[Id] = v.[Id] OR p.[Code] = v.[Code])');
SET IDENTITY_INSERT [Core].[Permissions] OFF;");

            migrationBuilder.Sql(@"INSERT INTO [Core].[RolePermissions] ([RoleId], [PermissionId])
SELECT v.[RoleId], v.[PermissionId] FROM (VALUES
(1, 1039), (1, 1040)
) v([RoleId], [PermissionId])
WHERE EXISTS (SELECT 1 FROM [Core].[Roles] r WHERE r.[Id] = v.[RoleId])
  AND EXISTS (SELECT 1 FROM [Core].[Permissions] p WHERE p.[Id] = v.[PermissionId])
  AND NOT EXISTS (SELECT 1 FROM [Core].[RolePermissions] rp WHERE rp.[RoleId] = v.[RoleId] AND rp.[PermissionId] = v.[PermissionId]);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DELETE FROM [Core].[RolePermissions] WHERE [PermissionId] IN (1039, 1040);
DELETE FROM [Core].[Permissions] WHERE [Id] IN (1039, 1040);");

            migrationBuilder.DropTable(
                name: "CostSettings",
                schema: "Boyahane");

            migrationBuilder.DropTable(
                name: "MachineCostRates",
                schema: "Boyahane");

            migrationBuilder.DropTable(
                name: "QuotationLines",
                schema: "Boyahane");

            migrationBuilder.DropTable(
                name: "Quotations",
                schema: "Boyahane");

            migrationBuilder.DropColumn(
                name: "PlanSequence",
                schema: "Boyahane",
                table: "Batches");
        }
    }
}
