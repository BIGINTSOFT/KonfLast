using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Konfeksiyon.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddShipmentPickingAndReservations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Shipments_Status",
                schema: "Konfeksiyon",
                table: "Shipments");

            migrationBuilder.DropIndex(
                name: "IX_SalesOrderReservations_SalesOrderLineId_WarehouseId",
                schema: "Konfeksiyon",
                table: "SalesOrderReservations");

            migrationBuilder.AddColumn<Guid>(
                name: "DespatchUuid",
                schema: "Konfeksiyon",
                table: "Shipments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DriverIdentityNo",
                schema: "Konfeksiyon",
                table: "Shipments",
                type: "nvarchar(11)",
                maxLength: 11,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DriverName",
                schema: "Konfeksiyon",
                table: "Shipments",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PickedAt",
                schema: "Konfeksiyon",
                table: "Shipments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PickedBy",
                schema: "Konfeksiyon",
                table: "Shipments",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PickingBy",
                schema: "Konfeksiyon",
                table: "Shipments",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PickingStartedAt",
                schema: "Konfeksiyon",
                table: "Shipments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VehiclePlate",
                schema: "Konfeksiyon",
                table: "Shipments",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CheckedQuantity",
                schema: "Konfeksiyon",
                table: "ShipmentItems",
                type: "decimal(18,3)",
                precision: 18,
                scale: 3,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PickedQuantity",
                schema: "Konfeksiyon",
                table: "ShipmentItems",
                type: "decimal(18,3)",
                precision: 18,
                scale: 3,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "RequestedQuantity",
                schema: "Konfeksiyon",
                table: "ShipmentItems",
                type: "decimal(18,3)",
                precision: 18,
                scale: 3,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "FromProduction",
                schema: "Konfeksiyon",
                table: "SalesOrderReservations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "ShipmentPicks",
                schema: "Konfeksiyon",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ShipmentId = table.Column<int>(type: "int", nullable: false),
                    ShipmentItemId = table.Column<int>(type: "int", nullable: false),
                    LocationId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    PickedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PickedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShipmentPicks", x => x.Id);
                    table.CheckConstraint("CK_ShipmentPicks_Quantity", "[Quantity] > 0");
                    table.ForeignKey(
                        name: "FK_ShipmentPicks_ShipmentItems_ShipmentItemId",
                        column: x => x.ShipmentItemId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "ShipmentItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ShipmentPicks_Shipments_ShipmentId",
                        column: x => x.ShipmentId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "Shipments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ShipmentPicks_WarehouseLocations_LocationId",
                        column: x => x.LocationId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "WarehouseLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Shipments_Status",
                schema: "Konfeksiyon",
                table: "Shipments",
                sql: "[Status] IN ('DRAFT', 'PICKING', 'PICKED', 'SHIPPED', 'CANCELLED')");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrderReservations_SalesOrderLineId_WarehouseId_FromProduction",
                schema: "Konfeksiyon",
                table: "SalesOrderReservations",
                columns: new[] { "SalesOrderLineId", "WarehouseId", "FromProduction" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentPicks_LocationId",
                schema: "Konfeksiyon",
                table: "ShipmentPicks",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentPicks_RequestId",
                schema: "Konfeksiyon",
                table: "ShipmentPicks",
                column: "RequestId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentPicks_ShipmentId",
                schema: "Konfeksiyon",
                table: "ShipmentPicks",
                column: "ShipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentPicks_ShipmentItemId",
                schema: "Konfeksiyon",
                table: "ShipmentPicks",
                column: "ShipmentItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ShipmentPicks",
                schema: "Konfeksiyon");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Shipments_Status",
                schema: "Konfeksiyon",
                table: "Shipments");

            migrationBuilder.DropIndex(
                name: "IX_SalesOrderReservations_SalesOrderLineId_WarehouseId_FromProduction",
                schema: "Konfeksiyon",
                table: "SalesOrderReservations");

            migrationBuilder.DropColumn(
                name: "DespatchUuid",
                schema: "Konfeksiyon",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "DriverIdentityNo",
                schema: "Konfeksiyon",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "DriverName",
                schema: "Konfeksiyon",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "PickedAt",
                schema: "Konfeksiyon",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "PickedBy",
                schema: "Konfeksiyon",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "PickingBy",
                schema: "Konfeksiyon",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "PickingStartedAt",
                schema: "Konfeksiyon",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "VehiclePlate",
                schema: "Konfeksiyon",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "CheckedQuantity",
                schema: "Konfeksiyon",
                table: "ShipmentItems");

            migrationBuilder.DropColumn(
                name: "PickedQuantity",
                schema: "Konfeksiyon",
                table: "ShipmentItems");

            migrationBuilder.DropColumn(
                name: "RequestedQuantity",
                schema: "Konfeksiyon",
                table: "ShipmentItems");

            migrationBuilder.DropColumn(
                name: "FromProduction",
                schema: "Konfeksiyon",
                table: "SalesOrderReservations");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Shipments_Status",
                schema: "Konfeksiyon",
                table: "Shipments",
                sql: "[Status] IN ('DRAFT', 'SHIPPED', 'CANCELLED')");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrderReservations_SalesOrderLineId_WarehouseId",
                schema: "Konfeksiyon",
                table: "SalesOrderReservations",
                columns: new[] { "SalesOrderLineId", "WarehouseId" },
                unique: true);
        }
    }
}
