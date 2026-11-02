using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Boyahane.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentsAndCustomerCodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DocumentLanguage",
                schema: "Boyahane",
                table: "Customers",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "TURKISH");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Customers_DocumentLanguage",
                schema: "Boyahane",
                table: "Customers",
                sql: "[DocumentLanguage] IN ('TURKISH', 'ENGLISH')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Customers_DocumentLanguage",
                schema: "Boyahane",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "DocumentLanguage",
                schema: "Boyahane",
                table: "Customers");
        }
    }
}
