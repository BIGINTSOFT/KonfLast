using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Dokuma.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOperatorCardAndPauses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_StopReasons_Category",
                schema: "Dokuma",
                table: "StopReasons");

            migrationBuilder.DropCheckConstraint(
                name: "CK_LoomStops_Category",
                schema: "Dokuma",
                table: "LoomStops");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                schema: "Dokuma",
                table: "Operators",
                type: "nvarchar(201)",
                maxLength: 201,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(150)",
                oldMaxLength: 150);

            migrationBuilder.AddColumn<bool>(
                name: "CanPause",
                schema: "Dokuma",
                table: "Operators",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CanReportBreakdown",
                schema: "Dokuma",
                table: "Operators",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                schema: "Dokuma",
                table: "Operators",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LastName",
                schema: "Dokuma",
                table: "Operators",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "OperatorLooms",
                schema: "Dokuma",
                columns: table => new
                {
                    OperatorId = table.Column<int>(type: "int", nullable: false),
                    LoomId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OperatorLooms", x => new { x.OperatorId, x.LoomId });
                    table.ForeignKey(
                        name: "FK_OperatorLooms_Looms_LoomId",
                        column: x => x.LoomId,
                        principalSchema: "Dokuma",
                        principalTable: "Looms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OperatorLooms_Operators_OperatorId",
                        column: x => x.OperatorId,
                        principalSchema: "Dokuma",
                        principalTable: "Operators",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OperatorPhotos",
                schema: "Dokuma",
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
                        principalSchema: "Dokuma",
                        principalTable: "Operators",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SessionPauses",
                schema: "Dokuma",
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
                    table.CheckConstraint("CK_SessionPauses_Kind", "[Kind] IN ('BREAK', 'PAUSE')");
                    table.ForeignKey(
                        name: "FK_SessionPauses_StopReasons_ReasonId",
                        column: x => x.ReasonId,
                        principalSchema: "Dokuma",
                        principalTable: "StopReasons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SessionPauses_TabletSessions_SessionId",
                        column: x => x.SessionId,
                        principalSchema: "Dokuma",
                        principalTable: "TabletSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_StopReasons_Category",
                schema: "Dokuma",
                table: "StopReasons",
                sql: "[Category] IN ('PAUSE', 'STOP', 'BREAKDOWN', 'PLANNED')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_LoomStops_Category",
                schema: "Dokuma",
                table: "LoomStops",
                sql: "[Category] IN ('PAUSE', 'STOP', 'BREAKDOWN', 'PLANNED')");

            migrationBuilder.CreateIndex(
                name: "IX_OperatorLooms_LoomId",
                schema: "Dokuma",
                table: "OperatorLooms",
                column: "LoomId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionPauses_ReasonId",
                schema: "Dokuma",
                table: "SessionPauses",
                column: "ReasonId");

            migrationBuilder.CreateIndex(
                name: "UX_SessionPauses_Open",
                schema: "Dokuma",
                table: "SessionPauses",
                column: "SessionId",
                unique: true,
                filter: "[EndedAt] IS NULL");

            migrationBuilder.Sql(@"EXEC(N'UPDATE [Dokuma].[Operators] SET
    [FirstName] = CASE WHEN CHARINDEX('' '', [Name]) > 0 THEN LEFT([Name], CHARINDEX('' '', [Name]) - 1) ELSE LEFT([Name], 100) END,
    [LastName] = CASE WHEN CHARINDEX('' '', [Name]) > 0 THEN LEFT(LTRIM(SUBSTRING([Name], CHARINDEX('' '', [Name]) + 1, 200)), 100) ELSE '''' END')");

            migrationBuilder.Sql(@"INSERT INTO [Dokuma].[StopReasons] ([Code], [Name], [Category], [SortOrder], [IsActive], [CreatedAt], [IsDeleted])
SELECT v.[Code], v.[Name], 'PAUSE', v.[SortOrder], 1, SYSUTCDATETIME(), 0 FROM (VALUES
(N'SU', N'Su', 10),
(N'TUVALET', N'Tuvalet', 20),
(N'KISISEL', N'Kişisel ihtiyaç', 30),
(N'BEKLEME', N'Bekleme', 40),
(N'DIGER', N'Diğer', 50)
) v([Code], [Name], [SortOrder])
WHERE NOT EXISTS (SELECT 1 FROM [Dokuma].[StopReasons] r WHERE r.[Code] = v.[Code])");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DELETE FROM [Dokuma].[StopReasons] WHERE [Category] = 'PAUSE';");

            migrationBuilder.DropTable(
                name: "OperatorLooms",
                schema: "Dokuma");

            migrationBuilder.DropTable(
                name: "OperatorPhotos",
                schema: "Dokuma");

            migrationBuilder.DropTable(
                name: "SessionPauses",
                schema: "Dokuma");

            migrationBuilder.DropCheckConstraint(
                name: "CK_StopReasons_Category",
                schema: "Dokuma",
                table: "StopReasons");

            migrationBuilder.DropCheckConstraint(
                name: "CK_LoomStops_Category",
                schema: "Dokuma",
                table: "LoomStops");

            migrationBuilder.DropColumn(
                name: "CanPause",
                schema: "Dokuma",
                table: "Operators");

            migrationBuilder.DropColumn(
                name: "CanReportBreakdown",
                schema: "Dokuma",
                table: "Operators");

            migrationBuilder.DropColumn(
                name: "FirstName",
                schema: "Dokuma",
                table: "Operators");

            migrationBuilder.DropColumn(
                name: "LastName",
                schema: "Dokuma",
                table: "Operators");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                schema: "Dokuma",
                table: "Operators",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(201)",
                oldMaxLength: 201);

            migrationBuilder.AddCheckConstraint(
                name: "CK_StopReasons_Category",
                schema: "Dokuma",
                table: "StopReasons",
                sql: "[Category] IN ('STOP', 'BREAKDOWN', 'PLANNED')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_LoomStops_Category",
                schema: "Dokuma",
                table: "LoomStops",
                sql: "[Category] IN ('STOP', 'BREAKDOWN', 'PLANNED')");
        }
    }
}
