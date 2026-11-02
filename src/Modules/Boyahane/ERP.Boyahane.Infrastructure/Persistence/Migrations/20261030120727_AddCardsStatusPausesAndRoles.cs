using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Boyahane.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCardsStatusPausesAndRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TabletSessions_OperatorId",
                schema: "Boyahane",
                table: "TabletSessions");

            migrationBuilder.AddColumn<string>(
                name: "EndReason",
                schema: "Boyahane",
                table: "TabletSessions",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Role",
                schema: "Boyahane",
                table: "Operators",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "OPERATOR");

            migrationBuilder.AddColumn<bool>(
                name: "CanDye",
                schema: "Boyahane",
                table: "Machines",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "CanFinish",
                schema: "Boyahane",
                table: "Machines",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "CanPretreat",
                schema: "Boyahane",
                table: "Machines",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "CanPrint",
                schema: "Boyahane",
                table: "Machines",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "CanWashDry",
                schema: "Boyahane",
                table: "Machines",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                schema: "Boyahane",
                table: "Machines",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "AVAILABLE");

            migrationBuilder.CreateTable(
                name: "MachineStatusLogs",
                schema: "Boyahane",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MachineId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ChangedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TabletId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MachineStatusLogs", x => x.Id);
                    table.CheckConstraint("CK_MachineStatusLogs_Status", "[Status] IN ('AVAILABLE', 'BREAKDOWN', 'MAINTENANCE')");
                    table.ForeignKey(
                        name: "FK_MachineStatusLogs_Machines_MachineId",
                        column: x => x.MachineId,
                        principalSchema: "Boyahane",
                        principalTable: "Machines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RfidCards",
                schema: "Boyahane",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Uid = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    CardType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Label = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
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
                    table.PrimaryKey("PK_RfidCards", x => x.Id);
                    table.CheckConstraint("CK_RfidCards_CardType", "[CardType] IN ('OPERATOR', 'BATCH')");
                });

            migrationBuilder.CreateTable(
                name: "StopReasons",
                schema: "Boyahane",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_StopReasons", x => x.Id);
                    table.CheckConstraint("CK_StopReasons_Category", "[Category] IN ('PAUSE', 'BREAKDOWN')");
                });

            migrationBuilder.CreateTable(
                name: "SessionPauses",
                schema: "Boyahane",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SessionId = table.Column<long>(type: "bigint", nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ReasonId = table.Column<int>(type: "int", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionPauses", x => x.Id);
                    table.CheckConstraint("CK_SessionPauses_Kind", "[Kind] IN ('BREAK', 'PAUSE', 'MACHINE_BREAKDOWN')");
                    table.ForeignKey(
                        name: "FK_SessionPauses_StopReasons_ReasonId",
                        column: x => x.ReasonId,
                        principalSchema: "Boyahane",
                        principalTable: "StopReasons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SessionPauses_TabletSessions_SessionId",
                        column: x => x.SessionId,
                        principalSchema: "Boyahane",
                        principalTable: "TabletSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "UX_TabletSessions_OperatorOpen",
                schema: "Boyahane",
                table: "TabletSessions",
                column: "OperatorId",
                unique: true,
                filter: "[EndedAt] IS NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_TabletSessions_EndReason",
                schema: "Boyahane",
                table: "TabletSessions",
                sql: "[EndReason] IN ('LOGOUT', 'TAKEN_OVER', 'MOVED_AWAY', 'CLOSED_BY_OFFICE')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Operators_Role",
                schema: "Boyahane",
                table: "Operators",
                sql: "[Role] IN ('OPERATOR', 'SUPERVISOR', 'MAINTENANCE')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Machines_Status",
                schema: "Boyahane",
                table: "Machines",
                sql: "[Status] IN ('AVAILABLE', 'BREAKDOWN', 'MAINTENANCE')");

            migrationBuilder.CreateIndex(
                name: "IX_MachineStatusLogs_MachineId_StartedAt",
                schema: "Boyahane",
                table: "MachineStatusLogs",
                columns: new[] { "MachineId", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_RfidCards_Uid",
                schema: "Boyahane",
                table: "RfidCards",
                column: "Uid",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_SessionPauses_ReasonId",
                schema: "Boyahane",
                table: "SessionPauses",
                column: "ReasonId");

            migrationBuilder.CreateIndex(
                name: "UX_SessionPauses_Open",
                schema: "Boyahane",
                table: "SessionPauses",
                column: "SessionId",
                unique: true,
                filter: "[EndedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_StopReasons_Code",
                schema: "Boyahane",
                table: "StopReasons",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            // Kartı olmayan numaralar kart listesine alınır; sonraki atamalar listeden denetlenir.
            migrationBuilder.Sql(@"INSERT INTO [Boyahane].[RfidCards] ([Uid], [CardType], [IsActive], [IsDeleted], [CreatedAt])
SELECT DISTINCT o.[CardUid], 'OPERATOR', 1, 0, SYSUTCDATETIME() FROM [Boyahane].[Operators] o WHERE o.[CardUid] IS NOT NULL
UNION
SELECT DISTINCT b.[CardUid], 'BATCH', 1, 0, SYSUTCDATETIME() FROM [Boyahane].[Batches] b WHERE b.[CardUid] IS NOT NULL;");

            migrationBuilder.Sql(@"SET IDENTITY_INSERT [Core].[Permissions] ON;
EXEC(N'INSERT INTO [Core].[Permissions] ([Id], [Code], [Module], [Description])
SELECT 1021, N''Boyahane.Batch.ResolveBreakdown'', N''Boyahane'', N''Makine arızasını ofisten kapatma''
WHERE NOT EXISTS (SELECT 1 FROM [Core].[Permissions] p WHERE p.[Id] = 1021 OR p.[Code] = N''Boyahane.Batch.ResolveBreakdown'')');
SET IDENTITY_INSERT [Core].[Permissions] OFF;");

            // Varsayılan Boyahane rolleri; kurulumdan sonra yetkiler rol ekranından yönetilir.
            migrationBuilder.Sql(@"SET IDENTITY_INSERT [Core].[Roles] ON;
EXEC(N'INSERT INTO [Core].[Roles] ([Id], [Code], [Name], [IsSystem], [ModuleKey])
SELECT v.[Id], v.[Code], v.[Name], v.[IsSystem], v.[ModuleKey] FROM (VALUES
(101, N''BOYAHANE_SALES'', N''Boyahane Satış'', 1, N''Boyahane''),
(102, N''BOYAHANE_RECEIVING'', N''Boyahane Mal Kabul ve Kalite'', 1, N''Boyahane''),
(103, N''BOYAHANE_LAB'', N''Boyahane Laboratuvar'', 1, N''Boyahane''),
(104, N''BOYAHANE_PLANNER'', N''Boyahane Üretim Planlama'', 1, N''Boyahane''),
(105, N''BOYAHANE_MANAGER'', N''Boyahane Yöneticisi'', 1, N''Boyahane'')
) v([Id], [Code], [Name], [IsSystem], [ModuleKey])
WHERE NOT EXISTS (SELECT 1 FROM [Core].[Roles] r WHERE r.[Id] = v.[Id] OR r.[Code] = v.[Code])');
SET IDENTITY_INSERT [Core].[Roles] OFF;");

            migrationBuilder.Sql(@"INSERT INTO [Core].[RolePermissions] ([RoleId], [PermissionId])
SELECT r.[Id], p.[Id] FROM (VALUES
(N'BOYAHANE_SALES', N'Boyahane.View'),
(N'BOYAHANE_SALES', N'Boyahane.Customer.View'),
(N'BOYAHANE_SALES', N'Boyahane.Customer.Manage'),
(N'BOYAHANE_SALES', N'Boyahane.Material.View'),
(N'BOYAHANE_SALES', N'Boyahane.Order.View'),
(N'BOYAHANE_SALES', N'Boyahane.Order.Manage'),
(N'BOYAHANE_SALES', N'Boyahane.Receipt.View'),
(N'BOYAHANE_SALES', N'Boyahane.Inspection.View'),
(N'BOYAHANE_SALES', N'Boyahane.Inspection.CustomerDecision'),
(N'BOYAHANE_SALES', N'Boyahane.Lab.View'),
(N'BOYAHANE_SALES', N'Boyahane.Lab.CustomerDecision'),
(N'BOYAHANE_SALES', N'Boyahane.Batch.View'),
(N'BOYAHANE_SALES', N'Pricing.View'),
(N'BOYAHANE_RECEIVING', N'Boyahane.View'),
(N'BOYAHANE_RECEIVING', N'Boyahane.Customer.View'),
(N'BOYAHANE_RECEIVING', N'Boyahane.Material.View'),
(N'BOYAHANE_RECEIVING', N'Boyahane.Material.Manage'),
(N'BOYAHANE_RECEIVING', N'Boyahane.Order.View'),
(N'BOYAHANE_RECEIVING', N'Boyahane.Receipt.View'),
(N'BOYAHANE_RECEIVING', N'Boyahane.Receipt.Manage'),
(N'BOYAHANE_RECEIVING', N'Boyahane.Inspection.View'),
(N'BOYAHANE_RECEIVING', N'Boyahane.Inspection.Manage'),
(N'BOYAHANE_RECEIVING', N'Boyahane.Batch.View'),
(N'BOYAHANE_LAB', N'Boyahane.View'),
(N'BOYAHANE_LAB', N'Boyahane.Customer.View'),
(N'BOYAHANE_LAB', N'Boyahane.Material.View'),
(N'BOYAHANE_LAB', N'Boyahane.Order.View'),
(N'BOYAHANE_LAB', N'Boyahane.Lab.View'),
(N'BOYAHANE_LAB', N'Boyahane.Lab.Manage'),
(N'BOYAHANE_LAB', N'Boyahane.Lab.CustomerDecision'),
(N'BOYAHANE_LAB', N'Boyahane.Batch.View'),
(N'BOYAHANE_PLANNER', N'Boyahane.View'),
(N'BOYAHANE_PLANNER', N'Boyahane.Customer.View'),
(N'BOYAHANE_PLANNER', N'Boyahane.Material.View'),
(N'BOYAHANE_PLANNER', N'Boyahane.Order.View'),
(N'BOYAHANE_PLANNER', N'Boyahane.Order.Manage'),
(N'BOYAHANE_PLANNER', N'Boyahane.Receipt.View'),
(N'BOYAHANE_PLANNER', N'Boyahane.Inspection.View'),
(N'BOYAHANE_PLANNER', N'Boyahane.Lab.View'),
(N'BOYAHANE_PLANNER', N'Boyahane.Batch.View'),
(N'BOYAHANE_PLANNER', N'Boyahane.Batch.Manage'),
(N'BOYAHANE_PLANNER', N'Boyahane.Floor.View'),
(N'BOYAHANE_MANAGER', N'Boyahane.View'),
(N'BOYAHANE_MANAGER', N'Boyahane.Customer.View'),
(N'BOYAHANE_MANAGER', N'Boyahane.Customer.Manage'),
(N'BOYAHANE_MANAGER', N'Boyahane.Material.View'),
(N'BOYAHANE_MANAGER', N'Boyahane.Material.Manage'),
(N'BOYAHANE_MANAGER', N'Boyahane.Receipt.View'),
(N'BOYAHANE_MANAGER', N'Boyahane.Receipt.Manage'),
(N'BOYAHANE_MANAGER', N'Boyahane.Inspection.View'),
(N'BOYAHANE_MANAGER', N'Boyahane.Inspection.Manage'),
(N'BOYAHANE_MANAGER', N'Boyahane.Inspection.CustomerDecision'),
(N'BOYAHANE_MANAGER', N'Boyahane.Order.View'),
(N'BOYAHANE_MANAGER', N'Boyahane.Order.Manage'),
(N'BOYAHANE_MANAGER', N'Boyahane.Lab.View'),
(N'BOYAHANE_MANAGER', N'Boyahane.Lab.Manage'),
(N'BOYAHANE_MANAGER', N'Boyahane.Lab.CustomerDecision'),
(N'BOYAHANE_MANAGER', N'Boyahane.Batch.View'),
(N'BOYAHANE_MANAGER', N'Boyahane.Batch.Manage'),
(N'BOYAHANE_MANAGER', N'Boyahane.Batch.CapacityOverride'),
(N'BOYAHANE_MANAGER', N'Boyahane.Floor.View'),
(N'BOYAHANE_MANAGER', N'Boyahane.Floor.Manage'),
(N'BOYAHANE_MANAGER', N'Boyahane.Batch.ResolveBreakdown'),
(N'BOYAHANE_MANAGER', N'Pricing.View'),
(N'ADMIN', N'Boyahane.Batch.ResolveBreakdown')
) v([RoleCode], [PermissionCode])
JOIN [Core].[Roles] r ON r.[Code] = v.[RoleCode]
JOIN [Core].[Permissions] p ON p.[Code] = v.[PermissionCode]
WHERE NOT EXISTS (SELECT 1 FROM [Core].[RolePermissions] rp WHERE rp.[RoleId] = r.[Id] AND rp.[PermissionId] = p.[Id]);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DELETE rp FROM [Core].[RolePermissions] rp JOIN [Core].[Roles] r ON r.[Id] = rp.[RoleId] WHERE r.[Code] IN ('BOYAHANE_SALES', 'BOYAHANE_RECEIVING', 'BOYAHANE_LAB', 'BOYAHANE_PLANNER', 'BOYAHANE_MANAGER');
DELETE FROM [Core].[RolePermissions] WHERE [PermissionId] = 1021;
DELETE FROM [Core].[Permissions] WHERE [Id] = 1021;
DELETE FROM [Core].[Roles] WHERE [Code] IN ('BOYAHANE_SALES', 'BOYAHANE_RECEIVING', 'BOYAHANE_LAB', 'BOYAHANE_PLANNER', 'BOYAHANE_MANAGER');");

            migrationBuilder.DropTable(
                name: "MachineStatusLogs",
                schema: "Boyahane");

            migrationBuilder.DropTable(
                name: "RfidCards",
                schema: "Boyahane");

            migrationBuilder.DropTable(
                name: "SessionPauses",
                schema: "Boyahane");

            migrationBuilder.DropTable(
                name: "StopReasons",
                schema: "Boyahane");

            migrationBuilder.DropIndex(
                name: "UX_TabletSessions_OperatorOpen",
                schema: "Boyahane",
                table: "TabletSessions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_TabletSessions_EndReason",
                schema: "Boyahane",
                table: "TabletSessions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Operators_Role",
                schema: "Boyahane",
                table: "Operators");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Machines_Status",
                schema: "Boyahane",
                table: "Machines");

            migrationBuilder.DropColumn(
                name: "EndReason",
                schema: "Boyahane",
                table: "TabletSessions");

            migrationBuilder.DropColumn(
                name: "Role",
                schema: "Boyahane",
                table: "Operators");

            migrationBuilder.DropColumn(
                name: "CanDye",
                schema: "Boyahane",
                table: "Machines");

            migrationBuilder.DropColumn(
                name: "CanFinish",
                schema: "Boyahane",
                table: "Machines");

            migrationBuilder.DropColumn(
                name: "CanPretreat",
                schema: "Boyahane",
                table: "Machines");

            migrationBuilder.DropColumn(
                name: "CanPrint",
                schema: "Boyahane",
                table: "Machines");

            migrationBuilder.DropColumn(
                name: "CanWashDry",
                schema: "Boyahane",
                table: "Machines");

            migrationBuilder.DropColumn(
                name: "Status",
                schema: "Boyahane",
                table: "Machines");

            migrationBuilder.CreateIndex(
                name: "IX_TabletSessions_OperatorId",
                schema: "Boyahane",
                table: "TabletSessions",
                column: "OperatorId");
        }
    }
}
