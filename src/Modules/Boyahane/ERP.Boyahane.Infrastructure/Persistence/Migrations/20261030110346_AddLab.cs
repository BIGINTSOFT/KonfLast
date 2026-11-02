using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Boyahane.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLab : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DyeChemicals",
                schema: "Boyahane",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
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
                    table.PrimaryKey("PK_DyeChemicals", x => x.Id);
                    table.CheckConstraint("CK_DyeChemicals_Kind", "[Kind] IN ('DYE', 'CHEMICAL')");
                });

            migrationBuilder.CreateTable(
                name: "LabRequests",
                schema: "Boyahane",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestNo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    OrderLineId = table.Column<int>(type: "int", nullable: false),
                    TargetNote = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LabRequests", x => x.Id);
                    table.CheckConstraint("CK_LabRequests_Status", "[Status] IN ('OPEN', 'APPROVED', 'CANCELLED')");
                    table.ForeignKey(
                        name: "FK_LabRequests_OrderLines_OrderLineId",
                        column: x => x.OrderLineId,
                        principalSchema: "Boyahane",
                        principalTable: "OrderLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LabTrials",
                schema: "Boyahane",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestId = table.Column<int>(type: "int", nullable: false),
                    TrialCode = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Decision = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    DecisionBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DecisionChannel = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    DecisionOn = table.Column<DateOnly>(type: "date", nullable: true),
                    DecisionNote = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DecisionRecordedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ReusedFromTrialId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LabTrials", x => x.Id);
                    table.CheckConstraint("CK_LabTrials_Decision", "[Decision] IN ('PENDING', 'ACCEPTED', 'REJECTED')");
                    table.ForeignKey(
                        name: "FK_LabTrials_LabRequests_RequestId",
                        column: x => x.RequestId,
                        principalSchema: "Boyahane",
                        principalTable: "LabRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LabTrials_LabTrials_ReusedFromTrialId",
                        column: x => x.ReusedFromTrialId,
                        principalSchema: "Boyahane",
                        principalTable: "LabTrials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LabTrialItems",
                schema: "Boyahane",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TrialId = table.Column<int>(type: "int", nullable: false),
                    DyeChemicalId = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(10,4)", precision: 10, scale: 4, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LabTrialItems", x => x.Id);
                    table.CheckConstraint("CK_LabTrialItems_Amount", "[Amount] > 0");
                    table.ForeignKey(
                        name: "FK_LabTrialItems_DyeChemicals_DyeChemicalId",
                        column: x => x.DyeChemicalId,
                        principalSchema: "Boyahane",
                        principalTable: "DyeChemicals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LabTrialItems_LabTrials_TrialId",
                        column: x => x.TrialId,
                        principalSchema: "Boyahane",
                        principalTable: "LabTrials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DyeChemicals_Code",
                schema: "Boyahane",
                table: "DyeChemicals",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_LabRequests_RequestNo",
                schema: "Boyahane",
                table: "LabRequests",
                column: "RequestNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_LabRequests_ActivePerLine",
                schema: "Boyahane",
                table: "LabRequests",
                column: "OrderLineId",
                unique: true,
                filter: "[Status] <> 'CANCELLED'");

            migrationBuilder.CreateIndex(
                name: "IX_LabTrialItems_DyeChemicalId",
                schema: "Boyahane",
                table: "LabTrialItems",
                column: "DyeChemicalId");

            migrationBuilder.CreateIndex(
                name: "IX_LabTrialItems_TrialId_DyeChemicalId",
                schema: "Boyahane",
                table: "LabTrialItems",
                columns: new[] { "TrialId", "DyeChemicalId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LabTrials_RequestId_TrialCode",
                schema: "Boyahane",
                table: "LabTrials",
                columns: new[] { "RequestId", "TrialCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LabTrials_ReusedFromTrialId",
                schema: "Boyahane",
                table: "LabTrials",
                column: "ReusedFromTrialId");

            // Yeni yetkiler; varsayılan olarak yalnızca Sistem Yöneticisine verilir, diğer roller rol ekranından yetkilendirilir.
            migrationBuilder.Sql(@"SET IDENTITY_INSERT [Core].[Permissions] ON;
EXEC(N'INSERT INTO [Core].[Permissions] ([Id], [Code], [Module], [Description])
SELECT v.[Id], v.[Code], v.[Module], v.[Description] FROM (VALUES
(1013, N''Boyahane.Lab.View'', N''Boyahane'', N''Laboratuvarı görüntüleme''),
(1014, N''Boyahane.Lab.Manage'', N''Boyahane'', N''Laboratuvar talebi ve denemeleri, boya / kimyasal kartları''),
(1015, N''Boyahane.Lab.CustomerDecision'', N''Boyahane'', N''Lab denemesi için müşteri kararını girme, önceki reçeteyi kullanma'')
) v([Id], [Code], [Module], [Description])
WHERE NOT EXISTS (SELECT 1 FROM [Core].[Permissions] p WHERE p.[Id] = v.[Id] OR p.[Code] = v.[Code])');
SET IDENTITY_INSERT [Core].[Permissions] OFF;");

            migrationBuilder.Sql(@"INSERT INTO [Core].[RolePermissions] ([RoleId], [PermissionId])
SELECT v.[RoleId], v.[PermissionId] FROM (VALUES
(1, 1013),
(1, 1014),
(1, 1015)
) v([RoleId], [PermissionId])
WHERE EXISTS (SELECT 1 FROM [Core].[Roles] r WHERE r.[Id] = v.[RoleId])
  AND EXISTS (SELECT 1 FROM [Core].[Permissions] p WHERE p.[Id] = v.[PermissionId])
  AND NOT EXISTS (SELECT 1 FROM [Core].[RolePermissions] rp WHERE rp.[RoleId] = v.[RoleId] AND rp.[PermissionId] = v.[PermissionId]);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DELETE FROM [Core].[RolePermissions] WHERE [PermissionId] IN (1013, 1014, 1015);
DELETE FROM [Core].[Permissions] WHERE [Id] IN (1013, 1014, 1015);");

            migrationBuilder.DropTable(
                name: "LabTrialItems",
                schema: "Boyahane");

            migrationBuilder.DropTable(
                name: "DyeChemicals",
                schema: "Boyahane");

            migrationBuilder.DropTable(
                name: "LabTrials",
                schema: "Boyahane");

            migrationBuilder.DropTable(
                name: "LabRequests",
                schema: "Boyahane");
        }
    }
}
