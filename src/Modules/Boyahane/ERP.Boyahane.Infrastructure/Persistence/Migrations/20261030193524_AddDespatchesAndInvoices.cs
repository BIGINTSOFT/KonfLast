using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Boyahane.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDespatchesAndInvoices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DespatchId",
                schema: "Boyahane",
                table: "PackingLists",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "VatRate",
                schema: "Boyahane",
                table: "Customers",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Invoices",
                schema: "Boyahane",
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
                        principalSchema: "Boyahane",
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Despatches",
                schema: "Boyahane",
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
                        principalSchema: "Boyahane",
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Despatches_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalSchema: "Boyahane",
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InvoiceLines",
                schema: "Boyahane",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InvoiceId = table.Column<int>(type: "int", nullable: false),
                    OrderLineId = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(14,4)", precision: 14, scale: 4, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceLines", x => x.Id);
                    table.CheckConstraint("CK_InvoiceLines_Unit", "[Unit] IN ('PIECE', 'METER', 'KILOGRAM', 'ROLL', 'BOX')");
                    table.ForeignKey(
                        name: "FK_InvoiceLines_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalSchema: "Boyahane",
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InvoiceLines_OrderLines_OrderLineId",
                        column: x => x.OrderLineId,
                        principalSchema: "Boyahane",
                        principalTable: "OrderLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PackingLists_DespatchId",
                schema: "Boyahane",
                table: "PackingLists",
                column: "DespatchId");

            migrationBuilder.CreateIndex(
                name: "IX_Despatches_CustomerId",
                schema: "Boyahane",
                table: "Despatches",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Despatches_DespatchNo",
                schema: "Boyahane",
                table: "Despatches",
                column: "DespatchNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Despatches_InvoiceId",
                schema: "Boyahane",
                table: "Despatches",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceLines_InvoiceId",
                schema: "Boyahane",
                table: "InvoiceLines",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceLines_OrderLineId",
                schema: "Boyahane",
                table: "InvoiceLines",
                column: "OrderLineId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_CustomerId",
                schema: "Boyahane",
                table: "Invoices",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_InvoiceNo",
                schema: "Boyahane",
                table: "Invoices",
                column: "InvoiceNo",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_PackingLists_Despatches_DespatchId",
                schema: "Boyahane",
                table: "PackingLists",
                column: "DespatchId",
                principalSchema: "Boyahane",
                principalTable: "Despatches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // Sevkiyat ve fatura yetkileri Sistem Yöneticisi ve Boyahane Yöneticisine; çeki yetkileri onaylanan rollere.
            migrationBuilder.Sql(@"SET IDENTITY_INSERT [Core].[Permissions] ON;
EXEC(N'INSERT INTO [Core].[Permissions] ([Id], [Code], [Module], [Description])
SELECT v.[Id], v.[Code], v.[Module], v.[Description] FROM (VALUES
(1027, N''Boyahane.Shipment.View'', N''Boyahane'', N''İrsaliyeleri görüntüleme''),
(1028, N''Boyahane.Shipment.Manage'', N''Boyahane'', N''İrsaliye hazırlama ve onaylama''),
(1029, N''Boyahane.Invoice.View'', N''Boyahane'', N''Faturaları görüntüleme''),
(1030, N''Boyahane.Invoice.Manage'', N''Boyahane'', N''Fatura hazırlama ve onaylama'')
) v([Id], [Code], [Module], [Description])
WHERE NOT EXISTS (SELECT 1 FROM [Core].[Permissions] p WHERE p.[Id] = v.[Id] OR p.[Code] = v.[Code])');
SET IDENTITY_INSERT [Core].[Permissions] OFF;");

            migrationBuilder.Sql(@"INSERT INTO [Core].[RolePermissions] ([RoleId], [PermissionId])
SELECT r.[Id], p.[Id] FROM (VALUES
(N'ADMIN', 1027), (N'ADMIN', 1028), (N'ADMIN', 1029), (N'ADMIN', 1030),
(N'BOYAHANE_MANAGER', 1027), (N'BOYAHANE_MANAGER', 1028), (N'BOYAHANE_MANAGER', 1029), (N'BOYAHANE_MANAGER', 1030),
(N'BOYAHANE_RECEIVING', 1025), (N'BOYAHANE_RECEIVING', 1026),
(N'BOYAHANE_PLANNER', 1025), (N'BOYAHANE_PLANNER', 1026),
(N'BOYAHANE_SALES', 1025)
) v([RoleCode], [PermissionId])
JOIN [Core].[Roles] r ON r.[Code] = v.[RoleCode]
JOIN [Core].[Permissions] p ON p.[Id] = v.[PermissionId]
WHERE NOT EXISTS (SELECT 1 FROM [Core].[RolePermissions] rp WHERE rp.[RoleId] = r.[Id] AND rp.[PermissionId] = p.[Id]);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DELETE FROM [Core].[RolePermissions] WHERE [PermissionId] IN (1027, 1028, 1029, 1030);
DELETE FROM [Core].[Permissions] WHERE [Id] IN (1027, 1028, 1029, 1030);");

            migrationBuilder.DropForeignKey(
                name: "FK_PackingLists_Despatches_DespatchId",
                schema: "Boyahane",
                table: "PackingLists");

            migrationBuilder.DropTable(
                name: "Despatches",
                schema: "Boyahane");

            migrationBuilder.DropTable(
                name: "InvoiceLines",
                schema: "Boyahane");

            migrationBuilder.DropTable(
                name: "Invoices",
                schema: "Boyahane");

            migrationBuilder.DropIndex(
                name: "IX_PackingLists_DespatchId",
                schema: "Boyahane",
                table: "PackingLists");

            migrationBuilder.DropColumn(
                name: "DespatchId",
                schema: "Boyahane",
                table: "PackingLists");

            migrationBuilder.DropColumn(
                name: "VatRate",
                schema: "Boyahane",
                table: "Customers");
        }
    }
}
