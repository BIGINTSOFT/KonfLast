using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Dokuma.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentsAndCustomerCodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CustomerArticleCode",
                schema: "Dokuma",
                table: "OrderLines",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DocumentLanguage",
                schema: "Dokuma",
                table: "Customers",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "TURKISH");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Customers_DocumentLanguage",
                schema: "Dokuma",
                table: "Customers",
                sql: "[DocumentLanguage] IN ('TURKISH', 'ENGLISH')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Customers_DocumentLanguage",
                schema: "Dokuma",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "CustomerArticleCode",
                schema: "Dokuma",
                table: "OrderLines");

            migrationBuilder.DropColumn(
                name: "DocumentLanguage",
                schema: "Dokuma",
                table: "Customers");
        }
    }
}
