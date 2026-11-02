using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Boyahane.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFabricReceipts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "RequiresRawInspection",
                schema: "Boyahane",
                table: "Customers",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateTable(
                name: "FabricReceipts",
                schema: "Boyahane",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReceiptNo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ReceiptDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CustomerId = table.Column<int>(type: "int", nullable: true),
                    DespatchNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FabricReceipts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FabricReceipts_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "Boyahane",
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FabricLots",
                schema: "Boyahane",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReceiptId = table.Column<int>(type: "int", nullable: false),
                    LotNo = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    FabricId = table.Column<int>(type: "int", nullable: false),
                    CustomerFabricCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DeclaredRollCount = table.Column<int>(type: "int", nullable: true),
                    DeclaredMeters = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: true),
                    DeclaredKilograms = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: true),
                    InspectionRequired = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FabricLots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FabricLots_FabricReceipts_ReceiptId",
                        column: x => x.ReceiptId,
                        principalSchema: "Boyahane",
                        principalTable: "FabricReceipts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FabricLots_Fabrics_FabricId",
                        column: x => x.FabricId,
                        principalSchema: "Boyahane",
                        principalTable: "Fabrics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FabricRolls",
                schema: "Boyahane",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LotId = table.Column<int>(type: "int", nullable: false),
                    RollNo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CustomerRollNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Meters = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    Kilograms = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    MetersMeasured = table.Column<bool>(type: "bit", nullable: false),
                    KilogramsMeasured = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FabricRolls", x => x.Id);
                    table.CheckConstraint("CK_FabricRolls_Kilograms", "[Kilograms] > 0");
                    table.CheckConstraint("CK_FabricRolls_Meters", "[Meters] > 0");
                    table.CheckConstraint("CK_FabricRolls_Status", "[Status] IN ('PENDING_INSPECTION', 'APPROVED', 'CONDITIONAL', 'REJECTED')");
                    table.ForeignKey(
                        name: "FK_FabricRolls_FabricLots_LotId",
                        column: x => x.LotId,
                        principalSchema: "Boyahane",
                        principalTable: "FabricLots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FabricLots_FabricId",
                schema: "Boyahane",
                table: "FabricLots",
                column: "FabricId");

            migrationBuilder.CreateIndex(
                name: "IX_FabricLots_LotNo",
                schema: "Boyahane",
                table: "FabricLots",
                column: "LotNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FabricLots_ReceiptId",
                schema: "Boyahane",
                table: "FabricLots",
                column: "ReceiptId");

            migrationBuilder.CreateIndex(
                name: "IX_FabricReceipts_CustomerId",
                schema: "Boyahane",
                table: "FabricReceipts",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_FabricReceipts_ReceiptDate",
                schema: "Boyahane",
                table: "FabricReceipts",
                column: "ReceiptDate");

            migrationBuilder.CreateIndex(
                name: "IX_FabricReceipts_ReceiptNo",
                schema: "Boyahane",
                table: "FabricReceipts",
                column: "ReceiptNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FabricRolls_LotId",
                schema: "Boyahane",
                table: "FabricRolls",
                column: "LotId");

            migrationBuilder.CreateIndex(
                name: "IX_FabricRolls_RollNo",
                schema: "Boyahane",
                table: "FabricRolls",
                column: "RollNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FabricRolls_Status",
                schema: "Boyahane",
                table: "FabricRolls",
                column: "Status");

            // Yeni yetkiler; varsayılan olarak yalnızca Sistem Yöneticisine verilir, diğer roller rol ekranından yetkilendirilir.
            migrationBuilder.Sql(@"SET IDENTITY_INSERT [Core].[Permissions] ON;
EXEC(N'INSERT INTO [Core].[Permissions] ([Id], [Code], [Module], [Description])
SELECT v.[Id], v.[Code], v.[Module], v.[Description] FROM (VALUES
(1006, N''Boyahane.Receipt.View'', N''Boyahane'', N''Kumaş kabul belgelerini görüntüleme''),
(1007, N''Boyahane.Receipt.Manage'', N''Boyahane'', N''Kumaş kabul belgelerini oluşturma ve silme'')
) v([Id], [Code], [Module], [Description])
WHERE NOT EXISTS (SELECT 1 FROM [Core].[Permissions] p WHERE p.[Id] = v.[Id] OR p.[Code] = v.[Code])');
SET IDENTITY_INSERT [Core].[Permissions] OFF;");

            migrationBuilder.Sql(@"INSERT INTO [Core].[RolePermissions] ([RoleId], [PermissionId])
SELECT v.[RoleId], v.[PermissionId] FROM (VALUES
(1, 1006),
(1, 1007)
) v([RoleId], [PermissionId])
WHERE EXISTS (SELECT 1 FROM [Core].[Roles] r WHERE r.[Id] = v.[RoleId])
  AND EXISTS (SELECT 1 FROM [Core].[Permissions] p WHERE p.[Id] = v.[PermissionId])
  AND NOT EXISTS (SELECT 1 FROM [Core].[RolePermissions] rp WHERE rp.[RoleId] = v.[RoleId] AND rp.[PermissionId] = v.[PermissionId]);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DELETE FROM [Core].[RolePermissions] WHERE [PermissionId] IN (1006, 1007);
DELETE FROM [Core].[Permissions] WHERE [Id] IN (1006, 1007);");

            migrationBuilder.DropTable(
                name: "FabricRolls",
                schema: "Boyahane");

            migrationBuilder.DropTable(
                name: "FabricLots",
                schema: "Boyahane");

            migrationBuilder.DropTable(
                name: "FabricReceipts",
                schema: "Boyahane");

            migrationBuilder.DropColumn(
                name: "RequiresRawInspection",
                schema: "Boyahane",
                table: "Customers");
        }
    }
}
