using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Boyahane.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOperationsAndRoutes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_BatchSteps_StepType",
                schema: "Boyahane",
                table: "BatchSteps");

            migrationBuilder.AddColumn<int>(
                name: "OperationId",
                schema: "Boyahane",
                table: "BatchSteps",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "RouteId",
                schema: "Boyahane",
                table: "Batches",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Operations",
                schema: "Boyahane",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Behavior = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    NeedsMachine = table.Column<bool>(type: "bit", nullable: false),
                    StandardMinutes = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: true),
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
                    table.PrimaryKey("PK_Operations", x => x.Id);
                    table.CheckConstraint("CK_Operations_Behavior", "[Behavior] IN ('STANDARD', 'DYEING', 'COLOR_CHECK', 'FINAL_QUALITY')");
                });

            migrationBuilder.CreateTable(
                name: "Routes",
                schema: "Boyahane",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ProcessType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_Routes", x => x.Id);
                    table.CheckConstraint("CK_Routes_ProcessType", "[ProcessType] IN ('FABRIC_DYEING', 'YARN_DYEING', 'FINISHING', 'PRINTING')");
                });

            migrationBuilder.CreateTable(
                name: "MachineOperations",
                schema: "Boyahane",
                columns: table => new
                {
                    MachineId = table.Column<int>(type: "int", nullable: false),
                    OperationId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MachineOperations", x => new { x.MachineId, x.OperationId });
                    table.ForeignKey(
                        name: "FK_MachineOperations_Machines_MachineId",
                        column: x => x.MachineId,
                        principalSchema: "Boyahane",
                        principalTable: "Machines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MachineOperations_Operations_OperationId",
                        column: x => x.OperationId,
                        principalSchema: "Boyahane",
                        principalTable: "Operations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RouteSteps",
                schema: "Boyahane",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RouteId = table.Column<int>(type: "int", nullable: false),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    OperationId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RouteSteps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RouteSteps_Operations_OperationId",
                        column: x => x.OperationId,
                        principalSchema: "Boyahane",
                        principalTable: "Operations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RouteSteps_Routes_RouteId",
                        column: x => x.RouteId,
                        principalSchema: "Boyahane",
                        principalTable: "Routes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BatchSteps_OperationId",
                schema: "Boyahane",
                table: "BatchSteps",
                column: "OperationId");

            migrationBuilder.CreateIndex(
                name: "IX_Batches_RouteId",
                schema: "Boyahane",
                table: "Batches",
                column: "RouteId");

            migrationBuilder.CreateIndex(
                name: "IX_MachineOperations_OperationId",
                schema: "Boyahane",
                table: "MachineOperations",
                column: "OperationId");

            migrationBuilder.CreateIndex(
                name: "IX_Operations_Code",
                schema: "Boyahane",
                table: "Operations",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Routes_Code",
                schema: "Boyahane",
                table: "Routes",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UX_Routes_Default",
                schema: "Boyahane",
                table: "Routes",
                column: "ProcessType",
                unique: true,
                filter: "[IsDefault] = 1 AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_RouteSteps_OperationId",
                schema: "Boyahane",
                table: "RouteSteps",
                column: "OperationId");

            migrationBuilder.CreateIndex(
                name: "IX_RouteSteps_RouteId_Sequence",
                schema: "Boyahane",
                table: "RouteSteps",
                columns: new[] { "RouteId", "Sequence" },
                unique: true);

            // Varsayılan operasyonlar ve rotalar: ekrandan değiştirilebilir. Önceki sabit adımların karşılığıdır.
            migrationBuilder.Sql(@"INSERT INTO [Boyahane].[Operations] ([Code], [Name], [Behavior], [NeedsMachine], [IsActive], [IsDeleted], [CreatedAt])
SELECT v.[Code], v.[Name], v.[Behavior], v.[NeedsMachine], 1, 0, SYSUTCDATETIME() FROM (VALUES
(N'ON-TERBIYE', N'Ön terbiye', 'STANDARD', 1),
(N'BOYAMA', N'Boyama', 'DYEING', 1),
(N'RENK-KONTROL', N'Renk kontrolü', 'COLOR_CHECK', 0),
(N'YIKAMA-KURUTMA', N'Yıkama-kurutma', 'STANDARD', 1),
(N'TERBIYE-APRE', N'Terbiye-apre', 'STANDARD', 1),
(N'BASKI', N'Baskı', 'STANDARD', 1),
(N'FINAL-KALITE', N'Final kalite', 'FINAL_QUALITY', 0),
(N'PAKETLEME', N'Paketleme', 'STANDARD', 0)
) v([Code], [Name], [Behavior], [NeedsMachine]);");

            migrationBuilder.Sql(@"INSERT INTO [Boyahane].[Routes] ([Code], [Name], [ProcessType], [IsDefault], [IsActive], [IsDeleted], [CreatedAt])
SELECT v.[Code], v.[Name], v.[ProcessType], 1, 1, 0, SYSUTCDATETIME() FROM (VALUES
(N'KUMAS-BOYAMA', N'Kumaş boyama', 'FABRIC_DYEING'),
(N'IPLIK-BOYAMA', N'İplik boyama', 'YARN_DYEING'),
(N'BASKI', N'Baskı', 'PRINTING'),
(N'TERBIYE', N'Terbiye', 'FINISHING')
) v([Code], [Name], [ProcessType]);");

            migrationBuilder.Sql(@"INSERT INTO [Boyahane].[RouteSteps] ([RouteId], [Sequence], [OperationId], [CreatedAt])
SELECT r.[Id], v.[Seq], o.[Id], SYSUTCDATETIME() FROM (VALUES
(N'KUMAS-BOYAMA', 1, N'ON-TERBIYE'),
(N'KUMAS-BOYAMA', 2, N'BOYAMA'),
(N'KUMAS-BOYAMA', 3, N'RENK-KONTROL'),
(N'KUMAS-BOYAMA', 4, N'YIKAMA-KURUTMA'),
(N'KUMAS-BOYAMA', 5, N'TERBIYE-APRE'),
(N'KUMAS-BOYAMA', 6, N'FINAL-KALITE'),
(N'KUMAS-BOYAMA', 7, N'PAKETLEME'),
(N'IPLIK-BOYAMA', 1, N'BOYAMA'),
(N'IPLIK-BOYAMA', 2, N'RENK-KONTROL'),
(N'IPLIK-BOYAMA', 3, N'YIKAMA-KURUTMA'),
(N'IPLIK-BOYAMA', 4, N'FINAL-KALITE'),
(N'IPLIK-BOYAMA', 5, N'PAKETLEME'),
(N'BASKI', 1, N'BASKI'),
(N'BASKI', 2, N'YIKAMA-KURUTMA'),
(N'BASKI', 3, N'FINAL-KALITE'),
(N'BASKI', 4, N'PAKETLEME'),
(N'TERBIYE', 1, N'TERBIYE-APRE'),
(N'TERBIYE', 2, N'FINAL-KALITE'),
(N'TERBIYE', 3, N'PAKETLEME')
) v([RouteCode], [Seq], [OperationCode])
JOIN [Boyahane].[Routes] r ON r.[Code] = v.[RouteCode]
JOIN [Boyahane].[Operations] o ON o.[Code] = v.[OperationCode];");

            // Var olan parti adımları ve makine işaretleri yeni yapıya taşınır.
            migrationBuilder.Sql(@"EXEC(N'UPDATE s SET s.[OperationId] = o.[Id]
FROM [Boyahane].[BatchSteps] s
JOIN (VALUES (''PRETREATMENT'', N''ON-TERBIYE''), (''DYEING'', N''BOYAMA''), (''COLOR_CHECK'', N''RENK-KONTROL''), (''WASH_DRY'', N''YIKAMA-KURUTMA''), (''FINISHING'', N''TERBIYE-APRE''), (''PRINTING'', N''BASKI''), (''FINAL_QUALITY'', N''FINAL-KALITE''), (''PACKAGING'', N''PAKETLEME'')) m([StepType], [Code]) ON m.[StepType] = s.[StepType]
JOIN [Boyahane].[Operations] o ON o.[Code] = m.[Code];');");

            migrationBuilder.Sql(@"EXEC(N'INSERT INTO [Boyahane].[MachineOperations] ([MachineId], [OperationId])
SELECT m.[Id], o.[Id] FROM [Boyahane].[Machines] m
JOIN [Boyahane].[Operations] o ON (o.[Code] = N''ON-TERBIYE'' AND m.[CanPretreat] = 1)
  OR (o.[Code] = N''BOYAMA'' AND m.[CanDye] = 1)
  OR (o.[Code] = N''YIKAMA-KURUTMA'' AND m.[CanWashDry] = 1)
  OR (o.[Code] = N''TERBIYE-APRE'' AND m.[CanFinish] = 1)
  OR (o.[Code] = N''BASKI'' AND m.[CanPrint] = 1);');");

            migrationBuilder.Sql(@"UPDATE b SET b.[RouteId] = r.[Id]
FROM [Boyahane].[Batches] b
CROSS APPLY (SELECT TOP 1 l.[ProcessType] FROM [Boyahane].[BatchItems] i JOIN [Boyahane].[OrderLines] l ON l.[Id] = i.[OrderLineId] WHERE i.[BatchId] = b.[Id] ORDER BY i.[Id]) p
JOIN [Boyahane].[Routes] r ON r.[ProcessType] = p.[ProcessType] AND r.[IsDefault] = 1;");

            migrationBuilder.Sql(@"SET IDENTITY_INSERT [Core].[Permissions] ON;
EXEC(N'INSERT INTO [Core].[Permissions] ([Id], [Code], [Module], [Description])
SELECT v.[Id], v.[Code], v.[Module], v.[Description] FROM (VALUES
(1022, N''Boyahane.Operation.View'', N''Boyahane'', N''Operasyonları ve rotaları görüntüleme''),
(1023, N''Boyahane.Operation.Manage'', N''Boyahane'', N''Operasyon ve rota tanımları''),
(1024, N''Boyahane.Report.View'', N''Boyahane'', N''Boyahane raporları'')
) v([Id], [Code], [Module], [Description])
WHERE NOT EXISTS (SELECT 1 FROM [Core].[Permissions] p WHERE p.[Id] = v.[Id] OR p.[Code] = v.[Code])');
SET IDENTITY_INSERT [Core].[Permissions] OFF;");

            migrationBuilder.Sql(@"INSERT INTO [Core].[RolePermissions] ([RoleId], [PermissionId])
SELECT r.[Id], p.[Id] FROM (VALUES
(N'ADMIN', N'Boyahane.Operation.View'),
(N'ADMIN', N'Boyahane.Operation.Manage'),
(N'ADMIN', N'Boyahane.Report.View'),
(N'BOYAHANE_MANAGER', N'Boyahane.Operation.View'),
(N'BOYAHANE_MANAGER', N'Boyahane.Operation.Manage'),
(N'BOYAHANE_MANAGER', N'Boyahane.Report.View'),
(N'BOYAHANE_PLANNER', N'Boyahane.Operation.View'),
(N'BOYAHANE_PLANNER', N'Boyahane.Operation.Manage'),
(N'BOYAHANE_PLANNER', N'Boyahane.Report.View'),
(N'BOYAHANE_SALES', N'Boyahane.Report.View')
) v([RoleCode], [PermissionCode])
JOIN [Core].[Roles] r ON r.[Code] = v.[RoleCode]
JOIN [Core].[Permissions] p ON p.[Code] = v.[PermissionCode]
WHERE NOT EXISTS (SELECT 1 FROM [Core].[RolePermissions] rp WHERE rp.[RoleId] = r.[Id] AND rp.[PermissionId] = p.[Id]);");

            migrationBuilder.DropColumn(
                name: "StepType",
                schema: "Boyahane",
                table: "BatchSteps");

            migrationBuilder.DropColumn(
                name: "CanPretreat",
                schema: "Boyahane",
                table: "Machines");

            migrationBuilder.DropColumn(
                name: "CanDye",
                schema: "Boyahane",
                table: "Machines");

            migrationBuilder.DropColumn(
                name: "CanWashDry",
                schema: "Boyahane",
                table: "Machines");

            migrationBuilder.DropColumn(
                name: "CanFinish",
                schema: "Boyahane",
                table: "Machines");

            migrationBuilder.DropColumn(
                name: "CanPrint",
                schema: "Boyahane",
                table: "Machines");

            migrationBuilder.AddForeignKey(
                name: "FK_Batches_Routes_RouteId",
                schema: "Boyahane",
                table: "Batches",
                column: "RouteId",
                principalSchema: "Boyahane",
                principalTable: "Routes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BatchSteps_Operations_OperationId",
                schema: "Boyahane",
                table: "BatchSteps",
                column: "OperationId",
                principalSchema: "Boyahane",
                principalTable: "Operations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DELETE FROM [Core].[RolePermissions] WHERE [PermissionId] IN (1022, 1023, 1024);
DELETE FROM [Core].[Permissions] WHERE [Id] IN (1022, 1023, 1024);");

            migrationBuilder.DropForeignKey(
                name: "FK_Batches_Routes_RouteId",
                schema: "Boyahane",
                table: "Batches");

            migrationBuilder.DropForeignKey(
                name: "FK_BatchSteps_Operations_OperationId",
                schema: "Boyahane",
                table: "BatchSteps");

            migrationBuilder.DropTable(
                name: "MachineOperations",
                schema: "Boyahane");

            migrationBuilder.DropTable(
                name: "RouteSteps",
                schema: "Boyahane");

            migrationBuilder.DropTable(
                name: "Operations",
                schema: "Boyahane");

            migrationBuilder.DropTable(
                name: "Routes",
                schema: "Boyahane");

            migrationBuilder.DropIndex(
                name: "IX_BatchSteps_OperationId",
                schema: "Boyahane",
                table: "BatchSteps");

            migrationBuilder.DropIndex(
                name: "IX_Batches_RouteId",
                schema: "Boyahane",
                table: "Batches");

            migrationBuilder.DropColumn(
                name: "OperationId",
                schema: "Boyahane",
                table: "BatchSteps");

            migrationBuilder.DropColumn(
                name: "RouteId",
                schema: "Boyahane",
                table: "Batches");

            migrationBuilder.AddColumn<bool>(
                name: "CanDye",
                schema: "Boyahane",
                table: "Machines",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CanFinish",
                schema: "Boyahane",
                table: "Machines",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CanPretreat",
                schema: "Boyahane",
                table: "Machines",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CanPrint",
                schema: "Boyahane",
                table: "Machines",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CanWashDry",
                schema: "Boyahane",
                table: "Machines",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "StepType",
                schema: "Boyahane",
                table: "BatchSteps",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddCheckConstraint(
                name: "CK_BatchSteps_StepType",
                schema: "Boyahane",
                table: "BatchSteps",
                sql: "[StepType] IN ('PRETREATMENT', 'DYEING', 'COLOR_CHECK', 'WASH_DRY', 'FINISHING', 'PRINTING', 'FINAL_QUALITY', 'PACKAGING')");
        }
    }
}
