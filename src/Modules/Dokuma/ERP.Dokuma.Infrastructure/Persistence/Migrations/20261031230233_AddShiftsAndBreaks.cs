using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Dokuma.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddShiftsAndBreaks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_TabletSessions_EndReason",
                schema: "Dokuma",
                table: "TabletSessions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_SessionPauses_Kind",
                schema: "Dokuma",
                table: "SessionPauses");

            migrationBuilder.AddColumn<int>(
                name: "BreakScheduleId",
                schema: "Dokuma",
                table: "SessionPauses",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BreakGroupId",
                schema: "Dokuma",
                table: "Operators",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BreakGroups",
                schema: "Dokuma",
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
                name: "Shifts",
                schema: "Dokuma",
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
                name: "BreakSchedules",
                schema: "Dokuma",
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
                        principalSchema: "Dokuma",
                        principalTable: "BreakGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BreakSchedules_Shifts_ShiftId",
                        column: x => x.ShiftId,
                        principalSchema: "Dokuma",
                        principalTable: "Shifts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_TabletSessions_EndReason",
                schema: "Dokuma",
                table: "TabletSessions",
                sql: "[EndReason] IN ('LOGOUT', 'SHIFT_ENDED', 'TAKEN_OVER', 'MOVED_AWAY', 'CLOSED_BY_OFFICE')");

            migrationBuilder.CreateIndex(
                name: "IX_SessionPauses_BreakScheduleId",
                schema: "Dokuma",
                table: "SessionPauses",
                column: "BreakScheduleId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_SessionPauses_Kind",
                schema: "Dokuma",
                table: "SessionPauses",
                sql: "[Kind] IN ('WAITING_FOR_BEAM', 'BREAK', 'PAUSE')");

            migrationBuilder.CreateIndex(
                name: "IX_Operators_BreakGroupId",
                schema: "Dokuma",
                table: "Operators",
                column: "BreakGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_BreakGroups_Code",
                schema: "Dokuma",
                table: "BreakGroups",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_BreakSchedules_BreakGroupId",
                schema: "Dokuma",
                table: "BreakSchedules",
                column: "BreakGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_BreakSchedules_Code",
                schema: "Dokuma",
                table: "BreakSchedules",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_BreakSchedules_ShiftId",
                schema: "Dokuma",
                table: "BreakSchedules",
                column: "ShiftId");

            migrationBuilder.CreateIndex(
                name: "IX_Shifts_Code",
                schema: "Dokuma",
                table: "Shifts",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.AddForeignKey(
                name: "FK_Operators_BreakGroups_BreakGroupId",
                schema: "Dokuma",
                table: "Operators",
                column: "BreakGroupId",
                principalSchema: "Dokuma",
                principalTable: "BreakGroups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SessionPauses_BreakSchedules_BreakScheduleId",
                schema: "Dokuma",
                table: "SessionPauses",
                column: "BreakScheduleId",
                principalSchema: "Dokuma",
                principalTable: "BreakSchedules",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Operators_BreakGroups_BreakGroupId",
                schema: "Dokuma",
                table: "Operators");

            migrationBuilder.DropForeignKey(
                name: "FK_SessionPauses_BreakSchedules_BreakScheduleId",
                schema: "Dokuma",
                table: "SessionPauses");

            migrationBuilder.DropTable(
                name: "BreakSchedules",
                schema: "Dokuma");

            migrationBuilder.DropTable(
                name: "BreakGroups",
                schema: "Dokuma");

            migrationBuilder.DropTable(
                name: "Shifts",
                schema: "Dokuma");

            migrationBuilder.DropCheckConstraint(
                name: "CK_TabletSessions_EndReason",
                schema: "Dokuma",
                table: "TabletSessions");

            migrationBuilder.DropIndex(
                name: "IX_SessionPauses_BreakScheduleId",
                schema: "Dokuma",
                table: "SessionPauses");

            migrationBuilder.DropCheckConstraint(
                name: "CK_SessionPauses_Kind",
                schema: "Dokuma",
                table: "SessionPauses");

            migrationBuilder.DropIndex(
                name: "IX_Operators_BreakGroupId",
                schema: "Dokuma",
                table: "Operators");

            migrationBuilder.DropColumn(
                name: "BreakScheduleId",
                schema: "Dokuma",
                table: "SessionPauses");

            migrationBuilder.DropColumn(
                name: "BreakGroupId",
                schema: "Dokuma",
                table: "Operators");

            migrationBuilder.AddCheckConstraint(
                name: "CK_TabletSessions_EndReason",
                schema: "Dokuma",
                table: "TabletSessions",
                sql: "[EndReason] IN ('LOGOUT', 'TAKEN_OVER', 'MOVED_AWAY', 'CLOSED_BY_OFFICE')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_SessionPauses_Kind",
                schema: "Dokuma",
                table: "SessionPauses",
                sql: "[Kind] IN ('BREAK', 'PAUSE')");
        }
    }
}
