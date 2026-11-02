using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Dokuma.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSizingMaterialsPurchasing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PurchaseOrderLineId",
                schema: "Dokuma",
                table: "YarnLots",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SizingPickUpPercent",
                schema: "Dokuma",
                table: "Beams",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SizingRecipeId",
                schema: "Dokuma",
                table: "Beams",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SizingViscositySeconds",
                schema: "Dokuma",
                table: "Beams",
                type: "decimal(6,1)",
                precision: 6,
                scale: 1,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Materials",
                schema: "Dokuma",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    MinimumStock = table.Column<decimal>(type: "decimal(14,3)", precision: 14, scale: 3, nullable: true),
                    PreferredSupplierId = table.Column<int>(type: "int", nullable: true),
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
                    table.PrimaryKey("PK_Materials", x => x.Id);
                    table.CheckConstraint("CK_Materials_MinimumStock", "[MinimumStock] IS NULL OR [MinimumStock] >= 0");
                    table.CheckConstraint("CK_Materials_Type", "[Type] IN ('SIZING_CHEMICAL', 'SPARE_PART', 'CONSUMABLE', 'OTHER')");
                    table.CheckConstraint("CK_Materials_Unit", "[Unit] IN ('KILOGRAM', 'PIECE', 'LITER', 'METER')");
                    table.ForeignKey(
                        name: "FK_Materials_Suppliers_PreferredSupplierId",
                        column: x => x.PreferredSupplierId,
                        principalSchema: "Dokuma",
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseOrders",
                schema: "Dokuma",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderNo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    SupplierId = table.Column<int>(type: "int", nullable: false),
                    OrderDate = table.Column<DateOnly>(type: "date", nullable: false),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Currency = table.Column<string>(type: "char(3)", maxLength: 200, nullable: false, defaultValue: "TRY"),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseOrders", x => x.Id);
                    table.CheckConstraint("CK_PurchaseOrders_Status", "[Status] IN ('DRAFT', 'APPROVED', 'PARTIALLY_RECEIVED', 'RECEIVED', 'CLOSED', 'CANCELLED')");
                    table.ForeignKey(
                        name: "FK_PurchaseOrders_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalSchema: "Dokuma",
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SizingRecipes",
                schema: "Dokuma",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ArticleId = table.Column<int>(type: "int", nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    ConcentrationPercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    BathTemperatureC = table.Column<decimal>(type: "decimal(5,1)", precision: 5, scale: 1, nullable: true),
                    ViscositySeconds = table.Column<decimal>(type: "decimal(6,1)", precision: 6, scale: 1, nullable: true),
                    MachineSpeedMetersPerMinute = table.Column<decimal>(type: "decimal(6,1)", precision: 6, scale: 1, nullable: true),
                    TargetPickUpPercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
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
                    table.PrimaryKey("PK_SizingRecipes", x => x.Id);
                    table.CheckConstraint("CK_SizingRecipes_ConcentrationPercent", "[ConcentrationPercent] IS NULL OR [ConcentrationPercent] BETWEEN 0 AND 100");
                    table.CheckConstraint("CK_SizingRecipes_MachineSpeed", "[MachineSpeedMetersPerMinute] IS NULL OR [MachineSpeedMetersPerMinute] > 0");
                    table.CheckConstraint("CK_SizingRecipes_TargetPickUpPercent", "[TargetPickUpPercent] IS NULL OR [TargetPickUpPercent] BETWEEN 0 AND 500");
                    table.CheckConstraint("CK_SizingRecipes_ViscositySeconds", "[ViscositySeconds] IS NULL OR [ViscositySeconds] > 0");
                    table.ForeignKey(
                        name: "FK_SizingRecipes_Articles_ArticleId",
                        column: x => x.ArticleId,
                        principalSchema: "Dokuma",
                        principalTable: "Articles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseOrderLines",
                schema: "Dokuma",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    YarnId = table.Column<int>(type: "int", nullable: true),
                    MaterialId = table.Column<int>(type: "int", nullable: true),
                    Quantity = table.Column<decimal>(type: "decimal(14,3)", precision: 14, scale: 3, nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ReceivedQuantity = table.Column<decimal>(type: "decimal(14,3)", precision: 14, scale: 3, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseOrderLines", x => x.Id);
                    table.CheckConstraint("CK_PurchaseOrderLines_Item", "([YarnId] IS NOT NULL AND [MaterialId] IS NULL AND [Unit] = 'KILOGRAM') OR ([YarnId] IS NULL AND [MaterialId] IS NOT NULL)");
                    table.CheckConstraint("CK_PurchaseOrderLines_Quantity", "[Quantity] > 0");
                    table.CheckConstraint("CK_PurchaseOrderLines_ReceivedQuantity", "[ReceivedQuantity] >= 0");
                    table.CheckConstraint("CK_PurchaseOrderLines_Unit", "[Unit] IN ('KILOGRAM', 'PIECE', 'LITER', 'METER')");
                    table.CheckConstraint("CK_PurchaseOrderLines_UnitPrice", "[UnitPrice] IS NULL OR [UnitPrice] >= 0");
                    table.ForeignKey(
                        name: "FK_PurchaseOrderLines_Materials_MaterialId",
                        column: x => x.MaterialId,
                        principalSchema: "Dokuma",
                        principalTable: "Materials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseOrderLines_PurchaseOrders_OrderId",
                        column: x => x.OrderId,
                        principalSchema: "Dokuma",
                        principalTable: "PurchaseOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseOrderLines_Yarns_YarnId",
                        column: x => x.YarnId,
                        principalSchema: "Dokuma",
                        principalTable: "Yarns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SizingRecipeLines",
                schema: "Dokuma",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RecipeId = table.Column<int>(type: "int", nullable: false),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    MaterialId = table.Column<int>(type: "int", nullable: false),
                    BathPercent = table.Column<decimal>(type: "decimal(6,3)", precision: 6, scale: 3, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SizingRecipeLines", x => x.Id);
                    table.CheckConstraint("CK_SizingRecipeLines_BathPercent", "[BathPercent] > 0 AND [BathPercent] <= 100");
                    table.CheckConstraint("CK_SizingRecipeLines_Sequence", "[Sequence] >= 1");
                    table.ForeignKey(
                        name: "FK_SizingRecipeLines_Materials_MaterialId",
                        column: x => x.MaterialId,
                        principalSchema: "Dokuma",
                        principalTable: "Materials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SizingRecipeLines_SizingRecipes_RecipeId",
                        column: x => x.RecipeId,
                        principalSchema: "Dokuma",
                        principalTable: "SizingRecipes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MaterialLots",
                schema: "Dokuma",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LotNo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    MaterialId = table.Column<int>(type: "int", nullable: false),
                    ReceiptDate = table.Column<DateOnly>(type: "date", nullable: false),
                    SupplierId = table.Column<int>(type: "int", nullable: true),
                    SupplierLotNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DespatchNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Quantity = table.Column<decimal>(type: "decimal(14,3)", precision: 14, scale: 3, nullable: false),
                    RemainingQuantity = table.Column<decimal>(type: "decimal(14,3)", precision: 14, scale: 3, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    Currency = table.Column<string>(type: "char(3)", maxLength: 200, nullable: false, defaultValue: "TRY"),
                    PurchaseOrderLineId = table.Column<int>(type: "int", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaterialLots", x => x.Id);
                    table.CheckConstraint("CK_MaterialLots_Quantity", "[Quantity] > 0");
                    table.CheckConstraint("CK_MaterialLots_RemainingQuantity", "[RemainingQuantity] >= 0");
                    table.CheckConstraint("CK_MaterialLots_UnitPrice", "[UnitPrice] IS NULL OR [UnitPrice] >= 0");
                    table.ForeignKey(
                        name: "FK_MaterialLots_Materials_MaterialId",
                        column: x => x.MaterialId,
                        principalSchema: "Dokuma",
                        principalTable: "Materials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MaterialLots_PurchaseOrderLines_PurchaseOrderLineId",
                        column: x => x.PurchaseOrderLineId,
                        principalSchema: "Dokuma",
                        principalTable: "PurchaseOrderLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MaterialLots_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalSchema: "Dokuma",
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MaterialMovements",
                schema: "Dokuma",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaterialId = table.Column<int>(type: "int", nullable: false),
                    LotId = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    MovementDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(14,3)", precision: 14, scale: 3, nullable: false),
                    LoomId = table.Column<int>(type: "int", nullable: true),
                    Department = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    BeamId = table.Column<int>(type: "int", nullable: true),
                    Reference = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaterialMovements", x => x.Id);
                    table.CheckConstraint("CK_MaterialMovements_Quantity", "[Quantity] <> 0");
                    table.CheckConstraint("CK_MaterialMovements_Type", "[Type] IN ('RECEIPT', 'ISSUE', 'SIZING_CONSUMPTION', 'ADJUSTMENT', 'COUNT')");
                    table.ForeignKey(
                        name: "FK_MaterialMovements_Beams_BeamId",
                        column: x => x.BeamId,
                        principalSchema: "Dokuma",
                        principalTable: "Beams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MaterialMovements_Looms_LoomId",
                        column: x => x.LoomId,
                        principalSchema: "Dokuma",
                        principalTable: "Looms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MaterialMovements_MaterialLots_LotId",
                        column: x => x.LotId,
                        principalSchema: "Dokuma",
                        principalTable: "MaterialLots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MaterialMovements_Materials_MaterialId",
                        column: x => x.MaterialId,
                        principalSchema: "Dokuma",
                        principalTable: "Materials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_YarnLots_PurchaseOrderLineId",
                schema: "Dokuma",
                table: "YarnLots",
                column: "PurchaseOrderLineId");

            migrationBuilder.CreateIndex(
                name: "IX_Beams_SizingRecipeId",
                schema: "Dokuma",
                table: "Beams",
                column: "SizingRecipeId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Beams_SizingPickUpPercent",
                schema: "Dokuma",
                table: "Beams",
                sql: "[SizingPickUpPercent] IS NULL OR [SizingPickUpPercent] BETWEEN 0 AND 500");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Beams_SizingViscositySeconds",
                schema: "Dokuma",
                table: "Beams",
                sql: "[SizingViscositySeconds] IS NULL OR [SizingViscositySeconds] > 0");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialLots_LotNo",
                schema: "Dokuma",
                table: "MaterialLots",
                column: "LotNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MaterialLots_MaterialId_ReceiptDate",
                schema: "Dokuma",
                table: "MaterialLots",
                columns: new[] { "MaterialId", "ReceiptDate" });

            migrationBuilder.CreateIndex(
                name: "IX_MaterialLots_PurchaseOrderLineId",
                schema: "Dokuma",
                table: "MaterialLots",
                column: "PurchaseOrderLineId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialLots_SupplierId",
                schema: "Dokuma",
                table: "MaterialLots",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialMovements_BeamId",
                schema: "Dokuma",
                table: "MaterialMovements",
                column: "BeamId",
                filter: "[BeamId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialMovements_LoomId",
                schema: "Dokuma",
                table: "MaterialMovements",
                column: "LoomId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialMovements_LotId",
                schema: "Dokuma",
                table: "MaterialMovements",
                column: "LotId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialMovements_MaterialId_MovementDate",
                schema: "Dokuma",
                table: "MaterialMovements",
                columns: new[] { "MaterialId", "MovementDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Materials_Code",
                schema: "Dokuma",
                table: "Materials",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Materials_PreferredSupplierId",
                schema: "Dokuma",
                table: "Materials",
                column: "PreferredSupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderLines_MaterialId",
                schema: "Dokuma",
                table: "PurchaseOrderLines",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderLines_OrderId_Sequence",
                schema: "Dokuma",
                table: "PurchaseOrderLines",
                columns: new[] { "OrderId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderLines_YarnId",
                schema: "Dokuma",
                table: "PurchaseOrderLines",
                column: "YarnId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_OrderDate",
                schema: "Dokuma",
                table: "PurchaseOrders",
                column: "OrderDate");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_OrderNo",
                schema: "Dokuma",
                table: "PurchaseOrders",
                column: "OrderNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_Status",
                schema: "Dokuma",
                table: "PurchaseOrders",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_SupplierId",
                schema: "Dokuma",
                table: "PurchaseOrders",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_SizingRecipeLines_MaterialId",
                schema: "Dokuma",
                table: "SizingRecipeLines",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_SizingRecipeLines_RecipeId_Sequence",
                schema: "Dokuma",
                table: "SizingRecipeLines",
                columns: new[] { "RecipeId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SizingRecipes_Code",
                schema: "Dokuma",
                table: "SizingRecipes",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UX_SizingRecipes_ArticleDefault",
                schema: "Dokuma",
                table: "SizingRecipes",
                column: "ArticleId",
                unique: true,
                filter: "[IsDefault] = 1 AND [IsDeleted] = 0");

            migrationBuilder.AddForeignKey(
                name: "FK_Beams_SizingRecipes_SizingRecipeId",
                schema: "Dokuma",
                table: "Beams",
                column: "SizingRecipeId",
                principalSchema: "Dokuma",
                principalTable: "SizingRecipes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_YarnLots_PurchaseOrderLines_PurchaseOrderLineId",
                schema: "Dokuma",
                table: "YarnLots",
                column: "PurchaseOrderLineId",
                principalSchema: "Dokuma",
                principalTable: "PurchaseOrderLines",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // Yeni yetkiler; varsayılan olarak yalnızca Sistem Yöneticisine verilir, diğer roller rol ekranından yetkilendirilir.
            migrationBuilder.Sql(@"SET IDENTITY_INSERT [Core].[Permissions] ON;
EXEC(N'INSERT INTO [Core].[Permissions] ([Id], [Code], [Module], [Description])
SELECT v.[Id], v.[Code], v.[Module], v.[Description] FROM (VALUES
(2032, N''Dokuma.PurchaseOrder.View'', N''Dokuma'', N''Satın alma siparişlerini ve tedarik önerilerini görüntüleme''),
(2033, N''Dokuma.PurchaseOrder.Manage'', N''Dokuma'', N''Satın alma siparişi hazırlama, onaylama, teslim alma ve kapatma''),
(2034, N''Dokuma.Stock.View'', N''Dokuma'', N''Malzeme stoğunu, lotlarını ve hareketlerini görüntüleme''),
(2035, N''Dokuma.Stock.Manage'', N''Dokuma'', N''Malzeme girişi, tezgâha / bölüme çıkış, sayım ve düzeltme'')
) v([Id], [Code], [Module], [Description])
WHERE NOT EXISTS (SELECT 1 FROM [Core].[Permissions] p WHERE p.[Id] = v.[Id] OR p.[Code] = v.[Code])');
SET IDENTITY_INSERT [Core].[Permissions] OFF;");

            migrationBuilder.Sql(@"INSERT INTO [Core].[RolePermissions] ([RoleId], [PermissionId])
SELECT v.[RoleId], v.[PermissionId] FROM (VALUES
(1, 2032), (1, 2033), (1, 2034), (1, 2035)
) v([RoleId], [PermissionId])
WHERE EXISTS (SELECT 1 FROM [Core].[Roles] r WHERE r.[Id] = v.[RoleId])
  AND EXISTS (SELECT 1 FROM [Core].[Permissions] p WHERE p.[Id] = v.[PermissionId])
  AND NOT EXISTS (SELECT 1 FROM [Core].[RolePermissions] rp WHERE rp.[RoleId] = v.[RoleId] AND rp.[PermissionId] = v.[PermissionId]);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DELETE FROM [Core].[RolePermissions] WHERE [PermissionId] IN (2032, 2033, 2034, 2035);
DELETE FROM [Core].[Permissions] WHERE [Id] IN (2032, 2033, 2034, 2035);");

            migrationBuilder.DropForeignKey(
                name: "FK_Beams_SizingRecipes_SizingRecipeId",
                schema: "Dokuma",
                table: "Beams");

            migrationBuilder.DropForeignKey(
                name: "FK_YarnLots_PurchaseOrderLines_PurchaseOrderLineId",
                schema: "Dokuma",
                table: "YarnLots");

            migrationBuilder.DropTable(
                name: "MaterialMovements",
                schema: "Dokuma");

            migrationBuilder.DropTable(
                name: "SizingRecipeLines",
                schema: "Dokuma");

            migrationBuilder.DropTable(
                name: "MaterialLots",
                schema: "Dokuma");

            migrationBuilder.DropTable(
                name: "SizingRecipes",
                schema: "Dokuma");

            migrationBuilder.DropTable(
                name: "PurchaseOrderLines",
                schema: "Dokuma");

            migrationBuilder.DropTable(
                name: "Materials",
                schema: "Dokuma");

            migrationBuilder.DropTable(
                name: "PurchaseOrders",
                schema: "Dokuma");

            migrationBuilder.DropIndex(
                name: "IX_YarnLots_PurchaseOrderLineId",
                schema: "Dokuma",
                table: "YarnLots");

            migrationBuilder.DropIndex(
                name: "IX_Beams_SizingRecipeId",
                schema: "Dokuma",
                table: "Beams");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Beams_SizingPickUpPercent",
                schema: "Dokuma",
                table: "Beams");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Beams_SizingViscositySeconds",
                schema: "Dokuma",
                table: "Beams");

            migrationBuilder.DropColumn(
                name: "PurchaseOrderLineId",
                schema: "Dokuma",
                table: "YarnLots");

            migrationBuilder.DropColumn(
                name: "SizingPickUpPercent",
                schema: "Dokuma",
                table: "Beams");

            migrationBuilder.DropColumn(
                name: "SizingRecipeId",
                schema: "Dokuma",
                table: "Beams");

            migrationBuilder.DropColumn(
                name: "SizingViscositySeconds",
                schema: "Dokuma",
                table: "Beams");
        }
    }
}
