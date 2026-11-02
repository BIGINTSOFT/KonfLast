using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Orme.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialOrme : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Orme");

            migrationBuilder.CreateTable(
                name: "Customers",
                schema: "Orme",
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
                    VatRate = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    PaymentTermDays = table.Column<int>(type: "int", nullable: true),
                    FirstQualityMaxPoints = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: true),
                    SecondQualityMaxPoints = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: true),
                    SecondQualityDiscountPercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
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
                name: "Fabrics",
                schema: "Orme",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FabricCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    KnitName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    BedType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    DiameterInch = table.Column<int>(type: "int", nullable: true),
                    Gauge = table.Column<int>(type: "int", nullable: true),
                    Composition = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    RawWidthCm = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: true),
                    RawWeightGsm = table.Column<int>(type: "int", nullable: false),
                    WastePercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    ExpectedKilogramsPerHour = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: true),
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
                    table.PrimaryKey("PK_Fabrics", x => x.Id);
                    table.CheckConstraint("CK_Fabrics_BedType", "[BedType] IN ('SINGLE', 'DOUBLE')");
                    table.CheckConstraint("CK_Fabrics_ExpectedKilogramsPerHour", "[ExpectedKilogramsPerHour] IS NULL OR [ExpectedKilogramsPerHour] > 0");
                    table.CheckConstraint("CK_Fabrics_RawWeightGsm", "[RawWeightGsm] > 0");
                    table.CheckConstraint("CK_Fabrics_RawWidthCm", "[RawWidthCm] IS NULL OR [RawWidthCm] > 0");
                    table.CheckConstraint("CK_Fabrics_WastePercent", "[WastePercent] IS NULL OR [WastePercent] BETWEEN 0 AND 100");
                });

            migrationBuilder.CreateTable(
                name: "Machines",
                schema: "Orme",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    BedType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    DiameterInch = table.Column<int>(type: "int", nullable: false),
                    Gauge = table.Column<int>(type: "int", nullable: false),
                    NeedleCount = table.Column<int>(type: "int", nullable: true),
                    FeederCount = table.Column<int>(type: "int", nullable: true),
                    NominalSpeedRpm = table.Column<int>(type: "int", nullable: true),
                    Brand = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
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
                    table.PrimaryKey("PK_Machines", x => x.Id);
                    table.CheckConstraint("CK_Machines_BedType", "[BedType] IN ('SINGLE', 'DOUBLE')");
                    table.CheckConstraint("CK_Machines_DiameterInch", "[DiameterInch] > 0");
                    table.CheckConstraint("CK_Machines_FeederCount", "[FeederCount] IS NULL OR [FeederCount] > 0");
                    table.CheckConstraint("CK_Machines_Gauge", "[Gauge] > 0");
                    table.CheckConstraint("CK_Machines_NeedleCount", "[NeedleCount] IS NULL OR [NeedleCount] > 0");
                    table.CheckConstraint("CK_Machines_NominalSpeedRpm", "[NominalSpeedRpm] IS NULL OR [NominalSpeedRpm] > 0");
                });

            migrationBuilder.CreateTable(
                name: "Suppliers",
                schema: "Orme",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TaxNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Currency = table.Column<string>(type: "char(3)", maxLength: 200, nullable: false, defaultValue: "TRY"),
                    LeadTimeDays = table.Column<int>(type: "int", nullable: true),
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
                    table.PrimaryKey("PK_Suppliers", x => x.Id);
                    table.CheckConstraint("CK_Suppliers_LeadTimeDays", "[LeadTimeDays] IS NULL OR [LeadTimeDays] >= 0");
                });

            migrationBuilder.CreateTable(
                name: "Yarns",
                schema: "Orme",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    YarnCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CountValue = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: false),
                    CountSystem = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Plies = table.Column<int>(type: "int", nullable: false),
                    Composition = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Twist = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Color = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
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
                    table.PrimaryKey("PK_Yarns", x => x.Id);
                    table.CheckConstraint("CK_Yarns_CountSystem", "[CountSystem] IN ('NE', 'NM', 'DTEX', 'DENIER')");
                    table.CheckConstraint("CK_Yarns_CountValue", "[CountValue] > 0");
                    table.CheckConstraint("CK_Yarns_Plies", "[Plies] >= 1");
                });

            migrationBuilder.CreateTable(
                name: "CustomerAddresses",
                schema: "Orme",
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
                        principalSchema: "Orme",
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CustomerContacts",
                schema: "Orme",
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
                        principalSchema: "Orme",
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FabricYarns",
                schema: "Orme",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FabricId = table.Column<int>(type: "int", nullable: false),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    YarnId = table.Column<int>(type: "int", nullable: false),
                    Percent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    StitchLengthMm = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FabricYarns", x => x.Id);
                    table.CheckConstraint("CK_FabricYarns_Percent", "[Percent] > 0 AND [Percent] <= 100");
                    table.CheckConstraint("CK_FabricYarns_Sequence", "[Sequence] >= 1");
                    table.CheckConstraint("CK_FabricYarns_StitchLengthMm", "[StitchLengthMm] IS NULL OR [StitchLengthMm] > 0");
                    table.ForeignKey(
                        name: "FK_FabricYarns_Fabrics_FabricId",
                        column: x => x.FabricId,
                        principalSchema: "Orme",
                        principalTable: "Fabrics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FabricYarns_Yarns_YarnId",
                        column: x => x.YarnId,
                        principalSchema: "Orme",
                        principalTable: "Yarns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "UX_CustomerAddresses_DefaultDelivery",
                schema: "Orme",
                table: "CustomerAddresses",
                column: "CustomerId",
                unique: true,
                filter: "[IsDefaultDelivery] = 1");

            migrationBuilder.CreateIndex(
                name: "UX_CustomerAddresses_DefaultInvoice",
                schema: "Orme",
                table: "CustomerAddresses",
                column: "CustomerId",
                unique: true,
                filter: "[IsDefaultInvoice] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerContacts_CustomerId",
                schema: "Orme",
                table: "CustomerContacts",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_CustomerCode",
                schema: "Orme",
                table: "Customers",
                column: "CustomerCode",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_TaxNumber",
                schema: "Orme",
                table: "Customers",
                column: "TaxNumber");

            migrationBuilder.CreateIndex(
                name: "IX_Fabrics_FabricCode",
                schema: "Orme",
                table: "Fabrics",
                column: "FabricCode",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_FabricYarns_FabricId_Sequence",
                schema: "Orme",
                table: "FabricYarns",
                columns: new[] { "FabricId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FabricYarns_YarnId",
                schema: "Orme",
                table: "FabricYarns",
                column: "YarnId");

            migrationBuilder.CreateIndex(
                name: "IX_Machines_Code",
                schema: "Orme",
                table: "Machines",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_Code",
                schema: "Orme",
                table: "Suppliers",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Yarns_YarnCode",
                schema: "Orme",
                table: "Yarns",
                column: "YarnCode",
                unique: true,
                filter: "[IsDeleted] = 0");

            // Modülün yetkileri; varsayılan olarak yalnızca Sistem Yöneticisine verilir, diğer roller rol ekranından yetkilendirilir.
            migrationBuilder.Sql(@"SET IDENTITY_INSERT [Core].[Permissions] ON;
EXEC(N'INSERT INTO [Core].[Permissions] ([Id], [Code], [Module], [Description])
SELECT v.[Id], v.[Code], v.[Module], v.[Description] FROM (VALUES
(3001, N''Orme.View'', N''Orme'', N''Örme ekranlarını görüntüleme''),
(3002, N''Orme.Customer.View'', N''Orme'', N''Örme müşterilerini görüntüleme''),
(3003, N''Orme.Customer.Manage'', N''Orme'', N''Örme müşterilerini oluşturma ve düzenleme''),
(3004, N''Orme.Material.View'', N''Orme'', N''İplik ve kumaş kartlarını görüntüleme''),
(3005, N''Orme.Material.Manage'', N''Orme'', N''İplik ve kumaş kartlarını oluşturma ve düzenleme''),
(3006, N''Orme.Machine.View'', N''Orme'', N''Örme makinelerini görüntüleme''),
(3007, N''Orme.Machine.Manage'', N''Orme'', N''Örme makinelerini oluşturma ve düzenleme''),
(3008, N''Orme.Purchase.View'', N''Orme'', N''Tedarikçileri görüntüleme''),
(3009, N''Orme.Purchase.Manage'', N''Orme'', N''Tedarikçi kartlarını oluşturma ve düzenleme'')
) v([Id], [Code], [Module], [Description])
WHERE NOT EXISTS (SELECT 1 FROM [Core].[Permissions] p WHERE p.[Id] = v.[Id] OR p.[Code] = v.[Code])');
SET IDENTITY_INSERT [Core].[Permissions] OFF;");

            migrationBuilder.Sql(@"INSERT INTO [Core].[RolePermissions] ([RoleId], [PermissionId])
SELECT v.[RoleId], v.[PermissionId] FROM (VALUES
(1, 3001), (1, 3002), (1, 3003), (1, 3004), (1, 3005), (1, 3006), (1, 3007), (1, 3008), (1, 3009)
) v([RoleId], [PermissionId])
WHERE EXISTS (SELECT 1 FROM [Core].[Roles] r WHERE r.[Id] = v.[RoleId])
  AND EXISTS (SELECT 1 FROM [Core].[Permissions] p WHERE p.[Id] = v.[PermissionId])
  AND NOT EXISTS (SELECT 1 FROM [Core].[RolePermissions] rp WHERE rp.[RoleId] = v.[RoleId] AND rp.[PermissionId] = v.[PermissionId]);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DELETE FROM [Core].[RolePermissions] WHERE [PermissionId] BETWEEN 3001 AND 3009;
DELETE FROM [Core].[Permissions] WHERE [Id] BETWEEN 3001 AND 3009;");

            migrationBuilder.DropTable(
                name: "CustomerAddresses",
                schema: "Orme");

            migrationBuilder.DropTable(
                name: "CustomerContacts",
                schema: "Orme");

            migrationBuilder.DropTable(
                name: "FabricYarns",
                schema: "Orme");

            migrationBuilder.DropTable(
                name: "Machines",
                schema: "Orme");

            migrationBuilder.DropTable(
                name: "Suppliers",
                schema: "Orme");

            migrationBuilder.DropTable(
                name: "Customers",
                schema: "Orme");

            migrationBuilder.DropTable(
                name: "Fabrics",
                schema: "Orme");

            migrationBuilder.DropTable(
                name: "Yarns",
                schema: "Orme");
        }
    }
}
