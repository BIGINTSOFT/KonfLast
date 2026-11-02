using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ERP.Konfeksiyon.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialKonfeksiyon : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Konfeksiyon");

            migrationBuilder.CreateTable(
                name: "ApprovalFlows",
                schema: "Konfeksiyon",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DocumentType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalFlows", x => x.Id);
                    table.CheckConstraint("CK_ApprovalFlows_DocumentType", "[DocumentType] IN ('SALES_ORDER')");
                });

            migrationBuilder.CreateTable(
                name: "BreakGroups",
                schema: "Konfeksiyon",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
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
                    table.PrimaryKey("PK_BreakGroups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Colors",
                schema: "Konfeksiyon",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ColorCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    HexCode = table.Column<string>(type: "varchar(7)", unicode: false, maxLength: 7, nullable: true),
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
                    table.PrimaryKey("PK_Colors", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Customers",
                schema: "Konfeksiyon",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PartyType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TaxNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    TaxOffice = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    MersisNo = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                    TradeRegistryNo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    IsEInvoiceUser = table.Column<bool>(type: "bit", nullable: false),
                    EInvoiceAlias = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EInvoiceProfile = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    IsEDespatchUser = table.Column<bool>(type: "bit", nullable: false),
                    EDespatchAlias = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EArchiveDelivery = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    EArchiveEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Currency = table.Column<string>(type: "char(3)", maxLength: 200, nullable: false, defaultValue: "TRY"),
                    RequireUniqueCustomerOrderNo = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_Customers", x => x.Id);
                    table.CheckConstraint("CK_Customers_EArchiveDelivery", "[EArchiveDelivery] IN ('ELECTRONIC', 'PAPER')");
                    table.CheckConstraint("CK_Customers_EInvoiceProfile", "[EInvoiceProfile] IN ('BASIC', 'COMMERCIAL')");
                    table.CheckConstraint("CK_Customers_PartyType", "[PartyType] IN ('COMPANY', 'INDIVIDUAL')");
                });

            migrationBuilder.CreateTable(
                name: "DefectTypes",
                schema: "Konfeksiyon",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
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
                    table.PrimaryKey("PK_DefectTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Factories",
                schema: "Konfeksiyon",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    AddressLine = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    District = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PostalCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    CountryCode = table.Column<string>(type: "char(2)", maxLength: 200, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ManagerName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SgkWorkplaceNo = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    IndoorArea = table.Column<int>(type: "int", nullable: true),
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
                    table.PrimaryKey("PK_Factories", x => x.Id);
                    table.CheckConstraint("CK_Factories_CountryCode", "LEN([CountryCode]) = 2");
                    table.CheckConstraint("CK_Factories_IndoorArea", "[IndoorArea] IS NULL OR [IndoorArea] > 0");
                });

            migrationBuilder.CreateTable(
                name: "MachineTypes",
                schema: "Konfeksiyon",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
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
                    table.PrimaryKey("PK_MachineTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PayrollSettings",
                schema: "Konfeksiyon",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    MinuteRate = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Currency = table.Column<string>(type: "varchar(3)", unicode: false, maxLength: 3, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollSettings", x => x.Id);
                    table.CheckConstraint("CK_PayrollSettings_MinuteRate", "[MinuteRate] >= 0");
                });

            migrationBuilder.CreateTable(
                name: "ProductGroups",
                schema: "Konfeksiyon",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
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
                    table.PrimaryKey("PK_ProductGroups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RfidCards",
                schema: "Konfeksiyon",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Uid = table.Column<string>(type: "varchar(47)", unicode: false, maxLength: 47, nullable: false),
                    CardType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Label = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
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
                    table.PrimaryKey("PK_RfidCards", x => x.Id);
                    table.CheckConstraint("CK_RfidCards_CardType", "[CardType] IN ('BUNDLE', 'OPERATOR')");
                });

            migrationBuilder.CreateTable(
                name: "RfidReadLogs",
                schema: "Konfeksiyon",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReadId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TabletId = table.Column<int>(type: "int", nullable: false),
                    Uid = table.Column<string>(type: "varchar(47)", unicode: false, maxLength: 47, nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ResultCode = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    OperatorId = table.Column<int>(type: "int", nullable: true),
                    BundleId = table.Column<int>(type: "int", nullable: true),
                    ReadAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RfidReadLogs", x => x.Id);
                    table.CheckConstraint("CK_RfidReadLogs_Kind", "[Kind] IN ('OPERATOR', 'BUNDLE')");
                });

            migrationBuilder.CreateTable(
                name: "Shifts",
                schema: "Konfeksiyon",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time(0)", nullable: false),
                    EndTime = table.Column<TimeOnly>(type: "time(0)", nullable: false),
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
                    table.PrimaryKey("PK_Shifts", x => x.Id);
                    table.CheckConstraint("CK_Shifts_Times", "[StartTime] <> [EndTime]");
                });

            migrationBuilder.CreateTable(
                name: "Sizes",
                schema: "Konfeksiyon",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SizeCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_Sizes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StopReasons",
                schema: "Konfeksiyon",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_StopReasons", x => x.Id);
                    table.CheckConstraint("CK_StopReasons_Category", "[Category] IN ('PAUSE', 'BREAKDOWN')");
                });

            migrationBuilder.CreateTable(
                name: "Suppliers",
                schema: "Konfeksiyon",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SupplierCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TaxNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("PK_Suppliers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Warehouses",
                schema: "Konfeksiyon",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
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
                    table.PrimaryKey("PK_Warehouses", x => x.Id);
                    table.CheckConstraint("CK_Warehouses_Type", "[Type] IN ('RAW_MATERIAL', 'FINISHED_GOODS', 'GENERAL')");
                });

            migrationBuilder.CreateTable(
                name: "ApprovalFlowSteps",
                schema: "Konfeksiyon",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ApprovalFlowId = table.Column<int>(type: "int", nullable: false),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ApproverType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    RoleId = table.Column<int>(type: "int", nullable: true),
                    MinAmount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalFlowSteps", x => x.Id);
                    table.CheckConstraint("CK_ApprovalFlowSteps_ApproverType", "[ApproverType] IN ('ROLE', 'USERS')");
                    table.CheckConstraint("CK_ApprovalFlowSteps_MinAmount", "[MinAmount] IS NULL OR [MinAmount] >= 0");
                    table.CheckConstraint("CK_ApprovalFlowSteps_Role", "[ApproverType] <> 'ROLE' OR [RoleId] IS NOT NULL");
                    table.ForeignKey(
                        name: "FK_ApprovalFlowSteps_ApprovalFlows_ApprovalFlowId",
                        column: x => x.ApprovalFlowId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "ApprovalFlows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ApprovalFlowSteps_Roles_RoleId",
                        column: x => x.RoleId,
                        principalSchema: "Core",
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Operators",
                schema: "Konfeksiyon",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OperatorCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    BreakGroupId = table.Column<int>(type: "int", nullable: true),
                    Role = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CanPause = table.Column<bool>(type: "bit", nullable: false),
                    CanReportBreakdown = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_Operators", x => x.Id);
                    table.CheckConstraint("CK_Operators_Role", "[Role] IN ('OPERATOR', 'LINE_CHIEF', 'MAINTENANCE')");
                    table.ForeignKey(
                        name: "FK_Operators_BreakGroups_BreakGroupId",
                        column: x => x.BreakGroupId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "BreakGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CustomerAddresses",
                schema: "Konfeksiyon",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AddressLine = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    District = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PostalCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    CountryCode = table.Column<string>(type: "char(2)", maxLength: 200, nullable: false),
                    ContactName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    IsDefaultInvoice = table.Column<bool>(type: "bit", nullable: false),
                    IsDefaultDelivery = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerAddresses", x => x.Id);
                    table.CheckConstraint("CK_CustomerAddresses_CountryCode", "LEN([CountryCode]) = 2");
                    table.ForeignKey(
                        name: "FK_CustomerAddresses_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CustomerBuyers",
                schema: "Konfeksiyon",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ContactName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerBuyers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerBuyers_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Models",
                schema: "Konfeksiyon",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ModelCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CustomerId = table.Column<int>(type: "int", nullable: true),
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
                    table.PrimaryKey("PK_Models", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Models_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PriceLists",
                schema: "Konfeksiyon",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CustomerId = table.Column<int>(type: "int", nullable: true),
                    Currency = table.Column<string>(type: "char(3)", maxLength: 200, nullable: false),
                    ValidFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    ValidTo = table.Column<DateOnly>(type: "date", nullable: true),
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
                    table.PrimaryKey("PK_PriceLists", x => x.Id);
                    table.CheckConstraint("CK_PriceLists_Validity", "[ValidTo] IS NULL OR [ValidTo] >= [ValidFrom]");
                    table.ForeignKey(
                        name: "FK_PriceLists_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductionAreas",
                schema: "Konfeksiyon",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    FactoryId = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_ProductionAreas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductionAreas_Factories_FactoryId",
                        column: x => x.FactoryId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "Factories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Operations",
                schema: "Konfeksiyon",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OperationCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Sam = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: false),
                    PieceRate = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    MachineTypeId = table.Column<int>(type: "int", nullable: true),
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
                    table.PrimaryKey("PK_Operations", x => x.Id);
                    table.CheckConstraint("CK_Operations_Sam", "[Sam] >= 0");
                    table.ForeignKey(
                        name: "FK_Operations_MachineTypes_MachineTypeId",
                        column: x => x.MachineTypeId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "MachineTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PayrollBonusTiers",
                schema: "Konfeksiyon",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PayrollSettingId = table.Column<int>(type: "int", nullable: false),
                    MinEfficiencyPercent = table.Column<decimal>(type: "decimal(7,2)", precision: 7, scale: 2, nullable: false),
                    BonusPercent = table.Column<decimal>(type: "decimal(7,2)", precision: 7, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollBonusTiers", x => x.Id);
                    table.CheckConstraint("CK_PayrollBonusTiers_Percents", "[MinEfficiencyPercent] > 0 AND [BonusPercent] > 0");
                    table.ForeignKey(
                        name: "FK_PayrollBonusTiers_PayrollSettings_PayrollSettingId",
                        column: x => x.PayrollSettingId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "PayrollSettings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BreakSchedules",
                schema: "Konfeksiyon",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ShiftId = table.Column<int>(type: "int", nullable: false),
                    BreakGroupId = table.Column<int>(type: "int", nullable: true),
                    StartTime = table.Column<TimeOnly>(type: "time(0)", nullable: false),
                    EndTime = table.Column<TimeOnly>(type: "time(0)", nullable: false),
                    Policy = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
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
                    table.PrimaryKey("PK_BreakSchedules", x => x.Id);
                    table.CheckConstraint("CK_BreakSchedules_Policy", "[Policy] IN ('IMMEDIATE', 'AFTER_BUNDLE', 'AFTER_QUANTITY', 'AFTER_OPERATION')");
                    table.CheckConstraint("CK_BreakSchedules_Times", "[StartTime] <> [EndTime]");
                    table.ForeignKey(
                        name: "FK_BreakSchedules_BreakGroups_BreakGroupId",
                        column: x => x.BreakGroupId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "BreakGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BreakSchedules_Shifts_ShiftId",
                        column: x => x.ShiftId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "Shifts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseOrders",
                schema: "Konfeksiyon",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderNo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    SupplierId = table.Column<int>(type: "int", nullable: false),
                    OrderDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ExpectedDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Currency = table.Column<string>(type: "varchar(3)", unicode: false, maxLength: 3, nullable: false),
                    ExchangeRate = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
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
                    table.PrimaryKey("PK_PurchaseOrders", x => x.Id);
                    table.CheckConstraint("CK_PurchaseOrders_ExchangeRate", "[ExchangeRate] IS NULL OR [ExchangeRate] > 0");
                    table.CheckConstraint("CK_PurchaseOrders_ExpectedDate", "[ExpectedDate] IS NULL OR [ExpectedDate] >= [OrderDate]");
                    table.CheckConstraint("CK_PurchaseOrders_Status", "[Status] IN ('DRAFT', 'APPROVED', 'PARTIALLY_RECEIVED', 'RECEIVED', 'CLOSED', 'CANCELLED')");
                    table.ForeignKey(
                        name: "FK_PurchaseOrders_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WarehouseLocations",
                schema: "Konfeksiyon",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WarehouseId = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Zone = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Rack = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Bin = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_WarehouseLocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WarehouseLocations_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ApprovalFlowStepUsers",
                schema: "Konfeksiyon",
                columns: table => new
                {
                    ApprovalFlowStepId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalFlowStepUsers", x => new { x.ApprovalFlowStepId, x.UserId });
                    table.ForeignKey(
                        name: "FK_ApprovalFlowStepUsers_ApprovalFlowSteps_ApprovalFlowStepId",
                        column: x => x.ApprovalFlowStepId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "ApprovalFlowSteps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ApprovalFlowStepUsers_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "Core",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OperatorCards",
                schema: "Konfeksiyon",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OperatorId = table.Column<int>(type: "int", nullable: false),
                    RfidCardId = table.Column<int>(type: "int", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OperatorCards", x => x.Id);
                    table.CheckConstraint("CK_OperatorCards_Period", "[RevokedAt] IS NULL OR [RevokedAt] >= [AssignedAt]");
                    table.ForeignKey(
                        name: "FK_OperatorCards_Operators_OperatorId",
                        column: x => x.OperatorId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "Operators",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OperatorCards_RfidCards_RfidCardId",
                        column: x => x.RfidCardId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "RfidCards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OperatorPhotos",
                schema: "Konfeksiyon",
                columns: table => new
                {
                    OperatorId = table.Column<int>(type: "int", nullable: false),
                    Content = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    ContentType = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OperatorPhotos", x => x.OperatorId);
                    table.ForeignKey(
                        name: "FK_OperatorPhotos_Operators_OperatorId",
                        column: x => x.OperatorId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "Operators",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SalesOrders",
                schema: "Konfeksiyon",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderNo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    CustomerBuyerId = table.Column<int>(type: "int", nullable: true),
                    DeliveryAddressId = table.Column<int>(type: "int", nullable: true),
                    CustomerOrderNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    OrderDate = table.Column<DateOnly>(type: "date", nullable: false),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Currency = table.Column<string>(type: "varchar(3)", unicode: false, maxLength: 3, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SubmittedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DecidedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DecidedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DecisionNote = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ApprovalRound = table.Column<int>(type: "int", nullable: false),
                    ClosedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ClosedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CloseNote = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ClosedAutomatically = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
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
                    table.PrimaryKey("PK_SalesOrders", x => x.Id);
                    table.CheckConstraint("CK_SalesOrders_DueDate", "[DueDate] >= [OrderDate]");
                    table.CheckConstraint("CK_SalesOrders_Status", "[Status] IN ('DRAFT', 'SUBMITTED', 'APPROVED', 'CLOSED', 'CANCELLED')");
                    table.ForeignKey(
                        name: "FK_SalesOrders_CustomerAddresses_DeliveryAddressId",
                        column: x => x.DeliveryAddressId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "CustomerAddresses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesOrders_CustomerBuyers_CustomerBuyerId",
                        column: x => x.CustomerBuyerId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "CustomerBuyers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesOrders_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductionRoutes",
                schema: "Konfeksiyon",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ModelId = table.Column<int>(type: "int", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
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
                    table.PrimaryKey("PK_ProductionRoutes", x => x.Id);
                    table.CheckConstraint("CK_ProductionRoutes_Status", "[Status] IN ('DRAFT', 'ACTIVE', 'ARCHIVED')");
                    table.CheckConstraint("CK_ProductionRoutes_Version", "[Version] > 0");
                    table.ForeignKey(
                        name: "FK_ProductionRoutes_Models_ModelId",
                        column: x => x.ModelId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "Models",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Products",
                schema: "Konfeksiyon",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ModelId = table.Column<int>(type: "int", nullable: false),
                    ProductGroupId = table.Column<int>(type: "int", nullable: true),
                    TargetGroup = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Season = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    CustomerArticleCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Composition = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    FabricWeight = table.Column<int>(type: "int", nullable: true),
                    CareInstructions = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    OriginCountryCode = table.Column<string>(type: "char(2)", maxLength: 200, nullable: false),
                    GtipCode = table.Column<string>(type: "varchar(12)", maxLength: 200, nullable: true),
                    VatRate = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    SalesUnit = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ListPrice = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    Currency = table.Column<string>(type: "char(3)", maxLength: 200, nullable: false),
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
                    table.PrimaryKey("PK_Products", x => x.Id);
                    table.CheckConstraint("CK_Products_FabricWeight", "[FabricWeight] IS NULL OR [FabricWeight] > 0");
                    table.CheckConstraint("CK_Products_ListPrice", "[ListPrice] IS NULL OR [ListPrice] >= 0");
                    table.CheckConstraint("CK_Products_SalesUnit", "[SalesUnit] IN ('PIECE', 'METER', 'KILOGRAM', 'ROLL', 'BOX')");
                    table.CheckConstraint("CK_Products_TargetGroup", "[TargetGroup] IN ('UNISEX', 'MEN', 'WOMEN', 'KIDS', 'BABY')");
                    table.CheckConstraint("CK_Products_VatRate", "[VatRate] >= 0 AND [VatRate] <= 100");
                    table.ForeignKey(
                        name: "FK_Products_Models_ModelId",
                        column: x => x.ModelId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "Models",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Products_ProductGroups_ProductGroupId",
                        column: x => x.ProductGroupId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "ProductGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductionLines",
                schema: "Konfeksiyon",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LineCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    ProductionAreaId = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_ProductionLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductionLines_ProductionAreas_ProductionAreaId",
                        column: x => x.ProductionAreaId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "ProductionAreas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Machines",
                schema: "Konfeksiyon",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MachineCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    MachineName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    MachineTypeId = table.Column<int>(type: "int", nullable: false),
                    OperationId = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Brand = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModelNo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SerialNo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ManufactureYear = table.Column<int>(type: "int", nullable: true),
                    AssetNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PurchaseDate = table.Column<DateOnly>(type: "date", nullable: true),
                    SupplierId = table.Column<int>(type: "int", nullable: true),
                    WarrantyEndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    MaintenanceIntervalDays = table.Column<int>(type: "int", nullable: true),
                    LastMaintenanceDate = table.Column<DateOnly>(type: "date", nullable: true),
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
                    table.PrimaryKey("PK_Machines", x => x.Id);
                    table.CheckConstraint("CK_Machines_MaintenanceInterval", "[MaintenanceIntervalDays] IS NULL OR [MaintenanceIntervalDays] > 0");
                    table.CheckConstraint("CK_Machines_Status", "[Status] IN ('AVAILABLE', 'RUNNING', 'IDLE', 'BREAKDOWN', 'MAINTENANCE', 'OFFLINE')");
                    table.CheckConstraint("CK_Machines_Warranty", "[WarrantyEndDate] IS NULL OR [PurchaseDate] IS NULL OR [WarrantyEndDate] >= [PurchaseDate]");
                    table.ForeignKey(
                        name: "FK_Machines_MachineTypes_MachineTypeId",
                        column: x => x.MachineTypeId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "MachineTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Machines_Operations_OperationId",
                        column: x => x.OperationId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "Operations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Machines_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OperatorOperationQualifications",
                schema: "Konfeksiyon",
                columns: table => new
                {
                    OperatorId = table.Column<int>(type: "int", nullable: false),
                    OperationId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OperatorOperationQualifications", x => new { x.OperatorId, x.OperationId });
                    table.ForeignKey(
                        name: "FK_OperatorOperationQualifications_Operations_OperationId",
                        column: x => x.OperationId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "Operations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OperatorOperationQualifications_Operators_OperatorId",
                        column: x => x.OperatorId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "Operators",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GoodsReceipts",
                schema: "Konfeksiyon",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PurchaseOrderId = table.Column<int>(type: "int", nullable: false),
                    WarehouseId = table.Column<int>(type: "int", nullable: false),
                    DocumentNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Note = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    ReceivedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReceivedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoodsReceipts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GoodsReceipts_PurchaseOrders_PurchaseOrderId",
                        column: x => x.PurchaseOrderId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "PurchaseOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GoodsReceipts_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StockCounts",
                schema: "Konfeksiyon",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CountNo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    WarehouseId = table.Column<int>(type: "int", nullable: false),
                    LocationId = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Note = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    PostedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PostedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockCounts", x => x.Id);
                    table.CheckConstraint("CK_StockCounts_Status", "[Status] IN ('OPEN', 'POSTED', 'CANCELLED')");
                    table.ForeignKey(
                        name: "FK_StockCounts_WarehouseLocations_LocationId",
                        column: x => x.LocationId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "WarehouseLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockCounts_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SalesOrderApprovalEvents",
                schema: "Konfeksiyon",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SalesOrderId = table.Column<int>(type: "int", nullable: false),
                    Round = table.Column<int>(type: "int", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    StepName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UserName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesOrderApprovalEvents", x => x.Id);
                    table.CheckConstraint("CK_SalesOrderApprovalEvents_EventType", "[EventType] IN ('SUBMITTED', 'APPROVED', 'REJECTED', 'REVISED', 'CANCELLED')");
                    table.ForeignKey(
                        name: "FK_SalesOrderApprovalEvents_SalesOrders_SalesOrderId",
                        column: x => x.SalesOrderId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "SalesOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SalesOrderApprovalSteps",
                schema: "Konfeksiyon",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SalesOrderId = table.Column<int>(type: "int", nullable: false),
                    Round = table.Column<int>(type: "int", nullable: false),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ApproverType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    RoleId = table.Column<int>(type: "int", nullable: true),
                    ApproverUserIds = table.Column<string>(type: "varchar(1000)", unicode: false, maxLength: 1000, nullable: true),
                    ApproverNames = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    DecidedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DecidedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesOrderApprovalSteps", x => x.Id);
                    table.CheckConstraint("CK_SalesOrderApprovalSteps_ApproverType", "[ApproverType] IN ('ROLE', 'USERS')");
                    table.CheckConstraint("CK_SalesOrderApprovalSteps_Status", "[Status] IN ('WAITING', 'PENDING', 'APPROVED', 'REJECTED', 'CANCELLED')");
                    table.ForeignKey(
                        name: "FK_SalesOrderApprovalSteps_SalesOrders_SalesOrderId",
                        column: x => x.SalesOrderId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "SalesOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductionRouteOperations",
                schema: "Konfeksiyon",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductionRouteId = table.Column<int>(type: "int", nullable: false),
                    SequenceNo = table.Column<int>(type: "int", nullable: false),
                    OperationId = table.Column<int>(type: "int", nullable: false),
                    Sam = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionRouteOperations", x => x.Id);
                    table.CheckConstraint("CK_ProductionRouteOperations_Sam", "[Sam] >= 0");
                    table.CheckConstraint("CK_ProductionRouteOperations_SequenceNo", "[SequenceNo] > 0");
                    table.ForeignKey(
                        name: "FK_ProductionRouteOperations_Operations_OperationId",
                        column: x => x.OperationId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "Operations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductionRouteOperations_ProductionRoutes_ProductionRouteId",
                        column: x => x.ProductionRouteId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "ProductionRoutes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CustomerDiscounts",
                schema: "Konfeksiyon",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: true),
                    DiscountPercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    ValidFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    ValidTo = table.Column<DateOnly>(type: "date", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerDiscounts", x => x.Id);
                    table.CheckConstraint("CK_CustomerDiscounts_Percent", "[DiscountPercent] > 0 AND [DiscountPercent] < 100");
                    table.CheckConstraint("CK_CustomerDiscounts_Validity", "[ValidTo] IS NULL OR [ValidTo] >= [ValidFrom]");
                    table.ForeignKey(
                        name: "FK_CustomerDiscounts_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustomerDiscounts_Products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PriceListItems",
                schema: "Konfeksiyon",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PriceListId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    ColorId = table.Column<int>(type: "int", nullable: true),
                    SizeId = table.Column<int>(type: "int", nullable: true),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PriceListItems", x => x.Id);
                    table.CheckConstraint("CK_PriceListItems_UnitPrice", "[UnitPrice] >= 0");
                    table.ForeignKey(
                        name: "FK_PriceListItems_Colors_ColorId",
                        column: x => x.ColorId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "Colors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PriceListItems_PriceLists_PriceListId",
                        column: x => x.PriceListId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "PriceLists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PriceListItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PriceListItems_Sizes_SizeId",
                        column: x => x.SizeId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "Sizes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductionOrders",
                schema: "Konfeksiyon",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderNo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    MaterialWarehouseId = table.Column<int>(type: "int", nullable: true),
                    SalesOrderId = table.Column<int>(type: "int", nullable: true),
                    CustomerOrderNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    OrderDate = table.Column<DateOnly>(type: "date", nullable: false),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    OperationSequence = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
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
                    table.PrimaryKey("PK_ProductionOrders", x => x.Id);
                    table.CheckConstraint("CK_ProductionOrders_DueDate", "[DueDate] >= [OrderDate]");
                    table.CheckConstraint("CK_ProductionOrders_OperationSequence", "[OperationSequence] IN ('SEQUENTIAL', 'FREE', 'ROUTE')");
                    table.CheckConstraint("CK_ProductionOrders_Status", "[Status] IN ('DRAFT', 'RELEASED', 'IN_PRODUCTION', 'COMPLETED', 'CANCELLED')");
                    table.ForeignKey(
                        name: "FK_ProductionOrders_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductionOrders_Products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductionOrders_SalesOrders_SalesOrderId",
                        column: x => x.SalesOrderId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "SalesOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductionOrders_Warehouses_MaterialWarehouseId",
                        column: x => x.MaterialWarehouseId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SalesOrderLines",
                schema: "Konfeksiyon",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SalesOrderId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    ColorId = table.Column<int>(type: "int", nullable: false),
                    SizeId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    ShippedFromReservation = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesOrderLines", x => x.Id);
                    table.CheckConstraint("CK_SalesOrderLines_Quantity", "[Quantity] > 0");
                    table.CheckConstraint("CK_SalesOrderLines_UnitPrice", "[UnitPrice] IS NULL OR [UnitPrice] >= 0");
                    table.ForeignKey(
                        name: "FK_SalesOrderLines_Colors_ColorId",
                        column: x => x.ColorId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "Colors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesOrderLines_Products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesOrderLines_SalesOrders_SalesOrderId",
                        column: x => x.SalesOrderId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "SalesOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesOrderLines_Sizes_SizeId",
                        column: x => x.SizeId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "Sizes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StockItems",
                schema: "Konfeksiyon",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    ItemType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: true),
                    ColorId = table.Column<int>(type: "int", nullable: true),
                    SizeId = table.Column<int>(type: "int", nullable: true),
                    UnitCost = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    IsLotTracked = table.Column<bool>(type: "bit", nullable: false),
                    MinimumStock = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: true),
                    ReorderQuantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: true),
                    SupplierId = table.Column<int>(type: "int", nullable: true),
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
                    table.PrimaryKey("PK_StockItems", x => x.Id);
                    table.CheckConstraint("CK_StockItems_FinishedGood", "([ItemType] = 'FINISHED_GOOD' AND [ProductId] IS NOT NULL AND [ColorId] IS NOT NULL AND [SizeId] IS NOT NULL) OR ([ItemType] <> 'FINISHED_GOOD' AND [ProductId] IS NULL AND [ColorId] IS NULL AND [SizeId] IS NULL)");
                    table.CheckConstraint("CK_StockItems_ItemType", "[ItemType] IN ('FABRIC', 'ACCESSORY', 'FINISHED_GOOD', 'OTHER')");
                    table.CheckConstraint("CK_StockItems_Reorder", "([MinimumStock] IS NULL OR [MinimumStock] >= 0) AND ([ReorderQuantity] IS NULL OR [ReorderQuantity] > 0)");
                    table.CheckConstraint("CK_StockItems_Unit", "[Unit] IN ('PIECE', 'METER', 'KILOGRAM', 'ROLL', 'BOX')");
                    table.ForeignKey(
                        name: "FK_StockItems_Colors_ColorId",
                        column: x => x.ColorId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "Colors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockItems_Sizes_SizeId",
                        column: x => x.SizeId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "Sizes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockItems_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LineMachineAssignments",
                schema: "Konfeksiyon",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductionLineId = table.Column<int>(type: "int", nullable: false),
                    MachineId = table.Column<int>(type: "int", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UnassignedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LineMachineAssignments", x => x.Id);
                    table.CheckConstraint("CK_LineMachineAssignments_Period", "[UnassignedAt] IS NULL OR [UnassignedAt] >= [AssignedAt]");
                    table.CheckConstraint("CK_LineMachineAssignments_Position", "[Position] > 0");
                    table.ForeignKey(
                        name: "FK_LineMachineAssignments_Machines_MachineId",
                        column: x => x.MachineId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "Machines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LineMachineAssignments_ProductionLines_ProductionLineId",
                        column: x => x.ProductionLineId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "ProductionLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MachineOperations",
                schema: "Konfeksiyon",
                columns: table => new
                {
                    MachineId = table.Column<int>(type: "int", nullable: false),
                    OperationId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MachineOperations", x => new { x.MachineId, x.OperationId });
                    table.ForeignKey(
                        name: "FK_MachineOperations_Machines_MachineId",
                        column: x => x.MachineId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "Machines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MachineOperations_Operations_OperationId",
                        column: x => x.OperationId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "Operations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MachineStatusLogs",
                schema: "Konfeksiyon",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MachineId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    ChangedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TabletId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MachineStatusLogs", x => x.Id);
                    table.CheckConstraint("CK_MachineStatusLogs_Period", "[EndedAt] IS NULL OR [EndedAt] >= [StartedAt]");
                    table.CheckConstraint("CK_MachineStatusLogs_Status", "[Status] IN ('AVAILABLE', 'RUNNING', 'IDLE', 'BREAKDOWN', 'MAINTENANCE', 'OFFLINE')");
                    table.ForeignKey(
                        name: "FK_MachineStatusLogs_Machines_MachineId",
                        column: x => x.MachineId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "Machines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OperatorMachines",
                schema: "Konfeksiyon",
                columns: table => new
                {
                    OperatorId = table.Column<int>(type: "int", nullable: false),
                    MachineId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OperatorMachines", x => new { x.OperatorId, x.MachineId });
                    table.ForeignKey(
                        name: "FK_OperatorMachines_Machines_MachineId",
                        column: x => x.MachineId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "Machines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OperatorMachines_Operators_OperatorId",
                        column: x => x.OperatorId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "Operators",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Tablets",
                schema: "Konfeksiyon",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TabletCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    MachineId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    DeviceIdentifier = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DeviceKeyHash = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: true),
                    LastSeenAt = table.Column<DateTime>(type: "datetime2", nullable: true),
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
                    table.PrimaryKey("PK_Tablets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tablets_Machines_MachineId",
                        column: x => x.MachineId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "Machines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OperationDependencies",
                schema: "Konfeksiyon",
                columns: table => new
                {
                    RouteOperationId = table.Column<int>(type: "int", nullable: false),
                    PredecessorId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OperationDependencies", x => new { x.RouteOperationId, x.PredecessorId });
                    table.CheckConstraint("CK_OperationDependencies_NotSelf", "[RouteOperationId] <> [PredecessorId]");
                    table.ForeignKey(
                        name: "FK_OperationDependencies_ProductionRouteOperations_PredecessorId",
                        column: x => x.PredecessorId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "ProductionRouteOperations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OperationDependencies_ProductionRouteOperations_RouteOperationId",
                        column: x => x.RouteOperationId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "ProductionRouteOperations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Bundles",
                schema: "Konfeksiyon",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BundleNo = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    ProductionOrderId = table.Column<int>(type: "int", nullable: false),
                    ColorId = table.Column<int>(type: "int", nullable: false),
                    SizeId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    ProductionRouteId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    SequenceMode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bundles", x => x.Id);
                    table.CheckConstraint("CK_Bundles_Quantity", "[Quantity] > 0");
                    table.CheckConstraint("CK_Bundles_SequenceMode", "[SequenceMode] IN ('SEQUENTIAL', 'FREE', 'ROUTE')");
                    table.CheckConstraint("CK_Bundles_Status", "[Status] IN ('CREATED', 'WAITING', 'IN_PRODUCTION', 'PARTIALLY_COMPLETED', 'COMPLETED', 'QUALITY_HOLD', 'REWORK', 'CANCELLED', 'CLOSED')");
                    table.ForeignKey(
                        name: "FK_Bundles_Colors_ColorId",
                        column: x => x.ColorId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "Colors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Bundles_ProductionOrders_ProductionOrderId",
                        column: x => x.ProductionOrderId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "ProductionOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Bundles_ProductionRoutes_ProductionRouteId",
                        column: x => x.ProductionRouteId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "ProductionRoutes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Bundles_Sizes_SizeId",
                        column: x => x.SizeId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "Sizes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LineProductionPlans",
                schema: "Konfeksiyon",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductionLineId = table.Column<int>(type: "int", nullable: false),
                    ProductionOrderId = table.Column<int>(type: "int", nullable: false),
                    PlannedStart = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PlannedEnd = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PlannedQuantity = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LineProductionPlans", x => x.Id);
                    table.CheckConstraint("CK_LineProductionPlans_Period", "[PlannedEnd] > [PlannedStart]");
                    table.CheckConstraint("CK_LineProductionPlans_Quantity", "[PlannedQuantity] > 0");
                    table.CheckConstraint("CK_LineProductionPlans_Status", "[Status] IN ('PLANNED', 'COMPLETED', 'CANCELLED')");
                    table.ForeignKey(
                        name: "FK_LineProductionPlans_ProductionLines_ProductionLineId",
                        column: x => x.ProductionLineId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "ProductionLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LineProductionPlans_ProductionOrders_ProductionOrderId",
                        column: x => x.ProductionOrderId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "ProductionOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductionOrderItems",
                schema: "Konfeksiyon",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductionOrderId = table.Column<int>(type: "int", nullable: false),
                    ColorId = table.Column<int>(type: "int", nullable: false),
                    SizeId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionOrderItems", x => x.Id);
                    table.CheckConstraint("CK_ProductionOrderItems_Quantity", "[Quantity] > 0");
                    table.ForeignKey(
                        name: "FK_ProductionOrderItems_Colors_ColorId",
                        column: x => x.ColorId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "Colors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductionOrderItems_ProductionOrders_ProductionOrderId",
                        column: x => x.ProductionOrderId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "ProductionOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductionOrderItems_Sizes_SizeId",
                        column: x => x.SizeId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "Sizes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Shipments",
                schema: "Konfeksiyon",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ShipmentNo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    WarehouseId = table.Column<int>(type: "int", nullable: false),
                    ProductionOrderId = table.Column<int>(type: "int", nullable: true),
                    SalesOrderId = table.Column<int>(type: "int", nullable: true),
                    ShipDate = table.Column<DateOnly>(type: "date", nullable: false),
                    DocumentNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Carrier = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ShippedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ShippedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
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
                    table.PrimaryKey("PK_Shipments", x => x.Id);
                    table.CheckConstraint("CK_Shipments_Shipped", "[Status] <> 'SHIPPED' OR ([ShippedAt] IS NOT NULL AND [ShippedBy] IS NOT NULL)");
                    table.CheckConstraint("CK_Shipments_Status", "[Status] IN ('DRAFT', 'SHIPPED', 'CANCELLED')");
                    table.ForeignKey(
                        name: "FK_Shipments_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Shipments_ProductionOrders_ProductionOrderId",
                        column: x => x.ProductionOrderId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "ProductionOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Shipments_SalesOrders_SalesOrderId",
                        column: x => x.SalesOrderId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "SalesOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Shipments_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductMaterials",
                schema: "Konfeksiyon",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    StockItemId = table.Column<int>(type: "int", nullable: false),
                    ColorId = table.Column<int>(type: "int", nullable: true),
                    SizeId = table.Column<int>(type: "int", nullable: true),
                    QuantityPerPiece = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    WastePercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductMaterials", x => x.Id);
                    table.CheckConstraint("CK_ProductMaterials_QuantityPerPiece", "[QuantityPerPiece] > 0");
                    table.CheckConstraint("CK_ProductMaterials_WastePercent", "[WastePercent] >= 0 AND [WastePercent] <= 100");
                    table.ForeignKey(
                        name: "FK_ProductMaterials_Colors_ColorId",
                        column: x => x.ColorId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "Colors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductMaterials_Products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductMaterials_Sizes_SizeId",
                        column: x => x.SizeId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "Sizes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductMaterials_StockItems_StockItemId",
                        column: x => x.StockItemId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "StockItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseOrderItems",
                schema: "Konfeksiyon",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PurchaseOrderId = table.Column<int>(type: "int", nullable: false),
                    StockItemId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    ProductionOrderId = table.Column<int>(type: "int", nullable: true),
                    ReceivedQuantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseOrderItems", x => x.Id);
                    table.CheckConstraint("CK_PurchaseOrderItems_Quantities", "[Quantity] > 0 AND [ReceivedQuantity] >= 0 AND [ReceivedQuantity] <= [Quantity]");
                    table.CheckConstraint("CK_PurchaseOrderItems_UnitPrice", "[UnitPrice] IS NULL OR [UnitPrice] >= 0");
                    table.ForeignKey(
                        name: "FK_PurchaseOrderItems_ProductionOrders_ProductionOrderId",
                        column: x => x.ProductionOrderId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "ProductionOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseOrderItems_PurchaseOrders_PurchaseOrderId",
                        column: x => x.PurchaseOrderId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "PurchaseOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseOrderItems_StockItems_StockItemId",
                        column: x => x.StockItemId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "StockItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SalesOrderReservations",
                schema: "Konfeksiyon",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SalesOrderId = table.Column<int>(type: "int", nullable: false),
                    SalesOrderLineId = table.Column<int>(type: "int", nullable: false),
                    WarehouseId = table.Column<int>(type: "int", nullable: false),
                    StockItemId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesOrderReservations", x => x.Id);
                    table.CheckConstraint("CK_SalesOrderReservations_Quantity", "[Quantity] > 0");
                    table.ForeignKey(
                        name: "FK_SalesOrderReservations_SalesOrderLines_SalesOrderLineId",
                        column: x => x.SalesOrderLineId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "SalesOrderLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesOrderReservations_SalesOrders_SalesOrderId",
                        column: x => x.SalesOrderId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "SalesOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesOrderReservations_StockItems_StockItemId",
                        column: x => x.StockItemId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "StockItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesOrderReservations_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StockLots",
                schema: "Konfeksiyon",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StockItemId = table.Column<int>(type: "int", nullable: false),
                    LotNo = table.Column<string>(type: "nvarchar(12)", maxLength: 12, nullable: false, computedColumnSql: "CONCAT(N'T', RIGHT(CONCAT('00000000', [Id]), 8))", stored: true),
                    BatchNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Shade = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Width = table.Column<decimal>(type: "decimal(9,2)", precision: 9, scale: 2, nullable: true),
                    SupplierRollNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    InitialQuantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    ReceivedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PurchaseOrderId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockLots", x => x.Id);
                    table.CheckConstraint("CK_StockLots_InitialQuantity", "[InitialQuantity] > 0");
                    table.CheckConstraint("CK_StockLots_Width", "[Width] IS NULL OR [Width] > 0");
                    table.ForeignKey(
                        name: "FK_StockLots_PurchaseOrders_PurchaseOrderId",
                        column: x => x.PurchaseOrderId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "PurchaseOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockLots_StockItems_StockItemId",
                        column: x => x.StockItemId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "StockItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StockReservations",
                schema: "Konfeksiyon",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductionOrderId = table.Column<int>(type: "int", nullable: false),
                    WarehouseId = table.Column<int>(type: "int", nullable: false),
                    StockItemId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockReservations", x => x.Id);
                    table.CheckConstraint("CK_StockReservations_Quantity", "[Quantity] > 0");
                    table.ForeignKey(
                        name: "FK_StockReservations_ProductionOrders_ProductionOrderId",
                        column: x => x.ProductionOrderId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "ProductionOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockReservations_StockItems_StockItemId",
                        column: x => x.StockItemId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "StockItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockReservations_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkSessions",
                schema: "Konfeksiyon",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OperatorId = table.Column<int>(type: "int", nullable: false),
                    MachineId = table.Column<int>(type: "int", nullable: false),
                    TabletId = table.Column<int>(type: "int", nullable: false),
                    LoginAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LogoutAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    EndReason = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    ClosedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkSessions", x => x.Id);
                    table.CheckConstraint("CK_WorkSessions_EndReason", "[EndReason] IS NULL OR [EndReason] IN ('LOGOUT', 'TAKEN_OVER', 'MOVED_TO_ANOTHER_MACHINE', 'CLOSED_BY_SUPERVISOR', 'SHIFT_ENDED')");
                    table.CheckConstraint("CK_WorkSessions_Period", "[LogoutAt] IS NULL OR [LogoutAt] >= [LoginAt]");
                    table.CheckConstraint("CK_WorkSessions_Status", "[Status] IN ('ACTIVE', 'LOGGED_OUT', 'BREAK', 'PAUSED', 'MACHINE_BREAKDOWN', 'WAITING_FOR_BUNDLE')");
                    table.ForeignKey(
                        name: "FK_WorkSessions_Machines_MachineId",
                        column: x => x.MachineId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "Machines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkSessions_Operators_OperatorId",
                        column: x => x.OperatorId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "Operators",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkSessions_Tablets_TabletId",
                        column: x => x.TabletId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "Tablets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BundleCardAssignments",
                schema: "Konfeksiyon",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RfidCardId = table.Column<int>(type: "int", nullable: false),
                    BundleId = table.Column<int>(type: "int", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReleasedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BundleCardAssignments", x => x.Id);
                    table.CheckConstraint("CK_BundleCardAssignments_Period", "[ReleasedAt] IS NULL OR [ReleasedAt] >= [AssignedAt]");
                    table.ForeignKey(
                        name: "FK_BundleCardAssignments_Bundles_BundleId",
                        column: x => x.BundleId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "Bundles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BundleCardAssignments_RfidCards_RfidCardId",
                        column: x => x.RfidCardId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "RfidCards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BundleOperations",
                schema: "Konfeksiyon",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BundleId = table.Column<int>(type: "int", nullable: false),
                    RouteOperationId = table.Column<int>(type: "int", nullable: false),
                    OperationId = table.Column<int>(type: "int", nullable: false),
                    SequenceNo = table.Column<int>(type: "int", nullable: false),
                    Sam = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: false),
                    UnitRate = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    PlannedQuantity = table.Column<int>(type: "int", nullable: false),
                    ProducedQuantity = table.Column<int>(type: "int", nullable: false),
                    RejectedQuantity = table.Column<int>(type: "int", nullable: false),
                    ReworkQuantity = table.Column<int>(type: "int", nullable: false),
                    MachineId = table.Column<int>(type: "int", nullable: true),
                    OperatorId = table.Column<int>(type: "int", nullable: true),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BundleOperations", x => x.Id);
                    table.CheckConstraint("CK_BundleOperations_Quantities", "[PlannedQuantity] > 0 AND [ProducedQuantity] >= 0 AND [RejectedQuantity] >= 0 AND [ProducedQuantity] + [RejectedQuantity] <= [PlannedQuantity]");
                    table.CheckConstraint("CK_BundleOperations_ReworkQuantity", "[ReworkQuantity] >= 0");
                    table.CheckConstraint("CK_BundleOperations_Status", "[Status] IN ('WAITING', 'READY', 'IN_PROGRESS', 'PAUSED', 'COMPLETED', 'QUALITY_HOLD', 'CANCELLED')");
                    table.ForeignKey(
                        name: "FK_BundleOperations_Bundles_BundleId",
                        column: x => x.BundleId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "Bundles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BundleOperations_Machines_MachineId",
                        column: x => x.MachineId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "Machines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BundleOperations_Operations_OperationId",
                        column: x => x.OperationId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "Operations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BundleOperations_Operators_OperatorId",
                        column: x => x.OperatorId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "Operators",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BundleOperations_ProductionRouteOperations_RouteOperationId",
                        column: x => x.RouteOperationId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "ProductionRouteOperations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ShipmentItems",
                schema: "Konfeksiyon",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ShipmentId = table.Column<int>(type: "int", nullable: false),
                    StockItemId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShipmentItems", x => x.Id);
                    table.CheckConstraint("CK_ShipmentItems_Quantity", "[Quantity] > 0");
                    table.ForeignKey(
                        name: "FK_ShipmentItems_Shipments_ShipmentId",
                        column: x => x.ShipmentId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "Shipments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ShipmentItems_StockItems_StockItemId",
                        column: x => x.StockItemId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "StockItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GoodsReceiptLines",
                schema: "Konfeksiyon",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GoodsReceiptId = table.Column<long>(type: "bigint", nullable: false),
                    PurchaseOrderItemId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoodsReceiptLines", x => x.Id);
                    table.CheckConstraint("CK_GoodsReceiptLines_Quantity", "[Quantity] > 0");
                    table.ForeignKey(
                        name: "FK_GoodsReceiptLines_GoodsReceipts_GoodsReceiptId",
                        column: x => x.GoodsReceiptId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "GoodsReceipts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GoodsReceiptLines_PurchaseOrderItems_PurchaseOrderItemId",
                        column: x => x.PurchaseOrderItemId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "PurchaseOrderItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StockCountLines",
                schema: "Konfeksiyon",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StockCountId = table.Column<int>(type: "int", nullable: false),
                    LocationId = table.Column<int>(type: "int", nullable: false),
                    StockItemId = table.Column<int>(type: "int", nullable: false),
                    LotId = table.Column<int>(type: "int", nullable: true),
                    ExpectedQuantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    CountedQuantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockCountLines", x => x.Id);
                    table.CheckConstraint("CK_StockCountLines_Quantities", "[ExpectedQuantity] >= 0 AND ([CountedQuantity] IS NULL OR [CountedQuantity] >= 0)");
                    table.ForeignKey(
                        name: "FK_StockCountLines_StockCounts_StockCountId",
                        column: x => x.StockCountId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "StockCounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockCountLines_StockItems_StockItemId",
                        column: x => x.StockItemId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "StockItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockCountLines_StockLots_LotId",
                        column: x => x.LotId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "StockLots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockCountLines_WarehouseLocations_LocationId",
                        column: x => x.LocationId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "WarehouseLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Stocks",
                schema: "Konfeksiyon",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WarehouseId = table.Column<int>(type: "int", nullable: false),
                    LocationId = table.Column<int>(type: "int", nullable: false),
                    StockItemId = table.Column<int>(type: "int", nullable: false),
                    LotId = table.Column<int>(type: "int", nullable: true),
                    Quantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Stocks", x => x.Id);
                    table.CheckConstraint("CK_Stocks_Quantity", "[Quantity] >= 0");
                    table.ForeignKey(
                        name: "FK_Stocks_StockItems_StockItemId",
                        column: x => x.StockItemId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "StockItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Stocks_StockLots_LotId",
                        column: x => x.LotId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "StockLots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Stocks_WarehouseLocations_LocationId",
                        column: x => x.LocationId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "WarehouseLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Stocks_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StockTransactions",
                schema: "Konfeksiyon",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    WarehouseId = table.Column<int>(type: "int", nullable: false),
                    LocationId = table.Column<int>(type: "int", nullable: false),
                    StockItemId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    BalanceAfter = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    LocationBalanceAfter = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    LotId = table.Column<int>(type: "int", nullable: true),
                    LotBalanceAfter = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: true),
                    UnitCost = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    CounterWarehouseId = table.Column<int>(type: "int", nullable: true),
                    CounterLocationId = table.Column<int>(type: "int", nullable: true),
                    BundleId = table.Column<int>(type: "int", nullable: true),
                    ProductionOrderId = table.Column<int>(type: "int", nullable: true),
                    PurchaseOrderId = table.Column<int>(type: "int", nullable: true),
                    ShipmentId = table.Column<int>(type: "int", nullable: true),
                    DocumentNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Note = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    OccurredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockTransactions", x => x.Id);
                    table.CheckConstraint("CK_StockTransactions_Quantity", "[Quantity] <> 0 AND [BalanceAfter] >= 0 AND [LocationBalanceAfter] >= 0");
                    table.CheckConstraint("CK_StockTransactions_Type", "[Type] IN ('RECEIPT', 'ISSUE', 'TRANSFER_OUT', 'TRANSFER_IN', 'ADJUSTMENT', 'PURCHASE_RECEIPT', 'SHIPMENT', 'PRODUCTION_RECEIPT', 'PRODUCTION_ISSUE', 'PRODUCTION_RETURN', 'RELOCATION_OUT', 'RELOCATION_IN')");
                    table.ForeignKey(
                        name: "FK_StockTransactions_Bundles_BundleId",
                        column: x => x.BundleId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "Bundles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockTransactions_ProductionOrders_ProductionOrderId",
                        column: x => x.ProductionOrderId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "ProductionOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockTransactions_PurchaseOrders_PurchaseOrderId",
                        column: x => x.PurchaseOrderId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "PurchaseOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockTransactions_Shipments_ShipmentId",
                        column: x => x.ShipmentId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "Shipments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockTransactions_StockItems_StockItemId",
                        column: x => x.StockItemId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "StockItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockTransactions_StockLots_LotId",
                        column: x => x.LotId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "StockLots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockTransactions_WarehouseLocations_CounterLocationId",
                        column: x => x.CounterLocationId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "WarehouseLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockTransactions_WarehouseLocations_LocationId",
                        column: x => x.LocationId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "WarehouseLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockTransactions_Warehouses_CounterWarehouseId",
                        column: x => x.CounterWarehouseId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockTransactions_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkSessionPauses",
                schema: "Konfeksiyon",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkSessionId = table.Column<long>(type: "bigint", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    StopReasonId = table.Column<int>(type: "int", nullable: true),
                    BreakScheduleId = table.Column<int>(type: "int", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkSessionPauses", x => x.Id);
                    table.CheckConstraint("CK_WorkSessionPauses_Period", "[EndedAt] IS NULL OR [EndedAt] >= [StartedAt]");
                    table.CheckConstraint("CK_WorkSessionPauses_Type", "[Type] IN ('BREAK', 'PAUSE', 'MACHINE_BREAKDOWN', 'WAITING_FOR_BUNDLE')");
                    table.ForeignKey(
                        name: "FK_WorkSessionPauses_BreakSchedules_BreakScheduleId",
                        column: x => x.BreakScheduleId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "BreakSchedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkSessionPauses_StopReasons_StopReasonId",
                        column: x => x.StopReasonId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "StopReasons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkSessionPauses_WorkSessions_WorkSessionId",
                        column: x => x.WorkSessionId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "WorkSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BundleOperationEvents",
                schema: "Konfeksiyon",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    BundleId = table.Column<int>(type: "int", nullable: false),
                    BundleOperationId = table.Column<long>(type: "bigint", nullable: false),
                    MachineId = table.Column<int>(type: "int", nullable: false),
                    OperatorId = table.Column<int>(type: "int", nullable: false),
                    WorkSessionId = table.Column<long>(type: "bigint", nullable: true),
                    TabletId = table.Column<int>(type: "int", nullable: true),
                    EnteredBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ProducedQuantity = table.Column<int>(type: "int", nullable: false),
                    RejectedQuantity = table.Column<int>(type: "int", nullable: false),
                    RunSeconds = table.Column<int>(type: "int", nullable: true),
                    EfficiencyPercent = table.Column<int>(type: "int", nullable: true),
                    OccurredAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BundleOperationEvents", x => x.Id);
                    table.CheckConstraint("CK_BundleOperationEvents_EventType", "[EventType] IN ('OPERATION_STARTED', 'OPERATION_RESUMED', 'OPERATION_PAUSED', 'OPERATION_COMPLETED', 'QUANTITY_REPORTED', 'REJECTION_REPORTED')");
                    table.CheckConstraint("CK_BundleOperationEvents_Quantities", "[ProducedQuantity] >= 0 AND [RejectedQuantity] >= 0");
                    table.ForeignKey(
                        name: "FK_BundleOperationEvents_BundleOperations_BundleOperationId",
                        column: x => x.BundleOperationId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "BundleOperations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BundleOperationEvents_Bundles_BundleId",
                        column: x => x.BundleId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "Bundles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BundleOperationEvents_Machines_MachineId",
                        column: x => x.MachineId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "Machines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BundleOperationEvents_Operators_OperatorId",
                        column: x => x.OperatorId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "Operators",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BundleOperationEvents_WorkSessions_WorkSessionId",
                        column: x => x.WorkSessionId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "WorkSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "QualityInspections",
                schema: "Konfeksiyon",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BundleId = table.Column<int>(type: "int", nullable: false),
                    BundleOperationId = table.Column<long>(type: "bigint", nullable: false),
                    Result = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    InspectedQuantity = table.Column<int>(type: "int", nullable: false),
                    RejectedQuantity = table.Column<int>(type: "int", nullable: false),
                    ReworkQuantity = table.Column<int>(type: "int", nullable: false),
                    PlacedOnHold = table.Column<bool>(type: "bit", nullable: false),
                    ReworkTaskId = table.Column<long>(type: "bigint", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    InspectedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    InspectedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QualityInspections", x => x.Id);
                    table.CheckConstraint("CK_QualityInspections_Quantities", "[InspectedQuantity] > 0 AND [RejectedQuantity] >= 0 AND [ReworkQuantity] >= 0 AND [RejectedQuantity] + [ReworkQuantity] <= [InspectedQuantity]");
                    table.CheckConstraint("CK_QualityInspections_Result", "[Result] IN ('PASSED', 'REJECTED', 'REWORK', 'HOLD')");
                    table.ForeignKey(
                        name: "FK_QualityInspections_BundleOperations_BundleOperationId",
                        column: x => x.BundleOperationId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "BundleOperations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QualityInspections_Bundles_BundleId",
                        column: x => x.BundleId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "Bundles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "QualityRecords",
                schema: "Konfeksiyon",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    QualityInspectionId = table.Column<long>(type: "bigint", nullable: false),
                    DefectTypeId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QualityRecords", x => x.Id);
                    table.CheckConstraint("CK_QualityRecords_Quantity", "[Quantity] > 0");
                    table.ForeignKey(
                        name: "FK_QualityRecords_DefectTypes_DefectTypeId",
                        column: x => x.DefectTypeId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "DefectTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QualityRecords_QualityInspections_QualityInspectionId",
                        column: x => x.QualityInspectionId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "QualityInspections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ReworkTasks",
                schema: "Konfeksiyon",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    QualityInspectionId = table.Column<long>(type: "bigint", nullable: false),
                    BundleId = table.Column<int>(type: "int", nullable: false),
                    BundleOperationId = table.Column<long>(type: "bigint", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DoneAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ClosedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ClosedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReworkTasks", x => x.Id);
                    table.CheckConstraint("CK_ReworkTasks_Quantity", "[Quantity] > 0");
                    table.CheckConstraint("CK_ReworkTasks_Status", "[Status] IN ('OPEN', 'DONE', 'APPROVED', 'REJECTED')");
                    table.ForeignKey(
                        name: "FK_ReworkTasks_BundleOperations_BundleOperationId",
                        column: x => x.BundleOperationId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "BundleOperations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReworkTasks_Bundles_BundleId",
                        column: x => x.BundleId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "Bundles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReworkTasks_QualityInspections_QualityInspectionId",
                        column: x => x.QualityInspectionId,
                        principalSchema: "Konfeksiyon",
                        principalTable: "QualityInspections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "Konfeksiyon",
                table: "DefectTypes",
                columns: new[] { "Id", "Code", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "IsActive", "IsDeleted", "Name", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
                    { 1, "DIKIS", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "system", null, null, true, false, "Dikiş hatası", null, null },
                    { 2, "OLCU", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "system", null, null, true, false, "Ölçü hatası", null, null },
                    { 3, "LEKE", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "system", null, null, true, false, "Leke", null, null },
                    { 4, "SOKUK", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "system", null, null, true, false, "Sökük", null, null },
                    { 5, "KUMAS", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "system", null, null, true, false, "Kumaş hatası", null, null },
                    { 6, "YANLIS-RENK", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "system", null, null, true, false, "Yanlış renk", null, null },
                    { 7, "YANLIS-BEDEN", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "system", null, null, true, false, "Yanlış beden", null, null },
                    { 8, "DIGER", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "system", null, null, true, false, "Diğer", null, null }
                });

            migrationBuilder.InsertData(
                schema: "Konfeksiyon",
                table: "PayrollSettings",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "Currency", "MinuteRate", "UpdatedAt", "UpdatedBy" },
                values: new object[] { 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "system", "TRY", 0m, null, null });

            migrationBuilder.InsertData(
                schema: "Konfeksiyon",
                table: "StopReasons",
                columns: new[] { "Id", "Category", "Code", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "IsActive", "IsDeleted", "Name", "SortOrder", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
                    { 1, "PAUSE", "SU", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "system", null, null, true, false, "Su", 10, null, null },
                    { 2, "PAUSE", "TUVALET", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "system", null, null, true, false, "Tuvalet", 20, null, null },
                    { 3, "PAUSE", "KISISEL", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "system", null, null, true, false, "Kişisel ihtiyaç", 30, null, null },
                    { 4, "PAUSE", "BEKLEME", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "system", null, null, true, false, "Bekleme", 40, null, null },
                    { 5, "PAUSE", "DIGER", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "system", null, null, true, false, "Diğer", 90, null, null },
                    { 6, "BREAKDOWN", "IGNE", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "system", null, null, true, false, "İğne kırıldı", 10, null, null },
                    { 7, "BREAKDOWN", "IPLIK", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "system", null, null, true, false, "İplik / masura sorunu", 20, null, null },
                    { 8, "BREAKDOWN", "MOTOR", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "system", null, null, true, false, "Motor arızası", 30, null, null },
                    { 9, "BREAKDOWN", "ELEKTRIK", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "system", null, null, true, false, "Elektrik arızası", 40, null, null },
                    { 10, "BREAKDOWN", "ARIZA-DIGER", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "system", null, null, true, false, "Diğer arıza", 90, null, null }
                });

            migrationBuilder.InsertData(
                schema: "Konfeksiyon",
                table: "Warehouses",
                columns: new[] { "Id", "Code", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "IsActive", "IsDeleted", "Name", "Type", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
                    { 1, "HAMMADDE", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "system", null, null, true, false, "Hammadde Deposu", "RAW_MATERIAL", null, null },
                    { 2, "MAMUL", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "system", null, null, true, false, "Mamul Deposu", "FINISHED_GOODS", null, null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalFlows_DocumentType",
                schema: "Konfeksiyon",
                table: "ApprovalFlows",
                column: "DocumentType",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalFlowSteps_ApprovalFlowId_Sequence",
                schema: "Konfeksiyon",
                table: "ApprovalFlowSteps",
                columns: new[] { "ApprovalFlowId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalFlowSteps_RoleId",
                schema: "Konfeksiyon",
                table: "ApprovalFlowSteps",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalFlowStepUsers_UserId",
                schema: "Konfeksiyon",
                table: "ApprovalFlowStepUsers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_BreakGroups_Code",
                schema: "Konfeksiyon",
                table: "BreakGroups",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_BreakSchedules_BreakGroupId",
                schema: "Konfeksiyon",
                table: "BreakSchedules",
                column: "BreakGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_BreakSchedules_Code",
                schema: "Konfeksiyon",
                table: "BreakSchedules",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_BreakSchedules_ShiftId",
                schema: "Konfeksiyon",
                table: "BreakSchedules",
                column: "ShiftId");

            migrationBuilder.CreateIndex(
                name: "IX_BundleCardAssignments_BundleId_AssignedAt",
                schema: "Konfeksiyon",
                table: "BundleCardAssignments",
                columns: new[] { "BundleId", "AssignedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_BundleCardAssignments_RfidCardId_AssignedAt",
                schema: "Konfeksiyon",
                table: "BundleCardAssignments",
                columns: new[] { "RfidCardId", "AssignedAt" });

            migrationBuilder.CreateIndex(
                name: "UX_BundleCardAssignments_CurrentPerBundle",
                schema: "Konfeksiyon",
                table: "BundleCardAssignments",
                column: "BundleId",
                unique: true,
                filter: "[ReleasedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "UX_BundleCardAssignments_CurrentPerCard",
                schema: "Konfeksiyon",
                table: "BundleCardAssignments",
                column: "RfidCardId",
                unique: true,
                filter: "[ReleasedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_BundleOperationEvents_BundleId_Id",
                schema: "Konfeksiyon",
                table: "BundleOperationEvents",
                columns: new[] { "BundleId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_BundleOperationEvents_BundleOperationId_Id",
                schema: "Konfeksiyon",
                table: "BundleOperationEvents",
                columns: new[] { "BundleOperationId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_BundleOperationEvents_MachineId_OccurredAt",
                schema: "Konfeksiyon",
                table: "BundleOperationEvents",
                columns: new[] { "MachineId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_BundleOperationEvents_OccurredAt_EventType",
                schema: "Konfeksiyon",
                table: "BundleOperationEvents",
                columns: new[] { "OccurredAt", "EventType" });

            migrationBuilder.CreateIndex(
                name: "IX_BundleOperationEvents_OperatorId_OccurredAt",
                schema: "Konfeksiyon",
                table: "BundleOperationEvents",
                columns: new[] { "OperatorId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_BundleOperationEvents_RequestId_EventType",
                schema: "Konfeksiyon",
                table: "BundleOperationEvents",
                columns: new[] { "RequestId", "EventType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BundleOperationEvents_WorkSessionId",
                schema: "Konfeksiyon",
                table: "BundleOperationEvents",
                column: "WorkSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_BundleOperations_BundleId_RouteOperationId",
                schema: "Konfeksiyon",
                table: "BundleOperations",
                columns: new[] { "BundleId", "RouteOperationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BundleOperations_BundleId_SequenceNo",
                schema: "Konfeksiyon",
                table: "BundleOperations",
                columns: new[] { "BundleId", "SequenceNo" });

            migrationBuilder.CreateIndex(
                name: "IX_BundleOperations_MachineId_StartedAt",
                schema: "Konfeksiyon",
                table: "BundleOperations",
                columns: new[] { "MachineId", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_BundleOperations_OperationId_Status",
                schema: "Konfeksiyon",
                table: "BundleOperations",
                columns: new[] { "OperationId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_BundleOperations_OperatorId_StartedAt",
                schema: "Konfeksiyon",
                table: "BundleOperations",
                columns: new[] { "OperatorId", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_BundleOperations_RouteOperationId",
                schema: "Konfeksiyon",
                table: "BundleOperations",
                column: "RouteOperationId");

            migrationBuilder.CreateIndex(
                name: "UX_BundleOperations_InProgressPerBundle",
                schema: "Konfeksiyon",
                table: "BundleOperations",
                column: "BundleId",
                unique: true,
                filter: "[Status] = 'IN_PROGRESS'");

            migrationBuilder.CreateIndex(
                name: "UX_BundleOperations_InProgressPerMachine",
                schema: "Konfeksiyon",
                table: "BundleOperations",
                column: "MachineId",
                unique: true,
                filter: "[Status] = 'IN_PROGRESS' AND [MachineId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UX_BundleOperations_InProgressPerOperator",
                schema: "Konfeksiyon",
                table: "BundleOperations",
                column: "OperatorId",
                unique: true,
                filter: "[Status] = 'IN_PROGRESS' AND [OperatorId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Bundles_BundleNo",
                schema: "Konfeksiyon",
                table: "Bundles",
                column: "BundleNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Bundles_ColorId",
                schema: "Konfeksiyon",
                table: "Bundles",
                column: "ColorId");

            migrationBuilder.CreateIndex(
                name: "IX_Bundles_ProductionOrderId_ColorId_SizeId_Status",
                schema: "Konfeksiyon",
                table: "Bundles",
                columns: new[] { "ProductionOrderId", "ColorId", "SizeId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Bundles_ProductionRouteId",
                schema: "Konfeksiyon",
                table: "Bundles",
                column: "ProductionRouteId");

            migrationBuilder.CreateIndex(
                name: "IX_Bundles_SizeId",
                schema: "Konfeksiyon",
                table: "Bundles",
                column: "SizeId");

            migrationBuilder.CreateIndex(
                name: "IX_Bundles_Status",
                schema: "Konfeksiyon",
                table: "Bundles",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Colors_ColorCode",
                schema: "Konfeksiyon",
                table: "Colors",
                column: "ColorCode",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerAddresses_CustomerId",
                schema: "Konfeksiyon",
                table: "CustomerAddresses",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "UX_CustomerAddresses_DefaultDelivery",
                schema: "Konfeksiyon",
                table: "CustomerAddresses",
                column: "CustomerId",
                unique: true,
                filter: "[IsDefaultDelivery] = 1");

            migrationBuilder.CreateIndex(
                name: "UX_CustomerAddresses_DefaultInvoice",
                schema: "Konfeksiyon",
                table: "CustomerAddresses",
                column: "CustomerId",
                unique: true,
                filter: "[IsDefaultInvoice] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerBuyers_CustomerId_Code",
                schema: "Konfeksiyon",
                table: "CustomerBuyers",
                columns: new[] { "CustomerId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerDiscounts_CustomerId_ProductId_ValidFrom",
                schema: "Konfeksiyon",
                table: "CustomerDiscounts",
                columns: new[] { "CustomerId", "ProductId", "ValidFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerDiscounts_ProductId",
                schema: "Konfeksiyon",
                table: "CustomerDiscounts",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_CustomerCode",
                schema: "Konfeksiyon",
                table: "Customers",
                column: "CustomerCode",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_TaxNumber",
                schema: "Konfeksiyon",
                table: "Customers",
                column: "TaxNumber");

            migrationBuilder.CreateIndex(
                name: "IX_DefectTypes_Code",
                schema: "Konfeksiyon",
                table: "DefectTypes",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Factories_Code",
                schema: "Konfeksiyon",
                table: "Factories",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceiptLines_GoodsReceiptId",
                schema: "Konfeksiyon",
                table: "GoodsReceiptLines",
                column: "GoodsReceiptId");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceiptLines_PurchaseOrderItemId",
                schema: "Konfeksiyon",
                table: "GoodsReceiptLines",
                column: "PurchaseOrderItemId");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceipts_PurchaseOrderId_Id",
                schema: "Konfeksiyon",
                table: "GoodsReceipts",
                columns: new[] { "PurchaseOrderId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceipts_RequestId",
                schema: "Konfeksiyon",
                table: "GoodsReceipts",
                column: "RequestId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceipts_WarehouseId",
                schema: "Konfeksiyon",
                table: "GoodsReceipts",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_LineMachineAssignments_MachineId_AssignedAt",
                schema: "Konfeksiyon",
                table: "LineMachineAssignments",
                columns: new[] { "MachineId", "AssignedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_LineMachineAssignments_ProductionLineId_UnassignedAt_Position",
                schema: "Konfeksiyon",
                table: "LineMachineAssignments",
                columns: new[] { "ProductionLineId", "UnassignedAt", "Position" });

            migrationBuilder.CreateIndex(
                name: "UX_LineMachineAssignments_CurrentPerMachine",
                schema: "Konfeksiyon",
                table: "LineMachineAssignments",
                column: "MachineId",
                unique: true,
                filter: "[UnassignedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_LineProductionPlans_ProductionLineId_PlannedStart_PlannedEnd",
                schema: "Konfeksiyon",
                table: "LineProductionPlans",
                columns: new[] { "ProductionLineId", "PlannedStart", "PlannedEnd" });

            migrationBuilder.CreateIndex(
                name: "IX_LineProductionPlans_ProductionOrderId_Status",
                schema: "Konfeksiyon",
                table: "LineProductionPlans",
                columns: new[] { "ProductionOrderId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_MachineOperations_OperationId",
                schema: "Konfeksiyon",
                table: "MachineOperations",
                column: "OperationId");

            migrationBuilder.CreateIndex(
                name: "IX_Machines_MachineCode",
                schema: "Konfeksiyon",
                table: "Machines",
                column: "MachineCode",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Machines_MachineTypeId",
                schema: "Konfeksiyon",
                table: "Machines",
                column: "MachineTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Machines_OperationId",
                schema: "Konfeksiyon",
                table: "Machines",
                column: "OperationId");

            migrationBuilder.CreateIndex(
                name: "IX_Machines_Status",
                schema: "Konfeksiyon",
                table: "Machines",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Machines_SupplierId",
                schema: "Konfeksiyon",
                table: "Machines",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_MachineStatusLogs_MachineId_StartedAt",
                schema: "Konfeksiyon",
                table: "MachineStatusLogs",
                columns: new[] { "MachineId", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_MachineStatusLogs_Status_StartedAt",
                schema: "Konfeksiyon",
                table: "MachineStatusLogs",
                columns: new[] { "Status", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "UX_MachineStatusLogs_OpenPerMachine",
                schema: "Konfeksiyon",
                table: "MachineStatusLogs",
                column: "MachineId",
                unique: true,
                filter: "[EndedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_MachineTypes_Code",
                schema: "Konfeksiyon",
                table: "MachineTypes",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Models_CustomerId",
                schema: "Konfeksiyon",
                table: "Models",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Models_ModelCode",
                schema: "Konfeksiyon",
                table: "Models",
                column: "ModelCode",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_OperationDependencies_PredecessorId",
                schema: "Konfeksiyon",
                table: "OperationDependencies",
                column: "PredecessorId");

            migrationBuilder.CreateIndex(
                name: "IX_Operations_MachineTypeId",
                schema: "Konfeksiyon",
                table: "Operations",
                column: "MachineTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Operations_OperationCode",
                schema: "Konfeksiyon",
                table: "Operations",
                column: "OperationCode",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_OperatorCards_OperatorId_AssignedAt",
                schema: "Konfeksiyon",
                table: "OperatorCards",
                columns: new[] { "OperatorId", "AssignedAt" });

            migrationBuilder.CreateIndex(
                name: "UX_OperatorCards_CurrentPerCard",
                schema: "Konfeksiyon",
                table: "OperatorCards",
                column: "RfidCardId",
                unique: true,
                filter: "[RevokedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "UX_OperatorCards_CurrentPerOperator",
                schema: "Konfeksiyon",
                table: "OperatorCards",
                column: "OperatorId",
                unique: true,
                filter: "[RevokedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_OperatorMachines_MachineId",
                schema: "Konfeksiyon",
                table: "OperatorMachines",
                column: "MachineId");

            migrationBuilder.CreateIndex(
                name: "IX_OperatorOperationQualifications_OperationId",
                schema: "Konfeksiyon",
                table: "OperatorOperationQualifications",
                column: "OperationId");

            migrationBuilder.CreateIndex(
                name: "IX_Operators_BreakGroupId",
                schema: "Konfeksiyon",
                table: "Operators",
                column: "BreakGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_Operators_OperatorCode",
                schema: "Konfeksiyon",
                table: "Operators",
                column: "OperatorCode",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollBonusTiers_PayrollSettingId_MinEfficiencyPercent",
                schema: "Konfeksiyon",
                table: "PayrollBonusTiers",
                columns: new[] { "PayrollSettingId", "MinEfficiencyPercent" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PriceListItems_ColorId",
                schema: "Konfeksiyon",
                table: "PriceListItems",
                column: "ColorId");

            migrationBuilder.CreateIndex(
                name: "IX_PriceListItems_PriceListId_ProductId_ColorId_SizeId",
                schema: "Konfeksiyon",
                table: "PriceListItems",
                columns: new[] { "PriceListId", "ProductId", "ColorId", "SizeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PriceListItems_ProductId",
                schema: "Konfeksiyon",
                table: "PriceListItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_PriceListItems_SizeId",
                schema: "Konfeksiyon",
                table: "PriceListItems",
                column: "SizeId");

            migrationBuilder.CreateIndex(
                name: "IX_PriceLists_Code",
                schema: "Konfeksiyon",
                table: "PriceLists",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_PriceLists_CustomerId_ValidFrom",
                schema: "Konfeksiyon",
                table: "PriceLists",
                columns: new[] { "CustomerId", "ValidFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductGroups_Code",
                schema: "Konfeksiyon",
                table: "ProductGroups",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionAreas_Code",
                schema: "Konfeksiyon",
                table: "ProductionAreas",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionAreas_FactoryId",
                schema: "Konfeksiyon",
                table: "ProductionAreas",
                column: "FactoryId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionLines_LineCode",
                schema: "Konfeksiyon",
                table: "ProductionLines",
                column: "LineCode",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionLines_ProductionAreaId",
                schema: "Konfeksiyon",
                table: "ProductionLines",
                column: "ProductionAreaId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrderItems_ColorId",
                schema: "Konfeksiyon",
                table: "ProductionOrderItems",
                column: "ColorId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrderItems_ProductionOrderId_ColorId_SizeId",
                schema: "Konfeksiyon",
                table: "ProductionOrderItems",
                columns: new[] { "ProductionOrderId", "ColorId", "SizeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrderItems_SizeId",
                schema: "Konfeksiyon",
                table: "ProductionOrderItems",
                column: "SizeId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrders_CustomerId",
                schema: "Konfeksiyon",
                table: "ProductionOrders",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrders_DueDate",
                schema: "Konfeksiyon",
                table: "ProductionOrders",
                column: "DueDate");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrders_MaterialWarehouseId",
                schema: "Konfeksiyon",
                table: "ProductionOrders",
                column: "MaterialWarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrders_OrderNo",
                schema: "Konfeksiyon",
                table: "ProductionOrders",
                column: "OrderNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrders_ProductId",
                schema: "Konfeksiyon",
                table: "ProductionOrders",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrders_SalesOrderId",
                schema: "Konfeksiyon",
                table: "ProductionOrders",
                column: "SalesOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrders_Status_DueDate",
                schema: "Konfeksiyon",
                table: "ProductionOrders",
                columns: new[] { "Status", "DueDate" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductionRouteOperations_OperationId",
                schema: "Konfeksiyon",
                table: "ProductionRouteOperations",
                column: "OperationId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionRouteOperations_ProductionRouteId_SequenceNo",
                schema: "Konfeksiyon",
                table: "ProductionRouteOperations",
                columns: new[] { "ProductionRouteId", "SequenceNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductionRoutes_ModelId_Version",
                schema: "Konfeksiyon",
                table: "ProductionRoutes",
                columns: new[] { "ModelId", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_ProductionRoutes_ActivePerModel",
                schema: "Konfeksiyon",
                table: "ProductionRoutes",
                column: "ModelId",
                unique: true,
                filter: "[Status] = 'ACTIVE' AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ProductMaterials_ColorId",
                schema: "Konfeksiyon",
                table: "ProductMaterials",
                column: "ColorId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductMaterials_ProductId_StockItemId",
                schema: "Konfeksiyon",
                table: "ProductMaterials",
                columns: new[] { "ProductId", "StockItemId" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductMaterials_SizeId",
                schema: "Konfeksiyon",
                table: "ProductMaterials",
                column: "SizeId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductMaterials_StockItemId",
                schema: "Konfeksiyon",
                table: "ProductMaterials",
                column: "StockItemId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_ModelId",
                schema: "Konfeksiyon",
                table: "Products",
                column: "ModelId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_ProductCode",
                schema: "Konfeksiyon",
                table: "Products",
                column: "ProductCode",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Products_ProductGroupId",
                schema: "Konfeksiyon",
                table: "Products",
                column: "ProductGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_Season",
                schema: "Konfeksiyon",
                table: "Products",
                column: "Season");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderItems_ProductionOrderId",
                schema: "Konfeksiyon",
                table: "PurchaseOrderItems",
                column: "ProductionOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderItems_PurchaseOrderId_StockItemId",
                schema: "Konfeksiyon",
                table: "PurchaseOrderItems",
                columns: new[] { "PurchaseOrderId", "StockItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderItems_StockItemId",
                schema: "Konfeksiyon",
                table: "PurchaseOrderItems",
                column: "StockItemId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_OrderNo",
                schema: "Konfeksiyon",
                table: "PurchaseOrders",
                column: "OrderNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_Status_ExpectedDate",
                schema: "Konfeksiyon",
                table: "PurchaseOrders",
                columns: new[] { "Status", "ExpectedDate" });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_SupplierId",
                schema: "Konfeksiyon",
                table: "PurchaseOrders",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_QualityInspections_BundleId_Id",
                schema: "Konfeksiyon",
                table: "QualityInspections",
                columns: new[] { "BundleId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_QualityInspections_BundleOperationId",
                schema: "Konfeksiyon",
                table: "QualityInspections",
                column: "BundleOperationId");

            migrationBuilder.CreateIndex(
                name: "IX_QualityInspections_InspectedAt_Result",
                schema: "Konfeksiyon",
                table: "QualityInspections",
                columns: new[] { "InspectedAt", "Result" });

            migrationBuilder.CreateIndex(
                name: "IX_QualityRecords_DefectTypeId",
                schema: "Konfeksiyon",
                table: "QualityRecords",
                column: "DefectTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_QualityRecords_QualityInspectionId",
                schema: "Konfeksiyon",
                table: "QualityRecords",
                column: "QualityInspectionId");

            migrationBuilder.CreateIndex(
                name: "IX_ReworkTasks_BundleId_Id",
                schema: "Konfeksiyon",
                table: "ReworkTasks",
                columns: new[] { "BundleId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_ReworkTasks_BundleOperationId_Status",
                schema: "Konfeksiyon",
                table: "ReworkTasks",
                columns: new[] { "BundleOperationId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ReworkTasks_QualityInspectionId",
                schema: "Konfeksiyon",
                table: "ReworkTasks",
                column: "QualityInspectionId");

            migrationBuilder.CreateIndex(
                name: "IX_ReworkTasks_Status_Id",
                schema: "Konfeksiyon",
                table: "ReworkTasks",
                columns: new[] { "Status", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_RfidCards_Uid",
                schema: "Konfeksiyon",
                table: "RfidCards",
                column: "Uid",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_RfidReadLogs_ReadAt",
                schema: "Konfeksiyon",
                table: "RfidReadLogs",
                column: "ReadAt");

            migrationBuilder.CreateIndex(
                name: "IX_RfidReadLogs_ReadId",
                schema: "Konfeksiyon",
                table: "RfidReadLogs",
                column: "ReadId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RfidReadLogs_TabletId_Uid_Kind_ReadAt",
                schema: "Konfeksiyon",
                table: "RfidReadLogs",
                columns: new[] { "TabletId", "Uid", "Kind", "ReadAt" });

            migrationBuilder.CreateIndex(
                name: "IX_RfidReadLogs_Uid_ReadAt",
                schema: "Konfeksiyon",
                table: "RfidReadLogs",
                columns: new[] { "Uid", "ReadAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrderApprovalEvents_SalesOrderId_CreatedAt",
                schema: "Konfeksiyon",
                table: "SalesOrderApprovalEvents",
                columns: new[] { "SalesOrderId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrderApprovalSteps_SalesOrderId_Round_Sequence",
                schema: "Konfeksiyon",
                table: "SalesOrderApprovalSteps",
                columns: new[] { "SalesOrderId", "Round", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrderApprovalSteps_Status",
                schema: "Konfeksiyon",
                table: "SalesOrderApprovalSteps",
                column: "Status",
                filter: "[Status] = 'PENDING'");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrderLines_ColorId",
                schema: "Konfeksiyon",
                table: "SalesOrderLines",
                column: "ColorId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrderLines_ProductId",
                schema: "Konfeksiyon",
                table: "SalesOrderLines",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrderLines_SalesOrderId_ProductId_ColorId_SizeId",
                schema: "Konfeksiyon",
                table: "SalesOrderLines",
                columns: new[] { "SalesOrderId", "ProductId", "ColorId", "SizeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrderLines_SizeId",
                schema: "Konfeksiyon",
                table: "SalesOrderLines",
                column: "SizeId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrderReservations_SalesOrderId",
                schema: "Konfeksiyon",
                table: "SalesOrderReservations",
                column: "SalesOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrderReservations_SalesOrderLineId_WarehouseId",
                schema: "Konfeksiyon",
                table: "SalesOrderReservations",
                columns: new[] { "SalesOrderLineId", "WarehouseId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrderReservations_StockItemId",
                schema: "Konfeksiyon",
                table: "SalesOrderReservations",
                column: "StockItemId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrderReservations_WarehouseId_StockItemId",
                schema: "Konfeksiyon",
                table: "SalesOrderReservations",
                columns: new[] { "WarehouseId", "StockItemId" });

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_CustomerBuyerId",
                schema: "Konfeksiyon",
                table: "SalesOrders",
                column: "CustomerBuyerId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_CustomerId",
                schema: "Konfeksiyon",
                table: "SalesOrders",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_DeliveryAddressId",
                schema: "Konfeksiyon",
                table: "SalesOrders",
                column: "DeliveryAddressId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_OrderNo",
                schema: "Konfeksiyon",
                table: "SalesOrders",
                column: "OrderNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_Status_DueDate",
                schema: "Konfeksiyon",
                table: "SalesOrders",
                columns: new[] { "Status", "DueDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Shifts_Code",
                schema: "Konfeksiyon",
                table: "Shifts",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentItems_ShipmentId_StockItemId",
                schema: "Konfeksiyon",
                table: "ShipmentItems",
                columns: new[] { "ShipmentId", "StockItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentItems_StockItemId",
                schema: "Konfeksiyon",
                table: "ShipmentItems",
                column: "StockItemId");

            migrationBuilder.CreateIndex(
                name: "IX_Shipments_CustomerId",
                schema: "Konfeksiyon",
                table: "Shipments",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Shipments_ProductionOrderId_Status",
                schema: "Konfeksiyon",
                table: "Shipments",
                columns: new[] { "ProductionOrderId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Shipments_SalesOrderId_Status",
                schema: "Konfeksiyon",
                table: "Shipments",
                columns: new[] { "SalesOrderId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Shipments_ShipmentNo",
                schema: "Konfeksiyon",
                table: "Shipments",
                column: "ShipmentNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Shipments_ShippedAt",
                schema: "Konfeksiyon",
                table: "Shipments",
                column: "ShippedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Shipments_Status_ShipDate",
                schema: "Konfeksiyon",
                table: "Shipments",
                columns: new[] { "Status", "ShipDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Shipments_WarehouseId",
                schema: "Konfeksiyon",
                table: "Shipments",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_Sizes_SizeCode",
                schema: "Konfeksiyon",
                table: "Sizes",
                column: "SizeCode",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_StockCountLines_LocationId",
                schema: "Konfeksiyon",
                table: "StockCountLines",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_StockCountLines_LotId",
                schema: "Konfeksiyon",
                table: "StockCountLines",
                column: "LotId");

            migrationBuilder.CreateIndex(
                name: "IX_StockCountLines_StockCountId_LocationId_StockItemId_LotId",
                schema: "Konfeksiyon",
                table: "StockCountLines",
                columns: new[] { "StockCountId", "LocationId", "StockItemId", "LotId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockCountLines_StockItemId",
                schema: "Konfeksiyon",
                table: "StockCountLines",
                column: "StockItemId");

            migrationBuilder.CreateIndex(
                name: "IX_StockCounts_CountNo",
                schema: "Konfeksiyon",
                table: "StockCounts",
                column: "CountNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockCounts_LocationId",
                schema: "Konfeksiyon",
                table: "StockCounts",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_StockCounts_WarehouseId_Status",
                schema: "Konfeksiyon",
                table: "StockCounts",
                columns: new[] { "WarehouseId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_StockItems_Code",
                schema: "Konfeksiyon",
                table: "StockItems",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_StockItems_ColorId",
                schema: "Konfeksiyon",
                table: "StockItems",
                column: "ColorId");

            migrationBuilder.CreateIndex(
                name: "IX_StockItems_ItemType",
                schema: "Konfeksiyon",
                table: "StockItems",
                column: "ItemType");

            migrationBuilder.CreateIndex(
                name: "IX_StockItems_SizeId",
                schema: "Konfeksiyon",
                table: "StockItems",
                column: "SizeId");

            migrationBuilder.CreateIndex(
                name: "IX_StockItems_SupplierId",
                schema: "Konfeksiyon",
                table: "StockItems",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "UX_StockItems_FinishedGood",
                schema: "Konfeksiyon",
                table: "StockItems",
                columns: new[] { "ProductId", "ColorId", "SizeId" },
                unique: true,
                filter: "[ProductId] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_StockLots_BatchNo",
                schema: "Konfeksiyon",
                table: "StockLots",
                column: "BatchNo");

            migrationBuilder.CreateIndex(
                name: "IX_StockLots_LotNo",
                schema: "Konfeksiyon",
                table: "StockLots",
                column: "LotNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockLots_PurchaseOrderId",
                schema: "Konfeksiyon",
                table: "StockLots",
                column: "PurchaseOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_StockLots_StockItemId_Id",
                schema: "Konfeksiyon",
                table: "StockLots",
                columns: new[] { "StockItemId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_StockReservations_ProductionOrderId_WarehouseId_StockItemId",
                schema: "Konfeksiyon",
                table: "StockReservations",
                columns: new[] { "ProductionOrderId", "WarehouseId", "StockItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockReservations_StockItemId",
                schema: "Konfeksiyon",
                table: "StockReservations",
                column: "StockItemId");

            migrationBuilder.CreateIndex(
                name: "IX_StockReservations_WarehouseId_StockItemId",
                schema: "Konfeksiyon",
                table: "StockReservations",
                columns: new[] { "WarehouseId", "StockItemId" });

            migrationBuilder.CreateIndex(
                name: "IX_Stocks_LocationId_StockItemId_LotId",
                schema: "Konfeksiyon",
                table: "Stocks",
                columns: new[] { "LocationId", "StockItemId", "LotId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Stocks_LotId",
                schema: "Konfeksiyon",
                table: "Stocks",
                column: "LotId");

            migrationBuilder.CreateIndex(
                name: "IX_Stocks_StockItemId",
                schema: "Konfeksiyon",
                table: "Stocks",
                column: "StockItemId");

            migrationBuilder.CreateIndex(
                name: "IX_Stocks_WarehouseId_StockItemId",
                schema: "Konfeksiyon",
                table: "Stocks",
                columns: new[] { "WarehouseId", "StockItemId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockTransactions_BundleId_Type",
                schema: "Konfeksiyon",
                table: "StockTransactions",
                columns: new[] { "BundleId", "Type" });

            migrationBuilder.CreateIndex(
                name: "IX_StockTransactions_CounterLocationId",
                schema: "Konfeksiyon",
                table: "StockTransactions",
                column: "CounterLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransactions_CounterWarehouseId",
                schema: "Konfeksiyon",
                table: "StockTransactions",
                column: "CounterWarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransactions_LocationId_Id",
                schema: "Konfeksiyon",
                table: "StockTransactions",
                columns: new[] { "LocationId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_StockTransactions_LotId_Id",
                schema: "Konfeksiyon",
                table: "StockTransactions",
                columns: new[] { "LotId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_StockTransactions_OccurredAt_Type",
                schema: "Konfeksiyon",
                table: "StockTransactions",
                columns: new[] { "OccurredAt", "Type" });

            migrationBuilder.CreateIndex(
                name: "IX_StockTransactions_ProductionOrderId",
                schema: "Konfeksiyon",
                table: "StockTransactions",
                column: "ProductionOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransactions_PurchaseOrderId",
                schema: "Konfeksiyon",
                table: "StockTransactions",
                column: "PurchaseOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransactions_RequestId_Type",
                schema: "Konfeksiyon",
                table: "StockTransactions",
                columns: new[] { "RequestId", "Type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockTransactions_ShipmentId",
                schema: "Konfeksiyon",
                table: "StockTransactions",
                column: "ShipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransactions_StockItemId_WarehouseId_Id",
                schema: "Konfeksiyon",
                table: "StockTransactions",
                columns: new[] { "StockItemId", "WarehouseId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_StockTransactions_WarehouseId_Id",
                schema: "Konfeksiyon",
                table: "StockTransactions",
                columns: new[] { "WarehouseId", "Id" });

            migrationBuilder.CreateIndex(
                name: "UX_StockTransactions_ReceiptPerBundle",
                schema: "Konfeksiyon",
                table: "StockTransactions",
                column: "BundleId",
                unique: true,
                filter: "[BundleId] IS NOT NULL AND [Type] = 'PRODUCTION_RECEIPT'");

            migrationBuilder.CreateIndex(
                name: "IX_StopReasons_Category_SortOrder",
                schema: "Konfeksiyon",
                table: "StopReasons",
                columns: new[] { "Category", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_StopReasons_Code",
                schema: "Konfeksiyon",
                table: "StopReasons",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_SupplierCode",
                schema: "Konfeksiyon",
                table: "Suppliers",
                column: "SupplierCode",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Tablets_TabletCode",
                schema: "Konfeksiyon",
                table: "Tablets",
                column: "TabletCode",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UX_Tablets_DeviceIdentifier",
                schema: "Konfeksiyon",
                table: "Tablets",
                column: "DeviceIdentifier",
                unique: true,
                filter: "[DeviceIdentifier] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UX_Tablets_Machine",
                schema: "Konfeksiyon",
                table: "Tablets",
                column: "MachineId",
                unique: true,
                filter: "[MachineId] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseLocations_WarehouseId_Code",
                schema: "Konfeksiyon",
                table: "WarehouseLocations",
                columns: new[] { "WarehouseId", "Code" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UX_WarehouseLocations_Default",
                schema: "Konfeksiyon",
                table: "WarehouseLocations",
                column: "WarehouseId",
                unique: true,
                filter: "[IsDefault] = 1 AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Warehouses_Code",
                schema: "Konfeksiyon",
                table: "Warehouses",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_WorkSessionPauses_BreakScheduleId_StartedAt",
                schema: "Konfeksiyon",
                table: "WorkSessionPauses",
                columns: new[] { "BreakScheduleId", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkSessionPauses_StopReasonId",
                schema: "Konfeksiyon",
                table: "WorkSessionPauses",
                column: "StopReasonId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkSessionPauses_Type_StartedAt",
                schema: "Konfeksiyon",
                table: "WorkSessionPauses",
                columns: new[] { "Type", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkSessionPauses_WorkSessionId_StartedAt",
                schema: "Konfeksiyon",
                table: "WorkSessionPauses",
                columns: new[] { "WorkSessionId", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "UX_WorkSessionPauses_OpenPerSession",
                schema: "Konfeksiyon",
                table: "WorkSessionPauses",
                column: "WorkSessionId",
                unique: true,
                filter: "[EndedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_WorkSessions_LoginAt",
                schema: "Konfeksiyon",
                table: "WorkSessions",
                column: "LoginAt");

            migrationBuilder.CreateIndex(
                name: "IX_WorkSessions_MachineId_LoginAt",
                schema: "Konfeksiyon",
                table: "WorkSessions",
                columns: new[] { "MachineId", "LoginAt" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkSessions_OperatorId_LoginAt",
                schema: "Konfeksiyon",
                table: "WorkSessions",
                columns: new[] { "OperatorId", "LoginAt" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkSessions_TabletId",
                schema: "Konfeksiyon",
                table: "WorkSessions",
                column: "TabletId");

            migrationBuilder.CreateIndex(
                name: "UX_WorkSessions_OpenPerMachine",
                schema: "Konfeksiyon",
                table: "WorkSessions",
                column: "MachineId",
                unique: true,
                filter: "[LogoutAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "UX_WorkSessions_OpenPerOperator",
                schema: "Konfeksiyon",
                table: "WorkSessions",
                column: "OperatorId",
                unique: true,
                filter: "[LogoutAt] IS NULL");

            // Modülün yetkileri ve sistem rollerine başlangıç atamaları (KonfeksiyonPermissions). Kayıt varsa dokunulmaz:
            // mevcut kurulumda rol yetkileri veritabanından yönetilir.
            migrationBuilder.Sql(@"SET IDENTITY_INSERT [Core].[Permissions] ON;
EXEC(N'INSERT INTO [Core].[Permissions] ([Id], [Code], [Module], [Description])
SELECT v.[Id], v.[Code], v.[Module], v.[Description] FROM (VALUES
(7, N''ProductionOrder.View'', N''ProductionOrder'', N''Üretim siparişlerini görüntüleme''),
(8, N''ProductionOrder.Create'', N''ProductionOrder'', N''Üretim siparişi oluşturma''),
(9, N''ProductionOrder.Update'', N''ProductionOrder'', N''Üretim siparişi güncelleme''),
(10, N''ProductionLine.View'', N''ProductionLine'', N''Bantları görüntüleme''),
(11, N''ProductionLine.Update'', N''ProductionLine'', N''Bant ve bant planı düzenleme''),
(12, N''Bundle.View'', N''Bundle'', N''Demetleri görüntüleme''),
(13, N''Bundle.Create'', N''Bundle'', N''Demet oluşturma''),
(14, N''Bundle.Start'', N''Bundle'', N''Demet operasyonu başlatma (kullanılmıyor: tablet yetkileri saha rollerinden yönetilir)''),
(15, N''Bundle.Complete'', N''Bundle'', N''Demet operasyonu tamamlama (kullanılmıyor: tablet yetkileri saha rollerinden yönetilir)''),
(16, N''Production.View'', N''Production'', N''Üretim hareketlerini görüntüleme''),
(17, N''Production.Report'', N''Production'', N''Üretim miktarı bildirme (kullanılmıyor: tablet yetkileri saha rollerinden yönetilir)''),
(18, N''Quality.View'', N''Quality'', N''Kalite kayıtlarını görüntüleme''),
(19, N''Quality.Create'', N''Quality'', N''Kalite kaydı oluşturma''),
(20, N''Machine.View'', N''Machine'', N''Makineleri görüntüleme''),
(21, N''Machine.Update'', N''Machine'', N''Makine güncelleme / durum değiştirme''),
(22, N''Operator.View'', N''Operator'', N''Operatörleri görüntüleme''),
(23, N''Operator.Update'', N''Operator'', N''Operatör güncelleme, oturum kapatma, süren işi bırakma''),
(24, N''Stock.View'', N''Stock'', N''Stok ve depo görüntüleme''),
(25, N''Stock.Manage'', N''Stock'', N''Stok hareketi oluşturma''),
(26, N''Dashboard.View'', N''Dashboard'', N''Canlı üretim ekranlarını görüntüleme''),
(27, N''Report.View'', N''Report'', N''Raporları görüntüleme''),
(29, N''ProductionRoute.View'', N''ProductionRoute'', N''Operasyon rotalarını görüntüleme''),
(30, N''ProductionRoute.Manage'', N''ProductionRoute'', N''Operasyon rotası oluşturma / etkinleştirme''),
(31, N''Tablet.View'', N''Tablet'', N''Tabletleri görüntüleme''),
(32, N''Tablet.Manage'', N''Tablet'', N''Tablet tanımlama / cihaz eşleştirme''),
(33, N''RfidCard.View'', N''RfidCard'', N''RFID kartlarını görüntüleme''),
(34, N''RfidCard.Manage'', N''RfidCard'', N''RFID kartı tanımlama''),
(35, N''Purchase.View'', N''Purchase'', N''Tedarikçi ve satın alma siparişlerini görüntüleme''),
(36, N''Purchase.Manage'', N''Purchase'', N''Tedarikçi ve satın alma siparişi oluşturma / onaylama''),
(37, N''Shipment.View'', N''Shipment'', N''Sevkiyatları görüntüleme''),
(38, N''Shipment.Manage'', N''Shipment'', N''Sevkiyat oluşturma / sevk etme''),
(41, N''Floor.Pause'', N''Floor'', N''Tablette kendi kartıyla ara verme / arayı onaylama''),
(42, N''Floor.ReportBreakdown'', N''Floor'', N''Tablette kendi kartıyla arıza bildirme''),
(43, N''Floor.ResolveBreakdown'', N''Floor'', N''Tablette kartını okutarak arızayı kapatma''),
(44, N''Payroll.View'', N''Payroll'', N''Parça başı ücret ve prim kazançlarını görüntüleme''),
(45, N''Payroll.Manage'', N''Payroll'', N''Ücret ve prim ayarlarını düzenleme''),
(46, N''Costing.View'', N''Costing'', N''Malzeme ve işçilik maliyetlerini görüntüleme''),
(47, N''SalesOrder.View'', N''SalesOrder'', N''Müşteri siparişlerini görüntüleme''),
(48, N''SalesOrder.Manage'', N''SalesOrder'', N''Müşteri siparişi oluşturma / onaya gönderme''),
(49, N''SalesOrder.Approve'', N''SalesOrder'', N''Müşteri siparişini onaylama / reddetme''),
(50, N''ProductionOrder.Approve'', N''ProductionOrder'', N''Üretim siparişini onaylayıp üretime açma''),
(51, N''Purchase.Approve'', N''Purchase'', N''Satın alma siparişini onaylama''),
(52, N''Report.Warehouse'', N''Report'', N''Yalnızca depo raporlarını görüntüleme''),
(53, N''SalesOrder.Close'', N''SalesOrder'', N''Müşteri siparişini elle kapatma (kalan sevk edilmeyecekse)''),
(56, N''Production.ManualReport'', N''Production'', N''Tabletsiz / kartsız elle üretim bildirimi (askıda kalan iş, arızalı tablet)''),
(57, N''Bundle.ReleaseCard'', N''Bundle'', N''Kart boşaltma: okutulan demet kartını demetten ayırma''),
(58, N''ProductionOrder.Cancel'', N''ProductionOrder'', N''Üretim siparişini iptal etme (üretimdeki dahil)''),
(59, N''ProductionOrder.Complete'', N''ProductionOrder'', N''Üretim siparişini tamamlandı olarak kapatma''),
(60, N''SalesOrder.Cancel'', N''SalesOrder'', N''Müşteri siparişini iptal etme (onaylı dahil)''),
(61, N''ProductionRoute.Activate'', N''ProductionRoute'', N''Operasyon rotasını etkinleştirme (SAM''''i devreye alma)''),
(62, N''Stock.Adjust'', N''Stock'', N''Elle stok düzeltme ve sayım fişini kesinleştirme''),
(63, N''Costing.Manage'', N''Costing'', N''Stok kartı birim maliyetini değiştirme''),
(64, N''Operator.AssignFloorRole'', N''Operator'', N''Operatörün saha görevini ve ara / arıza haklarını değiştirme''),
(65, N''Operator.AssignCard'', N''Operator'', N''Personel kartını operatöre atama / geri alma''),
(66, N''Production.ReleaseJob'', N''Production'', N''Süren işi ofisten adet bildirmeden bırakma''),
(67, N''WorkSession.Close'', N''WorkSession'', N''Çalışma oturumunu ofisten kapatma''),
(68, N''Shipment.Ship'', N''Shipment'', N''Sevkiyatı kesinleştirme (stoktan düşer)''),
(71, N''Purchase.Close'', N''Purchase'', N''Satın alma siparişinin kalanını kapatma''),
(72, N''Purchase.Cancel'', N''Purchase'', N''Satın alma siparişini iptal etme''),
(73, N''Quality.ReleaseHold'', N''Quality'', N''Kalite bekletmesini kaldırma''),
(74, N''Quality.Reject'', N''Quality'', N''Kalite kontrolünde parçayı hatalı (hurda) ayırma''),
(75, N''Machine.Manage'', N''Machine'', N''Makine tanımı oluşturma / düzenleme / silme''),
(76, N''Machine.ResolveBreakdown'', N''Machine'', N''Arızalı makineyi ofisten kullanıma açma''),
(77, N''SalesOrder.OverridePrice'', N''SalesOrder'', N''Müşteri siparişinde fiyat listesinin altında fiyat girme''),
(78, N''Stock.Reserve'', N''Stock'', N''Stok ayırmayı elle değiştirme / bırakma''),
(79, N''Report.Personnel'', N''Report'', N''Operatör performansı ve çalışma süresi raporları''),
(80, N''Report.Financial'', N''Report'', N''Stok değeri ve tutar içeren raporlar / sütunlar''),
(81, N''Report.Export'', N''Report'', N''Raporu dosya olarak dışa aktarma''),
(82, N''Purchase.ViewPrices'', N''Purchase'', N''Satın alma fiyatlarını ve tutarlarını görme''),
(83, N''SalesOrder.ViewPrices'', N''SalesOrder'', N''Satış fiyatlarını ve tutarlarını görme''),
(84, N''ApprovalFlow.Manage'', N''ApprovalFlow'', N''Onay akışlarını düzenleme''),
(85, N''Bundle.Cancel'', N''Bundle'', N''Demeti iptal etme''),
(86, N''LinePlan.Manage'', N''LinePlan'', N''Bant planı oluşturma / değiştirme / iptal etme''),
(88, N''Stock.EditLot'', N''Stock'', N''Top / parti bilgisini düzeltme''),
(89, N''Warehouse.Manage'', N''Warehouse'', N''Depo ve depo adresi tanımlama''),
(90, N''Quality.ManageDefectTypes'', N''Quality'', N''Hata türü tanımlama''),
(91, N''Operator.ViewPersonal'', N''Operator'', N''Operatör fotoğrafını görme''),
(92, N''Floor.ReportReject'', N''Floor'', N''Tablette hatalı adet bildirme'')
) v([Id], [Code], [Module], [Description])
WHERE NOT EXISTS (SELECT 1 FROM [Core].[Permissions] p WHERE p.[Id] = v.[Id] OR p.[Code] = v.[Code])');
SET IDENTITY_INSERT [Core].[Permissions] OFF;");

            migrationBuilder.Sql(@"INSERT INTO [Core].[RolePermissions] ([RoleId], [PermissionId])
SELECT v.[RoleId], v.[PermissionId] FROM (VALUES
(1, 7), (1, 8), (1, 9), (1, 10), (1, 11), (1, 12), (1, 13), (1, 14), (1, 15), (1, 16), (1, 17), (1, 18),
(1, 19), (1, 20), (1, 21), (1, 22), (1, 23), (1, 24), (1, 25), (1, 26), (1, 27), (1, 29), (1, 30), (1, 31),
(1, 32), (1, 33), (1, 34), (1, 35), (1, 36), (1, 37), (1, 38), (1, 41), (1, 42), (1, 43), (1, 44), (1, 45),
(1, 46), (1, 47), (1, 48), (1, 49), (1, 50), (1, 51), (1, 52), (1, 53), (1, 56), (1, 57), (1, 58), (1, 59),
(1, 60), (1, 61), (1, 62), (1, 63), (1, 64), (1, 65), (1, 66), (1, 67), (1, 68), (1, 71), (1, 72), (1, 73),
(1, 74), (1, 75), (1, 76), (1, 77), (1, 78), (1, 79), (1, 80), (1, 81), (1, 82), (1, 83), (1, 84), (1, 85),
(1, 86), (1, 88), (1, 89), (1, 90), (1, 91), (1, 92), (2, 26), (2, 27), (2, 7), (2, 8), (2, 9), (2, 10),
(2, 11), (2, 12), (2, 13), (2, 16), (2, 20), (2, 21), (2, 22), (2, 23), (2, 18), (2, 29), (2, 30), (2, 31),
(2, 32), (2, 33), (2, 34), (2, 35), (2, 37), (2, 44), (2, 46), (2, 50), (2, 47), (2, 48), (2, 49), (2, 56),
(2, 57), (2, 58), (2, 59), (2, 60), (2, 61), (2, 64), (2, 65), (2, 91), (2, 66), (2, 67), (2, 75), (2, 76),
(2, 77), (2, 78), (2, 79), (2, 80), (2, 81), (2, 82), (2, 83), (2, 85), (2, 86), (2, 90), (3, 26), (3, 27),
(3, 7), (3, 8), (3, 9), (3, 10), (3, 11), (3, 12), (3, 13), (3, 16), (3, 20), (3, 22), (3, 29), (3, 30),
(3, 33), (3, 34), (3, 35), (3, 36), (3, 37), (3, 50), (3, 51), (3, 47), (3, 48), (3, 56), (3, 57), (3, 58),
(3, 59), (3, 60), (3, 61), (3, 91), (3, 77), (3, 78), (3, 79), (3, 80), (3, 81), (3, 82), (3, 83), (3, 85),
(3, 86), (3, 71), (3, 72), (4, 26), (4, 7), (4, 10), (4, 12), (4, 16), (4, 20), (4, 21), (4, 22), (4, 29),
(4, 31), (4, 33), (4, 41), (4, 42), (4, 43), (4, 56), (4, 57), (4, 75), (4, 76), (4, 91), (4, 92), (5, 18),
(5, 19), (5, 12), (5, 16), (5, 27), (5, 73), (5, 74), (5, 79), (5, 80), (5, 81), (6, 24), (6, 25), (6, 12),
(6, 35), (6, 37), (6, 38), (6, 52), (6, 62), (6, 63), (6, 68), (6, 88), (6, 89), (6, 80), (6, 81), (6, 82),
(7, 20), (7, 21), (7, 31), (7, 42), (7, 43), (7, 75), (7, 76), (7, 92), (8, 12), (8, 14), (8, 15), (8, 17),
(8, 92), (9, 26), (9, 27), (9, 7), (9, 10), (9, 16), (9, 18), (9, 24), (9, 35), (9, 37), (9, 44), (9, 46),
(9, 47), (9, 49), (9, 79), (9, 80), (9, 81), (9, 82), (9, 83), (10, 26), (10, 35), (10, 36), (10, 51), (10, 24),
(10, 7), (10, 71), (10, 72), (10, 82)
) v([RoleId], [PermissionId])
WHERE EXISTS (SELECT 1 FROM [Core].[Roles] r WHERE r.[Id] = v.[RoleId])
  AND EXISTS (SELECT 1 FROM [Core].[Permissions] p WHERE p.[Id] = v.[PermissionId])
  AND NOT EXISTS (SELECT 1 FROM [Core].[RolePermissions] rp WHERE rp.[RoleId] = v.[RoleId] AND rp.[PermissionId] = v.[PermissionId]);");

            // Üretim olayları ve stok hareketleri yalnızca eklenir; değiştirilemez, silinemez.
            migrationBuilder.Sql(@"EXEC(N'CREATE TRIGGER [Konfeksiyon].[TR_BundleOperationEvents_AppendOnly]
    ON [Konfeksiyon].[BundleOperationEvents]
    INSTEAD OF UPDATE, DELETE
    AS
    BEGIN
        SET NOCOUNT ON;
        THROW 51000, ''BundleOperationEvents is append-only: production events cannot be updated or deleted.'', 1;
    END');");

            migrationBuilder.Sql(@"EXEC(N'CREATE TRIGGER [Konfeksiyon].[TR_StockTransactions_AppendOnly]
    ON [Konfeksiyon].[StockTransactions]
    INSTEAD OF UPDATE, DELETE
    AS
    BEGIN
        SET NOCOUNT ON;
        THROW 51000, ''StockTransactions is append-only: stock movements cannot be updated or deleted.'', 1;
    END');");

            // Her deponun adres belirtilmeyen stok için varsayılan adresi.
            migrationBuilder.Sql(@"INSERT INTO [Konfeksiyon].[WarehouseLocations]
    ([WarehouseId], [Code], [Zone], [Description], [IsDefault], [IsActive], [CreatedAt], [CreatedBy], [IsDeleted])
SELECT w.[Id], N'GENEL', N'GENEL', N'Adres belirtilmeyen stok', 1, 1, SYSUTCDATETIME(), N'system', 0
FROM [Konfeksiyon].[Warehouses] AS w
WHERE NOT EXISTS (SELECT 1 FROM [Konfeksiyon].[WarehouseLocations] l WHERE l.[WarehouseId] = w.[Id] AND l.[IsDefault] = 1);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DELETE FROM [Core].[RolePermissions] WHERE [PermissionId] IN (7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51, 52, 53, 56, 57, 58, 59, 60, 61, 62, 63, 64, 65, 66, 67, 68, 71, 72, 73, 74, 75, 76, 77, 78, 79, 80, 81, 82, 83, 84, 85, 86, 88, 89, 90, 91, 92);
DELETE FROM [Core].[Permissions] WHERE [Id] IN (7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51, 52, 53, 56, 57, 58, 59, 60, 61, 62, 63, 64, 65, 66, 67, 68, 71, 72, 73, 74, 75, 76, 77, 78, 79, 80, 81, 82, 83, 84, 85, 86, 88, 89, 90, 91, 92);");

            migrationBuilder.DropTable(
                name: "ApprovalFlowStepUsers",
                schema: "Konfeksiyon");

            migrationBuilder.DropTable(
                name: "BundleCardAssignments",
                schema: "Konfeksiyon");

            migrationBuilder.DropTable(
                name: "BundleOperationEvents",
                schema: "Konfeksiyon");

            migrationBuilder.DropTable(
                name: "CustomerDiscounts",
                schema: "Konfeksiyon");

            migrationBuilder.DropTable(
                name: "GoodsReceiptLines",
                schema: "Konfeksiyon");

            migrationBuilder.DropTable(
                name: "LineMachineAssignments",
                schema: "Konfeksiyon");

            migrationBuilder.DropTable(
                name: "LineProductionPlans",
                schema: "Konfeksiyon");

            migrationBuilder.DropTable(
                name: "MachineOperations",
                schema: "Konfeksiyon");

            migrationBuilder.DropTable(
                name: "MachineStatusLogs",
                schema: "Konfeksiyon");

            migrationBuilder.DropTable(
                name: "OperationDependencies",
                schema: "Konfeksiyon");

            migrationBuilder.DropTable(
                name: "OperatorCards",
                schema: "Konfeksiyon");

            migrationBuilder.DropTable(
                name: "OperatorMachines",
                schema: "Konfeksiyon");

            migrationBuilder.DropTable(
                name: "OperatorOperationQualifications",
                schema: "Konfeksiyon");

            migrationBuilder.DropTable(
                name: "OperatorPhotos",
                schema: "Konfeksiyon");

            migrationBuilder.DropTable(
                name: "PayrollBonusTiers",
                schema: "Konfeksiyon");

            migrationBuilder.DropTable(
                name: "PriceListItems",
                schema: "Konfeksiyon");

            migrationBuilder.DropTable(
                name: "ProductionOrderItems",
                schema: "Konfeksiyon");

            migrationBuilder.DropTable(
                name: "ProductMaterials",
                schema: "Konfeksiyon");

            migrationBuilder.DropTable(
                name: "QualityRecords",
                schema: "Konfeksiyon");

            migrationBuilder.DropTable(
                name: "ReworkTasks",
                schema: "Konfeksiyon");

            migrationBuilder.DropTable(
                name: "RfidReadLogs",
                schema: "Konfeksiyon");

            migrationBuilder.DropTable(
                name: "SalesOrderApprovalEvents",
                schema: "Konfeksiyon");

            migrationBuilder.DropTable(
                name: "SalesOrderApprovalSteps",
                schema: "Konfeksiyon");

            migrationBuilder.DropTable(
                name: "SalesOrderReservations",
                schema: "Konfeksiyon");

            migrationBuilder.DropTable(
                name: "ShipmentItems",
                schema: "Konfeksiyon");

            migrationBuilder.DropTable(
                name: "StockCountLines",
                schema: "Konfeksiyon");

            migrationBuilder.DropTable(
                name: "StockReservations",
                schema: "Konfeksiyon");

            migrationBuilder.DropTable(
                name: "Stocks",
                schema: "Konfeksiyon");

            migrationBuilder.DropTable(
                name: "StockTransactions",
                schema: "Konfeksiyon");

            migrationBuilder.DropTable(
                name: "WorkSessionPauses",
                schema: "Konfeksiyon");

            migrationBuilder.DropTable(
                name: "ApprovalFlowSteps",
                schema: "Konfeksiyon");

            migrationBuilder.DropTable(
                name: "GoodsReceipts",
                schema: "Konfeksiyon");

            migrationBuilder.DropTable(
                name: "PurchaseOrderItems",
                schema: "Konfeksiyon");

            migrationBuilder.DropTable(
                name: "ProductionLines",
                schema: "Konfeksiyon");

            migrationBuilder.DropTable(
                name: "RfidCards",
                schema: "Konfeksiyon");

            migrationBuilder.DropTable(
                name: "PayrollSettings",
                schema: "Konfeksiyon");

            migrationBuilder.DropTable(
                name: "PriceLists",
                schema: "Konfeksiyon");

            migrationBuilder.DropTable(
                name: "DefectTypes",
                schema: "Konfeksiyon");

            migrationBuilder.DropTable(
                name: "QualityInspections",
                schema: "Konfeksiyon");

            migrationBuilder.DropTable(
                name: "SalesOrderLines",
                schema: "Konfeksiyon");

            migrationBuilder.DropTable(
                name: "StockCounts",
                schema: "Konfeksiyon");

            migrationBuilder.DropTable(
                name: "Shipments",
                schema: "Konfeksiyon");

            migrationBuilder.DropTable(
                name: "StockLots",
                schema: "Konfeksiyon");

            migrationBuilder.DropTable(
                name: "BreakSchedules",
                schema: "Konfeksiyon");

            migrationBuilder.DropTable(
                name: "StopReasons",
                schema: "Konfeksiyon");

            migrationBuilder.DropTable(
                name: "WorkSessions",
                schema: "Konfeksiyon");

            migrationBuilder.DropTable(
                name: "ApprovalFlows",
                schema: "Konfeksiyon");

            migrationBuilder.DropTable(
                name: "ProductionAreas",
                schema: "Konfeksiyon");

            migrationBuilder.DropTable(
                name: "BundleOperations",
                schema: "Konfeksiyon");

            migrationBuilder.DropTable(
                name: "WarehouseLocations",
                schema: "Konfeksiyon");

            migrationBuilder.DropTable(
                name: "PurchaseOrders",
                schema: "Konfeksiyon");

            migrationBuilder.DropTable(
                name: "StockItems",
                schema: "Konfeksiyon");

            migrationBuilder.DropTable(
                name: "Shifts",
                schema: "Konfeksiyon");

            migrationBuilder.DropTable(
                name: "Tablets",
                schema: "Konfeksiyon");

            migrationBuilder.DropTable(
                name: "Factories",
                schema: "Konfeksiyon");

            migrationBuilder.DropTable(
                name: "Bundles",
                schema: "Konfeksiyon");

            migrationBuilder.DropTable(
                name: "Operators",
                schema: "Konfeksiyon");

            migrationBuilder.DropTable(
                name: "ProductionRouteOperations",
                schema: "Konfeksiyon");

            migrationBuilder.DropTable(
                name: "Machines",
                schema: "Konfeksiyon");

            migrationBuilder.DropTable(
                name: "Colors",
                schema: "Konfeksiyon");

            migrationBuilder.DropTable(
                name: "ProductionOrders",
                schema: "Konfeksiyon");

            migrationBuilder.DropTable(
                name: "Sizes",
                schema: "Konfeksiyon");

            migrationBuilder.DropTable(
                name: "BreakGroups",
                schema: "Konfeksiyon");

            migrationBuilder.DropTable(
                name: "ProductionRoutes",
                schema: "Konfeksiyon");

            migrationBuilder.DropTable(
                name: "Operations",
                schema: "Konfeksiyon");

            migrationBuilder.DropTable(
                name: "Suppliers",
                schema: "Konfeksiyon");

            migrationBuilder.DropTable(
                name: "Products",
                schema: "Konfeksiyon");

            migrationBuilder.DropTable(
                name: "SalesOrders",
                schema: "Konfeksiyon");

            migrationBuilder.DropTable(
                name: "Warehouses",
                schema: "Konfeksiyon");

            migrationBuilder.DropTable(
                name: "MachineTypes",
                schema: "Konfeksiyon");

            migrationBuilder.DropTable(
                name: "Models",
                schema: "Konfeksiyon");

            migrationBuilder.DropTable(
                name: "ProductGroups",
                schema: "Konfeksiyon");

            migrationBuilder.DropTable(
                name: "CustomerAddresses",
                schema: "Konfeksiyon");

            migrationBuilder.DropTable(
                name: "CustomerBuyers",
                schema: "Konfeksiyon");

            migrationBuilder.DropTable(
                name: "Customers",
                schema: "Konfeksiyon");
        }
    }
}
