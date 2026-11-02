/*
    Tekstil ERP - Çekirdek (Core) kurulum / güncelleme script'i
    Bu dosya otomatik üretilir (database\generate-script.ps1); elle düzenlemeyin.

    Hedef veritabanına (örn. TekstilERP) bağlanıp script'in tamamını çalıştırın. Script tekrar
    çalıştırılabilir: daha önce uygulanmış adımlar atlanır. Sıra: önce 00_Core.sql, sonra modüller.

    sqlcmd ile:  sqlcmd -S "(localdb)\MSSQLLocalDB" -d TekstilERP -E -b -f 65001 -i 00_Core.sql
*/

-- Filtreli index'ler için zorunlu oturum ayarları (sqlcmd varsayılanı farklıdır).
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;
GO
IF OBJECT_ID(N'[Core].[__EFMigrationsHistory]') IS NULL
BEGIN
    IF SCHEMA_ID(N'Core') IS NULL EXEC(N'CREATE SCHEMA [Core];');
    CREATE TABLE [Core].[__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [Core].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030064926_InitialCore'
)
BEGIN
    IF SCHEMA_ID(N'Core') IS NULL EXEC(N'CREATE SCHEMA [Core];');
END;

IF NOT EXISTS (
    SELECT * FROM [Core].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030064926_InitialCore'
)
BEGIN
    CREATE TABLE [Core].[AuditLogs] (
        [Id] bigint NOT NULL IDENTITY,
        [EntityName] nvarchar(100) NOT NULL,
        [EntityId] nvarchar(100) NOT NULL,
        [Action] nvarchar(30) NOT NULL,
        [OldValues] nvarchar(max) NULL,
        [NewValues] nvarchar(max) NULL,
        [UserId] int NULL,
        [UserName] nvarchar(100) NOT NULL,
        [IpAddress] nvarchar(45) NULL,
        [DeviceInfo] nvarchar(300) NULL,
        [CorrelationId] nvarchar(64) NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_AuditLogs] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_AuditLogs_Action] CHECK ([Action] IN ('CREATED', 'UPDATED', 'DELETED'))
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Core].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030064926_InitialCore'
)
BEGIN
    CREATE TABLE [Core].[Currencies] (
        [Id] int NOT NULL IDENTITY,
        [Code] char(3) NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [Symbol] nvarchar(5) NULL,
        [DecimalPlaces] int NOT NULL,
        [FetchFromTcmb] bit NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_Currencies] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Currencies_DecimalPlaces] CHECK ([DecimalPlaces] BETWEEN 0 AND 4)
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Core].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030064926_InitialCore'
)
BEGIN
    CREATE TABLE [Core].[DocumentSequences] (
        [Series] nvarchar(50) NOT NULL,
        [Year] int NOT NULL,
        [LastValue] int NOT NULL,
        CONSTRAINT [PK_DocumentSequences] PRIMARY KEY ([Series], [Year])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Core].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030064926_InitialCore'
)
BEGIN
    CREATE TABLE [Core].[IntegrationMessages] (
        [Id] bigint NOT NULL IDENTITY,
        [EventId] uniqueidentifier NOT NULL,
        [EventType] nvarchar(100) NOT NULL,
        [EntityName] nvarchar(100) NOT NULL,
        [EntityId] nvarchar(50) NOT NULL,
        [Reference] nvarchar(50) NULL,
        [Payload] nvarchar(max) NOT NULL,
        [OccurredAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NOT NULL,
        CONSTRAINT [PK_IntegrationMessages] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Core].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030064926_InitialCore'
)
BEGIN
    CREATE TABLE [Core].[Permissions] (
        [Id] int NOT NULL IDENTITY,
        [Code] nvarchar(100) NOT NULL,
        [Module] nvarchar(50) NOT NULL,
        [Description] nvarchar(250) NOT NULL,
        CONSTRAINT [PK_Permissions] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Core].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030064926_InitialCore'
)
BEGIN
    CREATE TABLE [Core].[Roles] (
        [Id] int NOT NULL IDENTITY,
        [Code] nvarchar(50) NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [IsSystem] bit NOT NULL,
        [ModuleKey] nvarchar(50) NULL,
        CONSTRAINT [PK_Roles] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Core].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030064926_InitialCore'
)
BEGIN
    CREATE TABLE [Core].[Users] (
        [Id] int NOT NULL IDENTITY,
        [UserName] nvarchar(50) NOT NULL,
        [NormalizedUserName] nvarchar(50) NOT NULL,
        [FullName] nvarchar(150) NOT NULL,
        [Email] nvarchar(256) NULL,
        [PasswordHash] nvarchar(500) NOT NULL,
        [IsActive] bit NOT NULL,
        [FailedLoginCount] int NOT NULL,
        [LockoutEndAt] datetime2 NULL,
        [LastLoginAt] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Core].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030064926_InitialCore'
)
BEGIN
    CREATE TABLE [Core].[ExchangeRates] (
        [Id] int NOT NULL IDENTITY,
        [CurrencyId] int NOT NULL,
        [RateDate] date NOT NULL,
        [ForexBuying] decimal(18,6) NOT NULL,
        [ForexSelling] decimal(18,6) NOT NULL,
        [BanknoteBuying] decimal(18,6) NULL,
        [BanknoteSelling] decimal(18,6) NULL,
        [Source] nvarchar(30) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_ExchangeRates] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_ExchangeRates_Positive] CHECK ([ForexBuying] > 0 AND [ForexSelling] > 0 AND ([BanknoteBuying] IS NULL OR [BanknoteBuying] > 0) AND ([BanknoteSelling] IS NULL OR [BanknoteSelling] > 0)),
        CONSTRAINT [CK_ExchangeRates_Source] CHECK ([Source] IN ('TCMB', 'MANUAL')),
        CONSTRAINT [FK_ExchangeRates_Currencies_CurrencyId] FOREIGN KEY ([CurrencyId]) REFERENCES [Core].[Currencies] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Core].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030064926_InitialCore'
)
BEGIN
    CREATE TABLE [Core].[IntegrationDeliveries] (
        [Id] bigint NOT NULL IDENTITY,
        [IntegrationMessageId] bigint NOT NULL,
        [Channel] nvarchar(100) NOT NULL,
        [Status] nvarchar(30) NOT NULL,
        [AttemptCount] int NOT NULL,
        [NextAttemptAt] datetime2 NOT NULL,
        [LockedUntil] datetime2 NULL,
        [LastAttemptAt] datetime2 NULL,
        [LastError] nvarchar(1000) NULL,
        [DeliveredAt] datetime2 NULL,
        CONSTRAINT [PK_IntegrationDeliveries] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_IntegrationDeliveries_AttemptCount] CHECK ([AttemptCount] >= 0),
        CONSTRAINT [CK_IntegrationDeliveries_Delivered] CHECK ([Status] <> 'SUCCEEDED' OR [DeliveredAt] IS NOT NULL),
        CONSTRAINT [CK_IntegrationDeliveries_Status] CHECK ([Status] IN ('PENDING', 'SUCCEEDED', 'FAILED')),
        CONSTRAINT [FK_IntegrationDeliveries_IntegrationMessages_IntegrationMessageId] FOREIGN KEY ([IntegrationMessageId]) REFERENCES [Core].[IntegrationMessages] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Core].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030064926_InitialCore'
)
BEGIN
    CREATE TABLE [Core].[RolePermissions] (
        [RoleId] int NOT NULL,
        [PermissionId] int NOT NULL,
        CONSTRAINT [PK_RolePermissions] PRIMARY KEY ([RoleId], [PermissionId]),
        CONSTRAINT [FK_RolePermissions_Permissions_PermissionId] FOREIGN KEY ([PermissionId]) REFERENCES [Core].[Permissions] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_RolePermissions_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [Core].[Roles] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Core].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030064926_InitialCore'
)
BEGIN
    CREATE TABLE [Core].[PasswordResetTokens] (
        [Id] int NOT NULL IDENTITY,
        [UserId] int NOT NULL,
        [TokenHash] varchar(64) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [ExpiresAt] datetime2 NOT NULL,
        [RequestedByIp] nvarchar(45) NULL,
        [UsedAt] datetime2 NULL,
        CONSTRAINT [PK_PasswordResetTokens] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PasswordResetTokens_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Core].[Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Core].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030064926_InitialCore'
)
BEGIN
    CREATE TABLE [Core].[RefreshTokens] (
        [Id] int NOT NULL IDENTITY,
        [UserId] int NOT NULL,
        [TokenHash] varchar(64) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [ExpiresAt] datetime2 NOT NULL,
        [CreatedByIp] nvarchar(45) NULL,
        [DeviceInfo] nvarchar(300) NULL,
        [IsPersistent] bit NOT NULL,
        [RevokedAt] datetime2 NULL,
        [RevokedReason] nvarchar(30) NULL,
        [ReplacedByTokenHash] varchar(64) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_RefreshTokens] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RefreshTokens_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Core].[Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Core].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030064926_InitialCore'
)
BEGIN
    CREATE TABLE [Core].[UserFormDrafts] (
        [Id] int NOT NULL IDENTITY,
        [UserId] int NOT NULL,
        [FormKey] varchar(100) NOT NULL,
        [Payload] nvarchar(max) NOT NULL,
        [Title] nvarchar(200) NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_UserFormDrafts] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_UserFormDrafts_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Core].[Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Core].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030064926_InitialCore'
)
BEGIN
    CREATE TABLE [Core].[UserRoles] (
        [UserId] int NOT NULL,
        [RoleId] int NOT NULL,
        CONSTRAINT [PK_UserRoles] PRIMARY KEY ([UserId], [RoleId]),
        CONSTRAINT [FK_UserRoles_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [Core].[Roles] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_UserRoles_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Core].[Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Core].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030064926_InitialCore'
)
BEGIN
    CREATE TABLE [Core].[UserShortcuts] (
        [Id] int NOT NULL IDENTITY,
        [UserId] int NOT NULL,
        [Path] nvarchar(100) NOT NULL,
        [SortOrder] int NOT NULL,
        CONSTRAINT [PK_UserShortcuts] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_UserShortcuts_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Core].[Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Core].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030064926_InitialCore'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'CreatedAt', N'CreatedBy', N'DecimalPlaces', N'DeletedAt', N'DeletedBy', N'FetchFromTcmb', N'IsActive', N'IsDeleted', N'Name', N'Symbol', N'UpdatedAt', N'UpdatedBy') AND [object_id] = OBJECT_ID(N'[Core].[Currencies]'))
        SET IDENTITY_INSERT [Core].[Currencies] ON;
    EXEC(N'INSERT INTO [Core].[Currencies] ([Id], [Code], [CreatedAt], [CreatedBy], [DecimalPlaces], [DeletedAt], [DeletedBy], [FetchFromTcmb], [IsActive], [IsDeleted], [Name], [Symbol], [UpdatedAt], [UpdatedBy])
    VALUES (1, ''TRY'', ''2026-01-01T00:00:00.0000000Z'', N''system'', 2, NULL, NULL, CAST(0 AS bit), CAST(1 AS bit), CAST(0 AS bit), N''Türk lirası'', N''₺'', NULL, NULL),
    (2, ''USD'', ''2026-01-01T00:00:00.0000000Z'', N''system'', 2, NULL, NULL, CAST(1 AS bit), CAST(1 AS bit), CAST(0 AS bit), N''ABD doları'', N''$'', NULL, NULL),
    (3, ''EUR'', ''2026-01-01T00:00:00.0000000Z'', N''system'', 2, NULL, NULL, CAST(1 AS bit), CAST(1 AS bit), CAST(0 AS bit), N''Euro'', N''€'', NULL, NULL),
    (4, ''GBP'', ''2026-01-01T00:00:00.0000000Z'', N''system'', 2, NULL, NULL, CAST(1 AS bit), CAST(1 AS bit), CAST(0 AS bit), N''İngiliz sterlini'', N''£'', NULL, NULL)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'CreatedAt', N'CreatedBy', N'DecimalPlaces', N'DeletedAt', N'DeletedBy', N'FetchFromTcmb', N'IsActive', N'IsDeleted', N'Name', N'Symbol', N'UpdatedAt', N'UpdatedBy') AND [object_id] = OBJECT_ID(N'[Core].[Currencies]'))
        SET IDENTITY_INSERT [Core].[Currencies] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [Core].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030064926_InitialCore'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'Description', N'Module') AND [object_id] = OBJECT_ID(N'[Core].[Permissions]'))
        SET IDENTITY_INSERT [Core].[Permissions] ON;
    EXEC(N'INSERT INTO [Core].[Permissions] ([Id], [Code], [Description], [Module])
    VALUES (1, N''User.View'', N''Kullanıcıları görüntüleme'', N''User''),
    (2, N''User.Manage'', N''Kullanıcı oluşturma / güncelleme'', N''User''),
    (3, N''Role.View'', N''Rol ve yetkileri görüntüleme'', N''Role''),
    (4, N''Role.Manage'', N''Rol yetkilerini düzenleme'', N''Role''),
    (5, N''MasterData.View'', N''Ana verileri görüntüleme'', N''MasterData''),
    (6, N''MasterData.Manage'', N''Ana verileri düzenleme'', N''MasterData''),
    (28, N''AuditLog.View'', N''İşlem kayıtlarını (audit log) görüntüleme'', N''AuditLog''),
    (39, N''Integration.View'', N''Entegrasyon olaylarını ve teslimlerini görüntüleme'', N''Integration''),
    (40, N''Integration.Manage'', N''Entegrasyon teslimlerini yeniden gönderme'', N''Integration''),
    (54, N''Pricing.View'', N''Fiyat listeleri, iskontolar, para birimleri ve döviz kurlarını görüntüleme'', N''Pricing''),
    (55, N''Pricing.Manage'', N''Fiyat listesi, iskonto, para birimi ve döviz kuru düzenleme'', N''Pricing''),
    (69, N''User.AssignRoles'', N''Kullanıcıya rol verme / rolünü değiştirme'', N''User''),
    (70, N''User.ResetPassword'', N''Kullanıcı parolasını sıfırlama'', N''User''),
    (87, N''ExchangeRate.Manage'', N''Döviz kuru girme / silme'', N''ExchangeRate'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'Description', N'Module') AND [object_id] = OBJECT_ID(N'[Core].[Permissions]'))
        SET IDENTITY_INSERT [Core].[Permissions] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [Core].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030064926_InitialCore'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'IsSystem', N'ModuleKey', N'Name') AND [object_id] = OBJECT_ID(N'[Core].[Roles]'))
        SET IDENTITY_INSERT [Core].[Roles] ON;
    EXEC(N'INSERT INTO [Core].[Roles] ([Id], [Code], [IsSystem], [ModuleKey], [Name])
    VALUES (1, N''ADMIN'', CAST(1 AS bit), NULL, N''Sistem Yöneticisi''),
    (2, N''PRODUCTION_MANAGER'', CAST(1 AS bit), N''Konfeksiyon'', N''Üretim Müdürü''),
    (3, N''PRODUCTION_PLANNER'', CAST(1 AS bit), N''Konfeksiyon'', N''Üretim Planlama''),
    (4, N''LINE_MANAGER'', CAST(1 AS bit), N''Konfeksiyon'', N''Bant Şefi''),
    (5, N''QUALITY'', CAST(1 AS bit), N''Konfeksiyon'', N''Kalite''),
    (6, N''WAREHOUSE'', CAST(1 AS bit), N''Konfeksiyon'', N''Depo''),
    (7, N''MAINTENANCE'', CAST(1 AS bit), N''Konfeksiyon'', N''Bakım''),
    (8, N''OPERATOR'', CAST(1 AS bit), N''Konfeksiyon'', N''Operatör''),
    (9, N''MANAGEMENT'', CAST(1 AS bit), NULL, N''Üst Yönetim''),
    (10, N''PURCHASING'', CAST(1 AS bit), N''Konfeksiyon'', N''Satın Alma'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'IsSystem', N'ModuleKey', N'Name') AND [object_id] = OBJECT_ID(N'[Core].[Roles]'))
        SET IDENTITY_INSERT [Core].[Roles] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [Core].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030064926_InitialCore'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'PermissionId', N'RoleId') AND [object_id] = OBJECT_ID(N'[Core].[RolePermissions]'))
        SET IDENTITY_INSERT [Core].[RolePermissions] ON;
    EXEC(N'INSERT INTO [Core].[RolePermissions] ([PermissionId], [RoleId])
    VALUES (1, 1),
    (2, 1),
    (3, 1),
    (4, 1),
    (5, 1),
    (6, 1),
    (28, 1),
    (39, 1),
    (40, 1),
    (54, 1),
    (55, 1),
    (69, 1),
    (70, 1),
    (87, 1),
    (5, 2),
    (6, 2),
    (54, 2),
    (55, 2),
    (87, 2),
    (5, 3),
    (54, 3),
    (55, 3),
    (87, 3),
    (54, 9)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'PermissionId', N'RoleId') AND [object_id] = OBJECT_ID(N'[Core].[RolePermissions]'))
        SET IDENTITY_INSERT [Core].[RolePermissions] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [Core].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030064926_InitialCore'
)
BEGIN
    CREATE INDEX [IX_AuditLogs_CreatedAt] ON [Core].[AuditLogs] ([CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [Core].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030064926_InitialCore'
)
BEGIN
    CREATE INDEX [IX_AuditLogs_EntityName_EntityId_Id] ON [Core].[AuditLogs] ([EntityName], [EntityId], [Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [Core].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030064926_InitialCore'
)
BEGIN
    CREATE INDEX [IX_AuditLogs_UserName_Id] ON [Core].[AuditLogs] ([UserName], [Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [Core].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030064926_InitialCore'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Currencies_Code] ON [Core].[Currencies] ([Code]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [Core].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030064926_InitialCore'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ExchangeRates_CurrencyId_RateDate] ON [Core].[ExchangeRates] ([CurrencyId], [RateDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [Core].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030064926_InitialCore'
)
BEGIN
    CREATE INDEX [IX_ExchangeRates_RateDate] ON [Core].[ExchangeRates] ([RateDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [Core].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030064926_InitialCore'
)
BEGIN
    CREATE INDEX [IX_IntegrationDeliveries_Channel_Status] ON [Core].[IntegrationDeliveries] ([Channel], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [Core].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030064926_InitialCore'
)
BEGIN
    CREATE UNIQUE INDEX [IX_IntegrationDeliveries_IntegrationMessageId_Channel] ON [Core].[IntegrationDeliveries] ([IntegrationMessageId], [Channel]);
END;

IF NOT EXISTS (
    SELECT * FROM [Core].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030064926_InitialCore'
)
BEGIN
    CREATE INDEX [IX_IntegrationDeliveries_Status_NextAttemptAt] ON [Core].[IntegrationDeliveries] ([Status], [NextAttemptAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [Core].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030064926_InitialCore'
)
BEGIN
    CREATE INDEX [IX_IntegrationMessages_EntityName_EntityId] ON [Core].[IntegrationMessages] ([EntityName], [EntityId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Core].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030064926_InitialCore'
)
BEGIN
    CREATE UNIQUE INDEX [IX_IntegrationMessages_EventId] ON [Core].[IntegrationMessages] ([EventId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Core].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030064926_InitialCore'
)
BEGIN
    CREATE INDEX [IX_IntegrationMessages_EventType_Id] ON [Core].[IntegrationMessages] ([EventType], [Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [Core].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030064926_InitialCore'
)
BEGIN
    CREATE INDEX [IX_IntegrationMessages_Reference] ON [Core].[IntegrationMessages] ([Reference]);
END;

IF NOT EXISTS (
    SELECT * FROM [Core].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030064926_InitialCore'
)
BEGIN
    CREATE UNIQUE INDEX [IX_PasswordResetTokens_TokenHash] ON [Core].[PasswordResetTokens] ([TokenHash]);
END;

IF NOT EXISTS (
    SELECT * FROM [Core].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030064926_InitialCore'
)
BEGIN
    CREATE INDEX [IX_PasswordResetTokens_UserId] ON [Core].[PasswordResetTokens] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Core].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030064926_InitialCore'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Permissions_Code] ON [Core].[Permissions] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [Core].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030064926_InitialCore'
)
BEGIN
    CREATE UNIQUE INDEX [IX_RefreshTokens_TokenHash] ON [Core].[RefreshTokens] ([TokenHash]);
END;

IF NOT EXISTS (
    SELECT * FROM [Core].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030064926_InitialCore'
)
BEGIN
    CREATE INDEX [IX_RefreshTokens_UserId_ExpiresAt] ON [Core].[RefreshTokens] ([UserId], [ExpiresAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [Core].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030064926_InitialCore'
)
BEGIN
    CREATE INDEX [IX_RolePermissions_PermissionId] ON [Core].[RolePermissions] ([PermissionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Core].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030064926_InitialCore'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Roles_Code] ON [Core].[Roles] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [Core].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030064926_InitialCore'
)
BEGIN
    CREATE UNIQUE INDEX [IX_UserFormDrafts_UserId_FormKey] ON [Core].[UserFormDrafts] ([UserId], [FormKey]);
END;

IF NOT EXISTS (
    SELECT * FROM [Core].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030064926_InitialCore'
)
BEGIN
    CREATE INDEX [IX_UserRoles_RoleId] ON [Core].[UserRoles] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Core].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030064926_InitialCore'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Users_NormalizedUserName] ON [Core].[Users] ([NormalizedUserName]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [Core].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030064926_InitialCore'
)
BEGIN
    CREATE UNIQUE INDEX [IX_UserShortcuts_UserId_Path] ON [Core].[UserShortcuts] ([UserId], [Path]);
END;

IF NOT EXISTS (
    SELECT * FROM [Core].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030064926_InitialCore'
)
BEGIN
    CREATE INDEX [IX_UserShortcuts_UserId_SortOrder] ON [Core].[UserShortcuts] ([UserId], [SortOrder]);
END;

IF NOT EXISTS (
    SELECT * FROM [Core].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030064926_InitialCore'
)
BEGIN
    INSERT INTO [Core].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261030064926_InitialCore', N'10.0.12');
END;

COMMIT;
GO

