using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Konfeksiyon.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSubcontractingAndQuotations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsSubcontracted",
                schema: "Konfeksiyon",
                table: "ProductionRouteOperations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "SubcontractorId",
                schema: "Konfeksiyon",
                table: "ProductionRouteOperations",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AtSubcontractor",
                schema: "Konfeksiyon",
                table: "Bundles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "CostSettings",
                schema: "Konfeksiyon",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    LaborCostPerMinute = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    EnergyCostPerMinute = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Currency = table.Column<string>(type: "char(3)", maxLength: 200, nullable: false),
                    OverheadPercent = table.Column<decimal>(type: "decimal(7,2)", precision: 7, scale: 2, nullable: false),
                    DefaultFirePercent = table.Column<decimal>(type: "decimal(7,2)", precision: 7, scale: 2, nullable: false),
                    DefaultMarginPercent = table.Column<decimal>(type: "decimal(7,2)", precision: 7, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CostSettings", x => x.Id);
                    table.CheckConstraint("CK_CostSettings_Percents", "[OverheadPercent] >= 0 AND [DefaultFirePercent] >= 0 AND [DefaultMarginPercent] >= 0");
                    table.CheckConstraint("CK_CostSettings_Rates", "[LaborCostPerMinute] >= 0 AND [EnergyCostPerMinute] >= 0");
                });

            migrationBuilder.CreateTable(
                name: "Quotations",
                schema: "Konfeksiyon",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    QuotationNo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CustomerId = table.Column<int>(type: "int", nullable: true),
                    ProspectName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    QuotationDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ValidUntil = table.Column<DateOnly>(type: "date", nullable: false),
                    Currency = table.Column<string>(type: "char(3)", maxLength: 200, nullable: false),
                    DocumentLanguage = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    LaborCostPerMinute = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    EnergyCostPerMinute = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DecidedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DecidedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SalesOrderId = table.Column<int>(type: "int", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
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
                    table.PrimaryKey("PK_Quotations", x => x.Id);
                    table.CheckConstraint("CK_Quotations_DocumentLanguage", "[DocumentLanguage] IN ('TURKISH', 'ENGLISH')");
                    table.CheckConstraint("CK_Quotations_Party", "[CustomerId] IS NOT NULL OR [ProspectName] IS NOT NULL");
                    table.CheckConstraint("CK_Quotations_Status", "[Status] IN ('DRAFT', 'SENT', 'ACCEPTED', 'REJECTED', 'EXPIRED')");
                    table.CheckConstraint("CK_Quotations_Validity", "[ValidUntil] >= [QuotationDate]");
                    table.ForeignKey(
                        name: "FK_Quotations_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Quotations_SalesOrders_SalesOrderId",
                        column: x => x.SalesOrderId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "SalesOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SubcontractPrices",
                schema: "Konfeksiyon",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SupplierId = table.Column<int>(type: "int", nullable: false),
                    OperationId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: true),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Currency = table.Column<string>(type: "char(3)", maxLength: 200, nullable: false),
                    ValidFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubcontractPrices", x => x.Id);
                    table.CheckConstraint("CK_SubcontractPrices_UnitPrice", "[UnitPrice] >= 0");
                    table.ForeignKey(
                        name: "FK_SubcontractPrices_Operations_OperationId",
                        column: x => x.OperationId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "Operations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SubcontractPrices_Products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SubcontractPrices_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SubcontractSends",
                schema: "Konfeksiyon",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SendNo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    SupplierId = table.Column<int>(type: "int", nullable: false),
                    SendDate = table.Column<DateOnly>(type: "date", nullable: false),
                    OperationId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Carrier = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubcontractSends", x => x.Id);
                    table.CheckConstraint("CK_SubcontractSends_Status", "[Status] IN ('OPEN', 'PARTIALLY_RETURNED', 'CLOSED', 'CANCELLED')");
                    table.ForeignKey(
                        name: "FK_SubcontractSends_Operations_OperationId",
                        column: x => x.OperationId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "Operations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SubcontractSends_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "QuotationLines",
                schema: "Konfeksiyon",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    QuotationId = table.Column<int>(type: "int", nullable: false),
                    LineNo = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ModelReference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Colors = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    ManualMinutes = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: true),
                    ManualSubcontractCost = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    PriceOverridden = table.Column<bool>(type: "bit", nullable: false),
                    FirePercent = table.Column<decimal>(type: "decimal(7,2)", precision: 7, scale: 2, nullable: false),
                    OverheadPercent = table.Column<decimal>(type: "decimal(7,2)", precision: 7, scale: 2, nullable: false),
                    MarginPercent = table.Column<decimal>(type: "decimal(9,2)", precision: 9, scale: 2, nullable: false),
                    RouteMinutes = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: true),
                    RouteSubcontractCost = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    MaterialCost = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    LaborMinutes = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: false),
                    LaborCost = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    EnergyCost = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    SubcontractCost = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    FireCost = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    OverheadCost = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    UnitCost = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Warnings = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuotationLines", x => x.Id);
                    table.CheckConstraint("CK_QuotationLines_Item", "[ProductId] IS NOT NULL OR [Description] IS NOT NULL");
                    table.CheckConstraint("CK_QuotationLines_Quantity", "[Quantity] > 0");
                    table.ForeignKey(
                        name: "FK_QuotationLines_Products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QuotationLines_Quotations_QuotationId",
                        column: x => x.QuotationId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "Quotations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SubcontractReturns",
                schema: "Konfeksiyon",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReturnNo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    SubcontractSendId = table.Column<int>(type: "int", nullable: false),
                    SupplierId = table.Column<int>(type: "int", nullable: false),
                    ReturnDate = table.Column<DateOnly>(type: "date", nullable: false),
                    SupplierDocumentNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    PaidDate = table.Column<DateOnly>(type: "date", nullable: true),
                    PaidBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PaymentNote = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubcontractReturns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubcontractReturns_SubcontractSends_SubcontractSendId",
                        column: x => x.SubcontractSendId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "SubcontractSends",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SubcontractReturns_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SubcontractSendLines",
                schema: "Konfeksiyon",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SubcontractSendId = table.Column<int>(type: "int", nullable: false),
                    BundleId = table.Column<int>(type: "int", nullable: true),
                    BundleOperationId = table.Column<long>(type: "bigint", nullable: true),
                    ProductionOrderId = table.Column<int>(type: "int", nullable: true),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    ColorId = table.Column<int>(type: "int", nullable: false),
                    SizeId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    ReturnedGood = table.Column<int>(type: "int", nullable: false),
                    ReturnedDefective = table.Column<int>(type: "int", nullable: false),
                    ReturnedMissing = table.Column<int>(type: "int", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    Currency = table.Column<string>(type: "char(3)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubcontractSendLines", x => x.Id);
                    table.CheckConstraint("CK_SubcontractSendLines_Quantity", "[Quantity] > 0");
                    table.CheckConstraint("CK_SubcontractSendLines_Returned", "[ReturnedGood] >= 0 AND [ReturnedDefective] >= 0 AND [ReturnedMissing] >= 0 AND [ReturnedGood] + [ReturnedDefective] + [ReturnedMissing] <= [Quantity]");
                    table.CheckConstraint("CK_SubcontractSendLines_UnitPrice", "[UnitPrice] IS NULL OR [UnitPrice] >= 0");
                    table.ForeignKey(
                        name: "FK_SubcontractSendLines_BundleOperations_BundleOperationId",
                        column: x => x.BundleOperationId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "BundleOperations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SubcontractSendLines_Bundles_BundleId",
                        column: x => x.BundleId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "Bundles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SubcontractSendLines_Colors_ColorId",
                        column: x => x.ColorId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "Colors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SubcontractSendLines_ProductionOrders_ProductionOrderId",
                        column: x => x.ProductionOrderId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "ProductionOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SubcontractSendLines_Products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SubcontractSendLines_Sizes_SizeId",
                        column: x => x.SizeId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "Sizes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SubcontractSendLines_SubcontractSends_SubcontractSendId",
                        column: x => x.SubcontractSendId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "SubcontractSends",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "QuotationLineMaterials",
                schema: "Konfeksiyon",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    QuotationLineId = table.Column<int>(type: "int", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    StockItemId = table.Column<int>(type: "int", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    QuantityPerPiece = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    UnitCost = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    UnitCostOverridden = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuotationLineMaterials", x => x.Id);
                    table.CheckConstraint("CK_QuotationLineMaterials_Values", "[QuantityPerPiece] >= 0 AND [UnitCost] >= 0");
                    table.ForeignKey(
                        name: "FK_QuotationLineMaterials_QuotationLines_QuotationLineId",
                        column: x => x.QuotationLineId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "QuotationLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QuotationLineMaterials_StockItems_StockItemId",
                        column: x => x.StockItemId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "StockItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SubcontractReturnLines",
                schema: "Konfeksiyon",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SubcontractReturnId = table.Column<int>(type: "int", nullable: false),
                    SubcontractSendLineId = table.Column<int>(type: "int", nullable: false),
                    GoodQuantity = table.Column<int>(type: "int", nullable: false),
                    DefectiveQuantity = table.Column<int>(type: "int", nullable: false),
                    MissingQuantity = table.Column<int>(type: "int", nullable: false),
                    DefectTypeId = table.Column<int>(type: "int", nullable: true),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    Currency = table.Column<string>(type: "char(3)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubcontractReturnLines", x => x.Id);
                    table.CheckConstraint("CK_SubcontractReturnLines_Quantities", "[GoodQuantity] >= 0 AND [DefectiveQuantity] >= 0 AND [MissingQuantity] >= 0 AND [GoodQuantity] + [DefectiveQuantity] + [MissingQuantity] > 0");
                    table.CheckConstraint("CK_SubcontractReturnLines_UnitPrice", "[UnitPrice] IS NULL OR [UnitPrice] >= 0");
                    table.ForeignKey(
                        name: "FK_SubcontractReturnLines_DefectTypes_DefectTypeId",
                        column: x => x.DefectTypeId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "DefectTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SubcontractReturnLines_SubcontractReturns_SubcontractReturnId",
                        column: x => x.SubcontractReturnId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "SubcontractReturns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SubcontractReturnLines_SubcontractSendLines_SubcontractSendLineId",
                        column: x => x.SubcontractSendLineId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "SubcontractSendLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "Konfeksiyon",
                table: "CostSettings",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "Currency", "DefaultFirePercent", "DefaultMarginPercent", "EnergyCostPerMinute", "LaborCostPerMinute", "OverheadPercent", "UpdatedAt", "UpdatedBy" },
                values: new object[] { 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "system", "TRY", 0m, 0m, 0m, 0m, 0m, null, null });

            migrationBuilder.CreateIndex(
                name: "IX_ProductionRouteOperations_SubcontractorId",
                schema: "Konfeksiyon",
                table: "ProductionRouteOperations",
                column: "SubcontractorId");

            migrationBuilder.CreateIndex(
                name: "IX_QuotationLineMaterials_QuotationLineId",
                schema: "Konfeksiyon",
                table: "QuotationLineMaterials",
                column: "QuotationLineId");

            migrationBuilder.CreateIndex(
                name: "IX_QuotationLineMaterials_StockItemId",
                schema: "Konfeksiyon",
                table: "QuotationLineMaterials",
                column: "StockItemId");

            migrationBuilder.CreateIndex(
                name: "IX_QuotationLines_ProductId",
                schema: "Konfeksiyon",
                table: "QuotationLines",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_QuotationLines_QuotationId_LineNo",
                schema: "Konfeksiyon",
                table: "QuotationLines",
                columns: new[] { "QuotationId", "LineNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Quotations_CustomerId_QuotationDate",
                schema: "Konfeksiyon",
                table: "Quotations",
                columns: new[] { "CustomerId", "QuotationDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Quotations_QuotationNo",
                schema: "Konfeksiyon",
                table: "Quotations",
                column: "QuotationNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Quotations_SalesOrderId",
                schema: "Konfeksiyon",
                table: "Quotations",
                column: "SalesOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_Quotations_Status",
                schema: "Konfeksiyon",
                table: "Quotations",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_SubcontractPrices_OperationId",
                schema: "Konfeksiyon",
                table: "SubcontractPrices",
                column: "OperationId");

            migrationBuilder.CreateIndex(
                name: "IX_SubcontractPrices_ProductId",
                schema: "Konfeksiyon",
                table: "SubcontractPrices",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_SubcontractPrices_SupplierId_OperationId_ProductId_ValidFrom",
                schema: "Konfeksiyon",
                table: "SubcontractPrices",
                columns: new[] { "SupplierId", "OperationId", "ProductId", "ValidFrom" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SubcontractReturnLines_DefectTypeId",
                schema: "Konfeksiyon",
                table: "SubcontractReturnLines",
                column: "DefectTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_SubcontractReturnLines_SubcontractReturnId",
                schema: "Konfeksiyon",
                table: "SubcontractReturnLines",
                column: "SubcontractReturnId");

            migrationBuilder.CreateIndex(
                name: "IX_SubcontractReturnLines_SubcontractSendLineId",
                schema: "Konfeksiyon",
                table: "SubcontractReturnLines",
                column: "SubcontractSendLineId");

            migrationBuilder.CreateIndex(
                name: "IX_SubcontractReturns_ReturnDate",
                schema: "Konfeksiyon",
                table: "SubcontractReturns",
                column: "ReturnDate");

            migrationBuilder.CreateIndex(
                name: "IX_SubcontractReturns_ReturnNo",
                schema: "Konfeksiyon",
                table: "SubcontractReturns",
                column: "ReturnNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SubcontractReturns_SubcontractSendId",
                schema: "Konfeksiyon",
                table: "SubcontractReturns",
                column: "SubcontractSendId");

            migrationBuilder.CreateIndex(
                name: "IX_SubcontractReturns_SupplierId_ReturnDate",
                schema: "Konfeksiyon",
                table: "SubcontractReturns",
                columns: new[] { "SupplierId", "ReturnDate" });

            migrationBuilder.CreateIndex(
                name: "IX_SubcontractSendLines_BundleId",
                schema: "Konfeksiyon",
                table: "SubcontractSendLines",
                column: "BundleId");

            migrationBuilder.CreateIndex(
                name: "IX_SubcontractSendLines_BundleOperationId",
                schema: "Konfeksiyon",
                table: "SubcontractSendLines",
                column: "BundleOperationId");

            migrationBuilder.CreateIndex(
                name: "IX_SubcontractSendLines_ColorId",
                schema: "Konfeksiyon",
                table: "SubcontractSendLines",
                column: "ColorId");

            migrationBuilder.CreateIndex(
                name: "IX_SubcontractSendLines_ProductId",
                schema: "Konfeksiyon",
                table: "SubcontractSendLines",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_SubcontractSendLines_ProductionOrderId",
                schema: "Konfeksiyon",
                table: "SubcontractSendLines",
                column: "ProductionOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_SubcontractSendLines_SizeId",
                schema: "Konfeksiyon",
                table: "SubcontractSendLines",
                column: "SizeId");

            migrationBuilder.CreateIndex(
                name: "IX_SubcontractSendLines_SubcontractSendId",
                schema: "Konfeksiyon",
                table: "SubcontractSendLines",
                column: "SubcontractSendId");

            migrationBuilder.CreateIndex(
                name: "IX_SubcontractSends_OperationId",
                schema: "Konfeksiyon",
                table: "SubcontractSends",
                column: "OperationId");

            migrationBuilder.CreateIndex(
                name: "IX_SubcontractSends_SendNo",
                schema: "Konfeksiyon",
                table: "SubcontractSends",
                column: "SendNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SubcontractSends_Status",
                schema: "Konfeksiyon",
                table: "SubcontractSends",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_SubcontractSends_SupplierId_Status",
                schema: "Konfeksiyon",
                table: "SubcontractSends",
                columns: new[] { "SupplierId", "Status" });

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionRouteOperations_Suppliers_SubcontractorId",
                schema: "Konfeksiyon",
                table: "ProductionRouteOperations",
                column: "SubcontractorId",
                principalSchema: "Konfeksiyon",
                principalTable: "Suppliers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // Yeni yetkiler; varsayılan olarak yalnızca Sistem Yöneticisine verilir, diğer roller rol ekranından yetkilendirilir.
            migrationBuilder.Sql(@"SET IDENTITY_INSERT [Core].[Permissions] ON;
EXEC(N'INSERT INTO [Core].[Permissions] ([Id], [Code], [Module], [Description])
SELECT v.[Id], v.[Code], v.[Module], v.[Description] FROM (VALUES
(99, N''Quotation.View'', N''Quotation'', N''Teklifleri ve ön maliyet dökümünü görüntüleme''),
(100, N''Quotation.Manage'', N''Quotation'', N''Teklif hazırlama, gönderme, kabul ve ön maliyet parametreleri''),
(101, N''Subcontract.View'', N''Subcontract'', N''Fason gönderim / dönüş, fason fiyatı ve hakedişleri görüntüleme''),
(102, N''Subcontract.Manage'', N''Subcontract'', N''Fason gönderim / dönüş fişi, fason fiyatı ve hakediş ödemesi'')
) v([Id], [Code], [Module], [Description])
WHERE NOT EXISTS (SELECT 1 FROM [Core].[Permissions] p WHERE p.[Id] = v.[Id] OR p.[Code] = v.[Code])');
SET IDENTITY_INSERT [Core].[Permissions] OFF;");

            migrationBuilder.Sql(@"INSERT INTO [Core].[RolePermissions] ([RoleId], [PermissionId])
SELECT v.[RoleId], v.[PermissionId] FROM (VALUES
(1, 99), (1, 100), (1, 101), (1, 102)
) v([RoleId], [PermissionId])
WHERE EXISTS (SELECT 1 FROM [Core].[Roles] r WHERE r.[Id] = v.[RoleId])
  AND EXISTS (SELECT 1 FROM [Core].[Permissions] p WHERE p.[Id] = v.[PermissionId])
  AND NOT EXISTS (SELECT 1 FROM [Core].[RolePermissions] rp WHERE rp.[RoleId] = v.[RoleId] AND rp.[PermissionId] = v.[PermissionId]);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DELETE FROM [Core].[RolePermissions] WHERE [PermissionId] IN (99, 100, 101, 102);
DELETE FROM [Core].[Permissions] WHERE [Id] IN (99, 100, 101, 102);");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductionRouteOperations_Suppliers_SubcontractorId",
                schema: "Konfeksiyon",
                table: "ProductionRouteOperations");

            migrationBuilder.DropTable(
                name: "CostSettings",
                schema: "Konfeksiyon");

            migrationBuilder.DropTable(
                name: "QuotationLineMaterials",
                schema: "Konfeksiyon");

            migrationBuilder.DropTable(
                name: "SubcontractPrices",
                schema: "Konfeksiyon");

            migrationBuilder.DropTable(
                name: "SubcontractReturnLines",
                schema: "Konfeksiyon");

            migrationBuilder.DropTable(
                name: "QuotationLines",
                schema: "Konfeksiyon");

            migrationBuilder.DropTable(
                name: "SubcontractReturns",
                schema: "Konfeksiyon");

            migrationBuilder.DropTable(
                name: "SubcontractSendLines",
                schema: "Konfeksiyon");

            migrationBuilder.DropTable(
                name: "Quotations",
                schema: "Konfeksiyon");

            migrationBuilder.DropTable(
                name: "SubcontractSends",
                schema: "Konfeksiyon");

            migrationBuilder.DropIndex(
                name: "IX_ProductionRouteOperations_SubcontractorId",
                schema: "Konfeksiyon",
                table: "ProductionRouteOperations");

            migrationBuilder.DropColumn(
                name: "IsSubcontracted",
                schema: "Konfeksiyon",
                table: "ProductionRouteOperations");

            migrationBuilder.DropColumn(
                name: "SubcontractorId",
                schema: "Konfeksiyon",
                table: "ProductionRouteOperations");

            migrationBuilder.DropColumn(
                name: "AtSubcontractor",
                schema: "Konfeksiyon",
                table: "Bundles");
        }
    }
}
