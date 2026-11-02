using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Orme.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddQuotations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CostSettings",
                schema: "Orme",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MachineHourlyCost = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    LaborHourlyCost = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    OverheadPercent = table.Column<decimal>(type: "decimal(7,2)", precision: 7, scale: 2, nullable: false),
                    DefaultWastePercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    DefaultMarginPercent = table.Column<decimal>(type: "decimal(7,2)", precision: 7, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CostSettings", x => x.Id);
                    table.CheckConstraint("CK_CostSettings_HourlyCosts", "[MachineHourlyCost] >= 0 AND [LaborHourlyCost] >= 0");
                    table.CheckConstraint("CK_CostSettings_Percents", "[OverheadPercent] BETWEEN 0 AND 1000 AND [DefaultWastePercent] BETWEEN 0 AND 100 AND [DefaultMarginPercent] BETWEEN 0 AND 1000");
                });

            migrationBuilder.CreateTable(
                name: "MachineCostRates",
                schema: "Orme",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MachineId = table.Column<int>(type: "int", nullable: false),
                    HourlyCost = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MachineCostRates", x => x.Id);
                    table.CheckConstraint("CK_MachineCostRates_HourlyCost", "[HourlyCost] >= 0");
                    table.ForeignKey(
                        name: "FK_MachineCostRates_Machines_MachineId",
                        column: x => x.MachineId,
                        principalSchema: "Orme",
                        principalTable: "Machines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Quotations",
                schema: "Orme",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    QuotationNo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    QuotationDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ValidUntil = table.Column<DateOnly>(type: "date", nullable: false),
                    CustomerId = table.Column<int>(type: "int", nullable: true),
                    ProspectName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Currency = table.Column<string>(type: "char(3)", maxLength: 200, nullable: false, defaultValue: "TRY"),
                    DocumentLanguage = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Quotations", x => x.Id);
                    table.CheckConstraint("CK_Quotations_DocumentLanguage", "[DocumentLanguage] IN ('TURKISH', 'ENGLISH')");
                    table.CheckConstraint("CK_Quotations_Party", "[CustomerId] IS NOT NULL OR [ProspectName] IS NOT NULL");
                    table.CheckConstraint("CK_Quotations_Status", "[Status] IN ('DRAFT', 'SENT', 'ACCEPTED', 'REJECTED', 'EXPIRED')");
                    table.CheckConstraint("CK_Quotations_ValidUntil", "[ValidUntil] >= [QuotationDate]");
                    table.ForeignKey(
                        name: "FK_Quotations_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "Orme",
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "QuotationLines",
                schema: "Orme",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    QuotationId = table.Column<int>(type: "int", nullable: false),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    FabricId = table.Column<int>(type: "int", nullable: false),
                    Kilograms = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    IsCommission = table.Column<bool>(type: "bit", nullable: false),
                    MachineId = table.Column<int>(type: "int", nullable: true),
                    WastePercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    KilogramsPerHour = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: true),
                    MachineHourlyCost = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    LaborHourlyCost = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    OverheadPercent = table.Column<decimal>(type: "decimal(7,2)", precision: 7, scale: 2, nullable: false),
                    MarginPercent = table.Column<decimal>(type: "decimal(9,2)", precision: 9, scale: 2, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    CustomerFabricCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    OrderLineId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuotationLines", x => x.Id);
                    table.CheckConstraint("CK_QuotationLines_Costs", "[MachineHourlyCost] >= 0 AND [LaborHourlyCost] >= 0");
                    table.CheckConstraint("CK_QuotationLines_Kilograms", "[Kilograms] > 0");
                    table.CheckConstraint("CK_QuotationLines_KilogramsPerHour", "[KilogramsPerHour] IS NULL OR [KilogramsPerHour] > 0");
                    table.CheckConstraint("CK_QuotationLines_Percents", "[WastePercent] BETWEEN 0 AND 100 AND [OverheadPercent] BETWEEN 0 AND 1000 AND [MarginPercent] >= -100");
                    table.CheckConstraint("CK_QuotationLines_UnitPrice", "[UnitPrice] >= 0");
                    table.ForeignKey(
                        name: "FK_QuotationLines_Fabrics_FabricId",
                        column: x => x.FabricId,
                        principalSchema: "Orme",
                        principalTable: "Fabrics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QuotationLines_Machines_MachineId",
                        column: x => x.MachineId,
                        principalSchema: "Orme",
                        principalTable: "Machines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QuotationLines_OrderLines_OrderLineId",
                        column: x => x.OrderLineId,
                        principalSchema: "Orme",
                        principalTable: "OrderLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QuotationLines_Quotations_QuotationId",
                        column: x => x.QuotationId,
                        principalSchema: "Orme",
                        principalTable: "Quotations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "QuotationLineYarns",
                schema: "Orme",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LineId = table.Column<int>(type: "int", nullable: false),
                    YarnId = table.Column<int>(type: "int", nullable: false),
                    Percent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    PriceSource = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    PriceReference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuotationLineYarns", x => x.Id);
                    table.CheckConstraint("CK_QuotationLineYarns_Percent", "[Percent] > 0 AND [Percent] <= 100");
                    table.CheckConstraint("CK_QuotationLineYarns_PriceSource", "[PriceSource] IN ('NONE', 'PURCHASE_ORDER', 'YARN_LOT', 'MANUAL')");
                    table.CheckConstraint("CK_QuotationLineYarns_UnitPrice", "[UnitPrice] IS NULL OR [UnitPrice] >= 0");
                    table.ForeignKey(
                        name: "FK_QuotationLineYarns_QuotationLines_LineId",
                        column: x => x.LineId,
                        principalSchema: "Orme",
                        principalTable: "QuotationLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QuotationLineYarns_Yarns_YarnId",
                        column: x => x.YarnId,
                        principalSchema: "Orme",
                        principalTable: "Yarns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MachineCostRates_MachineId",
                schema: "Orme",
                table: "MachineCostRates",
                column: "MachineId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QuotationLines_FabricId",
                schema: "Orme",
                table: "QuotationLines",
                column: "FabricId");

            migrationBuilder.CreateIndex(
                name: "IX_QuotationLines_MachineId",
                schema: "Orme",
                table: "QuotationLines",
                column: "MachineId");

            migrationBuilder.CreateIndex(
                name: "IX_QuotationLines_OrderLineId",
                schema: "Orme",
                table: "QuotationLines",
                column: "OrderLineId",
                filter: "[OrderLineId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_QuotationLines_QuotationId_Sequence",
                schema: "Orme",
                table: "QuotationLines",
                columns: new[] { "QuotationId", "Sequence" });

            migrationBuilder.CreateIndex(
                name: "IX_QuotationLineYarns_LineId",
                schema: "Orme",
                table: "QuotationLineYarns",
                column: "LineId");

            migrationBuilder.CreateIndex(
                name: "IX_QuotationLineYarns_YarnId",
                schema: "Orme",
                table: "QuotationLineYarns",
                column: "YarnId");

            migrationBuilder.CreateIndex(
                name: "IX_Quotations_CustomerId",
                schema: "Orme",
                table: "Quotations",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Quotations_QuotationDate",
                schema: "Orme",
                table: "Quotations",
                column: "QuotationDate");

            migrationBuilder.CreateIndex(
                name: "IX_Quotations_QuotationNo",
                schema: "Orme",
                table: "Quotations",
                column: "QuotationNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Quotations_Status_ValidUntil",
                schema: "Orme",
                table: "Quotations",
                columns: new[] { "Status", "ValidUntil" });

            // Yeni yetkiler; varsayılan olarak yalnızca Sistem Yöneticisine verilir, diğer roller rol ekranından yetkilendirilir.
            migrationBuilder.Sql(@"SET IDENTITY_INSERT [Core].[Permissions] ON;
EXEC(N'INSERT INTO [Core].[Permissions] ([Id], [Code], [Module], [Description])
SELECT v.[Id], v.[Code], v.[Module], v.[Description] FROM (VALUES
(3032, N''Orme.Quotation.View'', N''Orme'', N''Teklifleri, ön maliyeti ve maliyet parametrelerini görüntüleme''),
(3033, N''Orme.Quotation.Manage'', N''Orme'', N''Teklif hazırlama, gönderme, kabul / ret ve maliyet parametrelerini düzenleme'')
) v([Id], [Code], [Module], [Description])
WHERE NOT EXISTS (SELECT 1 FROM [Core].[Permissions] p WHERE p.[Id] = v.[Id] OR p.[Code] = v.[Code])');
SET IDENTITY_INSERT [Core].[Permissions] OFF;");

            migrationBuilder.Sql(@"INSERT INTO [Core].[RolePermissions] ([RoleId], [PermissionId])
SELECT v.[RoleId], v.[PermissionId] FROM (VALUES
(1, 3032), (1, 3033)
) v([RoleId], [PermissionId])
WHERE EXISTS (SELECT 1 FROM [Core].[Roles] r WHERE r.[Id] = v.[RoleId])
  AND EXISTS (SELECT 1 FROM [Core].[Permissions] p WHERE p.[Id] = v.[PermissionId])
  AND NOT EXISTS (SELECT 1 FROM [Core].[RolePermissions] rp WHERE rp.[RoleId] = v.[RoleId] AND rp.[PermissionId] = v.[PermissionId]);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DELETE FROM [Core].[RolePermissions] WHERE [PermissionId] IN (3032, 3033);
DELETE FROM [Core].[Permissions] WHERE [Id] IN (3032, 3033);");

            migrationBuilder.DropTable(
                name: "CostSettings",
                schema: "Orme");

            migrationBuilder.DropTable(
                name: "MachineCostRates",
                schema: "Orme");

            migrationBuilder.DropTable(
                name: "QuotationLineYarns",
                schema: "Orme");

            migrationBuilder.DropTable(
                name: "QuotationLines",
                schema: "Orme");

            migrationBuilder.DropTable(
                name: "Quotations",
                schema: "Orme");
        }
    }
}
