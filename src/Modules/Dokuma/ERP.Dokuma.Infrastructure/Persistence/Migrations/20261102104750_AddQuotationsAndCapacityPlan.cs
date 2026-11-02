using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Dokuma.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddQuotationsAndCapacityPlan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PlanSequence",
                schema: "Dokuma",
                table: "Beams",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PlannedLoomId",
                schema: "Dokuma",
                table: "Beams",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CostSettings",
                schema: "Dokuma",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Currency = table.Column<string>(type: "char(3)", maxLength: 200, nullable: false, defaultValue: "TRY"),
                    LoomHourlyCost = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    LaborHourlyCost = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    WarpingCostPerMeter = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    SizingCostPerMeter = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    UseSizingRecipe = table.Column<bool>(type: "bit", nullable: false),
                    OverheadPercent = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: false),
                    DefaultWastePercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    DefaultMarginPercent = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: false),
                    DefaultEfficiencyPercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false, defaultValue: 85m),
                    QuotationValidityDays = table.Column<int>(type: "int", nullable: false, defaultValue: 15),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CostSettings", x => x.Id);
                    table.CheckConstraint("CK_CostSettings_Amounts", "[LoomHourlyCost] >= 0 AND [LaborHourlyCost] >= 0 AND [WarpingCostPerMeter] >= 0 AND [SizingCostPerMeter] >= 0");
                    table.CheckConstraint("CK_CostSettings_Efficiency", "[DefaultEfficiencyPercent] > 0 AND [DefaultEfficiencyPercent] <= 100");
                    table.CheckConstraint("CK_CostSettings_Percents", "[OverheadPercent] BETWEEN 0 AND 500 AND [DefaultWastePercent] BETWEEN 0 AND 100 AND [DefaultMarginPercent] BETWEEN 0 AND 500");
                    table.CheckConstraint("CK_CostSettings_ValidityDays", "[QuotationValidityDays] BETWEEN 1 AND 365");
                });

            migrationBuilder.CreateTable(
                name: "LoomCostRates",
                schema: "Dokuma",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LoomId = table.Column<int>(type: "int", nullable: false),
                    HourlyCost = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    EfficiencyPercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoomCostRates", x => x.Id);
                    table.CheckConstraint("CK_LoomCostRates_Efficiency", "[EfficiencyPercent] IS NULL OR ([EfficiencyPercent] > 0 AND [EfficiencyPercent] <= 100)");
                    table.CheckConstraint("CK_LoomCostRates_HourlyCost", "[HourlyCost] IS NULL OR [HourlyCost] >= 0");
                    table.ForeignKey(
                        name: "FK_LoomCostRates_Looms_LoomId",
                        column: x => x.LoomId,
                        principalSchema: "Dokuma",
                        principalTable: "Looms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Quotations",
                schema: "Dokuma",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    QuotationNo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    QuotationDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ValidUntil = table.Column<DateOnly>(type: "date", nullable: false),
                    CustomerId = table.Column<int>(type: "int", nullable: true),
                    ProspectName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ContactName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Ownership = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Currency = table.Column<string>(type: "char(3)", maxLength: 200, nullable: false, defaultValue: "TRY"),
                    DocumentLanguage = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    PaymentTermDays = table.Column<int>(type: "int", nullable: true),
                    DeliveryTerms = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DecidedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DecidedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
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
                    table.CheckConstraint("CK_Quotations_PaymentTermDays", "[PaymentTermDays] IS NULL OR [PaymentTermDays] BETWEEN 0 AND 365");
                    table.CheckConstraint("CK_Quotations_Status", "[Status] IN ('DRAFT', 'SENT', 'ACCEPTED', 'REJECTED', 'EXPIRED')");
                    table.CheckConstraint("CK_Quotations_ValidUntil", "[ValidUntil] >= [QuotationDate]");
                    table.ForeignKey(
                        name: "FK_Quotations_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "Dokuma",
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Quotations_Orders_OrderId",
                        column: x => x.OrderId,
                        principalSchema: "Dokuma",
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "QuotationLines",
                schema: "Dokuma",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    QuotationId = table.Column<int>(type: "int", nullable: false),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    ArticleId = table.Column<int>(type: "int", nullable: false),
                    Meters = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    CustomerArticleCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    LoomId = table.Column<int>(type: "int", nullable: true),
                    SpeedRpm = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: false),
                    EfficiencyPercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    PicksPerCm = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: false),
                    WarpKilogramsPerMeter = table.Column<decimal>(type: "decimal(12,6)", precision: 12, scale: 6, nullable: false),
                    WeftKilogramsPerMeter = table.Column<decimal>(type: "decimal(12,6)", precision: 12, scale: 6, nullable: false),
                    YarnCost = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    SizingCostPerWarpMeter = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    WarpingCostPerWarpMeter = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    SizingCost = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    WarpingCost = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    LoomHourlyCost = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    LaborHourlyCost = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    WeavingHoursPerMeter = table.Column<decimal>(type: "decimal(12,6)", precision: 12, scale: 6, nullable: false),
                    WeavingCost = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    WastePercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    OverheadPercent = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: false),
                    MarginPercent = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: false),
                    CostPerMeter = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    CalculatedPrice = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuotationLines", x => x.Id);
                    table.CheckConstraint("CK_QuotationLines_Meters", "[Meters] > 0");
                    table.CheckConstraint("CK_QuotationLines_Rates", "[SpeedRpm] > 0 AND [EfficiencyPercent] > 0 AND [EfficiencyPercent] <= 100 AND [PicksPerCm] > 0");
                    table.CheckConstraint("CK_QuotationLines_UnitPrice", "[UnitPrice] >= 0");
                    table.ForeignKey(
                        name: "FK_QuotationLines_Articles_ArticleId",
                        column: x => x.ArticleId,
                        principalSchema: "Dokuma",
                        principalTable: "Articles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QuotationLines_Looms_LoomId",
                        column: x => x.LoomId,
                        principalSchema: "Dokuma",
                        principalTable: "Looms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QuotationLines_Quotations_QuotationId",
                        column: x => x.QuotationId,
                        principalSchema: "Dokuma",
                        principalTable: "Quotations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "QuotationLineYarns",
                schema: "Dokuma",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LineId = table.Column<int>(type: "int", nullable: false),
                    YarnId = table.Column<int>(type: "int", nullable: false),
                    UsedInWarp = table.Column<bool>(type: "bit", nullable: false),
                    UsedInWeft = table.Column<bool>(type: "bit", nullable: false),
                    KilogramsPerMeter = table.Column<decimal>(type: "decimal(12,6)", precision: 12, scale: 6, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    PriceSource = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuotationLineYarns", x => x.Id);
                    table.CheckConstraint("CK_QuotationLineYarns_Kilograms", "[KilogramsPerMeter] >= 0");
                    table.CheckConstraint("CK_QuotationLineYarns_UnitPrice", "[UnitPrice] >= 0");
                    table.ForeignKey(
                        name: "FK_QuotationLineYarns_QuotationLines_LineId",
                        column: x => x.LineId,
                        principalSchema: "Dokuma",
                        principalTable: "QuotationLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QuotationLineYarns_Yarns_YarnId",
                        column: x => x.YarnId,
                        principalSchema: "Dokuma",
                        principalTable: "Yarns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Beams_PlannedLoomId_PlanSequence",
                schema: "Dokuma",
                table: "Beams",
                columns: new[] { "PlannedLoomId", "PlanSequence" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Beams_PlanSequence",
                schema: "Dokuma",
                table: "Beams",
                sql: "[PlanSequence] IS NULL OR [PlanSequence] >= 1");

            migrationBuilder.CreateIndex(
                name: "IX_LoomCostRates_LoomId",
                schema: "Dokuma",
                table: "LoomCostRates",
                column: "LoomId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QuotationLines_ArticleId",
                schema: "Dokuma",
                table: "QuotationLines",
                column: "ArticleId");

            migrationBuilder.CreateIndex(
                name: "IX_QuotationLines_LoomId",
                schema: "Dokuma",
                table: "QuotationLines",
                column: "LoomId");

            migrationBuilder.CreateIndex(
                name: "IX_QuotationLines_QuotationId_Sequence",
                schema: "Dokuma",
                table: "QuotationLines",
                columns: new[] { "QuotationId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QuotationLineYarns_LineId_YarnId",
                schema: "Dokuma",
                table: "QuotationLineYarns",
                columns: new[] { "LineId", "YarnId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QuotationLineYarns_YarnId",
                schema: "Dokuma",
                table: "QuotationLineYarns",
                column: "YarnId");

            migrationBuilder.CreateIndex(
                name: "IX_Quotations_CustomerId",
                schema: "Dokuma",
                table: "Quotations",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Quotations_OrderId",
                schema: "Dokuma",
                table: "Quotations",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_Quotations_QuotationDate",
                schema: "Dokuma",
                table: "Quotations",
                column: "QuotationDate");

            migrationBuilder.CreateIndex(
                name: "IX_Quotations_QuotationNo",
                schema: "Dokuma",
                table: "Quotations",
                column: "QuotationNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Quotations_Status",
                schema: "Dokuma",
                table: "Quotations",
                column: "Status");

            migrationBuilder.AddForeignKey(
                name: "FK_Beams_Looms_PlannedLoomId",
                schema: "Dokuma",
                table: "Beams",
                column: "PlannedLoomId",
                principalSchema: "Dokuma",
                principalTable: "Looms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // Yeni yetkiler; varsayılan olarak yalnızca Sistem Yöneticisine verilir, diğer roller rol ekranından yetkilendirilir.
            migrationBuilder.Sql(@"SET IDENTITY_INSERT [Core].[Permissions] ON;
EXEC(N'INSERT INTO [Core].[Permissions] ([Id], [Code], [Module], [Description])
SELECT v.[Id], v.[Code], v.[Module], v.[Description] FROM (VALUES
(2036, N''Dokuma.Quotation.View'', N''Dokuma'', N''Teklifleri, ön maliyet dökümünü ve maliyet parametrelerini görüntüleme''),
(2037, N''Dokuma.Quotation.Manage'', N''Dokuma'', N''Teklif hazırlama, gönderme, kabul / ret ve maliyet parametrelerini düzenleme''),
(2038, N''Dokuma.Planning.View'', N''Dokuma'', N''Tezgâh kapasite planını görüntüleme''),
(2039, N''Dokuma.Planning.Manage'', N''Dokuma'', N''Leventleri tezgâh kuyruğuna alma, sıralama ve başka tezgâha taşıma'')
) v([Id], [Code], [Module], [Description])
WHERE NOT EXISTS (SELECT 1 FROM [Core].[Permissions] p WHERE p.[Id] = v.[Id] OR p.[Code] = v.[Code])');
SET IDENTITY_INSERT [Core].[Permissions] OFF;");

            migrationBuilder.Sql(@"INSERT INTO [Core].[RolePermissions] ([RoleId], [PermissionId])
SELECT v.[RoleId], v.[PermissionId] FROM (VALUES
(1, 2036), (1, 2037), (1, 2038), (1, 2039)
) v([RoleId], [PermissionId])
WHERE EXISTS (SELECT 1 FROM [Core].[Roles] r WHERE r.[Id] = v.[RoleId])
  AND EXISTS (SELECT 1 FROM [Core].[Permissions] p WHERE p.[Id] = v.[PermissionId])
  AND NOT EXISTS (SELECT 1 FROM [Core].[RolePermissions] rp WHERE rp.[RoleId] = v.[RoleId] AND rp.[PermissionId] = v.[PermissionId]);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DELETE FROM [Core].[RolePermissions] WHERE [PermissionId] IN (2036, 2037, 2038, 2039);
DELETE FROM [Core].[Permissions] WHERE [Id] IN (2036, 2037, 2038, 2039);");

            migrationBuilder.DropForeignKey(
                name: "FK_Beams_Looms_PlannedLoomId",
                schema: "Dokuma",
                table: "Beams");

            migrationBuilder.DropTable(
                name: "CostSettings",
                schema: "Dokuma");

            migrationBuilder.DropTable(
                name: "LoomCostRates",
                schema: "Dokuma");

            migrationBuilder.DropTable(
                name: "QuotationLineYarns",
                schema: "Dokuma");

            migrationBuilder.DropTable(
                name: "QuotationLines",
                schema: "Dokuma");

            migrationBuilder.DropTable(
                name: "Quotations",
                schema: "Dokuma");

            migrationBuilder.DropIndex(
                name: "IX_Beams_PlannedLoomId_PlanSequence",
                schema: "Dokuma",
                table: "Beams");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Beams_PlanSequence",
                schema: "Dokuma",
                table: "Beams");

            migrationBuilder.DropColumn(
                name: "PlanSequence",
                schema: "Dokuma",
                table: "Beams");

            migrationBuilder.DropColumn(
                name: "PlannedLoomId",
                schema: "Dokuma",
                table: "Beams");
        }
    }
}
