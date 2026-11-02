using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Konfeksiyon.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCartonsAndDocumentLanguage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DocumentLanguage",
                schema: "Konfeksiyon",
                table: "Customers",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "TURKISH");

            migrationBuilder.CreateTable(
                name: "AssortmentTemplates",
                schema: "Konfeksiyon",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CustomerId = table.Column<int>(type: "int", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("PK_AssortmentTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssortmentTemplates_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AssortmentTemplateLines",
                schema: "Konfeksiyon",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssortmentTemplateId = table.Column<int>(type: "int", nullable: false),
                    SizeId = table.Column<int>(type: "int", nullable: false),
                    Ratio = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssortmentTemplateLines", x => x.Id);
                    table.CheckConstraint("CK_AssortmentTemplateLines_Ratio", "[Ratio] > 0");
                    table.ForeignKey(
                        name: "FK_AssortmentTemplateLines_AssortmentTemplates_AssortmentTemplateId",
                        column: x => x.AssortmentTemplateId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "AssortmentTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AssortmentTemplateLines_Sizes_SizeId",
                        column: x => x.SizeId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "Sizes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ShipmentCartons",
                schema: "Konfeksiyon",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ShipmentId = table.Column<int>(type: "int", nullable: false),
                    CartonNo = table.Column<int>(type: "int", nullable: false),
                    AssortmentTemplateId = table.Column<int>(type: "int", nullable: true),
                    NetKg = table.Column<decimal>(type: "decimal(10,3)", precision: 10, scale: 3, nullable: true),
                    GrossKg = table.Column<decimal>(type: "decimal(10,3)", precision: 10, scale: 3, nullable: true),
                    LoadedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LoadedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShipmentCartons", x => x.Id);
                    table.CheckConstraint("CK_ShipmentCartons_CartonNo", "[CartonNo] > 0");
                    table.CheckConstraint("CK_ShipmentCartons_Loaded", "([LoadedAt] IS NULL AND [LoadedBy] IS NULL) OR ([LoadedAt] IS NOT NULL AND [LoadedBy] IS NOT NULL)");
                    table.CheckConstraint("CK_ShipmentCartons_Weight", "([NetKg] IS NULL OR [NetKg] >= 0) AND ([GrossKg] IS NULL OR [GrossKg] >= 0)");
                    table.ForeignKey(
                        name: "FK_ShipmentCartons_AssortmentTemplates_AssortmentTemplateId",
                        column: x => x.AssortmentTemplateId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "AssortmentTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ShipmentCartons_Shipments_ShipmentId",
                        column: x => x.ShipmentId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "Shipments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ShipmentCartonLines",
                schema: "Konfeksiyon",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ShipmentCartonId = table.Column<int>(type: "int", nullable: false),
                    StockItemId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShipmentCartonLines", x => x.Id);
                    table.CheckConstraint("CK_ShipmentCartonLines_Quantity", "[Quantity] > 0");
                    table.ForeignKey(
                        name: "FK_ShipmentCartonLines_ShipmentCartons_ShipmentCartonId",
                        column: x => x.ShipmentCartonId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "ShipmentCartons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ShipmentCartonLines_StockItems_StockItemId",
                        column: x => x.StockItemId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "StockItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Customers_DocumentLanguage",
                schema: "Konfeksiyon",
                table: "Customers",
                sql: "[DocumentLanguage] IN ('TURKISH', 'ENGLISH')");

            migrationBuilder.CreateIndex(
                name: "IX_AssortmentTemplateLines_AssortmentTemplateId_SizeId",
                schema: "Konfeksiyon",
                table: "AssortmentTemplateLines",
                columns: new[] { "AssortmentTemplateId", "SizeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssortmentTemplateLines_SizeId",
                schema: "Konfeksiyon",
                table: "AssortmentTemplateLines",
                column: "SizeId");

            migrationBuilder.CreateIndex(
                name: "IX_AssortmentTemplates_Code",
                schema: "Konfeksiyon",
                table: "AssortmentTemplates",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_AssortmentTemplates_CustomerId",
                schema: "Konfeksiyon",
                table: "AssortmentTemplates",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentCartonLines_ShipmentCartonId_StockItemId",
                schema: "Konfeksiyon",
                table: "ShipmentCartonLines",
                columns: new[] { "ShipmentCartonId", "StockItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentCartonLines_StockItemId",
                schema: "Konfeksiyon",
                table: "ShipmentCartonLines",
                column: "StockItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentCartons_AssortmentTemplateId",
                schema: "Konfeksiyon",
                table: "ShipmentCartons",
                column: "AssortmentTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentCartons_ShipmentId_CartonNo",
                schema: "Konfeksiyon",
                table: "ShipmentCartons",
                columns: new[] { "ShipmentId", "CartonNo" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssortmentTemplateLines",
                schema: "Konfeksiyon");

            migrationBuilder.DropTable(
                name: "ShipmentCartonLines",
                schema: "Konfeksiyon");

            migrationBuilder.DropTable(
                name: "ShipmentCartons",
                schema: "Konfeksiyon");

            migrationBuilder.DropTable(
                name: "AssortmentTemplates",
                schema: "Konfeksiyon");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Customers_DocumentLanguage",
                schema: "Konfeksiyon",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "DocumentLanguage",
                schema: "Konfeksiyon",
                table: "Customers");
        }
    }
}
