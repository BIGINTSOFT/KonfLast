using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Boyahane.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBatchStepsAndTablets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_Batches_OnMachine",
                schema: "Boyahane",
                table: "Batches");

            migrationBuilder.AddColumn<string>(
                name: "FinalNote",
                schema: "Boyahane",
                table: "BatchItems",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FinalResult",
                schema: "Boyahane",
                table: "BatchItems",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CardUid",
                schema: "Boyahane",
                table: "Batches",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReworkOfBatchId",
                schema: "Boyahane",
                table: "Batches",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Operators",
                schema: "Boyahane",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    CardUid = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
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
                    table.PrimaryKey("PK_Operators", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StepRequests",
                schema: "Boyahane",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StepId = table.Column<int>(type: "int", nullable: false),
                    ReceivedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StepRequests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tablets",
                schema: "Boyahane",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TabletCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    MachineId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    DeviceIdentifier = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DeviceKeyHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    LastSeenAt = table.Column<DateTime>(type: "datetime2", nullable: true),
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
                    table.PrimaryKey("PK_Tablets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tablets_Machines_MachineId",
                        column: x => x.MachineId,
                        principalSchema: "Boyahane",
                        principalTable: "Machines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BatchSteps",
                schema: "Boyahane",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BatchId = table.Column<int>(type: "int", nullable: false),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    StepType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ReworkKind = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    MachineId = table.Column<int>(type: "int", nullable: true),
                    TabletId = table.Column<int>(type: "int", nullable: true),
                    StartedByOperatorId = table.Column<int>(type: "int", nullable: true),
                    CompletedByOperatorId = table.Column<int>(type: "int", nullable: true),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ColorResult = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BatchSteps", x => x.Id);
                    table.CheckConstraint("CK_BatchSteps_ColorResult", "[ColorResult] IN ('APPROVED', 'ADDITION', 'REDO')");
                    table.CheckConstraint("CK_BatchSteps_ReworkKind", "[ReworkKind] IN ('APPROVED', 'ADDITION', 'REDO')");
                    table.CheckConstraint("CK_BatchSteps_Status", "[Status] IN ('PENDING', 'IN_PROGRESS', 'COMPLETED', 'SKIPPED')");
                    table.CheckConstraint("CK_BatchSteps_StepType", "[StepType] IN ('PRETREATMENT', 'DYEING', 'COLOR_CHECK', 'WASH_DRY', 'FINISHING', 'PRINTING', 'FINAL_QUALITY', 'PACKAGING')");
                    table.ForeignKey(
                        name: "FK_BatchSteps_Batches_BatchId",
                        column: x => x.BatchId,
                        principalSchema: "Boyahane",
                        principalTable: "Batches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BatchSteps_Machines_MachineId",
                        column: x => x.MachineId,
                        principalSchema: "Boyahane",
                        principalTable: "Machines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BatchSteps_Operators_CompletedByOperatorId",
                        column: x => x.CompletedByOperatorId,
                        principalSchema: "Boyahane",
                        principalTable: "Operators",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BatchSteps_Operators_StartedByOperatorId",
                        column: x => x.StartedByOperatorId,
                        principalSchema: "Boyahane",
                        principalTable: "Operators",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BatchSteps_Tablets_TabletId",
                        column: x => x.TabletId,
                        principalSchema: "Boyahane",
                        principalTable: "Tablets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TabletSessions",
                schema: "Boyahane",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TabletId = table.Column<int>(type: "int", nullable: false),
                    OperatorId = table.Column<int>(type: "int", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TabletSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TabletSessions_Operators_OperatorId",
                        column: x => x.OperatorId,
                        principalSchema: "Boyahane",
                        principalTable: "Operators",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TabletSessions_Tablets_TabletId",
                        column: x => x.TabletId,
                        principalSchema: "Boyahane",
                        principalTable: "Tablets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BatchConsumptions",
                schema: "Boyahane",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StepId = table.Column<int>(type: "int", nullable: false),
                    DyeChemicalId = table.Column<int>(type: "int", nullable: false),
                    PlannedKilograms = table.Column<decimal>(type: "decimal(12,3)", precision: 12, scale: 3, nullable: false),
                    ActualKilograms = table.Column<decimal>(type: "decimal(12,3)", precision: 12, scale: 3, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BatchConsumptions", x => x.Id);
                    table.CheckConstraint("CK_BatchConsumptions_Actual", "[ActualKilograms] >= 0");
                    table.ForeignKey(
                        name: "FK_BatchConsumptions_BatchSteps_StepId",
                        column: x => x.StepId,
                        principalSchema: "Boyahane",
                        principalTable: "BatchSteps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BatchConsumptions_DyeChemicals_DyeChemicalId",
                        column: x => x.DyeChemicalId,
                        principalSchema: "Boyahane",
                        principalTable: "DyeChemicals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_BatchItems_FinalResult",
                schema: "Boyahane",
                table: "BatchItems",
                sql: "[FinalResult] IN ('APPROVED', 'REJECTED', 'REWORK')");

            migrationBuilder.CreateIndex(
                name: "IX_Batches_MachineId",
                schema: "Boyahane",
                table: "Batches",
                column: "MachineId");

            migrationBuilder.CreateIndex(
                name: "IX_Batches_ReworkOfBatchId",
                schema: "Boyahane",
                table: "Batches",
                column: "ReworkOfBatchId");

            migrationBuilder.CreateIndex(
                name: "UX_Batches_CardUid",
                schema: "Boyahane",
                table: "Batches",
                column: "CardUid",
                unique: true,
                filter: "[CardUid] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_BatchConsumptions_DyeChemicalId",
                schema: "Boyahane",
                table: "BatchConsumptions",
                column: "DyeChemicalId");

            migrationBuilder.CreateIndex(
                name: "IX_BatchConsumptions_StepId",
                schema: "Boyahane",
                table: "BatchConsumptions",
                column: "StepId");

            migrationBuilder.CreateIndex(
                name: "IX_BatchSteps_BatchId_Sequence",
                schema: "Boyahane",
                table: "BatchSteps",
                columns: new[] { "BatchId", "Sequence" });

            migrationBuilder.CreateIndex(
                name: "IX_BatchSteps_CompletedByOperatorId",
                schema: "Boyahane",
                table: "BatchSteps",
                column: "CompletedByOperatorId");

            migrationBuilder.CreateIndex(
                name: "IX_BatchSteps_StartedByOperatorId",
                schema: "Boyahane",
                table: "BatchSteps",
                column: "StartedByOperatorId");

            migrationBuilder.CreateIndex(
                name: "IX_BatchSteps_TabletId",
                schema: "Boyahane",
                table: "BatchSteps",
                column: "TabletId");

            migrationBuilder.CreateIndex(
                name: "UX_BatchSteps_BatchInProgress",
                schema: "Boyahane",
                table: "BatchSteps",
                column: "BatchId",
                unique: true,
                filter: "[Status] = 'IN_PROGRESS'");

            migrationBuilder.CreateIndex(
                name: "UX_BatchSteps_MachineInProgress",
                schema: "Boyahane",
                table: "BatchSteps",
                column: "MachineId",
                unique: true,
                filter: "[Status] = 'IN_PROGRESS' AND [MachineId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Operators_Code",
                schema: "Boyahane",
                table: "Operators",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UX_Operators_CardUid",
                schema: "Boyahane",
                table: "Operators",
                column: "CardUid",
                unique: true,
                filter: "[CardUid] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_StepRequests_RequestId",
                schema: "Boyahane",
                table: "StepRequests",
                column: "RequestId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tablets_TabletCode",
                schema: "Boyahane",
                table: "Tablets",
                column: "TabletCode",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UX_Tablets_Device",
                schema: "Boyahane",
                table: "Tablets",
                column: "DeviceIdentifier",
                unique: true,
                filter: "[DeviceIdentifier] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UX_Tablets_Machine",
                schema: "Boyahane",
                table: "Tablets",
                column: "MachineId",
                unique: true,
                filter: "[MachineId] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_TabletSessions_OperatorId",
                schema: "Boyahane",
                table: "TabletSessions",
                column: "OperatorId");

            migrationBuilder.CreateIndex(
                name: "UX_TabletSessions_Open",
                schema: "Boyahane",
                table: "TabletSessions",
                column: "TabletId",
                unique: true,
                filter: "[EndedAt] IS NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Batches_Batches_ReworkOfBatchId",
                schema: "Boyahane",
                table: "Batches",
                column: "ReworkOfBatchId",
                principalSchema: "Boyahane",
                principalTable: "Batches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // Yeni yetkiler; varsayılan olarak yalnızca Sistem Yöneticisine verilir, diğer roller rol ekranından yetkilendirilir.
            migrationBuilder.Sql(@"SET IDENTITY_INSERT [Core].[Permissions] ON;
EXEC(N'INSERT INTO [Core].[Permissions] ([Id], [Code], [Module], [Description])
SELECT v.[Id], v.[Code], v.[Module], v.[Description] FROM (VALUES
(1019, N''Boyahane.Floor.View'', N''Boyahane'', N''Personeli ve tabletleri görüntüleme''),
(1020, N''Boyahane.Floor.Manage'', N''Boyahane'', N''Personel ve tablet tanımları, cihaz sıfırlama'')
) v([Id], [Code], [Module], [Description])
WHERE NOT EXISTS (SELECT 1 FROM [Core].[Permissions] p WHERE p.[Id] = v.[Id] OR p.[Code] = v.[Code])');
SET IDENTITY_INSERT [Core].[Permissions] OFF;");

            migrationBuilder.Sql(@"INSERT INTO [Core].[RolePermissions] ([RoleId], [PermissionId])
SELECT v.[RoleId], v.[PermissionId] FROM (VALUES
(1, 1019),
(1, 1020)
) v([RoleId], [PermissionId])
WHERE EXISTS (SELECT 1 FROM [Core].[Roles] r WHERE r.[Id] = v.[RoleId])
  AND EXISTS (SELECT 1 FROM [Core].[Permissions] p WHERE p.[Id] = v.[PermissionId])
  AND NOT EXISTS (SELECT 1 FROM [Core].[RolePermissions] rp WHERE rp.[RoleId] = v.[RoleId] AND rp.[PermissionId] = v.[PermissionId]);");

            // Açık partilere işlem türünün adımları yazılır; makinedeki partinin ilk adımı işlemde sayılır.
            migrationBuilder.Sql(@"EXEC(N'INSERT INTO [Boyahane].[BatchSteps] ([BatchId], [Sequence], [StepType], [Status], [CreatedAt])
SELECT b.[Id], t.[Seq], t.[StepType], ''PENDING'', SYSUTCDATETIME()
FROM [Boyahane].[Batches] b
CROSS APPLY (SELECT TOP 1 l.[ProcessType] FROM [Boyahane].[BatchItems] i JOIN [Boyahane].[OrderLines] l ON l.[Id] = i.[OrderLineId] WHERE i.[BatchId] = b.[Id] ORDER BY i.[Id]) p
JOIN (VALUES
(''FABRIC_DYEING'',1,''PRETREATMENT''),(''FABRIC_DYEING'',2,''DYEING''),(''FABRIC_DYEING'',3,''COLOR_CHECK''),(''FABRIC_DYEING'',4,''WASH_DRY''),(''FABRIC_DYEING'',5,''FINISHING''),(''FABRIC_DYEING'',6,''FINAL_QUALITY''),(''FABRIC_DYEING'',7,''PACKAGING''),
(''YARN_DYEING'',1,''DYEING''),(''YARN_DYEING'',2,''COLOR_CHECK''),(''YARN_DYEING'',3,''WASH_DRY''),(''YARN_DYEING'',4,''FINAL_QUALITY''),(''YARN_DYEING'',5,''PACKAGING''),
(''PRINTING'',1,''PRINTING''),(''PRINTING'',2,''WASH_DRY''),(''PRINTING'',3,''FINAL_QUALITY''),(''PRINTING'',4,''PACKAGING''),
(''FINISHING'',1,''FINISHING''),(''FINISHING'',2,''FINAL_QUALITY''),(''FINISHING'',3,''PACKAGING'')
) t([ProcessType],[Seq],[StepType]) ON t.[ProcessType] = p.[ProcessType]
WHERE b.[Status] IN (''PLANNED'', ''ON_MACHINE'');');");

            migrationBuilder.Sql(@"UPDATE s SET s.[Status] = 'IN_PROGRESS', s.[MachineId] = b.[MachineId], s.[StartedAt] = b.[StartedAt]
FROM [Boyahane].[BatchSteps] s JOIN [Boyahane].[Batches] b ON b.[Id] = s.[BatchId]
WHERE b.[Status] = 'ON_MACHINE' AND s.[Sequence] = 1;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DELETE FROM [Core].[RolePermissions] WHERE [PermissionId] IN (1019, 1020);
DELETE FROM [Core].[Permissions] WHERE [Id] IN (1019, 1020);");

            migrationBuilder.DropForeignKey(
                name: "FK_Batches_Batches_ReworkOfBatchId",
                schema: "Boyahane",
                table: "Batches");

            migrationBuilder.DropTable(
                name: "BatchConsumptions",
                schema: "Boyahane");

            migrationBuilder.DropTable(
                name: "StepRequests",
                schema: "Boyahane");

            migrationBuilder.DropTable(
                name: "TabletSessions",
                schema: "Boyahane");

            migrationBuilder.DropTable(
                name: "BatchSteps",
                schema: "Boyahane");

            migrationBuilder.DropTable(
                name: "Operators",
                schema: "Boyahane");

            migrationBuilder.DropTable(
                name: "Tablets",
                schema: "Boyahane");

            migrationBuilder.DropCheckConstraint(
                name: "CK_BatchItems_FinalResult",
                schema: "Boyahane",
                table: "BatchItems");

            migrationBuilder.DropIndex(
                name: "IX_Batches_MachineId",
                schema: "Boyahane",
                table: "Batches");

            migrationBuilder.DropIndex(
                name: "IX_Batches_ReworkOfBatchId",
                schema: "Boyahane",
                table: "Batches");

            migrationBuilder.DropIndex(
                name: "UX_Batches_CardUid",
                schema: "Boyahane",
                table: "Batches");

            migrationBuilder.DropColumn(
                name: "FinalNote",
                schema: "Boyahane",
                table: "BatchItems");

            migrationBuilder.DropColumn(
                name: "FinalResult",
                schema: "Boyahane",
                table: "BatchItems");

            migrationBuilder.DropColumn(
                name: "CardUid",
                schema: "Boyahane",
                table: "Batches");

            migrationBuilder.DropColumn(
                name: "ReworkOfBatchId",
                schema: "Boyahane",
                table: "Batches");

            migrationBuilder.CreateIndex(
                name: "UX_Batches_OnMachine",
                schema: "Boyahane",
                table: "Batches",
                column: "MachineId",
                unique: true,
                filter: "[Status] = 'ON_MACHINE'");
        }
    }
}
