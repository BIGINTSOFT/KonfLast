using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ERP.Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Core");

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                schema: "Core",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EntityName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EntityId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Action = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    OldValues = table.Column<string>(type: "nvarchar(max)", maxLength: 200, nullable: true),
                    NewValues = table.Column<string>(type: "nvarchar(max)", maxLength: 200, nullable: true),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    UserName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    DeviceInfo = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                    table.CheckConstraint("CK_AuditLogs_Action", "[Action] IN ('CREATED', 'UPDATED', 'DELETED')");
                });

            migrationBuilder.CreateTable(
                name: "Currencies",
                schema: "Core",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "char(3)", maxLength: 200, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Symbol = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: true),
                    DecimalPlaces = table.Column<int>(type: "int", nullable: false),
                    FetchFromTcmb = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_Currencies", x => x.Id);
                    table.CheckConstraint("CK_Currencies_DecimalPlaces", "[DecimalPlaces] BETWEEN 0 AND 4");
                });

            migrationBuilder.CreateTable(
                name: "DocumentSequences",
                schema: "Core",
                columns: table => new
                {
                    Series = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    LastValue = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentSequences", x => new { x.Series, x.Year });
                });

            migrationBuilder.CreateTable(
                name: "IntegrationMessages",
                schema: "Core",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EntityName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EntityId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Payload = table.Column<string>(type: "nvarchar(max)", maxLength: 200, nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntegrationMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Permissions",
                schema: "Core",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Module = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                schema: "Core",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsSystem = table.Column<bool>(type: "bit", nullable: false),
                    ModuleKey = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                schema: "Core",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    PasswordHash = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    FailedLoginCount = table.Column<int>(type: "int", nullable: false),
                    LockoutEndAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastLoginAt = table.Column<DateTime>(type: "datetime2", nullable: true),
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
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExchangeRates",
                schema: "Core",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CurrencyId = table.Column<int>(type: "int", nullable: false),
                    RateDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ForexBuying = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    ForexSelling = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    BanknoteBuying = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    BanknoteSelling = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    Source = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExchangeRates", x => x.Id);
                    table.CheckConstraint("CK_ExchangeRates_Positive", "[ForexBuying] > 0 AND [ForexSelling] > 0 AND ([BanknoteBuying] IS NULL OR [BanknoteBuying] > 0) AND ([BanknoteSelling] IS NULL OR [BanknoteSelling] > 0)");
                    table.CheckConstraint("CK_ExchangeRates_Source", "[Source] IN ('TCMB', 'MANUAL')");
                    table.ForeignKey(
                        name: "FK_ExchangeRates_Currencies_CurrencyId",
                        column: x => x.CurrencyId,
                        principalSchema: "Core",
                        principalTable: "Currencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "IntegrationDeliveries",
                schema: "Core",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IntegrationMessageId = table.Column<long>(type: "bigint", nullable: false),
                    Channel = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    AttemptCount = table.Column<int>(type: "int", nullable: false),
                    NextAttemptAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LockedUntil = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastAttemptAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastError = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DeliveredAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntegrationDeliveries", x => x.Id);
                    table.CheckConstraint("CK_IntegrationDeliveries_AttemptCount", "[AttemptCount] >= 0");
                    table.CheckConstraint("CK_IntegrationDeliveries_Delivered", "[Status] <> 'SUCCEEDED' OR [DeliveredAt] IS NOT NULL");
                    table.CheckConstraint("CK_IntegrationDeliveries_Status", "[Status] IN ('PENDING', 'SUCCEEDED', 'FAILED')");
                    table.ForeignKey(
                        name: "FK_IntegrationDeliveries_IntegrationMessages_IntegrationMessageId",
                        column: x => x.IntegrationMessageId,
                        principalSchema: "Core",
                        principalTable: "IntegrationMessages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RolePermissions",
                schema: "Core",
                columns: table => new
                {
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    PermissionId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePermissions", x => new { x.RoleId, x.PermissionId });
                    table.ForeignKey(
                        name: "FK_RolePermissions_Permissions_PermissionId",
                        column: x => x.PermissionId,
                        principalSchema: "Core",
                        principalTable: "Permissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RolePermissions_Roles_RoleId",
                        column: x => x.RoleId,
                        principalSchema: "Core",
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PasswordResetTokens",
                schema: "Core",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    TokenHash = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RequestedByIp = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    UsedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PasswordResetTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PasswordResetTokens_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "Core",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                schema: "Core",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    TokenHash = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByIp = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    DeviceInfo = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    IsPersistent = table.Column<bool>(type: "bit", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RevokedReason = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    ReplacedByTokenHash = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "Core",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserFormDrafts",
                schema: "Core",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    FormKey = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    Payload = table.Column<string>(type: "nvarchar(max)", maxLength: 200, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserFormDrafts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserFormDrafts_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "Core",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserRoles",
                schema: "Core",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false),
                    RoleId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_UserRoles_Roles_RoleId",
                        column: x => x.RoleId,
                        principalSchema: "Core",
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserRoles_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "Core",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserShortcuts",
                schema: "Core",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Path = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserShortcuts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserShortcuts_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "Core",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "Core",
                table: "Currencies",
                columns: new[] { "Id", "Code", "CreatedAt", "CreatedBy", "DecimalPlaces", "DeletedAt", "DeletedBy", "FetchFromTcmb", "IsActive", "IsDeleted", "Name", "Symbol", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
                    { 1, "TRY", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "system", 2, null, null, false, true, false, "Türk lirası", "₺", null, null },
                    { 2, "USD", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "system", 2, null, null, true, true, false, "ABD doları", "$", null, null },
                    { 3, "EUR", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "system", 2, null, null, true, true, false, "Euro", "€", null, null },
                    { 4, "GBP", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "system", 2, null, null, true, true, false, "İngiliz sterlini", "£", null, null }
                });

            migrationBuilder.InsertData(
                schema: "Core",
                table: "Permissions",
                columns: new[] { "Id", "Code", "Description", "Module" },
                values: new object[,]
                {
                    { 1, "User.View", "Kullanıcıları görüntüleme", "User" },
                    { 2, "User.Manage", "Kullanıcı oluşturma / güncelleme", "User" },
                    { 3, "Role.View", "Rol ve yetkileri görüntüleme", "Role" },
                    { 4, "Role.Manage", "Rol yetkilerini düzenleme", "Role" },
                    { 5, "MasterData.View", "Ana verileri görüntüleme", "MasterData" },
                    { 6, "MasterData.Manage", "Ana verileri düzenleme", "MasterData" },
                    { 28, "AuditLog.View", "İşlem kayıtlarını (audit log) görüntüleme", "AuditLog" },
                    { 39, "Integration.View", "Entegrasyon olaylarını ve teslimlerini görüntüleme", "Integration" },
                    { 40, "Integration.Manage", "Entegrasyon teslimlerini yeniden gönderme", "Integration" },
                    { 54, "Pricing.View", "Fiyat listeleri, iskontolar, para birimleri ve döviz kurlarını görüntüleme", "Pricing" },
                    { 55, "Pricing.Manage", "Fiyat listesi, iskonto, para birimi ve döviz kuru düzenleme", "Pricing" },
                    { 69, "User.AssignRoles", "Kullanıcıya rol verme / rolünü değiştirme", "User" },
                    { 70, "User.ResetPassword", "Kullanıcı parolasını sıfırlama", "User" },
                    { 87, "ExchangeRate.Manage", "Döviz kuru girme / silme", "ExchangeRate" }
                });

            migrationBuilder.InsertData(
                schema: "Core",
                table: "Roles",
                columns: new[] { "Id", "Code", "IsSystem", "ModuleKey", "Name" },
                values: new object[,]
                {
                    { 1, "ADMIN", true, null, "Sistem Yöneticisi" },
                    { 2, "PRODUCTION_MANAGER", true, "Konfeksiyon", "Üretim Müdürü" },
                    { 3, "PRODUCTION_PLANNER", true, "Konfeksiyon", "Üretim Planlama" },
                    { 4, "LINE_MANAGER", true, "Konfeksiyon", "Bant Şefi" },
                    { 5, "QUALITY", true, "Konfeksiyon", "Kalite" },
                    { 6, "WAREHOUSE", true, "Konfeksiyon", "Depo" },
                    { 7, "MAINTENANCE", true, "Konfeksiyon", "Bakım" },
                    { 8, "OPERATOR", true, "Konfeksiyon", "Operatör" },
                    { 9, "MANAGEMENT", true, null, "Üst Yönetim" },
                    { 10, "PURCHASING", true, "Konfeksiyon", "Satın Alma" }
                });

            migrationBuilder.InsertData(
                schema: "Core",
                table: "RolePermissions",
                columns: new[] { "PermissionId", "RoleId" },
                values: new object[,]
                {
                    { 1, 1 },
                    { 2, 1 },
                    { 3, 1 },
                    { 4, 1 },
                    { 5, 1 },
                    { 6, 1 },
                    { 28, 1 },
                    { 39, 1 },
                    { 40, 1 },
                    { 54, 1 },
                    { 55, 1 },
                    { 69, 1 },
                    { 70, 1 },
                    { 87, 1 },
                    { 5, 2 },
                    { 6, 2 },
                    { 54, 2 },
                    { 55, 2 },
                    { 87, 2 },
                    { 5, 3 },
                    { 54, 3 },
                    { 55, 3 },
                    { 87, 3 },
                    { 54, 9 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_CreatedAt",
                schema: "Core",
                table: "AuditLogs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_EntityName_EntityId_Id",
                schema: "Core",
                table: "AuditLogs",
                columns: new[] { "EntityName", "EntityId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_UserName_Id",
                schema: "Core",
                table: "AuditLogs",
                columns: new[] { "UserName", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_Currencies_Code",
                schema: "Core",
                table: "Currencies",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ExchangeRates_CurrencyId_RateDate",
                schema: "Core",
                table: "ExchangeRates",
                columns: new[] { "CurrencyId", "RateDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExchangeRates_RateDate",
                schema: "Core",
                table: "ExchangeRates",
                column: "RateDate");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationDeliveries_Channel_Status",
                schema: "Core",
                table: "IntegrationDeliveries",
                columns: new[] { "Channel", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationDeliveries_IntegrationMessageId_Channel",
                schema: "Core",
                table: "IntegrationDeliveries",
                columns: new[] { "IntegrationMessageId", "Channel" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationDeliveries_Status_NextAttemptAt",
                schema: "Core",
                table: "IntegrationDeliveries",
                columns: new[] { "Status", "NextAttemptAt" });

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationMessages_EntityName_EntityId",
                schema: "Core",
                table: "IntegrationMessages",
                columns: new[] { "EntityName", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationMessages_EventId",
                schema: "Core",
                table: "IntegrationMessages",
                column: "EventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationMessages_EventType_Id",
                schema: "Core",
                table: "IntegrationMessages",
                columns: new[] { "EventType", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationMessages_Reference",
                schema: "Core",
                table: "IntegrationMessages",
                column: "Reference");

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetTokens_TokenHash",
                schema: "Core",
                table: "PasswordResetTokens",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetTokens_UserId",
                schema: "Core",
                table: "PasswordResetTokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_Code",
                schema: "Core",
                table: "Permissions",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_TokenHash",
                schema: "Core",
                table: "RefreshTokens",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId_ExpiresAt",
                schema: "Core",
                table: "RefreshTokens",
                columns: new[] { "UserId", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_PermissionId",
                schema: "Core",
                table: "RolePermissions",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_Code",
                schema: "Core",
                table: "Roles",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserFormDrafts_UserId_FormKey",
                schema: "Core",
                table: "UserFormDrafts",
                columns: new[] { "UserId", "FormKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_RoleId",
                schema: "Core",
                table: "UserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_NormalizedUserName",
                schema: "Core",
                table: "Users",
                column: "NormalizedUserName",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_UserShortcuts_UserId_Path",
                schema: "Core",
                table: "UserShortcuts",
                columns: new[] { "UserId", "Path" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserShortcuts_UserId_SortOrder",
                schema: "Core",
                table: "UserShortcuts",
                columns: new[] { "UserId", "SortOrder" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLogs",
                schema: "Core");

            migrationBuilder.DropTable(
                name: "DocumentSequences",
                schema: "Core");

            migrationBuilder.DropTable(
                name: "ExchangeRates",
                schema: "Core");

            migrationBuilder.DropTable(
                name: "IntegrationDeliveries",
                schema: "Core");

            migrationBuilder.DropTable(
                name: "PasswordResetTokens",
                schema: "Core");

            migrationBuilder.DropTable(
                name: "RefreshTokens",
                schema: "Core");

            migrationBuilder.DropTable(
                name: "RolePermissions",
                schema: "Core");

            migrationBuilder.DropTable(
                name: "UserFormDrafts",
                schema: "Core");

            migrationBuilder.DropTable(
                name: "UserRoles",
                schema: "Core");

            migrationBuilder.DropTable(
                name: "UserShortcuts",
                schema: "Core");

            migrationBuilder.DropTable(
                name: "Currencies",
                schema: "Core");

            migrationBuilder.DropTable(
                name: "IntegrationMessages",
                schema: "Core");

            migrationBuilder.DropTable(
                name: "Permissions",
                schema: "Core");

            migrationBuilder.DropTable(
                name: "Roles",
                schema: "Core");

            migrationBuilder.DropTable(
                name: "Users",
                schema: "Core");
        }
    }
}
