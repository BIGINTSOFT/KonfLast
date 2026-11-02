/*
    Tekstil ERP - Dokuma modülü kurulum / güncelleme script'i
    Bu dosya otomatik üretilir (database\generate-script.ps1); elle düzenlemeyin.

    Hedef veritabanına (örn. TekstilERP) bağlanıp script'in tamamını çalıştırın. Script tekrar
    çalıştırılabilir: daha önce uygulanmış adımlar atlanır. Sıra: önce 00_Core.sql, sonra modüller.

    sqlcmd ile:  sqlcmd -S "(localdb)\MSSQLLocalDB" -d TekstilERP -E -b -f 65001 -i 30_Dokuma.sql
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
IF OBJECT_ID(N'[Dokuma].[__EFMigrationsHistory]') IS NULL
BEGIN
    IF SCHEMA_ID(N'Dokuma') IS NULL EXEC(N'CREATE SCHEMA [Dokuma];');
    CREATE TABLE [Dokuma].[__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030070355_InitialDokuma'
)
BEGIN
    IF SCHEMA_ID(N'Dokuma') IS NULL EXEC(N'CREATE SCHEMA [Dokuma];');
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030070355_InitialDokuma'
)
BEGIN
    SET IDENTITY_INSERT [Core].[Permissions] ON;
    EXEC(N'INSERT INTO [Core].[Permissions] ([Id], [Code], [Module], [Description])
    SELECT v.[Id], v.[Code], v.[Module], v.[Description] FROM (VALUES
    (2001, N''Dokuma.View'', N''Dokuma'', N''Dokuma ekranlarını görüntüleme'')
    ) v([Id], [Code], [Module], [Description])
    WHERE NOT EXISTS (SELECT 1 FROM [Core].[Permissions] p WHERE p.[Id] = v.[Id] OR p.[Code] = v.[Code])');
    SET IDENTITY_INSERT [Core].[Permissions] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030070355_InitialDokuma'
)
BEGIN
    INSERT INTO [Core].[RolePermissions] ([RoleId], [PermissionId])
    SELECT v.[RoleId], v.[PermissionId] FROM (VALUES
    (1, 2001)
    ) v([RoleId], [PermissionId])
    WHERE EXISTS (SELECT 1 FROM [Core].[Roles] r WHERE r.[Id] = v.[RoleId])
      AND EXISTS (SELECT 1 FROM [Core].[Permissions] p WHERE p.[Id] = v.[PermissionId])
      AND NOT EXISTS (SELECT 1 FROM [Core].[RolePermissions] rp WHERE rp.[RoleId] = v.[RoleId] AND rp.[PermissionId] = v.[PermissionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030070355_InitialDokuma'
)
BEGIN
    INSERT INTO [Dokuma].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261030070355_InitialDokuma', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031203258_AddMasterDataCards'
)
BEGIN
    IF SCHEMA_ID(N'Dokuma') IS NULL EXEC(N'CREATE SCHEMA [Dokuma];');
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031203258_AddMasterDataCards'
)
BEGIN
    CREATE TABLE [Dokuma].[Articles] (
        [Id] int NOT NULL IDENTITY,
        [ArticleCode] nvarchar(30) NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [WeaveName] nvarchar(100) NULL,
        [SheddingType] nvarchar(30) NOT NULL,
        [Composition] nvarchar(200) NULL,
        [ReedNumber] decimal(8,2) NULL,
        [EndsPerDent] int NULL,
        [ReedWidthCm] decimal(6,2) NOT NULL,
        [WeftDensityPerCm] decimal(6,2) NOT NULL,
        [RawWidthCm] decimal(6,2) NOT NULL,
        [RawWeightGsm] int NOT NULL,
        [WarpCrimpPercent] decimal(5,2) NULL,
        [WeftCrimpPercent] decimal(5,2) NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_Articles] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Articles_EndsPerDent] CHECK ([EndsPerDent] IS NULL OR [EndsPerDent] >= 1),
        CONSTRAINT [CK_Articles_RawWeightGsm] CHECK ([RawWeightGsm] > 0),
        CONSTRAINT [CK_Articles_RawWidthCm] CHECK ([RawWidthCm] > 0),
        CONSTRAINT [CK_Articles_ReedNumber] CHECK ([ReedNumber] IS NULL OR [ReedNumber] > 0),
        CONSTRAINT [CK_Articles_ReedWidthCm] CHECK ([ReedWidthCm] > 0),
        CONSTRAINT [CK_Articles_SheddingType] CHECK ([SheddingType] IN ('CAM', 'DOBBY', 'JACQUARD')),
        CONSTRAINT [CK_Articles_WarpCrimpPercent] CHECK ([WarpCrimpPercent] IS NULL OR [WarpCrimpPercent] BETWEEN 0 AND 100),
        CONSTRAINT [CK_Articles_WeftCrimpPercent] CHECK ([WeftCrimpPercent] IS NULL OR [WeftCrimpPercent] BETWEEN 0 AND 100),
        CONSTRAINT [CK_Articles_WeftDensityPerCm] CHECK ([WeftDensityPerCm] > 0)
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031203258_AddMasterDataCards'
)
BEGIN
    CREATE TABLE [Dokuma].[Customers] (
        [Id] int NOT NULL IDENTITY,
        [CustomerCode] nvarchar(30) NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [PartyType] nvarchar(30) NOT NULL,
        [FirstName] nvarchar(100) NULL,
        [LastName] nvarchar(100) NULL,
        [TaxNumber] nvarchar(20) NULL,
        [TaxOffice] nvarchar(100) NULL,
        [MersisNo] nvarchar(16) NULL,
        [TradeRegistryNo] nvarchar(30) NULL,
        [Phone] nvarchar(30) NULL,
        [Email] nvarchar(256) NULL,
        [IsEInvoiceUser] bit NOT NULL,
        [EInvoiceAlias] nvarchar(256) NULL,
        [EInvoiceProfile] nvarchar(30) NOT NULL,
        [IsEDespatchUser] bit NOT NULL,
        [EDespatchAlias] nvarchar(256) NULL,
        [EArchiveDelivery] nvarchar(30) NOT NULL,
        [EArchiveEmail] nvarchar(256) NULL,
        [Currency] char(3) NOT NULL DEFAULT 'TRY',
        [FireTolerancePercent] decimal(5,2) NULL,
        [VatRate] decimal(5,2) NULL,
        [PaymentTermDays] int NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_Customers] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Customers_EArchiveDelivery] CHECK ([EArchiveDelivery] IN ('ELECTRONIC', 'PAPER')),
        CONSTRAINT [CK_Customers_EInvoiceProfile] CHECK ([EInvoiceProfile] IN ('BASIC', 'COMMERCIAL')),
        CONSTRAINT [CK_Customers_FireTolerancePercent] CHECK ([FireTolerancePercent] IS NULL OR [FireTolerancePercent] BETWEEN 0 AND 100),
        CONSTRAINT [CK_Customers_PartyType] CHECK ([PartyType] IN ('COMPANY', 'INDIVIDUAL')),
        CONSTRAINT [CK_Customers_PaymentTermDays] CHECK ([PaymentTermDays] IS NULL OR [PaymentTermDays] >= 0)
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031203258_AddMasterDataCards'
)
BEGIN
    CREATE TABLE [Dokuma].[Looms] (
        [Id] int NOT NULL IDENTITY,
        [Code] nvarchar(30) NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [LoomType] nvarchar(30) NOT NULL,
        [SheddingType] nvarchar(30) NOT NULL,
        [MaxReedWidthCm] decimal(6,2) NULL,
        [NominalSpeedRpm] int NULL,
        [Brand] nvarchar(100) NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_Looms] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Looms_LoomType] CHECK ([LoomType] IN ('AIR_JET', 'RAPIER', 'WATER_JET', 'PROJECTILE', 'SHUTTLE')),
        CONSTRAINT [CK_Looms_MaxReedWidthCm] CHECK ([MaxReedWidthCm] IS NULL OR [MaxReedWidthCm] > 0),
        CONSTRAINT [CK_Looms_NominalSpeedRpm] CHECK ([NominalSpeedRpm] IS NULL OR [NominalSpeedRpm] > 0),
        CONSTRAINT [CK_Looms_SheddingType] CHECK ([SheddingType] IN ('CAM', 'DOBBY', 'JACQUARD'))
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031203258_AddMasterDataCards'
)
BEGIN
    CREATE TABLE [Dokuma].[Suppliers] (
        [Id] int NOT NULL IDENTITY,
        [Code] nvarchar(30) NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [TaxNumber] nvarchar(20) NULL,
        [Phone] nvarchar(30) NULL,
        [Email] nvarchar(150) NULL,
        [Currency] char(3) NOT NULL DEFAULT 'TRY',
        [LeadTimeDays] int NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_Suppliers] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Suppliers_LeadTimeDays] CHECK ([LeadTimeDays] IS NULL OR [LeadTimeDays] >= 0)
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031203258_AddMasterDataCards'
)
BEGIN
    CREATE TABLE [Dokuma].[Yarns] (
        [Id] int NOT NULL IDENTITY,
        [YarnCode] nvarchar(30) NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [CountValue] decimal(8,2) NOT NULL,
        [CountSystem] nvarchar(30) NOT NULL,
        [Plies] int NOT NULL,
        [Composition] nvarchar(200) NULL,
        [Twist] nvarchar(50) NULL,
        [Color] nvarchar(100) NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_Yarns] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Yarns_CountSystem] CHECK ([CountSystem] IN ('NE', 'NM', 'DTEX', 'DENIER')),
        CONSTRAINT [CK_Yarns_CountValue] CHECK ([CountValue] > 0),
        CONSTRAINT [CK_Yarns_Plies] CHECK ([Plies] >= 1)
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031203258_AddMasterDataCards'
)
BEGIN
    CREATE TABLE [Dokuma].[CustomerAddresses] (
        [Id] int NOT NULL IDENTITY,
        [CustomerId] int NOT NULL,
        [Title] nvarchar(100) NOT NULL,
        [AddressLine] nvarchar(500) NOT NULL,
        [District] nvarchar(100) NOT NULL,
        [City] nvarchar(100) NOT NULL,
        [PostalCode] nvarchar(10) NULL,
        [CountryCode] char(2) NOT NULL DEFAULT 'TR',
        [IsDefaultInvoice] bit NOT NULL,
        [IsDefaultDelivery] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_CustomerAddresses] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_CustomerAddresses_CountryCode] CHECK (LEN([CountryCode]) = 2),
        CONSTRAINT [FK_CustomerAddresses_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Dokuma].[Customers] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031203258_AddMasterDataCards'
)
BEGIN
    CREATE TABLE [Dokuma].[CustomerContacts] (
        [Id] int NOT NULL IDENTITY,
        [CustomerId] int NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [Title] nvarchar(100) NULL,
        [Phone] nvarchar(30) NULL,
        [Email] nvarchar(256) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_CustomerContacts] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CustomerContacts_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Dokuma].[Customers] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031203258_AddMasterDataCards'
)
BEGIN
    CREATE TABLE [Dokuma].[ArticleYarns] (
        [Id] int NOT NULL IDENTITY,
        [ArticleId] int NOT NULL,
        [Role] nvarchar(30) NOT NULL,
        [Sequence] int NOT NULL,
        [YarnId] int NOT NULL,
        [Ends] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_ArticleYarns] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_ArticleYarns_Ends] CHECK ([Ends] >= 1),
        CONSTRAINT [CK_ArticleYarns_Role] CHECK ([Role] IN ('WARP', 'WEFT')),
        CONSTRAINT [CK_ArticleYarns_Sequence] CHECK ([Sequence] >= 1),
        CONSTRAINT [FK_ArticleYarns_Articles_ArticleId] FOREIGN KEY ([ArticleId]) REFERENCES [Dokuma].[Articles] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ArticleYarns_Yarns_YarnId] FOREIGN KEY ([YarnId]) REFERENCES [Dokuma].[Yarns] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031203258_AddMasterDataCards'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Articles_ArticleCode] ON [Dokuma].[Articles] ([ArticleCode]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031203258_AddMasterDataCards'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ArticleYarns_ArticleId_Role_Sequence] ON [Dokuma].[ArticleYarns] ([ArticleId], [Role], [Sequence]);
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031203258_AddMasterDataCards'
)
BEGIN
    CREATE INDEX [IX_ArticleYarns_YarnId] ON [Dokuma].[ArticleYarns] ([YarnId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031203258_AddMasterDataCards'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_CustomerAddresses_DefaultDelivery] ON [Dokuma].[CustomerAddresses] ([CustomerId]) WHERE [IsDefaultDelivery] = 1');
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031203258_AddMasterDataCards'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_CustomerAddresses_DefaultInvoice] ON [Dokuma].[CustomerAddresses] ([CustomerId]) WHERE [IsDefaultInvoice] = 1');
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031203258_AddMasterDataCards'
)
BEGIN
    CREATE INDEX [IX_CustomerContacts_CustomerId] ON [Dokuma].[CustomerContacts] ([CustomerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031203258_AddMasterDataCards'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Customers_CustomerCode] ON [Dokuma].[Customers] ([CustomerCode]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031203258_AddMasterDataCards'
)
BEGIN
    CREATE INDEX [IX_Customers_TaxNumber] ON [Dokuma].[Customers] ([TaxNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031203258_AddMasterDataCards'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Looms_Code] ON [Dokuma].[Looms] ([Code]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031203258_AddMasterDataCards'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Suppliers_Code] ON [Dokuma].[Suppliers] ([Code]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031203258_AddMasterDataCards'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Yarns_YarnCode] ON [Dokuma].[Yarns] ([YarnCode]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031203258_AddMasterDataCards'
)
BEGIN
    SET IDENTITY_INSERT [Core].[Permissions] ON;
    EXEC(N'INSERT INTO [Core].[Permissions] ([Id], [Code], [Module], [Description])
    SELECT v.[Id], v.[Code], v.[Module], v.[Description] FROM (VALUES
    (2002, N''Dokuma.Customer.View'', N''Dokuma'', N''Dokuma müşterilerini görüntüleme''),
    (2003, N''Dokuma.Customer.Manage'', N''Dokuma'', N''Dokuma müşterilerini oluşturma ve düzenleme''),
    (2004, N''Dokuma.Material.View'', N''Dokuma'', N''İplik ve artikel kartlarını görüntüleme''),
    (2005, N''Dokuma.Material.Manage'', N''Dokuma'', N''İplik ve artikel kartlarını oluşturma ve düzenleme''),
    (2006, N''Dokuma.Loom.View'', N''Dokuma'', N''Tezgâh kartlarını görüntüleme''),
    (2007, N''Dokuma.Loom.Manage'', N''Dokuma'', N''Tezgâh kartlarını oluşturma ve düzenleme''),
    (2008, N''Dokuma.Purchase.View'', N''Dokuma'', N''Tedarikçileri görüntüleme''),
    (2009, N''Dokuma.Purchase.Manage'', N''Dokuma'', N''Tedarikçi kartlarını oluşturma ve düzenleme'')
    ) v([Id], [Code], [Module], [Description])
    WHERE NOT EXISTS (SELECT 1 FROM [Core].[Permissions] p WHERE p.[Id] = v.[Id] OR p.[Code] = v.[Code])');
    SET IDENTITY_INSERT [Core].[Permissions] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031203258_AddMasterDataCards'
)
BEGIN
    INSERT INTO [Core].[RolePermissions] ([RoleId], [PermissionId])
    SELECT v.[RoleId], v.[PermissionId] FROM (VALUES
    (1, 2002), (1, 2003), (1, 2004), (1, 2005), (1, 2006), (1, 2007), (1, 2008), (1, 2009)
    ) v([RoleId], [PermissionId])
    WHERE EXISTS (SELECT 1 FROM [Core].[Roles] r WHERE r.[Id] = v.[RoleId])
      AND EXISTS (SELECT 1 FROM [Core].[Permissions] p WHERE p.[Id] = v.[PermissionId])
      AND NOT EXISTS (SELECT 1 FROM [Core].[RolePermissions] rp WHERE rp.[RoleId] = v.[RoleId] AND rp.[PermissionId] = v.[PermissionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031203258_AddMasterDataCards'
)
BEGIN
    INSERT INTO [Dokuma].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261031203258_AddMasterDataCards', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031204554_AddOrdersAndYarnLots'
)
BEGIN
    CREATE TABLE [Dokuma].[Orders] (
        [Id] int NOT NULL IDENTITY,
        [OrderNo] nvarchar(30) NOT NULL,
        [OrderDate] date NOT NULL,
        [CustomerId] int NULL,
        [CustomerOrderNo] nvarchar(50) NULL,
        [Ownership] nvarchar(30) NOT NULL,
        [DueDate] date NULL,
        [Currency] char(3) NOT NULL DEFAULT 'TRY',
        [Notes] nvarchar(1000) NULL,
        [Status] nvarchar(30) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_Orders] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Orders_CommissionCustomer] CHECK ([Ownership] <> 'COMMISSION' OR [CustomerId] IS NOT NULL),
        CONSTRAINT [CK_Orders_Ownership] CHECK ([Ownership] IN ('COMMISSION', 'OWN_GOODS')),
        CONSTRAINT [CK_Orders_Status] CHECK ([Status] IN ('DRAFT', 'CONFIRMED', 'CANCELLED')),
        CONSTRAINT [FK_Orders_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Dokuma].[Customers] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031204554_AddOrdersAndYarnLots'
)
BEGIN
    CREATE TABLE [Dokuma].[YarnLots] (
        [Id] int NOT NULL IDENTITY,
        [LotNo] nvarchar(30) NOT NULL,
        [ReceiptDate] date NOT NULL,
        [CustomerId] int NULL,
        [SupplierId] int NULL,
        [YarnId] int NOT NULL,
        [DespatchNo] nvarchar(50) NULL,
        [ProducerLotNo] nvarchar(50) NULL,
        [PackageCount] int NOT NULL,
        [Kilograms] decimal(12,2) NOT NULL,
        [Notes] nvarchar(1000) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_YarnLots] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_YarnLots_Kilograms] CHECK ([Kilograms] > 0),
        CONSTRAINT [CK_YarnLots_PackageCount] CHECK ([PackageCount] >= 1),
        CONSTRAINT [FK_YarnLots_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Dokuma].[Customers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_YarnLots_Suppliers_SupplierId] FOREIGN KEY ([SupplierId]) REFERENCES [Dokuma].[Suppliers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_YarnLots_Yarns_YarnId] FOREIGN KEY ([YarnId]) REFERENCES [Dokuma].[Yarns] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031204554_AddOrdersAndYarnLots'
)
BEGIN
    CREATE TABLE [Dokuma].[OrderLines] (
        [Id] int NOT NULL IDENTITY,
        [OrderId] int NOT NULL,
        [ArticleId] int NOT NULL,
        [Meters] decimal(12,2) NOT NULL,
        [UnitPrice] decimal(18,4) NULL,
        [Notes] nvarchar(500) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_OrderLines] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_OrderLines_Meters] CHECK ([Meters] > 0),
        CONSTRAINT [CK_OrderLines_UnitPrice] CHECK ([UnitPrice] IS NULL OR [UnitPrice] >= 0),
        CONSTRAINT [FK_OrderLines_Articles_ArticleId] FOREIGN KEY ([ArticleId]) REFERENCES [Dokuma].[Articles] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_OrderLines_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Dokuma].[Orders] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031204554_AddOrdersAndYarnLots'
)
BEGIN
    CREATE INDEX [IX_OrderLines_ArticleId] ON [Dokuma].[OrderLines] ([ArticleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031204554_AddOrdersAndYarnLots'
)
BEGIN
    CREATE INDEX [IX_OrderLines_OrderId] ON [Dokuma].[OrderLines] ([OrderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031204554_AddOrdersAndYarnLots'
)
BEGIN
    CREATE INDEX [IX_Orders_CustomerId] ON [Dokuma].[Orders] ([CustomerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031204554_AddOrdersAndYarnLots'
)
BEGIN
    CREATE INDEX [IX_Orders_OrderDate] ON [Dokuma].[Orders] ([OrderDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031204554_AddOrdersAndYarnLots'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Orders_OrderNo] ON [Dokuma].[Orders] ([OrderNo]);
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031204554_AddOrdersAndYarnLots'
)
BEGIN
    CREATE INDEX [IX_YarnLots_CustomerId] ON [Dokuma].[YarnLots] ([CustomerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031204554_AddOrdersAndYarnLots'
)
BEGIN
    CREATE UNIQUE INDEX [IX_YarnLots_LotNo] ON [Dokuma].[YarnLots] ([LotNo]);
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031204554_AddOrdersAndYarnLots'
)
BEGIN
    CREATE INDEX [IX_YarnLots_SupplierId] ON [Dokuma].[YarnLots] ([SupplierId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031204554_AddOrdersAndYarnLots'
)
BEGIN
    CREATE INDEX [IX_YarnLots_YarnId_CustomerId] ON [Dokuma].[YarnLots] ([YarnId], [CustomerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031204554_AddOrdersAndYarnLots'
)
BEGIN
    SET IDENTITY_INSERT [Core].[Permissions] ON;
    EXEC(N'INSERT INTO [Core].[Permissions] ([Id], [Code], [Module], [Description])
    SELECT v.[Id], v.[Code], v.[Module], v.[Description] FROM (VALUES
    (2010, N''Dokuma.Order.View'', N''Dokuma'', N''Dokuma siparişlerini ve iplik ihtiyacını görüntüleme''),
    (2011, N''Dokuma.Order.Manage'', N''Dokuma'', N''Dokuma siparişlerini oluşturma, onaylama ve iptal etme''),
    (2012, N''Dokuma.Receipt.View'', N''Dokuma'', N''İplik girişlerini görüntüleme''),
    (2013, N''Dokuma.Receipt.Manage'', N''Dokuma'', N''İplik girişi yapma, düzenleme ve silme'')
    ) v([Id], [Code], [Module], [Description])
    WHERE NOT EXISTS (SELECT 1 FROM [Core].[Permissions] p WHERE p.[Id] = v.[Id] OR p.[Code] = v.[Code])');
    SET IDENTITY_INSERT [Core].[Permissions] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031204554_AddOrdersAndYarnLots'
)
BEGIN
    INSERT INTO [Core].[RolePermissions] ([RoleId], [PermissionId])
    SELECT v.[RoleId], v.[PermissionId] FROM (VALUES
    (1, 2010), (1, 2011), (1, 2012), (1, 2013)
    ) v([RoleId], [PermissionId])
    WHERE EXISTS (SELECT 1 FROM [Core].[Roles] r WHERE r.[Id] = v.[RoleId])
      AND EXISTS (SELECT 1 FROM [Core].[Permissions] p WHERE p.[Id] = v.[PermissionId])
      AND NOT EXISTS (SELECT 1 FROM [Core].[RolePermissions] rp WHERE rp.[RoleId] = v.[RoleId] AND rp.[PermissionId] = v.[PermissionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031204554_AddOrdersAndYarnLots'
)
BEGIN
    INSERT INTO [Dokuma].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261031204554_AddOrdersAndYarnLots', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031205355_AddArticleWaste'
)
BEGIN
    ALTER TABLE [Dokuma].[Articles] ADD [WarpWastePercent] decimal(5,2) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031205355_AddArticleWaste'
)
BEGIN
    ALTER TABLE [Dokuma].[Articles] ADD [WeftWastePercent] decimal(5,2) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031205355_AddArticleWaste'
)
BEGIN
    EXEC(N'ALTER TABLE [Dokuma].[Articles] ADD CONSTRAINT [CK_Articles_WarpWastePercent] CHECK ([WarpWastePercent] IS NULL OR [WarpWastePercent] BETWEEN 0 AND 100)');
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031205355_AddArticleWaste'
)
BEGIN
    EXEC(N'ALTER TABLE [Dokuma].[Articles] ADD CONSTRAINT [CK_Articles_WeftWastePercent] CHECK ([WeftWastePercent] IS NULL OR [WeftWastePercent] BETWEEN 0 AND 100)');
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031205355_AddArticleWaste'
)
BEGIN
    INSERT INTO [Dokuma].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261031205355_AddArticleWaste', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031210343_AddWarpingAndBeams'
)
BEGIN
    CREATE TABLE [Dokuma].[WarpOrders] (
        [Id] int NOT NULL IDENTITY,
        [WarpNo] nvarchar(30) NOT NULL,
        [OrderLineId] int NOT NULL,
        [PlannedDate] date NOT NULL,
        [PlannedBeamCount] int NOT NULL,
        [MetersPerBeam] decimal(12,2) NOT NULL,
        [Ends] int NOT NULL,
        [Status] nvarchar(30) NOT NULL,
        [CompletedOn] date NULL,
        [Notes] nvarchar(1000) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_WarpOrders] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_WarpOrders_Ends] CHECK ([Ends] >= 1),
        CONSTRAINT [CK_WarpOrders_MetersPerBeam] CHECK ([MetersPerBeam] > 0),
        CONSTRAINT [CK_WarpOrders_PlannedBeamCount] CHECK ([PlannedBeamCount] >= 1),
        CONSTRAINT [CK_WarpOrders_Status] CHECK ([Status] IN ('OPEN', 'COMPLETED', 'CANCELLED')),
        CONSTRAINT [FK_WarpOrders_OrderLines_OrderLineId] FOREIGN KEY ([OrderLineId]) REFERENCES [Dokuma].[OrderLines] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031210343_AddWarpingAndBeams'
)
BEGIN
    CREATE TABLE [Dokuma].[Beams] (
        [Id] int NOT NULL IDENTITY,
        [BeamNo] nvarchar(30) NOT NULL,
        [WarpOrderId] int NOT NULL,
        [Meters] decimal(12,2) NOT NULL,
        [Ends] int NOT NULL,
        [SizingMode] nvarchar(30) NOT NULL,
        [IsSized] bit NOT NULL,
        [SizingSupplierId] int NULL,
        [SentToSizingOn] date NULL,
        [SizedOn] date NULL,
        [DrawingMethod] nvarchar(30) NULL,
        [ReadyOn] date NULL,
        [Status] nvarchar(30) NOT NULL,
        [Notes] nvarchar(500) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_Beams] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Beams_DrawingMethod] CHECK ([DrawingMethod] IS NULL OR [DrawingMethod] IN ('DRAWING', 'TYING')),
        CONSTRAINT [CK_Beams_Ends] CHECK ([Ends] >= 1),
        CONSTRAINT [CK_Beams_Meters] CHECK ([Meters] > 0),
        CONSTRAINT [CK_Beams_SizingMode] CHECK ([SizingMode] IN ('NONE', 'IN_HOUSE', 'COMMISSION')),
        CONSTRAINT [CK_Beams_Status] CHECK ([Status] IN ('WARPED', 'AT_SIZING', 'READY', 'ON_LOOM', 'FINISHED')),
        CONSTRAINT [FK_Beams_Suppliers_SizingSupplierId] FOREIGN KEY ([SizingSupplierId]) REFERENCES [Dokuma].[Suppliers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Beams_WarpOrders_WarpOrderId] FOREIGN KEY ([WarpOrderId]) REFERENCES [Dokuma].[WarpOrders] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031210343_AddWarpingAndBeams'
)
BEGIN
    CREATE TABLE [Dokuma].[WarpIssues] (
        [Id] int NOT NULL IDENTITY,
        [WarpOrderId] int NOT NULL,
        [YarnLotId] int NOT NULL,
        [IssuedOn] date NOT NULL,
        [Kilograms] decimal(12,2) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_WarpIssues] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_WarpIssues_Kilograms] CHECK ([Kilograms] > 0),
        CONSTRAINT [FK_WarpIssues_WarpOrders_WarpOrderId] FOREIGN KEY ([WarpOrderId]) REFERENCES [Dokuma].[WarpOrders] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_WarpIssues_YarnLots_YarnLotId] FOREIGN KEY ([YarnLotId]) REFERENCES [Dokuma].[YarnLots] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031210343_AddWarpingAndBeams'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Beams_BeamNo] ON [Dokuma].[Beams] ([BeamNo]);
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031210343_AddWarpingAndBeams'
)
BEGIN
    CREATE INDEX [IX_Beams_SizingSupplierId] ON [Dokuma].[Beams] ([SizingSupplierId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031210343_AddWarpingAndBeams'
)
BEGIN
    CREATE INDEX [IX_Beams_Status] ON [Dokuma].[Beams] ([Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031210343_AddWarpingAndBeams'
)
BEGIN
    CREATE INDEX [IX_Beams_WarpOrderId] ON [Dokuma].[Beams] ([WarpOrderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031210343_AddWarpingAndBeams'
)
BEGIN
    CREATE INDEX [IX_WarpIssues_WarpOrderId] ON [Dokuma].[WarpIssues] ([WarpOrderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031210343_AddWarpingAndBeams'
)
BEGIN
    CREATE INDEX [IX_WarpIssues_YarnLotId] ON [Dokuma].[WarpIssues] ([YarnLotId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031210343_AddWarpingAndBeams'
)
BEGIN
    CREATE INDEX [IX_WarpOrders_OrderLineId] ON [Dokuma].[WarpOrders] ([OrderLineId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031210343_AddWarpingAndBeams'
)
BEGIN
    CREATE UNIQUE INDEX [IX_WarpOrders_WarpNo] ON [Dokuma].[WarpOrders] ([WarpNo]);
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031210343_AddWarpingAndBeams'
)
BEGIN
    SET IDENTITY_INSERT [Core].[Permissions] ON;
    EXEC(N'INSERT INTO [Core].[Permissions] ([Id], [Code], [Module], [Description])
    SELECT v.[Id], v.[Code], v.[Module], v.[Description] FROM (VALUES
    (2014, N''Dokuma.Warp.View'', N''Dokuma'', N''Çözgü emirlerini ve leventleri görüntüleme''),
    (2015, N''Dokuma.Warp.Manage'', N''Dokuma'', N''Çözgü emri, çözgüye iplik verme, levent, haşıl ve tahar / düğüm'')
    ) v([Id], [Code], [Module], [Description])
    WHERE NOT EXISTS (SELECT 1 FROM [Core].[Permissions] p WHERE p.[Id] = v.[Id] OR p.[Code] = v.[Code])');
    SET IDENTITY_INSERT [Core].[Permissions] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031210343_AddWarpingAndBeams'
)
BEGIN
    INSERT INTO [Core].[RolePermissions] ([RoleId], [PermissionId])
    SELECT v.[RoleId], v.[PermissionId] FROM (VALUES
    (1, 2014), (1, 2015)
    ) v([RoleId], [PermissionId])
    WHERE EXISTS (SELECT 1 FROM [Core].[Roles] r WHERE r.[Id] = v.[RoleId])
      AND EXISTS (SELECT 1 FROM [Core].[Permissions] p WHERE p.[Id] = v.[PermissionId])
      AND NOT EXISTS (SELECT 1 FROM [Core].[RolePermissions] rp WHERE rp.[RoleId] = v.[RoleId] AND rp.[PermissionId] = v.[PermissionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031210343_AddWarpingAndBeams'
)
BEGIN
    INSERT INTO [Dokuma].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261031210343_AddWarpingAndBeams', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031211808_AddWeavingAndRolls'
)
BEGIN
    ALTER TABLE [Dokuma].[Beams] ADD [FinishedOn] date NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031211808_AddWeavingAndRolls'
)
BEGIN
    ALTER TABLE [Dokuma].[Beams] ADD [LeftoverMeters] decimal(12,2) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031211808_AddWeavingAndRolls'
)
BEGIN
    ALTER TABLE [Dokuma].[Beams] ADD [LoomId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031211808_AddWeavingAndRolls'
)
BEGIN
    ALTER TABLE [Dokuma].[Beams] ADD [MountedOn] date NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031211808_AddWeavingAndRolls'
)
BEGIN
    CREATE TABLE [Dokuma].[FabricRolls] (
        [Id] int NOT NULL IDENTITY,
        [RollNo] nvarchar(30) NOT NULL,
        [BeamId] int NOT NULL,
        [LoomId] int NOT NULL,
        [CutOn] date NOT NULL,
        [Meters] decimal(12,2) NOT NULL,
        [Kilograms] decimal(12,2) NULL,
        [Status] nvarchar(30) NOT NULL,
        [Notes] nvarchar(500) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_FabricRolls] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_FabricRolls_Kilograms] CHECK ([Kilograms] IS NULL OR [Kilograms] > 0),
        CONSTRAINT [CK_FabricRolls_Meters] CHECK ([Meters] > 0),
        CONSTRAINT [CK_FabricRolls_Status] CHECK ([Status] IN ('PENDING_INSPECTION')),
        CONSTRAINT [FK_FabricRolls_Beams_BeamId] FOREIGN KEY ([BeamId]) REFERENCES [Dokuma].[Beams] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_FabricRolls_Looms_LoomId] FOREIGN KEY ([LoomId]) REFERENCES [Dokuma].[Looms] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031211808_AddWeavingAndRolls'
)
BEGIN
    CREATE TABLE [Dokuma].[WeftIssues] (
        [Id] int NOT NULL IDENTITY,
        [BeamId] int NOT NULL,
        [YarnLotId] int NOT NULL,
        [IssuedOn] date NOT NULL,
        [Kilograms] decimal(12,2) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_WeftIssues] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_WeftIssues_Kilograms] CHECK ([Kilograms] > 0),
        CONSTRAINT [FK_WeftIssues_Beams_BeamId] FOREIGN KEY ([BeamId]) REFERENCES [Dokuma].[Beams] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_WeftIssues_YarnLots_YarnLotId] FOREIGN KEY ([YarnLotId]) REFERENCES [Dokuma].[YarnLots] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031211808_AddWeavingAndRolls'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_Beams_LoomOnLoom] ON [Dokuma].[Beams] ([LoomId]) WHERE [Status] = ''ON_LOOM''');
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031211808_AddWeavingAndRolls'
)
BEGIN
    EXEC(N'ALTER TABLE [Dokuma].[Beams] ADD CONSTRAINT [CK_Beams_LeftoverMeters] CHECK ([LeftoverMeters] IS NULL OR [LeftoverMeters] >= 0)');
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031211808_AddWeavingAndRolls'
)
BEGIN
    CREATE INDEX [IX_FabricRolls_BeamId] ON [Dokuma].[FabricRolls] ([BeamId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031211808_AddWeavingAndRolls'
)
BEGIN
    CREATE INDEX [IX_FabricRolls_LoomId] ON [Dokuma].[FabricRolls] ([LoomId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031211808_AddWeavingAndRolls'
)
BEGIN
    CREATE UNIQUE INDEX [IX_FabricRolls_RollNo] ON [Dokuma].[FabricRolls] ([RollNo]);
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031211808_AddWeavingAndRolls'
)
BEGIN
    CREATE INDEX [IX_FabricRolls_Status] ON [Dokuma].[FabricRolls] ([Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031211808_AddWeavingAndRolls'
)
BEGIN
    CREATE INDEX [IX_WeftIssues_BeamId] ON [Dokuma].[WeftIssues] ([BeamId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031211808_AddWeavingAndRolls'
)
BEGIN
    CREATE INDEX [IX_WeftIssues_YarnLotId] ON [Dokuma].[WeftIssues] ([YarnLotId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031211808_AddWeavingAndRolls'
)
BEGIN
    ALTER TABLE [Dokuma].[Beams] ADD CONSTRAINT [FK_Beams_Looms_LoomId] FOREIGN KEY ([LoomId]) REFERENCES [Dokuma].[Looms] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031211808_AddWeavingAndRolls'
)
BEGIN
    SET IDENTITY_INSERT [Core].[Permissions] ON;
    EXEC(N'INSERT INTO [Core].[Permissions] ([Id], [Code], [Module], [Description])
    SELECT v.[Id], v.[Code], v.[Module], v.[Description] FROM (VALUES
    (2016, N''Dokuma.Weaving.View'', N''Dokuma'', N''Tezgâh panosunu, dokunan leventleri ve topları görüntüleme''),
    (2017, N''Dokuma.Weaving.Manage'', N''Dokuma'', N''Leventi tezgâha bağlama ve bitirme, atkıya iplik verme, top kesme'')
    ) v([Id], [Code], [Module], [Description])
    WHERE NOT EXISTS (SELECT 1 FROM [Core].[Permissions] p WHERE p.[Id] = v.[Id] OR p.[Code] = v.[Code])');
    SET IDENTITY_INSERT [Core].[Permissions] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031211808_AddWeavingAndRolls'
)
BEGIN
    INSERT INTO [Core].[RolePermissions] ([RoleId], [PermissionId])
    SELECT v.[RoleId], v.[PermissionId] FROM (VALUES
    (1, 2016), (1, 2017)
    ) v([RoleId], [PermissionId])
    WHERE EXISTS (SELECT 1 FROM [Core].[Roles] r WHERE r.[Id] = v.[RoleId])
      AND EXISTS (SELECT 1 FROM [Core].[Permissions] p WHERE p.[Id] = v.[PermissionId])
      AND NOT EXISTS (SELECT 1 FROM [Core].[RolePermissions] rp WHERE rp.[RoleId] = v.[RoleId] AND rp.[PermissionId] = v.[PermissionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031211808_AddWeavingAndRolls'
)
BEGIN
    INSERT INTO [Dokuma].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261031211808_AddWeavingAndRolls', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031213326_AddRollInspection'
)
BEGIN
    ALTER TABLE [Dokuma].[FabricRolls] DROP CONSTRAINT [CK_FabricRolls_Status];
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031213326_AddRollInspection'
)
BEGIN
    ALTER TABLE [Dokuma].[FabricRolls] ADD [CalculatedStatus] nvarchar(30) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031213326_AddRollInspection'
)
BEGIN
    ALTER TABLE [Dokuma].[FabricRolls] ADD [InspectedMeters] decimal(12,2) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031213326_AddRollInspection'
)
BEGIN
    ALTER TABLE [Dokuma].[FabricRolls] ADD [InspectedOn] date NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031213326_AddRollInspection'
)
BEGIN
    ALTER TABLE [Dokuma].[FabricRolls] ADD [InspectedWidthCm] decimal(6,2) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031213326_AddRollInspection'
)
BEGIN
    ALTER TABLE [Dokuma].[FabricRolls] ADD [InspectionNotes] nvarchar(500) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031213326_AddRollInspection'
)
BEGIN
    ALTER TABLE [Dokuma].[FabricRolls] ADD [OverrideNote] nvarchar(500) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031213326_AddRollInspection'
)
BEGIN
    ALTER TABLE [Dokuma].[FabricRolls] ADD [PointsPer100M2] decimal(8,2) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031213326_AddRollInspection'
)
BEGIN
    ALTER TABLE [Dokuma].[FabricRolls] ADD [TotalPoints] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031213326_AddRollInspection'
)
BEGIN
    ALTER TABLE [Dokuma].[Customers] ADD [FirstQualityMaxPoints] decimal(8,2) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031213326_AddRollInspection'
)
BEGIN
    ALTER TABLE [Dokuma].[Customers] ADD [SecondQualityMaxPoints] decimal(8,2) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031213326_AddRollInspection'
)
BEGIN
    CREATE TABLE [Dokuma].[DefectTypes] (
        [Id] int NOT NULL IDENTITY,
        [Code] nvarchar(30) NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [AlwaysFourPoints] bit NOT NULL,
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
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031213326_AddRollInspection'
)
BEGIN
    CREATE TABLE [Dokuma].[QualitySettings] (
        [Id] int NOT NULL IDENTITY,
        [FirstQualityMaxPoints] decimal(8,2) NOT NULL,
        [SecondQualityMaxPoints] decimal(8,2) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_QualitySettings] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_QualitySettings_Limits] CHECK ([FirstQualityMaxPoints] >= 0 AND [SecondQualityMaxPoints] >= [FirstQualityMaxPoints])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031213326_AddRollInspection'
)
BEGIN
    CREATE TABLE [Dokuma].[RollDefects] (
        [Id] int NOT NULL IDENTITY,
        [RollId] int NOT NULL,
        [DefectTypeId] int NOT NULL,
        [AtMeter] decimal(12,2) NOT NULL,
        [LengthCm] decimal(8,2) NOT NULL,
        [Points] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_RollDefects] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_RollDefects_AtMeter] CHECK ([AtMeter] >= 0),
        CONSTRAINT [CK_RollDefects_LengthCm] CHECK ([LengthCm] > 0),
        CONSTRAINT [CK_RollDefects_Points] CHECK ([Points] BETWEEN 1 AND 4),
        CONSTRAINT [FK_RollDefects_DefectTypes_DefectTypeId] FOREIGN KEY ([DefectTypeId]) REFERENCES [Dokuma].[DefectTypes] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_RollDefects_FabricRolls_RollId] FOREIGN KEY ([RollId]) REFERENCES [Dokuma].[FabricRolls] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031213326_AddRollInspection'
)
BEGIN
    EXEC(N'ALTER TABLE [Dokuma].[FabricRolls] ADD CONSTRAINT [CK_FabricRolls_CalculatedStatus] CHECK ([CalculatedStatus] IS NULL OR [CalculatedStatus] IN (''PENDING_INSPECTION'', ''FIRST_QUALITY'', ''SECOND_QUALITY'', ''REJECTED''))');
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031213326_AddRollInspection'
)
BEGIN
    EXEC(N'ALTER TABLE [Dokuma].[FabricRolls] ADD CONSTRAINT [CK_FabricRolls_InspectedMeters] CHECK ([InspectedMeters] IS NULL OR [InspectedMeters] > 0)');
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031213326_AddRollInspection'
)
BEGIN
    EXEC(N'ALTER TABLE [Dokuma].[FabricRolls] ADD CONSTRAINT [CK_FabricRolls_InspectedWidthCm] CHECK ([InspectedWidthCm] IS NULL OR [InspectedWidthCm] > 0)');
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031213326_AddRollInspection'
)
BEGIN
    EXEC(N'ALTER TABLE [Dokuma].[FabricRolls] ADD CONSTRAINT [CK_FabricRolls_Status] CHECK ([Status] IN (''PENDING_INSPECTION'', ''FIRST_QUALITY'', ''SECOND_QUALITY'', ''REJECTED''))');
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031213326_AddRollInspection'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_DefectTypes_Code] ON [Dokuma].[DefectTypes] ([Code]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031213326_AddRollInspection'
)
BEGIN
    CREATE INDEX [IX_RollDefects_DefectTypeId] ON [Dokuma].[RollDefects] ([DefectTypeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031213326_AddRollInspection'
)
BEGIN
    CREATE INDEX [IX_RollDefects_RollId] ON [Dokuma].[RollDefects] ([RollId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031213326_AddRollInspection'
)
BEGIN
    EXEC(N'INSERT INTO [Dokuma].[DefectTypes] ([Code], [Name], [AlwaysFourPoints], [IsActive], [CreatedAt], [IsDeleted])
    SELECT v.[Code], v.[Name], v.[AlwaysFourPoints], 1, SYSUTCDATETIME(), 0 FROM (VALUES
    (N''ATKI-KACIGI'', N''Atkı kaçığı'', 0),
    (N''COZGU-KOPUGU'', N''Çözgü kopuğu'', 0),
    (N''CIFT-ATKI'', N''Çift atkı'', 0),
    (N''SIKLIK-FARKI'', N''Atkı sıklık farkı'', 0),
    (N''KALIN-YER'', N''Düğüm / kalın yer'', 0),
    (N''LEKE'', N''Leke'', 0),
    (N''KENAR'', N''Kenar hatası'', 0),
    (N''DELIK'', N''Delik'', 1)
    ) v([Code], [Name], [AlwaysFourPoints])
    WHERE NOT EXISTS (SELECT 1 FROM [Dokuma].[DefectTypes] d WHERE d.[Code] = v.[Code])');
    EXEC(N'INSERT INTO [Dokuma].[QualitySettings] ([FirstQualityMaxPoints], [SecondQualityMaxPoints], [CreatedAt])
    SELECT 20, 40, SYSUTCDATETIME() WHERE NOT EXISTS (SELECT 1 FROM [Dokuma].[QualitySettings])');
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031213326_AddRollInspection'
)
BEGIN
    SET IDENTITY_INSERT [Core].[Permissions] ON;
    EXEC(N'INSERT INTO [Core].[Permissions] ([Id], [Code], [Module], [Description])
    SELECT v.[Id], v.[Code], v.[Module], v.[Description] FROM (VALUES
    (2018, N''Dokuma.Inspection.View'', N''Dokuma'', N''Ham kontrolü görüntüleme''),
    (2019, N''Dokuma.Inspection.Manage'', N''Dokuma'', N''Ham kontrol yapma, hata türleri ve kalite sınırları''),
    (2020, N''Dokuma.Inspection.Override'', N''Dokuma'', N''Hesaplanan kaliteyi gerekçeyle değiştirme'')
    ) v([Id], [Code], [Module], [Description])
    WHERE NOT EXISTS (SELECT 1 FROM [Core].[Permissions] p WHERE p.[Id] = v.[Id] OR p.[Code] = v.[Code])');
    SET IDENTITY_INSERT [Core].[Permissions] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031213326_AddRollInspection'
)
BEGIN
    INSERT INTO [Core].[RolePermissions] ([RoleId], [PermissionId])
    SELECT v.[RoleId], v.[PermissionId] FROM (VALUES
    (1, 2018), (1, 2019), (1, 2020)
    ) v([RoleId], [PermissionId])
    WHERE EXISTS (SELECT 1 FROM [Core].[Roles] r WHERE r.[Id] = v.[RoleId])
      AND EXISTS (SELECT 1 FROM [Core].[Permissions] p WHERE p.[Id] = v.[PermissionId])
      AND NOT EXISTS (SELECT 1 FROM [Core].[RolePermissions] rp WHERE rp.[RoleId] = v.[RoleId] AND rp.[PermissionId] = v.[PermissionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031213326_AddRollInspection'
)
BEGIN
    INSERT INTO [Dokuma].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261031213326_AddRollInspection', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031215158_AddShippingAndAccounts'
)
BEGIN
    ALTER TABLE [Dokuma].[Customers] ADD [SecondQualityDiscountPercent] decimal(5,2) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031215158_AddShippingAndAccounts'
)
BEGIN
    CREATE TABLE [Dokuma].[Invoices] (
        [Id] int NOT NULL IDENTITY,
        [InvoiceNo] nvarchar(30) NOT NULL,
        [CustomerId] int NOT NULL,
        [InvoiceDate] date NOT NULL,
        [DueDate] date NULL,
        [Currency] nvarchar(3) NOT NULL,
        [VatRate] decimal(5,2) NULL,
        [WithholdingPercent] decimal(5,2) NULL,
        [Notes] nvarchar(1000) NULL,
        [Status] nvarchar(30) NOT NULL,
        [OpeningAmount] decimal(16,2) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_Invoices] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Invoices_Status] CHECK ([Status] IN ('DRAFT', 'CONFIRMED', 'CANCELLED')),
        CONSTRAINT [CK_Invoices_VatRate] CHECK ([VatRate] IS NULL OR [VatRate] BETWEEN 0 AND 100),
        CONSTRAINT [CK_Invoices_Withholding] CHECK ([WithholdingPercent] IS NULL OR [WithholdingPercent] BETWEEN 0 AND 100),
        CONSTRAINT [FK_Invoices_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Dokuma].[Customers] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031215158_AddShippingAndAccounts'
)
BEGIN
    CREATE TABLE [Dokuma].[Payments] (
        [Id] int NOT NULL IDENTITY,
        [PaymentNo] nvarchar(30) NOT NULL,
        [CustomerId] int NOT NULL,
        [PaymentDate] date NOT NULL,
        [Method] nvarchar(30) NOT NULL,
        [Amount] decimal(16,2) NOT NULL,
        [Currency] nvarchar(3) NOT NULL,
        [ReferenceNo] nvarchar(50) NULL,
        [DueDate] date NULL,
        [Notes] nvarchar(500) NULL,
        [Status] nvarchar(30) NOT NULL,
        [CancelReason] nvarchar(500) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_Payments] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Payments_Amount] CHECK ([Amount] > 0),
        CONSTRAINT [CK_Payments_Method] CHECK ([Method] IN ('CASH', 'BANK_TRANSFER', 'CHECK', 'OPENING')),
        CONSTRAINT [CK_Payments_Status] CHECK ([Status] IN ('ACTIVE', 'CANCELLED')),
        CONSTRAINT [FK_Payments_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Dokuma].[Customers] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031215158_AddShippingAndAccounts'
)
BEGIN
    CREATE TABLE [Dokuma].[YarnReturns] (
        [Id] int NOT NULL IDENTITY,
        [ReturnNo] nvarchar(30) NOT NULL,
        [YarnLotId] int NOT NULL,
        [ReturnDate] date NOT NULL,
        [Kilograms] decimal(12,2) NOT NULL,
        [Notes] nvarchar(500) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_YarnReturns] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_YarnReturns_Kilograms] CHECK ([Kilograms] > 0),
        CONSTRAINT [FK_YarnReturns_YarnLots_YarnLotId] FOREIGN KEY ([YarnLotId]) REFERENCES [Dokuma].[YarnLots] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031215158_AddShippingAndAccounts'
)
BEGIN
    CREATE TABLE [Dokuma].[Despatches] (
        [Id] int NOT NULL IDENTITY,
        [DespatchNo] nvarchar(30) NOT NULL,
        [CustomerId] int NOT NULL,
        [DespatchDate] date NOT NULL,
        [VehiclePlate] nvarchar(20) NULL,
        [DriverName] nvarchar(100) NULL,
        [DeliveryAddress] nvarchar(500) NULL,
        [Notes] nvarchar(1000) NULL,
        [Status] nvarchar(30) NOT NULL,
        [InvoiceId] int NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_Despatches] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Despatches_Status] CHECK ([Status] IN ('DRAFT', 'CONFIRMED', 'CANCELLED')),
        CONSTRAINT [FK_Despatches_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Dokuma].[Customers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Despatches_Invoices_InvoiceId] FOREIGN KEY ([InvoiceId]) REFERENCES [Dokuma].[Invoices] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031215158_AddShippingAndAccounts'
)
BEGIN
    CREATE TABLE [Dokuma].[InvoiceLines] (
        [Id] int NOT NULL IDENTITY,
        [InvoiceId] int NOT NULL,
        [OrderLineId] int NOT NULL,
        [Grade] nvarchar(30) NOT NULL,
        [Description] nvarchar(300) NOT NULL,
        [Quantity] decimal(14,2) NOT NULL,
        [UnitPrice] decimal(14,4) NULL,
        [DiscountPercent] decimal(5,2) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_InvoiceLines] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_InvoiceLines_Discount] CHECK ([DiscountPercent] IS NULL OR [DiscountPercent] BETWEEN 0 AND 100),
        CONSTRAINT [CK_InvoiceLines_Grade] CHECK ([Grade] IN ('FIRST_QUALITY', 'SECOND_QUALITY')),
        CONSTRAINT [FK_InvoiceLines_Invoices_InvoiceId] FOREIGN KEY ([InvoiceId]) REFERENCES [Dokuma].[Invoices] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_InvoiceLines_OrderLines_OrderLineId] FOREIGN KEY ([OrderLineId]) REFERENCES [Dokuma].[OrderLines] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031215158_AddShippingAndAccounts'
)
BEGIN
    CREATE TABLE [Dokuma].[PaymentAllocations] (
        [Id] int NOT NULL IDENTITY,
        [PaymentId] int NOT NULL,
        [InvoiceId] int NOT NULL,
        [Amount] decimal(16,2) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_PaymentAllocations] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_PaymentAllocations_Amount] CHECK ([Amount] > 0),
        CONSTRAINT [FK_PaymentAllocations_Invoices_InvoiceId] FOREIGN KEY ([InvoiceId]) REFERENCES [Dokuma].[Invoices] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PaymentAllocations_Payments_PaymentId] FOREIGN KEY ([PaymentId]) REFERENCES [Dokuma].[Payments] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031215158_AddShippingAndAccounts'
)
BEGIN
    CREATE TABLE [Dokuma].[PackingLists] (
        [Id] int NOT NULL IDENTITY,
        [PackingNo] nvarchar(30) NOT NULL,
        [OrderId] int NOT NULL,
        [PackingDate] date NOT NULL,
        [Notes] nvarchar(1000) NULL,
        [Status] nvarchar(30) NOT NULL,
        [SaleCustomerId] int NULL,
        [DespatchId] int NULL,
        [BoyahaneReceiptNo] nvarchar(30) NULL,
        [BoyahaneSentOn] date NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_PackingLists] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_PackingLists_Status] CHECK ([Status] IN ('DRAFT', 'CONFIRMED', 'CANCELLED')),
        CONSTRAINT [FK_PackingLists_Customers_SaleCustomerId] FOREIGN KEY ([SaleCustomerId]) REFERENCES [Dokuma].[Customers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PackingLists_Despatches_DespatchId] FOREIGN KEY ([DespatchId]) REFERENCES [Dokuma].[Despatches] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PackingLists_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Dokuma].[Orders] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031215158_AddShippingAndAccounts'
)
BEGIN
    CREATE TABLE [Dokuma].[PackingItems] (
        [Id] int NOT NULL IDENTITY,
        [PackingListId] int NOT NULL,
        [RollId] int NOT NULL,
        [Meters] decimal(12,2) NOT NULL,
        [Kilograms] decimal(12,2) NULL,
        [Grade] nvarchar(30) NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_PackingItems] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_PackingItems_Grade] CHECK ([Grade] IN ('FIRST_QUALITY', 'SECOND_QUALITY')),
        CONSTRAINT [CK_PackingItems_Kilograms] CHECK ([Kilograms] IS NULL OR [Kilograms] > 0),
        CONSTRAINT [CK_PackingItems_Meters] CHECK ([Meters] > 0),
        CONSTRAINT [FK_PackingItems_FabricRolls_RollId] FOREIGN KEY ([RollId]) REFERENCES [Dokuma].[FabricRolls] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PackingItems_PackingLists_PackingListId] FOREIGN KEY ([PackingListId]) REFERENCES [Dokuma].[PackingLists] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031215158_AddShippingAndAccounts'
)
BEGIN
    CREATE INDEX [IX_Despatches_CustomerId] ON [Dokuma].[Despatches] ([CustomerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031215158_AddShippingAndAccounts'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Despatches_DespatchNo] ON [Dokuma].[Despatches] ([DespatchNo]);
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031215158_AddShippingAndAccounts'
)
BEGIN
    CREATE INDEX [IX_Despatches_InvoiceId] ON [Dokuma].[Despatches] ([InvoiceId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031215158_AddShippingAndAccounts'
)
BEGIN
    CREATE INDEX [IX_InvoiceLines_InvoiceId] ON [Dokuma].[InvoiceLines] ([InvoiceId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031215158_AddShippingAndAccounts'
)
BEGIN
    CREATE INDEX [IX_InvoiceLines_OrderLineId] ON [Dokuma].[InvoiceLines] ([OrderLineId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031215158_AddShippingAndAccounts'
)
BEGIN
    CREATE INDEX [IX_Invoices_CustomerId] ON [Dokuma].[Invoices] ([CustomerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031215158_AddShippingAndAccounts'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Invoices_InvoiceNo] ON [Dokuma].[Invoices] ([InvoiceNo]);
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031215158_AddShippingAndAccounts'
)
BEGIN
    CREATE INDEX [IX_PackingItems_PackingListId] ON [Dokuma].[PackingItems] ([PackingListId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031215158_AddShippingAndAccounts'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_PackingItems_Active] ON [Dokuma].[PackingItems] ([RollId]) WHERE [IsActive] = 1');
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031215158_AddShippingAndAccounts'
)
BEGIN
    CREATE INDEX [IX_PackingLists_DespatchId] ON [Dokuma].[PackingLists] ([DespatchId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031215158_AddShippingAndAccounts'
)
BEGIN
    CREATE INDEX [IX_PackingLists_OrderId] ON [Dokuma].[PackingLists] ([OrderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031215158_AddShippingAndAccounts'
)
BEGIN
    CREATE UNIQUE INDEX [IX_PackingLists_PackingNo] ON [Dokuma].[PackingLists] ([PackingNo]);
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031215158_AddShippingAndAccounts'
)
BEGIN
    CREATE INDEX [IX_PackingLists_SaleCustomerId] ON [Dokuma].[PackingLists] ([SaleCustomerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031215158_AddShippingAndAccounts'
)
BEGIN
    CREATE INDEX [IX_PaymentAllocations_InvoiceId] ON [Dokuma].[PaymentAllocations] ([InvoiceId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031215158_AddShippingAndAccounts'
)
BEGIN
    CREATE UNIQUE INDEX [IX_PaymentAllocations_PaymentId_InvoiceId] ON [Dokuma].[PaymentAllocations] ([PaymentId], [InvoiceId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031215158_AddShippingAndAccounts'
)
BEGIN
    CREATE INDEX [IX_Payments_CustomerId_Currency_PaymentDate] ON [Dokuma].[Payments] ([CustomerId], [Currency], [PaymentDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031215158_AddShippingAndAccounts'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Payments_PaymentNo] ON [Dokuma].[Payments] ([PaymentNo]);
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031215158_AddShippingAndAccounts'
)
BEGIN
    CREATE UNIQUE INDEX [IX_YarnReturns_ReturnNo] ON [Dokuma].[YarnReturns] ([ReturnNo]);
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031215158_AddShippingAndAccounts'
)
BEGIN
    CREATE INDEX [IX_YarnReturns_YarnLotId] ON [Dokuma].[YarnReturns] ([YarnLotId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031215158_AddShippingAndAccounts'
)
BEGIN
    SET IDENTITY_INSERT [Core].[Permissions] ON;
    EXEC(N'INSERT INTO [Core].[Permissions] ([Id], [Code], [Module], [Description])
    SELECT v.[Id], v.[Code], v.[Module], v.[Description] FROM (VALUES
    (2021, N''Dokuma.Packing.View'', N''Dokuma'', N''Çeki listelerini görüntüleme''),
    (2022, N''Dokuma.Packing.Manage'', N''Dokuma'', N''Çeki listesi hazırlama, onaylama ve Boyahane''''ye gönderme''),
    (2023, N''Dokuma.Shipment.View'', N''Dokuma'', N''İrsaliyeleri görüntüleme''),
    (2024, N''Dokuma.Shipment.Manage'', N''Dokuma'', N''İrsaliye hazırlama ve onaylama''),
    (2025, N''Dokuma.Invoice.View'', N''Dokuma'', N''Faturaları görüntüleme''),
    (2026, N''Dokuma.Invoice.Manage'', N''Dokuma'', N''Fatura hazırlama ve onaylama''),
    (2027, N''Dokuma.Account.View'', N''Dokuma'', N''Cari hesapları, ekstreyi ve tahsilatları görüntüleme''),
    (2028, N''Dokuma.Account.Manage'', N''Dokuma'', N''Tahsilat girişi, faturalara dağıtım ve iptal'')
    ) v([Id], [Code], [Module], [Description])
    WHERE NOT EXISTS (SELECT 1 FROM [Core].[Permissions] p WHERE p.[Id] = v.[Id] OR p.[Code] = v.[Code])');
    SET IDENTITY_INSERT [Core].[Permissions] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031215158_AddShippingAndAccounts'
)
BEGIN
    INSERT INTO [Core].[RolePermissions] ([RoleId], [PermissionId])
    SELECT v.[RoleId], v.[PermissionId] FROM (VALUES
    (1, 2021), (1, 2022), (1, 2023), (1, 2024), (1, 2025), (1, 2026), (1, 2027), (1, 2028)
    ) v([RoleId], [PermissionId])
    WHERE EXISTS (SELECT 1 FROM [Core].[Roles] r WHERE r.[Id] = v.[RoleId])
      AND EXISTS (SELECT 1 FROM [Core].[Permissions] p WHERE p.[Id] = v.[PermissionId])
      AND NOT EXISTS (SELECT 1 FROM [Core].[RolePermissions] rp WHERE rp.[RoleId] = v.[RoleId] AND rp.[PermissionId] = v.[PermissionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031215158_AddShippingAndAccounts'
)
BEGIN
    INSERT INTO [Dokuma].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261031215158_AddShippingAndAccounts', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031220748_AddYarnTakeBacks'
)
BEGIN
    CREATE TABLE [Dokuma].[YarnTakeBacks] (
        [Id] int NOT NULL IDENTITY,
        [WarpOrderId] int NULL,
        [BeamId] int NULL,
        [YarnLotId] int NOT NULL,
        [TakenOn] date NOT NULL,
        [Kilograms] decimal(12,2) NOT NULL,
        [Notes] nvarchar(500) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_YarnTakeBacks] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_YarnTakeBacks_Kilograms] CHECK ([Kilograms] > 0),
        CONSTRAINT [CK_YarnTakeBacks_Source] CHECK (([WarpOrderId] IS NULL AND [BeamId] IS NOT NULL) OR ([WarpOrderId] IS NOT NULL AND [BeamId] IS NULL)),
        CONSTRAINT [FK_YarnTakeBacks_Beams_BeamId] FOREIGN KEY ([BeamId]) REFERENCES [Dokuma].[Beams] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_YarnTakeBacks_WarpOrders_WarpOrderId] FOREIGN KEY ([WarpOrderId]) REFERENCES [Dokuma].[WarpOrders] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_YarnTakeBacks_YarnLots_YarnLotId] FOREIGN KEY ([YarnLotId]) REFERENCES [Dokuma].[YarnLots] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031220748_AddYarnTakeBacks'
)
BEGIN
    CREATE INDEX [IX_YarnTakeBacks_BeamId] ON [Dokuma].[YarnTakeBacks] ([BeamId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031220748_AddYarnTakeBacks'
)
BEGIN
    CREATE INDEX [IX_YarnTakeBacks_WarpOrderId] ON [Dokuma].[YarnTakeBacks] ([WarpOrderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031220748_AddYarnTakeBacks'
)
BEGIN
    CREATE INDEX [IX_YarnTakeBacks_YarnLotId] ON [Dokuma].[YarnTakeBacks] ([YarnLotId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031220748_AddYarnTakeBacks'
)
BEGIN
    INSERT INTO [Dokuma].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261031220748_AddYarnTakeBacks', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031221931_AddFloorAndReports'
)
BEGIN
    ALTER TABLE [Dokuma].[FabricRolls] ADD [ClientRequestId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031221931_AddFloorAndReports'
)
BEGIN
    ALTER TABLE [Dokuma].[FabricRolls] ADD [CutByOperatorId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031221931_AddFloorAndReports'
)
BEGIN
    CREATE TABLE [Dokuma].[Operators] (
        [Id] int NOT NULL IDENTITY,
        [Code] nvarchar(30) NOT NULL,
        [Name] nvarchar(150) NOT NULL,
        [Role] nvarchar(30) NOT NULL,
        [CardUid] nvarchar(10) NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_Operators] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Operators_Role] CHECK ([Role] IN ('WEAVER', 'SUPERVISOR', 'MAINTENANCE'))
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031221931_AddFloorAndReports'
)
BEGIN
    CREATE TABLE [Dokuma].[RfidCards] (
        [Id] int NOT NULL IDENTITY,
        [Uid] nvarchar(10) NOT NULL,
        [Label] nvarchar(50) NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_RfidCards] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031221931_AddFloorAndReports'
)
BEGIN
    CREATE TABLE [Dokuma].[StopReasons] (
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
        CONSTRAINT [CK_StopReasons_Category] CHECK ([Category] IN ('STOP', 'BREAKDOWN', 'PLANNED'))
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031221931_AddFloorAndReports'
)
BEGIN
    CREATE TABLE [Dokuma].[Tablets] (
        [Id] int NOT NULL IDENTITY,
        [TabletCode] nvarchar(30) NOT NULL,
        [Name] nvarchar(150) NULL,
        [LoomId] int NULL,
        [IsActive] bit NOT NULL,
        [DeviceIdentifier] nvarchar(200) NULL,
        [DeviceKeyHash] nvarchar(128) NULL,
        [LastSeenAt] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_Tablets] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Tablets_Looms_LoomId] FOREIGN KEY ([LoomId]) REFERENCES [Dokuma].[Looms] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031221931_AddFloorAndReports'
)
BEGIN
    CREATE TABLE [Dokuma].[LoomStops] (
        [Id] bigint NOT NULL IDENTITY,
        [LoomId] int NOT NULL,
        [BeamId] int NULL,
        [ReasonId] int NOT NULL,
        [Category] nvarchar(30) NOT NULL,
        [Note] nvarchar(500) NULL,
        [StartedAt] datetime2 NOT NULL,
        [EndedAt] datetime2 NULL,
        [StartedBy] nvarchar(150) NOT NULL,
        [EndedBy] nvarchar(150) NULL,
        [TabletId] int NULL,
        CONSTRAINT [PK_LoomStops] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_LoomStops_Category] CHECK ([Category] IN ('STOP', 'BREAKDOWN', 'PLANNED')),
        CONSTRAINT [FK_LoomStops_Beams_BeamId] FOREIGN KEY ([BeamId]) REFERENCES [Dokuma].[Beams] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_LoomStops_Looms_LoomId] FOREIGN KEY ([LoomId]) REFERENCES [Dokuma].[Looms] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_LoomStops_StopReasons_ReasonId] FOREIGN KEY ([ReasonId]) REFERENCES [Dokuma].[StopReasons] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031221931_AddFloorAndReports'
)
BEGIN
    CREATE TABLE [Dokuma].[TabletSessions] (
        [Id] bigint NOT NULL IDENTITY,
        [TabletId] int NOT NULL,
        [OperatorId] int NOT NULL,
        [StartedAt] datetime2 NOT NULL,
        [EndedAt] datetime2 NULL,
        [EndReason] nvarchar(30) NULL,
        CONSTRAINT [PK_TabletSessions] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_TabletSessions_EndReason] CHECK ([EndReason] IN ('LOGOUT', 'TAKEN_OVER', 'MOVED_AWAY', 'CLOSED_BY_OFFICE')),
        CONSTRAINT [FK_TabletSessions_Operators_OperatorId] FOREIGN KEY ([OperatorId]) REFERENCES [Dokuma].[Operators] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_TabletSessions_Tablets_TabletId] FOREIGN KEY ([TabletId]) REFERENCES [Dokuma].[Tablets] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031221931_AddFloorAndReports'
)
BEGIN
    CREATE INDEX [IX_FabricRolls_CutByOperatorId] ON [Dokuma].[FabricRolls] ([CutByOperatorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031221931_AddFloorAndReports'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_FabricRolls_ClientRequest] ON [Dokuma].[FabricRolls] ([ClientRequestId]) WHERE [ClientRequestId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031221931_AddFloorAndReports'
)
BEGIN
    CREATE INDEX [IX_LoomStops_BeamId] ON [Dokuma].[LoomStops] ([BeamId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031221931_AddFloorAndReports'
)
BEGIN
    CREATE INDEX [IX_LoomStops_LoomId_StartedAt] ON [Dokuma].[LoomStops] ([LoomId], [StartedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031221931_AddFloorAndReports'
)
BEGIN
    CREATE INDEX [IX_LoomStops_ReasonId] ON [Dokuma].[LoomStops] ([ReasonId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031221931_AddFloorAndReports'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_LoomStops_Open] ON [Dokuma].[LoomStops] ([LoomId]) WHERE [EndedAt] IS NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031221931_AddFloorAndReports'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Operators_Code] ON [Dokuma].[Operators] ([Code]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031221931_AddFloorAndReports'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_Operators_CardUid] ON [Dokuma].[Operators] ([CardUid]) WHERE [CardUid] IS NOT NULL AND [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031221931_AddFloorAndReports'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_RfidCards_Uid] ON [Dokuma].[RfidCards] ([Uid]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031221931_AddFloorAndReports'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_StopReasons_Code] ON [Dokuma].[StopReasons] ([Code]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031221931_AddFloorAndReports'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Tablets_TabletCode] ON [Dokuma].[Tablets] ([TabletCode]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031221931_AddFloorAndReports'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_Tablets_Device] ON [Dokuma].[Tablets] ([DeviceIdentifier]) WHERE [DeviceIdentifier] IS NOT NULL AND [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031221931_AddFloorAndReports'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_Tablets_Loom] ON [Dokuma].[Tablets] ([LoomId]) WHERE [LoomId] IS NOT NULL AND [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031221931_AddFloorAndReports'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_TabletSessions_Open] ON [Dokuma].[TabletSessions] ([TabletId]) WHERE [EndedAt] IS NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031221931_AddFloorAndReports'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_TabletSessions_OperatorOpen] ON [Dokuma].[TabletSessions] ([OperatorId]) WHERE [EndedAt] IS NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031221931_AddFloorAndReports'
)
BEGIN
    ALTER TABLE [Dokuma].[FabricRolls] ADD CONSTRAINT [FK_FabricRolls_Operators_CutByOperatorId] FOREIGN KEY ([CutByOperatorId]) REFERENCES [Dokuma].[Operators] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031221931_AddFloorAndReports'
)
BEGIN
    SET IDENTITY_INSERT [Core].[Permissions] ON;
    EXEC(N'INSERT INTO [Core].[Permissions] ([Id], [Code], [Module], [Description])
    SELECT v.[Id], v.[Code], v.[Module], v.[Description] FROM (VALUES
    (2029, N''Dokuma.Floor.View'', N''Dokuma'', N''Dokuma personelini, kartları, tabletleri ve oturumları görüntüleme''),
    (2030, N''Dokuma.Floor.Manage'', N''Dokuma'', N''Personel, kart, tablet ve duruş nedeni tanımlama; ofisten duruş ve oturum işlemleri''),
    (2031, N''Dokuma.Report.View'', N''Dokuma'', N''Tezgâh verimi, sipariş takibi ve kalite raporları'')
    ) v([Id], [Code], [Module], [Description])
    WHERE NOT EXISTS (SELECT 1 FROM [Core].[Permissions] p WHERE p.[Id] = v.[Id] OR p.[Code] = v.[Code])');
    SET IDENTITY_INSERT [Core].[Permissions] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031221931_AddFloorAndReports'
)
BEGIN
    INSERT INTO [Core].[RolePermissions] ([RoleId], [PermissionId])
    SELECT v.[RoleId], v.[PermissionId] FROM (VALUES
    (1, 2029), (1, 2030), (1, 2031)
    ) v([RoleId], [PermissionId])
    WHERE EXISTS (SELECT 1 FROM [Core].[Roles] r WHERE r.[Id] = v.[RoleId])
      AND EXISTS (SELECT 1 FROM [Core].[Permissions] p WHERE p.[Id] = v.[PermissionId])
      AND NOT EXISTS (SELECT 1 FROM [Core].[RolePermissions] rp WHERE rp.[RoleId] = v.[RoleId] AND rp.[PermissionId] = v.[PermissionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031221931_AddFloorAndReports'
)
BEGIN
    INSERT INTO [Dokuma].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261031221931_AddFloorAndReports', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031223951_AddDefaultRoles'
)
BEGIN
    SET IDENTITY_INSERT [Core].[Roles] ON;
    EXEC(N'INSERT INTO [Core].[Roles] ([Id], [Code], [Name], [IsSystem], [ModuleKey])
    SELECT v.[Id], v.[Code], v.[Name], v.[IsSystem], v.[ModuleKey] FROM (VALUES
    (201, N''DOKUMA_MANAGER'', N''Dokuma Yöneticisi'', 1, N''Dokuma''),
    (202, N''DOKUMA_SALES'', N''Dokuma Satış ve Planlama'', 1, N''Dokuma''),
    (203, N''DOKUMA_PRODUCTION'', N''Dokuma Üretim Sorumlusu'', 1, N''Dokuma''),
    (204, N''DOKUMA_QUALITY'', N''Dokuma Kalite Kontrol'', 1, N''Dokuma''),
    (205, N''DOKUMA_QUALITY_LEAD'', N''Dokuma Kalite Sorumlusu'', 1, N''Dokuma''),
    (206, N''DOKUMA_WAREHOUSE'', N''Dokuma Depo ve Sevkiyat'', 1, N''Dokuma''),
    (207, N''DOKUMA_ACCOUNTING'', N''Dokuma Muhasebe'', 1, N''Dokuma'')
    ) v([Id], [Code], [Name], [IsSystem], [ModuleKey])
    WHERE NOT EXISTS (SELECT 1 FROM [Core].[Roles] r WHERE r.[Id] = v.[Id] OR r.[Code] = v.[Code])');
    SET IDENTITY_INSERT [Core].[Roles] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031223951_AddDefaultRoles'
)
BEGIN
    INSERT INTO [Core].[RolePermissions] ([RoleId], [PermissionId])
    SELECT r.[Id], p.[Id] FROM (VALUES
    (N'DOKUMA_MANAGER', N'Dokuma.View'),
    (N'DOKUMA_MANAGER', N'Dokuma.Customer.View'),
    (N'DOKUMA_MANAGER', N'Dokuma.Customer.Manage'),
    (N'DOKUMA_MANAGER', N'Dokuma.Material.View'),
    (N'DOKUMA_MANAGER', N'Dokuma.Material.Manage'),
    (N'DOKUMA_MANAGER', N'Dokuma.Loom.View'),
    (N'DOKUMA_MANAGER', N'Dokuma.Loom.Manage'),
    (N'DOKUMA_MANAGER', N'Dokuma.Purchase.View'),
    (N'DOKUMA_MANAGER', N'Dokuma.Purchase.Manage'),
    (N'DOKUMA_MANAGER', N'Dokuma.Order.View'),
    (N'DOKUMA_MANAGER', N'Dokuma.Order.Manage'),
    (N'DOKUMA_MANAGER', N'Dokuma.Receipt.View'),
    (N'DOKUMA_MANAGER', N'Dokuma.Receipt.Manage'),
    (N'DOKUMA_MANAGER', N'Dokuma.Warp.View'),
    (N'DOKUMA_MANAGER', N'Dokuma.Warp.Manage'),
    (N'DOKUMA_MANAGER', N'Dokuma.Weaving.View'),
    (N'DOKUMA_MANAGER', N'Dokuma.Weaving.Manage'),
    (N'DOKUMA_MANAGER', N'Dokuma.Inspection.View'),
    (N'DOKUMA_MANAGER', N'Dokuma.Inspection.Manage'),
    (N'DOKUMA_MANAGER', N'Dokuma.Packing.View'),
    (N'DOKUMA_MANAGER', N'Dokuma.Packing.Manage'),
    (N'DOKUMA_MANAGER', N'Dokuma.Shipment.View'),
    (N'DOKUMA_MANAGER', N'Dokuma.Shipment.Manage'),
    (N'DOKUMA_MANAGER', N'Dokuma.Invoice.View'),
    (N'DOKUMA_MANAGER', N'Dokuma.Invoice.Manage'),
    (N'DOKUMA_MANAGER', N'Dokuma.Account.View'),
    (N'DOKUMA_MANAGER', N'Dokuma.Account.Manage'),
    (N'DOKUMA_MANAGER', N'Dokuma.Floor.View'),
    (N'DOKUMA_MANAGER', N'Dokuma.Floor.Manage'),
    (N'DOKUMA_MANAGER', N'Dokuma.Inspection.Override'),
    (N'DOKUMA_MANAGER', N'Dokuma.Report.View'),
    (N'DOKUMA_SALES', N'Dokuma.View'),
    (N'DOKUMA_SALES', N'Dokuma.Customer.View'),
    (N'DOKUMA_SALES', N'Dokuma.Customer.Manage'),
    (N'DOKUMA_SALES', N'Dokuma.Order.View'),
    (N'DOKUMA_SALES', N'Dokuma.Order.Manage'),
    (N'DOKUMA_SALES', N'Dokuma.Report.View'),
    (N'DOKUMA_SALES', N'Dokuma.Material.View'),
    (N'DOKUMA_SALES', N'Dokuma.Loom.View'),
    (N'DOKUMA_SALES', N'Dokuma.Warp.View'),
    (N'DOKUMA_SALES', N'Dokuma.Weaving.View'),
    (N'DOKUMA_PRODUCTION', N'Dokuma.View'),
    (N'DOKUMA_PRODUCTION', N'Dokuma.Warp.View'),
    (N'DOKUMA_PRODUCTION', N'Dokuma.Warp.Manage'),
    (N'DOKUMA_PRODUCTION', N'Dokuma.Weaving.View'),
    (N'DOKUMA_PRODUCTION', N'Dokuma.Weaving.Manage'),
    (N'DOKUMA_PRODUCTION', N'Dokuma.Floor.View'),
    (N'DOKUMA_PRODUCTION', N'Dokuma.Floor.Manage'),
    (N'DOKUMA_PRODUCTION', N'Dokuma.Report.View'),
    (N'DOKUMA_PRODUCTION', N'Dokuma.Order.View'),
    (N'DOKUMA_PRODUCTION', N'Dokuma.Receipt.View'),
    (N'DOKUMA_QUALITY', N'Dokuma.View'),
    (N'DOKUMA_QUALITY', N'Dokuma.Inspection.View'),
    (N'DOKUMA_QUALITY', N'Dokuma.Inspection.Manage'),
    (N'DOKUMA_QUALITY', N'Dokuma.Report.View'),
    (N'DOKUMA_QUALITY', N'Dokuma.Warp.View'),
    (N'DOKUMA_QUALITY', N'Dokuma.Weaving.View'),
    (N'DOKUMA_QUALITY_LEAD', N'Dokuma.View'),
    (N'DOKUMA_QUALITY_LEAD', N'Dokuma.Inspection.View'),
    (N'DOKUMA_QUALITY_LEAD', N'Dokuma.Inspection.Manage'),
    (N'DOKUMA_QUALITY_LEAD', N'Dokuma.Inspection.Override'),
    (N'DOKUMA_QUALITY_LEAD', N'Dokuma.Report.View'),
    (N'DOKUMA_QUALITY_LEAD', N'Dokuma.Warp.View'),
    (N'DOKUMA_QUALITY_LEAD', N'Dokuma.Weaving.View'),
    (N'DOKUMA_WAREHOUSE', N'Dokuma.View'),
    (N'DOKUMA_WAREHOUSE', N'Dokuma.Receipt.View'),
    (N'DOKUMA_WAREHOUSE', N'Dokuma.Receipt.Manage'),
    (N'DOKUMA_WAREHOUSE', N'Dokuma.Packing.View'),
    (N'DOKUMA_WAREHOUSE', N'Dokuma.Packing.Manage'),
    (N'DOKUMA_WAREHOUSE', N'Dokuma.Shipment.View'),
    (N'DOKUMA_WAREHOUSE', N'Dokuma.Shipment.Manage'),
    (N'DOKUMA_ACCOUNTING', N'Dokuma.View'),
    (N'DOKUMA_ACCOUNTING', N'Dokuma.Invoice.View'),
    (N'DOKUMA_ACCOUNTING', N'Dokuma.Invoice.Manage'),
    (N'DOKUMA_ACCOUNTING', N'Dokuma.Account.View'),
    (N'DOKUMA_ACCOUNTING', N'Dokuma.Account.Manage'),
    (N'DOKUMA_ACCOUNTING', N'Dokuma.Order.View'),
    (N'DOKUMA_ACCOUNTING', N'Dokuma.Shipment.View')
    ) v([RoleCode], [PermissionCode])
    JOIN [Core].[Roles] r ON r.[Code] = v.[RoleCode]
    JOIN [Core].[Permissions] p ON p.[Code] = v.[PermissionCode]
    WHERE NOT EXISTS (SELECT 1 FROM [Core].[RolePermissions] rp WHERE rp.[RoleId] = r.[Id] AND rp.[PermissionId] = p.[Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031223951_AddDefaultRoles'
)
BEGIN
    INSERT INTO [Dokuma].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261031223951_AddDefaultRoles', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031224742_AddOperatorCardAndPauses'
)
BEGIN
    ALTER TABLE [Dokuma].[StopReasons] DROP CONSTRAINT [CK_StopReasons_Category];
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031224742_AddOperatorCardAndPauses'
)
BEGIN
    ALTER TABLE [Dokuma].[LoomStops] DROP CONSTRAINT [CK_LoomStops_Category];
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031224742_AddOperatorCardAndPauses'
)
BEGIN
    DECLARE @var nvarchar(max);
    SELECT @var = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Dokuma].[Operators]') AND [c].[name] = N'Name');
    IF @var IS NOT NULL EXEC(N'ALTER TABLE [Dokuma].[Operators] DROP CONSTRAINT ' + @var + ';');
    ALTER TABLE [Dokuma].[Operators] ALTER COLUMN [Name] nvarchar(201) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031224742_AddOperatorCardAndPauses'
)
BEGIN
    ALTER TABLE [Dokuma].[Operators] ADD [CanPause] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031224742_AddOperatorCardAndPauses'
)
BEGIN
    ALTER TABLE [Dokuma].[Operators] ADD [CanReportBreakdown] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031224742_AddOperatorCardAndPauses'
)
BEGIN
    ALTER TABLE [Dokuma].[Operators] ADD [FirstName] nvarchar(100) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031224742_AddOperatorCardAndPauses'
)
BEGIN
    ALTER TABLE [Dokuma].[Operators] ADD [LastName] nvarchar(100) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031224742_AddOperatorCardAndPauses'
)
BEGIN
    CREATE TABLE [Dokuma].[OperatorLooms] (
        [OperatorId] int NOT NULL,
        [LoomId] int NOT NULL,
        CONSTRAINT [PK_OperatorLooms] PRIMARY KEY ([OperatorId], [LoomId]),
        CONSTRAINT [FK_OperatorLooms_Looms_LoomId] FOREIGN KEY ([LoomId]) REFERENCES [Dokuma].[Looms] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_OperatorLooms_Operators_OperatorId] FOREIGN KEY ([OperatorId]) REFERENCES [Dokuma].[Operators] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031224742_AddOperatorCardAndPauses'
)
BEGIN
    CREATE TABLE [Dokuma].[OperatorPhotos] (
        [OperatorId] int NOT NULL,
        [Content] varbinary(max) NOT NULL,
        [ContentType] nvarchar(30) NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_OperatorPhotos] PRIMARY KEY ([OperatorId]),
        CONSTRAINT [FK_OperatorPhotos_Operators_OperatorId] FOREIGN KEY ([OperatorId]) REFERENCES [Dokuma].[Operators] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031224742_AddOperatorCardAndPauses'
)
BEGIN
    CREATE TABLE [Dokuma].[SessionPauses] (
        [Id] bigint NOT NULL IDENTITY,
        [SessionId] bigint NOT NULL,
        [Kind] nvarchar(30) NOT NULL,
        [ReasonId] int NULL,
        [Note] nvarchar(500) NULL,
        [StartedAt] datetime2 NOT NULL,
        [EndedAt] datetime2 NULL,
        CONSTRAINT [PK_SessionPauses] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_SessionPauses_Kind] CHECK ([Kind] IN ('BREAK', 'PAUSE')),
        CONSTRAINT [FK_SessionPauses_StopReasons_ReasonId] FOREIGN KEY ([ReasonId]) REFERENCES [Dokuma].[StopReasons] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_SessionPauses_TabletSessions_SessionId] FOREIGN KEY ([SessionId]) REFERENCES [Dokuma].[TabletSessions] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031224742_AddOperatorCardAndPauses'
)
BEGIN
    EXEC(N'ALTER TABLE [Dokuma].[StopReasons] ADD CONSTRAINT [CK_StopReasons_Category] CHECK ([Category] IN (''PAUSE'', ''STOP'', ''BREAKDOWN'', ''PLANNED''))');
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031224742_AddOperatorCardAndPauses'
)
BEGIN
    EXEC(N'ALTER TABLE [Dokuma].[LoomStops] ADD CONSTRAINT [CK_LoomStops_Category] CHECK ([Category] IN (''PAUSE'', ''STOP'', ''BREAKDOWN'', ''PLANNED''))');
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031224742_AddOperatorCardAndPauses'
)
BEGIN
    CREATE INDEX [IX_OperatorLooms_LoomId] ON [Dokuma].[OperatorLooms] ([LoomId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031224742_AddOperatorCardAndPauses'
)
BEGIN
    CREATE INDEX [IX_SessionPauses_ReasonId] ON [Dokuma].[SessionPauses] ([ReasonId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031224742_AddOperatorCardAndPauses'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_SessionPauses_Open] ON [Dokuma].[SessionPauses] ([SessionId]) WHERE [EndedAt] IS NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031224742_AddOperatorCardAndPauses'
)
BEGIN
    EXEC(N'UPDATE [Dokuma].[Operators] SET
        [FirstName] = CASE WHEN CHARINDEX('' '', [Name]) > 0 THEN LEFT([Name], CHARINDEX('' '', [Name]) - 1) ELSE LEFT([Name], 100) END,
        [LastName] = CASE WHEN CHARINDEX('' '', [Name]) > 0 THEN LEFT(LTRIM(SUBSTRING([Name], CHARINDEX('' '', [Name]) + 1, 200)), 100) ELSE '''' END')
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031224742_AddOperatorCardAndPauses'
)
BEGIN
    INSERT INTO [Dokuma].[StopReasons] ([Code], [Name], [Category], [SortOrder], [IsActive], [CreatedAt], [IsDeleted])
    SELECT v.[Code], v.[Name], 'PAUSE', v.[SortOrder], 1, SYSUTCDATETIME(), 0 FROM (VALUES
    (N'SU', N'Su', 10),
    (N'TUVALET', N'Tuvalet', 20),
    (N'KISISEL', N'Kişisel ihtiyaç', 30),
    (N'BEKLEME', N'Bekleme', 40),
    (N'DIGER', N'Diğer', 50)
    ) v([Code], [Name], [SortOrder])
    WHERE NOT EXISTS (SELECT 1 FROM [Dokuma].[StopReasons] r WHERE r.[Code] = v.[Code])
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031224742_AddOperatorCardAndPauses'
)
BEGIN
    INSERT INTO [Dokuma].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261031224742_AddOperatorCardAndPauses', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031230233_AddShiftsAndBreaks'
)
BEGIN
    ALTER TABLE [Dokuma].[TabletSessions] DROP CONSTRAINT [CK_TabletSessions_EndReason];
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031230233_AddShiftsAndBreaks'
)
BEGIN
    ALTER TABLE [Dokuma].[SessionPauses] DROP CONSTRAINT [CK_SessionPauses_Kind];
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031230233_AddShiftsAndBreaks'
)
BEGIN
    ALTER TABLE [Dokuma].[SessionPauses] ADD [BreakScheduleId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031230233_AddShiftsAndBreaks'
)
BEGIN
    ALTER TABLE [Dokuma].[Operators] ADD [BreakGroupId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031230233_AddShiftsAndBreaks'
)
BEGIN
    CREATE TABLE [Dokuma].[BreakGroups] (
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
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031230233_AddShiftsAndBreaks'
)
BEGIN
    CREATE TABLE [Dokuma].[Shifts] (
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
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031230233_AddShiftsAndBreaks'
)
BEGIN
    CREATE TABLE [Dokuma].[BreakSchedules] (
        [Id] int NOT NULL IDENTITY,
        [Code] nvarchar(30) NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [ShiftId] int NOT NULL,
        [BreakGroupId] int NULL,
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
        CONSTRAINT [PK_BreakSchedules] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_BreakSchedules_Times] CHECK ([StartTime] <> [EndTime]),
        CONSTRAINT [FK_BreakSchedules_BreakGroups_BreakGroupId] FOREIGN KEY ([BreakGroupId]) REFERENCES [Dokuma].[BreakGroups] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_BreakSchedules_Shifts_ShiftId] FOREIGN KEY ([ShiftId]) REFERENCES [Dokuma].[Shifts] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031230233_AddShiftsAndBreaks'
)
BEGIN
    EXEC(N'ALTER TABLE [Dokuma].[TabletSessions] ADD CONSTRAINT [CK_TabletSessions_EndReason] CHECK ([EndReason] IN (''LOGOUT'', ''SHIFT_ENDED'', ''TAKEN_OVER'', ''MOVED_AWAY'', ''CLOSED_BY_OFFICE''))');
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031230233_AddShiftsAndBreaks'
)
BEGIN
    CREATE INDEX [IX_SessionPauses_BreakScheduleId] ON [Dokuma].[SessionPauses] ([BreakScheduleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031230233_AddShiftsAndBreaks'
)
BEGIN
    EXEC(N'ALTER TABLE [Dokuma].[SessionPauses] ADD CONSTRAINT [CK_SessionPauses_Kind] CHECK ([Kind] IN (''WAITING_FOR_BEAM'', ''BREAK'', ''PAUSE''))');
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031230233_AddShiftsAndBreaks'
)
BEGIN
    CREATE INDEX [IX_Operators_BreakGroupId] ON [Dokuma].[Operators] ([BreakGroupId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031230233_AddShiftsAndBreaks'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_BreakGroups_Code] ON [Dokuma].[BreakGroups] ([Code]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031230233_AddShiftsAndBreaks'
)
BEGIN
    CREATE INDEX [IX_BreakSchedules_BreakGroupId] ON [Dokuma].[BreakSchedules] ([BreakGroupId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031230233_AddShiftsAndBreaks'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_BreakSchedules_Code] ON [Dokuma].[BreakSchedules] ([Code]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031230233_AddShiftsAndBreaks'
)
BEGIN
    CREATE INDEX [IX_BreakSchedules_ShiftId] ON [Dokuma].[BreakSchedules] ([ShiftId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031230233_AddShiftsAndBreaks'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Shifts_Code] ON [Dokuma].[Shifts] ([Code]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031230233_AddShiftsAndBreaks'
)
BEGIN
    ALTER TABLE [Dokuma].[Operators] ADD CONSTRAINT [FK_Operators_BreakGroups_BreakGroupId] FOREIGN KEY ([BreakGroupId]) REFERENCES [Dokuma].[BreakGroups] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031230233_AddShiftsAndBreaks'
)
BEGIN
    ALTER TABLE [Dokuma].[SessionPauses] ADD CONSTRAINT [FK_SessionPauses_BreakSchedules_BreakScheduleId] FOREIGN KEY ([BreakScheduleId]) REFERENCES [Dokuma].[BreakSchedules] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031230233_AddShiftsAndBreaks'
)
BEGIN
    INSERT INTO [Dokuma].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261031230233_AddShiftsAndBreaks', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102090034_AddDocumentsAndCustomerCodes'
)
BEGIN
    ALTER TABLE [Dokuma].[OrderLines] ADD [CustomerArticleCode] nvarchar(50) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102090034_AddDocumentsAndCustomerCodes'
)
BEGIN
    ALTER TABLE [Dokuma].[Customers] ADD [DocumentLanguage] nvarchar(30) NOT NULL DEFAULT N'TURKISH';
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102090034_AddDocumentsAndCustomerCodes'
)
BEGIN
    EXEC(N'ALTER TABLE [Dokuma].[Customers] ADD CONSTRAINT [CK_Customers_DocumentLanguage] CHECK ([DocumentLanguage] IN (''TURKISH'', ''ENGLISH''))');
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102090034_AddDocumentsAndCustomerCodes'
)
BEGIN
    INSERT INTO [Dokuma].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261102090034_AddDocumentsAndCustomerCodes', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102095400_AddSizingMaterialsPurchasing'
)
BEGIN
    ALTER TABLE [Dokuma].[YarnLots] ADD [PurchaseOrderLineId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102095400_AddSizingMaterialsPurchasing'
)
BEGIN
    ALTER TABLE [Dokuma].[Beams] ADD [SizingPickUpPercent] decimal(5,2) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102095400_AddSizingMaterialsPurchasing'
)
BEGIN
    ALTER TABLE [Dokuma].[Beams] ADD [SizingRecipeId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102095400_AddSizingMaterialsPurchasing'
)
BEGIN
    ALTER TABLE [Dokuma].[Beams] ADD [SizingViscositySeconds] decimal(6,1) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102095400_AddSizingMaterialsPurchasing'
)
BEGIN
    CREATE TABLE [Dokuma].[Materials] (
        [Id] int NOT NULL IDENTITY,
        [Code] nvarchar(30) NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [Type] nvarchar(30) NOT NULL,
        [Unit] nvarchar(30) NOT NULL,
        [MinimumStock] decimal(14,3) NULL,
        [PreferredSupplierId] int NULL,
        [Notes] nvarchar(500) NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_Materials] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Materials_MinimumStock] CHECK ([MinimumStock] IS NULL OR [MinimumStock] >= 0),
        CONSTRAINT [CK_Materials_Type] CHECK ([Type] IN ('SIZING_CHEMICAL', 'SPARE_PART', 'CONSUMABLE', 'OTHER')),
        CONSTRAINT [CK_Materials_Unit] CHECK ([Unit] IN ('KILOGRAM', 'PIECE', 'LITER', 'METER')),
        CONSTRAINT [FK_Materials_Suppliers_PreferredSupplierId] FOREIGN KEY ([PreferredSupplierId]) REFERENCES [Dokuma].[Suppliers] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102095400_AddSizingMaterialsPurchasing'
)
BEGIN
    CREATE TABLE [Dokuma].[PurchaseOrders] (
        [Id] int NOT NULL IDENTITY,
        [OrderNo] nvarchar(30) NOT NULL,
        [SupplierId] int NOT NULL,
        [OrderDate] date NOT NULL,
        [DueDate] date NULL,
        [Currency] char(3) NOT NULL DEFAULT 'TRY',
        [Notes] nvarchar(1000) NULL,
        [Status] nvarchar(30) NOT NULL,
        [ApprovedAt] datetime2 NULL,
        [ApprovedBy] nvarchar(100) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_PurchaseOrders] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_PurchaseOrders_Status] CHECK ([Status] IN ('DRAFT', 'APPROVED', 'PARTIALLY_RECEIVED', 'RECEIVED', 'CLOSED', 'CANCELLED')),
        CONSTRAINT [FK_PurchaseOrders_Suppliers_SupplierId] FOREIGN KEY ([SupplierId]) REFERENCES [Dokuma].[Suppliers] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102095400_AddSizingMaterialsPurchasing'
)
BEGIN
    CREATE TABLE [Dokuma].[SizingRecipes] (
        [Id] int NOT NULL IDENTITY,
        [Code] nvarchar(30) NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [ArticleId] int NOT NULL,
        [IsDefault] bit NOT NULL,
        [ConcentrationPercent] decimal(5,2) NULL,
        [BathTemperatureC] decimal(5,1) NULL,
        [ViscositySeconds] decimal(6,1) NULL,
        [MachineSpeedMetersPerMinute] decimal(6,1) NULL,
        [TargetPickUpPercent] decimal(5,2) NULL,
        [Notes] nvarchar(500) NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_SizingRecipes] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_SizingRecipes_ConcentrationPercent] CHECK ([ConcentrationPercent] IS NULL OR [ConcentrationPercent] BETWEEN 0 AND 100),
        CONSTRAINT [CK_SizingRecipes_MachineSpeed] CHECK ([MachineSpeedMetersPerMinute] IS NULL OR [MachineSpeedMetersPerMinute] > 0),
        CONSTRAINT [CK_SizingRecipes_TargetPickUpPercent] CHECK ([TargetPickUpPercent] IS NULL OR [TargetPickUpPercent] BETWEEN 0 AND 500),
        CONSTRAINT [CK_SizingRecipes_ViscositySeconds] CHECK ([ViscositySeconds] IS NULL OR [ViscositySeconds] > 0),
        CONSTRAINT [FK_SizingRecipes_Articles_ArticleId] FOREIGN KEY ([ArticleId]) REFERENCES [Dokuma].[Articles] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102095400_AddSizingMaterialsPurchasing'
)
BEGIN
    CREATE TABLE [Dokuma].[PurchaseOrderLines] (
        [Id] int NOT NULL IDENTITY,
        [OrderId] int NOT NULL,
        [Sequence] int NOT NULL,
        [YarnId] int NULL,
        [MaterialId] int NULL,
        [Quantity] decimal(14,3) NOT NULL,
        [Unit] nvarchar(30) NOT NULL,
        [UnitPrice] decimal(18,4) NULL,
        [DueDate] date NULL,
        [ReceivedQuantity] decimal(14,3) NOT NULL,
        [Notes] nvarchar(500) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_PurchaseOrderLines] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_PurchaseOrderLines_Item] CHECK (([YarnId] IS NOT NULL AND [MaterialId] IS NULL AND [Unit] = 'KILOGRAM') OR ([YarnId] IS NULL AND [MaterialId] IS NOT NULL)),
        CONSTRAINT [CK_PurchaseOrderLines_Quantity] CHECK ([Quantity] > 0),
        CONSTRAINT [CK_PurchaseOrderLines_ReceivedQuantity] CHECK ([ReceivedQuantity] >= 0),
        CONSTRAINT [CK_PurchaseOrderLines_Unit] CHECK ([Unit] IN ('KILOGRAM', 'PIECE', 'LITER', 'METER')),
        CONSTRAINT [CK_PurchaseOrderLines_UnitPrice] CHECK ([UnitPrice] IS NULL OR [UnitPrice] >= 0),
        CONSTRAINT [FK_PurchaseOrderLines_Materials_MaterialId] FOREIGN KEY ([MaterialId]) REFERENCES [Dokuma].[Materials] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PurchaseOrderLines_PurchaseOrders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Dokuma].[PurchaseOrders] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PurchaseOrderLines_Yarns_YarnId] FOREIGN KEY ([YarnId]) REFERENCES [Dokuma].[Yarns] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102095400_AddSizingMaterialsPurchasing'
)
BEGIN
    CREATE TABLE [Dokuma].[SizingRecipeLines] (
        [Id] int NOT NULL IDENTITY,
        [RecipeId] int NOT NULL,
        [Sequence] int NOT NULL,
        [MaterialId] int NOT NULL,
        [BathPercent] decimal(6,3) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_SizingRecipeLines] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_SizingRecipeLines_BathPercent] CHECK ([BathPercent] > 0 AND [BathPercent] <= 100),
        CONSTRAINT [CK_SizingRecipeLines_Sequence] CHECK ([Sequence] >= 1),
        CONSTRAINT [FK_SizingRecipeLines_Materials_MaterialId] FOREIGN KEY ([MaterialId]) REFERENCES [Dokuma].[Materials] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_SizingRecipeLines_SizingRecipes_RecipeId] FOREIGN KEY ([RecipeId]) REFERENCES [Dokuma].[SizingRecipes] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102095400_AddSizingMaterialsPurchasing'
)
BEGIN
    CREATE TABLE [Dokuma].[MaterialLots] (
        [Id] int NOT NULL IDENTITY,
        [LotNo] nvarchar(30) NOT NULL,
        [MaterialId] int NOT NULL,
        [ReceiptDate] date NOT NULL,
        [SupplierId] int NULL,
        [SupplierLotNo] nvarchar(50) NULL,
        [DespatchNo] nvarchar(50) NULL,
        [Quantity] decimal(14,3) NOT NULL,
        [RemainingQuantity] decimal(14,3) NOT NULL,
        [UnitPrice] decimal(18,4) NULL,
        [Currency] char(3) NOT NULL DEFAULT 'TRY',
        [PurchaseOrderLineId] int NULL,
        [Notes] nvarchar(500) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_MaterialLots] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_MaterialLots_Quantity] CHECK ([Quantity] > 0),
        CONSTRAINT [CK_MaterialLots_RemainingQuantity] CHECK ([RemainingQuantity] >= 0),
        CONSTRAINT [CK_MaterialLots_UnitPrice] CHECK ([UnitPrice] IS NULL OR [UnitPrice] >= 0),
        CONSTRAINT [FK_MaterialLots_Materials_MaterialId] FOREIGN KEY ([MaterialId]) REFERENCES [Dokuma].[Materials] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_MaterialLots_PurchaseOrderLines_PurchaseOrderLineId] FOREIGN KEY ([PurchaseOrderLineId]) REFERENCES [Dokuma].[PurchaseOrderLines] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_MaterialLots_Suppliers_SupplierId] FOREIGN KEY ([SupplierId]) REFERENCES [Dokuma].[Suppliers] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102095400_AddSizingMaterialsPurchasing'
)
BEGIN
    CREATE TABLE [Dokuma].[MaterialMovements] (
        [Id] int NOT NULL IDENTITY,
        [MaterialId] int NOT NULL,
        [LotId] int NOT NULL,
        [Type] nvarchar(30) NOT NULL,
        [MovementDate] date NOT NULL,
        [Quantity] decimal(14,3) NOT NULL,
        [LoomId] int NULL,
        [Department] nvarchar(100) NULL,
        [Reason] nvarchar(200) NULL,
        [BeamId] int NULL,
        [Reference] nvarchar(50) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_MaterialMovements] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_MaterialMovements_Quantity] CHECK ([Quantity] <> 0),
        CONSTRAINT [CK_MaterialMovements_Type] CHECK ([Type] IN ('RECEIPT', 'ISSUE', 'SIZING_CONSUMPTION', 'ADJUSTMENT', 'COUNT')),
        CONSTRAINT [FK_MaterialMovements_Beams_BeamId] FOREIGN KEY ([BeamId]) REFERENCES [Dokuma].[Beams] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_MaterialMovements_Looms_LoomId] FOREIGN KEY ([LoomId]) REFERENCES [Dokuma].[Looms] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_MaterialMovements_MaterialLots_LotId] FOREIGN KEY ([LotId]) REFERENCES [Dokuma].[MaterialLots] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_MaterialMovements_Materials_MaterialId] FOREIGN KEY ([MaterialId]) REFERENCES [Dokuma].[Materials] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102095400_AddSizingMaterialsPurchasing'
)
BEGIN
    CREATE INDEX [IX_YarnLots_PurchaseOrderLineId] ON [Dokuma].[YarnLots] ([PurchaseOrderLineId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102095400_AddSizingMaterialsPurchasing'
)
BEGIN
    CREATE INDEX [IX_Beams_SizingRecipeId] ON [Dokuma].[Beams] ([SizingRecipeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102095400_AddSizingMaterialsPurchasing'
)
BEGIN
    EXEC(N'ALTER TABLE [Dokuma].[Beams] ADD CONSTRAINT [CK_Beams_SizingPickUpPercent] CHECK ([SizingPickUpPercent] IS NULL OR [SizingPickUpPercent] BETWEEN 0 AND 500)');
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102095400_AddSizingMaterialsPurchasing'
)
BEGIN
    EXEC(N'ALTER TABLE [Dokuma].[Beams] ADD CONSTRAINT [CK_Beams_SizingViscositySeconds] CHECK ([SizingViscositySeconds] IS NULL OR [SizingViscositySeconds] > 0)');
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102095400_AddSizingMaterialsPurchasing'
)
BEGIN
    CREATE UNIQUE INDEX [IX_MaterialLots_LotNo] ON [Dokuma].[MaterialLots] ([LotNo]);
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102095400_AddSizingMaterialsPurchasing'
)
BEGIN
    CREATE INDEX [IX_MaterialLots_MaterialId_ReceiptDate] ON [Dokuma].[MaterialLots] ([MaterialId], [ReceiptDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102095400_AddSizingMaterialsPurchasing'
)
BEGIN
    CREATE INDEX [IX_MaterialLots_PurchaseOrderLineId] ON [Dokuma].[MaterialLots] ([PurchaseOrderLineId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102095400_AddSizingMaterialsPurchasing'
)
BEGIN
    CREATE INDEX [IX_MaterialLots_SupplierId] ON [Dokuma].[MaterialLots] ([SupplierId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102095400_AddSizingMaterialsPurchasing'
)
BEGIN
    EXEC(N'CREATE INDEX [IX_MaterialMovements_BeamId] ON [Dokuma].[MaterialMovements] ([BeamId]) WHERE [BeamId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102095400_AddSizingMaterialsPurchasing'
)
BEGIN
    CREATE INDEX [IX_MaterialMovements_LoomId] ON [Dokuma].[MaterialMovements] ([LoomId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102095400_AddSizingMaterialsPurchasing'
)
BEGIN
    CREATE INDEX [IX_MaterialMovements_LotId] ON [Dokuma].[MaterialMovements] ([LotId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102095400_AddSizingMaterialsPurchasing'
)
BEGIN
    CREATE INDEX [IX_MaterialMovements_MaterialId_MovementDate] ON [Dokuma].[MaterialMovements] ([MaterialId], [MovementDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102095400_AddSizingMaterialsPurchasing'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Materials_Code] ON [Dokuma].[Materials] ([Code]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102095400_AddSizingMaterialsPurchasing'
)
BEGIN
    CREATE INDEX [IX_Materials_PreferredSupplierId] ON [Dokuma].[Materials] ([PreferredSupplierId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102095400_AddSizingMaterialsPurchasing'
)
BEGIN
    CREATE INDEX [IX_PurchaseOrderLines_MaterialId] ON [Dokuma].[PurchaseOrderLines] ([MaterialId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102095400_AddSizingMaterialsPurchasing'
)
BEGIN
    CREATE UNIQUE INDEX [IX_PurchaseOrderLines_OrderId_Sequence] ON [Dokuma].[PurchaseOrderLines] ([OrderId], [Sequence]);
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102095400_AddSizingMaterialsPurchasing'
)
BEGIN
    CREATE INDEX [IX_PurchaseOrderLines_YarnId] ON [Dokuma].[PurchaseOrderLines] ([YarnId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102095400_AddSizingMaterialsPurchasing'
)
BEGIN
    CREATE INDEX [IX_PurchaseOrders_OrderDate] ON [Dokuma].[PurchaseOrders] ([OrderDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102095400_AddSizingMaterialsPurchasing'
)
BEGIN
    CREATE UNIQUE INDEX [IX_PurchaseOrders_OrderNo] ON [Dokuma].[PurchaseOrders] ([OrderNo]);
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102095400_AddSizingMaterialsPurchasing'
)
BEGIN
    CREATE INDEX [IX_PurchaseOrders_Status] ON [Dokuma].[PurchaseOrders] ([Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102095400_AddSizingMaterialsPurchasing'
)
BEGIN
    CREATE INDEX [IX_PurchaseOrders_SupplierId] ON [Dokuma].[PurchaseOrders] ([SupplierId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102095400_AddSizingMaterialsPurchasing'
)
BEGIN
    CREATE INDEX [IX_SizingRecipeLines_MaterialId] ON [Dokuma].[SizingRecipeLines] ([MaterialId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102095400_AddSizingMaterialsPurchasing'
)
BEGIN
    CREATE UNIQUE INDEX [IX_SizingRecipeLines_RecipeId_Sequence] ON [Dokuma].[SizingRecipeLines] ([RecipeId], [Sequence]);
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102095400_AddSizingMaterialsPurchasing'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_SizingRecipes_Code] ON [Dokuma].[SizingRecipes] ([Code]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102095400_AddSizingMaterialsPurchasing'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_SizingRecipes_ArticleDefault] ON [Dokuma].[SizingRecipes] ([ArticleId]) WHERE [IsDefault] = 1 AND [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102095400_AddSizingMaterialsPurchasing'
)
BEGIN
    ALTER TABLE [Dokuma].[Beams] ADD CONSTRAINT [FK_Beams_SizingRecipes_SizingRecipeId] FOREIGN KEY ([SizingRecipeId]) REFERENCES [Dokuma].[SizingRecipes] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102095400_AddSizingMaterialsPurchasing'
)
BEGIN
    ALTER TABLE [Dokuma].[YarnLots] ADD CONSTRAINT [FK_YarnLots_PurchaseOrderLines_PurchaseOrderLineId] FOREIGN KEY ([PurchaseOrderLineId]) REFERENCES [Dokuma].[PurchaseOrderLines] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102095400_AddSizingMaterialsPurchasing'
)
BEGIN
    SET IDENTITY_INSERT [Core].[Permissions] ON;
    EXEC(N'INSERT INTO [Core].[Permissions] ([Id], [Code], [Module], [Description])
    SELECT v.[Id], v.[Code], v.[Module], v.[Description] FROM (VALUES
    (2032, N''Dokuma.PurchaseOrder.View'', N''Dokuma'', N''Satın alma siparişlerini ve tedarik önerilerini görüntüleme''),
    (2033, N''Dokuma.PurchaseOrder.Manage'', N''Dokuma'', N''Satın alma siparişi hazırlama, onaylama, teslim alma ve kapatma''),
    (2034, N''Dokuma.Stock.View'', N''Dokuma'', N''Malzeme stoğunu, lotlarını ve hareketlerini görüntüleme''),
    (2035, N''Dokuma.Stock.Manage'', N''Dokuma'', N''Malzeme girişi, tezgâha / bölüme çıkış, sayım ve düzeltme'')
    ) v([Id], [Code], [Module], [Description])
    WHERE NOT EXISTS (SELECT 1 FROM [Core].[Permissions] p WHERE p.[Id] = v.[Id] OR p.[Code] = v.[Code])');
    SET IDENTITY_INSERT [Core].[Permissions] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102095400_AddSizingMaterialsPurchasing'
)
BEGIN
    INSERT INTO [Core].[RolePermissions] ([RoleId], [PermissionId])
    SELECT v.[RoleId], v.[PermissionId] FROM (VALUES
    (1, 2032), (1, 2033), (1, 2034), (1, 2035)
    ) v([RoleId], [PermissionId])
    WHERE EXISTS (SELECT 1 FROM [Core].[Roles] r WHERE r.[Id] = v.[RoleId])
      AND EXISTS (SELECT 1 FROM [Core].[Permissions] p WHERE p.[Id] = v.[PermissionId])
      AND NOT EXISTS (SELECT 1 FROM [Core].[RolePermissions] rp WHERE rp.[RoleId] = v.[RoleId] AND rp.[PermissionId] = v.[PermissionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102095400_AddSizingMaterialsPurchasing'
)
BEGIN
    INSERT INTO [Dokuma].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261102095400_AddSizingMaterialsPurchasing', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102104750_AddQuotationsAndCapacityPlan'
)
BEGIN
    ALTER TABLE [Dokuma].[Beams] ADD [PlanSequence] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102104750_AddQuotationsAndCapacityPlan'
)
BEGIN
    ALTER TABLE [Dokuma].[Beams] ADD [PlannedLoomId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102104750_AddQuotationsAndCapacityPlan'
)
BEGIN
    CREATE TABLE [Dokuma].[CostSettings] (
        [Id] int NOT NULL IDENTITY,
        [Currency] char(3) NOT NULL DEFAULT 'TRY',
        [LoomHourlyCost] decimal(18,4) NOT NULL,
        [LaborHourlyCost] decimal(18,4) NOT NULL,
        [WarpingCostPerMeter] decimal(18,4) NOT NULL,
        [SizingCostPerMeter] decimal(18,4) NOT NULL,
        [UseSizingRecipe] bit NOT NULL,
        [OverheadPercent] decimal(6,2) NOT NULL,
        [DefaultWastePercent] decimal(5,2) NOT NULL,
        [DefaultMarginPercent] decimal(6,2) NOT NULL,
        [DefaultEfficiencyPercent] decimal(5,2) NOT NULL DEFAULT 85.0,
        [QuotationValidityDays] int NOT NULL DEFAULT 15,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_CostSettings] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_CostSettings_Amounts] CHECK ([LoomHourlyCost] >= 0 AND [LaborHourlyCost] >= 0 AND [WarpingCostPerMeter] >= 0 AND [SizingCostPerMeter] >= 0),
        CONSTRAINT [CK_CostSettings_Efficiency] CHECK ([DefaultEfficiencyPercent] > 0 AND [DefaultEfficiencyPercent] <= 100),
        CONSTRAINT [CK_CostSettings_Percents] CHECK ([OverheadPercent] BETWEEN 0 AND 500 AND [DefaultWastePercent] BETWEEN 0 AND 100 AND [DefaultMarginPercent] BETWEEN 0 AND 500),
        CONSTRAINT [CK_CostSettings_ValidityDays] CHECK ([QuotationValidityDays] BETWEEN 1 AND 365)
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102104750_AddQuotationsAndCapacityPlan'
)
BEGIN
    CREATE TABLE [Dokuma].[LoomCostRates] (
        [Id] int NOT NULL IDENTITY,
        [LoomId] int NOT NULL,
        [HourlyCost] decimal(18,4) NULL,
        [EfficiencyPercent] decimal(5,2) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_LoomCostRates] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_LoomCostRates_Efficiency] CHECK ([EfficiencyPercent] IS NULL OR ([EfficiencyPercent] > 0 AND [EfficiencyPercent] <= 100)),
        CONSTRAINT [CK_LoomCostRates_HourlyCost] CHECK ([HourlyCost] IS NULL OR [HourlyCost] >= 0),
        CONSTRAINT [FK_LoomCostRates_Looms_LoomId] FOREIGN KEY ([LoomId]) REFERENCES [Dokuma].[Looms] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102104750_AddQuotationsAndCapacityPlan'
)
BEGIN
    CREATE TABLE [Dokuma].[Quotations] (
        [Id] int NOT NULL IDENTITY,
        [QuotationNo] nvarchar(30) NOT NULL,
        [QuotationDate] date NOT NULL,
        [ValidUntil] date NOT NULL,
        [CustomerId] int NULL,
        [ProspectName] nvarchar(200) NULL,
        [ContactName] nvarchar(100) NULL,
        [Ownership] nvarchar(30) NOT NULL,
        [Currency] char(3) NOT NULL DEFAULT 'TRY',
        [DocumentLanguage] nvarchar(30) NOT NULL,
        [PaymentTermDays] int NULL,
        [DeliveryTerms] nvarchar(200) NULL,
        [Notes] nvarchar(1000) NULL,
        [Status] nvarchar(30) NOT NULL,
        [SentAt] datetime2 NULL,
        [DecidedAt] datetime2 NULL,
        [DecidedBy] nvarchar(100) NULL,
        [RejectionReason] nvarchar(500) NULL,
        [OrderId] int NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_Quotations] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Quotations_DocumentLanguage] CHECK ([DocumentLanguage] IN ('TURKISH', 'ENGLISH')),
        CONSTRAINT [CK_Quotations_Ownership] CHECK ([Ownership] IN ('COMMISSION', 'OWN_GOODS')),
        CONSTRAINT [CK_Quotations_Party] CHECK ([CustomerId] IS NOT NULL OR [ProspectName] IS NOT NULL),
        CONSTRAINT [CK_Quotations_PaymentTermDays] CHECK ([PaymentTermDays] IS NULL OR [PaymentTermDays] BETWEEN 0 AND 365),
        CONSTRAINT [CK_Quotations_Status] CHECK ([Status] IN ('DRAFT', 'SENT', 'ACCEPTED', 'REJECTED', 'EXPIRED')),
        CONSTRAINT [CK_Quotations_ValidUntil] CHECK ([ValidUntil] >= [QuotationDate]),
        CONSTRAINT [FK_Quotations_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Dokuma].[Customers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Quotations_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Dokuma].[Orders] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102104750_AddQuotationsAndCapacityPlan'
)
BEGIN
    CREATE TABLE [Dokuma].[QuotationLines] (
        [Id] int NOT NULL IDENTITY,
        [QuotationId] int NOT NULL,
        [Sequence] int NOT NULL,
        [ArticleId] int NOT NULL,
        [Meters] decimal(12,2) NOT NULL,
        [CustomerArticleCode] nvarchar(50) NULL,
        [LoomId] int NULL,
        [SpeedRpm] decimal(8,2) NOT NULL,
        [EfficiencyPercent] decimal(5,2) NOT NULL,
        [PicksPerCm] decimal(8,2) NOT NULL,
        [WarpKilogramsPerMeter] decimal(12,6) NOT NULL,
        [WeftKilogramsPerMeter] decimal(12,6) NOT NULL,
        [YarnCost] decimal(18,4) NOT NULL,
        [SizingCostPerWarpMeter] decimal(18,4) NOT NULL,
        [WarpingCostPerWarpMeter] decimal(18,4) NOT NULL,
        [SizingCost] decimal(18,4) NOT NULL,
        [WarpingCost] decimal(18,4) NOT NULL,
        [LoomHourlyCost] decimal(18,4) NOT NULL,
        [LaborHourlyCost] decimal(18,4) NOT NULL,
        [WeavingHoursPerMeter] decimal(12,6) NOT NULL,
        [WeavingCost] decimal(18,4) NOT NULL,
        [WastePercent] decimal(5,2) NOT NULL,
        [OverheadPercent] decimal(6,2) NOT NULL,
        [MarginPercent] decimal(6,2) NOT NULL,
        [CostPerMeter] decimal(18,4) NOT NULL,
        [CalculatedPrice] decimal(18,4) NOT NULL,
        [UnitPrice] decimal(18,4) NOT NULL,
        [Notes] nvarchar(500) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_QuotationLines] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_QuotationLines_Meters] CHECK ([Meters] > 0),
        CONSTRAINT [CK_QuotationLines_Rates] CHECK ([SpeedRpm] > 0 AND [EfficiencyPercent] > 0 AND [EfficiencyPercent] <= 100 AND [PicksPerCm] > 0),
        CONSTRAINT [CK_QuotationLines_UnitPrice] CHECK ([UnitPrice] >= 0),
        CONSTRAINT [FK_QuotationLines_Articles_ArticleId] FOREIGN KEY ([ArticleId]) REFERENCES [Dokuma].[Articles] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_QuotationLines_Looms_LoomId] FOREIGN KEY ([LoomId]) REFERENCES [Dokuma].[Looms] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_QuotationLines_Quotations_QuotationId] FOREIGN KEY ([QuotationId]) REFERENCES [Dokuma].[Quotations] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102104750_AddQuotationsAndCapacityPlan'
)
BEGIN
    CREATE TABLE [Dokuma].[QuotationLineYarns] (
        [Id] int NOT NULL IDENTITY,
        [LineId] int NOT NULL,
        [YarnId] int NOT NULL,
        [UsedInWarp] bit NOT NULL,
        [UsedInWeft] bit NOT NULL,
        [KilogramsPerMeter] decimal(12,6) NOT NULL,
        [UnitPrice] decimal(18,4) NOT NULL,
        [PriceSource] nvarchar(100) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_QuotationLineYarns] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_QuotationLineYarns_Kilograms] CHECK ([KilogramsPerMeter] >= 0),
        CONSTRAINT [CK_QuotationLineYarns_UnitPrice] CHECK ([UnitPrice] >= 0),
        CONSTRAINT [FK_QuotationLineYarns_QuotationLines_LineId] FOREIGN KEY ([LineId]) REFERENCES [Dokuma].[QuotationLines] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_QuotationLineYarns_Yarns_YarnId] FOREIGN KEY ([YarnId]) REFERENCES [Dokuma].[Yarns] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102104750_AddQuotationsAndCapacityPlan'
)
BEGIN
    CREATE INDEX [IX_Beams_PlannedLoomId_PlanSequence] ON [Dokuma].[Beams] ([PlannedLoomId], [PlanSequence]);
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102104750_AddQuotationsAndCapacityPlan'
)
BEGIN
    EXEC(N'ALTER TABLE [Dokuma].[Beams] ADD CONSTRAINT [CK_Beams_PlanSequence] CHECK ([PlanSequence] IS NULL OR [PlanSequence] >= 1)');
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102104750_AddQuotationsAndCapacityPlan'
)
BEGIN
    CREATE UNIQUE INDEX [IX_LoomCostRates_LoomId] ON [Dokuma].[LoomCostRates] ([LoomId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102104750_AddQuotationsAndCapacityPlan'
)
BEGIN
    CREATE INDEX [IX_QuotationLines_ArticleId] ON [Dokuma].[QuotationLines] ([ArticleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102104750_AddQuotationsAndCapacityPlan'
)
BEGIN
    CREATE INDEX [IX_QuotationLines_LoomId] ON [Dokuma].[QuotationLines] ([LoomId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102104750_AddQuotationsAndCapacityPlan'
)
BEGIN
    CREATE UNIQUE INDEX [IX_QuotationLines_QuotationId_Sequence] ON [Dokuma].[QuotationLines] ([QuotationId], [Sequence]);
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102104750_AddQuotationsAndCapacityPlan'
)
BEGIN
    CREATE UNIQUE INDEX [IX_QuotationLineYarns_LineId_YarnId] ON [Dokuma].[QuotationLineYarns] ([LineId], [YarnId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102104750_AddQuotationsAndCapacityPlan'
)
BEGIN
    CREATE INDEX [IX_QuotationLineYarns_YarnId] ON [Dokuma].[QuotationLineYarns] ([YarnId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102104750_AddQuotationsAndCapacityPlan'
)
BEGIN
    CREATE INDEX [IX_Quotations_CustomerId] ON [Dokuma].[Quotations] ([CustomerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102104750_AddQuotationsAndCapacityPlan'
)
BEGIN
    CREATE INDEX [IX_Quotations_OrderId] ON [Dokuma].[Quotations] ([OrderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102104750_AddQuotationsAndCapacityPlan'
)
BEGIN
    CREATE INDEX [IX_Quotations_QuotationDate] ON [Dokuma].[Quotations] ([QuotationDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102104750_AddQuotationsAndCapacityPlan'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Quotations_QuotationNo] ON [Dokuma].[Quotations] ([QuotationNo]);
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102104750_AddQuotationsAndCapacityPlan'
)
BEGIN
    CREATE INDEX [IX_Quotations_Status] ON [Dokuma].[Quotations] ([Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102104750_AddQuotationsAndCapacityPlan'
)
BEGIN
    ALTER TABLE [Dokuma].[Beams] ADD CONSTRAINT [FK_Beams_Looms_PlannedLoomId] FOREIGN KEY ([PlannedLoomId]) REFERENCES [Dokuma].[Looms] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102104750_AddQuotationsAndCapacityPlan'
)
BEGIN
    SET IDENTITY_INSERT [Core].[Permissions] ON;
    EXEC(N'INSERT INTO [Core].[Permissions] ([Id], [Code], [Module], [Description])
    SELECT v.[Id], v.[Code], v.[Module], v.[Description] FROM (VALUES
    (2036, N''Dokuma.Quotation.View'', N''Dokuma'', N''Teklifleri, ön maliyet dökümünü ve maliyet parametrelerini görüntüleme''),
    (2037, N''Dokuma.Quotation.Manage'', N''Dokuma'', N''Teklif hazırlama, gönderme, kabul / ret ve maliyet parametrelerini düzenleme''),
    (2038, N''Dokuma.Planning.View'', N''Dokuma'', N''Tezgâh kapasite planını görüntüleme''),
    (2039, N''Dokuma.Planning.Manage'', N''Dokuma'', N''Leventleri tezgâh kuyruğuna alma, sıralama ve başka tezgâha taşıma'')
    ) v([Id], [Code], [Module], [Description])
    WHERE NOT EXISTS (SELECT 1 FROM [Core].[Permissions] p WHERE p.[Id] = v.[Id] OR p.[Code] = v.[Code])');
    SET IDENTITY_INSERT [Core].[Permissions] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102104750_AddQuotationsAndCapacityPlan'
)
BEGIN
    INSERT INTO [Core].[RolePermissions] ([RoleId], [PermissionId])
    SELECT v.[RoleId], v.[PermissionId] FROM (VALUES
    (1, 2036), (1, 2037), (1, 2038), (1, 2039)
    ) v([RoleId], [PermissionId])
    WHERE EXISTS (SELECT 1 FROM [Core].[Roles] r WHERE r.[Id] = v.[RoleId])
      AND EXISTS (SELECT 1 FROM [Core].[Permissions] p WHERE p.[Id] = v.[PermissionId])
      AND NOT EXISTS (SELECT 1 FROM [Core].[RolePermissions] rp WHERE rp.[RoleId] = v.[RoleId] AND rp.[PermissionId] = v.[PermissionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Dokuma].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102104750_AddQuotationsAndCapacityPlan'
)
BEGIN
    INSERT INTO [Dokuma].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261102104750_AddQuotationsAndCapacityPlan', N'10.0.12');
END;

COMMIT;
GO

