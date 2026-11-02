using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Boyahane.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentsAndAccounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Payments",
                schema: "Boyahane",
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
                    table.CheckConstraint("CK_Payments_Method", "[Method] IN ('CASH', 'BANK_TRANSFER', 'CHECK')");
                    table.CheckConstraint("CK_Payments_Status", "[Status] IN ('ACTIVE', 'CANCELLED')");
                    table.ForeignKey(
                        name: "FK_Payments_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "Boyahane",
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PaymentAllocations",
                schema: "Boyahane",
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
                        principalSchema: "Boyahane",
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PaymentAllocations_Payments_PaymentId",
                        column: x => x.PaymentId,
                        principalSchema: "Boyahane",
                        principalTable: "Payments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentAllocations_InvoiceId",
                schema: "Boyahane",
                table: "PaymentAllocations",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentAllocations_PaymentId_InvoiceId",
                schema: "Boyahane",
                table: "PaymentAllocations",
                columns: new[] { "PaymentId", "InvoiceId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_CustomerId_Currency_PaymentDate",
                schema: "Boyahane",
                table: "Payments",
                columns: new[] { "CustomerId", "Currency", "PaymentDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Payments_PaymentNo",
                schema: "Boyahane",
                table: "Payments",
                column: "PaymentNo",
                unique: true);

            // Cari yetkileri Sistem Yöneticisi ve Boyahane Yöneticisine; diğer roller rol ekranından yetkilendirilir.
            migrationBuilder.Sql(@"SET IDENTITY_INSERT [Core].[Permissions] ON;
EXEC(N'INSERT INTO [Core].[Permissions] ([Id], [Code], [Module], [Description])
SELECT v.[Id], v.[Code], v.[Module], v.[Description] FROM (VALUES
(1035, N''Boyahane.Account.View'', N''Boyahane'', N''Cari hesapları, ekstreyi ve tahsilatları görüntüleme''),
(1036, N''Boyahane.Account.Manage'', N''Boyahane'', N''Tahsilat girişi, faturalara dağıtım ve iptal'')
) v([Id], [Code], [Module], [Description])
WHERE NOT EXISTS (SELECT 1 FROM [Core].[Permissions] p WHERE p.[Id] = v.[Id] OR p.[Code] = v.[Code])');
SET IDENTITY_INSERT [Core].[Permissions] OFF;");

            // Kullanıcı onayı (2026-10-30): depo ve satın alma yetkileri Mal Kabul, Laboratuvar ve Planlama rollerine.
            migrationBuilder.Sql(@"INSERT INTO [Core].[RolePermissions] ([RoleId], [PermissionId])
SELECT r.[Id], p.[Id] FROM (VALUES
(N'ADMIN', 1035), (N'ADMIN', 1036),
(N'BOYAHANE_MANAGER', 1035), (N'BOYAHANE_MANAGER', 1036),
(N'BOYAHANE_RECEIVING', 1031), (N'BOYAHANE_RECEIVING', 1032), (N'BOYAHANE_RECEIVING', 1033), (N'BOYAHANE_RECEIVING', 1034),
(N'BOYAHANE_PLANNER', 1031), (N'BOYAHANE_PLANNER', 1033),
(N'BOYAHANE_LAB', 1031)
) v([RoleCode], [PermissionId])
JOIN [Core].[Roles] r ON r.[Code] = v.[RoleCode]
JOIN [Core].[Permissions] p ON p.[Id] = v.[PermissionId]
WHERE NOT EXISTS (SELECT 1 FROM [Core].[RolePermissions] rp WHERE rp.[RoleId] = r.[Id] AND rp.[PermissionId] = p.[Id]);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DELETE rp FROM [Core].[RolePermissions] rp JOIN [Core].[Roles] r ON r.[Id] = rp.[RoleId]
WHERE (r.[Code] = N'BOYAHANE_RECEIVING' AND rp.[PermissionId] IN (1031, 1032, 1033, 1034))
   OR (r.[Code] = N'BOYAHANE_PLANNER' AND rp.[PermissionId] IN (1031, 1033))
   OR (r.[Code] = N'BOYAHANE_LAB' AND rp.[PermissionId] = 1031);
DELETE FROM [Core].[RolePermissions] WHERE [PermissionId] IN (1035, 1036);
DELETE FROM [Core].[Permissions] WHERE [Id] IN (1035, 1036);");

            migrationBuilder.DropTable(
                name: "PaymentAllocations",
                schema: "Boyahane");

            migrationBuilder.DropTable(
                name: "Payments",
                schema: "Boyahane");
        }
    }
}
