using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Boyahane.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Boyahane");

            migrationBuilder.CreateTable(
                name: "Customers",
                schema: "Boyahane",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PartyType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TaxNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    TaxOffice = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    MersisNo = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                    TradeRegistryNo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    IsEInvoiceUser = table.Column<bool>(type: "bit", nullable: false),
                    EInvoiceAlias = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EInvoiceProfile = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    IsEDespatchUser = table.Column<bool>(type: "bit", nullable: false),
                    EDespatchAlias = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EArchiveDelivery = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    EArchiveEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Currency = table.Column<string>(type: "char(3)", maxLength: 200, nullable: false, defaultValue: "TRY"),
                    FireTolerancePercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    PaymentTermDays = table.Column<int>(type: "int", nullable: true),
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
                    table.PrimaryKey("PK_Customers", x => x.Id);
                    table.CheckConstraint("CK_Customers_EArchiveDelivery", "[EArchiveDelivery] IN ('ELECTRONIC', 'PAPER')");
                    table.CheckConstraint("CK_Customers_EInvoiceProfile", "[EInvoiceProfile] IN ('BASIC', 'COMMERCIAL')");
                    table.CheckConstraint("CK_Customers_FireTolerancePercent", "[FireTolerancePercent] IS NULL OR [FireTolerancePercent] BETWEEN 0 AND 100");
                    table.CheckConstraint("CK_Customers_PartyType", "[PartyType] IN ('COMPANY', 'INDIVIDUAL')");
                    table.CheckConstraint("CK_Customers_PaymentTermDays", "[PaymentTermDays] IS NULL OR [PaymentTermDays] >= 0");
                });

            migrationBuilder.CreateTable(
                name: "CustomerAddresses",
                schema: "Boyahane",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AddressLine = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    District = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PostalCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    CountryCode = table.Column<string>(type: "char(2)", maxLength: 200, nullable: false, defaultValue: "TR"),
                    IsDefaultInvoice = table.Column<bool>(type: "bit", nullable: false),
                    IsDefaultDelivery = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerAddresses", x => x.Id);
                    table.CheckConstraint("CK_CustomerAddresses_CountryCode", "LEN([CountryCode]) = 2");
                    table.ForeignKey(
                        name: "FK_CustomerAddresses_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "Boyahane",
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CustomerContacts",
                schema: "Boyahane",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerContacts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerContacts_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "Boyahane",
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "UX_CustomerAddresses_DefaultDelivery",
                schema: "Boyahane",
                table: "CustomerAddresses",
                column: "CustomerId",
                unique: true,
                filter: "[IsDefaultDelivery] = 1");

            migrationBuilder.CreateIndex(
                name: "UX_CustomerAddresses_DefaultInvoice",
                schema: "Boyahane",
                table: "CustomerAddresses",
                column: "CustomerId",
                unique: true,
                filter: "[IsDefaultInvoice] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerContacts_CustomerId",
                schema: "Boyahane",
                table: "CustomerContacts",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_CustomerCode",
                schema: "Boyahane",
                table: "Customers",
                column: "CustomerCode",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_TaxNumber",
                schema: "Boyahane",
                table: "Customers",
                column: "TaxNumber");

            // Yeni yetkiler; varsayılan olarak yalnızca Sistem Yöneticisine verilir, diğer roller rol ekranından yetkilendirilir.
            migrationBuilder.Sql(@"SET IDENTITY_INSERT [Core].[Permissions] ON;
EXEC(N'INSERT INTO [Core].[Permissions] ([Id], [Code], [Module], [Description])
SELECT v.[Id], v.[Code], v.[Module], v.[Description] FROM (VALUES
(1002, N''Boyahane.Customer.View'', N''Boyahane'', N''Boyahane müşterilerini görüntüleme''),
(1003, N''Boyahane.Customer.Manage'', N''Boyahane'', N''Boyahane müşterilerini oluşturma ve düzenleme'')
) v([Id], [Code], [Module], [Description])
WHERE NOT EXISTS (SELECT 1 FROM [Core].[Permissions] p WHERE p.[Id] = v.[Id] OR p.[Code] = v.[Code])');
SET IDENTITY_INSERT [Core].[Permissions] OFF;");

            migrationBuilder.Sql(@"INSERT INTO [Core].[RolePermissions] ([RoleId], [PermissionId])
SELECT v.[RoleId], v.[PermissionId] FROM (VALUES
(1, 1002),
(1, 1003)
) v([RoleId], [PermissionId])
WHERE EXISTS (SELECT 1 FROM [Core].[Roles] r WHERE r.[Id] = v.[RoleId])
  AND EXISTS (SELECT 1 FROM [Core].[Permissions] p WHERE p.[Id] = v.[PermissionId])
  AND NOT EXISTS (SELECT 1 FROM [Core].[RolePermissions] rp WHERE rp.[RoleId] = v.[RoleId] AND rp.[PermissionId] = v.[PermissionId]);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DELETE FROM [Core].[RolePermissions] WHERE [PermissionId] IN (1002, 1003);
DELETE FROM [Core].[Permissions] WHERE [Id] IN (1002, 1003);");

            migrationBuilder.DropTable(
                name: "CustomerAddresses",
                schema: "Boyahane");

            migrationBuilder.DropTable(
                name: "CustomerContacts",
                schema: "Boyahane");

            migrationBuilder.DropTable(
                name: "Customers",
                schema: "Boyahane");
        }
    }
}
