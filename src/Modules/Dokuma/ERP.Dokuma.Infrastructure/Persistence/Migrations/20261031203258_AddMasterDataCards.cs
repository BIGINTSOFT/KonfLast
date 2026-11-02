using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Dokuma.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMasterDataCards : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Dokuma");

            migrationBuilder.CreateTable(
                name: "Articles",
                schema: "Dokuma",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ArticleCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    WeaveName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SheddingType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Composition = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ReedNumber = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: true),
                    EndsPerDent = table.Column<int>(type: "int", nullable: true),
                    ReedWidthCm = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: false),
                    WeftDensityPerCm = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: false),
                    RawWidthCm = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: false),
                    RawWeightGsm = table.Column<int>(type: "int", nullable: false),
                    WarpCrimpPercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    WeftCrimpPercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
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
                    table.PrimaryKey("PK_Articles", x => x.Id);
                    table.CheckConstraint("CK_Articles_EndsPerDent", "[EndsPerDent] IS NULL OR [EndsPerDent] >= 1");
                    table.CheckConstraint("CK_Articles_RawWeightGsm", "[RawWeightGsm] > 0");
                    table.CheckConstraint("CK_Articles_RawWidthCm", "[RawWidthCm] > 0");
                    table.CheckConstraint("CK_Articles_ReedNumber", "[ReedNumber] IS NULL OR [ReedNumber] > 0");
                    table.CheckConstraint("CK_Articles_ReedWidthCm", "[ReedWidthCm] > 0");
                    table.CheckConstraint("CK_Articles_SheddingType", "[SheddingType] IN ('CAM', 'DOBBY', 'JACQUARD')");
                    table.CheckConstraint("CK_Articles_WarpCrimpPercent", "[WarpCrimpPercent] IS NULL OR [WarpCrimpPercent] BETWEEN 0 AND 100");
                    table.CheckConstraint("CK_Articles_WeftCrimpPercent", "[WeftCrimpPercent] IS NULL OR [WeftCrimpPercent] BETWEEN 0 AND 100");
                    table.CheckConstraint("CK_Articles_WeftDensityPerCm", "[WeftDensityPerCm] > 0");
                });

            migrationBuilder.CreateTable(
                name: "Customers",
                schema: "Dokuma",
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
                name: "Looms",
                schema: "Dokuma",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    LoomType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    SheddingType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    MaxReedWidthCm = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: true),
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
                    table.PrimaryKey("PK_Looms", x => x.Id);
                    table.CheckConstraint("CK_Looms_LoomType", "[LoomType] IN ('AIR_JET', 'RAPIER', 'WATER_JET', 'PROJECTILE', 'SHUTTLE')");
                    table.CheckConstraint("CK_Looms_MaxReedWidthCm", "[MaxReedWidthCm] IS NULL OR [MaxReedWidthCm] > 0");
                    table.CheckConstraint("CK_Looms_NominalSpeedRpm", "[NominalSpeedRpm] IS NULL OR [NominalSpeedRpm] > 0");
                    table.CheckConstraint("CK_Looms_SheddingType", "[SheddingType] IN ('CAM', 'DOBBY', 'JACQUARD')");
                });

            migrationBuilder.CreateTable(
                name: "Suppliers",
                schema: "Dokuma",
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
                schema: "Dokuma",
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
                schema: "Dokuma",
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
                        principalSchema: "Dokuma",
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CustomerContacts",
                schema: "Dokuma",
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
                        principalSchema: "Dokuma",
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ArticleYarns",
                schema: "Dokuma",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ArticleId = table.Column<int>(type: "int", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    YarnId = table.Column<int>(type: "int", nullable: false),
                    Ends = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArticleYarns", x => x.Id);
                    table.CheckConstraint("CK_ArticleYarns_Ends", "[Ends] >= 1");
                    table.CheckConstraint("CK_ArticleYarns_Role", "[Role] IN ('WARP', 'WEFT')");
                    table.CheckConstraint("CK_ArticleYarns_Sequence", "[Sequence] >= 1");
                    table.ForeignKey(
                        name: "FK_ArticleYarns_Articles_ArticleId",
                        column: x => x.ArticleId,
                        principalSchema: "Dokuma",
                        principalTable: "Articles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ArticleYarns_Yarns_YarnId",
                        column: x => x.YarnId,
                        principalSchema: "Dokuma",
                        principalTable: "Yarns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Articles_ArticleCode",
                schema: "Dokuma",
                table: "Articles",
                column: "ArticleCode",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ArticleYarns_ArticleId_Role_Sequence",
                schema: "Dokuma",
                table: "ArticleYarns",
                columns: new[] { "ArticleId", "Role", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ArticleYarns_YarnId",
                schema: "Dokuma",
                table: "ArticleYarns",
                column: "YarnId");

            migrationBuilder.CreateIndex(
                name: "UX_CustomerAddresses_DefaultDelivery",
                schema: "Dokuma",
                table: "CustomerAddresses",
                column: "CustomerId",
                unique: true,
                filter: "[IsDefaultDelivery] = 1");

            migrationBuilder.CreateIndex(
                name: "UX_CustomerAddresses_DefaultInvoice",
                schema: "Dokuma",
                table: "CustomerAddresses",
                column: "CustomerId",
                unique: true,
                filter: "[IsDefaultInvoice] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerContacts_CustomerId",
                schema: "Dokuma",
                table: "CustomerContacts",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_CustomerCode",
                schema: "Dokuma",
                table: "Customers",
                column: "CustomerCode",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_TaxNumber",
                schema: "Dokuma",
                table: "Customers",
                column: "TaxNumber");

            migrationBuilder.CreateIndex(
                name: "IX_Looms_Code",
                schema: "Dokuma",
                table: "Looms",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_Code",
                schema: "Dokuma",
                table: "Suppliers",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Yarns_YarnCode",
                schema: "Dokuma",
                table: "Yarns",
                column: "YarnCode",
                unique: true,
                filter: "[IsDeleted] = 0");

            // Yeni yetkiler; varsayılan olarak yalnızca Sistem Yöneticisine verilir, diğer roller rol ekranından yetkilendirilir.
            migrationBuilder.Sql(@"SET IDENTITY_INSERT [Core].[Permissions] ON;
EXEC(N'INSERT INTO [Core].[Permissions] ([Id], [Code], [Module], [Description])
SELECT v.[Id], v.[Code], v.[Module], v.[Description] FROM (VALUES
(2002, N''Dokuma.Customer.View'', N''Dokuma'', N''Dokuma müşterilerini görüntüleme''),
(2003, N''Dokuma.Customer.Manage'', N''Dokuma'', N''Dokuma müşterilerini oluşturma ve düzenleme''),
(2004, N''Dokuma.Material.View'', N''Dokuma'', N''İplik ve artikel kartlarını görüntüleme''),
(2005, N''Dokuma.Material.Manage'', N''Dokuma'', N''İplik ve artikel kartlarını oluşturma ve düzenleme''),
(2006, N''Dokuma.Loom.View'', N''Dokuma'', N''Tezgâh kartlarını görüntüleme''),
(2007, N''Dokuma.Loom.Manage'', N''Dokuma'', N''Tezgâh kartlarını oluşturma ve düzenleme''),
(2008, N''Dokuma.Purchase.View'', N''Dokuma'', N''Tedarikçileri görüntüleme''),
(2009, N''Dokuma.Purchase.Manage'', N''Dokuma'', N''Tedarikçi kartlarını oluşturma ve düzenleme'')
) v([Id], [Code], [Module], [Description])
WHERE NOT EXISTS (SELECT 1 FROM [Core].[Permissions] p WHERE p.[Id] = v.[Id] OR p.[Code] = v.[Code])');
SET IDENTITY_INSERT [Core].[Permissions] OFF;");

            migrationBuilder.Sql(@"INSERT INTO [Core].[RolePermissions] ([RoleId], [PermissionId])
SELECT v.[RoleId], v.[PermissionId] FROM (VALUES
(1, 2002), (1, 2003), (1, 2004), (1, 2005), (1, 2006), (1, 2007), (1, 2008), (1, 2009)
) v([RoleId], [PermissionId])
WHERE EXISTS (SELECT 1 FROM [Core].[Roles] r WHERE r.[Id] = v.[RoleId])
  AND EXISTS (SELECT 1 FROM [Core].[Permissions] p WHERE p.[Id] = v.[PermissionId])
  AND NOT EXISTS (SELECT 1 FROM [Core].[RolePermissions] rp WHERE rp.[RoleId] = v.[RoleId] AND rp.[PermissionId] = v.[PermissionId]);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DELETE FROM [Core].[RolePermissions] WHERE [PermissionId] BETWEEN 2002 AND 2009;
DELETE FROM [Core].[Permissions] WHERE [Id] BETWEEN 2002 AND 2009;");

            migrationBuilder.DropTable(
                name: "ArticleYarns",
                schema: "Dokuma");

            migrationBuilder.DropTable(
                name: "CustomerAddresses",
                schema: "Dokuma");

            migrationBuilder.DropTable(
                name: "CustomerContacts",
                schema: "Dokuma");

            migrationBuilder.DropTable(
                name: "Looms",
                schema: "Dokuma");

            migrationBuilder.DropTable(
                name: "Suppliers",
                schema: "Dokuma");

            migrationBuilder.DropTable(
                name: "Articles",
                schema: "Dokuma");

            migrationBuilder.DropTable(
                name: "Yarns",
                schema: "Dokuma");

            migrationBuilder.DropTable(
                name: "Customers",
                schema: "Dokuma");
        }
    }
}
