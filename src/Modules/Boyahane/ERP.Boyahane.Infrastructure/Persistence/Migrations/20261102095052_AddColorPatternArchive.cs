using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Boyahane.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddColorPatternArchive : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Operations_Behavior",
                schema: "Boyahane",
                table: "Operations");

            migrationBuilder.DropCheckConstraint(
                name: "CK_BatchSteps_ColorResult",
                schema: "Boyahane",
                table: "BatchSteps");

            migrationBuilder.DropCheckConstraint(
                name: "CK_BatchSteps_ReworkKind",
                schema: "Boyahane",
                table: "BatchSteps");

            migrationBuilder.AddColumn<int>(
                name: "ColorCardId",
                schema: "Boyahane",
                table: "OrderLines",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ColorwayId",
                schema: "Boyahane",
                table: "OrderLines",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PatternId",
                schema: "Boyahane",
                table: "OrderLines",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StrippingRecipeId",
                schema: "Boyahane",
                table: "BatchSteps",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ColorCards",
                schema: "Boyahane",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CustomerId = table.Column<int>(type: "int", nullable: true),
                    CustomerColorCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Pantone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    LabL = table.Column<decimal>(type: "decimal(7,3)", precision: 7, scale: 3, nullable: true),
                    LabA = table.Column<decimal>(type: "decimal(7,3)", precision: 7, scale: 3, nullable: true),
                    LabB = table.Column<decimal>(type: "decimal(7,3)", precision: 7, scale: 3, nullable: true),
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
                    table.PrimaryKey("PK_ColorCards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ColorCards_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "Boyahane",
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Patterns",
                schema: "Boyahane",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CustomerId = table.Column<int>(type: "int", nullable: true),
                    RepeatWidthCm = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: true),
                    RepeatHeightCm = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: true),
                    PrintType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ScreenCount = table.Column<int>(type: "int", nullable: true),
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
                    table.PrimaryKey("PK_Patterns", x => x.Id);
                    table.CheckConstraint("CK_Patterns_PrintType", "[PrintType] IN ('PIGMENT', 'REACTIVE', 'DISCHARGE', 'OTHER')");
                    table.CheckConstraint("CK_Patterns_ScreenCount", "[ScreenCount] IS NULL OR [ScreenCount] > 0");
                    table.ForeignKey(
                        name: "FK_Patterns_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "Boyahane",
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StrippingRecipes",
                schema: "Boyahane",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
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
                    table.PrimaryKey("PK_StrippingRecipes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ColorCardImages",
                schema: "Boyahane",
                columns: table => new
                {
                    ColorCardId = table.Column<int>(type: "int", nullable: false),
                    Content = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ColorCardImages", x => x.ColorCardId);
                    table.ForeignKey(
                        name: "FK_ColorCardImages_ColorCards_ColorCardId",
                        column: x => x.ColorCardId,
                        principalSchema: "Boyahane",
                        principalTable: "ColorCards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PatternColorways",
                schema: "Boyahane",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PatternId = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatternColorways", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PatternColorways_Patterns_PatternId",
                        column: x => x.PatternId,
                        principalSchema: "Boyahane",
                        principalTable: "Patterns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PatternImages",
                schema: "Boyahane",
                columns: table => new
                {
                    PatternId = table.Column<int>(type: "int", nullable: false),
                    Content = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatternImages", x => x.PatternId);
                    table.ForeignKey(
                        name: "FK_PatternImages_Patterns_PatternId",
                        column: x => x.PatternId,
                        principalSchema: "Boyahane",
                        principalTable: "Patterns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StrippingRecipeLines",
                schema: "Boyahane",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RecipeId = table.Column<int>(type: "int", nullable: false),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    DyeChemicalId = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(10,4)", precision: 10, scale: 4, nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    TemperatureCelsius = table.Column<int>(type: "int", nullable: true),
                    DurationMinutes = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StrippingRecipeLines", x => x.Id);
                    table.CheckConstraint("CK_StrippingRecipeLines_Amount", "[Amount] > 0");
                    table.CheckConstraint("CK_StrippingRecipeLines_Unit", "[Unit] IN ('PERCENT', 'GRAMS_PER_LITER')");
                    table.ForeignKey(
                        name: "FK_StrippingRecipeLines_DyeChemicals_DyeChemicalId",
                        column: x => x.DyeChemicalId,
                        principalSchema: "Boyahane",
                        principalTable: "DyeChemicals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StrippingRecipeLines_StrippingRecipes_RecipeId",
                        column: x => x.RecipeId,
                        principalSchema: "Boyahane",
                        principalTable: "StrippingRecipes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ColorwayColors",
                schema: "Boyahane",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ColorwayId = table.Column<int>(type: "int", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    ColorCardId = table.Column<int>(type: "int", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PasteKilogramsPerMeter = table.Column<decimal>(type: "decimal(10,4)", precision: 10, scale: 4, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ColorwayColors", x => x.Id);
                    table.CheckConstraint("CK_ColorwayColors_Paste", "[PasteKilogramsPerMeter] IS NULL OR [PasteKilogramsPerMeter] > 0");
                    table.CheckConstraint("CK_ColorwayColors_Position", "[Position] > 0");
                    table.ForeignKey(
                        name: "FK_ColorwayColors_ColorCards_ColorCardId",
                        column: x => x.ColorCardId,
                        principalSchema: "Boyahane",
                        principalTable: "ColorCards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ColorwayColors_PatternColorways_ColorwayId",
                        column: x => x.ColorwayId,
                        principalSchema: "Boyahane",
                        principalTable: "PatternColorways",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PrintRecipeLines",
                schema: "Boyahane",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ColorwayColorId = table.Column<int>(type: "int", nullable: false),
                    DyeChemicalId = table.Column<int>(type: "int", nullable: false),
                    GramsPerKilogram = table.Column<decimal>(type: "decimal(10,4)", precision: 10, scale: 4, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrintRecipeLines", x => x.Id);
                    table.CheckConstraint("CK_PrintRecipeLines_Amount", "[GramsPerKilogram] > 0");
                    table.ForeignKey(
                        name: "FK_PrintRecipeLines_ColorwayColors_ColorwayColorId",
                        column: x => x.ColorwayColorId,
                        principalSchema: "Boyahane",
                        principalTable: "ColorwayColors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PrintRecipeLines_DyeChemicals_DyeChemicalId",
                        column: x => x.DyeChemicalId,
                        principalSchema: "Boyahane",
                        principalTable: "DyeChemicals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrderLines_ColorCardId",
                schema: "Boyahane",
                table: "OrderLines",
                column: "ColorCardId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderLines_ColorwayId",
                schema: "Boyahane",
                table: "OrderLines",
                column: "ColorwayId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderLines_PatternId",
                schema: "Boyahane",
                table: "OrderLines",
                column: "PatternId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Operations_Behavior",
                schema: "Boyahane",
                table: "Operations",
                sql: "[Behavior] IN ('STANDARD', 'DYEING', 'COLOR_CHECK', 'FINAL_QUALITY', 'STRIPPING', 'PRINTING')");

            migrationBuilder.CreateIndex(
                name: "IX_BatchSteps_StrippingRecipeId",
                schema: "Boyahane",
                table: "BatchSteps",
                column: "StrippingRecipeId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_BatchSteps_ColorResult",
                schema: "Boyahane",
                table: "BatchSteps",
                sql: "[ColorResult] IN ('APPROVED', 'ADDITION', 'REDO', 'STRIPPING')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_BatchSteps_ReworkKind",
                schema: "Boyahane",
                table: "BatchSteps",
                sql: "[ReworkKind] IN ('APPROVED', 'ADDITION', 'REDO', 'STRIPPING')");

            migrationBuilder.CreateIndex(
                name: "IX_ColorCards_Code",
                schema: "Boyahane",
                table: "ColorCards",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ColorCards_CustomerId",
                schema: "Boyahane",
                table: "ColorCards",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_ColorwayColors_ColorCardId",
                schema: "Boyahane",
                table: "ColorwayColors",
                column: "ColorCardId");

            migrationBuilder.CreateIndex(
                name: "IX_ColorwayColors_ColorwayId_Position",
                schema: "Boyahane",
                table: "ColorwayColors",
                columns: new[] { "ColorwayId", "Position" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PatternColorways_PatternId_Code",
                schema: "Boyahane",
                table: "PatternColorways",
                columns: new[] { "PatternId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Patterns_Code",
                schema: "Boyahane",
                table: "Patterns",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Patterns_CustomerId",
                schema: "Boyahane",
                table: "Patterns",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_PrintRecipeLines_ColorwayColorId_DyeChemicalId",
                schema: "Boyahane",
                table: "PrintRecipeLines",
                columns: new[] { "ColorwayColorId", "DyeChemicalId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PrintRecipeLines_DyeChemicalId",
                schema: "Boyahane",
                table: "PrintRecipeLines",
                column: "DyeChemicalId");

            migrationBuilder.CreateIndex(
                name: "IX_StrippingRecipeLines_DyeChemicalId",
                schema: "Boyahane",
                table: "StrippingRecipeLines",
                column: "DyeChemicalId");

            migrationBuilder.CreateIndex(
                name: "IX_StrippingRecipeLines_RecipeId_Sequence",
                schema: "Boyahane",
                table: "StrippingRecipeLines",
                columns: new[] { "RecipeId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StrippingRecipes_Code",
                schema: "Boyahane",
                table: "StrippingRecipes",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.AddForeignKey(
                name: "FK_BatchSteps_StrippingRecipes_StrippingRecipeId",
                schema: "Boyahane",
                table: "BatchSteps",
                column: "StrippingRecipeId",
                principalSchema: "Boyahane",
                principalTable: "StrippingRecipes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderLines_ColorCards_ColorCardId",
                schema: "Boyahane",
                table: "OrderLines",
                column: "ColorCardId",
                principalSchema: "Boyahane",
                principalTable: "ColorCards",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderLines_PatternColorways_ColorwayId",
                schema: "Boyahane",
                table: "OrderLines",
                column: "ColorwayId",
                principalSchema: "Boyahane",
                principalTable: "PatternColorways",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderLines_Patterns_PatternId",
                schema: "Boyahane",
                table: "OrderLines",
                column: "PatternId",
                principalSchema: "Boyahane",
                principalTable: "Patterns",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BatchSteps_StrippingRecipes_StrippingRecipeId",
                schema: "Boyahane",
                table: "BatchSteps");

            migrationBuilder.DropForeignKey(
                name: "FK_OrderLines_ColorCards_ColorCardId",
                schema: "Boyahane",
                table: "OrderLines");

            migrationBuilder.DropForeignKey(
                name: "FK_OrderLines_PatternColorways_ColorwayId",
                schema: "Boyahane",
                table: "OrderLines");

            migrationBuilder.DropForeignKey(
                name: "FK_OrderLines_Patterns_PatternId",
                schema: "Boyahane",
                table: "OrderLines");

            migrationBuilder.DropTable(
                name: "ColorCardImages",
                schema: "Boyahane");

            migrationBuilder.DropTable(
                name: "PatternImages",
                schema: "Boyahane");

            migrationBuilder.DropTable(
                name: "PrintRecipeLines",
                schema: "Boyahane");

            migrationBuilder.DropTable(
                name: "StrippingRecipeLines",
                schema: "Boyahane");

            migrationBuilder.DropTable(
                name: "ColorwayColors",
                schema: "Boyahane");

            migrationBuilder.DropTable(
                name: "StrippingRecipes",
                schema: "Boyahane");

            migrationBuilder.DropTable(
                name: "ColorCards",
                schema: "Boyahane");

            migrationBuilder.DropTable(
                name: "PatternColorways",
                schema: "Boyahane");

            migrationBuilder.DropTable(
                name: "Patterns",
                schema: "Boyahane");

            migrationBuilder.DropIndex(
                name: "IX_OrderLines_ColorCardId",
                schema: "Boyahane",
                table: "OrderLines");

            migrationBuilder.DropIndex(
                name: "IX_OrderLines_ColorwayId",
                schema: "Boyahane",
                table: "OrderLines");

            migrationBuilder.DropIndex(
                name: "IX_OrderLines_PatternId",
                schema: "Boyahane",
                table: "OrderLines");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Operations_Behavior",
                schema: "Boyahane",
                table: "Operations");

            migrationBuilder.DropIndex(
                name: "IX_BatchSteps_StrippingRecipeId",
                schema: "Boyahane",
                table: "BatchSteps");

            migrationBuilder.DropCheckConstraint(
                name: "CK_BatchSteps_ColorResult",
                schema: "Boyahane",
                table: "BatchSteps");

            migrationBuilder.DropCheckConstraint(
                name: "CK_BatchSteps_ReworkKind",
                schema: "Boyahane",
                table: "BatchSteps");

            migrationBuilder.DropColumn(
                name: "ColorCardId",
                schema: "Boyahane",
                table: "OrderLines");

            migrationBuilder.DropColumn(
                name: "ColorwayId",
                schema: "Boyahane",
                table: "OrderLines");

            migrationBuilder.DropColumn(
                name: "PatternId",
                schema: "Boyahane",
                table: "OrderLines");

            migrationBuilder.DropColumn(
                name: "StrippingRecipeId",
                schema: "Boyahane",
                table: "BatchSteps");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Operations_Behavior",
                schema: "Boyahane",
                table: "Operations",
                sql: "[Behavior] IN ('STANDARD', 'DYEING', 'COLOR_CHECK', 'FINAL_QUALITY')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_BatchSteps_ColorResult",
                schema: "Boyahane",
                table: "BatchSteps",
                sql: "[ColorResult] IN ('APPROVED', 'ADDITION', 'REDO')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_BatchSteps_ReworkKind",
                schema: "Boyahane",
                table: "BatchSteps",
                sql: "[ReworkKind] IN ('APPROVED', 'ADDITION', 'REDO')");
        }
    }
}
