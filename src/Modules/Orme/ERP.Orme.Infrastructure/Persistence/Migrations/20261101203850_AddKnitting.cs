using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Orme.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddKnitting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "KnitOrders",
                schema: "Orme",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KnitOrderNo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    OrderLineId = table.Column<int>(type: "int", nullable: false),
                    PlannedDate = table.Column<DateOnly>(type: "date", nullable: false),
                    PlannedKilograms = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    CompletedOn = table.Column<DateOnly>(type: "date", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnitOrders", x => x.Id);
                    table.CheckConstraint("CK_KnitOrders_PlannedKilograms", "[PlannedKilograms] > 0");
                    table.CheckConstraint("CK_KnitOrders_Status", "[Status] IN ('OPEN', 'COMPLETED', 'CANCELLED')");
                    table.ForeignKey(
                        name: "FK_KnitOrders_OrderLines_OrderLineId",
                        column: x => x.OrderLineId,
                        principalSchema: "Orme",
                        principalTable: "OrderLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FabricRolls",
                schema: "Orme",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RollNo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    KnitOrderId = table.Column<int>(type: "int", nullable: false),
                    MachineId = table.Column<int>(type: "int", nullable: false),
                    CutOn = table.Column<DateOnly>(type: "date", nullable: false),
                    Kilograms = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    Meters = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
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
                    table.CheckConstraint("CK_FabricRolls_Meters", "[Meters] IS NULL OR [Meters] > 0");
                    table.CheckConstraint("CK_FabricRolls_Status", "[Status] IN ('PENDING_INSPECTION', 'FIRST_QUALITY', 'SECOND_QUALITY', 'REJECTED')");
                    table.ForeignKey(
                        name: "FK_FabricRolls_KnitOrders_KnitOrderId",
                        column: x => x.KnitOrderId,
                        principalSchema: "Orme",
                        principalTable: "KnitOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FabricRolls_Machines_MachineId",
                        column: x => x.MachineId,
                        principalSchema: "Orme",
                        principalTable: "Machines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "KnitOrderMachines",
                schema: "Orme",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KnitOrderId = table.Column<int>(type: "int", nullable: false),
                    MachineId = table.Column<int>(type: "int", nullable: false),
                    AssignedOn = table.Column<DateOnly>(type: "date", nullable: false),
                    ReleasedOn = table.Column<DateOnly>(type: "date", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnitOrderMachines", x => x.Id);
                    table.CheckConstraint("CK_KnitOrderMachines_Released", "[ReleasedOn] IS NULL OR [ReleasedOn] >= [AssignedOn]");
                    table.ForeignKey(
                        name: "FK_KnitOrderMachines_KnitOrders_KnitOrderId",
                        column: x => x.KnitOrderId,
                        principalSchema: "Orme",
                        principalTable: "KnitOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KnitOrderMachines_Machines_MachineId",
                        column: x => x.MachineId,
                        principalSchema: "Orme",
                        principalTable: "Machines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "YarnIssues",
                schema: "Orme",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KnitOrderId = table.Column<int>(type: "int", nullable: false),
                    YarnLotId = table.Column<int>(type: "int", nullable: false),
                    IssuedOn = table.Column<DateOnly>(type: "date", nullable: false),
                    Kilograms = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_YarnIssues", x => x.Id);
                    table.CheckConstraint("CK_YarnIssues_Kilograms", "[Kilograms] > 0");
                    table.ForeignKey(
                        name: "FK_YarnIssues_KnitOrders_KnitOrderId",
                        column: x => x.KnitOrderId,
                        principalSchema: "Orme",
                        principalTable: "KnitOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_YarnIssues_YarnLots_YarnLotId",
                        column: x => x.YarnLotId,
                        principalSchema: "Orme",
                        principalTable: "YarnLots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "YarnTakeBacks",
                schema: "Orme",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KnitOrderId = table.Column<int>(type: "int", nullable: false),
                    YarnLotId = table.Column<int>(type: "int", nullable: false),
                    TakenOn = table.Column<DateOnly>(type: "date", nullable: false),
                    Kilograms = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_YarnTakeBacks", x => x.Id);
                    table.CheckConstraint("CK_YarnTakeBacks_Kilograms", "[Kilograms] > 0");
                    table.ForeignKey(
                        name: "FK_YarnTakeBacks_KnitOrders_KnitOrderId",
                        column: x => x.KnitOrderId,
                        principalSchema: "Orme",
                        principalTable: "KnitOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_YarnTakeBacks_YarnLots_YarnLotId",
                        column: x => x.YarnLotId,
                        principalSchema: "Orme",
                        principalTable: "YarnLots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FabricRolls_KnitOrderId",
                schema: "Orme",
                table: "FabricRolls",
                column: "KnitOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_FabricRolls_MachineId",
                schema: "Orme",
                table: "FabricRolls",
                column: "MachineId");

            migrationBuilder.CreateIndex(
                name: "IX_FabricRolls_RollNo",
                schema: "Orme",
                table: "FabricRolls",
                column: "RollNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FabricRolls_Status",
                schema: "Orme",
                table: "FabricRolls",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_KnitOrderMachines_KnitOrderId",
                schema: "Orme",
                table: "KnitOrderMachines",
                column: "KnitOrderId");

            migrationBuilder.CreateIndex(
                name: "UX_KnitOrderMachines_RunningMachine",
                schema: "Orme",
                table: "KnitOrderMachines",
                column: "MachineId",
                unique: true,
                filter: "[ReleasedOn] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_KnitOrders_KnitOrderNo",
                schema: "Orme",
                table: "KnitOrders",
                column: "KnitOrderNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KnitOrders_OrderLineId",
                schema: "Orme",
                table: "KnitOrders",
                column: "OrderLineId");

            migrationBuilder.CreateIndex(
                name: "IX_YarnIssues_KnitOrderId",
                schema: "Orme",
                table: "YarnIssues",
                column: "KnitOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_YarnIssues_YarnLotId",
                schema: "Orme",
                table: "YarnIssues",
                column: "YarnLotId");

            migrationBuilder.CreateIndex(
                name: "IX_YarnTakeBacks_KnitOrderId",
                schema: "Orme",
                table: "YarnTakeBacks",
                column: "KnitOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_YarnTakeBacks_YarnLotId",
                schema: "Orme",
                table: "YarnTakeBacks",
                column: "YarnLotId");

            // Yeni yetkiler; varsayılan olarak yalnızca Sistem Yöneticisine verilir, diğer roller rol ekranından yetkilendirilir.
            migrationBuilder.Sql(@"SET IDENTITY_INSERT [Core].[Permissions] ON;
EXEC(N'INSERT INTO [Core].[Permissions] ([Id], [Code], [Module], [Description])
SELECT v.[Id], v.[Code], v.[Module], v.[Description] FROM (VALUES
(3014, N''Orme.Knitting.View'', N''Orme'', N''Örme emirlerini, makine panosunu ve ham topları görüntüleme''),
(3015, N''Orme.Knitting.Manage'', N''Orme'', N''Örme emri açma, makine atama, iplik verme ve top kaydetme'')
) v([Id], [Code], [Module], [Description])
WHERE NOT EXISTS (SELECT 1 FROM [Core].[Permissions] p WHERE p.[Id] = v.[Id] OR p.[Code] = v.[Code])');
SET IDENTITY_INSERT [Core].[Permissions] OFF;");

            migrationBuilder.Sql(@"INSERT INTO [Core].[RolePermissions] ([RoleId], [PermissionId])
SELECT v.[RoleId], v.[PermissionId] FROM (VALUES
(1, 3014), (1, 3015)
) v([RoleId], [PermissionId])
WHERE EXISTS (SELECT 1 FROM [Core].[Roles] r WHERE r.[Id] = v.[RoleId])
  AND EXISTS (SELECT 1 FROM [Core].[Permissions] p WHERE p.[Id] = v.[PermissionId])
  AND NOT EXISTS (SELECT 1 FROM [Core].[RolePermissions] rp WHERE rp.[RoleId] = v.[RoleId] AND rp.[PermissionId] = v.[PermissionId]);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DELETE FROM [Core].[RolePermissions] WHERE [PermissionId] BETWEEN 3014 AND 3015;
DELETE FROM [Core].[Permissions] WHERE [Id] BETWEEN 3014 AND 3015;");

            migrationBuilder.DropTable(
                name: "FabricRolls",
                schema: "Orme");

            migrationBuilder.DropTable(
                name: "KnitOrderMachines",
                schema: "Orme");

            migrationBuilder.DropTable(
                name: "YarnIssues",
                schema: "Orme");

            migrationBuilder.DropTable(
                name: "YarnTakeBacks",
                schema: "Orme");

            migrationBuilder.DropTable(
                name: "KnitOrders",
                schema: "Orme");
        }
    }
}
