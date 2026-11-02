using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Dokuma.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddShippingAndAccounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "SecondQualityDiscountPercent",
                schema: "Dokuma",
                table: "Customers",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Invoices",
                schema: "Dokuma",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InvoiceNo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    InvoiceDate = table.Column<DateOnly>(type: "date", nullable: false),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    VatRate = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    WithholdingPercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    OpeningAmount = table.Column<decimal>(type: "decimal(16,2)", precision: 16, scale: 2, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Invoices", x => x.Id);
                    table.CheckConstraint("CK_Invoices_Status", "[Status] IN ('DRAFT', 'CONFIRMED', 'CANCELLED')");
                    table.CheckConstraint("CK_Invoices_VatRate", "[VatRate] IS NULL OR [VatRate] BETWEEN 0 AND 100");
                    table.CheckConstraint("CK_Invoices_Withholding", "[WithholdingPercent] IS NULL OR [WithholdingPercent] BETWEEN 0 AND 100");
                    table.ForeignKey(
                        name: "FK_Invoices_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "Dokuma",
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Payments",
                schema: "Dokuma",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PaymentNo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    PaymentDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Method = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(16,2)", precision: 16, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    ReferenceNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CancelReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payments", x => x.Id);
                    table.CheckConstraint("CK_Payments_Amount", "[Amount] > 0");
                    table.CheckConstraint("CK_Payments_Method", "[Method] IN ('CASH', 'BANK_TRANSFER', 'CHECK', 'OPENING')");
                    table.CheckConstraint("CK_Payments_Status", "[Status] IN ('ACTIVE', 'CANCELLED')");
                    table.ForeignKey(
                        name: "FK_Payments_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "Dokuma",
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "YarnReturns",
                schema: "Dokuma",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReturnNo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    YarnLotId = table.Column<int>(type: "int", nullable: false),
                    ReturnDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Kilograms = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_YarnReturns", x => x.Id);
                    table.CheckConstraint("CK_YarnReturns_Kilograms", "[Kilograms] > 0");
                    table.ForeignKey(
                        name: "FK_YarnReturns_YarnLots_YarnLotId",
                        column: x => x.YarnLotId,
                        principalSchema: "Dokuma",
                        principalTable: "YarnLots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Despatches",
                schema: "Dokuma",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DespatchNo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    DespatchDate = table.Column<DateOnly>(type: "date", nullable: false),
                    VehiclePlate = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    DriverName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DeliveryAddress = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    InvoiceId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Despatches", x => x.Id);
                    table.CheckConstraint("CK_Despatches_Status", "[Status] IN ('DRAFT', 'CONFIRMED', 'CANCELLED')");
                    table.ForeignKey(
                        name: "FK_Despatches_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "Dokuma",
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Despatches_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalSchema: "Dokuma",
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InvoiceLines",
                schema: "Dokuma",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InvoiceId = table.Column<int>(type: "int", nullable: false),
                    OrderLineId = table.Column<int>(type: "int", nullable: false),
                    Grade = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(14,4)", precision: 14, scale: 4, nullable: true),
                    DiscountPercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceLines", x => x.Id);
                    table.CheckConstraint("CK_InvoiceLines_Discount", "[DiscountPercent] IS NULL OR [DiscountPercent] BETWEEN 0 AND 100");
                    table.CheckConstraint("CK_InvoiceLines_Grade", "[Grade] IN ('FIRST_QUALITY', 'SECOND_QUALITY')");
                    table.ForeignKey(
                        name: "FK_InvoiceLines_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalSchema: "Dokuma",
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InvoiceLines_OrderLines_OrderLineId",
                        column: x => x.OrderLineId,
                        principalSchema: "Dokuma",
                        principalTable: "OrderLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PaymentAllocations",
                schema: "Dokuma",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PaymentId = table.Column<int>(type: "int", nullable: false),
                    InvoiceId = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(16,2)", precision: 16, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentAllocations", x => x.Id);
                    table.CheckConstraint("CK_PaymentAllocations_Amount", "[Amount] > 0");
                    table.ForeignKey(
                        name: "FK_PaymentAllocations_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalSchema: "Dokuma",
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PaymentAllocations_Payments_PaymentId",
                        column: x => x.PaymentId,
                        principalSchema: "Dokuma",
                        principalTable: "Payments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PackingLists",
                schema: "Dokuma",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PackingNo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    PackingDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    SaleCustomerId = table.Column<int>(type: "int", nullable: true),
                    DespatchId = table.Column<int>(type: "int", nullable: true),
                    BoyahaneReceiptNo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    BoyahaneSentOn = table.Column<DateOnly>(type: "date", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PackingLists", x => x.Id);
                    table.CheckConstraint("CK_PackingLists_Status", "[Status] IN ('DRAFT', 'CONFIRMED', 'CANCELLED')");
                    table.ForeignKey(
                        name: "FK_PackingLists_Customers_SaleCustomerId",
                        column: x => x.SaleCustomerId,
                        principalSchema: "Dokuma",
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PackingLists_Despatches_DespatchId",
                        column: x => x.DespatchId,
                        principalSchema: "Dokuma",
                        principalTable: "Despatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PackingLists_Orders_OrderId",
                        column: x => x.OrderId,
                        principalSchema: "Dokuma",
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PackingItems",
                schema: "Dokuma",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PackingListId = table.Column<int>(type: "int", nullable: false),
                    RollId = table.Column<int>(type: "int", nullable: false),
                    Meters = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    Kilograms = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: true),
                    Grade = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PackingItems", x => x.Id);
                    table.CheckConstraint("CK_PackingItems_Grade", "[Grade] IN ('FIRST_QUALITY', 'SECOND_QUALITY')");
                    table.CheckConstraint("CK_PackingItems_Kilograms", "[Kilograms] IS NULL OR [Kilograms] > 0");
                    table.CheckConstraint("CK_PackingItems_Meters", "[Meters] > 0");
                    table.ForeignKey(
                        name: "FK_PackingItems_FabricRolls_RollId",
                        column: x => x.RollId,
                        principalSchema: "Dokuma",
                        principalTable: "FabricRolls",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PackingItems_PackingLists_PackingListId",
                        column: x => x.PackingListId,
                        principalSchema: "Dokuma",
                        principalTable: "PackingLists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Despatches_CustomerId",
                schema: "Dokuma",
                table: "Despatches",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Despatches_DespatchNo",
                schema: "Dokuma",
                table: "Despatches",
                column: "DespatchNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Despatches_InvoiceId",
                schema: "Dokuma",
                table: "Despatches",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceLines_InvoiceId",
                schema: "Dokuma",
                table: "InvoiceLines",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceLines_OrderLineId",
                schema: "Dokuma",
                table: "InvoiceLines",
                column: "OrderLineId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_CustomerId",
                schema: "Dokuma",
                table: "Invoices",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_InvoiceNo",
                schema: "Dokuma",
                table: "Invoices",
                column: "InvoiceNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PackingItems_PackingListId",
                schema: "Dokuma",
                table: "PackingItems",
                column: "PackingListId");

            migrationBuilder.CreateIndex(
                name: "UX_PackingItems_Active",
                schema: "Dokuma",
                table: "PackingItems",
                column: "RollId",
                unique: true,
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_PackingLists_DespatchId",
                schema: "Dokuma",
                table: "PackingLists",
                column: "DespatchId");

            migrationBuilder.CreateIndex(
                name: "IX_PackingLists_OrderId",
                schema: "Dokuma",
                table: "PackingLists",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_PackingLists_PackingNo",
                schema: "Dokuma",
                table: "PackingLists",
                column: "PackingNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PackingLists_SaleCustomerId",
                schema: "Dokuma",
                table: "PackingLists",
                column: "SaleCustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentAllocations_InvoiceId",
                schema: "Dokuma",
                table: "PaymentAllocations",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentAllocations_PaymentId_InvoiceId",
                schema: "Dokuma",
                table: "PaymentAllocations",
                columns: new[] { "PaymentId", "InvoiceId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_CustomerId_Currency_PaymentDate",
                schema: "Dokuma",
                table: "Payments",
                columns: new[] { "CustomerId", "Currency", "PaymentDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Payments_PaymentNo",
                schema: "Dokuma",
                table: "Payments",
                column: "PaymentNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_YarnReturns_ReturnNo",
                schema: "Dokuma",
                table: "YarnReturns",
                column: "ReturnNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_YarnReturns_YarnLotId",
                schema: "Dokuma",
                table: "YarnReturns",
                column: "YarnLotId");

            // Yeni yetkiler; varsayılan olarak yalnızca Sistem Yöneticisine verilir, diğer roller rol ekranından yetkilendirilir.
            migrationBuilder.Sql(@"SET IDENTITY_INSERT [Core].[Permissions] ON;
EXEC(N'INSERT INTO [Core].[Permissions] ([Id], [Code], [Module], [Description])
SELECT v.[Id], v.[Code], v.[Module], v.[Description] FROM (VALUES
(2021, N''Dokuma.Packing.View'', N''Dokuma'', N''Çeki listelerini görüntüleme''),
(2022, N''Dokuma.Packing.Manage'', N''Dokuma'', N''Çeki listesi hazırlama, onaylama ve Boyahane''''ye gönderme''),
(2023, N''Dokuma.Shipment.View'', N''Dokuma'', N''İrsaliyeleri görüntüleme''),
(2024, N''Dokuma.Shipment.Manage'', N''Dokuma'', N''İrsaliye hazırlama ve onaylama''),
(2025, N''Dokuma.Invoice.View'', N''Dokuma'', N''Faturaları görüntüleme''),
(2026, N''Dokuma.Invoice.Manage'', N''Dokuma'', N''Fatura hazırlama ve onaylama''),
(2027, N''Dokuma.Account.View'', N''Dokuma'', N''Cari hesapları, ekstreyi ve tahsilatları görüntüleme''),
(2028, N''Dokuma.Account.Manage'', N''Dokuma'', N''Tahsilat girişi, faturalara dağıtım ve iptal'')
) v([Id], [Code], [Module], [Description])
WHERE NOT EXISTS (SELECT 1 FROM [Core].[Permissions] p WHERE p.[Id] = v.[Id] OR p.[Code] = v.[Code])');
SET IDENTITY_INSERT [Core].[Permissions] OFF;");

            migrationBuilder.Sql(@"INSERT INTO [Core].[RolePermissions] ([RoleId], [PermissionId])
SELECT v.[RoleId], v.[PermissionId] FROM (VALUES
(1, 2021), (1, 2022), (1, 2023), (1, 2024), (1, 2025), (1, 2026), (1, 2027), (1, 2028)
) v([RoleId], [PermissionId])
WHERE EXISTS (SELECT 1 FROM [Core].[Roles] r WHERE r.[Id] = v.[RoleId])
  AND EXISTS (SELECT 1 FROM [Core].[Permissions] p WHERE p.[Id] = v.[PermissionId])
  AND NOT EXISTS (SELECT 1 FROM [Core].[RolePermissions] rp WHERE rp.[RoleId] = v.[RoleId] AND rp.[PermissionId] = v.[PermissionId]);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DELETE FROM [Core].[RolePermissions] WHERE [PermissionId] BETWEEN 2021 AND 2028;
DELETE FROM [Core].[Permissions] WHERE [Id] BETWEEN 2021 AND 2028;");

            migrationBuilder.DropTable(
                name: "InvoiceLines",
                schema: "Dokuma");

            migrationBuilder.DropTable(
                name: "PackingItems",
                schema: "Dokuma");

            migrationBuilder.DropTable(
                name: "PaymentAllocations",
                schema: "Dokuma");

            migrationBuilder.DropTable(
                name: "YarnReturns",
                schema: "Dokuma");

            migrationBuilder.DropTable(
                name: "PackingLists",
                schema: "Dokuma");

            migrationBuilder.DropTable(
                name: "Payments",
                schema: "Dokuma");

            migrationBuilder.DropTable(
                name: "Despatches",
                schema: "Dokuma");

            migrationBuilder.DropTable(
                name: "Invoices",
                schema: "Dokuma");

            migrationBuilder.DropColumn(
                name: "SecondQualityDiscountPercent",
                schema: "Dokuma",
                table: "Customers");
        }
    }
}
