using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Boyahane.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBatches : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Machines",
                schema: "Boyahane",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    MachineType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CapacityKg = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    DefaultLiquorRatio = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: true),
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
                    table.CheckConstraint("CK_Machines_CapacityKg", "[CapacityKg] > 0");
                    table.CheckConstraint("CK_Machines_MachineType", "[MachineType] IN ('JET', 'OVERFLOW', 'JIGGER', 'STENTER', 'YARN_VESSEL')");
                });

            migrationBuilder.CreateTable(
                name: "Batches",
                schema: "Boyahane",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BatchNo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    MachineId = table.Column<int>(type: "int", nullable: false),
                    RecipeTrialId = table.Column<int>(type: "int", nullable: false),
                    LiquorRatio = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: false),
                    PlannedDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CapacityOverrideBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CapacityOverrideNote = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Batches", x => x.Id);
                    table.CheckConstraint("CK_Batches_LiquorRatio", "[LiquorRatio] > 0");
                    table.CheckConstraint("CK_Batches_Status", "[Status] IN ('PLANNED', 'ON_MACHINE', 'COMPLETED', 'CANCELLED')");
                    table.ForeignKey(
                        name: "FK_Batches_LabTrials_RecipeTrialId",
                        column: x => x.RecipeTrialId,
                        principalSchema: "Boyahane",
                        principalTable: "LabTrials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Batches_Machines_MachineId",
                        column: x => x.MachineId,
                        principalSchema: "Boyahane",
                        principalTable: "Machines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BatchItems",
                schema: "Boyahane",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BatchId = table.Column<int>(type: "int", nullable: false),
                    OrderLineId = table.Column<int>(type: "int", nullable: false),
                    RollId = table.Column<int>(type: "int", nullable: true),
                    YarnLotId = table.Column<int>(type: "int", nullable: true),
                    Kilograms = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    Meters = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BatchItems", x => x.Id);
                    table.CheckConstraint("CK_BatchItems_Item", "([RollId] IS NOT NULL AND [YarnLotId] IS NULL) OR ([RollId] IS NULL AND [YarnLotId] IS NOT NULL)");
                    table.CheckConstraint("CK_BatchItems_Kilograms", "[Kilograms] > 0");
                    table.ForeignKey(
                        name: "FK_BatchItems_Batches_BatchId",
                        column: x => x.BatchId,
                        principalSchema: "Boyahane",
                        principalTable: "Batches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BatchItems_FabricRolls_RollId",
                        column: x => x.RollId,
                        principalSchema: "Boyahane",
                        principalTable: "FabricRolls",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BatchItems_OrderLines_OrderLineId",
                        column: x => x.OrderLineId,
                        principalSchema: "Boyahane",
                        principalTable: "OrderLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BatchItems_YarnLots_YarnLotId",
                        column: x => x.YarnLotId,
                        principalSchema: "Boyahane",
                        principalTable: "YarnLots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Batches_BatchNo",
                schema: "Boyahane",
                table: "Batches",
                column: "BatchNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Batches_RecipeTrialId",
                schema: "Boyahane",
                table: "Batches",
                column: "RecipeTrialId");

            migrationBuilder.CreateIndex(
                name: "IX_Batches_Status",
                schema: "Boyahane",
                table: "Batches",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "UX_Batches_OnMachine",
                schema: "Boyahane",
                table: "Batches",
                column: "MachineId",
                unique: true,
                filter: "[Status] = 'ON_MACHINE'");

            migrationBuilder.CreateIndex(
                name: "IX_BatchItems_BatchId",
                schema: "Boyahane",
                table: "BatchItems",
                column: "BatchId");

            migrationBuilder.CreateIndex(
                name: "IX_BatchItems_OrderLineId",
                schema: "Boyahane",
                table: "BatchItems",
                column: "OrderLineId");

            migrationBuilder.CreateIndex(
                name: "UX_BatchItems_ActiveRoll",
                schema: "Boyahane",
                table: "BatchItems",
                column: "RollId",
                unique: true,
                filter: "[IsActive] = 1 AND [RollId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UX_BatchItems_ActiveYarnLot",
                schema: "Boyahane",
                table: "BatchItems",
                column: "YarnLotId",
                unique: true,
                filter: "[IsActive] = 1 AND [YarnLotId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Machines_Code",
                schema: "Boyahane",
                table: "Machines",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            // Yeni yetkiler; varsayılan olarak yalnızca Sistem Yöneticisine verilir, diğer roller rol ekranından yetkilendirilir.
            migrationBuilder.Sql(@"SET IDENTITY_INSERT [Core].[Permissions] ON;
EXEC(N'INSERT INTO [Core].[Permissions] ([Id], [Code], [Module], [Description])
SELECT v.[Id], v.[Code], v.[Module], v.[Description] FROM (VALUES
(1016, N''Boyahane.Batch.View'', N''Boyahane'', N''Partileri ve makineleri görüntüleme''),
(1017, N''Boyahane.Batch.Manage'', N''Boyahane'', N''Parti açma ve yürütme, makine kartları''),
(1018, N''Boyahane.Batch.CapacityOverride'', N''Boyahane'', N''Makine kapasitesini aşan parti açma'')
) v([Id], [Code], [Module], [Description])
WHERE NOT EXISTS (SELECT 1 FROM [Core].[Permissions] p WHERE p.[Id] = v.[Id] OR p.[Code] = v.[Code])');
SET IDENTITY_INSERT [Core].[Permissions] OFF;");

            migrationBuilder.Sql(@"INSERT INTO [Core].[RolePermissions] ([RoleId], [PermissionId])
SELECT v.[RoleId], v.[PermissionId] FROM (VALUES
(1, 1016),
(1, 1017),
(1, 1018)
) v([RoleId], [PermissionId])
WHERE EXISTS (SELECT 1 FROM [Core].[Roles] r WHERE r.[Id] = v.[RoleId])
  AND EXISTS (SELECT 1 FROM [Core].[Permissions] p WHERE p.[Id] = v.[PermissionId])
  AND NOT EXISTS (SELECT 1 FROM [Core].[RolePermissions] rp WHERE rp.[RoleId] = v.[RoleId] AND rp.[PermissionId] = v.[PermissionId]);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DELETE FROM [Core].[RolePermissions] WHERE [PermissionId] IN (1016, 1017, 1018);
DELETE FROM [Core].[Permissions] WHERE [Id] IN (1016, 1017, 1018);");

            migrationBuilder.DropTable(
                name: "BatchItems",
                schema: "Boyahane");

            migrationBuilder.DropTable(
                name: "Batches",
                schema: "Boyahane");

            migrationBuilder.DropTable(
                name: "Machines",
                schema: "Boyahane");
        }
    }
}
