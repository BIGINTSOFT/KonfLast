using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Boyahane.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderClose : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Orders_Status",
                schema: "Boyahane",
                table: "Orders");

            migrationBuilder.AddColumn<string>(
                name: "CloseNote",
                schema: "Boyahane",
                table: "Orders",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ClosedAt",
                schema: "Boyahane",
                table: "Orders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClosedBy",
                schema: "Boyahane",
                table: "Orders",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Orders_Status",
                schema: "Boyahane",
                table: "Orders",
                sql: "[Status] IN ('DRAFT', 'CONFIRMED', 'CANCELLED', 'CLOSED')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Orders_Status",
                schema: "Boyahane",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CloseNote",
                schema: "Boyahane",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ClosedAt",
                schema: "Boyahane",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ClosedBy",
                schema: "Boyahane",
                table: "Orders");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Orders_Status",
                schema: "Boyahane",
                table: "Orders",
                sql: "[Status] IN ('DRAFT', 'CONFIRMED', 'CANCELLED')");
        }
    }
}
