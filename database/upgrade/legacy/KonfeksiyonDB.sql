/*
    ESKİ YAPI (dondurulmuş): Modüler yapıya geçmeden önceki tek parça (dbo) kurulum script'i. Artık
    üretilmez. Yalnızca eski bir KonfeksiyonDB'yi taşımadan önce son eski sürüme getirmek için kullanılır;
    ardından database\upgrade\upgrade-from-KonfeksiyonDB.ps1 çalıştırılır. Yeni kurulumda kullanmayın.

    Konfeksiyon ERP - veritabanı kurulum / güncelleme script'i
    Bu dosya otomatik üretilir (database\generate-script.ps1); elle düzenlemeyin.

    Kullanım: Hedef veritabanına (örn. KonfeksiyonDB) bağlanıp script'in tamamını çalıştırın.
    Tüm nesneler dbo şeması altında oluşturulur. Script tekrar çalıştırılabilir:
    daha önce uygulanmış adımlar atlanır.

    sqlcmd ile:  sqlcmd -S "(localdb)\MSSQLLocalDB" -d KonfeksiyonDB -E -b -f 65001 -i KonfeksiyonDB.sql
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

IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007211319_InitialCreate'
)
BEGIN
    CREATE TABLE [dbo].[Colors] (
        [Id] int NOT NULL IDENTITY,
        [ColorCode] nvarchar(30) NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [HexCode] varchar(7) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_Colors] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007211319_InitialCreate'
)
BEGIN
    CREATE TABLE [dbo].[Customers] (
        [Id] int NOT NULL IDENTITY,
        [CustomerCode] nvarchar(30) NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [TaxNumber] nvarchar(20) NULL,
        [Phone] nvarchar(30) NULL,
        [Email] nvarchar(256) NULL,
        [Address] nvarchar(500) NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_Customers] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007211319_InitialCreate'
)
BEGIN
    CREATE TABLE [dbo].[MachineTypes] (
        [Id] int NOT NULL IDENTITY,
        [Code] nvarchar(30) NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_MachineTypes] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007211319_InitialCreate'
)
BEGIN
    CREATE TABLE [dbo].[Operators] (
        [Id] int NOT NULL IDENTITY,
        [OperatorCode] nvarchar(30) NOT NULL,
        [FirstName] nvarchar(100) NOT NULL,
        [LastName] nvarchar(100) NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_Operators] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007211319_InitialCreate'
)
BEGIN
    CREATE TABLE [dbo].[Permissions] (
        [Id] int NOT NULL IDENTITY,
        [Code] nvarchar(100) NOT NULL,
        [Module] nvarchar(50) NOT NULL,
        [Description] nvarchar(250) NOT NULL,
        CONSTRAINT [PK_Permissions] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007211319_InitialCreate'
)
BEGIN
    CREATE TABLE [dbo].[Roles] (
        [Id] int NOT NULL IDENTITY,
        [Code] nvarchar(50) NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [IsSystem] bit NOT NULL,
        CONSTRAINT [PK_Roles] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007211319_InitialCreate'
)
BEGIN
    CREATE TABLE [dbo].[Sizes] (
        [Id] int NOT NULL IDENTITY,
        [SizeCode] nvarchar(30) NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [SortOrder] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_Sizes] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007211319_InitialCreate'
)
BEGIN
    CREATE TABLE [dbo].[Users] (
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
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007211319_InitialCreate'
)
BEGIN
    CREATE TABLE [dbo].[Models] (
        [Id] int NOT NULL IDENTITY,
        [ModelCode] nvarchar(30) NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [Description] nvarchar(500) NULL,
        [CustomerId] int NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_Models] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Models_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [dbo].[Customers] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007211319_InitialCreate'
)
BEGIN
    CREATE TABLE [dbo].[Operations] (
        [Id] int NOT NULL IDENTITY,
        [OperationCode] nvarchar(30) NOT NULL,
        [Name] nvarchar(150) NOT NULL,
        [Description] nvarchar(500) NULL,
        [Sam] decimal(9,4) NOT NULL,
        [MachineTypeId] int NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_Operations] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Operations_Sam] CHECK ([Sam] >= 0),
        CONSTRAINT [FK_Operations_MachineTypes_MachineTypeId] FOREIGN KEY ([MachineTypeId]) REFERENCES [dbo].[MachineTypes] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007211319_InitialCreate'
)
BEGIN
    CREATE TABLE [dbo].[RolePermissions] (
        [RoleId] int NOT NULL,
        [PermissionId] int NOT NULL,
        CONSTRAINT [PK_RolePermissions] PRIMARY KEY ([RoleId], [PermissionId]),
        CONSTRAINT [FK_RolePermissions_Permissions_PermissionId] FOREIGN KEY ([PermissionId]) REFERENCES [dbo].[Permissions] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_RolePermissions_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [dbo].[Roles] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007211319_InitialCreate'
)
BEGIN
    CREATE TABLE [dbo].[RefreshTokens] (
        [Id] int NOT NULL IDENTITY,
        [UserId] int NOT NULL,
        [TokenHash] varchar(64) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [ExpiresAt] datetime2 NOT NULL,
        [CreatedByIp] nvarchar(45) NULL,
        [DeviceInfo] nvarchar(300) NULL,
        [RevokedAt] datetime2 NULL,
        [RevokedReason] nvarchar(30) NULL,
        [ReplacedByTokenHash] varchar(64) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_RefreshTokens] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RefreshTokens_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007211319_InitialCreate'
)
BEGIN
    CREATE TABLE [dbo].[UserRoles] (
        [UserId] int NOT NULL,
        [RoleId] int NOT NULL,
        CONSTRAINT [PK_UserRoles] PRIMARY KEY ([UserId], [RoleId]),
        CONSTRAINT [FK_UserRoles_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [dbo].[Roles] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_UserRoles_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007211319_InitialCreate'
)
BEGIN
    CREATE TABLE [dbo].[Products] (
        [Id] int NOT NULL IDENTITY,
        [ProductCode] nvarchar(30) NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [Description] nvarchar(500) NULL,
        [ModelId] int NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_Products] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Products_Models_ModelId] FOREIGN KEY ([ModelId]) REFERENCES [dbo].[Models] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007211319_InitialCreate'
)
BEGIN
    CREATE TABLE [dbo].[Machines] (
        [Id] int NOT NULL IDENTITY,
        [MachineCode] nvarchar(30) NOT NULL,
        [MachineName] nvarchar(150) NOT NULL,
        [MachineTypeId] int NOT NULL,
        [OperationId] int NULL,
        [Status] nvarchar(30) NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_Machines] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Machines_Status] CHECK ([Status] IN ('AVAILABLE', 'RUNNING', 'IDLE', 'BREAKDOWN', 'MAINTENANCE', 'OFFLINE')),
        CONSTRAINT [FK_Machines_MachineTypes_MachineTypeId] FOREIGN KEY ([MachineTypeId]) REFERENCES [dbo].[MachineTypes] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Machines_Operations_OperationId] FOREIGN KEY ([OperationId]) REFERENCES [dbo].[Operations] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007211319_InitialCreate'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'Description', N'Module') AND [object_id] = OBJECT_ID(N'[dbo].[Permissions]'))
        SET IDENTITY_INSERT [dbo].[Permissions] ON;
    EXEC(N'INSERT INTO [dbo].[Permissions] ([Id], [Code], [Description], [Module])
    VALUES (1, N''User.View'', N''Kullanıcıları görüntüleme'', N''User''),
    (2, N''User.Manage'', N''Kullanıcı oluşturma / güncelleme'', N''User''),
    (3, N''Role.View'', N''Rol ve yetkileri görüntüleme'', N''Role''),
    (4, N''Role.Manage'', N''Rol yetkilerini düzenleme'', N''Role''),
    (5, N''MasterData.View'', N''Ana verileri görüntüleme'', N''MasterData''),
    (6, N''MasterData.Manage'', N''Ana verileri düzenleme'', N''MasterData''),
    (7, N''ProductionOrder.View'', N''Üretim siparişlerini görüntüleme'', N''ProductionOrder''),
    (8, N''ProductionOrder.Create'', N''Üretim siparişi oluşturma'', N''ProductionOrder''),
    (9, N''ProductionOrder.Update'', N''Üretim siparişi güncelleme'', N''ProductionOrder''),
    (10, N''ProductionLine.View'', N''Bantları görüntüleme'', N''ProductionLine''),
    (11, N''ProductionLine.Update'', N''Bant ve bant planı düzenleme'', N''ProductionLine''),
    (12, N''Bundle.View'', N''Demetleri görüntüleme'', N''Bundle''),
    (13, N''Bundle.Create'', N''Demet oluşturma'', N''Bundle''),
    (14, N''Bundle.Start'', N''Demet operasyonu başlatma'', N''Bundle''),
    (15, N''Bundle.Complete'', N''Demet operasyonu tamamlama'', N''Bundle''),
    (16, N''Production.View'', N''Üretim hareketlerini görüntüleme'', N''Production''),
    (17, N''Production.Report'', N''Üretim miktarı bildirme'', N''Production''),
    (18, N''Quality.View'', N''Kalite kayıtlarını görüntüleme'', N''Quality''),
    (19, N''Quality.Create'', N''Kalite kaydı oluşturma'', N''Quality''),
    (20, N''Machine.View'', N''Makineleri görüntüleme'', N''Machine''),
    (21, N''Machine.Update'', N''Makine güncelleme / durum değiştirme'', N''Machine''),
    (22, N''Operator.View'', N''Operatörleri görüntüleme'', N''Operator''),
    (23, N''Operator.Update'', N''Operatör güncelleme'', N''Operator''),
    (24, N''Stock.View'', N''Stok ve depo görüntüleme'', N''Stock''),
    (25, N''Stock.Manage'', N''Stok hareketi oluşturma'', N''Stock''),
    (26, N''Dashboard.View'', N''Canlı üretim ekranlarını görüntüleme'', N''Dashboard''),
    (27, N''Report.View'', N''Raporları görüntüleme'', N''Report'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'Description', N'Module') AND [object_id] = OBJECT_ID(N'[dbo].[Permissions]'))
        SET IDENTITY_INSERT [dbo].[Permissions] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007211319_InitialCreate'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'IsSystem', N'Name') AND [object_id] = OBJECT_ID(N'[dbo].[Roles]'))
        SET IDENTITY_INSERT [dbo].[Roles] ON;
    EXEC(N'INSERT INTO [dbo].[Roles] ([Id], [Code], [IsSystem], [Name])
    VALUES (1, N''ADMIN'', CAST(1 AS bit), N''Sistem Yöneticisi''),
    (2, N''PRODUCTION_MANAGER'', CAST(1 AS bit), N''Üretim Müdürü''),
    (3, N''PRODUCTION_PLANNER'', CAST(1 AS bit), N''Üretim Planlama''),
    (4, N''LINE_MANAGER'', CAST(1 AS bit), N''Bant Şefi''),
    (5, N''QUALITY'', CAST(1 AS bit), N''Kalite''),
    (6, N''WAREHOUSE'', CAST(1 AS bit), N''Depo''),
    (7, N''MAINTENANCE'', CAST(1 AS bit), N''Bakım''),
    (8, N''OPERATOR'', CAST(1 AS bit), N''Operatör''),
    (9, N''MANAGEMENT'', CAST(1 AS bit), N''Üst Yönetim'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'IsSystem', N'Name') AND [object_id] = OBJECT_ID(N'[dbo].[Roles]'))
        SET IDENTITY_INSERT [dbo].[Roles] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007211319_InitialCreate'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'PermissionId', N'RoleId') AND [object_id] = OBJECT_ID(N'[dbo].[RolePermissions]'))
        SET IDENTITY_INSERT [dbo].[RolePermissions] ON;
    EXEC(N'INSERT INTO [dbo].[RolePermissions] ([PermissionId], [RoleId])
    VALUES (1, 1),
    (2, 1),
    (3, 1),
    (4, 1),
    (5, 1),
    (6, 1),
    (7, 1),
    (8, 1),
    (9, 1),
    (10, 1),
    (11, 1),
    (12, 1),
    (13, 1),
    (14, 1),
    (15, 1),
    (16, 1),
    (17, 1),
    (18, 1),
    (19, 1),
    (20, 1),
    (21, 1),
    (22, 1),
    (23, 1),
    (24, 1),
    (25, 1),
    (26, 1),
    (27, 1),
    (5, 2),
    (6, 2),
    (7, 2),
    (8, 2),
    (9, 2),
    (10, 2),
    (11, 2),
    (12, 2),
    (13, 2),
    (16, 2),
    (18, 2),
    (20, 2),
    (21, 2),
    (22, 2),
    (23, 2);
    INSERT INTO [dbo].[RolePermissions] ([PermissionId], [RoleId])
    VALUES (26, 2),
    (27, 2),
    (5, 3),
    (7, 3),
    (8, 3),
    (9, 3),
    (10, 3),
    (11, 3),
    (12, 3),
    (13, 3),
    (16, 3),
    (20, 3),
    (22, 3),
    (26, 3),
    (27, 3),
    (7, 4),
    (10, 4),
    (12, 4),
    (16, 4),
    (20, 4),
    (21, 4),
    (22, 4),
    (26, 4),
    (12, 5),
    (16, 5),
    (18, 5),
    (19, 5),
    (27, 5),
    (12, 6),
    (24, 6),
    (25, 6),
    (20, 7),
    (21, 7),
    (12, 8),
    (14, 8),
    (15, 8),
    (17, 8),
    (7, 9),
    (10, 9),
    (16, 9),
    (18, 9),
    (24, 9);
    INSERT INTO [dbo].[RolePermissions] ([PermissionId], [RoleId])
    VALUES (26, 9),
    (27, 9)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'PermissionId', N'RoleId') AND [object_id] = OBJECT_ID(N'[dbo].[RolePermissions]'))
        SET IDENTITY_INSERT [dbo].[RolePermissions] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007211319_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Colors_ColorCode] ON [dbo].[Colors] ([ColorCode]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007211319_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Customers_CustomerCode] ON [dbo].[Customers] ([CustomerCode]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007211319_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Machines_MachineCode] ON [dbo].[Machines] ([MachineCode]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007211319_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Machines_MachineTypeId] ON [dbo].[Machines] ([MachineTypeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007211319_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Machines_OperationId] ON [dbo].[Machines] ([OperationId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007211319_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Machines_Status] ON [dbo].[Machines] ([Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007211319_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_MachineTypes_Code] ON [dbo].[MachineTypes] ([Code]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007211319_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Models_CustomerId] ON [dbo].[Models] ([CustomerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007211319_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Models_ModelCode] ON [dbo].[Models] ([ModelCode]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007211319_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Operations_MachineTypeId] ON [dbo].[Operations] ([MachineTypeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007211319_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Operations_OperationCode] ON [dbo].[Operations] ([OperationCode]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007211319_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Operators_OperatorCode] ON [dbo].[Operators] ([OperatorCode]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007211319_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Permissions_Code] ON [dbo].[Permissions] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007211319_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Products_ModelId] ON [dbo].[Products] ([ModelId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007211319_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Products_ProductCode] ON [dbo].[Products] ([ProductCode]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007211319_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_RefreshTokens_TokenHash] ON [dbo].[RefreshTokens] ([TokenHash]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007211319_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_RefreshTokens_UserId_ExpiresAt] ON [dbo].[RefreshTokens] ([UserId], [ExpiresAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007211319_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_RolePermissions_PermissionId] ON [dbo].[RolePermissions] ([PermissionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007211319_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Roles_Code] ON [dbo].[Roles] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007211319_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Sizes_SizeCode] ON [dbo].[Sizes] ([SizeCode]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007211319_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_UserRoles_RoleId] ON [dbo].[UserRoles] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007211319_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Users_NormalizedUserName] ON [dbo].[Users] ([NormalizedUserName]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007211319_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261007211319_InitialCreate', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007213501_AddAuditLogs'
)
BEGIN
    CREATE TABLE [dbo].[AuditLogs] (
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
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007213501_AddAuditLogs'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'Description', N'Module') AND [object_id] = OBJECT_ID(N'[dbo].[Permissions]'))
        SET IDENTITY_INSERT [dbo].[Permissions] ON;
    EXEC(N'INSERT INTO [dbo].[Permissions] ([Id], [Code], [Description], [Module])
    VALUES (28, N''AuditLog.View'', N''İşlem kayıtlarını (audit log) görüntüleme'', N''AuditLog'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'Description', N'Module') AND [object_id] = OBJECT_ID(N'[dbo].[Permissions]'))
        SET IDENTITY_INSERT [dbo].[Permissions] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007213501_AddAuditLogs'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'PermissionId', N'RoleId') AND [object_id] = OBJECT_ID(N'[dbo].[RolePermissions]'))
        SET IDENTITY_INSERT [dbo].[RolePermissions] ON;
    EXEC(N'INSERT INTO [dbo].[RolePermissions] ([PermissionId], [RoleId])
    VALUES (28, 1)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'PermissionId', N'RoleId') AND [object_id] = OBJECT_ID(N'[dbo].[RolePermissions]'))
        SET IDENTITY_INSERT [dbo].[RolePermissions] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007213501_AddAuditLogs'
)
BEGIN
    CREATE INDEX [IX_AuditLogs_CreatedAt] ON [dbo].[AuditLogs] ([CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007213501_AddAuditLogs'
)
BEGIN
    CREATE INDEX [IX_AuditLogs_EntityName_EntityId_Id] ON [dbo].[AuditLogs] ([EntityName], [EntityId], [Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007213501_AddAuditLogs'
)
BEGIN
    CREATE INDEX [IX_AuditLogs_UserName_Id] ON [dbo].[AuditLogs] ([UserName], [Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007213501_AddAuditLogs'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261007213501_AddAuditLogs', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008044457_AddMasterDataIsActive'
)
BEGIN
    ALTER TABLE [dbo].[Sizes] ADD [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008044457_AddMasterDataIsActive'
)
BEGIN
    ALTER TABLE [dbo].[MachineTypes] ADD [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008044457_AddMasterDataIsActive'
)
BEGIN
    ALTER TABLE [dbo].[Colors] ADD [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008044457_AddMasterDataIsActive'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261008044457_AddMasterDataIsActive', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008050304_AddProductionOrders'
)
BEGIN
    CREATE TABLE [dbo].[DocumentSequences] (
        [Series] nvarchar(50) NOT NULL,
        [Year] int NOT NULL,
        [LastValue] int NOT NULL,
        CONSTRAINT [PK_DocumentSequences] PRIMARY KEY ([Series], [Year])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008050304_AddProductionOrders'
)
BEGIN
    CREATE TABLE [dbo].[ProductionOrders] (
        [Id] int NOT NULL IDENTITY,
        [OrderNo] nvarchar(30) NOT NULL,
        [CustomerId] int NOT NULL,
        [ProductId] int NOT NULL,
        [CustomerOrderNo] nvarchar(50) NULL,
        [OrderDate] date NOT NULL,
        [DueDate] date NOT NULL,
        [Status] nvarchar(30) NOT NULL,
        [Notes] nvarchar(1000) NULL,
        [RowVersion] rowversion NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_ProductionOrders] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_ProductionOrders_DueDate] CHECK ([DueDate] >= [OrderDate]),
        CONSTRAINT [CK_ProductionOrders_Status] CHECK ([Status] IN ('DRAFT', 'RELEASED', 'IN_PRODUCTION', 'COMPLETED', 'CANCELLED')),
        CONSTRAINT [FK_ProductionOrders_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [dbo].[Customers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ProductionOrders_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [dbo].[Products] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008050304_AddProductionOrders'
)
BEGIN
    CREATE TABLE [dbo].[ProductionOrderItems] (
        [Id] int NOT NULL IDENTITY,
        [ProductionOrderId] int NOT NULL,
        [ColorId] int NOT NULL,
        [SizeId] int NOT NULL,
        [Quantity] int NOT NULL,
        CONSTRAINT [PK_ProductionOrderItems] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_ProductionOrderItems_Quantity] CHECK ([Quantity] > 0),
        CONSTRAINT [FK_ProductionOrderItems_Colors_ColorId] FOREIGN KEY ([ColorId]) REFERENCES [dbo].[Colors] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ProductionOrderItems_ProductionOrders_ProductionOrderId] FOREIGN KEY ([ProductionOrderId]) REFERENCES [dbo].[ProductionOrders] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ProductionOrderItems_Sizes_SizeId] FOREIGN KEY ([SizeId]) REFERENCES [dbo].[Sizes] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008050304_AddProductionOrders'
)
BEGIN
    CREATE INDEX [IX_ProductionOrderItems_ColorId] ON [dbo].[ProductionOrderItems] ([ColorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008050304_AddProductionOrders'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ProductionOrderItems_ProductionOrderId_ColorId_SizeId] ON [dbo].[ProductionOrderItems] ([ProductionOrderId], [ColorId], [SizeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008050304_AddProductionOrders'
)
BEGIN
    CREATE INDEX [IX_ProductionOrderItems_SizeId] ON [dbo].[ProductionOrderItems] ([SizeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008050304_AddProductionOrders'
)
BEGIN
    CREATE INDEX [IX_ProductionOrders_CustomerId] ON [dbo].[ProductionOrders] ([CustomerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008050304_AddProductionOrders'
)
BEGIN
    CREATE INDEX [IX_ProductionOrders_DueDate] ON [dbo].[ProductionOrders] ([DueDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008050304_AddProductionOrders'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ProductionOrders_OrderNo] ON [dbo].[ProductionOrders] ([OrderNo]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008050304_AddProductionOrders'
)
BEGIN
    CREATE INDEX [IX_ProductionOrders_ProductId] ON [dbo].[ProductionOrders] ([ProductId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008050304_AddProductionOrders'
)
BEGIN
    CREATE INDEX [IX_ProductionOrders_Status_DueDate] ON [dbo].[ProductionOrders] ([Status], [DueDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008050304_AddProductionOrders'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261008050304_AddProductionOrders', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008051541_AddProductionRoutes'
)
BEGIN
    CREATE TABLE [dbo].[ProductionRoutes] (
        [Id] int NOT NULL IDENTITY,
        [ModelId] int NOT NULL,
        [Version] int NOT NULL,
        [Name] nvarchar(100) NULL,
        [Description] nvarchar(500) NULL,
        [Status] nvarchar(30) NOT NULL,
        [RowVersion] rowversion NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_ProductionRoutes] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_ProductionRoutes_Status] CHECK ([Status] IN ('DRAFT', 'ACTIVE', 'ARCHIVED')),
        CONSTRAINT [CK_ProductionRoutes_Version] CHECK ([Version] > 0),
        CONSTRAINT [FK_ProductionRoutes_Models_ModelId] FOREIGN KEY ([ModelId]) REFERENCES [dbo].[Models] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008051541_AddProductionRoutes'
)
BEGIN
    CREATE TABLE [dbo].[ProductionRouteOperations] (
        [Id] int NOT NULL IDENTITY,
        [ProductionRouteId] int NOT NULL,
        [SequenceNo] int NOT NULL,
        [OperationId] int NOT NULL,
        [Sam] decimal(9,4) NOT NULL,
        CONSTRAINT [PK_ProductionRouteOperations] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_ProductionRouteOperations_Sam] CHECK ([Sam] >= 0),
        CONSTRAINT [CK_ProductionRouteOperations_SequenceNo] CHECK ([SequenceNo] > 0),
        CONSTRAINT [FK_ProductionRouteOperations_Operations_OperationId] FOREIGN KEY ([OperationId]) REFERENCES [dbo].[Operations] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ProductionRouteOperations_ProductionRoutes_ProductionRouteId] FOREIGN KEY ([ProductionRouteId]) REFERENCES [dbo].[ProductionRoutes] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008051541_AddProductionRoutes'
)
BEGIN
    CREATE TABLE [dbo].[OperationDependencies] (
        [RouteOperationId] int NOT NULL,
        [PredecessorId] int NOT NULL,
        CONSTRAINT [PK_OperationDependencies] PRIMARY KEY ([RouteOperationId], [PredecessorId]),
        CONSTRAINT [CK_OperationDependencies_NotSelf] CHECK ([RouteOperationId] <> [PredecessorId]),
        CONSTRAINT [FK_OperationDependencies_ProductionRouteOperations_PredecessorId] FOREIGN KEY ([PredecessorId]) REFERENCES [dbo].[ProductionRouteOperations] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_OperationDependencies_ProductionRouteOperations_RouteOperationId] FOREIGN KEY ([RouteOperationId]) REFERENCES [dbo].[ProductionRouteOperations] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008051541_AddProductionRoutes'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'Description', N'Module') AND [object_id] = OBJECT_ID(N'[dbo].[Permissions]'))
        SET IDENTITY_INSERT [dbo].[Permissions] ON;
    EXEC(N'INSERT INTO [dbo].[Permissions] ([Id], [Code], [Description], [Module])
    VALUES (29, N''ProductionRoute.View'', N''Operasyon rotalarını görüntüleme'', N''ProductionRoute''),
    (30, N''ProductionRoute.Manage'', N''Operasyon rotası oluşturma / etkinleştirme'', N''ProductionRoute'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'Description', N'Module') AND [object_id] = OBJECT_ID(N'[dbo].[Permissions]'))
        SET IDENTITY_INSERT [dbo].[Permissions] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008051541_AddProductionRoutes'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'PermissionId', N'RoleId') AND [object_id] = OBJECT_ID(N'[dbo].[RolePermissions]'))
        SET IDENTITY_INSERT [dbo].[RolePermissions] ON;
    EXEC(N'INSERT INTO [dbo].[RolePermissions] ([PermissionId], [RoleId])
    VALUES (29, 1),
    (30, 1),
    (29, 2),
    (30, 2),
    (29, 3),
    (30, 3),
    (29, 4)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'PermissionId', N'RoleId') AND [object_id] = OBJECT_ID(N'[dbo].[RolePermissions]'))
        SET IDENTITY_INSERT [dbo].[RolePermissions] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008051541_AddProductionRoutes'
)
BEGIN
    CREATE INDEX [IX_OperationDependencies_PredecessorId] ON [dbo].[OperationDependencies] ([PredecessorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008051541_AddProductionRoutes'
)
BEGIN
    CREATE INDEX [IX_ProductionRouteOperations_OperationId] ON [dbo].[ProductionRouteOperations] ([OperationId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008051541_AddProductionRoutes'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ProductionRouteOperations_ProductionRouteId_SequenceNo] ON [dbo].[ProductionRouteOperations] ([ProductionRouteId], [SequenceNo]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008051541_AddProductionRoutes'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ProductionRoutes_ModelId_Version] ON [dbo].[ProductionRoutes] ([ModelId], [Version]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008051541_AddProductionRoutes'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_ProductionRoutes_ActivePerModel] ON [dbo].[ProductionRoutes] ([ModelId]) WHERE [Status] = ''ACTIVE'' AND [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008051541_AddProductionRoutes'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261008051541_AddProductionRoutes', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008052636_AddProductionLines'
)
BEGIN
    CREATE TABLE [dbo].[Factories] (
        [Id] int NOT NULL IDENTITY,
        [Code] nvarchar(30) NOT NULL,
        [Name] nvarchar(150) NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_Factories] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008052636_AddProductionLines'
)
BEGIN
    CREATE TABLE [dbo].[ProductionAreas] (
        [Id] int NOT NULL IDENTITY,
        [Code] nvarchar(30) NOT NULL,
        [Name] nvarchar(150) NOT NULL,
        [FactoryId] int NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_ProductionAreas] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ProductionAreas_Factories_FactoryId] FOREIGN KEY ([FactoryId]) REFERENCES [dbo].[Factories] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008052636_AddProductionLines'
)
BEGIN
    CREATE TABLE [dbo].[ProductionLines] (
        [Id] int NOT NULL IDENTITY,
        [LineCode] nvarchar(30) NOT NULL,
        [Name] nvarchar(150) NOT NULL,
        [ProductionAreaId] int NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_ProductionLines] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ProductionLines_ProductionAreas_ProductionAreaId] FOREIGN KEY ([ProductionAreaId]) REFERENCES [dbo].[ProductionAreas] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008052636_AddProductionLines'
)
BEGIN
    CREATE TABLE [dbo].[LineMachineAssignments] (
        [Id] int NOT NULL IDENTITY,
        [ProductionLineId] int NOT NULL,
        [MachineId] int NOT NULL,
        [Position] int NOT NULL,
        [AssignedAt] datetime2 NOT NULL,
        [UnassignedAt] datetime2 NULL,
        CONSTRAINT [PK_LineMachineAssignments] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_LineMachineAssignments_Period] CHECK ([UnassignedAt] IS NULL OR [UnassignedAt] >= [AssignedAt]),
        CONSTRAINT [CK_LineMachineAssignments_Position] CHECK ([Position] > 0),
        CONSTRAINT [FK_LineMachineAssignments_Machines_MachineId] FOREIGN KEY ([MachineId]) REFERENCES [dbo].[Machines] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_LineMachineAssignments_ProductionLines_ProductionLineId] FOREIGN KEY ([ProductionLineId]) REFERENCES [dbo].[ProductionLines] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008052636_AddProductionLines'
)
BEGIN
    CREATE TABLE [dbo].[LineProductionPlans] (
        [Id] int NOT NULL IDENTITY,
        [ProductionLineId] int NOT NULL,
        [ProductionOrderId] int NOT NULL,
        [PlannedStart] datetime2 NOT NULL,
        [PlannedEnd] datetime2 NOT NULL,
        [PlannedQuantity] int NOT NULL,
        [Status] nvarchar(30) NOT NULL,
        [Notes] nvarchar(500) NULL,
        [RowVersion] rowversion NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_LineProductionPlans] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_LineProductionPlans_Period] CHECK ([PlannedEnd] > [PlannedStart]),
        CONSTRAINT [CK_LineProductionPlans_Quantity] CHECK ([PlannedQuantity] > 0),
        CONSTRAINT [CK_LineProductionPlans_Status] CHECK ([Status] IN ('PLANNED', 'COMPLETED', 'CANCELLED')),
        CONSTRAINT [FK_LineProductionPlans_ProductionLines_ProductionLineId] FOREIGN KEY ([ProductionLineId]) REFERENCES [dbo].[ProductionLines] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_LineProductionPlans_ProductionOrders_ProductionOrderId] FOREIGN KEY ([ProductionOrderId]) REFERENCES [dbo].[ProductionOrders] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008052636_AddProductionLines'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Factories_Code] ON [dbo].[Factories] ([Code]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008052636_AddProductionLines'
)
BEGIN
    CREATE INDEX [IX_LineMachineAssignments_MachineId_AssignedAt] ON [dbo].[LineMachineAssignments] ([MachineId], [AssignedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008052636_AddProductionLines'
)
BEGIN
    CREATE INDEX [IX_LineMachineAssignments_ProductionLineId_UnassignedAt_Position] ON [dbo].[LineMachineAssignments] ([ProductionLineId], [UnassignedAt], [Position]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008052636_AddProductionLines'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_LineMachineAssignments_CurrentPerMachine] ON [dbo].[LineMachineAssignments] ([MachineId]) WHERE [UnassignedAt] IS NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008052636_AddProductionLines'
)
BEGIN
    CREATE INDEX [IX_LineProductionPlans_ProductionLineId_PlannedStart_PlannedEnd] ON [dbo].[LineProductionPlans] ([ProductionLineId], [PlannedStart], [PlannedEnd]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008052636_AddProductionLines'
)
BEGIN
    CREATE INDEX [IX_LineProductionPlans_ProductionOrderId_Status] ON [dbo].[LineProductionPlans] ([ProductionOrderId], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008052636_AddProductionLines'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_ProductionAreas_Code] ON [dbo].[ProductionAreas] ([Code]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008052636_AddProductionLines'
)
BEGIN
    CREATE INDEX [IX_ProductionAreas_FactoryId] ON [dbo].[ProductionAreas] ([FactoryId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008052636_AddProductionLines'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_ProductionLines_LineCode] ON [dbo].[ProductionLines] ([LineCode]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008052636_AddProductionLines'
)
BEGIN
    CREATE INDEX [IX_ProductionLines_ProductionAreaId] ON [dbo].[ProductionLines] ([ProductionAreaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008052636_AddProductionLines'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261008052636_AddProductionLines', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008053718_AddTablets'
)
BEGIN
    CREATE TABLE [dbo].[MachineStatusLogs] (
        [Id] bigint NOT NULL IDENTITY,
        [MachineId] int NOT NULL,
        [Status] nvarchar(30) NOT NULL,
        [StartedAt] datetime2 NOT NULL,
        [EndedAt] datetime2 NULL,
        [Reason] nvarchar(300) NULL,
        [ChangedBy] nvarchar(100) NOT NULL,
        [TabletId] int NULL,
        CONSTRAINT [PK_MachineStatusLogs] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_MachineStatusLogs_Period] CHECK ([EndedAt] IS NULL OR [EndedAt] >= [StartedAt]),
        CONSTRAINT [CK_MachineStatusLogs_Status] CHECK ([Status] IN ('AVAILABLE', 'RUNNING', 'IDLE', 'BREAKDOWN', 'MAINTENANCE', 'OFFLINE')),
        CONSTRAINT [FK_MachineStatusLogs_Machines_MachineId] FOREIGN KEY ([MachineId]) REFERENCES [dbo].[Machines] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008053718_AddTablets'
)
BEGIN
    CREATE TABLE [dbo].[Tablets] (
        [Id] int NOT NULL IDENTITY,
        [TabletCode] nvarchar(30) NOT NULL,
        [Name] nvarchar(150) NULL,
        [MachineId] int NULL,
        [IsActive] bit NOT NULL,
        [DeviceIdentifier] nvarchar(200) NULL,
        [DeviceKeyHash] varchar(64) NULL,
        [EnrollmentCodeHash] varchar(64) NULL,
        [EnrollmentCodeExpiresAt] datetime2 NULL,
        [EnrollmentFailedAttempts] int NOT NULL,
        [LastSeenAt] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_Tablets] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Tablets_Machines_MachineId] FOREIGN KEY ([MachineId]) REFERENCES [dbo].[Machines] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008053718_AddTablets'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'Description', N'Module') AND [object_id] = OBJECT_ID(N'[dbo].[Permissions]'))
        SET IDENTITY_INSERT [dbo].[Permissions] ON;
    EXEC(N'INSERT INTO [dbo].[Permissions] ([Id], [Code], [Description], [Module])
    VALUES (31, N''Tablet.View'', N''Tabletleri görüntüleme'', N''Tablet''),
    (32, N''Tablet.Manage'', N''Tablet tanımlama / cihaz eşleştirme'', N''Tablet'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'Description', N'Module') AND [object_id] = OBJECT_ID(N'[dbo].[Permissions]'))
        SET IDENTITY_INSERT [dbo].[Permissions] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008053718_AddTablets'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'PermissionId', N'RoleId') AND [object_id] = OBJECT_ID(N'[dbo].[RolePermissions]'))
        SET IDENTITY_INSERT [dbo].[RolePermissions] ON;
    EXEC(N'INSERT INTO [dbo].[RolePermissions] ([PermissionId], [RoleId])
    VALUES (31, 1),
    (32, 1),
    (31, 2),
    (32, 2),
    (31, 4),
    (31, 7)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'PermissionId', N'RoleId') AND [object_id] = OBJECT_ID(N'[dbo].[RolePermissions]'))
        SET IDENTITY_INSERT [dbo].[RolePermissions] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008053718_AddTablets'
)
BEGIN
    CREATE INDEX [IX_MachineStatusLogs_MachineId_StartedAt] ON [dbo].[MachineStatusLogs] ([MachineId], [StartedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008053718_AddTablets'
)
BEGIN
    CREATE INDEX [IX_MachineStatusLogs_Status_StartedAt] ON [dbo].[MachineStatusLogs] ([Status], [StartedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008053718_AddTablets'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_MachineStatusLogs_OpenPerMachine] ON [dbo].[MachineStatusLogs] ([MachineId]) WHERE [EndedAt] IS NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008053718_AddTablets'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Tablets_TabletCode] ON [dbo].[Tablets] ([TabletCode]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008053718_AddTablets'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_Tablets_DeviceIdentifier] ON [dbo].[Tablets] ([DeviceIdentifier]) WHERE [DeviceIdentifier] IS NOT NULL AND [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008053718_AddTablets'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_Tablets_Machine] ON [dbo].[Tablets] ([MachineId]) WHERE [MachineId] IS NOT NULL AND [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008053718_AddTablets'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261008053718_AddTablets', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008054638_AddBundles'
)
BEGIN
    CREATE TABLE [dbo].[Bundles] (
        [Id] int NOT NULL IDENTITY,
        [BundleNo] varchar(20) NOT NULL,
        [ProductionOrderId] int NOT NULL,
        [ColorId] int NOT NULL,
        [SizeId] int NOT NULL,
        [Quantity] int NOT NULL,
        [ProductionRouteId] int NOT NULL,
        [Status] nvarchar(30) NOT NULL,
        [RowVersion] rowversion NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_Bundles] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Bundles_Quantity] CHECK ([Quantity] > 0),
        CONSTRAINT [CK_Bundles_Status] CHECK ([Status] IN ('CREATED', 'WAITING', 'IN_PRODUCTION', 'PARTIALLY_COMPLETED', 'COMPLETED', 'QUALITY_HOLD', 'REWORK', 'CANCELLED', 'CLOSED')),
        CONSTRAINT [FK_Bundles_Colors_ColorId] FOREIGN KEY ([ColorId]) REFERENCES [dbo].[Colors] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Bundles_ProductionOrders_ProductionOrderId] FOREIGN KEY ([ProductionOrderId]) REFERENCES [dbo].[ProductionOrders] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Bundles_ProductionRoutes_ProductionRouteId] FOREIGN KEY ([ProductionRouteId]) REFERENCES [dbo].[ProductionRoutes] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Bundles_Sizes_SizeId] FOREIGN KEY ([SizeId]) REFERENCES [dbo].[Sizes] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008054638_AddBundles'
)
BEGIN
    CREATE TABLE [dbo].[BundleOperations] (
        [Id] bigint NOT NULL IDENTITY,
        [BundleId] int NOT NULL,
        [RouteOperationId] int NOT NULL,
        [OperationId] int NOT NULL,
        [SequenceNo] int NOT NULL,
        [Sam] decimal(9,4) NOT NULL,
        [Status] nvarchar(30) NOT NULL,
        [PlannedQuantity] int NOT NULL,
        [ProducedQuantity] int NOT NULL,
        [RejectedQuantity] int NOT NULL,
        [MachineId] int NULL,
        [OperatorId] int NULL,
        [StartedAt] datetime2 NULL,
        [CompletedAt] datetime2 NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_BundleOperations] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_BundleOperations_Quantities] CHECK ([PlannedQuantity] > 0 AND [ProducedQuantity] >= 0 AND [RejectedQuantity] >= 0 AND [ProducedQuantity] + [RejectedQuantity] <= [PlannedQuantity]),
        CONSTRAINT [CK_BundleOperations_Status] CHECK ([Status] IN ('WAITING', 'READY', 'IN_PROGRESS', 'PAUSED', 'COMPLETED', 'QUALITY_HOLD', 'CANCELLED')),
        CONSTRAINT [FK_BundleOperations_Bundles_BundleId] FOREIGN KEY ([BundleId]) REFERENCES [dbo].[Bundles] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_BundleOperations_Machines_MachineId] FOREIGN KEY ([MachineId]) REFERENCES [dbo].[Machines] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_BundleOperations_Operations_OperationId] FOREIGN KEY ([OperationId]) REFERENCES [dbo].[Operations] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_BundleOperations_Operators_OperatorId] FOREIGN KEY ([OperatorId]) REFERENCES [dbo].[Operators] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_BundleOperations_ProductionRouteOperations_RouteOperationId] FOREIGN KEY ([RouteOperationId]) REFERENCES [dbo].[ProductionRouteOperations] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008054638_AddBundles'
)
BEGIN
    CREATE UNIQUE INDEX [IX_BundleOperations_BundleId_RouteOperationId] ON [dbo].[BundleOperations] ([BundleId], [RouteOperationId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008054638_AddBundles'
)
BEGIN
    CREATE INDEX [IX_BundleOperations_BundleId_SequenceNo] ON [dbo].[BundleOperations] ([BundleId], [SequenceNo]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008054638_AddBundles'
)
BEGIN
    CREATE INDEX [IX_BundleOperations_MachineId_StartedAt] ON [dbo].[BundleOperations] ([MachineId], [StartedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008054638_AddBundles'
)
BEGIN
    CREATE INDEX [IX_BundleOperations_OperationId_Status] ON [dbo].[BundleOperations] ([OperationId], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008054638_AddBundles'
)
BEGIN
    CREATE INDEX [IX_BundleOperations_OperatorId_StartedAt] ON [dbo].[BundleOperations] ([OperatorId], [StartedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008054638_AddBundles'
)
BEGIN
    CREATE INDEX [IX_BundleOperations_RouteOperationId] ON [dbo].[BundleOperations] ([RouteOperationId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008054638_AddBundles'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Bundles_BundleNo] ON [dbo].[Bundles] ([BundleNo]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008054638_AddBundles'
)
BEGIN
    CREATE INDEX [IX_Bundles_ColorId] ON [dbo].[Bundles] ([ColorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008054638_AddBundles'
)
BEGIN
    CREATE INDEX [IX_Bundles_ProductionOrderId_ColorId_SizeId_Status] ON [dbo].[Bundles] ([ProductionOrderId], [ColorId], [SizeId], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008054638_AddBundles'
)
BEGIN
    CREATE INDEX [IX_Bundles_ProductionRouteId] ON [dbo].[Bundles] ([ProductionRouteId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008054638_AddBundles'
)
BEGIN
    CREATE INDEX [IX_Bundles_SizeId] ON [dbo].[Bundles] ([SizeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008054638_AddBundles'
)
BEGIN
    CREATE INDEX [IX_Bundles_Status] ON [dbo].[Bundles] ([Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008054638_AddBundles'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261008054638_AddBundles', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008055839_AddRfid'
)
BEGIN
    CREATE TABLE [dbo].[RfidCards] (
        [Id] int NOT NULL IDENTITY,
        [Uid] varchar(47) NOT NULL,
        [CardType] nvarchar(30) NOT NULL,
        [Label] nvarchar(50) NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_RfidCards] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_RfidCards_CardType] CHECK ([CardType] IN ('BUNDLE', 'OPERATOR'))
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008055839_AddRfid'
)
BEGIN
    CREATE TABLE [dbo].[RfidReadLogs] (
        [Id] bigint NOT NULL IDENTITY,
        [ReadId] uniqueidentifier NOT NULL,
        [TabletId] int NOT NULL,
        [Uid] varchar(47) NOT NULL,
        [Kind] nvarchar(30) NOT NULL,
        [ResultCode] varchar(50) NOT NULL,
        [OperatorId] int NULL,
        [BundleId] int NULL,
        [ReadAt] datetime2 NOT NULL,
        CONSTRAINT [PK_RfidReadLogs] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_RfidReadLogs_Kind] CHECK ([Kind] IN ('OPERATOR', 'BUNDLE'))
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008055839_AddRfid'
)
BEGIN
    CREATE TABLE [dbo].[BundleCardAssignments] (
        [Id] int NOT NULL IDENTITY,
        [RfidCardId] int NOT NULL,
        [BundleId] int NOT NULL,
        [AssignedAt] datetime2 NOT NULL,
        [ReleasedAt] datetime2 NULL,
        CONSTRAINT [PK_BundleCardAssignments] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_BundleCardAssignments_Period] CHECK ([ReleasedAt] IS NULL OR [ReleasedAt] >= [AssignedAt]),
        CONSTRAINT [FK_BundleCardAssignments_Bundles_BundleId] FOREIGN KEY ([BundleId]) REFERENCES [dbo].[Bundles] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_BundleCardAssignments_RfidCards_RfidCardId] FOREIGN KEY ([RfidCardId]) REFERENCES [dbo].[RfidCards] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008055839_AddRfid'
)
BEGIN
    CREATE TABLE [dbo].[OperatorCards] (
        [Id] int NOT NULL IDENTITY,
        [OperatorId] int NOT NULL,
        [RfidCardId] int NOT NULL,
        [AssignedAt] datetime2 NOT NULL,
        [RevokedAt] datetime2 NULL,
        CONSTRAINT [PK_OperatorCards] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_OperatorCards_Period] CHECK ([RevokedAt] IS NULL OR [RevokedAt] >= [AssignedAt]),
        CONSTRAINT [FK_OperatorCards_Operators_OperatorId] FOREIGN KEY ([OperatorId]) REFERENCES [dbo].[Operators] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_OperatorCards_RfidCards_RfidCardId] FOREIGN KEY ([RfidCardId]) REFERENCES [dbo].[RfidCards] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008055839_AddRfid'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'Description', N'Module') AND [object_id] = OBJECT_ID(N'[dbo].[Permissions]'))
        SET IDENTITY_INSERT [dbo].[Permissions] ON;
    EXEC(N'INSERT INTO [dbo].[Permissions] ([Id], [Code], [Description], [Module])
    VALUES (33, N''RfidCard.View'', N''RFID kartlarını görüntüleme'', N''RfidCard''),
    (34, N''RfidCard.Manage'', N''RFID kartı tanımlama'', N''RfidCard'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'Description', N'Module') AND [object_id] = OBJECT_ID(N'[dbo].[Permissions]'))
        SET IDENTITY_INSERT [dbo].[Permissions] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008055839_AddRfid'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'PermissionId', N'RoleId') AND [object_id] = OBJECT_ID(N'[dbo].[RolePermissions]'))
        SET IDENTITY_INSERT [dbo].[RolePermissions] ON;
    EXEC(N'INSERT INTO [dbo].[RolePermissions] ([PermissionId], [RoleId])
    VALUES (33, 1),
    (34, 1),
    (33, 2),
    (34, 2),
    (33, 3),
    (34, 3),
    (33, 4)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'PermissionId', N'RoleId') AND [object_id] = OBJECT_ID(N'[dbo].[RolePermissions]'))
        SET IDENTITY_INSERT [dbo].[RolePermissions] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008055839_AddRfid'
)
BEGIN
    CREATE INDEX [IX_BundleCardAssignments_BundleId_AssignedAt] ON [dbo].[BundleCardAssignments] ([BundleId], [AssignedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008055839_AddRfid'
)
BEGIN
    CREATE INDEX [IX_BundleCardAssignments_RfidCardId_AssignedAt] ON [dbo].[BundleCardAssignments] ([RfidCardId], [AssignedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008055839_AddRfid'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_BundleCardAssignments_CurrentPerBundle] ON [dbo].[BundleCardAssignments] ([BundleId]) WHERE [ReleasedAt] IS NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008055839_AddRfid'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_BundleCardAssignments_CurrentPerCard] ON [dbo].[BundleCardAssignments] ([RfidCardId]) WHERE [ReleasedAt] IS NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008055839_AddRfid'
)
BEGIN
    CREATE INDEX [IX_OperatorCards_OperatorId_AssignedAt] ON [dbo].[OperatorCards] ([OperatorId], [AssignedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008055839_AddRfid'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_OperatorCards_CurrentPerCard] ON [dbo].[OperatorCards] ([RfidCardId]) WHERE [RevokedAt] IS NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008055839_AddRfid'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_OperatorCards_CurrentPerOperator] ON [dbo].[OperatorCards] ([OperatorId]) WHERE [RevokedAt] IS NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008055839_AddRfid'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_RfidCards_Uid] ON [dbo].[RfidCards] ([Uid]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008055839_AddRfid'
)
BEGIN
    CREATE INDEX [IX_RfidReadLogs_ReadAt] ON [dbo].[RfidReadLogs] ([ReadAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008055839_AddRfid'
)
BEGIN
    CREATE UNIQUE INDEX [IX_RfidReadLogs_ReadId] ON [dbo].[RfidReadLogs] ([ReadId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008055839_AddRfid'
)
BEGIN
    CREATE INDEX [IX_RfidReadLogs_TabletId_Uid_Kind_ReadAt] ON [dbo].[RfidReadLogs] ([TabletId], [Uid], [Kind], [ReadAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008055839_AddRfid'
)
BEGIN
    CREATE INDEX [IX_RfidReadLogs_Uid_ReadAt] ON [dbo].[RfidReadLogs] ([Uid], [ReadAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008055839_AddRfid'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261008055839_AddRfid', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008061419_AddOperationEngine'
)
BEGIN
    CREATE TABLE [dbo].[BundleOperationEvents] (
        [Id] bigint NOT NULL IDENTITY,
        [RequestId] uniqueidentifier NOT NULL,
        [EventType] nvarchar(30) NOT NULL,
        [BundleId] int NOT NULL,
        [BundleOperationId] bigint NOT NULL,
        [MachineId] int NOT NULL,
        [OperatorId] int NOT NULL,
        [TabletId] int NULL,
        [ProducedQuantity] int NOT NULL,
        [RejectedQuantity] int NOT NULL,
        [OccurredAt] datetime2 NOT NULL,
        CONSTRAINT [PK_BundleOperationEvents] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_BundleOperationEvents_EventType] CHECK ([EventType] IN ('OPERATION_STARTED', 'OPERATION_RESUMED', 'OPERATION_PAUSED', 'OPERATION_COMPLETED', 'QUANTITY_REPORTED', 'REJECTION_REPORTED')),
        CONSTRAINT [CK_BundleOperationEvents_Quantities] CHECK ([ProducedQuantity] >= 0 AND [RejectedQuantity] >= 0),
        CONSTRAINT [FK_BundleOperationEvents_BundleOperations_BundleOperationId] FOREIGN KEY ([BundleOperationId]) REFERENCES [dbo].[BundleOperations] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_BundleOperationEvents_Bundles_BundleId] FOREIGN KEY ([BundleId]) REFERENCES [dbo].[Bundles] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_BundleOperationEvents_Machines_MachineId] FOREIGN KEY ([MachineId]) REFERENCES [dbo].[Machines] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_BundleOperationEvents_Operators_OperatorId] FOREIGN KEY ([OperatorId]) REFERENCES [dbo].[Operators] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008061419_AddOperationEngine'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_BundleOperations_InProgressPerBundle] ON [dbo].[BundleOperations] ([BundleId]) WHERE [Status] = ''IN_PROGRESS''');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008061419_AddOperationEngine'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_BundleOperations_InProgressPerMachine] ON [dbo].[BundleOperations] ([MachineId]) WHERE [Status] = ''IN_PROGRESS'' AND [MachineId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008061419_AddOperationEngine'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_BundleOperations_InProgressPerOperator] ON [dbo].[BundleOperations] ([OperatorId]) WHERE [Status] = ''IN_PROGRESS'' AND [OperatorId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008061419_AddOperationEngine'
)
BEGIN
    CREATE INDEX [IX_BundleOperationEvents_BundleId_Id] ON [dbo].[BundleOperationEvents] ([BundleId], [Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008061419_AddOperationEngine'
)
BEGIN
    CREATE INDEX [IX_BundleOperationEvents_BundleOperationId_Id] ON [dbo].[BundleOperationEvents] ([BundleOperationId], [Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008061419_AddOperationEngine'
)
BEGIN
    CREATE INDEX [IX_BundleOperationEvents_MachineId_OccurredAt] ON [dbo].[BundleOperationEvents] ([MachineId], [OccurredAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008061419_AddOperationEngine'
)
BEGIN
    CREATE INDEX [IX_BundleOperationEvents_OccurredAt_EventType] ON [dbo].[BundleOperationEvents] ([OccurredAt], [EventType]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008061419_AddOperationEngine'
)
BEGIN
    CREATE INDEX [IX_BundleOperationEvents_OperatorId_OccurredAt] ON [dbo].[BundleOperationEvents] ([OperatorId], [OccurredAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008061419_AddOperationEngine'
)
BEGIN
    CREATE UNIQUE INDEX [IX_BundleOperationEvents_RequestId_EventType] ON [dbo].[BundleOperationEvents] ([RequestId], [EventType]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008061419_AddOperationEngine'
)
BEGIN
    EXEC(N'CREATE TRIGGER [dbo].[TR_BundleOperationEvents_AppendOnly]
    ON [dbo].[BundleOperationEvents]
    INSTEAD OF UPDATE, DELETE
    AS
    BEGIN
        SET NOCOUNT ON;
        THROW 51000, ''BundleOperationEvents is append-only: production events cannot be updated or deleted.'', 1;
    END');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008061419_AddOperationEngine'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261008061419_AddOperationEngine', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008064838_AddWorkSessions'
)
BEGIN
    ALTER TABLE [dbo].[BundleOperationEvents] ADD [WorkSessionId] bigint NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008064838_AddWorkSessions'
)
BEGIN
    CREATE TABLE [dbo].[OperatorMachines] (
        [OperatorId] int NOT NULL,
        [MachineId] int NOT NULL,
        CONSTRAINT [PK_OperatorMachines] PRIMARY KEY ([OperatorId], [MachineId]),
        CONSTRAINT [FK_OperatorMachines_Machines_MachineId] FOREIGN KEY ([MachineId]) REFERENCES [dbo].[Machines] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_OperatorMachines_Operators_OperatorId] FOREIGN KEY ([OperatorId]) REFERENCES [dbo].[Operators] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008064838_AddWorkSessions'
)
BEGIN
    CREATE TABLE [dbo].[OperatorOperationQualifications] (
        [OperatorId] int NOT NULL,
        [OperationId] int NOT NULL,
        CONSTRAINT [PK_OperatorOperationQualifications] PRIMARY KEY ([OperatorId], [OperationId]),
        CONSTRAINT [FK_OperatorOperationQualifications_Operations_OperationId] FOREIGN KEY ([OperationId]) REFERENCES [dbo].[Operations] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_OperatorOperationQualifications_Operators_OperatorId] FOREIGN KEY ([OperatorId]) REFERENCES [dbo].[Operators] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008064838_AddWorkSessions'
)
BEGIN
    CREATE TABLE [dbo].[WorkSessions] (
        [Id] bigint NOT NULL IDENTITY,
        [OperatorId] int NOT NULL,
        [MachineId] int NOT NULL,
        [TabletId] int NOT NULL,
        [LoginAt] datetime2 NOT NULL,
        [LogoutAt] datetime2 NULL,
        [Status] nvarchar(30) NOT NULL,
        [EndReason] nvarchar(30) NULL,
        [ClosedBy] nvarchar(100) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_WorkSessions] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_WorkSessions_EndReason] CHECK ([EndReason] IS NULL OR [EndReason] IN ('LOGOUT', 'TAKEN_OVER', 'MOVED_TO_ANOTHER_MACHINE', 'CLOSED_BY_SUPERVISOR')),
        CONSTRAINT [CK_WorkSessions_Period] CHECK ([LogoutAt] IS NULL OR [LogoutAt] >= [LoginAt]),
        CONSTRAINT [CK_WorkSessions_Status] CHECK ([Status] IN ('ACTIVE', 'LOGGED_OUT', 'BREAK', 'PAUSED', 'MACHINE_BREAKDOWN', 'WAITING_FOR_BUNDLE')),
        CONSTRAINT [FK_WorkSessions_Machines_MachineId] FOREIGN KEY ([MachineId]) REFERENCES [dbo].[Machines] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_WorkSessions_Operators_OperatorId] FOREIGN KEY ([OperatorId]) REFERENCES [dbo].[Operators] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_WorkSessions_Tablets_TabletId] FOREIGN KEY ([TabletId]) REFERENCES [dbo].[Tablets] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008064838_AddWorkSessions'
)
BEGIN
    CREATE INDEX [IX_BundleOperationEvents_WorkSessionId] ON [dbo].[BundleOperationEvents] ([WorkSessionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008064838_AddWorkSessions'
)
BEGIN
    CREATE INDEX [IX_OperatorMachines_MachineId] ON [dbo].[OperatorMachines] ([MachineId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008064838_AddWorkSessions'
)
BEGIN
    CREATE INDEX [IX_OperatorOperationQualifications_OperationId] ON [dbo].[OperatorOperationQualifications] ([OperationId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008064838_AddWorkSessions'
)
BEGIN
    CREATE INDEX [IX_WorkSessions_LoginAt] ON [dbo].[WorkSessions] ([LoginAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008064838_AddWorkSessions'
)
BEGIN
    CREATE INDEX [IX_WorkSessions_MachineId_LoginAt] ON [dbo].[WorkSessions] ([MachineId], [LoginAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008064838_AddWorkSessions'
)
BEGIN
    CREATE INDEX [IX_WorkSessions_OperatorId_LoginAt] ON [dbo].[WorkSessions] ([OperatorId], [LoginAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008064838_AddWorkSessions'
)
BEGIN
    CREATE INDEX [IX_WorkSessions_TabletId] ON [dbo].[WorkSessions] ([TabletId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008064838_AddWorkSessions'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_WorkSessions_OpenPerMachine] ON [dbo].[WorkSessions] ([MachineId]) WHERE [LogoutAt] IS NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008064838_AddWorkSessions'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_WorkSessions_OpenPerOperator] ON [dbo].[WorkSessions] ([OperatorId]) WHERE [LogoutAt] IS NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008064838_AddWorkSessions'
)
BEGIN
    ALTER TABLE [dbo].[BundleOperationEvents] ADD CONSTRAINT [FK_BundleOperationEvents_WorkSessions_WorkSessionId] FOREIGN KEY ([WorkSessionId]) REFERENCES [dbo].[WorkSessions] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008064838_AddWorkSessions'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261008064838_AddWorkSessions', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008073225_AddShiftsAndPauses'
)
BEGIN
    ALTER TABLE [dbo].[WorkSessions] DROP CONSTRAINT [CK_WorkSessions_EndReason];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008073225_AddShiftsAndPauses'
)
BEGIN
    ALTER TABLE [dbo].[Operators] ADD [BreakGroupId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008073225_AddShiftsAndPauses'
)
BEGIN
    CREATE TABLE [dbo].[BreakGroups] (
        [Id] int NOT NULL IDENTITY,
        [Code] nvarchar(30) NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_BreakGroups] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008073225_AddShiftsAndPauses'
)
BEGIN
    CREATE TABLE [dbo].[Shifts] (
        [Id] int NOT NULL IDENTITY,
        [Code] nvarchar(30) NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [StartTime] time(0) NOT NULL,
        [EndTime] time(0) NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_Shifts] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Shifts_Times] CHECK ([StartTime] <> [EndTime])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008073225_AddShiftsAndPauses'
)
BEGIN
    CREATE TABLE [dbo].[BreakSchedules] (
        [Id] int NOT NULL IDENTITY,
        [Code] nvarchar(30) NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [ShiftId] int NOT NULL,
        [BreakGroupId] int NULL,
        [StartTime] time(0) NOT NULL,
        [EndTime] time(0) NOT NULL,
        [Policy] nvarchar(30) NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_BreakSchedules] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_BreakSchedules_Policy] CHECK ([Policy] IN ('IMMEDIATE', 'AFTER_BUNDLE', 'AFTER_QUANTITY', 'AFTER_OPERATION')),
        CONSTRAINT [CK_BreakSchedules_Times] CHECK ([StartTime] <> [EndTime]),
        CONSTRAINT [FK_BreakSchedules_BreakGroups_BreakGroupId] FOREIGN KEY ([BreakGroupId]) REFERENCES [dbo].[BreakGroups] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_BreakSchedules_Shifts_ShiftId] FOREIGN KEY ([ShiftId]) REFERENCES [dbo].[Shifts] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008073225_AddShiftsAndPauses'
)
BEGIN
    CREATE TABLE [dbo].[WorkSessionPauses] (
        [Id] bigint NOT NULL IDENTITY,
        [WorkSessionId] bigint NOT NULL,
        [Type] nvarchar(30) NOT NULL,
        [Reason] nvarchar(30) NULL,
        [BreakScheduleId] int NULL,
        [Note] nvarchar(200) NULL,
        [StartedAt] datetime2 NOT NULL,
        [EndedAt] datetime2 NULL,
        CONSTRAINT [PK_WorkSessionPauses] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_WorkSessionPauses_Period] CHECK ([EndedAt] IS NULL OR [EndedAt] >= [StartedAt]),
        CONSTRAINT [CK_WorkSessionPauses_Reason] CHECK ([Reason] IS NULL OR [Reason] IN ('WATER', 'TOILET', 'PERSONAL', 'WAITING', 'OTHER')),
        CONSTRAINT [CK_WorkSessionPauses_Type] CHECK ([Type] IN ('BREAK', 'PAUSE', 'MACHINE_BREAKDOWN', 'WAITING_FOR_BUNDLE')),
        CONSTRAINT [FK_WorkSessionPauses_BreakSchedules_BreakScheduleId] FOREIGN KEY ([BreakScheduleId]) REFERENCES [dbo].[BreakSchedules] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_WorkSessionPauses_WorkSessions_WorkSessionId] FOREIGN KEY ([WorkSessionId]) REFERENCES [dbo].[WorkSessions] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008073225_AddShiftsAndPauses'
)
BEGIN
    EXEC(N'ALTER TABLE [dbo].[WorkSessions] ADD CONSTRAINT [CK_WorkSessions_EndReason] CHECK ([EndReason] IS NULL OR [EndReason] IN (''LOGOUT'', ''TAKEN_OVER'', ''MOVED_TO_ANOTHER_MACHINE'', ''CLOSED_BY_SUPERVISOR'', ''SHIFT_ENDED''))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008073225_AddShiftsAndPauses'
)
BEGIN
    CREATE INDEX [IX_Operators_BreakGroupId] ON [dbo].[Operators] ([BreakGroupId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008073225_AddShiftsAndPauses'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_BreakGroups_Code] ON [dbo].[BreakGroups] ([Code]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008073225_AddShiftsAndPauses'
)
BEGIN
    CREATE INDEX [IX_BreakSchedules_BreakGroupId] ON [dbo].[BreakSchedules] ([BreakGroupId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008073225_AddShiftsAndPauses'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_BreakSchedules_Code] ON [dbo].[BreakSchedules] ([Code]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008073225_AddShiftsAndPauses'
)
BEGIN
    CREATE INDEX [IX_BreakSchedules_ShiftId] ON [dbo].[BreakSchedules] ([ShiftId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008073225_AddShiftsAndPauses'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Shifts_Code] ON [dbo].[Shifts] ([Code]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008073225_AddShiftsAndPauses'
)
BEGIN
    CREATE INDEX [IX_WorkSessionPauses_BreakScheduleId_StartedAt] ON [dbo].[WorkSessionPauses] ([BreakScheduleId], [StartedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008073225_AddShiftsAndPauses'
)
BEGIN
    CREATE INDEX [IX_WorkSessionPauses_Type_StartedAt] ON [dbo].[WorkSessionPauses] ([Type], [StartedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008073225_AddShiftsAndPauses'
)
BEGIN
    CREATE INDEX [IX_WorkSessionPauses_WorkSessionId_StartedAt] ON [dbo].[WorkSessionPauses] ([WorkSessionId], [StartedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008073225_AddShiftsAndPauses'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_WorkSessionPauses_OpenPerSession] ON [dbo].[WorkSessionPauses] ([WorkSessionId]) WHERE [EndedAt] IS NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008073225_AddShiftsAndPauses'
)
BEGIN
    ALTER TABLE [dbo].[Operators] ADD CONSTRAINT [FK_Operators_BreakGroups_BreakGroupId] FOREIGN KEY ([BreakGroupId]) REFERENCES [dbo].[BreakGroups] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008073225_AddShiftsAndPauses'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261008073225_AddShiftsAndPauses', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008075705_AddQuality'
)
BEGIN
    ALTER TABLE [dbo].[BundleOperations] ADD [ReworkQuantity] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008075705_AddQuality'
)
BEGIN
    CREATE TABLE [dbo].[DefectTypes] (
        [Id] int NOT NULL IDENTITY,
        [Code] nvarchar(30) NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_DefectTypes] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008075705_AddQuality'
)
BEGIN
    CREATE TABLE [dbo].[QualityInspections] (
        [Id] bigint NOT NULL IDENTITY,
        [BundleId] int NOT NULL,
        [BundleOperationId] bigint NOT NULL,
        [Result] nvarchar(30) NOT NULL,
        [InspectedQuantity] int NOT NULL,
        [RejectedQuantity] int NOT NULL,
        [ReworkQuantity] int NOT NULL,
        [PlacedOnHold] bit NOT NULL,
        [ReworkTaskId] bigint NULL,
        [Note] nvarchar(500) NULL,
        [InspectedAt] datetime2 NOT NULL,
        [InspectedBy] nvarchar(100) NOT NULL,
        CONSTRAINT [PK_QualityInspections] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_QualityInspections_Quantities] CHECK ([InspectedQuantity] > 0 AND [RejectedQuantity] >= 0 AND [ReworkQuantity] >= 0 AND [RejectedQuantity] + [ReworkQuantity] <= [InspectedQuantity]),
        CONSTRAINT [CK_QualityInspections_Result] CHECK ([Result] IN ('PASSED', 'REJECTED', 'REWORK', 'HOLD')),
        CONSTRAINT [FK_QualityInspections_BundleOperations_BundleOperationId] FOREIGN KEY ([BundleOperationId]) REFERENCES [dbo].[BundleOperations] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_QualityInspections_Bundles_BundleId] FOREIGN KEY ([BundleId]) REFERENCES [dbo].[Bundles] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008075705_AddQuality'
)
BEGIN
    CREATE TABLE [dbo].[QualityRecords] (
        [Id] bigint NOT NULL IDENTITY,
        [QualityInspectionId] bigint NOT NULL,
        [DefectTypeId] int NOT NULL,
        [Quantity] int NOT NULL,
        CONSTRAINT [PK_QualityRecords] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_QualityRecords_Quantity] CHECK ([Quantity] > 0),
        CONSTRAINT [FK_QualityRecords_DefectTypes_DefectTypeId] FOREIGN KEY ([DefectTypeId]) REFERENCES [dbo].[DefectTypes] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_QualityRecords_QualityInspections_QualityInspectionId] FOREIGN KEY ([QualityInspectionId]) REFERENCES [dbo].[QualityInspections] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008075705_AddQuality'
)
BEGIN
    CREATE TABLE [dbo].[ReworkTasks] (
        [Id] bigint NOT NULL IDENTITY,
        [QualityInspectionId] bigint NOT NULL,
        [BundleId] int NOT NULL,
        [BundleOperationId] bigint NOT NULL,
        [Quantity] int NOT NULL,
        [Status] nvarchar(30) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NOT NULL,
        [DoneAt] datetime2 NULL,
        [ClosedAt] datetime2 NULL,
        [ClosedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_ReworkTasks] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_ReworkTasks_Quantity] CHECK ([Quantity] > 0),
        CONSTRAINT [CK_ReworkTasks_Status] CHECK ([Status] IN ('OPEN', 'DONE', 'APPROVED', 'REJECTED')),
        CONSTRAINT [FK_ReworkTasks_BundleOperations_BundleOperationId] FOREIGN KEY ([BundleOperationId]) REFERENCES [dbo].[BundleOperations] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ReworkTasks_Bundles_BundleId] FOREIGN KEY ([BundleId]) REFERENCES [dbo].[Bundles] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ReworkTasks_QualityInspections_QualityInspectionId] FOREIGN KEY ([QualityInspectionId]) REFERENCES [dbo].[QualityInspections] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008075705_AddQuality'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'CreatedAt', N'CreatedBy', N'DeletedAt', N'DeletedBy', N'IsActive', N'IsDeleted', N'Name', N'UpdatedAt', N'UpdatedBy') AND [object_id] = OBJECT_ID(N'[dbo].[DefectTypes]'))
        SET IDENTITY_INSERT [dbo].[DefectTypes] ON;
    EXEC(N'INSERT INTO [dbo].[DefectTypes] ([Id], [Code], [CreatedAt], [CreatedBy], [DeletedAt], [DeletedBy], [IsActive], [IsDeleted], [Name], [UpdatedAt], [UpdatedBy])
    VALUES (1, N''DIKIS'', ''2026-01-01T00:00:00.0000000Z'', N''system'', NULL, NULL, CAST(1 AS bit), CAST(0 AS bit), N''Dikiş hatası'', NULL, NULL),
    (2, N''OLCU'', ''2026-01-01T00:00:00.0000000Z'', N''system'', NULL, NULL, CAST(1 AS bit), CAST(0 AS bit), N''Ölçü hatası'', NULL, NULL),
    (3, N''LEKE'', ''2026-01-01T00:00:00.0000000Z'', N''system'', NULL, NULL, CAST(1 AS bit), CAST(0 AS bit), N''Leke'', NULL, NULL),
    (4, N''SOKUK'', ''2026-01-01T00:00:00.0000000Z'', N''system'', NULL, NULL, CAST(1 AS bit), CAST(0 AS bit), N''Sökük'', NULL, NULL),
    (5, N''KUMAS'', ''2026-01-01T00:00:00.0000000Z'', N''system'', NULL, NULL, CAST(1 AS bit), CAST(0 AS bit), N''Kumaş hatası'', NULL, NULL),
    (6, N''YANLIS-RENK'', ''2026-01-01T00:00:00.0000000Z'', N''system'', NULL, NULL, CAST(1 AS bit), CAST(0 AS bit), N''Yanlış renk'', NULL, NULL),
    (7, N''YANLIS-BEDEN'', ''2026-01-01T00:00:00.0000000Z'', N''system'', NULL, NULL, CAST(1 AS bit), CAST(0 AS bit), N''Yanlış beden'', NULL, NULL),
    (8, N''DIGER'', ''2026-01-01T00:00:00.0000000Z'', N''system'', NULL, NULL, CAST(1 AS bit), CAST(0 AS bit), N''Diğer'', NULL, NULL)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'CreatedAt', N'CreatedBy', N'DeletedAt', N'DeletedBy', N'IsActive', N'IsDeleted', N'Name', N'UpdatedAt', N'UpdatedBy') AND [object_id] = OBJECT_ID(N'[dbo].[DefectTypes]'))
        SET IDENTITY_INSERT [dbo].[DefectTypes] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008075705_AddQuality'
)
BEGIN
    EXEC(N'ALTER TABLE [dbo].[BundleOperations] ADD CONSTRAINT [CK_BundleOperations_ReworkQuantity] CHECK ([ReworkQuantity] >= 0)');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008075705_AddQuality'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_DefectTypes_Code] ON [dbo].[DefectTypes] ([Code]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008075705_AddQuality'
)
BEGIN
    CREATE INDEX [IX_QualityInspections_BundleId_Id] ON [dbo].[QualityInspections] ([BundleId], [Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008075705_AddQuality'
)
BEGIN
    CREATE INDEX [IX_QualityInspections_BundleOperationId] ON [dbo].[QualityInspections] ([BundleOperationId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008075705_AddQuality'
)
BEGIN
    CREATE INDEX [IX_QualityInspections_InspectedAt_Result] ON [dbo].[QualityInspections] ([InspectedAt], [Result]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008075705_AddQuality'
)
BEGIN
    CREATE INDEX [IX_QualityRecords_DefectTypeId] ON [dbo].[QualityRecords] ([DefectTypeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008075705_AddQuality'
)
BEGIN
    CREATE INDEX [IX_QualityRecords_QualityInspectionId] ON [dbo].[QualityRecords] ([QualityInspectionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008075705_AddQuality'
)
BEGIN
    CREATE INDEX [IX_ReworkTasks_BundleId_Id] ON [dbo].[ReworkTasks] ([BundleId], [Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008075705_AddQuality'
)
BEGIN
    CREATE INDEX [IX_ReworkTasks_BundleOperationId_Status] ON [dbo].[ReworkTasks] ([BundleOperationId], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008075705_AddQuality'
)
BEGIN
    CREATE INDEX [IX_ReworkTasks_QualityInspectionId] ON [dbo].[ReworkTasks] ([QualityInspectionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008075705_AddQuality'
)
BEGIN
    CREATE INDEX [IX_ReworkTasks_Status_Id] ON [dbo].[ReworkTasks] ([Status], [Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008075705_AddQuality'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261008075705_AddQuality', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008082955_AddStock'
)
BEGIN
    CREATE TABLE [dbo].[StockItems] (
        [Id] int NOT NULL IDENTITY,
        [Code] nvarchar(100) NOT NULL,
        [Name] nvarchar(300) NOT NULL,
        [ItemType] nvarchar(30) NOT NULL,
        [Unit] nvarchar(30) NOT NULL,
        [ProductId] int NULL,
        [ColorId] int NULL,
        [SizeId] int NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_StockItems] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_StockItems_FinishedGood] CHECK (([ItemType] = 'FINISHED_GOOD' AND [ProductId] IS NOT NULL AND [ColorId] IS NOT NULL AND [SizeId] IS NOT NULL) OR ([ItemType] <> 'FINISHED_GOOD' AND [ProductId] IS NULL AND [ColorId] IS NULL AND [SizeId] IS NULL)),
        CONSTRAINT [CK_StockItems_ItemType] CHECK ([ItemType] IN ('FABRIC', 'ACCESSORY', 'FINISHED_GOOD', 'OTHER')),
        CONSTRAINT [CK_StockItems_Unit] CHECK ([Unit] IN ('PIECE', 'METER', 'KILOGRAM', 'ROLL', 'BOX')),
        CONSTRAINT [FK_StockItems_Colors_ColorId] FOREIGN KEY ([ColorId]) REFERENCES [dbo].[Colors] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_StockItems_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [dbo].[Products] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_StockItems_Sizes_SizeId] FOREIGN KEY ([SizeId]) REFERENCES [dbo].[Sizes] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008082955_AddStock'
)
BEGIN
    CREATE TABLE [dbo].[Warehouses] (
        [Id] int NOT NULL IDENTITY,
        [Code] nvarchar(30) NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [Type] nvarchar(30) NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_Warehouses] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Warehouses_Type] CHECK ([Type] IN ('RAW_MATERIAL', 'FINISHED_GOODS', 'GENERAL'))
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008082955_AddStock'
)
BEGIN
    CREATE TABLE [dbo].[Stocks] (
        [Id] int NOT NULL IDENTITY,
        [WarehouseId] int NOT NULL,
        [StockItemId] int NOT NULL,
        [Quantity] decimal(18,3) NOT NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_Stocks] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Stocks_Quantity] CHECK ([Quantity] >= 0),
        CONSTRAINT [FK_Stocks_StockItems_StockItemId] FOREIGN KEY ([StockItemId]) REFERENCES [dbo].[StockItems] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Stocks_Warehouses_WarehouseId] FOREIGN KEY ([WarehouseId]) REFERENCES [dbo].[Warehouses] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008082955_AddStock'
)
BEGIN
    CREATE TABLE [dbo].[StockTransactions] (
        [Id] bigint NOT NULL IDENTITY,
        [RequestId] uniqueidentifier NOT NULL,
        [Type] nvarchar(30) NOT NULL,
        [WarehouseId] int NOT NULL,
        [StockItemId] int NOT NULL,
        [Quantity] decimal(18,3) NOT NULL,
        [BalanceAfter] decimal(18,3) NOT NULL,
        [CounterWarehouseId] int NULL,
        [BundleId] int NULL,
        [ProductionOrderId] int NULL,
        [DocumentNo] nvarchar(50) NULL,
        [Note] nvarchar(300) NULL,
        [OccurredAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NOT NULL,
        CONSTRAINT [PK_StockTransactions] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_StockTransactions_Quantity] CHECK ([Quantity] <> 0 AND [BalanceAfter] >= 0),
        CONSTRAINT [CK_StockTransactions_Type] CHECK ([Type] IN ('RECEIPT', 'ISSUE', 'TRANSFER_OUT', 'TRANSFER_IN', 'ADJUSTMENT', 'PRODUCTION_RECEIPT')),
        CONSTRAINT [FK_StockTransactions_Bundles_BundleId] FOREIGN KEY ([BundleId]) REFERENCES [dbo].[Bundles] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_StockTransactions_ProductionOrders_ProductionOrderId] FOREIGN KEY ([ProductionOrderId]) REFERENCES [dbo].[ProductionOrders] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_StockTransactions_StockItems_StockItemId] FOREIGN KEY ([StockItemId]) REFERENCES [dbo].[StockItems] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_StockTransactions_Warehouses_CounterWarehouseId] FOREIGN KEY ([CounterWarehouseId]) REFERENCES [dbo].[Warehouses] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_StockTransactions_Warehouses_WarehouseId] FOREIGN KEY ([WarehouseId]) REFERENCES [dbo].[Warehouses] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008082955_AddStock'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'CreatedAt', N'CreatedBy', N'DeletedAt', N'DeletedBy', N'IsActive', N'IsDeleted', N'Name', N'Type', N'UpdatedAt', N'UpdatedBy') AND [object_id] = OBJECT_ID(N'[dbo].[Warehouses]'))
        SET IDENTITY_INSERT [dbo].[Warehouses] ON;
    EXEC(N'INSERT INTO [dbo].[Warehouses] ([Id], [Code], [CreatedAt], [CreatedBy], [DeletedAt], [DeletedBy], [IsActive], [IsDeleted], [Name], [Type], [UpdatedAt], [UpdatedBy])
    VALUES (1, N''HAMMADDE'', ''2026-01-01T00:00:00.0000000Z'', N''system'', NULL, NULL, CAST(1 AS bit), CAST(0 AS bit), N''Hammadde Deposu'', N''RAW_MATERIAL'', NULL, NULL),
    (2, N''MAMUL'', ''2026-01-01T00:00:00.0000000Z'', N''system'', NULL, NULL, CAST(1 AS bit), CAST(0 AS bit), N''Mamul Deposu'', N''FINISHED_GOODS'', NULL, NULL)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'CreatedAt', N'CreatedBy', N'DeletedAt', N'DeletedBy', N'IsActive', N'IsDeleted', N'Name', N'Type', N'UpdatedAt', N'UpdatedBy') AND [object_id] = OBJECT_ID(N'[dbo].[Warehouses]'))
        SET IDENTITY_INSERT [dbo].[Warehouses] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008082955_AddStock'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_StockItems_Code] ON [dbo].[StockItems] ([Code]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008082955_AddStock'
)
BEGIN
    CREATE INDEX [IX_StockItems_ColorId] ON [dbo].[StockItems] ([ColorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008082955_AddStock'
)
BEGIN
    CREATE INDEX [IX_StockItems_ItemType] ON [dbo].[StockItems] ([ItemType]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008082955_AddStock'
)
BEGIN
    CREATE INDEX [IX_StockItems_SizeId] ON [dbo].[StockItems] ([SizeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008082955_AddStock'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_StockItems_FinishedGood] ON [dbo].[StockItems] ([ProductId], [ColorId], [SizeId]) WHERE [ProductId] IS NOT NULL AND [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008082955_AddStock'
)
BEGIN
    CREATE INDEX [IX_Stocks_StockItemId] ON [dbo].[Stocks] ([StockItemId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008082955_AddStock'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Stocks_WarehouseId_StockItemId] ON [dbo].[Stocks] ([WarehouseId], [StockItemId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008082955_AddStock'
)
BEGIN
    CREATE INDEX [IX_StockTransactions_CounterWarehouseId] ON [dbo].[StockTransactions] ([CounterWarehouseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008082955_AddStock'
)
BEGIN
    CREATE INDEX [IX_StockTransactions_OccurredAt_Type] ON [dbo].[StockTransactions] ([OccurredAt], [Type]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008082955_AddStock'
)
BEGIN
    CREATE INDEX [IX_StockTransactions_ProductionOrderId] ON [dbo].[StockTransactions] ([ProductionOrderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008082955_AddStock'
)
BEGIN
    CREATE UNIQUE INDEX [IX_StockTransactions_RequestId_Type] ON [dbo].[StockTransactions] ([RequestId], [Type]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008082955_AddStock'
)
BEGIN
    CREATE INDEX [IX_StockTransactions_StockItemId_WarehouseId_Id] ON [dbo].[StockTransactions] ([StockItemId], [WarehouseId], [Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008082955_AddStock'
)
BEGIN
    CREATE INDEX [IX_StockTransactions_WarehouseId_Id] ON [dbo].[StockTransactions] ([WarehouseId], [Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008082955_AddStock'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_StockTransactions_ReceiptPerBundle] ON [dbo].[StockTransactions] ([BundleId]) WHERE [BundleId] IS NOT NULL AND [Type] = ''PRODUCTION_RECEIPT''');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008082955_AddStock'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Warehouses_Code] ON [dbo].[Warehouses] ([Code]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008082955_AddStock'
)
BEGIN
    EXEC(N'CREATE TRIGGER [dbo].[TR_StockTransactions_AppendOnly]
    ON [dbo].[StockTransactions]
    INSTEAD OF UPDATE, DELETE
    AS
    BEGIN
        SET NOCOUNT ON;
        THROW 51000, ''StockTransactions is append-only: stock movements cannot be updated or deleted.'', 1;
    END');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008082955_AddStock'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261008082955_AddStock', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008100932_AddPurchasing'
)
BEGIN
    ALTER TABLE [dbo].[StockTransactions] DROP CONSTRAINT [CK_StockTransactions_Type];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008100932_AddPurchasing'
)
BEGIN
    ALTER TABLE [dbo].[StockTransactions] ADD [PurchaseOrderId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008100932_AddPurchasing'
)
BEGIN
    CREATE TABLE [dbo].[Suppliers] (
        [Id] int NOT NULL IDENTITY,
        [SupplierCode] nvarchar(30) NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [TaxNumber] nvarchar(20) NULL,
        [Phone] nvarchar(30) NULL,
        [Email] nvarchar(256) NULL,
        [Address] nvarchar(500) NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_Suppliers] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008100932_AddPurchasing'
)
BEGIN
    CREATE TABLE [dbo].[PurchaseOrders] (
        [Id] int NOT NULL IDENTITY,
        [OrderNo] nvarchar(30) NOT NULL,
        [SupplierId] int NOT NULL,
        [OrderDate] date NOT NULL,
        [ExpectedDate] date NULL,
        [Currency] varchar(3) NOT NULL,
        [Status] nvarchar(30) NOT NULL,
        [Notes] nvarchar(1000) NULL,
        [RowVersion] rowversion NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_PurchaseOrders] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_PurchaseOrders_ExpectedDate] CHECK ([ExpectedDate] IS NULL OR [ExpectedDate] >= [OrderDate]),
        CONSTRAINT [CK_PurchaseOrders_Status] CHECK ([Status] IN ('DRAFT', 'APPROVED', 'PARTIALLY_RECEIVED', 'RECEIVED', 'CLOSED', 'CANCELLED')),
        CONSTRAINT [FK_PurchaseOrders_Suppliers_SupplierId] FOREIGN KEY ([SupplierId]) REFERENCES [dbo].[Suppliers] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008100932_AddPurchasing'
)
BEGIN
    CREATE TABLE [dbo].[GoodsReceipts] (
        [Id] bigint NOT NULL IDENTITY,
        [RequestId] uniqueidentifier NOT NULL,
        [PurchaseOrderId] int NOT NULL,
        [WarehouseId] int NOT NULL,
        [DocumentNo] nvarchar(50) NULL,
        [Note] nvarchar(300) NULL,
        [ReceivedAt] datetime2 NOT NULL,
        [ReceivedBy] nvarchar(100) NOT NULL,
        CONSTRAINT [PK_GoodsReceipts] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_GoodsReceipts_PurchaseOrders_PurchaseOrderId] FOREIGN KEY ([PurchaseOrderId]) REFERENCES [dbo].[PurchaseOrders] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_GoodsReceipts_Warehouses_WarehouseId] FOREIGN KEY ([WarehouseId]) REFERENCES [dbo].[Warehouses] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008100932_AddPurchasing'
)
BEGIN
    CREATE TABLE [dbo].[PurchaseOrderItems] (
        [Id] int NOT NULL IDENTITY,
        [PurchaseOrderId] int NOT NULL,
        [StockItemId] int NOT NULL,
        [Quantity] decimal(18,3) NOT NULL,
        [UnitPrice] decimal(18,4) NULL,
        [ReceivedQuantity] decimal(18,3) NOT NULL,
        CONSTRAINT [PK_PurchaseOrderItems] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_PurchaseOrderItems_Quantities] CHECK ([Quantity] > 0 AND [ReceivedQuantity] >= 0 AND [ReceivedQuantity] <= [Quantity]),
        CONSTRAINT [CK_PurchaseOrderItems_UnitPrice] CHECK ([UnitPrice] IS NULL OR [UnitPrice] >= 0),
        CONSTRAINT [FK_PurchaseOrderItems_PurchaseOrders_PurchaseOrderId] FOREIGN KEY ([PurchaseOrderId]) REFERENCES [dbo].[PurchaseOrders] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PurchaseOrderItems_StockItems_StockItemId] FOREIGN KEY ([StockItemId]) REFERENCES [dbo].[StockItems] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008100932_AddPurchasing'
)
BEGIN
    CREATE TABLE [dbo].[GoodsReceiptLines] (
        [Id] bigint NOT NULL IDENTITY,
        [GoodsReceiptId] bigint NOT NULL,
        [PurchaseOrderItemId] int NOT NULL,
        [Quantity] decimal(18,3) NOT NULL,
        CONSTRAINT [PK_GoodsReceiptLines] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_GoodsReceiptLines_Quantity] CHECK ([Quantity] > 0),
        CONSTRAINT [FK_GoodsReceiptLines_GoodsReceipts_GoodsReceiptId] FOREIGN KEY ([GoodsReceiptId]) REFERENCES [dbo].[GoodsReceipts] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_GoodsReceiptLines_PurchaseOrderItems_PurchaseOrderItemId] FOREIGN KEY ([PurchaseOrderItemId]) REFERENCES [dbo].[PurchaseOrderItems] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008100932_AddPurchasing'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'Description', N'Module') AND [object_id] = OBJECT_ID(N'[dbo].[Permissions]'))
        SET IDENTITY_INSERT [dbo].[Permissions] ON;
    EXEC(N'INSERT INTO [dbo].[Permissions] ([Id], [Code], [Description], [Module])
    VALUES (35, N''Purchase.View'', N''Tedarikçi ve satın alma siparişlerini görüntüleme'', N''Purchase''),
    (36, N''Purchase.Manage'', N''Tedarikçi ve satın alma siparişi oluşturma / onaylama'', N''Purchase'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'Description', N'Module') AND [object_id] = OBJECT_ID(N'[dbo].[Permissions]'))
        SET IDENTITY_INSERT [dbo].[Permissions] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008100932_AddPurchasing'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'PermissionId', N'RoleId') AND [object_id] = OBJECT_ID(N'[dbo].[RolePermissions]'))
        SET IDENTITY_INSERT [dbo].[RolePermissions] ON;
    EXEC(N'INSERT INTO [dbo].[RolePermissions] ([PermissionId], [RoleId])
    VALUES (35, 1),
    (36, 1),
    (35, 2),
    (35, 3),
    (36, 3),
    (35, 6),
    (35, 9)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'PermissionId', N'RoleId') AND [object_id] = OBJECT_ID(N'[dbo].[RolePermissions]'))
        SET IDENTITY_INSERT [dbo].[RolePermissions] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008100932_AddPurchasing'
)
BEGIN
    CREATE INDEX [IX_StockTransactions_PurchaseOrderId] ON [dbo].[StockTransactions] ([PurchaseOrderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008100932_AddPurchasing'
)
BEGIN
    EXEC(N'ALTER TABLE [dbo].[StockTransactions] ADD CONSTRAINT [CK_StockTransactions_Type] CHECK ([Type] IN (''RECEIPT'', ''ISSUE'', ''TRANSFER_OUT'', ''TRANSFER_IN'', ''ADJUSTMENT'', ''PURCHASE_RECEIPT'', ''PRODUCTION_RECEIPT''))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008100932_AddPurchasing'
)
BEGIN
    CREATE INDEX [IX_GoodsReceiptLines_GoodsReceiptId] ON [dbo].[GoodsReceiptLines] ([GoodsReceiptId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008100932_AddPurchasing'
)
BEGIN
    CREATE INDEX [IX_GoodsReceiptLines_PurchaseOrderItemId] ON [dbo].[GoodsReceiptLines] ([PurchaseOrderItemId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008100932_AddPurchasing'
)
BEGIN
    CREATE INDEX [IX_GoodsReceipts_PurchaseOrderId_Id] ON [dbo].[GoodsReceipts] ([PurchaseOrderId], [Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008100932_AddPurchasing'
)
BEGIN
    CREATE UNIQUE INDEX [IX_GoodsReceipts_RequestId] ON [dbo].[GoodsReceipts] ([RequestId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008100932_AddPurchasing'
)
BEGIN
    CREATE INDEX [IX_GoodsReceipts_WarehouseId] ON [dbo].[GoodsReceipts] ([WarehouseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008100932_AddPurchasing'
)
BEGIN
    CREATE UNIQUE INDEX [IX_PurchaseOrderItems_PurchaseOrderId_StockItemId] ON [dbo].[PurchaseOrderItems] ([PurchaseOrderId], [StockItemId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008100932_AddPurchasing'
)
BEGIN
    CREATE INDEX [IX_PurchaseOrderItems_StockItemId] ON [dbo].[PurchaseOrderItems] ([StockItemId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008100932_AddPurchasing'
)
BEGIN
    CREATE UNIQUE INDEX [IX_PurchaseOrders_OrderNo] ON [dbo].[PurchaseOrders] ([OrderNo]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008100932_AddPurchasing'
)
BEGIN
    CREATE INDEX [IX_PurchaseOrders_Status_ExpectedDate] ON [dbo].[PurchaseOrders] ([Status], [ExpectedDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008100932_AddPurchasing'
)
BEGIN
    CREATE INDEX [IX_PurchaseOrders_SupplierId] ON [dbo].[PurchaseOrders] ([SupplierId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008100932_AddPurchasing'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Suppliers_SupplierCode] ON [dbo].[Suppliers] ([SupplierCode]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008100932_AddPurchasing'
)
BEGIN
    ALTER TABLE [dbo].[StockTransactions] ADD CONSTRAINT [FK_StockTransactions_PurchaseOrders_PurchaseOrderId] FOREIGN KEY ([PurchaseOrderId]) REFERENCES [dbo].[PurchaseOrders] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008100932_AddPurchasing'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261008100932_AddPurchasing', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008102205_AddShipping'
)
BEGIN
    ALTER TABLE [dbo].[StockTransactions] DROP CONSTRAINT [CK_StockTransactions_Type];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008102205_AddShipping'
)
BEGIN
    ALTER TABLE [dbo].[StockTransactions] ADD [ShipmentId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008102205_AddShipping'
)
BEGIN
    CREATE TABLE [dbo].[Shipments] (
        [Id] int NOT NULL IDENTITY,
        [ShipmentNo] nvarchar(30) NOT NULL,
        [CustomerId] int NOT NULL,
        [WarehouseId] int NOT NULL,
        [ProductionOrderId] int NULL,
        [ShipDate] date NOT NULL,
        [DocumentNo] nvarchar(50) NULL,
        [Carrier] nvarchar(150) NULL,
        [Notes] nvarchar(1000) NULL,
        [Status] nvarchar(30) NOT NULL,
        [ShippedAt] datetime2 NULL,
        [ShippedBy] nvarchar(100) NULL,
        [RowVersion] rowversion NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_Shipments] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Shipments_Shipped] CHECK ([Status] <> 'SHIPPED' OR ([ShippedAt] IS NOT NULL AND [ShippedBy] IS NOT NULL)),
        CONSTRAINT [CK_Shipments_Status] CHECK ([Status] IN ('DRAFT', 'SHIPPED', 'CANCELLED')),
        CONSTRAINT [FK_Shipments_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [dbo].[Customers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Shipments_ProductionOrders_ProductionOrderId] FOREIGN KEY ([ProductionOrderId]) REFERENCES [dbo].[ProductionOrders] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Shipments_Warehouses_WarehouseId] FOREIGN KEY ([WarehouseId]) REFERENCES [dbo].[Warehouses] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008102205_AddShipping'
)
BEGIN
    CREATE TABLE [dbo].[ShipmentItems] (
        [Id] int NOT NULL IDENTITY,
        [ShipmentId] int NOT NULL,
        [StockItemId] int NOT NULL,
        [Quantity] decimal(18,3) NOT NULL,
        CONSTRAINT [PK_ShipmentItems] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_ShipmentItems_Quantity] CHECK ([Quantity] > 0),
        CONSTRAINT [FK_ShipmentItems_Shipments_ShipmentId] FOREIGN KEY ([ShipmentId]) REFERENCES [dbo].[Shipments] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ShipmentItems_StockItems_StockItemId] FOREIGN KEY ([StockItemId]) REFERENCES [dbo].[StockItems] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008102205_AddShipping'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'Description', N'Module') AND [object_id] = OBJECT_ID(N'[dbo].[Permissions]'))
        SET IDENTITY_INSERT [dbo].[Permissions] ON;
    EXEC(N'INSERT INTO [dbo].[Permissions] ([Id], [Code], [Description], [Module])
    VALUES (37, N''Shipment.View'', N''Sevkiyatları görüntüleme'', N''Shipment''),
    (38, N''Shipment.Manage'', N''Sevkiyat oluşturma / sevk etme'', N''Shipment'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'Description', N'Module') AND [object_id] = OBJECT_ID(N'[dbo].[Permissions]'))
        SET IDENTITY_INSERT [dbo].[Permissions] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008102205_AddShipping'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'PermissionId', N'RoleId') AND [object_id] = OBJECT_ID(N'[dbo].[RolePermissions]'))
        SET IDENTITY_INSERT [dbo].[RolePermissions] ON;
    EXEC(N'INSERT INTO [dbo].[RolePermissions] ([PermissionId], [RoleId])
    VALUES (37, 1),
    (38, 1),
    (37, 2),
    (37, 3),
    (37, 6),
    (38, 6),
    (37, 9)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'PermissionId', N'RoleId') AND [object_id] = OBJECT_ID(N'[dbo].[RolePermissions]'))
        SET IDENTITY_INSERT [dbo].[RolePermissions] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008102205_AddShipping'
)
BEGIN
    CREATE INDEX [IX_StockTransactions_ShipmentId] ON [dbo].[StockTransactions] ([ShipmentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008102205_AddShipping'
)
BEGIN
    EXEC(N'ALTER TABLE [dbo].[StockTransactions] ADD CONSTRAINT [CK_StockTransactions_Type] CHECK ([Type] IN (''RECEIPT'', ''ISSUE'', ''TRANSFER_OUT'', ''TRANSFER_IN'', ''ADJUSTMENT'', ''PURCHASE_RECEIPT'', ''SHIPMENT'', ''PRODUCTION_RECEIPT''))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008102205_AddShipping'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ShipmentItems_ShipmentId_StockItemId] ON [dbo].[ShipmentItems] ([ShipmentId], [StockItemId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008102205_AddShipping'
)
BEGIN
    CREATE INDEX [IX_ShipmentItems_StockItemId] ON [dbo].[ShipmentItems] ([StockItemId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008102205_AddShipping'
)
BEGIN
    CREATE INDEX [IX_Shipments_CustomerId] ON [dbo].[Shipments] ([CustomerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008102205_AddShipping'
)
BEGIN
    CREATE INDEX [IX_Shipments_ProductionOrderId_Status] ON [dbo].[Shipments] ([ProductionOrderId], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008102205_AddShipping'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Shipments_ShipmentNo] ON [dbo].[Shipments] ([ShipmentNo]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008102205_AddShipping'
)
BEGIN
    CREATE INDEX [IX_Shipments_ShippedAt] ON [dbo].[Shipments] ([ShippedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008102205_AddShipping'
)
BEGIN
    CREATE INDEX [IX_Shipments_Status_ShipDate] ON [dbo].[Shipments] ([Status], [ShipDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008102205_AddShipping'
)
BEGIN
    CREATE INDEX [IX_Shipments_WarehouseId] ON [dbo].[Shipments] ([WarehouseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008102205_AddShipping'
)
BEGIN
    ALTER TABLE [dbo].[StockTransactions] ADD CONSTRAINT [FK_StockTransactions_Shipments_ShipmentId] FOREIGN KEY ([ShipmentId]) REFERENCES [dbo].[Shipments] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008102205_AddShipping'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261008102205_AddShipping', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008103938_AddIntegrations'
)
BEGIN
    CREATE TABLE [dbo].[IntegrationMessages] (
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
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008103938_AddIntegrations'
)
BEGIN
    CREATE TABLE [dbo].[IntegrationDeliveries] (
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
        CONSTRAINT [FK_IntegrationDeliveries_IntegrationMessages_IntegrationMessageId] FOREIGN KEY ([IntegrationMessageId]) REFERENCES [dbo].[IntegrationMessages] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008103938_AddIntegrations'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'Description', N'Module') AND [object_id] = OBJECT_ID(N'[dbo].[Permissions]'))
        SET IDENTITY_INSERT [dbo].[Permissions] ON;
    EXEC(N'INSERT INTO [dbo].[Permissions] ([Id], [Code], [Description], [Module])
    VALUES (39, N''Integration.View'', N''Entegrasyon olaylarını ve teslimlerini görüntüleme'', N''Integration''),
    (40, N''Integration.Manage'', N''Entegrasyon teslimlerini yeniden gönderme'', N''Integration'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'Description', N'Module') AND [object_id] = OBJECT_ID(N'[dbo].[Permissions]'))
        SET IDENTITY_INSERT [dbo].[Permissions] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008103938_AddIntegrations'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'PermissionId', N'RoleId') AND [object_id] = OBJECT_ID(N'[dbo].[RolePermissions]'))
        SET IDENTITY_INSERT [dbo].[RolePermissions] ON;
    EXEC(N'INSERT INTO [dbo].[RolePermissions] ([PermissionId], [RoleId])
    VALUES (39, 1),
    (40, 1)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'PermissionId', N'RoleId') AND [object_id] = OBJECT_ID(N'[dbo].[RolePermissions]'))
        SET IDENTITY_INSERT [dbo].[RolePermissions] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008103938_AddIntegrations'
)
BEGIN
    CREATE INDEX [IX_IntegrationDeliveries_Channel_Status] ON [dbo].[IntegrationDeliveries] ([Channel], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008103938_AddIntegrations'
)
BEGIN
    CREATE UNIQUE INDEX [IX_IntegrationDeliveries_IntegrationMessageId_Channel] ON [dbo].[IntegrationDeliveries] ([IntegrationMessageId], [Channel]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008103938_AddIntegrations'
)
BEGIN
    CREATE INDEX [IX_IntegrationDeliveries_Status_NextAttemptAt] ON [dbo].[IntegrationDeliveries] ([Status], [NextAttemptAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008103938_AddIntegrations'
)
BEGIN
    CREATE INDEX [IX_IntegrationMessages_EntityName_EntityId] ON [dbo].[IntegrationMessages] ([EntityName], [EntityId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008103938_AddIntegrations'
)
BEGIN
    CREATE UNIQUE INDEX [IX_IntegrationMessages_EventId] ON [dbo].[IntegrationMessages] ([EventId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008103938_AddIntegrations'
)
BEGIN
    CREATE INDEX [IX_IntegrationMessages_EventType_Id] ON [dbo].[IntegrationMessages] ([EventType], [Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008103938_AddIntegrations'
)
BEGIN
    CREATE INDEX [IX_IntegrationMessages_Reference] ON [dbo].[IntegrationMessages] ([Reference]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008103938_AddIntegrations'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261008103938_AddIntegrations', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261009190736_RemoveTabletEnrollmentCode'
)
BEGIN
    DECLARE @var nvarchar(max);
    SELECT @var = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[dbo].[Tablets]') AND [c].[name] = N'EnrollmentCodeExpiresAt');
    IF @var IS NOT NULL EXEC(N'ALTER TABLE [dbo].[Tablets] DROP CONSTRAINT ' + @var + ';');
    ALTER TABLE [dbo].[Tablets] DROP COLUMN [EnrollmentCodeExpiresAt];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261009190736_RemoveTabletEnrollmentCode'
)
BEGIN
    DECLARE @var1 nvarchar(max);
    SELECT @var1 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[dbo].[Tablets]') AND [c].[name] = N'EnrollmentCodeHash');
    IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [dbo].[Tablets] DROP CONSTRAINT ' + @var1 + ';');
    ALTER TABLE [dbo].[Tablets] DROP COLUMN [EnrollmentCodeHash];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261009190736_RemoveTabletEnrollmentCode'
)
BEGIN
    DECLARE @var2 nvarchar(max);
    SELECT @var2 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[dbo].[Tablets]') AND [c].[name] = N'EnrollmentFailedAttempts');
    IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [dbo].[Tablets] DROP CONSTRAINT ' + @var2 + ';');
    ALTER TABLE [dbo].[Tablets] DROP COLUMN [EnrollmentFailedAttempts];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261009190736_RemoveTabletEnrollmentCode'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261009190736_RemoveTabletEnrollmentCode', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261009194036_AddOperatorPhotos'
)
BEGIN
    CREATE TABLE [dbo].[OperatorPhotos] (
        [OperatorId] int NOT NULL,
        [Content] varbinary(max) NOT NULL,
        [ContentType] varchar(30) NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_OperatorPhotos] PRIMARY KEY ([OperatorId]),
        CONSTRAINT [FK_OperatorPhotos_Operators_OperatorId] FOREIGN KEY ([OperatorId]) REFERENCES [dbo].[Operators] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261009194036_AddOperatorPhotos'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261009194036_AddOperatorPhotos', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261009201956_AddStopReasons'
)
BEGIN
    ALTER TABLE [dbo].[WorkSessionPauses] ADD [StopReasonId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261009201956_AddStopReasons'
)
BEGIN
    CREATE TABLE [dbo].[StopReasons] (
        [Id] int NOT NULL IDENTITY,
        [Code] nvarchar(30) NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [Category] nvarchar(30) NOT NULL,
        [SortOrder] int NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_StopReasons] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_StopReasons_Category] CHECK ([Category] IN ('PAUSE', 'BREAKDOWN'))
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261009201956_AddStopReasons'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Category', N'Code', N'CreatedAt', N'CreatedBy', N'DeletedAt', N'DeletedBy', N'IsActive', N'IsDeleted', N'Name', N'SortOrder', N'UpdatedAt', N'UpdatedBy') AND [object_id] = OBJECT_ID(N'[dbo].[StopReasons]'))
        SET IDENTITY_INSERT [dbo].[StopReasons] ON;
    EXEC(N'INSERT INTO [dbo].[StopReasons] ([Id], [Category], [Code], [CreatedAt], [CreatedBy], [DeletedAt], [DeletedBy], [IsActive], [IsDeleted], [Name], [SortOrder], [UpdatedAt], [UpdatedBy])
    VALUES (1, N''PAUSE'', N''SU'', ''2026-01-01T00:00:00.0000000Z'', N''system'', NULL, NULL, CAST(1 AS bit), CAST(0 AS bit), N''Su'', 10, NULL, NULL),
    (2, N''PAUSE'', N''TUVALET'', ''2026-01-01T00:00:00.0000000Z'', N''system'', NULL, NULL, CAST(1 AS bit), CAST(0 AS bit), N''Tuvalet'', 20, NULL, NULL),
    (3, N''PAUSE'', N''KISISEL'', ''2026-01-01T00:00:00.0000000Z'', N''system'', NULL, NULL, CAST(1 AS bit), CAST(0 AS bit), N''Kişisel ihtiyaç'', 30, NULL, NULL),
    (4, N''PAUSE'', N''BEKLEME'', ''2026-01-01T00:00:00.0000000Z'', N''system'', NULL, NULL, CAST(1 AS bit), CAST(0 AS bit), N''Bekleme'', 40, NULL, NULL),
    (5, N''PAUSE'', N''DIGER'', ''2026-01-01T00:00:00.0000000Z'', N''system'', NULL, NULL, CAST(1 AS bit), CAST(0 AS bit), N''Diğer'', 90, NULL, NULL),
    (6, N''BREAKDOWN'', N''IGNE'', ''2026-01-01T00:00:00.0000000Z'', N''system'', NULL, NULL, CAST(1 AS bit), CAST(0 AS bit), N''İğne kırıldı'', 10, NULL, NULL),
    (7, N''BREAKDOWN'', N''IPLIK'', ''2026-01-01T00:00:00.0000000Z'', N''system'', NULL, NULL, CAST(1 AS bit), CAST(0 AS bit), N''İplik / masura sorunu'', 20, NULL, NULL),
    (8, N''BREAKDOWN'', N''MOTOR'', ''2026-01-01T00:00:00.0000000Z'', N''system'', NULL, NULL, CAST(1 AS bit), CAST(0 AS bit), N''Motor arızası'', 30, NULL, NULL),
    (9, N''BREAKDOWN'', N''ELEKTRIK'', ''2026-01-01T00:00:00.0000000Z'', N''system'', NULL, NULL, CAST(1 AS bit), CAST(0 AS bit), N''Elektrik arızası'', 40, NULL, NULL),
    (10, N''BREAKDOWN'', N''ARIZA-DIGER'', ''2026-01-01T00:00:00.0000000Z'', N''system'', NULL, NULL, CAST(1 AS bit), CAST(0 AS bit), N''Diğer arıza'', 90, NULL, NULL)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Category', N'Code', N'CreatedAt', N'CreatedBy', N'DeletedAt', N'DeletedBy', N'IsActive', N'IsDeleted', N'Name', N'SortOrder', N'UpdatedAt', N'UpdatedBy') AND [object_id] = OBJECT_ID(N'[dbo].[StopReasons]'))
        SET IDENTITY_INSERT [dbo].[StopReasons] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261009201956_AddStopReasons'
)
BEGIN
    EXEC(N'UPDATE p SET p.[StopReasonId] = r.[Id]
    FROM [dbo].[WorkSessionPauses] p
    INNER JOIN [dbo].[StopReasons] r ON r.[Code] = CASE p.[Reason]
        WHEN ''WATER'' THEN ''SU''
        WHEN ''TOILET'' THEN ''TUVALET''
        WHEN ''PERSONAL'' THEN ''KISISEL''
        WHEN ''WAITING'' THEN ''BEKLEME''
        WHEN ''OTHER'' THEN ''DIGER'' END
    WHERE p.[Reason] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261009201956_AddStopReasons'
)
BEGIN
    ALTER TABLE [dbo].[WorkSessionPauses] DROP CONSTRAINT [CK_WorkSessionPauses_Reason];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261009201956_AddStopReasons'
)
BEGIN
    DECLARE @var3 nvarchar(max);
    SELECT @var3 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[dbo].[WorkSessionPauses]') AND [c].[name] = N'Reason');
    IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [dbo].[WorkSessionPauses] DROP CONSTRAINT ' + @var3 + ';');
    ALTER TABLE [dbo].[WorkSessionPauses] DROP COLUMN [Reason];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261009201956_AddStopReasons'
)
BEGIN
    CREATE INDEX [IX_WorkSessionPauses_StopReasonId] ON [dbo].[WorkSessionPauses] ([StopReasonId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261009201956_AddStopReasons'
)
BEGIN
    CREATE INDEX [IX_StopReasons_Category_SortOrder] ON [dbo].[StopReasons] ([Category], [SortOrder]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261009201956_AddStopReasons'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_StopReasons_Code] ON [dbo].[StopReasons] ([Code]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261009201956_AddStopReasons'
)
BEGIN
    ALTER TABLE [dbo].[WorkSessionPauses] ADD CONSTRAINT [FK_WorkSessionPauses_StopReasons_StopReasonId] FOREIGN KEY ([StopReasonId]) REFERENCES [dbo].[StopReasons] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261009201956_AddStopReasons'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261009201956_AddStopReasons', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261009203712_AddStaffRoles'
)
BEGIN
    ALTER TABLE [dbo].[Operators] ADD [CanPause] bit NOT NULL DEFAULT CAST(1 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261009203712_AddStaffRoles'
)
BEGIN
    ALTER TABLE [dbo].[Operators] ADD [CanReportBreakdown] bit NOT NULL DEFAULT CAST(1 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261009203712_AddStaffRoles'
)
BEGIN
    ALTER TABLE [dbo].[Operators] ADD [Role] nvarchar(30) NOT NULL DEFAULT N'OPERATOR';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261009203712_AddStaffRoles'
)
BEGIN
    EXEC(N'ALTER TABLE [dbo].[Operators] ADD CONSTRAINT [CK_Operators_Role] CHECK ([Role] IN (''OPERATOR'', ''LINE_CHIEF'', ''MAINTENANCE''))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261009203712_AddStaffRoles'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261009203712_AddStaffRoles', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261009212231_AddFloorPermissions'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'Description', N'Module') AND [object_id] = OBJECT_ID(N'[dbo].[Permissions]'))
        SET IDENTITY_INSERT [dbo].[Permissions] ON;
    EXEC(N'INSERT INTO [dbo].[Permissions] ([Id], [Code], [Description], [Module])
    VALUES (41, N''Floor.Pause'', N''Tablette kendi kartıyla ara verme / arayı onaylama'', N''Floor''),
    (42, N''Floor.ReportBreakdown'', N''Tablette kendi kartıyla arıza bildirme'', N''Floor''),
    (43, N''Floor.ResolveBreakdown'', N''Tablette kartını okutarak arızayı kapatma'', N''Floor'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'Description', N'Module') AND [object_id] = OBJECT_ID(N'[dbo].[Permissions]'))
        SET IDENTITY_INSERT [dbo].[Permissions] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261009212231_AddFloorPermissions'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'PermissionId', N'RoleId') AND [object_id] = OBJECT_ID(N'[dbo].[RolePermissions]'))
        SET IDENTITY_INSERT [dbo].[RolePermissions] ON;
    EXEC(N'INSERT INTO [dbo].[RolePermissions] ([PermissionId], [RoleId])
    VALUES (41, 1),
    (42, 1),
    (43, 1),
    (41, 4),
    (42, 4),
    (43, 4),
    (42, 7),
    (43, 7)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'PermissionId', N'RoleId') AND [object_id] = OBJECT_ID(N'[dbo].[RolePermissions]'))
        SET IDENTITY_INSERT [dbo].[RolePermissions] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261009212231_AddFloorPermissions'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261009212231_AddFloorPermissions', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261009220705_AddMachineOperations'
)
BEGIN
    CREATE TABLE [dbo].[MachineOperations] (
        [MachineId] int NOT NULL,
        [OperationId] int NOT NULL,
        CONSTRAINT [PK_MachineOperations] PRIMARY KEY ([MachineId], [OperationId]),
        CONSTRAINT [FK_MachineOperations_Machines_MachineId] FOREIGN KEY ([MachineId]) REFERENCES [dbo].[Machines] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_MachineOperations_Operations_OperationId] FOREIGN KEY ([OperationId]) REFERENCES [dbo].[Operations] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261009220705_AddMachineOperations'
)
BEGIN
    CREATE INDEX [IX_MachineOperations_OperationId] ON [dbo].[MachineOperations] ([OperationId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261009220705_AddMachineOperations'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261009220705_AddMachineOperations', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261009224628_AddRunTimingToEvents'
)
BEGIN
    ALTER TABLE [dbo].[BundleOperationEvents] ADD [EfficiencyPercent] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261009224628_AddRunTimingToEvents'
)
BEGIN
    ALTER TABLE [dbo].[BundleOperationEvents] ADD [RunSeconds] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261009224628_AddRunTimingToEvents'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261009224628_AddRunTimingToEvents', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261010205130_AddPayroll'
)
BEGIN
    ALTER TABLE [dbo].[Operations] ADD [PieceRate] decimal(18,4) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261010205130_AddPayroll'
)
BEGIN
    ALTER TABLE [dbo].[BundleOperations] ADD [UnitRate] decimal(18,4) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261010205130_AddPayroll'
)
BEGIN
    CREATE TABLE [dbo].[PayrollSettings] (
        [Id] int NOT NULL,
        [MinuteRate] decimal(18,4) NOT NULL,
        [Currency] varchar(3) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_PayrollSettings] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_PayrollSettings_MinuteRate] CHECK ([MinuteRate] >= 0)
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261010205130_AddPayroll'
)
BEGIN
    CREATE TABLE [dbo].[PayrollBonusTiers] (
        [Id] int NOT NULL IDENTITY,
        [PayrollSettingId] int NOT NULL,
        [MinEfficiencyPercent] decimal(7,2) NOT NULL,
        [BonusPercent] decimal(7,2) NOT NULL,
        CONSTRAINT [PK_PayrollBonusTiers] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_PayrollBonusTiers_Percents] CHECK ([MinEfficiencyPercent] > 0 AND [BonusPercent] > 0),
        CONSTRAINT [FK_PayrollBonusTiers_PayrollSettings_PayrollSettingId] FOREIGN KEY ([PayrollSettingId]) REFERENCES [dbo].[PayrollSettings] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261010205130_AddPayroll'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CreatedAt', N'CreatedBy', N'Currency', N'MinuteRate', N'UpdatedAt', N'UpdatedBy') AND [object_id] = OBJECT_ID(N'[dbo].[PayrollSettings]'))
        SET IDENTITY_INSERT [dbo].[PayrollSettings] ON;
    EXEC(N'INSERT INTO [dbo].[PayrollSettings] ([Id], [CreatedAt], [CreatedBy], [Currency], [MinuteRate], [UpdatedAt], [UpdatedBy])
    VALUES (1, ''2026-01-01T00:00:00.0000000Z'', N''system'', ''TRY'', 0.0, NULL, NULL)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CreatedAt', N'CreatedBy', N'Currency', N'MinuteRate', N'UpdatedAt', N'UpdatedBy') AND [object_id] = OBJECT_ID(N'[dbo].[PayrollSettings]'))
        SET IDENTITY_INSERT [dbo].[PayrollSettings] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261010205130_AddPayroll'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'Description', N'Module') AND [object_id] = OBJECT_ID(N'[dbo].[Permissions]'))
        SET IDENTITY_INSERT [dbo].[Permissions] ON;
    EXEC(N'INSERT INTO [dbo].[Permissions] ([Id], [Code], [Description], [Module])
    VALUES (44, N''Payroll.View'', N''Parça başı ücret ve prim kazançlarını görüntüleme'', N''Payroll''),
    (45, N''Payroll.Manage'', N''Ücret ve prim ayarlarını düzenleme'', N''Payroll''),
    (46, N''Costing.View'', N''Malzeme ve işçilik maliyetlerini görüntüleme'', N''Costing'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'Description', N'Module') AND [object_id] = OBJECT_ID(N'[dbo].[Permissions]'))
        SET IDENTITY_INSERT [dbo].[Permissions] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261010205130_AddPayroll'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'PermissionId', N'RoleId') AND [object_id] = OBJECT_ID(N'[dbo].[RolePermissions]'))
        SET IDENTITY_INSERT [dbo].[RolePermissions] ON;
    EXEC(N'INSERT INTO [dbo].[RolePermissions] ([PermissionId], [RoleId])
    VALUES (44, 1),
    (45, 1),
    (46, 1),
    (44, 2),
    (46, 2),
    (44, 9),
    (46, 9)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'PermissionId', N'RoleId') AND [object_id] = OBJECT_ID(N'[dbo].[RolePermissions]'))
        SET IDENTITY_INSERT [dbo].[RolePermissions] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261010205130_AddPayroll'
)
BEGIN
    CREATE UNIQUE INDEX [IX_PayrollBonusTiers_PayrollSettingId_MinEfficiencyPercent] ON [dbo].[PayrollBonusTiers] ([PayrollSettingId], [MinEfficiencyPercent]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261010205130_AddPayroll'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261010205130_AddPayroll', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261010205922_AddBillOfMaterials'
)
BEGIN
    ALTER TABLE [dbo].[StockTransactions] DROP CONSTRAINT [CK_StockTransactions_Type];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261010205922_AddBillOfMaterials'
)
BEGIN
    ALTER TABLE [dbo].[StockTransactions] ADD [UnitCost] decimal(18,4) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261010205922_AddBillOfMaterials'
)
BEGIN
    ALTER TABLE [dbo].[StockItems] ADD [UnitCost] decimal(18,4) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261010205922_AddBillOfMaterials'
)
BEGIN
    CREATE TABLE [dbo].[ProductMaterials] (
        [Id] int NOT NULL IDENTITY,
        [ProductId] int NOT NULL,
        [StockItemId] int NOT NULL,
        [ColorId] int NULL,
        [SizeId] int NULL,
        [QuantityPerPiece] decimal(18,4) NOT NULL,
        [WastePercent] decimal(5,2) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_ProductMaterials] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_ProductMaterials_QuantityPerPiece] CHECK ([QuantityPerPiece] > 0),
        CONSTRAINT [CK_ProductMaterials_WastePercent] CHECK ([WastePercent] >= 0 AND [WastePercent] <= 100),
        CONSTRAINT [FK_ProductMaterials_Colors_ColorId] FOREIGN KEY ([ColorId]) REFERENCES [dbo].[Colors] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ProductMaterials_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [dbo].[Products] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ProductMaterials_Sizes_SizeId] FOREIGN KEY ([SizeId]) REFERENCES [dbo].[Sizes] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ProductMaterials_StockItems_StockItemId] FOREIGN KEY ([StockItemId]) REFERENCES [dbo].[StockItems] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261010205922_AddBillOfMaterials'
)
BEGIN
    CREATE INDEX [IX_StockTransactions_BundleId_Type] ON [dbo].[StockTransactions] ([BundleId], [Type]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261010205922_AddBillOfMaterials'
)
BEGIN
    EXEC(N'ALTER TABLE [dbo].[StockTransactions] ADD CONSTRAINT [CK_StockTransactions_Type] CHECK ([Type] IN (''RECEIPT'', ''ISSUE'', ''TRANSFER_OUT'', ''TRANSFER_IN'', ''ADJUSTMENT'', ''PURCHASE_RECEIPT'', ''SHIPMENT'', ''PRODUCTION_RECEIPT'', ''PRODUCTION_ISSUE'', ''PRODUCTION_RETURN''))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261010205922_AddBillOfMaterials'
)
BEGIN
    CREATE INDEX [IX_ProductMaterials_ColorId] ON [dbo].[ProductMaterials] ([ColorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261010205922_AddBillOfMaterials'
)
BEGIN
    CREATE INDEX [IX_ProductMaterials_ProductId_StockItemId] ON [dbo].[ProductMaterials] ([ProductId], [StockItemId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261010205922_AddBillOfMaterials'
)
BEGIN
    CREATE INDEX [IX_ProductMaterials_SizeId] ON [dbo].[ProductMaterials] ([SizeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261010205922_AddBillOfMaterials'
)
BEGIN
    CREATE INDEX [IX_ProductMaterials_StockItemId] ON [dbo].[ProductMaterials] ([StockItemId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261010205922_AddBillOfMaterials'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261010205922_AddBillOfMaterials', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011213830_AddCustomerEDocumentAndAddresses'
)
BEGIN
    ALTER TABLE [dbo].[Customers] ADD [EArchiveDelivery] nvarchar(30) NOT NULL DEFAULT N'PAPER';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011213830_AddCustomerEDocumentAndAddresses'
)
BEGIN
    ALTER TABLE [dbo].[Customers] ADD [EArchiveEmail] nvarchar(256) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011213830_AddCustomerEDocumentAndAddresses'
)
BEGIN
    ALTER TABLE [dbo].[Customers] ADD [EDespatchAlias] nvarchar(256) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011213830_AddCustomerEDocumentAndAddresses'
)
BEGIN
    ALTER TABLE [dbo].[Customers] ADD [EInvoiceAlias] nvarchar(256) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011213830_AddCustomerEDocumentAndAddresses'
)
BEGIN
    ALTER TABLE [dbo].[Customers] ADD [EInvoiceProfile] nvarchar(30) NOT NULL DEFAULT N'BASIC';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011213830_AddCustomerEDocumentAndAddresses'
)
BEGIN
    ALTER TABLE [dbo].[Customers] ADD [FirstName] nvarchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011213830_AddCustomerEDocumentAndAddresses'
)
BEGIN
    ALTER TABLE [dbo].[Customers] ADD [IsEDespatchUser] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011213830_AddCustomerEDocumentAndAddresses'
)
BEGIN
    ALTER TABLE [dbo].[Customers] ADD [IsEInvoiceUser] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011213830_AddCustomerEDocumentAndAddresses'
)
BEGIN
    ALTER TABLE [dbo].[Customers] ADD [LastName] nvarchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011213830_AddCustomerEDocumentAndAddresses'
)
BEGIN
    ALTER TABLE [dbo].[Customers] ADD [MersisNo] nvarchar(16) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011213830_AddCustomerEDocumentAndAddresses'
)
BEGIN
    ALTER TABLE [dbo].[Customers] ADD [PartyType] nvarchar(30) NOT NULL DEFAULT N'COMPANY';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011213830_AddCustomerEDocumentAndAddresses'
)
BEGIN
    ALTER TABLE [dbo].[Customers] ADD [TaxOffice] nvarchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011213830_AddCustomerEDocumentAndAddresses'
)
BEGIN
    ALTER TABLE [dbo].[Customers] ADD [TradeRegistryNo] nvarchar(30) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011213830_AddCustomerEDocumentAndAddresses'
)
BEGIN
    CREATE TABLE [dbo].[CustomerAddresses] (
        [Id] int NOT NULL IDENTITY,
        [CustomerId] int NOT NULL,
        [Title] nvarchar(100) NOT NULL,
        [AddressLine] nvarchar(500) NOT NULL,
        [District] nvarchar(100) NOT NULL,
        [City] nvarchar(100) NOT NULL,
        [PostalCode] nvarchar(10) NULL,
        [CountryCode] char(2) NOT NULL,
        [ContactName] nvarchar(100) NULL,
        [Phone] nvarchar(30) NULL,
        [IsDefaultInvoice] bit NOT NULL,
        [IsDefaultDelivery] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_CustomerAddresses] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_CustomerAddresses_CountryCode] CHECK (LEN([CountryCode]) = 2),
        CONSTRAINT [FK_CustomerAddresses_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [dbo].[Customers] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011213830_AddCustomerEDocumentAndAddresses'
)
BEGIN
    CREATE INDEX [IX_Customers_TaxNumber] ON [dbo].[Customers] ([TaxNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011213830_AddCustomerEDocumentAndAddresses'
)
BEGIN
    EXEC(N'ALTER TABLE [dbo].[Customers] ADD CONSTRAINT [CK_Customers_EArchiveDelivery] CHECK ([EArchiveDelivery] IN (''ELECTRONIC'', ''PAPER''))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011213830_AddCustomerEDocumentAndAddresses'
)
BEGIN
    EXEC(N'ALTER TABLE [dbo].[Customers] ADD CONSTRAINT [CK_Customers_EInvoiceProfile] CHECK ([EInvoiceProfile] IN (''BASIC'', ''COMMERCIAL''))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011213830_AddCustomerEDocumentAndAddresses'
)
BEGIN
    EXEC(N'ALTER TABLE [dbo].[Customers] ADD CONSTRAINT [CK_Customers_PartyType] CHECK ([PartyType] IN (''COMPANY'', ''INDIVIDUAL''))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011213830_AddCustomerEDocumentAndAddresses'
)
BEGIN
    CREATE INDEX [IX_CustomerAddresses_CustomerId] ON [dbo].[CustomerAddresses] ([CustomerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011213830_AddCustomerEDocumentAndAddresses'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_CustomerAddresses_DefaultDelivery] ON [dbo].[CustomerAddresses] ([CustomerId]) WHERE [IsDefaultDelivery] = 1');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011213830_AddCustomerEDocumentAndAddresses'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_CustomerAddresses_DefaultInvoice] ON [dbo].[CustomerAddresses] ([CustomerId]) WHERE [IsDefaultInvoice] = 1');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011213830_AddCustomerEDocumentAndAddresses'
)
BEGIN

    EXEC(N'INSERT INTO [dbo].[CustomerAddresses]
        ([CustomerId], [Title], [AddressLine], [District], [City], [CountryCode], [IsDefaultInvoice], [IsDefaultDelivery], [CreatedAt], [CreatedBy])
    SELECT [Id], N''Merkez'', LTRIM(RTRIM([Address])), N'''', N'''', ''TR'', 1, 1, SYSUTCDATETIME(), N''migration''
    FROM [dbo].[Customers]
    WHERE [Address] IS NOT NULL AND LTRIM(RTRIM([Address])) <> N''''')
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011213830_AddCustomerEDocumentAndAddresses'
)
BEGIN
    DECLARE @var4 nvarchar(max);
    SELECT @var4 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[dbo].[Customers]') AND [c].[name] = N'Address');
    IF @var4 IS NOT NULL EXEC(N'ALTER TABLE [dbo].[Customers] DROP CONSTRAINT ' + @var4 + ';');
    ALTER TABLE [dbo].[Customers] DROP COLUMN [Address];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011213830_AddCustomerEDocumentAndAddresses'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261011213830_AddCustomerEDocumentAndAddresses', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012051942_AddProductFactoryMachineDetails'
)
BEGIN
    ALTER TABLE [dbo].[Products] ADD [CareInstructions] nvarchar(500) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012051942_AddProductFactoryMachineDetails'
)
BEGIN
    ALTER TABLE [dbo].[Products] ADD [Composition] nvarchar(200) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012051942_AddProductFactoryMachineDetails'
)
BEGIN
    ALTER TABLE [dbo].[Products] ADD [Currency] char(3) NOT NULL DEFAULT 'TRY';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012051942_AddProductFactoryMachineDetails'
)
BEGIN
    ALTER TABLE [dbo].[Products] ADD [CustomerArticleCode] nvarchar(50) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012051942_AddProductFactoryMachineDetails'
)
BEGIN
    ALTER TABLE [dbo].[Products] ADD [FabricWeight] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012051942_AddProductFactoryMachineDetails'
)
BEGIN
    ALTER TABLE [dbo].[Products] ADD [GtipCode] varchar(12) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012051942_AddProductFactoryMachineDetails'
)
BEGIN
    ALTER TABLE [dbo].[Products] ADD [ListPrice] decimal(18,4) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012051942_AddProductFactoryMachineDetails'
)
BEGIN
    ALTER TABLE [dbo].[Products] ADD [OriginCountryCode] char(2) NOT NULL DEFAULT 'TR';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012051942_AddProductFactoryMachineDetails'
)
BEGIN
    ALTER TABLE [dbo].[Products] ADD [ProductGroupId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012051942_AddProductFactoryMachineDetails'
)
BEGIN
    ALTER TABLE [dbo].[Products] ADD [SalesUnit] nvarchar(30) NOT NULL DEFAULT N'PIECE';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012051942_AddProductFactoryMachineDetails'
)
BEGIN
    ALTER TABLE [dbo].[Products] ADD [Season] nvarchar(20) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012051942_AddProductFactoryMachineDetails'
)
BEGIN
    ALTER TABLE [dbo].[Products] ADD [TargetGroup] nvarchar(30) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012051942_AddProductFactoryMachineDetails'
)
BEGIN
    ALTER TABLE [dbo].[Products] ADD [VatRate] decimal(5,2) NOT NULL DEFAULT 10.0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012051942_AddProductFactoryMachineDetails'
)
BEGIN
    ALTER TABLE [dbo].[Machines] ADD [AssetNo] nvarchar(50) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012051942_AddProductFactoryMachineDetails'
)
BEGIN
    ALTER TABLE [dbo].[Machines] ADD [Brand] nvarchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012051942_AddProductFactoryMachineDetails'
)
BEGIN
    ALTER TABLE [dbo].[Machines] ADD [LastMaintenanceDate] date NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012051942_AddProductFactoryMachineDetails'
)
BEGIN
    ALTER TABLE [dbo].[Machines] ADD [MaintenanceIntervalDays] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012051942_AddProductFactoryMachineDetails'
)
BEGIN
    ALTER TABLE [dbo].[Machines] ADD [ManufactureYear] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012051942_AddProductFactoryMachineDetails'
)
BEGIN
    ALTER TABLE [dbo].[Machines] ADD [ModelNo] nvarchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012051942_AddProductFactoryMachineDetails'
)
BEGIN
    ALTER TABLE [dbo].[Machines] ADD [Notes] nvarchar(500) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012051942_AddProductFactoryMachineDetails'
)
BEGIN
    ALTER TABLE [dbo].[Machines] ADD [PurchaseDate] date NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012051942_AddProductFactoryMachineDetails'
)
BEGIN
    ALTER TABLE [dbo].[Machines] ADD [SerialNo] nvarchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012051942_AddProductFactoryMachineDetails'
)
BEGIN
    ALTER TABLE [dbo].[Machines] ADD [SupplierId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012051942_AddProductFactoryMachineDetails'
)
BEGIN
    ALTER TABLE [dbo].[Machines] ADD [WarrantyEndDate] date NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012051942_AddProductFactoryMachineDetails'
)
BEGIN
    ALTER TABLE [dbo].[Factories] ADD [AddressLine] nvarchar(500) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012051942_AddProductFactoryMachineDetails'
)
BEGIN
    ALTER TABLE [dbo].[Factories] ADD [City] nvarchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012051942_AddProductFactoryMachineDetails'
)
BEGIN
    ALTER TABLE [dbo].[Factories] ADD [CountryCode] char(2) NOT NULL DEFAULT 'TR';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012051942_AddProductFactoryMachineDetails'
)
BEGIN
    ALTER TABLE [dbo].[Factories] ADD [District] nvarchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012051942_AddProductFactoryMachineDetails'
)
BEGIN
    ALTER TABLE [dbo].[Factories] ADD [Email] nvarchar(256) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012051942_AddProductFactoryMachineDetails'
)
BEGIN
    ALTER TABLE [dbo].[Factories] ADD [IndoorArea] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012051942_AddProductFactoryMachineDetails'
)
BEGIN
    ALTER TABLE [dbo].[Factories] ADD [ManagerName] nvarchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012051942_AddProductFactoryMachineDetails'
)
BEGIN
    ALTER TABLE [dbo].[Factories] ADD [Phone] nvarchar(30) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012051942_AddProductFactoryMachineDetails'
)
BEGIN
    ALTER TABLE [dbo].[Factories] ADD [PostalCode] nvarchar(10) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012051942_AddProductFactoryMachineDetails'
)
BEGIN
    ALTER TABLE [dbo].[Factories] ADD [SgkWorkplaceNo] nvarchar(40) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012051942_AddProductFactoryMachineDetails'
)
BEGIN
    CREATE TABLE [dbo].[ProductGroups] (
        [Id] int NOT NULL IDENTITY,
        [Code] nvarchar(30) NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_ProductGroups] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012051942_AddProductFactoryMachineDetails'
)
BEGIN
    CREATE INDEX [IX_Products_ProductGroupId] ON [dbo].[Products] ([ProductGroupId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012051942_AddProductFactoryMachineDetails'
)
BEGIN
    CREATE INDEX [IX_Products_Season] ON [dbo].[Products] ([Season]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012051942_AddProductFactoryMachineDetails'
)
BEGIN
    EXEC(N'ALTER TABLE [dbo].[Products] ADD CONSTRAINT [CK_Products_FabricWeight] CHECK ([FabricWeight] IS NULL OR [FabricWeight] > 0)');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012051942_AddProductFactoryMachineDetails'
)
BEGIN
    EXEC(N'ALTER TABLE [dbo].[Products] ADD CONSTRAINT [CK_Products_ListPrice] CHECK ([ListPrice] IS NULL OR [ListPrice] >= 0)');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012051942_AddProductFactoryMachineDetails'
)
BEGIN
    EXEC(N'ALTER TABLE [dbo].[Products] ADD CONSTRAINT [CK_Products_SalesUnit] CHECK ([SalesUnit] IN (''PIECE'', ''METER'', ''KILOGRAM'', ''ROLL'', ''BOX''))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012051942_AddProductFactoryMachineDetails'
)
BEGIN
    EXEC(N'ALTER TABLE [dbo].[Products] ADD CONSTRAINT [CK_Products_TargetGroup] CHECK ([TargetGroup] IN (''UNISEX'', ''MEN'', ''WOMEN'', ''KIDS'', ''BABY''))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012051942_AddProductFactoryMachineDetails'
)
BEGIN
    EXEC(N'ALTER TABLE [dbo].[Products] ADD CONSTRAINT [CK_Products_VatRate] CHECK ([VatRate] >= 0 AND [VatRate] <= 100)');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012051942_AddProductFactoryMachineDetails'
)
BEGIN
    CREATE INDEX [IX_Machines_SupplierId] ON [dbo].[Machines] ([SupplierId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012051942_AddProductFactoryMachineDetails'
)
BEGIN
    EXEC(N'ALTER TABLE [dbo].[Machines] ADD CONSTRAINT [CK_Machines_MaintenanceInterval] CHECK ([MaintenanceIntervalDays] IS NULL OR [MaintenanceIntervalDays] > 0)');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012051942_AddProductFactoryMachineDetails'
)
BEGIN
    EXEC(N'ALTER TABLE [dbo].[Machines] ADD CONSTRAINT [CK_Machines_Warranty] CHECK ([WarrantyEndDate] IS NULL OR [PurchaseDate] IS NULL OR [WarrantyEndDate] >= [PurchaseDate])');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012051942_AddProductFactoryMachineDetails'
)
BEGIN
    EXEC(N'ALTER TABLE [dbo].[Factories] ADD CONSTRAINT [CK_Factories_CountryCode] CHECK (LEN([CountryCode]) = 2)');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012051942_AddProductFactoryMachineDetails'
)
BEGIN
    EXEC(N'ALTER TABLE [dbo].[Factories] ADD CONSTRAINT [CK_Factories_IndoorArea] CHECK ([IndoorArea] IS NULL OR [IndoorArea] > 0)');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012051942_AddProductFactoryMachineDetails'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_ProductGroups_Code] ON [dbo].[ProductGroups] ([Code]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012051942_AddProductFactoryMachineDetails'
)
BEGIN
    ALTER TABLE [dbo].[Machines] ADD CONSTRAINT [FK_Machines_Suppliers_SupplierId] FOREIGN KEY ([SupplierId]) REFERENCES [dbo].[Suppliers] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012051942_AddProductFactoryMachineDetails'
)
BEGIN
    ALTER TABLE [dbo].[Products] ADD CONSTRAINT [FK_Products_ProductGroups_ProductGroupId] FOREIGN KEY ([ProductGroupId]) REFERENCES [dbo].[ProductGroups] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012051942_AddProductFactoryMachineDetails'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261012051942_AddProductFactoryMachineDetails', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012084216_AddWarehouseLocations'
)
BEGIN
    ALTER TABLE [dbo].[StockTransactions] DROP CONSTRAINT [CK_StockTransactions_Quantity];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012084216_AddWarehouseLocations'
)
BEGIN
    ALTER TABLE [dbo].[StockTransactions] DROP CONSTRAINT [CK_StockTransactions_Type];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012084216_AddWarehouseLocations'
)
BEGIN
    DROP INDEX [IX_Stocks_WarehouseId_StockItemId] ON [dbo].[Stocks];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012084216_AddWarehouseLocations'
)
BEGIN
    ALTER TABLE [dbo].[StockTransactions] ADD [CounterLocationId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012084216_AddWarehouseLocations'
)
BEGIN
    ALTER TABLE [dbo].[StockTransactions] ADD [LocationBalanceAfter] decimal(18,3) NOT NULL DEFAULT 0.0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012084216_AddWarehouseLocations'
)
BEGIN
    ALTER TABLE [dbo].[StockTransactions] ADD [LocationId] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012084216_AddWarehouseLocations'
)
BEGIN
    ALTER TABLE [dbo].[Stocks] ADD [LocationId] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012084216_AddWarehouseLocations'
)
BEGIN
    CREATE TABLE [dbo].[WarehouseLocations] (
        [Id] int NOT NULL IDENTITY,
        [WarehouseId] int NOT NULL,
        [Code] nvarchar(32) NOT NULL,
        [Zone] nvarchar(10) NOT NULL,
        [Rack] nvarchar(10) NULL,
        [Bin] nvarchar(10) NULL,
        [Description] nvarchar(100) NULL,
        [IsDefault] bit NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_WarehouseLocations] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_WarehouseLocations_Warehouses_WarehouseId] FOREIGN KEY ([WarehouseId]) REFERENCES [dbo].[Warehouses] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012084216_AddWarehouseLocations'
)
BEGIN
    INSERT INTO [dbo].[WarehouseLocations]
        ([WarehouseId], [Code], [Zone], [Description], [IsDefault], [IsActive], [CreatedAt], [CreatedBy], [IsDeleted])
    SELECT w.[Id], N'GENEL', N'GENEL', N'Adres belirtilmeyen stok', 1, 1, SYSUTCDATETIME(), N'system', 0
    FROM [dbo].[Warehouses] AS w;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012084216_AddWarehouseLocations'
)
BEGIN
    UPDATE s SET s.[LocationId] = l.[Id]
    FROM [dbo].[Stocks] AS s
    INNER JOIN [dbo].[WarehouseLocations] AS l ON l.[WarehouseId] = s.[WarehouseId] AND l.[IsDefault] = 1;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012084216_AddWarehouseLocations'
)
BEGIN
    ALTER TABLE [dbo].[StockTransactions] DISABLE TRIGGER [TR_StockTransactions_AppendOnly];

    UPDATE t SET t.[LocationId] = l.[Id], t.[LocationBalanceAfter] = t.[BalanceAfter]
    FROM [dbo].[StockTransactions] AS t
    INNER JOIN [dbo].[WarehouseLocations] AS l ON l.[WarehouseId] = t.[WarehouseId] AND l.[IsDefault] = 1;

    ALTER TABLE [dbo].[StockTransactions] ENABLE TRIGGER [TR_StockTransactions_AppendOnly];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012084216_AddWarehouseLocations'
)
BEGIN
    CREATE INDEX [IX_StockTransactions_CounterLocationId] ON [dbo].[StockTransactions] ([CounterLocationId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012084216_AddWarehouseLocations'
)
BEGIN
    CREATE INDEX [IX_StockTransactions_LocationId_Id] ON [dbo].[StockTransactions] ([LocationId], [Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012084216_AddWarehouseLocations'
)
BEGIN
    EXEC(N'ALTER TABLE [dbo].[StockTransactions] ADD CONSTRAINT [CK_StockTransactions_Quantity] CHECK ([Quantity] <> 0 AND [BalanceAfter] >= 0 AND [LocationBalanceAfter] >= 0)');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012084216_AddWarehouseLocations'
)
BEGIN
    EXEC(N'ALTER TABLE [dbo].[StockTransactions] ADD CONSTRAINT [CK_StockTransactions_Type] CHECK ([Type] IN (''RECEIPT'', ''ISSUE'', ''TRANSFER_OUT'', ''TRANSFER_IN'', ''ADJUSTMENT'', ''PURCHASE_RECEIPT'', ''SHIPMENT'', ''PRODUCTION_RECEIPT'', ''PRODUCTION_ISSUE'', ''PRODUCTION_RETURN'', ''RELOCATION_OUT'', ''RELOCATION_IN''))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012084216_AddWarehouseLocations'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Stocks_LocationId_StockItemId] ON [dbo].[Stocks] ([LocationId], [StockItemId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012084216_AddWarehouseLocations'
)
BEGIN
    CREATE INDEX [IX_Stocks_WarehouseId_StockItemId] ON [dbo].[Stocks] ([WarehouseId], [StockItemId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012084216_AddWarehouseLocations'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_WarehouseLocations_WarehouseId_Code] ON [dbo].[WarehouseLocations] ([WarehouseId], [Code]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012084216_AddWarehouseLocations'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_WarehouseLocations_Default] ON [dbo].[WarehouseLocations] ([WarehouseId]) WHERE [IsDefault] = 1 AND [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012084216_AddWarehouseLocations'
)
BEGIN
    ALTER TABLE [dbo].[Stocks] ADD CONSTRAINT [FK_Stocks_WarehouseLocations_LocationId] FOREIGN KEY ([LocationId]) REFERENCES [dbo].[WarehouseLocations] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012084216_AddWarehouseLocations'
)
BEGIN
    ALTER TABLE [dbo].[StockTransactions] ADD CONSTRAINT [FK_StockTransactions_WarehouseLocations_CounterLocationId] FOREIGN KEY ([CounterLocationId]) REFERENCES [dbo].[WarehouseLocations] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012084216_AddWarehouseLocations'
)
BEGIN
    ALTER TABLE [dbo].[StockTransactions] ADD CONSTRAINT [FK_StockTransactions_WarehouseLocations_LocationId] FOREIGN KEY ([LocationId]) REFERENCES [dbo].[WarehouseLocations] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012084216_AddWarehouseLocations'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261012084216_AddWarehouseLocations', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012094549_AddStockLots'
)
BEGIN
    DROP INDEX [IX_Stocks_LocationId_StockItemId] ON [dbo].[Stocks];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012094549_AddStockLots'
)
BEGIN
    ALTER TABLE [dbo].[StockTransactions] ADD [LotBalanceAfter] decimal(18,3) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012094549_AddStockLots'
)
BEGIN
    ALTER TABLE [dbo].[StockTransactions] ADD [LotId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012094549_AddStockLots'
)
BEGIN
    ALTER TABLE [dbo].[Stocks] ADD [LotId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012094549_AddStockLots'
)
BEGIN
    ALTER TABLE [dbo].[StockItems] ADD [IsLotTracked] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012094549_AddStockLots'
)
BEGIN
    CREATE TABLE [dbo].[StockLots] (
        [Id] int NOT NULL IDENTITY,
        [StockItemId] int NOT NULL,
        [LotNo] AS CONCAT(N'T', RIGHT(CONCAT('00000000', [Id]), 8)) PERSISTED,
        [BatchNo] nvarchar(50) NULL,
        [Shade] nvarchar(30) NULL,
        [Width] decimal(9,2) NULL,
        [SupplierRollNo] nvarchar(50) NULL,
        [InitialQuantity] decimal(18,3) NOT NULL,
        [ReceivedAt] datetime2 NOT NULL,
        [PurchaseOrderId] int NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_StockLots] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_StockLots_InitialQuantity] CHECK ([InitialQuantity] > 0),
        CONSTRAINT [CK_StockLots_Width] CHECK ([Width] IS NULL OR [Width] > 0),
        CONSTRAINT [FK_StockLots_PurchaseOrders_PurchaseOrderId] FOREIGN KEY ([PurchaseOrderId]) REFERENCES [dbo].[PurchaseOrders] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_StockLots_StockItems_StockItemId] FOREIGN KEY ([StockItemId]) REFERENCES [dbo].[StockItems] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012094549_AddStockLots'
)
BEGIN
    EXEC(N'UPDATE [dbo].[StockItems] SET [IsLotTracked] = 1 WHERE [ItemType] = N''FABRIC'';');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012094549_AddStockLots'
)
BEGIN
    EXEC(N'
    DECLARE @opened TABLE ([LotId] int NOT NULL, [StockId] int NOT NULL);

    MERGE INTO [dbo].[StockLots] AS target
    USING (
        SELECT s.[Id], s.[StockItemId], s.[Quantity]
        FROM [dbo].[Stocks] AS s
        INNER JOIN [dbo].[StockItems] AS i ON i.[Id] = s.[StockItemId]
        WHERE i.[IsLotTracked] = 1 AND s.[Quantity] > 0
    ) AS source ON 1 = 0
    WHEN NOT MATCHED THEN
        INSERT ([StockItemId], [BatchNo], [InitialQuantity], [ReceivedAt], [CreatedAt], [CreatedBy])
        VALUES (source.[StockItemId], N''AÇILIŞ'', source.[Quantity], SYSUTCDATETIME(), SYSUTCDATETIME(), N''system'')
    OUTPUT inserted.[Id], source.[Id] INTO @opened ([LotId], [StockId]);

    UPDATE s SET s.[LotId] = o.[LotId]
    FROM [dbo].[Stocks] AS s
    INNER JOIN @opened AS o ON o.[StockId] = s.[Id];
    ');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012094549_AddStockLots'
)
BEGIN
    CREATE INDEX [IX_StockTransactions_LotId_Id] ON [dbo].[StockTransactions] ([LotId], [Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012094549_AddStockLots'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Stocks_LocationId_StockItemId_LotId] ON [dbo].[Stocks] ([LocationId], [StockItemId], [LotId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012094549_AddStockLots'
)
BEGIN
    CREATE INDEX [IX_Stocks_LotId] ON [dbo].[Stocks] ([LotId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012094549_AddStockLots'
)
BEGIN
    CREATE INDEX [IX_StockLots_BatchNo] ON [dbo].[StockLots] ([BatchNo]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012094549_AddStockLots'
)
BEGIN
    CREATE UNIQUE INDEX [IX_StockLots_LotNo] ON [dbo].[StockLots] ([LotNo]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012094549_AddStockLots'
)
BEGIN
    CREATE INDEX [IX_StockLots_PurchaseOrderId] ON [dbo].[StockLots] ([PurchaseOrderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012094549_AddStockLots'
)
BEGIN
    CREATE INDEX [IX_StockLots_StockItemId_Id] ON [dbo].[StockLots] ([StockItemId], [Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012094549_AddStockLots'
)
BEGIN
    ALTER TABLE [dbo].[Stocks] ADD CONSTRAINT [FK_Stocks_StockLots_LotId] FOREIGN KEY ([LotId]) REFERENCES [dbo].[StockLots] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012094549_AddStockLots'
)
BEGIN
    ALTER TABLE [dbo].[StockTransactions] ADD CONSTRAINT [FK_StockTransactions_StockLots_LotId] FOREIGN KEY ([LotId]) REFERENCES [dbo].[StockLots] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012094549_AddStockLots'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261012094549_AddStockLots', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012100821_AddUserShortcuts'
)
BEGIN
    CREATE TABLE [dbo].[UserShortcuts] (
        [Id] int NOT NULL IDENTITY,
        [UserId] int NOT NULL,
        [Path] nvarchar(100) NOT NULL,
        [SortOrder] int NOT NULL,
        CONSTRAINT [PK_UserShortcuts] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_UserShortcuts_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012100821_AddUserShortcuts'
)
BEGIN
    CREATE UNIQUE INDEX [IX_UserShortcuts_UserId_Path] ON [dbo].[UserShortcuts] ([UserId], [Path]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012100821_AddUserShortcuts'
)
BEGIN
    CREATE INDEX [IX_UserShortcuts_UserId_SortOrder] ON [dbo].[UserShortcuts] ([UserId], [SortOrder]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012100821_AddUserShortcuts'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261012100821_AddUserShortcuts', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012105608_AddStockReservations'
)
BEGIN
    CREATE TABLE [dbo].[StockReservations] (
        [Id] int NOT NULL IDENTITY,
        [ProductionOrderId] int NOT NULL,
        [WarehouseId] int NOT NULL,
        [StockItemId] int NOT NULL,
        [Quantity] decimal(18,3) NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        [UpdatedBy] nvarchar(100) NOT NULL,
        CONSTRAINT [PK_StockReservations] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_StockReservations_Quantity] CHECK ([Quantity] > 0),
        CONSTRAINT [FK_StockReservations_ProductionOrders_ProductionOrderId] FOREIGN KEY ([ProductionOrderId]) REFERENCES [dbo].[ProductionOrders] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_StockReservations_StockItems_StockItemId] FOREIGN KEY ([StockItemId]) REFERENCES [dbo].[StockItems] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_StockReservations_Warehouses_WarehouseId] FOREIGN KEY ([WarehouseId]) REFERENCES [dbo].[Warehouses] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012105608_AddStockReservations'
)
BEGIN
    CREATE UNIQUE INDEX [IX_StockReservations_ProductionOrderId_WarehouseId_StockItemId] ON [dbo].[StockReservations] ([ProductionOrderId], [WarehouseId], [StockItemId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012105608_AddStockReservations'
)
BEGIN
    CREATE INDEX [IX_StockReservations_StockItemId] ON [dbo].[StockReservations] ([StockItemId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012105608_AddStockReservations'
)
BEGIN
    CREATE INDEX [IX_StockReservations_WarehouseId_StockItemId] ON [dbo].[StockReservations] ([WarehouseId], [StockItemId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012105608_AddStockReservations'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261012105608_AddStockReservations', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012110903_AddOrderMaterialWarehouse'
)
BEGIN
    ALTER TABLE [dbo].[ProductionOrders] ADD [MaterialWarehouseId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012110903_AddOrderMaterialWarehouse'
)
BEGIN
    CREATE INDEX [IX_ProductionOrders_MaterialWarehouseId] ON [dbo].[ProductionOrders] ([MaterialWarehouseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012110903_AddOrderMaterialWarehouse'
)
BEGIN
    ALTER TABLE [dbo].[ProductionOrders] ADD CONSTRAINT [FK_ProductionOrders_Warehouses_MaterialWarehouseId] FOREIGN KEY ([MaterialWarehouseId]) REFERENCES [dbo].[Warehouses] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012110903_AddOrderMaterialWarehouse'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261012110903_AddOrderMaterialWarehouse', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012111423_AddStockCounts'
)
BEGIN
    CREATE TABLE [dbo].[StockCounts] (
        [Id] int NOT NULL IDENTITY,
        [CountNo] nvarchar(30) NOT NULL,
        [WarehouseId] int NOT NULL,
        [LocationId] int NULL,
        [Status] nvarchar(30) NOT NULL,
        [Note] nvarchar(300) NULL,
        [PostedAt] datetime2 NULL,
        [PostedBy] nvarchar(100) NULL,
        [RowVersion] rowversion NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_StockCounts] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_StockCounts_Status] CHECK ([Status] IN ('OPEN', 'POSTED', 'CANCELLED')),
        CONSTRAINT [FK_StockCounts_WarehouseLocations_LocationId] FOREIGN KEY ([LocationId]) REFERENCES [dbo].[WarehouseLocations] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_StockCounts_Warehouses_WarehouseId] FOREIGN KEY ([WarehouseId]) REFERENCES [dbo].[Warehouses] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012111423_AddStockCounts'
)
BEGIN
    CREATE TABLE [dbo].[StockCountLines] (
        [Id] int NOT NULL IDENTITY,
        [StockCountId] int NOT NULL,
        [LocationId] int NOT NULL,
        [StockItemId] int NOT NULL,
        [LotId] int NULL,
        [ExpectedQuantity] decimal(18,3) NOT NULL,
        [CountedQuantity] decimal(18,3) NULL,
        CONSTRAINT [PK_StockCountLines] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_StockCountLines_Quantities] CHECK ([ExpectedQuantity] >= 0 AND ([CountedQuantity] IS NULL OR [CountedQuantity] >= 0)),
        CONSTRAINT [FK_StockCountLines_StockCounts_StockCountId] FOREIGN KEY ([StockCountId]) REFERENCES [dbo].[StockCounts] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_StockCountLines_StockItems_StockItemId] FOREIGN KEY ([StockItemId]) REFERENCES [dbo].[StockItems] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_StockCountLines_StockLots_LotId] FOREIGN KEY ([LotId]) REFERENCES [dbo].[StockLots] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_StockCountLines_WarehouseLocations_LocationId] FOREIGN KEY ([LocationId]) REFERENCES [dbo].[WarehouseLocations] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012111423_AddStockCounts'
)
BEGIN
    CREATE INDEX [IX_StockCountLines_LocationId] ON [dbo].[StockCountLines] ([LocationId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012111423_AddStockCounts'
)
BEGIN
    CREATE INDEX [IX_StockCountLines_LotId] ON [dbo].[StockCountLines] ([LotId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012111423_AddStockCounts'
)
BEGIN
    CREATE UNIQUE INDEX [IX_StockCountLines_StockCountId_LocationId_StockItemId_LotId] ON [dbo].[StockCountLines] ([StockCountId], [LocationId], [StockItemId], [LotId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012111423_AddStockCounts'
)
BEGIN
    CREATE INDEX [IX_StockCountLines_StockItemId] ON [dbo].[StockCountLines] ([StockItemId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012111423_AddStockCounts'
)
BEGIN
    CREATE UNIQUE INDEX [IX_StockCounts_CountNo] ON [dbo].[StockCounts] ([CountNo]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012111423_AddStockCounts'
)
BEGIN
    CREATE INDEX [IX_StockCounts_LocationId] ON [dbo].[StockCounts] ([LocationId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012111423_AddStockCounts'
)
BEGIN
    CREATE INDEX [IX_StockCounts_WarehouseId_Status] ON [dbo].[StockCounts] ([WarehouseId], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012111423_AddStockCounts'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261012111423_AddStockCounts', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012114419_AddStockItemReorder'
)
BEGIN
    ALTER TABLE [dbo].[StockItems] ADD [MinimumStock] decimal(18,3) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012114419_AddStockItemReorder'
)
BEGIN
    ALTER TABLE [dbo].[StockItems] ADD [ReorderQuantity] decimal(18,3) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012114419_AddStockItemReorder'
)
BEGIN
    ALTER TABLE [dbo].[StockItems] ADD [SupplierId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012114419_AddStockItemReorder'
)
BEGIN
    CREATE INDEX [IX_StockItems_SupplierId] ON [dbo].[StockItems] ([SupplierId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012114419_AddStockItemReorder'
)
BEGIN
    EXEC(N'ALTER TABLE [dbo].[StockItems] ADD CONSTRAINT [CK_StockItems_Reorder] CHECK (([MinimumStock] IS NULL OR [MinimumStock] >= 0) AND ([ReorderQuantity] IS NULL OR [ReorderQuantity] > 0))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012114419_AddStockItemReorder'
)
BEGIN
    ALTER TABLE [dbo].[StockItems] ADD CONSTRAINT [FK_StockItems_Suppliers_SupplierId] FOREIGN KEY ([SupplierId]) REFERENCES [dbo].[Suppliers] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012114419_AddStockItemReorder'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261012114419_AddStockItemReorder', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012123343_AddSalesOrders'
)
BEGIN
    ALTER TABLE [dbo].[ProductionOrders] ADD [SalesOrderId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012123343_AddSalesOrders'
)
BEGIN
    CREATE TABLE [dbo].[SalesOrders] (
        [Id] int NOT NULL IDENTITY,
        [OrderNo] nvarchar(30) NOT NULL,
        [CustomerId] int NOT NULL,
        [CustomerOrderNo] nvarchar(50) NULL,
        [OrderDate] date NOT NULL,
        [DueDate] date NOT NULL,
        [Currency] varchar(3) NOT NULL,
        [Status] nvarchar(30) NOT NULL,
        [Notes] nvarchar(1000) NULL,
        [SubmittedAt] datetime2 NULL,
        [SubmittedBy] nvarchar(100) NULL,
        [DecidedAt] datetime2 NULL,
        [DecidedBy] nvarchar(100) NULL,
        [DecisionNote] nvarchar(500) NULL,
        [RowVersion] rowversion NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_SalesOrders] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_SalesOrders_DueDate] CHECK ([DueDate] >= [OrderDate]),
        CONSTRAINT [CK_SalesOrders_Status] CHECK ([Status] IN ('DRAFT', 'SUBMITTED', 'APPROVED', 'CLOSED', 'CANCELLED')),
        CONSTRAINT [FK_SalesOrders_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [dbo].[Customers] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012123343_AddSalesOrders'
)
BEGIN
    CREATE TABLE [dbo].[SalesOrderLines] (
        [Id] int NOT NULL IDENTITY,
        [SalesOrderId] int NOT NULL,
        [ProductId] int NOT NULL,
        [ColorId] int NOT NULL,
        [SizeId] int NOT NULL,
        [Quantity] int NOT NULL,
        [UnitPrice] decimal(18,4) NULL,
        CONSTRAINT [PK_SalesOrderLines] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_SalesOrderLines_Quantity] CHECK ([Quantity] > 0),
        CONSTRAINT [CK_SalesOrderLines_UnitPrice] CHECK ([UnitPrice] IS NULL OR [UnitPrice] >= 0),
        CONSTRAINT [FK_SalesOrderLines_Colors_ColorId] FOREIGN KEY ([ColorId]) REFERENCES [dbo].[Colors] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_SalesOrderLines_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [dbo].[Products] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_SalesOrderLines_SalesOrders_SalesOrderId] FOREIGN KEY ([SalesOrderId]) REFERENCES [dbo].[SalesOrders] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_SalesOrderLines_Sizes_SizeId] FOREIGN KEY ([SizeId]) REFERENCES [dbo].[Sizes] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012123343_AddSalesOrders'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'Description', N'Module') AND [object_id] = OBJECT_ID(N'[dbo].[Permissions]'))
        SET IDENTITY_INSERT [dbo].[Permissions] ON;
    EXEC(N'INSERT INTO [dbo].[Permissions] ([Id], [Code], [Description], [Module])
    VALUES (47, N''SalesOrder.View'', N''Müşteri siparişlerini görüntüleme'', N''SalesOrder''),
    (48, N''SalesOrder.Manage'', N''Müşteri siparişi oluşturma / onaya gönderme'', N''SalesOrder''),
    (49, N''SalesOrder.Approve'', N''Müşteri siparişini onaylama / reddetme'', N''SalesOrder''),
    (50, N''ProductionOrder.Approve'', N''Üretim siparişini onaylayıp üretime açma'', N''ProductionOrder''),
    (51, N''Purchase.Approve'', N''Satın alma siparişini onaylama'', N''Purchase'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'Description', N'Module') AND [object_id] = OBJECT_ID(N'[dbo].[Permissions]'))
        SET IDENTITY_INSERT [dbo].[Permissions] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012123343_AddSalesOrders'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'PermissionId', N'RoleId') AND [object_id] = OBJECT_ID(N'[dbo].[RolePermissions]'))
        SET IDENTITY_INSERT [dbo].[RolePermissions] ON;
    EXEC(N'INSERT INTO [dbo].[RolePermissions] ([PermissionId], [RoleId])
    VALUES (47, 1),
    (48, 1),
    (49, 1),
    (50, 1),
    (51, 1),
    (47, 2),
    (48, 2),
    (49, 2),
    (50, 2),
    (47, 3),
    (48, 3),
    (50, 3),
    (51, 3),
    (47, 9),
    (49, 9)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'PermissionId', N'RoleId') AND [object_id] = OBJECT_ID(N'[dbo].[RolePermissions]'))
        SET IDENTITY_INSERT [dbo].[RolePermissions] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012123343_AddSalesOrders'
)
BEGIN
    INSERT INTO [dbo].[RolePermissions] ([RoleId], [PermissionId])
    SELECT rp.[RoleId], map.[ApproveId]
    FROM [dbo].[RolePermissions] AS rp
    INNER JOIN (VALUES (9, 50), (36, 51)) AS map([BaseId], [ApproveId]) ON map.[BaseId] = rp.[PermissionId]
    WHERE NOT EXISTS (
        SELECT 1 FROM [dbo].[RolePermissions] AS existing
        WHERE existing.[RoleId] = rp.[RoleId] AND existing.[PermissionId] = map.[ApproveId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012123343_AddSalesOrders'
)
BEGIN
    CREATE INDEX [IX_ProductionOrders_SalesOrderId] ON [dbo].[ProductionOrders] ([SalesOrderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012123343_AddSalesOrders'
)
BEGIN
    CREATE INDEX [IX_SalesOrderLines_ColorId] ON [dbo].[SalesOrderLines] ([ColorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012123343_AddSalesOrders'
)
BEGIN
    CREATE INDEX [IX_SalesOrderLines_ProductId] ON [dbo].[SalesOrderLines] ([ProductId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012123343_AddSalesOrders'
)
BEGIN
    CREATE UNIQUE INDEX [IX_SalesOrderLines_SalesOrderId_ProductId_ColorId_SizeId] ON [dbo].[SalesOrderLines] ([SalesOrderId], [ProductId], [ColorId], [SizeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012123343_AddSalesOrders'
)
BEGIN
    CREATE INDEX [IX_SalesOrderLines_SizeId] ON [dbo].[SalesOrderLines] ([SizeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012123343_AddSalesOrders'
)
BEGIN
    CREATE INDEX [IX_SalesOrders_CustomerId] ON [dbo].[SalesOrders] ([CustomerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012123343_AddSalesOrders'
)
BEGIN
    CREATE UNIQUE INDEX [IX_SalesOrders_OrderNo] ON [dbo].[SalesOrders] ([OrderNo]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012123343_AddSalesOrders'
)
BEGIN
    CREATE INDEX [IX_SalesOrders_Status_DueDate] ON [dbo].[SalesOrders] ([Status], [DueDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012123343_AddSalesOrders'
)
BEGIN
    ALTER TABLE [dbo].[ProductionOrders] ADD CONSTRAINT [FK_ProductionOrders_SalesOrders_SalesOrderId] FOREIGN KEY ([SalesOrderId]) REFERENCES [dbo].[SalesOrders] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012123343_AddSalesOrders'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261012123343_AddSalesOrders', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012124118_AddWarehouseReportPermission'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'Description', N'Module') AND [object_id] = OBJECT_ID(N'[dbo].[Permissions]'))
        SET IDENTITY_INSERT [dbo].[Permissions] ON;
    EXEC(N'INSERT INTO [dbo].[Permissions] ([Id], [Code], [Description], [Module])
    VALUES (52, N''Report.Warehouse'', N''Yalnızca depo raporlarını görüntüleme'', N''Report'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'Description', N'Module') AND [object_id] = OBJECT_ID(N'[dbo].[Permissions]'))
        SET IDENTITY_INSERT [dbo].[Permissions] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012124118_AddWarehouseReportPermission'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'PermissionId', N'RoleId') AND [object_id] = OBJECT_ID(N'[dbo].[RolePermissions]'))
        SET IDENTITY_INSERT [dbo].[RolePermissions] ON;
    EXEC(N'INSERT INTO [dbo].[RolePermissions] ([PermissionId], [RoleId])
    VALUES (52, 1),
    (52, 6)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'PermissionId', N'RoleId') AND [object_id] = OBJECT_ID(N'[dbo].[RolePermissions]'))
        SET IDENTITY_INSERT [dbo].[RolePermissions] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012124118_AddWarehouseReportPermission'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261012124118_AddWarehouseReportPermission', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013131352_AddShipmentSalesOrderAndPurchaseRate'
)
BEGIN
    ALTER TABLE [dbo].[Shipments] ADD [SalesOrderId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013131352_AddShipmentSalesOrderAndPurchaseRate'
)
BEGIN
    ALTER TABLE [dbo].[PurchaseOrders] ADD [ExchangeRate] decimal(18,6) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013131352_AddShipmentSalesOrderAndPurchaseRate'
)
BEGIN
    EXEC(N'UPDATE [dbo].[Permissions] SET [Description] = N''Demet operasyonu başlatma (kullanılmıyor: tablet yetkileri saha rollerinden yönetilir)''
    WHERE [Id] = 14;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013131352_AddShipmentSalesOrderAndPurchaseRate'
)
BEGIN
    EXEC(N'UPDATE [dbo].[Permissions] SET [Description] = N''Demet operasyonu tamamlama (kullanılmıyor: tablet yetkileri saha rollerinden yönetilir)''
    WHERE [Id] = 15;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013131352_AddShipmentSalesOrderAndPurchaseRate'
)
BEGIN
    EXEC(N'UPDATE [dbo].[Permissions] SET [Description] = N''Üretim miktarı bildirme (kullanılmıyor: tablet yetkileri saha rollerinden yönetilir)''
    WHERE [Id] = 17;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013131352_AddShipmentSalesOrderAndPurchaseRate'
)
BEGIN
    EXEC(N'UPDATE [dbo].[Permissions] SET [Description] = N''Operatör güncelleme, oturum kapatma, süren işi bırakma''
    WHERE [Id] = 23;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013131352_AddShipmentSalesOrderAndPurchaseRate'
)
BEGIN
    CREATE INDEX [IX_Shipments_SalesOrderId_Status] ON [dbo].[Shipments] ([SalesOrderId], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013131352_AddShipmentSalesOrderAndPurchaseRate'
)
BEGIN
    EXEC(N'ALTER TABLE [dbo].[PurchaseOrders] ADD CONSTRAINT [CK_PurchaseOrders_ExchangeRate] CHECK ([ExchangeRate] IS NULL OR [ExchangeRate] > 0)');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013131352_AddShipmentSalesOrderAndPurchaseRate'
)
BEGIN
    ALTER TABLE [dbo].[Shipments] ADD CONSTRAINT [FK_Shipments_SalesOrders_SalesOrderId] FOREIGN KEY ([SalesOrderId]) REFERENCES [dbo].[SalesOrders] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013131352_AddShipmentSalesOrderAndPurchaseRate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261013131352_AddShipmentSalesOrderAndPurchaseRate', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021043951_AddCustomerBuyersAndOrderDefaults'
)
BEGIN
    ALTER TABLE [dbo].[SalesOrders] ADD [CustomerBuyerId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021043951_AddCustomerBuyersAndOrderDefaults'
)
BEGIN
    ALTER TABLE [dbo].[SalesOrders] ADD [DeliveryAddressId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021043951_AddCustomerBuyersAndOrderDefaults'
)
BEGIN
    ALTER TABLE [dbo].[Customers] ADD [Currency] char(3) NOT NULL DEFAULT 'TRY';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021043951_AddCustomerBuyersAndOrderDefaults'
)
BEGIN
    ALTER TABLE [dbo].[Customers] ADD [RequireUniqueCustomerOrderNo] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021043951_AddCustomerBuyersAndOrderDefaults'
)
BEGIN
    CREATE TABLE [dbo].[CustomerBuyers] (
        [Id] int NOT NULL IDENTITY,
        [CustomerId] int NOT NULL,
        [Code] nvarchar(30) NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [ContactName] nvarchar(100) NULL,
        [Email] nvarchar(256) NULL,
        [Phone] nvarchar(30) NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_CustomerBuyers] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CustomerBuyers_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [dbo].[Customers] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021043951_AddCustomerBuyersAndOrderDefaults'
)
BEGIN
    CREATE INDEX [IX_SalesOrders_CustomerBuyerId] ON [dbo].[SalesOrders] ([CustomerBuyerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021043951_AddCustomerBuyersAndOrderDefaults'
)
BEGIN
    CREATE INDEX [IX_SalesOrders_DeliveryAddressId] ON [dbo].[SalesOrders] ([DeliveryAddressId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021043951_AddCustomerBuyersAndOrderDefaults'
)
BEGIN
    CREATE UNIQUE INDEX [IX_CustomerBuyers_CustomerId_Code] ON [dbo].[CustomerBuyers] ([CustomerId], [Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021043951_AddCustomerBuyersAndOrderDefaults'
)
BEGIN
    ALTER TABLE [dbo].[SalesOrders] ADD CONSTRAINT [FK_SalesOrders_CustomerAddresses_DeliveryAddressId] FOREIGN KEY ([DeliveryAddressId]) REFERENCES [dbo].[CustomerAddresses] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021043951_AddCustomerBuyersAndOrderDefaults'
)
BEGIN
    ALTER TABLE [dbo].[SalesOrders] ADD CONSTRAINT [FK_SalesOrders_CustomerBuyers_CustomerBuyerId] FOREIGN KEY ([CustomerBuyerId]) REFERENCES [dbo].[CustomerBuyers] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021043951_AddCustomerBuyersAndOrderDefaults'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261021043951_AddCustomerBuyersAndOrderDefaults', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021045039_AddCurrenciesAndExchangeRates'
)
BEGIN
    CREATE TABLE [dbo].[Currencies] (
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
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021045039_AddCurrenciesAndExchangeRates'
)
BEGIN
    CREATE TABLE [dbo].[ExchangeRates] (
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
        CONSTRAINT [FK_ExchangeRates_Currencies_CurrencyId] FOREIGN KEY ([CurrencyId]) REFERENCES [dbo].[Currencies] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021045039_AddCurrenciesAndExchangeRates'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'CreatedAt', N'CreatedBy', N'DecimalPlaces', N'DeletedAt', N'DeletedBy', N'FetchFromTcmb', N'IsActive', N'IsDeleted', N'Name', N'Symbol', N'UpdatedAt', N'UpdatedBy') AND [object_id] = OBJECT_ID(N'[dbo].[Currencies]'))
        SET IDENTITY_INSERT [dbo].[Currencies] ON;
    EXEC(N'INSERT INTO [dbo].[Currencies] ([Id], [Code], [CreatedAt], [CreatedBy], [DecimalPlaces], [DeletedAt], [DeletedBy], [FetchFromTcmb], [IsActive], [IsDeleted], [Name], [Symbol], [UpdatedAt], [UpdatedBy])
    VALUES (1, ''TRY'', ''2026-01-01T00:00:00.0000000Z'', N''system'', 2, NULL, NULL, CAST(0 AS bit), CAST(1 AS bit), CAST(0 AS bit), N''Türk lirası'', N''₺'', NULL, NULL),
    (2, ''USD'', ''2026-01-01T00:00:00.0000000Z'', N''system'', 2, NULL, NULL, CAST(1 AS bit), CAST(1 AS bit), CAST(0 AS bit), N''ABD doları'', N''$'', NULL, NULL),
    (3, ''EUR'', ''2026-01-01T00:00:00.0000000Z'', N''system'', 2, NULL, NULL, CAST(1 AS bit), CAST(1 AS bit), CAST(0 AS bit), N''Euro'', N''€'', NULL, NULL),
    (4, ''GBP'', ''2026-01-01T00:00:00.0000000Z'', N''system'', 2, NULL, NULL, CAST(1 AS bit), CAST(1 AS bit), CAST(0 AS bit), N''İngiliz sterlini'', N''£'', NULL, NULL)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'CreatedAt', N'CreatedBy', N'DecimalPlaces', N'DeletedAt', N'DeletedBy', N'FetchFromTcmb', N'IsActive', N'IsDeleted', N'Name', N'Symbol', N'UpdatedAt', N'UpdatedBy') AND [object_id] = OBJECT_ID(N'[dbo].[Currencies]'))
        SET IDENTITY_INSERT [dbo].[Currencies] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021045039_AddCurrenciesAndExchangeRates'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Currencies_Code] ON [dbo].[Currencies] ([Code]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021045039_AddCurrenciesAndExchangeRates'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ExchangeRates_CurrencyId_RateDate] ON [dbo].[ExchangeRates] ([CurrencyId], [RateDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021045039_AddCurrenciesAndExchangeRates'
)
BEGIN
    CREATE INDEX [IX_ExchangeRates_RateDate] ON [dbo].[ExchangeRates] ([RateDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021045039_AddCurrenciesAndExchangeRates'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261021045039_AddCurrenciesAndExchangeRates', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021050846_AddPriceListsAndDiscounts'
)
BEGIN
    CREATE TABLE [dbo].[CustomerDiscounts] (
        [Id] int NOT NULL IDENTITY,
        [CustomerId] int NOT NULL,
        [ProductId] int NULL,
        [DiscountPercent] decimal(5,2) NOT NULL,
        [ValidFrom] date NOT NULL,
        [ValidTo] date NULL,
        [Notes] nvarchar(500) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_CustomerDiscounts] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_CustomerDiscounts_Percent] CHECK ([DiscountPercent] > 0 AND [DiscountPercent] < 100),
        CONSTRAINT [CK_CustomerDiscounts_Validity] CHECK ([ValidTo] IS NULL OR [ValidTo] >= [ValidFrom]),
        CONSTRAINT [FK_CustomerDiscounts_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [dbo].[Customers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_CustomerDiscounts_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [dbo].[Products] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021050846_AddPriceListsAndDiscounts'
)
BEGIN
    CREATE TABLE [dbo].[PriceLists] (
        [Id] int NOT NULL IDENTITY,
        [Code] nvarchar(30) NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [CustomerId] int NULL,
        [Currency] char(3) NOT NULL,
        [ValidFrom] date NOT NULL,
        [ValidTo] date NULL,
        [Notes] nvarchar(500) NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_PriceLists] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_PriceLists_Validity] CHECK ([ValidTo] IS NULL OR [ValidTo] >= [ValidFrom]),
        CONSTRAINT [FK_PriceLists_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [dbo].[Customers] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021050846_AddPriceListsAndDiscounts'
)
BEGIN
    CREATE TABLE [dbo].[PriceListItems] (
        [Id] int NOT NULL IDENTITY,
        [PriceListId] int NOT NULL,
        [ProductId] int NOT NULL,
        [ColorId] int NULL,
        [SizeId] int NULL,
        [UnitPrice] decimal(18,4) NOT NULL,
        CONSTRAINT [PK_PriceListItems] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_PriceListItems_UnitPrice] CHECK ([UnitPrice] >= 0),
        CONSTRAINT [FK_PriceListItems_Colors_ColorId] FOREIGN KEY ([ColorId]) REFERENCES [dbo].[Colors] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PriceListItems_PriceLists_PriceListId] FOREIGN KEY ([PriceListId]) REFERENCES [dbo].[PriceLists] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PriceListItems_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [dbo].[Products] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PriceListItems_Sizes_SizeId] FOREIGN KEY ([SizeId]) REFERENCES [dbo].[Sizes] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021050846_AddPriceListsAndDiscounts'
)
BEGIN
    CREATE INDEX [IX_CustomerDiscounts_CustomerId_ProductId_ValidFrom] ON [dbo].[CustomerDiscounts] ([CustomerId], [ProductId], [ValidFrom]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021050846_AddPriceListsAndDiscounts'
)
BEGIN
    CREATE INDEX [IX_CustomerDiscounts_ProductId] ON [dbo].[CustomerDiscounts] ([ProductId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021050846_AddPriceListsAndDiscounts'
)
BEGIN
    CREATE INDEX [IX_PriceListItems_ColorId] ON [dbo].[PriceListItems] ([ColorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021050846_AddPriceListsAndDiscounts'
)
BEGIN
    CREATE UNIQUE INDEX [IX_PriceListItems_PriceListId_ProductId_ColorId_SizeId] ON [dbo].[PriceListItems] ([PriceListId], [ProductId], [ColorId], [SizeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021050846_AddPriceListsAndDiscounts'
)
BEGIN
    CREATE INDEX [IX_PriceListItems_ProductId] ON [dbo].[PriceListItems] ([ProductId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021050846_AddPriceListsAndDiscounts'
)
BEGIN
    CREATE INDEX [IX_PriceListItems_SizeId] ON [dbo].[PriceListItems] ([SizeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021050846_AddPriceListsAndDiscounts'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_PriceLists_Code] ON [dbo].[PriceLists] ([Code]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021050846_AddPriceListsAndDiscounts'
)
BEGIN
    CREATE INDEX [IX_PriceLists_CustomerId_ValidFrom] ON [dbo].[PriceLists] ([CustomerId], [ValidFrom]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021050846_AddPriceListsAndDiscounts'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261021050846_AddPriceListsAndDiscounts', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021053300_AddUserFormDrafts'
)
BEGIN
    CREATE TABLE [dbo].[UserFormDrafts] (
        [Id] int NOT NULL IDENTITY,
        [UserId] int NOT NULL,
        [FormKey] varchar(100) NOT NULL,
        [Payload] nvarchar(max) NOT NULL,
        [Title] nvarchar(200) NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_UserFormDrafts] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_UserFormDrafts_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021053300_AddUserFormDrafts'
)
BEGIN
    CREATE UNIQUE INDEX [IX_UserFormDrafts_UserId_FormKey] ON [dbo].[UserFormDrafts] ([UserId], [FormKey]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021053300_AddUserFormDrafts'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261021053300_AddUserFormDrafts', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021054616_AddApprovalFlows'
)
BEGIN
    ALTER TABLE [dbo].[SalesOrders] ADD [ApprovalRound] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021054616_AddApprovalFlows'
)
BEGIN
    CREATE TABLE [dbo].[ApprovalFlows] (
        [Id] int NOT NULL IDENTITY,
        [DocumentType] nvarchar(30) NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_ApprovalFlows] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_ApprovalFlows_DocumentType] CHECK ([DocumentType] IN ('SALES_ORDER'))
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021054616_AddApprovalFlows'
)
BEGIN
    CREATE TABLE [dbo].[SalesOrderApprovalEvents] (
        [Id] int NOT NULL IDENTITY,
        [SalesOrderId] int NOT NULL,
        [Round] int NOT NULL,
        [EventType] nvarchar(30) NOT NULL,
        [StepName] nvarchar(100) NULL,
        [UserName] nvarchar(100) NOT NULL,
        [FullName] nvarchar(200) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [Note] nvarchar(500) NULL,
        CONSTRAINT [PK_SalesOrderApprovalEvents] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_SalesOrderApprovalEvents_EventType] CHECK ([EventType] IN ('SUBMITTED', 'APPROVED', 'REJECTED', 'REVISED', 'CANCELLED')),
        CONSTRAINT [FK_SalesOrderApprovalEvents_SalesOrders_SalesOrderId] FOREIGN KEY ([SalesOrderId]) REFERENCES [dbo].[SalesOrders] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021054616_AddApprovalFlows'
)
BEGIN
    CREATE TABLE [dbo].[SalesOrderApprovalSteps] (
        [Id] int NOT NULL IDENTITY,
        [SalesOrderId] int NOT NULL,
        [Round] int NOT NULL,
        [Sequence] int NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [ApproverType] nvarchar(30) NOT NULL,
        [RoleId] int NULL,
        [ApproverUserIds] varchar(1000) NULL,
        [ApproverNames] nvarchar(1000) NOT NULL,
        [Status] nvarchar(30) NOT NULL,
        [DecidedAt] datetime2 NULL,
        [DecidedBy] nvarchar(100) NULL,
        [Note] nvarchar(500) NULL,
        CONSTRAINT [PK_SalesOrderApprovalSteps] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_SalesOrderApprovalSteps_ApproverType] CHECK ([ApproverType] IN ('ROLE', 'USERS')),
        CONSTRAINT [CK_SalesOrderApprovalSteps_Status] CHECK ([Status] IN ('WAITING', 'PENDING', 'APPROVED', 'REJECTED', 'CANCELLED')),
        CONSTRAINT [FK_SalesOrderApprovalSteps_SalesOrders_SalesOrderId] FOREIGN KEY ([SalesOrderId]) REFERENCES [dbo].[SalesOrders] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021054616_AddApprovalFlows'
)
BEGIN
    CREATE TABLE [dbo].[ApprovalFlowSteps] (
        [Id] int NOT NULL IDENTITY,
        [ApprovalFlowId] int NOT NULL,
        [Sequence] int NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [ApproverType] nvarchar(30) NOT NULL,
        [RoleId] int NULL,
        [MinAmount] decimal(18,4) NULL,
        CONSTRAINT [PK_ApprovalFlowSteps] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_ApprovalFlowSteps_ApproverType] CHECK ([ApproverType] IN ('ROLE', 'USERS')),
        CONSTRAINT [CK_ApprovalFlowSteps_MinAmount] CHECK ([MinAmount] IS NULL OR [MinAmount] >= 0),
        CONSTRAINT [CK_ApprovalFlowSteps_Role] CHECK ([ApproverType] <> 'ROLE' OR [RoleId] IS NOT NULL),
        CONSTRAINT [FK_ApprovalFlowSteps_ApprovalFlows_ApprovalFlowId] FOREIGN KEY ([ApprovalFlowId]) REFERENCES [dbo].[ApprovalFlows] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ApprovalFlowSteps_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [dbo].[Roles] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021054616_AddApprovalFlows'
)
BEGIN
    CREATE TABLE [dbo].[ApprovalFlowStepUsers] (
        [ApprovalFlowStepId] int NOT NULL,
        [UserId] int NOT NULL,
        CONSTRAINT [PK_ApprovalFlowStepUsers] PRIMARY KEY ([ApprovalFlowStepId], [UserId]),
        CONSTRAINT [FK_ApprovalFlowStepUsers_ApprovalFlowSteps_ApprovalFlowStepId] FOREIGN KEY ([ApprovalFlowStepId]) REFERENCES [dbo].[ApprovalFlowSteps] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ApprovalFlowStepUsers_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021054616_AddApprovalFlows'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ApprovalFlows_DocumentType] ON [dbo].[ApprovalFlows] ([DocumentType]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021054616_AddApprovalFlows'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ApprovalFlowSteps_ApprovalFlowId_Sequence] ON [dbo].[ApprovalFlowSteps] ([ApprovalFlowId], [Sequence]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021054616_AddApprovalFlows'
)
BEGIN
    CREATE INDEX [IX_ApprovalFlowSteps_RoleId] ON [dbo].[ApprovalFlowSteps] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021054616_AddApprovalFlows'
)
BEGIN
    CREATE INDEX [IX_ApprovalFlowStepUsers_UserId] ON [dbo].[ApprovalFlowStepUsers] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021054616_AddApprovalFlows'
)
BEGIN
    CREATE INDEX [IX_SalesOrderApprovalEvents_SalesOrderId_CreatedAt] ON [dbo].[SalesOrderApprovalEvents] ([SalesOrderId], [CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021054616_AddApprovalFlows'
)
BEGIN
    CREATE UNIQUE INDEX [IX_SalesOrderApprovalSteps_SalesOrderId_Round_Sequence] ON [dbo].[SalesOrderApprovalSteps] ([SalesOrderId], [Round], [Sequence]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021054616_AddApprovalFlows'
)
BEGIN
    EXEC(N'CREATE INDEX [IX_SalesOrderApprovalSteps_Status] ON [dbo].[SalesOrderApprovalSteps] ([Status]) WHERE [Status] = ''PENDING''');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021054616_AddApprovalFlows'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261021054616_AddApprovalFlows', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021061824_AddSalesOrderClosing'
)
BEGIN
    ALTER TABLE [dbo].[SalesOrders] ADD [CloseNote] nvarchar(500) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021061824_AddSalesOrderClosing'
)
BEGIN
    ALTER TABLE [dbo].[SalesOrders] ADD [ClosedAt] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021061824_AddSalesOrderClosing'
)
BEGIN
    ALTER TABLE [dbo].[SalesOrders] ADD [ClosedAutomatically] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021061824_AddSalesOrderClosing'
)
BEGIN
    ALTER TABLE [dbo].[SalesOrders] ADD [ClosedBy] nvarchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021061824_AddSalesOrderClosing'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'Description', N'Module') AND [object_id] = OBJECT_ID(N'[dbo].[Permissions]'))
        SET IDENTITY_INSERT [dbo].[Permissions] ON;
    EXEC(N'INSERT INTO [dbo].[Permissions] ([Id], [Code], [Description], [Module])
    VALUES (53, N''SalesOrder.Close'', N''Müşteri siparişini elle kapatma (kalan sevk edilmeyecekse)'', N''SalesOrder'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'Description', N'Module') AND [object_id] = OBJECT_ID(N'[dbo].[Permissions]'))
        SET IDENTITY_INSERT [dbo].[Permissions] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021061824_AddSalesOrderClosing'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'PermissionId', N'RoleId') AND [object_id] = OBJECT_ID(N'[dbo].[RolePermissions]'))
        SET IDENTITY_INSERT [dbo].[RolePermissions] ON;
    EXEC(N'INSERT INTO [dbo].[RolePermissions] ([PermissionId], [RoleId])
    VALUES (53, 1)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'PermissionId', N'RoleId') AND [object_id] = OBJECT_ID(N'[dbo].[RolePermissions]'))
        SET IDENTITY_INSERT [dbo].[RolePermissions] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021061824_AddSalesOrderClosing'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261021061824_AddSalesOrderClosing', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021062901_AddSalesOrderReservations'
)
BEGIN
    ALTER TABLE [dbo].[SalesOrderLines] ADD [ShippedFromReservation] decimal(18,3) NOT NULL DEFAULT 0.0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021062901_AddSalesOrderReservations'
)
BEGIN
    CREATE TABLE [dbo].[SalesOrderReservations] (
        [Id] int NOT NULL IDENTITY,
        [SalesOrderId] int NOT NULL,
        [SalesOrderLineId] int NOT NULL,
        [WarehouseId] int NOT NULL,
        [StockItemId] int NOT NULL,
        [Quantity] decimal(18,3) NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        [UpdatedBy] nvarchar(100) NOT NULL,
        CONSTRAINT [PK_SalesOrderReservations] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_SalesOrderReservations_Quantity] CHECK ([Quantity] > 0),
        CONSTRAINT [FK_SalesOrderReservations_SalesOrderLines_SalesOrderLineId] FOREIGN KEY ([SalesOrderLineId]) REFERENCES [dbo].[SalesOrderLines] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_SalesOrderReservations_SalesOrders_SalesOrderId] FOREIGN KEY ([SalesOrderId]) REFERENCES [dbo].[SalesOrders] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_SalesOrderReservations_StockItems_StockItemId] FOREIGN KEY ([StockItemId]) REFERENCES [dbo].[StockItems] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_SalesOrderReservations_Warehouses_WarehouseId] FOREIGN KEY ([WarehouseId]) REFERENCES [dbo].[Warehouses] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021062901_AddSalesOrderReservations'
)
BEGIN
    CREATE INDEX [IX_SalesOrderReservations_SalesOrderId] ON [dbo].[SalesOrderReservations] ([SalesOrderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021062901_AddSalesOrderReservations'
)
BEGIN
    CREATE UNIQUE INDEX [IX_SalesOrderReservations_SalesOrderLineId_WarehouseId] ON [dbo].[SalesOrderReservations] ([SalesOrderLineId], [WarehouseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021062901_AddSalesOrderReservations'
)
BEGIN
    CREATE INDEX [IX_SalesOrderReservations_StockItemId] ON [dbo].[SalesOrderReservations] ([StockItemId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021062901_AddSalesOrderReservations'
)
BEGIN
    CREATE INDEX [IX_SalesOrderReservations_WarehouseId_StockItemId] ON [dbo].[SalesOrderReservations] ([WarehouseId], [StockItemId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021062901_AddSalesOrderReservations'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261021062901_AddSalesOrderReservations', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021065230_AddPasswordResetAndRememberMe'
)
BEGIN
    ALTER TABLE [dbo].[RefreshTokens] ADD [IsPersistent] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021065230_AddPasswordResetAndRememberMe'
)
BEGIN
    CREATE TABLE [dbo].[PasswordResetTokens] (
        [Id] int NOT NULL IDENTITY,
        [UserId] int NOT NULL,
        [TokenHash] varchar(64) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [ExpiresAt] datetime2 NOT NULL,
        [RequestedByIp] nvarchar(45) NULL,
        [UsedAt] datetime2 NULL,
        CONSTRAINT [PK_PasswordResetTokens] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PasswordResetTokens_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021065230_AddPasswordResetAndRememberMe'
)
BEGIN
    CREATE UNIQUE INDEX [IX_PasswordResetTokens_TokenHash] ON [dbo].[PasswordResetTokens] ([TokenHash]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021065230_AddPasswordResetAndRememberMe'
)
BEGIN
    CREATE INDEX [IX_PasswordResetTokens_UserId] ON [dbo].[PasswordResetTokens] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021065230_AddPasswordResetAndRememberMe'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261021065230_AddPasswordResetAndRememberMe', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021073900_AddPricingPermissions'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'Description', N'Module') AND [object_id] = OBJECT_ID(N'[dbo].[Permissions]'))
        SET IDENTITY_INSERT [dbo].[Permissions] ON;
    EXEC(N'INSERT INTO [dbo].[Permissions] ([Id], [Code], [Description], [Module])
    VALUES (54, N''Pricing.View'', N''Fiyat listeleri, iskontolar, para birimleri ve döviz kurlarını görüntüleme'', N''Pricing''),
    (55, N''Pricing.Manage'', N''Fiyat listesi, iskonto, para birimi ve döviz kuru düzenleme'', N''Pricing'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'Description', N'Module') AND [object_id] = OBJECT_ID(N'[dbo].[Permissions]'))
        SET IDENTITY_INSERT [dbo].[Permissions] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021073900_AddPricingPermissions'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'PermissionId', N'RoleId') AND [object_id] = OBJECT_ID(N'[dbo].[RolePermissions]'))
        SET IDENTITY_INSERT [dbo].[RolePermissions] ON;
    EXEC(N'INSERT INTO [dbo].[RolePermissions] ([PermissionId], [RoleId])
    VALUES (54, 1),
    (55, 1),
    (54, 2),
    (55, 2),
    (54, 3),
    (55, 3),
    (54, 9)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'PermissionId', N'RoleId') AND [object_id] = OBJECT_ID(N'[dbo].[RolePermissions]'))
        SET IDENTITY_INSERT [dbo].[RolePermissions] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021073900_AddPricingPermissions'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261021073900_AddPricingPermissions', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022054143_LinkPurchaseItemsToProductionOrders'
)
BEGIN
    ALTER TABLE [dbo].[PurchaseOrderItems] ADD [ProductionOrderId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022054143_LinkPurchaseItemsToProductionOrders'
)
BEGIN
    CREATE INDEX [IX_PurchaseOrderItems_ProductionOrderId] ON [dbo].[PurchaseOrderItems] ([ProductionOrderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022054143_LinkPurchaseItemsToProductionOrders'
)
BEGIN
    ALTER TABLE [dbo].[PurchaseOrderItems] ADD CONSTRAINT [FK_PurchaseOrderItems_ProductionOrders_ProductionOrderId] FOREIGN KEY ([ProductionOrderId]) REFERENCES [dbo].[ProductionOrders] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022054143_LinkPurchaseItemsToProductionOrders'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261022054143_LinkPurchaseItemsToProductionOrders', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022102915_AddPurchasingRole'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'IsSystem', N'Name') AND [object_id] = OBJECT_ID(N'[dbo].[Roles]'))
        SET IDENTITY_INSERT [dbo].[Roles] ON;
    EXEC(N'INSERT INTO [dbo].[Roles] ([Id], [Code], [IsSystem], [Name])
    VALUES (10, N''PURCHASING'', CAST(1 AS bit), N''Satın Alma'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'IsSystem', N'Name') AND [object_id] = OBJECT_ID(N'[dbo].[Roles]'))
        SET IDENTITY_INSERT [dbo].[Roles] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022102915_AddPurchasingRole'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'PermissionId', N'RoleId') AND [object_id] = OBJECT_ID(N'[dbo].[RolePermissions]'))
        SET IDENTITY_INSERT [dbo].[RolePermissions] ON;
    EXEC(N'INSERT INTO [dbo].[RolePermissions] ([PermissionId], [RoleId])
    VALUES (7, 10),
    (24, 10),
    (26, 10),
    (35, 10),
    (36, 10),
    (51, 10)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'PermissionId', N'RoleId') AND [object_id] = OBJECT_ID(N'[dbo].[RolePermissions]'))
        SET IDENTITY_INSERT [dbo].[RolePermissions] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022102915_AddPurchasingRole'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261022102915_AddPurchasingRole', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022211546_AddManualReportAndCardReleasePermissions'
)
BEGIN
    ALTER TABLE [dbo].[BundleOperationEvents] ADD [EnteredBy] nvarchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022211546_AddManualReportAndCardReleasePermissions'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'Description', N'Module') AND [object_id] = OBJECT_ID(N'[dbo].[Permissions]'))
        SET IDENTITY_INSERT [dbo].[Permissions] ON;
    EXEC(N'INSERT INTO [dbo].[Permissions] ([Id], [Code], [Description], [Module])
    VALUES (56, N''Production.ManualReport'', N''Tabletsiz / kartsız elle üretim bildirimi (askıda kalan iş, arızalı tablet)'', N''Production''),
    (57, N''Bundle.ReleaseCard'', N''Kart boşaltma: okutulan demet kartını demetten ayırma'', N''Bundle'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'Description', N'Module') AND [object_id] = OBJECT_ID(N'[dbo].[Permissions]'))
        SET IDENTITY_INSERT [dbo].[Permissions] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022211546_AddManualReportAndCardReleasePermissions'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'PermissionId', N'RoleId') AND [object_id] = OBJECT_ID(N'[dbo].[RolePermissions]'))
        SET IDENTITY_INSERT [dbo].[RolePermissions] ON;
    EXEC(N'INSERT INTO [dbo].[RolePermissions] ([PermissionId], [RoleId])
    VALUES (56, 1),
    (57, 1),
    (56, 2),
    (57, 2),
    (56, 3),
    (57, 3),
    (56, 4),
    (57, 4)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'PermissionId', N'RoleId') AND [object_id] = OBJECT_ID(N'[dbo].[RolePermissions]'))
        SET IDENTITY_INSERT [dbo].[RolePermissions] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022211546_AddManualReportAndCardReleasePermissions'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261022211546_AddManualReportAndCardReleasePermissions', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022214829_AddOperationSequenceMode'
)
BEGIN
    ALTER TABLE [dbo].[ProductionOrders] ADD [OperationSequence] nvarchar(30) NOT NULL DEFAULT N'SEQUENTIAL';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022214829_AddOperationSequenceMode'
)
BEGIN
    ALTER TABLE [dbo].[Bundles] ADD [SequenceMode] nvarchar(30) NOT NULL DEFAULT N'ROUTE';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022214829_AddOperationSequenceMode'
)
BEGIN
    EXEC(N'UPDATE [dbo].[ProductionOrders] SET [OperationSequence] = ''ROUTE'' WHERE EXISTS (SELECT 1 FROM [dbo].[Bundles] b WHERE b.[ProductionOrderId] = [ProductionOrders].[Id]);');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022214829_AddOperationSequenceMode'
)
BEGIN
    EXEC(N'ALTER TABLE [dbo].[ProductionOrders] ADD CONSTRAINT [CK_ProductionOrders_OperationSequence] CHECK ([OperationSequence] IN (''SEQUENTIAL'', ''FREE'', ''ROUTE''))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022214829_AddOperationSequenceMode'
)
BEGIN
    EXEC(N'ALTER TABLE [dbo].[Bundles] ADD CONSTRAINT [CK_Bundles_SequenceMode] CHECK ([SequenceMode] IN (''SEQUENTIAL'', ''FREE'', ''ROUTE''))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022214829_AddOperationSequenceMode'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261022214829_AddOperationSequenceMode', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022221221_SplitSensitivePermissions'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'Description', N'Module') AND [object_id] = OBJECT_ID(N'[dbo].[Permissions]'))
        SET IDENTITY_INSERT [dbo].[Permissions] ON;
    EXEC(N'INSERT INTO [dbo].[Permissions] ([Id], [Code], [Description], [Module])
    VALUES (58, N''ProductionOrder.Cancel'', N''Üretim siparişini iptal etme (üretimdeki dahil)'', N''ProductionOrder''),
    (59, N''ProductionOrder.Complete'', N''Üretim siparişini tamamlandı olarak kapatma'', N''ProductionOrder''),
    (60, N''SalesOrder.Cancel'', N''Müşteri siparişini iptal etme (onaylı dahil)'', N''SalesOrder''),
    (61, N''ProductionRoute.Activate'', N''Operasyon rotasını etkinleştirme (SAM''''i devreye alma)'', N''ProductionRoute''),
    (62, N''Stock.Adjust'', N''Elle stok düzeltme ve sayım fişini kesinleştirme'', N''Stock''),
    (63, N''Costing.Manage'', N''Stok kartı birim maliyetini değiştirme'', N''Costing''),
    (64, N''Operator.AssignFloorRole'', N''Operatörün saha görevini ve ara / arıza haklarını değiştirme'', N''Operator''),
    (65, N''Operator.AssignCard'', N''Personel kartını operatöre atama / geri alma'', N''Operator''),
    (66, N''Production.ReleaseJob'', N''Süren işi ofisten adet bildirmeden bırakma'', N''Production''),
    (67, N''WorkSession.Close'', N''Çalışma oturumunu ofisten kapatma'', N''WorkSession''),
    (68, N''Shipment.Ship'', N''Sevkiyatı kesinleştirme (stoktan düşer)'', N''Shipment''),
    (69, N''User.AssignRoles'', N''Kullanıcıya rol verme / rolünü değiştirme'', N''User''),
    (70, N''User.ResetPassword'', N''Kullanıcı parolasını sıfırlama'', N''User''),
    (71, N''Purchase.Close'', N''Satın alma siparişinin kalanını kapatma'', N''Purchase''),
    (72, N''Purchase.Cancel'', N''Satın alma siparişini iptal etme'', N''Purchase''),
    (73, N''Quality.ReleaseHold'', N''Kalite bekletmesini kaldırma'', N''Quality''),
    (74, N''Quality.Reject'', N''Kalite kontrolünde parçayı hatalı (hurda) ayırma'', N''Quality''),
    (75, N''Machine.Manage'', N''Makine tanımı oluşturma / düzenleme / silme'', N''Machine''),
    (76, N''Machine.ResolveBreakdown'', N''Arızalı makineyi ofisten kullanıma açma'', N''Machine''),
    (77, N''SalesOrder.OverridePrice'', N''Müşteri siparişinde fiyat listesinin altında fiyat girme'', N''SalesOrder''),
    (78, N''Stock.Reserve'', N''Stok ayırmayı elle değiştirme / bırakma'', N''Stock''),
    (79, N''Report.Personnel'', N''Operatör performansı ve çalışma süresi raporları'', N''Report''),
    (80, N''Report.Financial'', N''Stok değeri ve tutar içeren raporlar / sütunlar'', N''Report''),
    (81, N''Report.Export'', N''Raporu dosya olarak dışa aktarma'', N''Report''),
    (82, N''Purchase.ViewPrices'', N''Satın alma fiyatlarını ve tutarlarını görme'', N''Purchase''),
    (83, N''SalesOrder.ViewPrices'', N''Satış fiyatlarını ve tutarlarını görme'', N''SalesOrder''),
    (84, N''ApprovalFlow.Manage'', N''Onay akışlarını düzenleme'', N''ApprovalFlow''),
    (85, N''Bundle.Cancel'', N''Demeti iptal etme'', N''Bundle''),
    (86, N''LinePlan.Manage'', N''Bant planı oluşturma / değiştirme / iptal etme'', N''LinePlan''),
    (87, N''ExchangeRate.Manage'', N''Döviz kuru girme / silme'', N''ExchangeRate''),
    (88, N''Stock.EditLot'', N''Top / parti bilgisini düzeltme'', N''Stock''),
    (89, N''Warehouse.Manage'', N''Depo ve depo adresi tanımlama'', N''Warehouse''),
    (90, N''Quality.ManageDefectTypes'', N''Hata türü tanımlama'', N''Quality''),
    (91, N''Operator.ViewPersonal'', N''Operatör fotoğrafını görme'', N''Operator''),
    (92, N''Floor.ReportReject'', N''Tablette hatalı adet bildirme'', N''Floor'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'Description', N'Module') AND [object_id] = OBJECT_ID(N'[dbo].[Permissions]'))
        SET IDENTITY_INSERT [dbo].[Permissions] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022221221_SplitSensitivePermissions'
)
BEGIN
    INSERT INTO [dbo].[RolePermissions] ([RoleId], [PermissionId]) SELECT DISTINCT rp.[RoleId], 58 FROM [dbo].[RolePermissions] rp JOIN [dbo].[Permissions] p ON p.[Id] = rp.[PermissionId] WHERE p.[Code] IN ('ProductionOrder.Update') AND NOT EXISTS (SELECT 1 FROM [dbo].[RolePermissions] x WHERE x.[RoleId] = rp.[RoleId] AND x.[PermissionId] = 58);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022221221_SplitSensitivePermissions'
)
BEGIN
    INSERT INTO [dbo].[RolePermissions] ([RoleId], [PermissionId]) SELECT DISTINCT rp.[RoleId], 59 FROM [dbo].[RolePermissions] rp JOIN [dbo].[Permissions] p ON p.[Id] = rp.[PermissionId] WHERE p.[Code] IN ('ProductionOrder.Update') AND NOT EXISTS (SELECT 1 FROM [dbo].[RolePermissions] x WHERE x.[RoleId] = rp.[RoleId] AND x.[PermissionId] = 59);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022221221_SplitSensitivePermissions'
)
BEGIN
    INSERT INTO [dbo].[RolePermissions] ([RoleId], [PermissionId]) SELECT DISTINCT rp.[RoleId], 60 FROM [dbo].[RolePermissions] rp JOIN [dbo].[Permissions] p ON p.[Id] = rp.[PermissionId] WHERE p.[Code] IN ('SalesOrder.Manage') AND NOT EXISTS (SELECT 1 FROM [dbo].[RolePermissions] x WHERE x.[RoleId] = rp.[RoleId] AND x.[PermissionId] = 60);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022221221_SplitSensitivePermissions'
)
BEGIN
    INSERT INTO [dbo].[RolePermissions] ([RoleId], [PermissionId]) SELECT DISTINCT rp.[RoleId], 61 FROM [dbo].[RolePermissions] rp JOIN [dbo].[Permissions] p ON p.[Id] = rp.[PermissionId] WHERE p.[Code] IN ('ProductionRoute.Manage') AND NOT EXISTS (SELECT 1 FROM [dbo].[RolePermissions] x WHERE x.[RoleId] = rp.[RoleId] AND x.[PermissionId] = 61);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022221221_SplitSensitivePermissions'
)
BEGIN
    INSERT INTO [dbo].[RolePermissions] ([RoleId], [PermissionId]) SELECT DISTINCT rp.[RoleId], 62 FROM [dbo].[RolePermissions] rp JOIN [dbo].[Permissions] p ON p.[Id] = rp.[PermissionId] WHERE p.[Code] IN ('Stock.Manage') AND NOT EXISTS (SELECT 1 FROM [dbo].[RolePermissions] x WHERE x.[RoleId] = rp.[RoleId] AND x.[PermissionId] = 62);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022221221_SplitSensitivePermissions'
)
BEGIN
    INSERT INTO [dbo].[RolePermissions] ([RoleId], [PermissionId]) SELECT DISTINCT rp.[RoleId], 63 FROM [dbo].[RolePermissions] rp JOIN [dbo].[Permissions] p ON p.[Id] = rp.[PermissionId] WHERE p.[Code] IN ('Stock.Manage') AND NOT EXISTS (SELECT 1 FROM [dbo].[RolePermissions] x WHERE x.[RoleId] = rp.[RoleId] AND x.[PermissionId] = 63);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022221221_SplitSensitivePermissions'
)
BEGIN
    INSERT INTO [dbo].[RolePermissions] ([RoleId], [PermissionId]) SELECT DISTINCT rp.[RoleId], 64 FROM [dbo].[RolePermissions] rp JOIN [dbo].[Permissions] p ON p.[Id] = rp.[PermissionId] WHERE p.[Code] IN ('Operator.Update') AND NOT EXISTS (SELECT 1 FROM [dbo].[RolePermissions] x WHERE x.[RoleId] = rp.[RoleId] AND x.[PermissionId] = 64);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022221221_SplitSensitivePermissions'
)
BEGIN
    INSERT INTO [dbo].[RolePermissions] ([RoleId], [PermissionId]) SELECT DISTINCT rp.[RoleId], 65 FROM [dbo].[RolePermissions] rp JOIN [dbo].[Permissions] p ON p.[Id] = rp.[PermissionId] WHERE p.[Code] IN ('Operator.Update') AND NOT EXISTS (SELECT 1 FROM [dbo].[RolePermissions] x WHERE x.[RoleId] = rp.[RoleId] AND x.[PermissionId] = 65);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022221221_SplitSensitivePermissions'
)
BEGIN
    INSERT INTO [dbo].[RolePermissions] ([RoleId], [PermissionId]) SELECT DISTINCT rp.[RoleId], 66 FROM [dbo].[RolePermissions] rp JOIN [dbo].[Permissions] p ON p.[Id] = rp.[PermissionId] WHERE p.[Code] IN ('Operator.Update') AND NOT EXISTS (SELECT 1 FROM [dbo].[RolePermissions] x WHERE x.[RoleId] = rp.[RoleId] AND x.[PermissionId] = 66);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022221221_SplitSensitivePermissions'
)
BEGIN
    INSERT INTO [dbo].[RolePermissions] ([RoleId], [PermissionId]) SELECT DISTINCT rp.[RoleId], 67 FROM [dbo].[RolePermissions] rp JOIN [dbo].[Permissions] p ON p.[Id] = rp.[PermissionId] WHERE p.[Code] IN ('Operator.Update') AND NOT EXISTS (SELECT 1 FROM [dbo].[RolePermissions] x WHERE x.[RoleId] = rp.[RoleId] AND x.[PermissionId] = 67);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022221221_SplitSensitivePermissions'
)
BEGIN
    INSERT INTO [dbo].[RolePermissions] ([RoleId], [PermissionId]) SELECT DISTINCT rp.[RoleId], 68 FROM [dbo].[RolePermissions] rp JOIN [dbo].[Permissions] p ON p.[Id] = rp.[PermissionId] WHERE p.[Code] IN ('Shipment.Manage') AND NOT EXISTS (SELECT 1 FROM [dbo].[RolePermissions] x WHERE x.[RoleId] = rp.[RoleId] AND x.[PermissionId] = 68);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022221221_SplitSensitivePermissions'
)
BEGIN
    INSERT INTO [dbo].[RolePermissions] ([RoleId], [PermissionId]) SELECT DISTINCT rp.[RoleId], 69 FROM [dbo].[RolePermissions] rp JOIN [dbo].[Permissions] p ON p.[Id] = rp.[PermissionId] WHERE p.[Code] IN ('User.Manage') AND NOT EXISTS (SELECT 1 FROM [dbo].[RolePermissions] x WHERE x.[RoleId] = rp.[RoleId] AND x.[PermissionId] = 69);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022221221_SplitSensitivePermissions'
)
BEGIN
    INSERT INTO [dbo].[RolePermissions] ([RoleId], [PermissionId]) SELECT DISTINCT rp.[RoleId], 70 FROM [dbo].[RolePermissions] rp JOIN [dbo].[Permissions] p ON p.[Id] = rp.[PermissionId] WHERE p.[Code] IN ('User.Manage') AND NOT EXISTS (SELECT 1 FROM [dbo].[RolePermissions] x WHERE x.[RoleId] = rp.[RoleId] AND x.[PermissionId] = 70);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022221221_SplitSensitivePermissions'
)
BEGIN
    INSERT INTO [dbo].[RolePermissions] ([RoleId], [PermissionId]) SELECT DISTINCT rp.[RoleId], 71 FROM [dbo].[RolePermissions] rp JOIN [dbo].[Permissions] p ON p.[Id] = rp.[PermissionId] WHERE p.[Code] IN ('Purchase.Manage') AND NOT EXISTS (SELECT 1 FROM [dbo].[RolePermissions] x WHERE x.[RoleId] = rp.[RoleId] AND x.[PermissionId] = 71);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022221221_SplitSensitivePermissions'
)
BEGIN
    INSERT INTO [dbo].[RolePermissions] ([RoleId], [PermissionId]) SELECT DISTINCT rp.[RoleId], 72 FROM [dbo].[RolePermissions] rp JOIN [dbo].[Permissions] p ON p.[Id] = rp.[PermissionId] WHERE p.[Code] IN ('Purchase.Manage') AND NOT EXISTS (SELECT 1 FROM [dbo].[RolePermissions] x WHERE x.[RoleId] = rp.[RoleId] AND x.[PermissionId] = 72);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022221221_SplitSensitivePermissions'
)
BEGIN
    INSERT INTO [dbo].[RolePermissions] ([RoleId], [PermissionId]) SELECT DISTINCT rp.[RoleId], 73 FROM [dbo].[RolePermissions] rp JOIN [dbo].[Permissions] p ON p.[Id] = rp.[PermissionId] WHERE p.[Code] IN ('Quality.Create') AND NOT EXISTS (SELECT 1 FROM [dbo].[RolePermissions] x WHERE x.[RoleId] = rp.[RoleId] AND x.[PermissionId] = 73);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022221221_SplitSensitivePermissions'
)
BEGIN
    INSERT INTO [dbo].[RolePermissions] ([RoleId], [PermissionId]) SELECT DISTINCT rp.[RoleId], 74 FROM [dbo].[RolePermissions] rp JOIN [dbo].[Permissions] p ON p.[Id] = rp.[PermissionId] WHERE p.[Code] IN ('Quality.Create') AND NOT EXISTS (SELECT 1 FROM [dbo].[RolePermissions] x WHERE x.[RoleId] = rp.[RoleId] AND x.[PermissionId] = 74);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022221221_SplitSensitivePermissions'
)
BEGIN
    INSERT INTO [dbo].[RolePermissions] ([RoleId], [PermissionId]) SELECT DISTINCT rp.[RoleId], 75 FROM [dbo].[RolePermissions] rp JOIN [dbo].[Permissions] p ON p.[Id] = rp.[PermissionId] WHERE p.[Code] IN ('Machine.Update') AND NOT EXISTS (SELECT 1 FROM [dbo].[RolePermissions] x WHERE x.[RoleId] = rp.[RoleId] AND x.[PermissionId] = 75);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022221221_SplitSensitivePermissions'
)
BEGIN
    INSERT INTO [dbo].[RolePermissions] ([RoleId], [PermissionId]) SELECT DISTINCT rp.[RoleId], 76 FROM [dbo].[RolePermissions] rp JOIN [dbo].[Permissions] p ON p.[Id] = rp.[PermissionId] WHERE p.[Code] IN ('Machine.Update') AND NOT EXISTS (SELECT 1 FROM [dbo].[RolePermissions] x WHERE x.[RoleId] = rp.[RoleId] AND x.[PermissionId] = 76);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022221221_SplitSensitivePermissions'
)
BEGIN
    INSERT INTO [dbo].[RolePermissions] ([RoleId], [PermissionId]) SELECT DISTINCT rp.[RoleId], 77 FROM [dbo].[RolePermissions] rp JOIN [dbo].[Permissions] p ON p.[Id] = rp.[PermissionId] WHERE p.[Code] IN ('SalesOrder.Manage') AND NOT EXISTS (SELECT 1 FROM [dbo].[RolePermissions] x WHERE x.[RoleId] = rp.[RoleId] AND x.[PermissionId] = 77);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022221221_SplitSensitivePermissions'
)
BEGIN
    INSERT INTO [dbo].[RolePermissions] ([RoleId], [PermissionId]) SELECT DISTINCT rp.[RoleId], 78 FROM [dbo].[RolePermissions] rp JOIN [dbo].[Permissions] p ON p.[Id] = rp.[PermissionId] WHERE p.[Code] IN ('SalesOrder.Manage', 'ProductionOrder.Update') AND NOT EXISTS (SELECT 1 FROM [dbo].[RolePermissions] x WHERE x.[RoleId] = rp.[RoleId] AND x.[PermissionId] = 78);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022221221_SplitSensitivePermissions'
)
BEGIN
    INSERT INTO [dbo].[RolePermissions] ([RoleId], [PermissionId]) SELECT DISTINCT rp.[RoleId], 79 FROM [dbo].[RolePermissions] rp JOIN [dbo].[Permissions] p ON p.[Id] = rp.[PermissionId] WHERE p.[Code] IN ('Report.View') AND NOT EXISTS (SELECT 1 FROM [dbo].[RolePermissions] x WHERE x.[RoleId] = rp.[RoleId] AND x.[PermissionId] = 79);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022221221_SplitSensitivePermissions'
)
BEGIN
    INSERT INTO [dbo].[RolePermissions] ([RoleId], [PermissionId]) SELECT DISTINCT rp.[RoleId], 80 FROM [dbo].[RolePermissions] rp JOIN [dbo].[Permissions] p ON p.[Id] = rp.[PermissionId] WHERE p.[Code] IN ('Report.View', 'Report.Warehouse') AND NOT EXISTS (SELECT 1 FROM [dbo].[RolePermissions] x WHERE x.[RoleId] = rp.[RoleId] AND x.[PermissionId] = 80);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022221221_SplitSensitivePermissions'
)
BEGIN
    INSERT INTO [dbo].[RolePermissions] ([RoleId], [PermissionId]) SELECT DISTINCT rp.[RoleId], 81 FROM [dbo].[RolePermissions] rp JOIN [dbo].[Permissions] p ON p.[Id] = rp.[PermissionId] WHERE p.[Code] IN ('Report.View', 'Report.Warehouse') AND NOT EXISTS (SELECT 1 FROM [dbo].[RolePermissions] x WHERE x.[RoleId] = rp.[RoleId] AND x.[PermissionId] = 81);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022221221_SplitSensitivePermissions'
)
BEGIN
    INSERT INTO [dbo].[RolePermissions] ([RoleId], [PermissionId]) SELECT DISTINCT rp.[RoleId], 82 FROM [dbo].[RolePermissions] rp JOIN [dbo].[Permissions] p ON p.[Id] = rp.[PermissionId] WHERE p.[Code] IN ('Purchase.View') AND NOT EXISTS (SELECT 1 FROM [dbo].[RolePermissions] x WHERE x.[RoleId] = rp.[RoleId] AND x.[PermissionId] = 82);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022221221_SplitSensitivePermissions'
)
BEGIN
    INSERT INTO [dbo].[RolePermissions] ([RoleId], [PermissionId]) SELECT DISTINCT rp.[RoleId], 83 FROM [dbo].[RolePermissions] rp JOIN [dbo].[Permissions] p ON p.[Id] = rp.[PermissionId] WHERE p.[Code] IN ('SalesOrder.View') AND NOT EXISTS (SELECT 1 FROM [dbo].[RolePermissions] x WHERE x.[RoleId] = rp.[RoleId] AND x.[PermissionId] = 83);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022221221_SplitSensitivePermissions'
)
BEGIN
    INSERT INTO [dbo].[RolePermissions] ([RoleId], [PermissionId]) SELECT DISTINCT rp.[RoleId], 84 FROM [dbo].[RolePermissions] rp JOIN [dbo].[Permissions] p ON p.[Id] = rp.[PermissionId] WHERE p.[Code] IN ('Role.Manage') AND NOT EXISTS (SELECT 1 FROM [dbo].[RolePermissions] x WHERE x.[RoleId] = rp.[RoleId] AND x.[PermissionId] = 84);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022221221_SplitSensitivePermissions'
)
BEGIN
    INSERT INTO [dbo].[RolePermissions] ([RoleId], [PermissionId]) SELECT DISTINCT rp.[RoleId], 85 FROM [dbo].[RolePermissions] rp JOIN [dbo].[Permissions] p ON p.[Id] = rp.[PermissionId] WHERE p.[Code] IN ('Bundle.Create') AND NOT EXISTS (SELECT 1 FROM [dbo].[RolePermissions] x WHERE x.[RoleId] = rp.[RoleId] AND x.[PermissionId] = 85);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022221221_SplitSensitivePermissions'
)
BEGIN
    INSERT INTO [dbo].[RolePermissions] ([RoleId], [PermissionId]) SELECT DISTINCT rp.[RoleId], 86 FROM [dbo].[RolePermissions] rp JOIN [dbo].[Permissions] p ON p.[Id] = rp.[PermissionId] WHERE p.[Code] IN ('ProductionLine.Update') AND NOT EXISTS (SELECT 1 FROM [dbo].[RolePermissions] x WHERE x.[RoleId] = rp.[RoleId] AND x.[PermissionId] = 86);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022221221_SplitSensitivePermissions'
)
BEGIN
    INSERT INTO [dbo].[RolePermissions] ([RoleId], [PermissionId]) SELECT DISTINCT rp.[RoleId], 87 FROM [dbo].[RolePermissions] rp JOIN [dbo].[Permissions] p ON p.[Id] = rp.[PermissionId] WHERE p.[Code] IN ('Pricing.Manage') AND NOT EXISTS (SELECT 1 FROM [dbo].[RolePermissions] x WHERE x.[RoleId] = rp.[RoleId] AND x.[PermissionId] = 87);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022221221_SplitSensitivePermissions'
)
BEGIN
    INSERT INTO [dbo].[RolePermissions] ([RoleId], [PermissionId]) SELECT DISTINCT rp.[RoleId], 88 FROM [dbo].[RolePermissions] rp JOIN [dbo].[Permissions] p ON p.[Id] = rp.[PermissionId] WHERE p.[Code] IN ('Stock.Manage') AND NOT EXISTS (SELECT 1 FROM [dbo].[RolePermissions] x WHERE x.[RoleId] = rp.[RoleId] AND x.[PermissionId] = 88);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022221221_SplitSensitivePermissions'
)
BEGIN
    INSERT INTO [dbo].[RolePermissions] ([RoleId], [PermissionId]) SELECT DISTINCT rp.[RoleId], 89 FROM [dbo].[RolePermissions] rp JOIN [dbo].[Permissions] p ON p.[Id] = rp.[PermissionId] WHERE p.[Code] IN ('Stock.Manage') AND NOT EXISTS (SELECT 1 FROM [dbo].[RolePermissions] x WHERE x.[RoleId] = rp.[RoleId] AND x.[PermissionId] = 89);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022221221_SplitSensitivePermissions'
)
BEGIN
    INSERT INTO [dbo].[RolePermissions] ([RoleId], [PermissionId]) SELECT DISTINCT rp.[RoleId], 90 FROM [dbo].[RolePermissions] rp JOIN [dbo].[Permissions] p ON p.[Id] = rp.[PermissionId] WHERE p.[Code] IN ('MasterData.Manage') AND NOT EXISTS (SELECT 1 FROM [dbo].[RolePermissions] x WHERE x.[RoleId] = rp.[RoleId] AND x.[PermissionId] = 90);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022221221_SplitSensitivePermissions'
)
BEGIN
    INSERT INTO [dbo].[RolePermissions] ([RoleId], [PermissionId]) SELECT DISTINCT rp.[RoleId], 91 FROM [dbo].[RolePermissions] rp JOIN [dbo].[Permissions] p ON p.[Id] = rp.[PermissionId] WHERE p.[Code] IN ('Operator.View') AND NOT EXISTS (SELECT 1 FROM [dbo].[RolePermissions] x WHERE x.[RoleId] = rp.[RoleId] AND x.[PermissionId] = 91);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022221221_SplitSensitivePermissions'
)
BEGIN
    INSERT INTO [dbo].[RolePermissions] ([RoleId], [PermissionId]) SELECT r.[Id], 92 FROM [dbo].[Roles] r WHERE r.[Code] IN ('OPERATOR', 'LINE_MANAGER', 'MAINTENANCE') AND NOT EXISTS (SELECT 1 FROM [dbo].[RolePermissions] x WHERE x.[RoleId] = r.[Id] AND x.[PermissionId] = 92);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022221221_SplitSensitivePermissions'
)
BEGIN
    INSERT INTO [dbo].[RolePermissions] ([RoleId], [PermissionId]) SELECT r.[Id], p.[Id] FROM [dbo].[Roles] r CROSS JOIN [dbo].[Permissions] p WHERE r.[Code] = 'ADMIN' AND p.[Id] BETWEEN 58 AND 92 AND NOT EXISTS (SELECT 1 FROM [dbo].[RolePermissions] x WHERE x.[RoleId] = r.[Id] AND x.[PermissionId] = p.[Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022221221_SplitSensitivePermissions'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261022221221_SplitSensitivePermissions', N'10.0.12');
END;

COMMIT;
GO

