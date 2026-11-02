using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Orme.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFloorAndReports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ClientRequestId",
                schema: "Orme",
                table: "FabricRolls",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CutByOperatorId",
                schema: "Orme",
                table: "FabricRolls",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BreakGroups",
                schema: "Orme",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
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
                    table.PrimaryKey("PK_BreakGroups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RfidCards",
                schema: "Orme",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Uid = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
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
                });

            migrationBuilder.CreateTable(
                name: "Shifts",
                schema: "Orme",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time(0)", nullable: false),
                    EndTime = table.Column<TimeOnly>(type: "time(0)", nullable: false),
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
                    table.PrimaryKey("PK_Shifts", x => x.Id);
                    table.CheckConstraint("CK_Shifts_Times", "[StartTime] <> [EndTime]");
                });

            migrationBuilder.CreateTable(
                name: "StopReasons",
                schema: "Orme",
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
                    table.CheckConstraint("CK_StopReasons_Category", "[Category] IN ('PAUSE', 'STOP', 'BREAKDOWN', 'PLANNED')");
                });

            migrationBuilder.CreateTable(
                name: "Tablets",
                schema: "Orme",
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
                        principalSchema: "Orme",
                        principalTable: "Machines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Operators",
                schema: "Orme",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(201)", maxLength: 201, nullable: false),
                    Role = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CardUid = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    BreakGroupId = table.Column<int>(type: "int", nullable: true),
                    CanPause = table.Column<bool>(type: "bit", nullable: false),
                    CanReportBreakdown = table.Column<bool>(type: "bit", nullable: false),
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
                    table.CheckConstraint("CK_Operators_Role", "[Role] IN ('KNITTER', 'SUPERVISOR', 'MAINTENANCE')");
                    table.ForeignKey(
                        name: "FK_Operators_BreakGroups_BreakGroupId",
                        column: x => x.BreakGroupId,
                        principalSchema: "Orme",
                        principalTable: "BreakGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BreakSchedules",
                schema: "Orme",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ShiftId = table.Column<int>(type: "int", nullable: false),
                    BreakGroupId = table.Column<int>(type: "int", nullable: true),
                    StartTime = table.Column<TimeOnly>(type: "time(0)", nullable: false),
                    EndTime = table.Column<TimeOnly>(type: "time(0)", nullable: false),
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
                    table.PrimaryKey("PK_BreakSchedules", x => x.Id);
                    table.CheckConstraint("CK_BreakSchedules_Times", "[StartTime] <> [EndTime]");
                    table.ForeignKey(
                        name: "FK_BreakSchedules_BreakGroups_BreakGroupId",
                        column: x => x.BreakGroupId,
                        principalSchema: "Orme",
                        principalTable: "BreakGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BreakSchedules_Shifts_ShiftId",
                        column: x => x.ShiftId,
                        principalSchema: "Orme",
                        principalTable: "Shifts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MachineStops",
                schema: "Orme",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MachineId = table.Column<int>(type: "int", nullable: false),
                    KnitOrderId = table.Column<int>(type: "int", nullable: true),
                    ReasonId = table.Column<int>(type: "int", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StartedBy = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    EndedBy = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    TabletId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MachineStops", x => x.Id);
                    table.CheckConstraint("CK_MachineStops_Category", "[Category] IN ('PAUSE', 'STOP', 'BREAKDOWN', 'PLANNED')");
                    table.ForeignKey(
                        name: "FK_MachineStops_KnitOrders_KnitOrderId",
                        column: x => x.KnitOrderId,
                        principalSchema: "Orme",
                        principalTable: "KnitOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MachineStops_Machines_MachineId",
                        column: x => x.MachineId,
                        principalSchema: "Orme",
                        principalTable: "Machines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MachineStops_StopReasons_ReasonId",
                        column: x => x.ReasonId,
                        principalSchema: "Orme",
                        principalTable: "StopReasons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OperatorMachines",
                schema: "Orme",
                columns: table => new
                {
                    OperatorId = table.Column<int>(type: "int", nullable: false),
                    MachineId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OperatorMachines", x => new { x.OperatorId, x.MachineId });
                    table.ForeignKey(
                        name: "FK_OperatorMachines_Machines_MachineId",
                        column: x => x.MachineId,
                        principalSchema: "Orme",
                        principalTable: "Machines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OperatorMachines_Operators_OperatorId",
                        column: x => x.OperatorId,
                        principalSchema: "Orme",
                        principalTable: "Operators",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OperatorPhotos",
                schema: "Orme",
                columns: table => new
                {
                    OperatorId = table.Column<int>(type: "int", nullable: false),
                    Content = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OperatorPhotos", x => x.OperatorId);
                    table.ForeignKey(
                        name: "FK_OperatorPhotos_Operators_OperatorId",
                        column: x => x.OperatorId,
                        principalSchema: "Orme",
                        principalTable: "Operators",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TabletSessions",
                schema: "Orme",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TabletId = table.Column<int>(type: "int", nullable: false),
                    OperatorId = table.Column<int>(type: "int", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndReason = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TabletSessions", x => x.Id);
                    table.CheckConstraint("CK_TabletSessions_EndReason", "[EndReason] IN ('LOGOUT', 'SHIFT_ENDED', 'TAKEN_OVER', 'MOVED_AWAY', 'CLOSED_BY_OFFICE')");
                    table.ForeignKey(
                        name: "FK_TabletSessions_Operators_OperatorId",
                        column: x => x.OperatorId,
                        principalSchema: "Orme",
                        principalTable: "Operators",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TabletSessions_Tablets_TabletId",
                        column: x => x.TabletId,
                        principalSchema: "Orme",
                        principalTable: "Tablets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SessionPauses",
                schema: "Orme",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SessionId = table.Column<long>(type: "bigint", nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ReasonId = table.Column<int>(type: "int", nullable: true),
                    BreakScheduleId = table.Column<int>(type: "int", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionPauses", x => x.Id);
                    table.CheckConstraint("CK_SessionPauses_Kind", "[Kind] IN ('WAITING_FOR_ORDER', 'BREAK', 'PAUSE')");
                    table.ForeignKey(
                        name: "FK_SessionPauses_BreakSchedules_BreakScheduleId",
                        column: x => x.BreakScheduleId,
                        principalSchema: "Orme",
                        principalTable: "BreakSchedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SessionPauses_StopReasons_ReasonId",
                        column: x => x.ReasonId,
                        principalSchema: "Orme",
                        principalTable: "StopReasons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SessionPauses_TabletSessions_SessionId",
                        column: x => x.SessionId,
                        principalSchema: "Orme",
                        principalTable: "TabletSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FabricRolls_CutByOperatorId",
                schema: "Orme",
                table: "FabricRolls",
                column: "CutByOperatorId");

            migrationBuilder.CreateIndex(
                name: "UX_FabricRolls_ClientRequestId",
                schema: "Orme",
                table: "FabricRolls",
                column: "ClientRequestId",
                unique: true,
                filter: "[ClientRequestId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_BreakGroups_Code",
                schema: "Orme",
                table: "BreakGroups",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_BreakSchedules_BreakGroupId",
                schema: "Orme",
                table: "BreakSchedules",
                column: "BreakGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_BreakSchedules_Code",
                schema: "Orme",
                table: "BreakSchedules",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_BreakSchedules_ShiftId",
                schema: "Orme",
                table: "BreakSchedules",
                column: "ShiftId");

            migrationBuilder.CreateIndex(
                name: "IX_MachineStops_KnitOrderId",
                schema: "Orme",
                table: "MachineStops",
                column: "KnitOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_MachineStops_MachineId_StartedAt",
                schema: "Orme",
                table: "MachineStops",
                columns: new[] { "MachineId", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_MachineStops_ReasonId",
                schema: "Orme",
                table: "MachineStops",
                column: "ReasonId");

            migrationBuilder.CreateIndex(
                name: "UX_MachineStops_Open",
                schema: "Orme",
                table: "MachineStops",
                column: "MachineId",
                unique: true,
                filter: "[EndedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_OperatorMachines_MachineId",
                schema: "Orme",
                table: "OperatorMachines",
                column: "MachineId");

            migrationBuilder.CreateIndex(
                name: "IX_Operators_BreakGroupId",
                schema: "Orme",
                table: "Operators",
                column: "BreakGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_Operators_Code",
                schema: "Orme",
                table: "Operators",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UX_Operators_CardUid",
                schema: "Orme",
                table: "Operators",
                column: "CardUid",
                unique: true,
                filter: "[CardUid] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_RfidCards_Uid",
                schema: "Orme",
                table: "RfidCards",
                column: "Uid",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_SessionPauses_BreakScheduleId",
                schema: "Orme",
                table: "SessionPauses",
                column: "BreakScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionPauses_ReasonId",
                schema: "Orme",
                table: "SessionPauses",
                column: "ReasonId");

            migrationBuilder.CreateIndex(
                name: "UX_SessionPauses_Open",
                schema: "Orme",
                table: "SessionPauses",
                column: "SessionId",
                unique: true,
                filter: "[EndedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Shifts_Code",
                schema: "Orme",
                table: "Shifts",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_StopReasons_Code",
                schema: "Orme",
                table: "StopReasons",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Tablets_TabletCode",
                schema: "Orme",
                table: "Tablets",
                column: "TabletCode",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UX_Tablets_Device",
                schema: "Orme",
                table: "Tablets",
                column: "DeviceIdentifier",
                unique: true,
                filter: "[DeviceIdentifier] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UX_Tablets_Machine",
                schema: "Orme",
                table: "Tablets",
                column: "MachineId",
                unique: true,
                filter: "[MachineId] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UX_TabletSessions_Open",
                schema: "Orme",
                table: "TabletSessions",
                column: "TabletId",
                unique: true,
                filter: "[EndedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "UX_TabletSessions_OperatorOpen",
                schema: "Orme",
                table: "TabletSessions",
                column: "OperatorId",
                unique: true,
                filter: "[EndedAt] IS NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_FabricRolls_Operators_CutByOperatorId",
                schema: "Orme",
                table: "FabricRolls",
                column: "CutByOperatorId",
                principalSchema: "Orme",
                principalTable: "Operators",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // Başlangıç ara nedenleri; duruş ve arıza nedenlerini işletme tanımlar.
            migrationBuilder.Sql(@"EXEC(N'INSERT INTO [Orme].[StopReasons] ([Code], [Name], [Category], [SortOrder], [IsActive], [CreatedAt], [IsDeleted])
SELECT v.[Code], v.[Name], ''PAUSE'', v.[SortOrder], 1, SYSUTCDATETIME(), 0 FROM (VALUES
(N''SU'', N''Su'', 10),
(N''TUVALET'', N''Tuvalet'', 20),
(N''KISISEL'', N''Kişisel ihtiyaç'', 30),
(N''BEKLEME'', N''Bekleme'', 40),
(N''DIGER'', N''Diğer'', 50)
) v([Code], [Name], [SortOrder])
WHERE NOT EXISTS (SELECT 1 FROM [Orme].[StopReasons] r WHERE r.[Code] = v.[Code])');");

            // Yeni yetkiler; varsayılan olarak yalnızca Sistem Yöneticisine verilir, diğer roller rol ekranından yetkilendirilir.
            migrationBuilder.Sql(@"SET IDENTITY_INSERT [Core].[Permissions] ON;
EXEC(N'INSERT INTO [Core].[Permissions] ([Id], [Code], [Module], [Description])
SELECT v.[Id], v.[Code], v.[Module], v.[Description] FROM (VALUES
(3027, N''Orme.Floor.View'', N''Orme'', N''Örme personelini, kartları, tabletleri ve oturumları görüntüleme''),
(3028, N''Orme.Floor.Manage'', N''Orme'', N''Personel, kart, tablet ve duruş nedeni tanımlama; ofisten duruş ve oturum işlemleri''),
(3029, N''Orme.Report.View'', N''Orme'', N''Makine verimi, sipariş takibi ve kalite raporları'')
) v([Id], [Code], [Module], [Description])
WHERE NOT EXISTS (SELECT 1 FROM [Core].[Permissions] p WHERE p.[Id] = v.[Id] OR p.[Code] = v.[Code])');
SET IDENTITY_INSERT [Core].[Permissions] OFF;");

            migrationBuilder.Sql(@"INSERT INTO [Core].[RolePermissions] ([RoleId], [PermissionId])
SELECT v.[RoleId], v.[PermissionId] FROM (VALUES
(1, 3027), (1, 3028), (1, 3029)
) v([RoleId], [PermissionId])
WHERE EXISTS (SELECT 1 FROM [Core].[Roles] r WHERE r.[Id] = v.[RoleId])
  AND EXISTS (SELECT 1 FROM [Core].[Permissions] p WHERE p.[Id] = v.[PermissionId])
  AND NOT EXISTS (SELECT 1 FROM [Core].[RolePermissions] rp WHERE rp.[RoleId] = v.[RoleId] AND rp.[PermissionId] = v.[PermissionId]);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DELETE FROM [Core].[RolePermissions] WHERE [PermissionId] BETWEEN 3027 AND 3029;
DELETE FROM [Core].[Permissions] WHERE [Id] BETWEEN 3027 AND 3029;");

            migrationBuilder.DropForeignKey(
                name: "FK_FabricRolls_Operators_CutByOperatorId",
                schema: "Orme",
                table: "FabricRolls");

            migrationBuilder.DropTable(
                name: "MachineStops",
                schema: "Orme");

            migrationBuilder.DropTable(
                name: "OperatorMachines",
                schema: "Orme");

            migrationBuilder.DropTable(
                name: "OperatorPhotos",
                schema: "Orme");

            migrationBuilder.DropTable(
                name: "RfidCards",
                schema: "Orme");

            migrationBuilder.DropTable(
                name: "SessionPauses",
                schema: "Orme");

            migrationBuilder.DropTable(
                name: "BreakSchedules",
                schema: "Orme");

            migrationBuilder.DropTable(
                name: "StopReasons",
                schema: "Orme");

            migrationBuilder.DropTable(
                name: "TabletSessions",
                schema: "Orme");

            migrationBuilder.DropTable(
                name: "Shifts",
                schema: "Orme");

            migrationBuilder.DropTable(
                name: "Operators",
                schema: "Orme");

            migrationBuilder.DropTable(
                name: "Tablets",
                schema: "Orme");

            migrationBuilder.DropTable(
                name: "BreakGroups",
                schema: "Orme");

            migrationBuilder.DropIndex(
                name: "IX_FabricRolls_CutByOperatorId",
                schema: "Orme",
                table: "FabricRolls");

            migrationBuilder.DropIndex(
                name: "UX_FabricRolls_ClientRequestId",
                schema: "Orme",
                table: "FabricRolls");

            migrationBuilder.DropColumn(
                name: "ClientRequestId",
                schema: "Orme",
                table: "FabricRolls");

            migrationBuilder.DropColumn(
                name: "CutByOperatorId",
                schema: "Orme",
                table: "FabricRolls");
        }
    }
}
