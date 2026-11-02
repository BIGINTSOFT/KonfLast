using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Dokuma.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddYarnTakeBacks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "YarnTakeBacks",
                schema: "Dokuma",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WarpOrderId = table.Column<int>(type: "int", nullable: true),
                    BeamId = table.Column<int>(type: "int", nullable: true),
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
                    table.CheckConstraint("CK_YarnTakeBacks_Source", "([WarpOrderId] IS NULL AND [BeamId] IS NOT NULL) OR ([WarpOrderId] IS NOT NULL AND [BeamId] IS NULL)");
                    table.ForeignKey(
                        name: "FK_YarnTakeBacks_Beams_BeamId",
                        column: x => x.BeamId,
                        principalSchema: "Dokuma",
                        principalTable: "Beams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_YarnTakeBacks_WarpOrders_WarpOrderId",
                        column: x => x.WarpOrderId,
                        principalSchema: "Dokuma",
                        principalTable: "WarpOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_YarnTakeBacks_YarnLots_YarnLotId",
                        column: x => x.YarnLotId,
                        principalSchema: "Dokuma",
                        principalTable: "YarnLots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_YarnTakeBacks_BeamId",
                schema: "Dokuma",
                table: "YarnTakeBacks",
                column: "BeamId");

            migrationBuilder.CreateIndex(
                name: "IX_YarnTakeBacks_WarpOrderId",
                schema: "Dokuma",
                table: "YarnTakeBacks",
                column: "WarpOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_YarnTakeBacks_YarnLotId",
                schema: "Dokuma",
                table: "YarnTakeBacks",
                column: "YarnLotId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "YarnTakeBacks",
                schema: "Dokuma");
        }
    }
}
