using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Boyahane.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddChemicalStock : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChemicalLots",
                schema: "Boyahane",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LotNo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    DyeChemicalId = table.Column<int>(type: "int", nullable: false),
                    SupplierLotNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SupplierName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    ReceiptDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ExpiryDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ReceivedKilograms = table.Column<decimal>(type: "decimal(14,3)", precision: 14, scale: 3, nullable: false),
                    RemainingKilograms = table.Column<decimal>(type: "decimal(14,3)", precision: 14, scale: 3, nullable: false),
                    UnitCost = table.Column<decimal>(type: "decimal(14,4)", precision: 14, scale: 4, nullable: true),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChemicalLots", x => x.Id);
                    table.CheckConstraint("CK_ChemicalLots_Received", "[ReceivedKilograms] > 0");
                    table.CheckConstraint("CK_ChemicalLots_Remaining", "[RemainingKilograms] >= 0");
                    table.ForeignKey(
                        name: "FK_ChemicalLots_DyeChemicals_DyeChemicalId",
                        column: x => x.DyeChemicalId,
                        principalSchema: "Boyahane",
                        principalTable: "DyeChemicals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ChemicalMovements",
                schema: "Boyahane",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LotId = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Kilograms = table.Column<decimal>(type: "decimal(14,3)", precision: 14, scale: 3, nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChemicalMovements", x => x.Id);
                    table.CheckConstraint("CK_ChemicalMovements_Type", "[Type] IN ('RECEIPT', 'CONSUMPTION', 'ADJUSTMENT')");
                    table.ForeignKey(
                        name: "FK_ChemicalMovements_ChemicalLots_LotId",
                        column: x => x.LotId,
                        principalSchema: "Boyahane",
                        principalTable: "ChemicalLots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ConsumptionLots",
                schema: "Boyahane",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConsumptionId = table.Column<int>(type: "int", nullable: false),
                    LotId = table.Column<int>(type: "int", nullable: false),
                    Kilograms = table.Column<decimal>(type: "decimal(14,3)", precision: 14, scale: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsumptionLots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConsumptionLots_BatchConsumptions_ConsumptionId",
                        column: x => x.ConsumptionId,
                        principalSchema: "Boyahane",
                        principalTable: "BatchConsumptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ConsumptionLots_ChemicalLots_LotId",
                        column: x => x.LotId,
                        principalSchema: "Boyahane",
                        principalTable: "ChemicalLots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChemicalLots_DyeChemicalId_ExpiryDate",
                schema: "Boyahane",
                table: "ChemicalLots",
                columns: new[] { "DyeChemicalId", "ExpiryDate" });

            migrationBuilder.CreateIndex(
                name: "IX_ChemicalLots_LotNo",
                schema: "Boyahane",
                table: "ChemicalLots",
                column: "LotNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChemicalMovements_LotId",
                schema: "Boyahane",
                table: "ChemicalMovements",
                column: "LotId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsumptionLots_ConsumptionId",
                schema: "Boyahane",
                table: "ConsumptionLots",
                column: "ConsumptionId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsumptionLots_LotId",
                schema: "Boyahane",
                table: "ConsumptionLots",
                column: "LotId");

            // Depo yetkileri Sistem Yöneticisi ve Boyahane Yöneticisine; irsaliye ve fatura yetkileri onaylanan rollere.
            migrationBuilder.Sql(@"SET IDENTITY_INSERT [Core].[Permissions] ON;
EXEC(N'INSERT INTO [Core].[Permissions] ([Id], [Code], [Module], [Description])
SELECT v.[Id], v.[Code], v.[Module], v.[Description] FROM (VALUES
(1031, N''Boyahane.ChemicalStock.View'', N''Boyahane'', N''Boya ve kimyasal stoğunu görüntüleme''),
(1032, N''Boyahane.ChemicalStock.Manage'', N''Boyahane'', N''Boya ve kimyasal girişi, sayım düzeltmesi'')
) v([Id], [Code], [Module], [Description])
WHERE NOT EXISTS (SELECT 1 FROM [Core].[Permissions] p WHERE p.[Id] = v.[Id] OR p.[Code] = v.[Code])');
SET IDENTITY_INSERT [Core].[Permissions] OFF;");

            migrationBuilder.Sql(@"INSERT INTO [Core].[RolePermissions] ([RoleId], [PermissionId])
SELECT r.[Id], p.[Id] FROM (VALUES
(N'ADMIN', 1031), (N'ADMIN', 1032), (N'BOYAHANE_MANAGER', 1031), (N'BOYAHANE_MANAGER', 1032),
(N'BOYAHANE_RECEIVING', 1027), (N'BOYAHANE_RECEIVING', 1028),
(N'BOYAHANE_PLANNER', 1027), (N'BOYAHANE_PLANNER', 1028),
(N'BOYAHANE_SALES', 1029), (N'BOYAHANE_SALES', 1030)
) v([RoleCode], [PermissionId])
JOIN [Core].[Roles] r ON r.[Code] = v.[RoleCode]
JOIN [Core].[Permissions] p ON p.[Id] = v.[PermissionId]
WHERE NOT EXISTS (SELECT 1 FROM [Core].[RolePermissions] rp WHERE rp.[RoleId] = r.[Id] AND rp.[PermissionId] = p.[Id]);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DELETE FROM [Core].[RolePermissions] WHERE [PermissionId] IN (1031, 1032);
DELETE FROM [Core].[Permissions] WHERE [Id] IN (1031, 1032);");

            migrationBuilder.DropTable(
                name: "ChemicalMovements",
                schema: "Boyahane");

            migrationBuilder.DropTable(
                name: "ConsumptionLots",
                schema: "Boyahane");

            migrationBuilder.DropTable(
                name: "ChemicalLots",
                schema: "Boyahane");
        }
    }
}
