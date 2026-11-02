using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Boyahane.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOperatorCardAndPauseReasons : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Name",
                schema: "Boyahane",
                table: "Operators",
                type: "nvarchar(201)",
                maxLength: 201,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(150)",
                oldMaxLength: 150);

            migrationBuilder.AddColumn<bool>(
                name: "CanPause",
                schema: "Boyahane",
                table: "Operators",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CanReportBreakdown",
                schema: "Boyahane",
                table: "Operators",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                schema: "Boyahane",
                table: "Operators",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LastName",
                schema: "Boyahane",
                table: "Operators",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "OperatorMachines",
                schema: "Boyahane",
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
                        principalSchema: "Boyahane",
                        principalTable: "Machines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OperatorMachines_Operators_OperatorId",
                        column: x => x.OperatorId,
                        principalSchema: "Boyahane",
                        principalTable: "Operators",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OperatorOperations",
                schema: "Boyahane",
                columns: table => new
                {
                    OperatorId = table.Column<int>(type: "int", nullable: false),
                    OperationId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OperatorOperations", x => new { x.OperatorId, x.OperationId });
                    table.ForeignKey(
                        name: "FK_OperatorOperations_Operations_OperationId",
                        column: x => x.OperationId,
                        principalSchema: "Boyahane",
                        principalTable: "Operations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OperatorOperations_Operators_OperatorId",
                        column: x => x.OperatorId,
                        principalSchema: "Boyahane",
                        principalTable: "Operators",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OperatorPhotos",
                schema: "Boyahane",
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
                        principalSchema: "Boyahane",
                        principalTable: "Operators",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OperatorMachines_MachineId",
                schema: "Boyahane",
                table: "OperatorMachines",
                column: "MachineId");

            migrationBuilder.CreateIndex(
                name: "IX_OperatorOperations_OperationId",
                schema: "Boyahane",
                table: "OperatorOperations",
                column: "OperationId");

            // Mevcut ad soyad ayrılır; mevcut operatörler yetkinliksiz kalıp adım başlatamaz duruma düşmesin diye tüm operasyonlar verilir.
            migrationBuilder.Sql(@"EXEC(N'UPDATE [Boyahane].[Operators] SET
    [FirstName] = CASE WHEN CHARINDEX('' '', [Name]) > 0 THEN LEFT([Name], CHARINDEX('' '', [Name]) - 1) ELSE LEFT([Name], 100) END,
    [LastName] = CASE WHEN CHARINDEX('' '', [Name]) > 0 THEN LEFT(LTRIM(SUBSTRING([Name], CHARINDEX('' '', [Name]) + 1, 200)), 100) ELSE '''' END')");

            migrationBuilder.Sql(@"INSERT INTO [Boyahane].[OperatorOperations] ([OperatorId], [OperationId])
SELECT o.[Id], p.[Id] FROM [Boyahane].[Operators] o CROSS JOIN [Boyahane].[Operations] p WHERE o.[IsDeleted] = 0 AND p.[IsDeleted] = 0");

            migrationBuilder.Sql(@"INSERT INTO [Boyahane].[StopReasons] ([Code], [Name], [Category], [SortOrder], [IsActive], [CreatedAt], [IsDeleted])
SELECT v.[Code], v.[Name], 'PAUSE', v.[SortOrder], 1, SYSUTCDATETIME(), 0 FROM (VALUES
(N'SU', N'Su', 10),
(N'TUVALET', N'Tuvalet', 20),
(N'KISISEL', N'Kişisel ihtiyaç', 30),
(N'BEKLEME', N'Bekleme', 40),
(N'DIGER', N'Diğer', 50)
) v([Code], [Name], [SortOrder])
WHERE NOT EXISTS (SELECT 1 FROM [Boyahane].[StopReasons] r WHERE r.[Code] = v.[Code])");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OperatorMachines",
                schema: "Boyahane");

            migrationBuilder.DropTable(
                name: "OperatorOperations",
                schema: "Boyahane");

            migrationBuilder.DropTable(
                name: "OperatorPhotos",
                schema: "Boyahane");

            migrationBuilder.DropColumn(
                name: "CanPause",
                schema: "Boyahane",
                table: "Operators");

            migrationBuilder.DropColumn(
                name: "CanReportBreakdown",
                schema: "Boyahane",
                table: "Operators");

            migrationBuilder.DropColumn(
                name: "FirstName",
                schema: "Boyahane",
                table: "Operators");

            migrationBuilder.DropColumn(
                name: "LastName",
                schema: "Boyahane",
                table: "Operators");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                schema: "Boyahane",
                table: "Operators",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(201)",
                oldMaxLength: 201);
        }
    }
}
