using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Dokuma.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddArticleWaste : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "WarpWastePercent",
                schema: "Dokuma",
                table: "Articles",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "WeftWastePercent",
                schema: "Dokuma",
                table: "Articles",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Articles_WarpWastePercent",
                schema: "Dokuma",
                table: "Articles",
                sql: "[WarpWastePercent] IS NULL OR [WarpWastePercent] BETWEEN 0 AND 100");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Articles_WeftWastePercent",
                schema: "Dokuma",
                table: "Articles",
                sql: "[WeftWastePercent] IS NULL OR [WeftWastePercent] BETWEEN 0 AND 100");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Articles_WarpWastePercent",
                schema: "Dokuma",
                table: "Articles");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Articles_WeftWastePercent",
                schema: "Dokuma",
                table: "Articles");

            migrationBuilder.DropColumn(
                name: "WarpWastePercent",
                schema: "Dokuma",
                table: "Articles");

            migrationBuilder.DropColumn(
                name: "WeftWastePercent",
                schema: "Dokuma",
                table: "Articles");
        }
    }
}
