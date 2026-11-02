/*
    Tekstil ERP - Boyahane modülü kurulum / güncelleme script'i
    Bu dosya otomatik üretilir (database\generate-script.ps1); elle düzenlemeyin.

    Hedef veritabanına (örn. TekstilERP) bağlanıp script'in tamamını çalıştırın. Script tekrar
    çalıştırılabilir: daha önce uygulanmış adımlar atlanır. Sıra: önce 00_Core.sql, sonra modüller.

    sqlcmd ile:  sqlcmd -S "(localdb)\MSSQLLocalDB" -d TekstilERP -E -b -f 65001 -i 20_Boyahane.sql
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
IF OBJECT_ID(N'[Boyahane].[__EFMigrationsHistory]') IS NULL
BEGIN
    IF SCHEMA_ID(N'Boyahane') IS NULL EXEC(N'CREATE SCHEMA [Boyahane];');
    CREATE TABLE [Boyahane].[__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030070349_InitialBoyahane'
)
BEGIN
    IF SCHEMA_ID(N'Boyahane') IS NULL EXEC(N'CREATE SCHEMA [Boyahane];');
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030070349_InitialBoyahane'
)
BEGIN
    SET IDENTITY_INSERT [Core].[Permissions] ON;
    EXEC(N'INSERT INTO [Core].[Permissions] ([Id], [Code], [Module], [Description])
    SELECT v.[Id], v.[Code], v.[Module], v.[Description] FROM (VALUES
    (1001, N''Boyahane.View'', N''Boyahane'', N''Boyahane ekranlarını görüntüleme'')
    ) v([Id], [Code], [Module], [Description])
    WHERE NOT EXISTS (SELECT 1 FROM [Core].[Permissions] p WHERE p.[Id] = v.[Id] OR p.[Code] = v.[Code])');
    SET IDENTITY_INSERT [Core].[Permissions] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030070349_InitialBoyahane'
)
BEGIN
    INSERT INTO [Core].[RolePermissions] ([RoleId], [PermissionId])
    SELECT v.[RoleId], v.[PermissionId] FROM (VALUES
    (1, 1001)
    ) v([RoleId], [PermissionId])
    WHERE EXISTS (SELECT 1 FROM [Core].[Roles] r WHERE r.[Id] = v.[RoleId])
      AND EXISTS (SELECT 1 FROM [Core].[Permissions] p WHERE p.[Id] = v.[PermissionId])
      AND NOT EXISTS (SELECT 1 FROM [Core].[RolePermissions] rp WHERE rp.[RoleId] = v.[RoleId] AND rp.[PermissionId] = v.[PermissionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030070349_InitialBoyahane'
)
BEGIN
    INSERT INTO [Boyahane].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261030070349_InitialBoyahane', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030095422_AddCustomers'
)
BEGIN
    IF SCHEMA_ID(N'Boyahane') IS NULL EXEC(N'CREATE SCHEMA [Boyahane];');
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030095422_AddCustomers'
)
BEGIN
    CREATE TABLE [Boyahane].[Customers] (
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
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030095422_AddCustomers'
)
BEGIN
    CREATE TABLE [Boyahane].[CustomerAddresses] (
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
        CONSTRAINT [FK_CustomerAddresses_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Boyahane].[Customers] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030095422_AddCustomers'
)
BEGIN
    CREATE TABLE [Boyahane].[CustomerContacts] (
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
        CONSTRAINT [FK_CustomerContacts_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Boyahane].[Customers] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030095422_AddCustomers'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_CustomerAddresses_DefaultDelivery] ON [Boyahane].[CustomerAddresses] ([CustomerId]) WHERE [IsDefaultDelivery] = 1');
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030095422_AddCustomers'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_CustomerAddresses_DefaultInvoice] ON [Boyahane].[CustomerAddresses] ([CustomerId]) WHERE [IsDefaultInvoice] = 1');
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030095422_AddCustomers'
)
BEGIN
    CREATE INDEX [IX_CustomerContacts_CustomerId] ON [Boyahane].[CustomerContacts] ([CustomerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030095422_AddCustomers'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Customers_CustomerCode] ON [Boyahane].[Customers] ([CustomerCode]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030095422_AddCustomers'
)
BEGIN
    CREATE INDEX [IX_Customers_TaxNumber] ON [Boyahane].[Customers] ([TaxNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030095422_AddCustomers'
)
BEGIN
    SET IDENTITY_INSERT [Core].[Permissions] ON;
    EXEC(N'INSERT INTO [Core].[Permissions] ([Id], [Code], [Module], [Description])
    SELECT v.[Id], v.[Code], v.[Module], v.[Description] FROM (VALUES
    (1002, N''Boyahane.Customer.View'', N''Boyahane'', N''Boyahane müşterilerini görüntüleme''),
    (1003, N''Boyahane.Customer.Manage'', N''Boyahane'', N''Boyahane müşterilerini oluşturma ve düzenleme'')
    ) v([Id], [Code], [Module], [Description])
    WHERE NOT EXISTS (SELECT 1 FROM [Core].[Permissions] p WHERE p.[Id] = v.[Id] OR p.[Code] = v.[Code])');
    SET IDENTITY_INSERT [Core].[Permissions] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030095422_AddCustomers'
)
BEGIN
    INSERT INTO [Core].[RolePermissions] ([RoleId], [PermissionId])
    SELECT v.[RoleId], v.[PermissionId] FROM (VALUES
    (1, 1002),
    (1, 1003)
    ) v([RoleId], [PermissionId])
    WHERE EXISTS (SELECT 1 FROM [Core].[Roles] r WHERE r.[Id] = v.[RoleId])
      AND EXISTS (SELECT 1 FROM [Core].[Permissions] p WHERE p.[Id] = v.[PermissionId])
      AND NOT EXISTS (SELECT 1 FROM [Core].[RolePermissions] rp WHERE rp.[RoleId] = v.[RoleId] AND rp.[PermissionId] = v.[PermissionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030095422_AddCustomers'
)
BEGIN
    INSERT INTO [Boyahane].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261030095422_AddCustomers', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030100529_AddMaterialCards'
)
BEGIN
    CREATE TABLE [Boyahane].[Fabrics] (
        [Id] int NOT NULL IDENTITY,
        [FabricCode] nvarchar(30) NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [FabricType] nvarchar(30) NOT NULL,
        [Structure] nvarchar(100) NULL,
        [Composition] nvarchar(200) NULL,
        [RawWidthCm] decimal(6,2) NOT NULL,
        [RawWeightGsm] int NOT NULL,
        [FinishedWidthCm] decimal(6,2) NULL,
        [FinishedWeightGsm] int NULL,
        [ShrinkageTolerancePercent] decimal(5,2) NULL,
        [DefaultUnit] nvarchar(30) NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_Fabrics] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Fabrics_DefaultUnit] CHECK ([DefaultUnit] IN ('METER', 'KILOGRAM')),
        CONSTRAINT [CK_Fabrics_FabricType] CHECK ([FabricType] IN ('KNIT', 'WOVEN')),
        CONSTRAINT [CK_Fabrics_RawWeightGsm] CHECK ([RawWeightGsm] > 0),
        CONSTRAINT [CK_Fabrics_RawWidthCm] CHECK ([RawWidthCm] > 0),
        CONSTRAINT [CK_Fabrics_ShrinkageTolerancePercent] CHECK ([ShrinkageTolerancePercent] IS NULL OR [ShrinkageTolerancePercent] BETWEEN 0 AND 100)
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030100529_AddMaterialCards'
)
BEGIN
    CREATE TABLE [Boyahane].[Yarns] (
        [Id] int NOT NULL IDENTITY,
        [YarnCode] nvarchar(30) NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [CountValue] decimal(8,2) NOT NULL,
        [CountSystem] nvarchar(30) NOT NULL,
        [Plies] int NOT NULL,
        [Composition] nvarchar(200) NULL,
        [Twist] nvarchar(50) NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_Yarns] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Yarns_CountSystem] CHECK ([CountSystem] IN ('NE', 'NM', 'DTEX')),
        CONSTRAINT [CK_Yarns_CountValue] CHECK ([CountValue] > 0),
        CONSTRAINT [CK_Yarns_Plies] CHECK ([Plies] >= 1)
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030100529_AddMaterialCards'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Fabrics_FabricCode] ON [Boyahane].[Fabrics] ([FabricCode]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030100529_AddMaterialCards'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Yarns_YarnCode] ON [Boyahane].[Yarns] ([YarnCode]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030100529_AddMaterialCards'
)
BEGIN
    SET IDENTITY_INSERT [Core].[Permissions] ON;
    EXEC(N'INSERT INTO [Core].[Permissions] ([Id], [Code], [Module], [Description])
    SELECT v.[Id], v.[Code], v.[Module], v.[Description] FROM (VALUES
    (1004, N''Boyahane.Material.View'', N''Boyahane'', N''Kumaş ve iplik kartlarını görüntüleme''),
    (1005, N''Boyahane.Material.Manage'', N''Boyahane'', N''Kumaş ve iplik kartlarını oluşturma ve düzenleme'')
    ) v([Id], [Code], [Module], [Description])
    WHERE NOT EXISTS (SELECT 1 FROM [Core].[Permissions] p WHERE p.[Id] = v.[Id] OR p.[Code] = v.[Code])');
    SET IDENTITY_INSERT [Core].[Permissions] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030100529_AddMaterialCards'
)
BEGIN
    INSERT INTO [Core].[RolePermissions] ([RoleId], [PermissionId])
    SELECT v.[RoleId], v.[PermissionId] FROM (VALUES
    (1, 1004),
    (1, 1005)
    ) v([RoleId], [PermissionId])
    WHERE EXISTS (SELECT 1 FROM [Core].[Roles] r WHERE r.[Id] = v.[RoleId])
      AND EXISTS (SELECT 1 FROM [Core].[Permissions] p WHERE p.[Id] = v.[PermissionId])
      AND NOT EXISTS (SELECT 1 FROM [Core].[RolePermissions] rp WHERE rp.[RoleId] = v.[RoleId] AND rp.[PermissionId] = v.[PermissionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030100529_AddMaterialCards'
)
BEGIN
    INSERT INTO [Boyahane].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261030100529_AddMaterialCards', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030101846_AddFabricReceipts'
)
BEGIN
    ALTER TABLE [Boyahane].[Customers] ADD [RequiresRawInspection] bit NOT NULL DEFAULT CAST(1 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030101846_AddFabricReceipts'
)
BEGIN
    CREATE TABLE [Boyahane].[FabricReceipts] (
        [Id] int NOT NULL IDENTITY,
        [ReceiptNo] nvarchar(30) NOT NULL,
        [ReceiptDate] date NOT NULL,
        [CustomerId] int NULL,
        [DespatchNo] nvarchar(50) NULL,
        [Notes] nvarchar(1000) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_FabricReceipts] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_FabricReceipts_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Boyahane].[Customers] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030101846_AddFabricReceipts'
)
BEGIN
    CREATE TABLE [Boyahane].[FabricLots] (
        [Id] int NOT NULL IDENTITY,
        [ReceiptId] int NOT NULL,
        [LotNo] nvarchar(40) NOT NULL,
        [FabricId] int NOT NULL,
        [CustomerFabricCode] nvarchar(100) NULL,
        [DeclaredRollCount] int NULL,
        [DeclaredMeters] decimal(12,2) NULL,
        [DeclaredKilograms] decimal(12,2) NULL,
        [InspectionRequired] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_FabricLots] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_FabricLots_FabricReceipts_ReceiptId] FOREIGN KEY ([ReceiptId]) REFERENCES [Boyahane].[FabricReceipts] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_FabricLots_Fabrics_FabricId] FOREIGN KEY ([FabricId]) REFERENCES [Boyahane].[Fabrics] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030101846_AddFabricReceipts'
)
BEGIN
    CREATE TABLE [Boyahane].[FabricRolls] (
        [Id] int NOT NULL IDENTITY,
        [LotId] int NOT NULL,
        [RollNo] nvarchar(30) NOT NULL,
        [CustomerRollNo] nvarchar(50) NULL,
        [Meters] decimal(12,2) NOT NULL,
        [Kilograms] decimal(12,2) NOT NULL,
        [MetersMeasured] bit NOT NULL,
        [KilogramsMeasured] bit NOT NULL,
        [Status] nvarchar(30) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_FabricRolls] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_FabricRolls_Kilograms] CHECK ([Kilograms] > 0),
        CONSTRAINT [CK_FabricRolls_Meters] CHECK ([Meters] > 0),
        CONSTRAINT [CK_FabricRolls_Status] CHECK ([Status] IN ('PENDING_INSPECTION', 'APPROVED', 'CONDITIONAL', 'REJECTED')),
        CONSTRAINT [FK_FabricRolls_FabricLots_LotId] FOREIGN KEY ([LotId]) REFERENCES [Boyahane].[FabricLots] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030101846_AddFabricReceipts'
)
BEGIN
    CREATE INDEX [IX_FabricLots_FabricId] ON [Boyahane].[FabricLots] ([FabricId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030101846_AddFabricReceipts'
)
BEGIN
    CREATE UNIQUE INDEX [IX_FabricLots_LotNo] ON [Boyahane].[FabricLots] ([LotNo]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030101846_AddFabricReceipts'
)
BEGIN
    CREATE INDEX [IX_FabricLots_ReceiptId] ON [Boyahane].[FabricLots] ([ReceiptId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030101846_AddFabricReceipts'
)
BEGIN
    CREATE INDEX [IX_FabricReceipts_CustomerId] ON [Boyahane].[FabricReceipts] ([CustomerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030101846_AddFabricReceipts'
)
BEGIN
    CREATE INDEX [IX_FabricReceipts_ReceiptDate] ON [Boyahane].[FabricReceipts] ([ReceiptDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030101846_AddFabricReceipts'
)
BEGIN
    CREATE UNIQUE INDEX [IX_FabricReceipts_ReceiptNo] ON [Boyahane].[FabricReceipts] ([ReceiptNo]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030101846_AddFabricReceipts'
)
BEGIN
    CREATE INDEX [IX_FabricRolls_LotId] ON [Boyahane].[FabricRolls] ([LotId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030101846_AddFabricReceipts'
)
BEGIN
    CREATE UNIQUE INDEX [IX_FabricRolls_RollNo] ON [Boyahane].[FabricRolls] ([RollNo]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030101846_AddFabricReceipts'
)
BEGIN
    CREATE INDEX [IX_FabricRolls_Status] ON [Boyahane].[FabricRolls] ([Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030101846_AddFabricReceipts'
)
BEGIN
    SET IDENTITY_INSERT [Core].[Permissions] ON;
    EXEC(N'INSERT INTO [Core].[Permissions] ([Id], [Code], [Module], [Description])
    SELECT v.[Id], v.[Code], v.[Module], v.[Description] FROM (VALUES
    (1006, N''Boyahane.Receipt.View'', N''Boyahane'', N''Kumaş kabul belgelerini görüntüleme''),
    (1007, N''Boyahane.Receipt.Manage'', N''Boyahane'', N''Kumaş kabul belgelerini oluşturma ve silme'')
    ) v([Id], [Code], [Module], [Description])
    WHERE NOT EXISTS (SELECT 1 FROM [Core].[Permissions] p WHERE p.[Id] = v.[Id] OR p.[Code] = v.[Code])');
    SET IDENTITY_INSERT [Core].[Permissions] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030101846_AddFabricReceipts'
)
BEGIN
    INSERT INTO [Core].[RolePermissions] ([RoleId], [PermissionId])
    SELECT v.[RoleId], v.[PermissionId] FROM (VALUES
    (1, 1006),
    (1, 1007)
    ) v([RoleId], [PermissionId])
    WHERE EXISTS (SELECT 1 FROM [Core].[Roles] r WHERE r.[Id] = v.[RoleId])
      AND EXISTS (SELECT 1 FROM [Core].[Permissions] p WHERE p.[Id] = v.[PermissionId])
      AND NOT EXISTS (SELECT 1 FROM [Core].[RolePermissions] rp WHERE rp.[RoleId] = v.[RoleId] AND rp.[PermissionId] = v.[PermissionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030101846_AddFabricReceipts'
)
BEGIN
    INSERT INTO [Boyahane].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261030101846_AddFabricReceipts', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030103132_AddRawInspection'
)
BEGIN
    ALTER TABLE [Boyahane].[FabricRolls] ADD [CustomerDecisionAccepted] bit NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030103132_AddRawInspection'
)
BEGIN
    ALTER TABLE [Boyahane].[FabricRolls] ADD [CustomerDecisionBy] nvarchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030103132_AddRawInspection'
)
BEGIN
    ALTER TABLE [Boyahane].[FabricRolls] ADD [CustomerDecisionChannel] nvarchar(30) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030103132_AddRawInspection'
)
BEGIN
    ALTER TABLE [Boyahane].[FabricRolls] ADD [CustomerDecisionNote] nvarchar(500) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030103132_AddRawInspection'
)
BEGIN
    ALTER TABLE [Boyahane].[FabricRolls] ADD [CustomerDecisionOn] date NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030103132_AddRawInspection'
)
BEGIN
    ALTER TABLE [Boyahane].[FabricRolls] ADD [CustomerDecisionRecordedBy] nvarchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030103132_AddRawInspection'
)
BEGIN
    ALTER TABLE [Boyahane].[FabricRolls] ADD [InspectedAt] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030103132_AddRawInspection'
)
BEGIN
    ALTER TABLE [Boyahane].[FabricRolls] ADD [InspectedBy] nvarchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030103132_AddRawInspection'
)
BEGIN
    ALTER TABLE [Boyahane].[FabricRolls] ADD [InspectedKilograms] decimal(12,2) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030103132_AddRawInspection'
)
BEGIN
    ALTER TABLE [Boyahane].[FabricRolls] ADD [InspectedMeters] decimal(12,2) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030103132_AddRawInspection'
)
BEGIN
    ALTER TABLE [Boyahane].[FabricRolls] ADD [InspectedWeightGsm] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030103132_AddRawInspection'
)
BEGIN
    ALTER TABLE [Boyahane].[FabricRolls] ADD [InspectedWidthCm] decimal(6,2) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030103132_AddRawInspection'
)
BEGIN
    ALTER TABLE [Boyahane].[FabricRolls] ADD [InspectionNote] nvarchar(1000) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030103132_AddRawInspection'
)
BEGIN
    ALTER TABLE [Boyahane].[FabricRolls] ADD [IsConditionallyApproved] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030103132_AddRawInspection'
)
BEGIN
    CREATE TABLE [Boyahane].[DefectTypes] (
        [Id] int NOT NULL IDENTITY,
        [Code] nvarchar(30) NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [Category] nvarchar(30) NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_DefectTypes] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_DefectTypes_Category] CHECK ([Category] IN ('APPEARANCE', 'STRUCTURE', 'MEASURE'))
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030103132_AddRawInspection'
)
BEGIN
    CREATE TABLE [Boyahane].[FabricRollDefects] (
        [Id] int NOT NULL IDENTITY,
        [RollId] int NOT NULL,
        [DefectTypeId] int NOT NULL,
        [Quantity] int NOT NULL,
        [AtMeter] decimal(12,2) NULL,
        [Note] nvarchar(200) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_FabricRollDefects] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_FabricRollDefects_Quantity] CHECK ([Quantity] >= 1),
        CONSTRAINT [FK_FabricRollDefects_DefectTypes_DefectTypeId] FOREIGN KEY ([DefectTypeId]) REFERENCES [Boyahane].[DefectTypes] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_FabricRollDefects_FabricRolls_RollId] FOREIGN KEY ([RollId]) REFERENCES [Boyahane].[FabricRolls] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030103132_AddRawInspection'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_DefectTypes_Code] ON [Boyahane].[DefectTypes] ([Code]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030103132_AddRawInspection'
)
BEGIN
    CREATE INDEX [IX_FabricRollDefects_DefectTypeId] ON [Boyahane].[FabricRollDefects] ([DefectTypeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030103132_AddRawInspection'
)
BEGIN
    CREATE INDEX [IX_FabricRollDefects_RollId] ON [Boyahane].[FabricRollDefects] ([RollId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030103132_AddRawInspection'
)
BEGIN
    SET IDENTITY_INSERT [Core].[Permissions] ON;
    EXEC(N'INSERT INTO [Core].[Permissions] ([Id], [Code], [Module], [Description])
    SELECT v.[Id], v.[Code], v.[Module], v.[Description] FROM (VALUES
    (1008, N''Boyahane.Inspection.View'', N''Boyahane'', N''Ham kontrolü görüntüleme''),
    (1009, N''Boyahane.Inspection.Manage'', N''Boyahane'', N''Ham kontrol yapma, hata türlerini yönetme''),
    (1010, N''Boyahane.Inspection.CustomerDecision'', N''Boyahane'', N''Şartlı top için müşteri kararını girme'')
    ) v([Id], [Code], [Module], [Description])
    WHERE NOT EXISTS (SELECT 1 FROM [Core].[Permissions] p WHERE p.[Id] = v.[Id] OR p.[Code] = v.[Code])');
    SET IDENTITY_INSERT [Core].[Permissions] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030103132_AddRawInspection'
)
BEGIN
    INSERT INTO [Core].[RolePermissions] ([RoleId], [PermissionId])
    SELECT v.[RoleId], v.[PermissionId] FROM (VALUES
    (1, 1008),
    (1, 1009),
    (1, 1010)
    ) v([RoleId], [PermissionId])
    WHERE EXISTS (SELECT 1 FROM [Core].[Roles] r WHERE r.[Id] = v.[RoleId])
      AND EXISTS (SELECT 1 FROM [Core].[Permissions] p WHERE p.[Id] = v.[PermissionId])
      AND NOT EXISTS (SELECT 1 FROM [Core].[RolePermissions] rp WHERE rp.[RoleId] = v.[RoleId] AND rp.[PermissionId] = v.[PermissionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030103132_AddRawInspection'
)
BEGIN
    INSERT INTO [Boyahane].[DefectTypes] ([Code], [Name], [Category], [IsActive], [IsDeleted], [CreatedAt], [CreatedBy])
    SELECT v.[Code], v.[Name], v.[Category], 1, 0, SYSUTCDATETIME(), N'system' FROM (VALUES
    (N'YAG-LEKE', N'Yağ lekesi', 'APPEARANCE'),
    (N'KIR-LEKE', N'Kir lekesi', 'APPEARANCE'),
    (N'DELIK', N'Delik', 'APPEARANCE'),
    (N'CIZGI', N'Çizgi / barre', 'APPEARANCE'),
    (N'IPLIK-KOPUK', N'İplik kopuğu', 'STRUCTURE'),
    (N'KACIK', N'Kaçık', 'STRUCTURE'),
    (N'ORGU-HATA', N'Örgü / dokuma hatası', 'STRUCTURE'),
    (N'EN-FARK', N'En farkı', 'MEASURE'),
    (N'GRAMAJ-FARK', N'Gramaj farkı', 'MEASURE')
    ) v([Code], [Name], [Category])
    WHERE NOT EXISTS (SELECT 1 FROM [Boyahane].[DefectTypes] d WHERE d.[Code] = v.[Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030103132_AddRawInspection'
)
BEGIN
    INSERT INTO [Boyahane].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261030103132_AddRawInspection', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030104128_AddOrders'
)
BEGIN
    ALTER TABLE [Boyahane].[FabricRolls] ADD [OrderLineId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030104128_AddOrders'
)
BEGIN
    CREATE TABLE [Boyahane].[Orders] (
        [Id] int NOT NULL IDENTITY,
        [OrderNo] nvarchar(30) NOT NULL,
        [OrderDate] date NOT NULL,
        [CustomerId] int NOT NULL,
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
        CONSTRAINT [CK_Orders_Ownership] CHECK ([Ownership] IN ('COMMISSION', 'OWN_GOODS')),
        CONSTRAINT [CK_Orders_Status] CHECK ([Status] IN ('DRAFT', 'CONFIRMED', 'CANCELLED')),
        CONSTRAINT [FK_Orders_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Boyahane].[Customers] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030104128_AddOrders'
)
BEGIN
    CREATE TABLE [Boyahane].[OrderLines] (
        [Id] int NOT NULL IDENTITY,
        [OrderId] int NOT NULL,
        [ProcessType] nvarchar(30) NOT NULL,
        [FabricId] int NULL,
        [YarnId] int NULL,
        [ColorName] nvarchar(100) NULL,
        [ColorCode] nvarchar(50) NULL,
        [Quantity] decimal(12,2) NOT NULL,
        [Unit] nvarchar(30) NOT NULL,
        [UnitPrice] decimal(12,4) NULL,
        [Notes] nvarchar(500) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_OrderLines] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_OrderLines_Item] CHECK (([FabricId] IS NOT NULL AND [YarnId] IS NULL) OR ([FabricId] IS NULL AND [YarnId] IS NOT NULL)),
        CONSTRAINT [CK_OrderLines_ProcessType] CHECK ([ProcessType] IN ('FABRIC_DYEING', 'YARN_DYEING', 'FINISHING', 'PRINTING')),
        CONSTRAINT [CK_OrderLines_Quantity] CHECK ([Quantity] > 0),
        CONSTRAINT [CK_OrderLines_Unit] CHECK ([Unit] IN ('METER', 'KILOGRAM')),
        CONSTRAINT [FK_OrderLines_Fabrics_FabricId] FOREIGN KEY ([FabricId]) REFERENCES [Boyahane].[Fabrics] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_OrderLines_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Boyahane].[Orders] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_OrderLines_Yarns_YarnId] FOREIGN KEY ([YarnId]) REFERENCES [Boyahane].[Yarns] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030104128_AddOrders'
)
BEGIN
    CREATE INDEX [IX_FabricRolls_OrderLineId] ON [Boyahane].[FabricRolls] ([OrderLineId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030104128_AddOrders'
)
BEGIN
    CREATE INDEX [IX_OrderLines_FabricId] ON [Boyahane].[OrderLines] ([FabricId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030104128_AddOrders'
)
BEGIN
    CREATE INDEX [IX_OrderLines_OrderId] ON [Boyahane].[OrderLines] ([OrderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030104128_AddOrders'
)
BEGIN
    CREATE INDEX [IX_OrderLines_YarnId] ON [Boyahane].[OrderLines] ([YarnId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030104128_AddOrders'
)
BEGIN
    CREATE INDEX [IX_Orders_CustomerId] ON [Boyahane].[Orders] ([CustomerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030104128_AddOrders'
)
BEGIN
    CREATE INDEX [IX_Orders_OrderDate] ON [Boyahane].[Orders] ([OrderDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030104128_AddOrders'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Orders_OrderNo] ON [Boyahane].[Orders] ([OrderNo]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030104128_AddOrders'
)
BEGIN
    CREATE INDEX [IX_Orders_Status] ON [Boyahane].[Orders] ([Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030104128_AddOrders'
)
BEGIN
    ALTER TABLE [Boyahane].[FabricRolls] ADD CONSTRAINT [FK_FabricRolls_OrderLines_OrderLineId] FOREIGN KEY ([OrderLineId]) REFERENCES [Boyahane].[OrderLines] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030104128_AddOrders'
)
BEGIN
    SET IDENTITY_INSERT [Core].[Permissions] ON;
    EXEC(N'INSERT INTO [Core].[Permissions] ([Id], [Code], [Module], [Description])
    SELECT v.[Id], v.[Code], v.[Module], v.[Description] FROM (VALUES
    (1011, N''Boyahane.Order.View'', N''Boyahane'', N''Boyahane siparişlerini görüntüleme''),
    (1012, N''Boyahane.Order.Manage'', N''Boyahane'', N''Boyahane siparişlerini oluşturma, onaylama ve iptal etme'')
    ) v([Id], [Code], [Module], [Description])
    WHERE NOT EXISTS (SELECT 1 FROM [Core].[Permissions] p WHERE p.[Id] = v.[Id] OR p.[Code] = v.[Code])');
    SET IDENTITY_INSERT [Core].[Permissions] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030104128_AddOrders'
)
BEGIN
    INSERT INTO [Core].[RolePermissions] ([RoleId], [PermissionId])
    SELECT v.[RoleId], v.[PermissionId] FROM (VALUES
    (1, 1011),
    (1, 1012)
    ) v([RoleId], [PermissionId])
    WHERE EXISTS (SELECT 1 FROM [Core].[Roles] r WHERE r.[Id] = v.[RoleId])
      AND EXISTS (SELECT 1 FROM [Core].[Permissions] p WHERE p.[Id] = v.[PermissionId])
      AND NOT EXISTS (SELECT 1 FROM [Core].[RolePermissions] rp WHERE rp.[RoleId] = v.[RoleId] AND rp.[PermissionId] = v.[PermissionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030104128_AddOrders'
)
BEGIN
    INSERT INTO [Boyahane].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261030104128_AddOrders', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030105531_AddStockOrdersAndYarnLots'
)
BEGIN
    DECLARE @var nvarchar(max);
    SELECT @var = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Boyahane].[Orders]') AND [c].[name] = N'CustomerId');
    IF @var IS NOT NULL EXEC(N'ALTER TABLE [Boyahane].[Orders] DROP CONSTRAINT ' + @var + ';');
    ALTER TABLE [Boyahane].[Orders] ALTER COLUMN [CustomerId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030105531_AddStockOrdersAndYarnLots'
)
BEGIN
    CREATE TABLE [Boyahane].[YarnLots] (
        [Id] int NOT NULL IDENTITY,
        [LotNo] nvarchar(30) NOT NULL,
        [ReceiptDate] date NOT NULL,
        [CustomerId] int NULL,
        [YarnId] int NOT NULL,
        [DespatchNo] nvarchar(50) NULL,
        [CustomerLotNo] nvarchar(50) NULL,
        [PackageType] nvarchar(30) NOT NULL,
        [PackageCount] int NOT NULL,
        [Kilograms] decimal(12,2) NOT NULL,
        [Notes] nvarchar(1000) NULL,
        [OrderLineId] int NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_YarnLots] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_YarnLots_Kilograms] CHECK ([Kilograms] > 0),
        CONSTRAINT [CK_YarnLots_PackageCount] CHECK ([PackageCount] >= 1),
        CONSTRAINT [CK_YarnLots_PackageType] CHECK ([PackageType] IN ('BOBBIN', 'HANK')),
        CONSTRAINT [FK_YarnLots_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Boyahane].[Customers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_YarnLots_OrderLines_OrderLineId] FOREIGN KEY ([OrderLineId]) REFERENCES [Boyahane].[OrderLines] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_YarnLots_Yarns_YarnId] FOREIGN KEY ([YarnId]) REFERENCES [Boyahane].[Yarns] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030105531_AddStockOrdersAndYarnLots'
)
BEGIN
    CREATE INDEX [IX_YarnLots_CustomerId] ON [Boyahane].[YarnLots] ([CustomerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030105531_AddStockOrdersAndYarnLots'
)
BEGIN
    CREATE UNIQUE INDEX [IX_YarnLots_LotNo] ON [Boyahane].[YarnLots] ([LotNo]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030105531_AddStockOrdersAndYarnLots'
)
BEGIN
    CREATE INDEX [IX_YarnLots_OrderLineId] ON [Boyahane].[YarnLots] ([OrderLineId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030105531_AddStockOrdersAndYarnLots'
)
BEGIN
    CREATE INDEX [IX_YarnLots_YarnId] ON [Boyahane].[YarnLots] ([YarnId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030105531_AddStockOrdersAndYarnLots'
)
BEGIN
    INSERT INTO [Boyahane].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261030105531_AddStockOrdersAndYarnLots', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030110346_AddLab'
)
BEGIN
    CREATE TABLE [Boyahane].[DyeChemicals] (
        [Id] int NOT NULL IDENTITY,
        [Code] nvarchar(30) NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [Kind] nvarchar(30) NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_DyeChemicals] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_DyeChemicals_Kind] CHECK ([Kind] IN ('DYE', 'CHEMICAL'))
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030110346_AddLab'
)
BEGIN
    CREATE TABLE [Boyahane].[LabRequests] (
        [Id] int NOT NULL IDENTITY,
        [RequestNo] nvarchar(30) NOT NULL,
        [OrderLineId] int NOT NULL,
        [TargetNote] nvarchar(500) NULL,
        [Status] nvarchar(30) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_LabRequests] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_LabRequests_Status] CHECK ([Status] IN ('OPEN', 'APPROVED', 'CANCELLED')),
        CONSTRAINT [FK_LabRequests_OrderLines_OrderLineId] FOREIGN KEY ([OrderLineId]) REFERENCES [Boyahane].[OrderLines] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030110346_AddLab'
)
BEGIN
    CREATE TABLE [Boyahane].[LabTrials] (
        [Id] int NOT NULL IDENTITY,
        [RequestId] int NOT NULL,
        [TrialCode] nvarchar(2) NOT NULL,
        [Notes] nvarchar(500) NULL,
        [Decision] nvarchar(30) NOT NULL,
        [DecisionBy] nvarchar(100) NULL,
        [DecisionChannel] nvarchar(30) NULL,
        [DecisionOn] date NULL,
        [DecisionNote] nvarchar(500) NULL,
        [DecisionRecordedBy] nvarchar(100) NULL,
        [ReusedFromTrialId] int NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_LabTrials] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_LabTrials_Decision] CHECK ([Decision] IN ('PENDING', 'ACCEPTED', 'REJECTED')),
        CONSTRAINT [FK_LabTrials_LabRequests_RequestId] FOREIGN KEY ([RequestId]) REFERENCES [Boyahane].[LabRequests] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_LabTrials_LabTrials_ReusedFromTrialId] FOREIGN KEY ([ReusedFromTrialId]) REFERENCES [Boyahane].[LabTrials] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030110346_AddLab'
)
BEGIN
    CREATE TABLE [Boyahane].[LabTrialItems] (
        [Id] int NOT NULL IDENTITY,
        [TrialId] int NOT NULL,
        [DyeChemicalId] int NOT NULL,
        [Amount] decimal(10,4) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_LabTrialItems] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_LabTrialItems_Amount] CHECK ([Amount] > 0),
        CONSTRAINT [FK_LabTrialItems_DyeChemicals_DyeChemicalId] FOREIGN KEY ([DyeChemicalId]) REFERENCES [Boyahane].[DyeChemicals] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_LabTrialItems_LabTrials_TrialId] FOREIGN KEY ([TrialId]) REFERENCES [Boyahane].[LabTrials] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030110346_AddLab'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_DyeChemicals_Code] ON [Boyahane].[DyeChemicals] ([Code]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030110346_AddLab'
)
BEGIN
    CREATE UNIQUE INDEX [IX_LabRequests_RequestNo] ON [Boyahane].[LabRequests] ([RequestNo]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030110346_AddLab'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_LabRequests_ActivePerLine] ON [Boyahane].[LabRequests] ([OrderLineId]) WHERE [Status] <> ''CANCELLED''');
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030110346_AddLab'
)
BEGIN
    CREATE INDEX [IX_LabTrialItems_DyeChemicalId] ON [Boyahane].[LabTrialItems] ([DyeChemicalId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030110346_AddLab'
)
BEGIN
    CREATE UNIQUE INDEX [IX_LabTrialItems_TrialId_DyeChemicalId] ON [Boyahane].[LabTrialItems] ([TrialId], [DyeChemicalId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030110346_AddLab'
)
BEGIN
    CREATE UNIQUE INDEX [IX_LabTrials_RequestId_TrialCode] ON [Boyahane].[LabTrials] ([RequestId], [TrialCode]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030110346_AddLab'
)
BEGIN
    CREATE INDEX [IX_LabTrials_ReusedFromTrialId] ON [Boyahane].[LabTrials] ([ReusedFromTrialId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030110346_AddLab'
)
BEGIN
    SET IDENTITY_INSERT [Core].[Permissions] ON;
    EXEC(N'INSERT INTO [Core].[Permissions] ([Id], [Code], [Module], [Description])
    SELECT v.[Id], v.[Code], v.[Module], v.[Description] FROM (VALUES
    (1013, N''Boyahane.Lab.View'', N''Boyahane'', N''Laboratuvarı görüntüleme''),
    (1014, N''Boyahane.Lab.Manage'', N''Boyahane'', N''Laboratuvar talebi ve denemeleri, boya / kimyasal kartları''),
    (1015, N''Boyahane.Lab.CustomerDecision'', N''Boyahane'', N''Lab denemesi için müşteri kararını girme, önceki reçeteyi kullanma'')
    ) v([Id], [Code], [Module], [Description])
    WHERE NOT EXISTS (SELECT 1 FROM [Core].[Permissions] p WHERE p.[Id] = v.[Id] OR p.[Code] = v.[Code])');
    SET IDENTITY_INSERT [Core].[Permissions] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030110346_AddLab'
)
BEGIN
    INSERT INTO [Core].[RolePermissions] ([RoleId], [PermissionId])
    SELECT v.[RoleId], v.[PermissionId] FROM (VALUES
    (1, 1013),
    (1, 1014),
    (1, 1015)
    ) v([RoleId], [PermissionId])
    WHERE EXISTS (SELECT 1 FROM [Core].[Roles] r WHERE r.[Id] = v.[RoleId])
      AND EXISTS (SELECT 1 FROM [Core].[Permissions] p WHERE p.[Id] = v.[PermissionId])
      AND NOT EXISTS (SELECT 1 FROM [Core].[RolePermissions] rp WHERE rp.[RoleId] = v.[RoleId] AND rp.[PermissionId] = v.[PermissionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030110346_AddLab'
)
BEGIN
    INSERT INTO [Boyahane].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261030110346_AddLab', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030111427_AddBatches'
)
BEGIN
    CREATE TABLE [Boyahane].[Machines] (
        [Id] int NOT NULL IDENTITY,
        [Code] nvarchar(30) NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [MachineType] nvarchar(30) NOT NULL,
        [CapacityKg] decimal(10,2) NOT NULL,
        [DefaultLiquorRatio] decimal(6,2) NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_Machines] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Machines_CapacityKg] CHECK ([CapacityKg] > 0),
        CONSTRAINT [CK_Machines_MachineType] CHECK ([MachineType] IN ('JET', 'OVERFLOW', 'JIGGER', 'STENTER', 'YARN_VESSEL'))
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030111427_AddBatches'
)
BEGIN
    CREATE TABLE [Boyahane].[Batches] (
        [Id] int NOT NULL IDENTITY,
        [BatchNo] nvarchar(30) NOT NULL,
        [MachineId] int NOT NULL,
        [RecipeTrialId] int NOT NULL,
        [LiquorRatio] decimal(6,2) NOT NULL,
        [PlannedDate] date NULL,
        [Notes] nvarchar(1000) NULL,
        [Status] nvarchar(30) NOT NULL,
        [CapacityOverrideBy] nvarchar(100) NULL,
        [CapacityOverrideNote] nvarchar(500) NULL,
        [StartedAt] datetime2 NULL,
        [CompletedAt] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_Batches] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Batches_LiquorRatio] CHECK ([LiquorRatio] > 0),
        CONSTRAINT [CK_Batches_Status] CHECK ([Status] IN ('PLANNED', 'ON_MACHINE', 'COMPLETED', 'CANCELLED')),
        CONSTRAINT [FK_Batches_LabTrials_RecipeTrialId] FOREIGN KEY ([RecipeTrialId]) REFERENCES [Boyahane].[LabTrials] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Batches_Machines_MachineId] FOREIGN KEY ([MachineId]) REFERENCES [Boyahane].[Machines] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030111427_AddBatches'
)
BEGIN
    CREATE TABLE [Boyahane].[BatchItems] (
        [Id] int NOT NULL IDENTITY,
        [BatchId] int NOT NULL,
        [OrderLineId] int NOT NULL,
        [RollId] int NULL,
        [YarnLotId] int NULL,
        [Kilograms] decimal(12,2) NOT NULL,
        [Meters] decimal(12,2) NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_BatchItems] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_BatchItems_Item] CHECK (([RollId] IS NOT NULL AND [YarnLotId] IS NULL) OR ([RollId] IS NULL AND [YarnLotId] IS NOT NULL)),
        CONSTRAINT [CK_BatchItems_Kilograms] CHECK ([Kilograms] > 0),
        CONSTRAINT [FK_BatchItems_Batches_BatchId] FOREIGN KEY ([BatchId]) REFERENCES [Boyahane].[Batches] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_BatchItems_FabricRolls_RollId] FOREIGN KEY ([RollId]) REFERENCES [Boyahane].[FabricRolls] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_BatchItems_OrderLines_OrderLineId] FOREIGN KEY ([OrderLineId]) REFERENCES [Boyahane].[OrderLines] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_BatchItems_YarnLots_YarnLotId] FOREIGN KEY ([YarnLotId]) REFERENCES [Boyahane].[YarnLots] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030111427_AddBatches'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Batches_BatchNo] ON [Boyahane].[Batches] ([BatchNo]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030111427_AddBatches'
)
BEGIN
    CREATE INDEX [IX_Batches_RecipeTrialId] ON [Boyahane].[Batches] ([RecipeTrialId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030111427_AddBatches'
)
BEGIN
    CREATE INDEX [IX_Batches_Status] ON [Boyahane].[Batches] ([Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030111427_AddBatches'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_Batches_OnMachine] ON [Boyahane].[Batches] ([MachineId]) WHERE [Status] = ''ON_MACHINE''');
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030111427_AddBatches'
)
BEGIN
    CREATE INDEX [IX_BatchItems_BatchId] ON [Boyahane].[BatchItems] ([BatchId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030111427_AddBatches'
)
BEGIN
    CREATE INDEX [IX_BatchItems_OrderLineId] ON [Boyahane].[BatchItems] ([OrderLineId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030111427_AddBatches'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_BatchItems_ActiveRoll] ON [Boyahane].[BatchItems] ([RollId]) WHERE [IsActive] = 1 AND [RollId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030111427_AddBatches'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_BatchItems_ActiveYarnLot] ON [Boyahane].[BatchItems] ([YarnLotId]) WHERE [IsActive] = 1 AND [YarnLotId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030111427_AddBatches'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Machines_Code] ON [Boyahane].[Machines] ([Code]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030111427_AddBatches'
)
BEGIN
    SET IDENTITY_INSERT [Core].[Permissions] ON;
    EXEC(N'INSERT INTO [Core].[Permissions] ([Id], [Code], [Module], [Description])
    SELECT v.[Id], v.[Code], v.[Module], v.[Description] FROM (VALUES
    (1016, N''Boyahane.Batch.View'', N''Boyahane'', N''Partileri ve makineleri görüntüleme''),
    (1017, N''Boyahane.Batch.Manage'', N''Boyahane'', N''Parti açma ve yürütme, makine kartları''),
    (1018, N''Boyahane.Batch.CapacityOverride'', N''Boyahane'', N''Makine kapasitesini aşan parti açma'')
    ) v([Id], [Code], [Module], [Description])
    WHERE NOT EXISTS (SELECT 1 FROM [Core].[Permissions] p WHERE p.[Id] = v.[Id] OR p.[Code] = v.[Code])');
    SET IDENTITY_INSERT [Core].[Permissions] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030111427_AddBatches'
)
BEGIN
    INSERT INTO [Core].[RolePermissions] ([RoleId], [PermissionId])
    SELECT v.[RoleId], v.[PermissionId] FROM (VALUES
    (1, 1016),
    (1, 1017),
    (1, 1018)
    ) v([RoleId], [PermissionId])
    WHERE EXISTS (SELECT 1 FROM [Core].[Roles] r WHERE r.[Id] = v.[RoleId])
      AND EXISTS (SELECT 1 FROM [Core].[Permissions] p WHERE p.[Id] = v.[PermissionId])
      AND NOT EXISTS (SELECT 1 FROM [Core].[RolePermissions] rp WHERE rp.[RoleId] = v.[RoleId] AND rp.[PermissionId] = v.[PermissionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030111427_AddBatches'
)
BEGIN
    INSERT INTO [Boyahane].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261030111427_AddBatches', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030112751_AddYarnLotStatusAndRecipelessBatches'
)
BEGIN
    ALTER TABLE [Boyahane].[YarnLots] ADD [RejectionNote] nvarchar(500) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030112751_AddYarnLotStatusAndRecipelessBatches'
)
BEGIN
    ALTER TABLE [Boyahane].[YarnLots] ADD [Status] nvarchar(30) NOT NULL DEFAULT N'APPROVED';
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030112751_AddYarnLotStatusAndRecipelessBatches'
)
BEGIN
    DECLARE @var1 nvarchar(max);
    SELECT @var1 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Boyahane].[Batches]') AND [c].[name] = N'RecipeTrialId');
    IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [Boyahane].[Batches] DROP CONSTRAINT ' + @var1 + ';');
    ALTER TABLE [Boyahane].[Batches] ALTER COLUMN [RecipeTrialId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030112751_AddYarnLotStatusAndRecipelessBatches'
)
BEGIN
    DECLARE @var2 nvarchar(max);
    SELECT @var2 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Boyahane].[Batches]') AND [c].[name] = N'LiquorRatio');
    IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [Boyahane].[Batches] DROP CONSTRAINT ' + @var2 + ';');
    ALTER TABLE [Boyahane].[Batches] ALTER COLUMN [LiquorRatio] decimal(6,2) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030112751_AddYarnLotStatusAndRecipelessBatches'
)
BEGIN
    EXEC(N'ALTER TABLE [Boyahane].[YarnLots] ADD CONSTRAINT [CK_YarnLots_Status] CHECK ([Status] IN (''APPROVED'', ''REJECTED''))');
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030112751_AddYarnLotStatusAndRecipelessBatches'
)
BEGIN
    INSERT INTO [Boyahane].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261030112751_AddYarnLotStatusAndRecipelessBatches', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030114540_AddBatchStepsAndTablets'
)
BEGIN
    DROP INDEX [UX_Batches_OnMachine] ON [Boyahane].[Batches];
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030114540_AddBatchStepsAndTablets'
)
BEGIN
    ALTER TABLE [Boyahane].[BatchItems] ADD [FinalNote] nvarchar(500) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030114540_AddBatchStepsAndTablets'
)
BEGIN
    ALTER TABLE [Boyahane].[BatchItems] ADD [FinalResult] nvarchar(30) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030114540_AddBatchStepsAndTablets'
)
BEGIN
    ALTER TABLE [Boyahane].[Batches] ADD [CardUid] nvarchar(10) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030114540_AddBatchStepsAndTablets'
)
BEGIN
    ALTER TABLE [Boyahane].[Batches] ADD [ReworkOfBatchId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030114540_AddBatchStepsAndTablets'
)
BEGIN
    CREATE TABLE [Boyahane].[Operators] (
        [Id] int NOT NULL IDENTITY,
        [Code] nvarchar(30) NOT NULL,
        [Name] nvarchar(150) NOT NULL,
        [CardUid] nvarchar(10) NULL,
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
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030114540_AddBatchStepsAndTablets'
)
BEGIN
    CREATE TABLE [Boyahane].[StepRequests] (
        [Id] bigint NOT NULL IDENTITY,
        [RequestId] uniqueidentifier NOT NULL,
        [StepId] int NOT NULL,
        [ReceivedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_StepRequests] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030114540_AddBatchStepsAndTablets'
)
BEGIN
    CREATE TABLE [Boyahane].[Tablets] (
        [Id] int NOT NULL IDENTITY,
        [TabletCode] nvarchar(30) NOT NULL,
        [Name] nvarchar(150) NULL,
        [MachineId] int NULL,
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
        CONSTRAINT [FK_Tablets_Machines_MachineId] FOREIGN KEY ([MachineId]) REFERENCES [Boyahane].[Machines] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030114540_AddBatchStepsAndTablets'
)
BEGIN
    CREATE TABLE [Boyahane].[BatchSteps] (
        [Id] int NOT NULL IDENTITY,
        [BatchId] int NOT NULL,
        [Sequence] int NOT NULL,
        [StepType] nvarchar(30) NOT NULL,
        [ReworkKind] nvarchar(30) NULL,
        [Status] nvarchar(30) NOT NULL,
        [MachineId] int NULL,
        [TabletId] int NULL,
        [StartedByOperatorId] int NULL,
        [CompletedByOperatorId] int NULL,
        [StartedAt] datetime2 NULL,
        [CompletedAt] datetime2 NULL,
        [ColorResult] nvarchar(30) NULL,
        [Note] nvarchar(500) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_BatchSteps] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_BatchSteps_ColorResult] CHECK ([ColorResult] IN ('APPROVED', 'ADDITION', 'REDO')),
        CONSTRAINT [CK_BatchSteps_ReworkKind] CHECK ([ReworkKind] IN ('APPROVED', 'ADDITION', 'REDO')),
        CONSTRAINT [CK_BatchSteps_Status] CHECK ([Status] IN ('PENDING', 'IN_PROGRESS', 'COMPLETED', 'SKIPPED')),
        CONSTRAINT [CK_BatchSteps_StepType] CHECK ([StepType] IN ('PRETREATMENT', 'DYEING', 'COLOR_CHECK', 'WASH_DRY', 'FINISHING', 'PRINTING', 'FINAL_QUALITY', 'PACKAGING')),
        CONSTRAINT [FK_BatchSteps_Batches_BatchId] FOREIGN KEY ([BatchId]) REFERENCES [Boyahane].[Batches] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_BatchSteps_Machines_MachineId] FOREIGN KEY ([MachineId]) REFERENCES [Boyahane].[Machines] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_BatchSteps_Operators_CompletedByOperatorId] FOREIGN KEY ([CompletedByOperatorId]) REFERENCES [Boyahane].[Operators] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_BatchSteps_Operators_StartedByOperatorId] FOREIGN KEY ([StartedByOperatorId]) REFERENCES [Boyahane].[Operators] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_BatchSteps_Tablets_TabletId] FOREIGN KEY ([TabletId]) REFERENCES [Boyahane].[Tablets] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030114540_AddBatchStepsAndTablets'
)
BEGIN
    CREATE TABLE [Boyahane].[TabletSessions] (
        [Id] bigint NOT NULL IDENTITY,
        [TabletId] int NOT NULL,
        [OperatorId] int NOT NULL,
        [StartedAt] datetime2 NOT NULL,
        [EndedAt] datetime2 NULL,
        CONSTRAINT [PK_TabletSessions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_TabletSessions_Operators_OperatorId] FOREIGN KEY ([OperatorId]) REFERENCES [Boyahane].[Operators] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_TabletSessions_Tablets_TabletId] FOREIGN KEY ([TabletId]) REFERENCES [Boyahane].[Tablets] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030114540_AddBatchStepsAndTablets'
)
BEGIN
    CREATE TABLE [Boyahane].[BatchConsumptions] (
        [Id] int NOT NULL IDENTITY,
        [StepId] int NOT NULL,
        [DyeChemicalId] int NOT NULL,
        [PlannedKilograms] decimal(12,3) NOT NULL,
        [ActualKilograms] decimal(12,3) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_BatchConsumptions] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_BatchConsumptions_Actual] CHECK ([ActualKilograms] >= 0),
        CONSTRAINT [FK_BatchConsumptions_BatchSteps_StepId] FOREIGN KEY ([StepId]) REFERENCES [Boyahane].[BatchSteps] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_BatchConsumptions_DyeChemicals_DyeChemicalId] FOREIGN KEY ([DyeChemicalId]) REFERENCES [Boyahane].[DyeChemicals] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030114540_AddBatchStepsAndTablets'
)
BEGIN
    EXEC(N'ALTER TABLE [Boyahane].[BatchItems] ADD CONSTRAINT [CK_BatchItems_FinalResult] CHECK ([FinalResult] IN (''APPROVED'', ''REJECTED'', ''REWORK''))');
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030114540_AddBatchStepsAndTablets'
)
BEGIN
    CREATE INDEX [IX_Batches_MachineId] ON [Boyahane].[Batches] ([MachineId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030114540_AddBatchStepsAndTablets'
)
BEGIN
    CREATE INDEX [IX_Batches_ReworkOfBatchId] ON [Boyahane].[Batches] ([ReworkOfBatchId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030114540_AddBatchStepsAndTablets'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_Batches_CardUid] ON [Boyahane].[Batches] ([CardUid]) WHERE [CardUid] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030114540_AddBatchStepsAndTablets'
)
BEGIN
    CREATE INDEX [IX_BatchConsumptions_DyeChemicalId] ON [Boyahane].[BatchConsumptions] ([DyeChemicalId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030114540_AddBatchStepsAndTablets'
)
BEGIN
    CREATE INDEX [IX_BatchConsumptions_StepId] ON [Boyahane].[BatchConsumptions] ([StepId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030114540_AddBatchStepsAndTablets'
)
BEGIN
    CREATE INDEX [IX_BatchSteps_BatchId_Sequence] ON [Boyahane].[BatchSteps] ([BatchId], [Sequence]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030114540_AddBatchStepsAndTablets'
)
BEGIN
    CREATE INDEX [IX_BatchSteps_CompletedByOperatorId] ON [Boyahane].[BatchSteps] ([CompletedByOperatorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030114540_AddBatchStepsAndTablets'
)
BEGIN
    CREATE INDEX [IX_BatchSteps_StartedByOperatorId] ON [Boyahane].[BatchSteps] ([StartedByOperatorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030114540_AddBatchStepsAndTablets'
)
BEGIN
    CREATE INDEX [IX_BatchSteps_TabletId] ON [Boyahane].[BatchSteps] ([TabletId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030114540_AddBatchStepsAndTablets'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_BatchSteps_BatchInProgress] ON [Boyahane].[BatchSteps] ([BatchId]) WHERE [Status] = ''IN_PROGRESS''');
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030114540_AddBatchStepsAndTablets'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_BatchSteps_MachineInProgress] ON [Boyahane].[BatchSteps] ([MachineId]) WHERE [Status] = ''IN_PROGRESS'' AND [MachineId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030114540_AddBatchStepsAndTablets'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Operators_Code] ON [Boyahane].[Operators] ([Code]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030114540_AddBatchStepsAndTablets'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_Operators_CardUid] ON [Boyahane].[Operators] ([CardUid]) WHERE [CardUid] IS NOT NULL AND [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030114540_AddBatchStepsAndTablets'
)
BEGIN
    CREATE UNIQUE INDEX [IX_StepRequests_RequestId] ON [Boyahane].[StepRequests] ([RequestId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030114540_AddBatchStepsAndTablets'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Tablets_TabletCode] ON [Boyahane].[Tablets] ([TabletCode]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030114540_AddBatchStepsAndTablets'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_Tablets_Device] ON [Boyahane].[Tablets] ([DeviceIdentifier]) WHERE [DeviceIdentifier] IS NOT NULL AND [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030114540_AddBatchStepsAndTablets'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_Tablets_Machine] ON [Boyahane].[Tablets] ([MachineId]) WHERE [MachineId] IS NOT NULL AND [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030114540_AddBatchStepsAndTablets'
)
BEGIN
    CREATE INDEX [IX_TabletSessions_OperatorId] ON [Boyahane].[TabletSessions] ([OperatorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030114540_AddBatchStepsAndTablets'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_TabletSessions_Open] ON [Boyahane].[TabletSessions] ([TabletId]) WHERE [EndedAt] IS NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030114540_AddBatchStepsAndTablets'
)
BEGIN
    ALTER TABLE [Boyahane].[Batches] ADD CONSTRAINT [FK_Batches_Batches_ReworkOfBatchId] FOREIGN KEY ([ReworkOfBatchId]) REFERENCES [Boyahane].[Batches] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030114540_AddBatchStepsAndTablets'
)
BEGIN
    SET IDENTITY_INSERT [Core].[Permissions] ON;
    EXEC(N'INSERT INTO [Core].[Permissions] ([Id], [Code], [Module], [Description])
    SELECT v.[Id], v.[Code], v.[Module], v.[Description] FROM (VALUES
    (1019, N''Boyahane.Floor.View'', N''Boyahane'', N''Personeli ve tabletleri görüntüleme''),
    (1020, N''Boyahane.Floor.Manage'', N''Boyahane'', N''Personel ve tablet tanımları, cihaz sıfırlama'')
    ) v([Id], [Code], [Module], [Description])
    WHERE NOT EXISTS (SELECT 1 FROM [Core].[Permissions] p WHERE p.[Id] = v.[Id] OR p.[Code] = v.[Code])');
    SET IDENTITY_INSERT [Core].[Permissions] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030114540_AddBatchStepsAndTablets'
)
BEGIN
    INSERT INTO [Core].[RolePermissions] ([RoleId], [PermissionId])
    SELECT v.[RoleId], v.[PermissionId] FROM (VALUES
    (1, 1019),
    (1, 1020)
    ) v([RoleId], [PermissionId])
    WHERE EXISTS (SELECT 1 FROM [Core].[Roles] r WHERE r.[Id] = v.[RoleId])
      AND EXISTS (SELECT 1 FROM [Core].[Permissions] p WHERE p.[Id] = v.[PermissionId])
      AND NOT EXISTS (SELECT 1 FROM [Core].[RolePermissions] rp WHERE rp.[RoleId] = v.[RoleId] AND rp.[PermissionId] = v.[PermissionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030114540_AddBatchStepsAndTablets'
)
BEGIN
    EXEC(N'INSERT INTO [Boyahane].[BatchSteps] ([BatchId], [Sequence], [StepType], [Status], [CreatedAt])
    SELECT b.[Id], t.[Seq], t.[StepType], ''PENDING'', SYSUTCDATETIME()
    FROM [Boyahane].[Batches] b
    CROSS APPLY (SELECT TOP 1 l.[ProcessType] FROM [Boyahane].[BatchItems] i JOIN [Boyahane].[OrderLines] l ON l.[Id] = i.[OrderLineId] WHERE i.[BatchId] = b.[Id] ORDER BY i.[Id]) p
    JOIN (VALUES
    (''FABRIC_DYEING'',1,''PRETREATMENT''),(''FABRIC_DYEING'',2,''DYEING''),(''FABRIC_DYEING'',3,''COLOR_CHECK''),(''FABRIC_DYEING'',4,''WASH_DRY''),(''FABRIC_DYEING'',5,''FINISHING''),(''FABRIC_DYEING'',6,''FINAL_QUALITY''),(''FABRIC_DYEING'',7,''PACKAGING''),
    (''YARN_DYEING'',1,''DYEING''),(''YARN_DYEING'',2,''COLOR_CHECK''),(''YARN_DYEING'',3,''WASH_DRY''),(''YARN_DYEING'',4,''FINAL_QUALITY''),(''YARN_DYEING'',5,''PACKAGING''),
    (''PRINTING'',1,''PRINTING''),(''PRINTING'',2,''WASH_DRY''),(''PRINTING'',3,''FINAL_QUALITY''),(''PRINTING'',4,''PACKAGING''),
    (''FINISHING'',1,''FINISHING''),(''FINISHING'',2,''FINAL_QUALITY''),(''FINISHING'',3,''PACKAGING'')
    ) t([ProcessType],[Seq],[StepType]) ON t.[ProcessType] = p.[ProcessType]
    WHERE b.[Status] IN (''PLANNED'', ''ON_MACHINE'');');
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030114540_AddBatchStepsAndTablets'
)
BEGIN
    UPDATE s SET s.[Status] = 'IN_PROGRESS', s.[MachineId] = b.[MachineId], s.[StartedAt] = b.[StartedAt]
    FROM [Boyahane].[BatchSteps] s JOIN [Boyahane].[Batches] b ON b.[Id] = s.[BatchId]
    WHERE b.[Status] = 'ON_MACHINE' AND s.[Sequence] = 1;
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030114540_AddBatchStepsAndTablets'
)
BEGIN
    INSERT INTO [Boyahane].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261030114540_AddBatchStepsAndTablets', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030120727_AddCardsStatusPausesAndRoles'
)
BEGIN
    DROP INDEX [IX_TabletSessions_OperatorId] ON [Boyahane].[TabletSessions];
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030120727_AddCardsStatusPausesAndRoles'
)
BEGIN
    ALTER TABLE [Boyahane].[TabletSessions] ADD [EndReason] nvarchar(30) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030120727_AddCardsStatusPausesAndRoles'
)
BEGIN
    ALTER TABLE [Boyahane].[Operators] ADD [Role] nvarchar(30) NOT NULL DEFAULT N'OPERATOR';
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030120727_AddCardsStatusPausesAndRoles'
)
BEGIN
    ALTER TABLE [Boyahane].[Machines] ADD [CanDye] bit NOT NULL DEFAULT CAST(1 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030120727_AddCardsStatusPausesAndRoles'
)
BEGIN
    ALTER TABLE [Boyahane].[Machines] ADD [CanFinish] bit NOT NULL DEFAULT CAST(1 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030120727_AddCardsStatusPausesAndRoles'
)
BEGIN
    ALTER TABLE [Boyahane].[Machines] ADD [CanPretreat] bit NOT NULL DEFAULT CAST(1 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030120727_AddCardsStatusPausesAndRoles'
)
BEGIN
    ALTER TABLE [Boyahane].[Machines] ADD [CanPrint] bit NOT NULL DEFAULT CAST(1 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030120727_AddCardsStatusPausesAndRoles'
)
BEGIN
    ALTER TABLE [Boyahane].[Machines] ADD [CanWashDry] bit NOT NULL DEFAULT CAST(1 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030120727_AddCardsStatusPausesAndRoles'
)
BEGIN
    ALTER TABLE [Boyahane].[Machines] ADD [Status] nvarchar(30) NOT NULL DEFAULT N'AVAILABLE';
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030120727_AddCardsStatusPausesAndRoles'
)
BEGIN
    CREATE TABLE [Boyahane].[MachineStatusLogs] (
        [Id] bigint NOT NULL IDENTITY,
        [MachineId] int NOT NULL,
        [Status] nvarchar(30) NOT NULL,
        [StartedAt] datetime2 NOT NULL,
        [EndedAt] datetime2 NULL,
        [Reason] nvarchar(500) NULL,
        [ChangedBy] nvarchar(100) NOT NULL,
        [TabletId] int NULL,
        CONSTRAINT [PK_MachineStatusLogs] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_MachineStatusLogs_Status] CHECK ([Status] IN ('AVAILABLE', 'BREAKDOWN', 'MAINTENANCE')),
        CONSTRAINT [FK_MachineStatusLogs_Machines_MachineId] FOREIGN KEY ([MachineId]) REFERENCES [Boyahane].[Machines] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030120727_AddCardsStatusPausesAndRoles'
)
BEGIN
    CREATE TABLE [Boyahane].[RfidCards] (
        [Id] int NOT NULL IDENTITY,
        [Uid] nvarchar(10) NOT NULL,
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
        CONSTRAINT [CK_RfidCards_CardType] CHECK ([CardType] IN ('OPERATOR', 'BATCH'))
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030120727_AddCardsStatusPausesAndRoles'
)
BEGIN
    CREATE TABLE [Boyahane].[StopReasons] (
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
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030120727_AddCardsStatusPausesAndRoles'
)
BEGIN
    CREATE TABLE [Boyahane].[SessionPauses] (
        [Id] bigint NOT NULL IDENTITY,
        [SessionId] bigint NOT NULL,
        [Kind] nvarchar(30) NOT NULL,
        [ReasonId] int NULL,
        [Note] nvarchar(500) NULL,
        [StartedAt] datetime2 NOT NULL,
        [EndedAt] datetime2 NULL,
        CONSTRAINT [PK_SessionPauses] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_SessionPauses_Kind] CHECK ([Kind] IN ('BREAK', 'PAUSE', 'MACHINE_BREAKDOWN')),
        CONSTRAINT [FK_SessionPauses_StopReasons_ReasonId] FOREIGN KEY ([ReasonId]) REFERENCES [Boyahane].[StopReasons] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_SessionPauses_TabletSessions_SessionId] FOREIGN KEY ([SessionId]) REFERENCES [Boyahane].[TabletSessions] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030120727_AddCardsStatusPausesAndRoles'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_TabletSessions_OperatorOpen] ON [Boyahane].[TabletSessions] ([OperatorId]) WHERE [EndedAt] IS NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030120727_AddCardsStatusPausesAndRoles'
)
BEGIN
    EXEC(N'ALTER TABLE [Boyahane].[TabletSessions] ADD CONSTRAINT [CK_TabletSessions_EndReason] CHECK ([EndReason] IN (''LOGOUT'', ''TAKEN_OVER'', ''MOVED_AWAY'', ''CLOSED_BY_OFFICE''))');
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030120727_AddCardsStatusPausesAndRoles'
)
BEGIN
    EXEC(N'ALTER TABLE [Boyahane].[Operators] ADD CONSTRAINT [CK_Operators_Role] CHECK ([Role] IN (''OPERATOR'', ''SUPERVISOR'', ''MAINTENANCE''))');
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030120727_AddCardsStatusPausesAndRoles'
)
BEGIN
    EXEC(N'ALTER TABLE [Boyahane].[Machines] ADD CONSTRAINT [CK_Machines_Status] CHECK ([Status] IN (''AVAILABLE'', ''BREAKDOWN'', ''MAINTENANCE''))');
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030120727_AddCardsStatusPausesAndRoles'
)
BEGIN
    CREATE INDEX [IX_MachineStatusLogs_MachineId_StartedAt] ON [Boyahane].[MachineStatusLogs] ([MachineId], [StartedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030120727_AddCardsStatusPausesAndRoles'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_RfidCards_Uid] ON [Boyahane].[RfidCards] ([Uid]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030120727_AddCardsStatusPausesAndRoles'
)
BEGIN
    CREATE INDEX [IX_SessionPauses_ReasonId] ON [Boyahane].[SessionPauses] ([ReasonId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030120727_AddCardsStatusPausesAndRoles'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_SessionPauses_Open] ON [Boyahane].[SessionPauses] ([SessionId]) WHERE [EndedAt] IS NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030120727_AddCardsStatusPausesAndRoles'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_StopReasons_Code] ON [Boyahane].[StopReasons] ([Code]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030120727_AddCardsStatusPausesAndRoles'
)
BEGIN
    INSERT INTO [Boyahane].[RfidCards] ([Uid], [CardType], [IsActive], [IsDeleted], [CreatedAt])
    SELECT DISTINCT o.[CardUid], 'OPERATOR', 1, 0, SYSUTCDATETIME() FROM [Boyahane].[Operators] o WHERE o.[CardUid] IS NOT NULL
    UNION
    SELECT DISTINCT b.[CardUid], 'BATCH', 1, 0, SYSUTCDATETIME() FROM [Boyahane].[Batches] b WHERE b.[CardUid] IS NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030120727_AddCardsStatusPausesAndRoles'
)
BEGIN
    SET IDENTITY_INSERT [Core].[Permissions] ON;
    EXEC(N'INSERT INTO [Core].[Permissions] ([Id], [Code], [Module], [Description])
    SELECT 1021, N''Boyahane.Batch.ResolveBreakdown'', N''Boyahane'', N''Makine arızasını ofisten kapatma''
    WHERE NOT EXISTS (SELECT 1 FROM [Core].[Permissions] p WHERE p.[Id] = 1021 OR p.[Code] = N''Boyahane.Batch.ResolveBreakdown'')');
    SET IDENTITY_INSERT [Core].[Permissions] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030120727_AddCardsStatusPausesAndRoles'
)
BEGIN
    SET IDENTITY_INSERT [Core].[Roles] ON;
    EXEC(N'INSERT INTO [Core].[Roles] ([Id], [Code], [Name], [IsSystem], [ModuleKey])
    SELECT v.[Id], v.[Code], v.[Name], v.[IsSystem], v.[ModuleKey] FROM (VALUES
    (101, N''BOYAHANE_SALES'', N''Boyahane Satış'', 1, N''Boyahane''),
    (102, N''BOYAHANE_RECEIVING'', N''Boyahane Mal Kabul ve Kalite'', 1, N''Boyahane''),
    (103, N''BOYAHANE_LAB'', N''Boyahane Laboratuvar'', 1, N''Boyahane''),
    (104, N''BOYAHANE_PLANNER'', N''Boyahane Üretim Planlama'', 1, N''Boyahane''),
    (105, N''BOYAHANE_MANAGER'', N''Boyahane Yöneticisi'', 1, N''Boyahane'')
    ) v([Id], [Code], [Name], [IsSystem], [ModuleKey])
    WHERE NOT EXISTS (SELECT 1 FROM [Core].[Roles] r WHERE r.[Id] = v.[Id] OR r.[Code] = v.[Code])');
    SET IDENTITY_INSERT [Core].[Roles] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030120727_AddCardsStatusPausesAndRoles'
)
BEGIN
    INSERT INTO [Core].[RolePermissions] ([RoleId], [PermissionId])
    SELECT r.[Id], p.[Id] FROM (VALUES
    (N'BOYAHANE_SALES', N'Boyahane.View'),
    (N'BOYAHANE_SALES', N'Boyahane.Customer.View'),
    (N'BOYAHANE_SALES', N'Boyahane.Customer.Manage'),
    (N'BOYAHANE_SALES', N'Boyahane.Material.View'),
    (N'BOYAHANE_SALES', N'Boyahane.Order.View'),
    (N'BOYAHANE_SALES', N'Boyahane.Order.Manage'),
    (N'BOYAHANE_SALES', N'Boyahane.Receipt.View'),
    (N'BOYAHANE_SALES', N'Boyahane.Inspection.View'),
    (N'BOYAHANE_SALES', N'Boyahane.Inspection.CustomerDecision'),
    (N'BOYAHANE_SALES', N'Boyahane.Lab.View'),
    (N'BOYAHANE_SALES', N'Boyahane.Lab.CustomerDecision'),
    (N'BOYAHANE_SALES', N'Boyahane.Batch.View'),
    (N'BOYAHANE_SALES', N'Pricing.View'),
    (N'BOYAHANE_RECEIVING', N'Boyahane.View'),
    (N'BOYAHANE_RECEIVING', N'Boyahane.Customer.View'),
    (N'BOYAHANE_RECEIVING', N'Boyahane.Material.View'),
    (N'BOYAHANE_RECEIVING', N'Boyahane.Material.Manage'),
    (N'BOYAHANE_RECEIVING', N'Boyahane.Order.View'),
    (N'BOYAHANE_RECEIVING', N'Boyahane.Receipt.View'),
    (N'BOYAHANE_RECEIVING', N'Boyahane.Receipt.Manage'),
    (N'BOYAHANE_RECEIVING', N'Boyahane.Inspection.View'),
    (N'BOYAHANE_RECEIVING', N'Boyahane.Inspection.Manage'),
    (N'BOYAHANE_RECEIVING', N'Boyahane.Batch.View'),
    (N'BOYAHANE_LAB', N'Boyahane.View'),
    (N'BOYAHANE_LAB', N'Boyahane.Customer.View'),
    (N'BOYAHANE_LAB', N'Boyahane.Material.View'),
    (N'BOYAHANE_LAB', N'Boyahane.Order.View'),
    (N'BOYAHANE_LAB', N'Boyahane.Lab.View'),
    (N'BOYAHANE_LAB', N'Boyahane.Lab.Manage'),
    (N'BOYAHANE_LAB', N'Boyahane.Lab.CustomerDecision'),
    (N'BOYAHANE_LAB', N'Boyahane.Batch.View'),
    (N'BOYAHANE_PLANNER', N'Boyahane.View'),
    (N'BOYAHANE_PLANNER', N'Boyahane.Customer.View'),
    (N'BOYAHANE_PLANNER', N'Boyahane.Material.View'),
    (N'BOYAHANE_PLANNER', N'Boyahane.Order.View'),
    (N'BOYAHANE_PLANNER', N'Boyahane.Order.Manage'),
    (N'BOYAHANE_PLANNER', N'Boyahane.Receipt.View'),
    (N'BOYAHANE_PLANNER', N'Boyahane.Inspection.View'),
    (N'BOYAHANE_PLANNER', N'Boyahane.Lab.View'),
    (N'BOYAHANE_PLANNER', N'Boyahane.Batch.View'),
    (N'BOYAHANE_PLANNER', N'Boyahane.Batch.Manage'),
    (N'BOYAHANE_PLANNER', N'Boyahane.Floor.View'),
    (N'BOYAHANE_MANAGER', N'Boyahane.View'),
    (N'BOYAHANE_MANAGER', N'Boyahane.Customer.View'),
    (N'BOYAHANE_MANAGER', N'Boyahane.Customer.Manage'),
    (N'BOYAHANE_MANAGER', N'Boyahane.Material.View'),
    (N'BOYAHANE_MANAGER', N'Boyahane.Material.Manage'),
    (N'BOYAHANE_MANAGER', N'Boyahane.Receipt.View'),
    (N'BOYAHANE_MANAGER', N'Boyahane.Receipt.Manage'),
    (N'BOYAHANE_MANAGER', N'Boyahane.Inspection.View'),
    (N'BOYAHANE_MANAGER', N'Boyahane.Inspection.Manage'),
    (N'BOYAHANE_MANAGER', N'Boyahane.Inspection.CustomerDecision'),
    (N'BOYAHANE_MANAGER', N'Boyahane.Order.View'),
    (N'BOYAHANE_MANAGER', N'Boyahane.Order.Manage'),
    (N'BOYAHANE_MANAGER', N'Boyahane.Lab.View'),
    (N'BOYAHANE_MANAGER', N'Boyahane.Lab.Manage'),
    (N'BOYAHANE_MANAGER', N'Boyahane.Lab.CustomerDecision'),
    (N'BOYAHANE_MANAGER', N'Boyahane.Batch.View'),
    (N'BOYAHANE_MANAGER', N'Boyahane.Batch.Manage'),
    (N'BOYAHANE_MANAGER', N'Boyahane.Batch.CapacityOverride'),
    (N'BOYAHANE_MANAGER', N'Boyahane.Floor.View'),
    (N'BOYAHANE_MANAGER', N'Boyahane.Floor.Manage'),
    (N'BOYAHANE_MANAGER', N'Boyahane.Batch.ResolveBreakdown'),
    (N'BOYAHANE_MANAGER', N'Pricing.View'),
    (N'ADMIN', N'Boyahane.Batch.ResolveBreakdown')
    ) v([RoleCode], [PermissionCode])
    JOIN [Core].[Roles] r ON r.[Code] = v.[RoleCode]
    JOIN [Core].[Permissions] p ON p.[Code] = v.[PermissionCode]
    WHERE NOT EXISTS (SELECT 1 FROM [Core].[RolePermissions] rp WHERE rp.[RoleId] = r.[Id] AND rp.[PermissionId] = p.[Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030120727_AddCardsStatusPausesAndRoles'
)
BEGIN
    INSERT INTO [Boyahane].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261030120727_AddCardsStatusPausesAndRoles', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030130713_AddOperationsAndRoutes'
)
BEGIN
    ALTER TABLE [Boyahane].[BatchSteps] DROP CONSTRAINT [CK_BatchSteps_StepType];
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030130713_AddOperationsAndRoutes'
)
BEGIN
    ALTER TABLE [Boyahane].[BatchSteps] ADD [OperationId] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030130713_AddOperationsAndRoutes'
)
BEGIN
    ALTER TABLE [Boyahane].[Batches] ADD [RouteId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030130713_AddOperationsAndRoutes'
)
BEGIN
    CREATE TABLE [Boyahane].[Operations] (
        [Id] int NOT NULL IDENTITY,
        [Code] nvarchar(30) NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [Description] nvarchar(500) NULL,
        [Behavior] nvarchar(30) NOT NULL,
        [NeedsMachine] bit NOT NULL,
        [StandardMinutes] decimal(10,2) NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_Operations] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Operations_Behavior] CHECK ([Behavior] IN ('STANDARD', 'DYEING', 'COLOR_CHECK', 'FINAL_QUALITY'))
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030130713_AddOperationsAndRoutes'
)
BEGIN
    CREATE TABLE [Boyahane].[Routes] (
        [Id] int NOT NULL IDENTITY,
        [Code] nvarchar(30) NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [ProcessType] nvarchar(30) NOT NULL,
        [IsDefault] bit NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_Routes] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Routes_ProcessType] CHECK ([ProcessType] IN ('FABRIC_DYEING', 'YARN_DYEING', 'FINISHING', 'PRINTING'))
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030130713_AddOperationsAndRoutes'
)
BEGIN
    CREATE TABLE [Boyahane].[MachineOperations] (
        [MachineId] int NOT NULL,
        [OperationId] int NOT NULL,
        CONSTRAINT [PK_MachineOperations] PRIMARY KEY ([MachineId], [OperationId]),
        CONSTRAINT [FK_MachineOperations_Machines_MachineId] FOREIGN KEY ([MachineId]) REFERENCES [Boyahane].[Machines] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_MachineOperations_Operations_OperationId] FOREIGN KEY ([OperationId]) REFERENCES [Boyahane].[Operations] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030130713_AddOperationsAndRoutes'
)
BEGIN
    CREATE TABLE [Boyahane].[RouteSteps] (
        [Id] int NOT NULL IDENTITY,
        [RouteId] int NOT NULL,
        [Sequence] int NOT NULL,
        [OperationId] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_RouteSteps] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RouteSteps_Operations_OperationId] FOREIGN KEY ([OperationId]) REFERENCES [Boyahane].[Operations] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_RouteSteps_Routes_RouteId] FOREIGN KEY ([RouteId]) REFERENCES [Boyahane].[Routes] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030130713_AddOperationsAndRoutes'
)
BEGIN
    CREATE INDEX [IX_BatchSteps_OperationId] ON [Boyahane].[BatchSteps] ([OperationId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030130713_AddOperationsAndRoutes'
)
BEGIN
    CREATE INDEX [IX_Batches_RouteId] ON [Boyahane].[Batches] ([RouteId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030130713_AddOperationsAndRoutes'
)
BEGIN
    CREATE INDEX [IX_MachineOperations_OperationId] ON [Boyahane].[MachineOperations] ([OperationId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030130713_AddOperationsAndRoutes'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Operations_Code] ON [Boyahane].[Operations] ([Code]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030130713_AddOperationsAndRoutes'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Routes_Code] ON [Boyahane].[Routes] ([Code]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030130713_AddOperationsAndRoutes'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_Routes_Default] ON [Boyahane].[Routes] ([ProcessType]) WHERE [IsDefault] = 1 AND [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030130713_AddOperationsAndRoutes'
)
BEGIN
    CREATE INDEX [IX_RouteSteps_OperationId] ON [Boyahane].[RouteSteps] ([OperationId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030130713_AddOperationsAndRoutes'
)
BEGIN
    CREATE UNIQUE INDEX [IX_RouteSteps_RouteId_Sequence] ON [Boyahane].[RouteSteps] ([RouteId], [Sequence]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030130713_AddOperationsAndRoutes'
)
BEGIN
    INSERT INTO [Boyahane].[Operations] ([Code], [Name], [Behavior], [NeedsMachine], [IsActive], [IsDeleted], [CreatedAt])
    SELECT v.[Code], v.[Name], v.[Behavior], v.[NeedsMachine], 1, 0, SYSUTCDATETIME() FROM (VALUES
    (N'ON-TERBIYE', N'Ön terbiye', 'STANDARD', 1),
    (N'BOYAMA', N'Boyama', 'DYEING', 1),
    (N'RENK-KONTROL', N'Renk kontrolü', 'COLOR_CHECK', 0),
    (N'YIKAMA-KURUTMA', N'Yıkama-kurutma', 'STANDARD', 1),
    (N'TERBIYE-APRE', N'Terbiye-apre', 'STANDARD', 1),
    (N'BASKI', N'Baskı', 'STANDARD', 1),
    (N'FINAL-KALITE', N'Final kalite', 'FINAL_QUALITY', 0),
    (N'PAKETLEME', N'Paketleme', 'STANDARD', 0)
    ) v([Code], [Name], [Behavior], [NeedsMachine]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030130713_AddOperationsAndRoutes'
)
BEGIN
    INSERT INTO [Boyahane].[Routes] ([Code], [Name], [ProcessType], [IsDefault], [IsActive], [IsDeleted], [CreatedAt])
    SELECT v.[Code], v.[Name], v.[ProcessType], 1, 1, 0, SYSUTCDATETIME() FROM (VALUES
    (N'KUMAS-BOYAMA', N'Kumaş boyama', 'FABRIC_DYEING'),
    (N'IPLIK-BOYAMA', N'İplik boyama', 'YARN_DYEING'),
    (N'BASKI', N'Baskı', 'PRINTING'),
    (N'TERBIYE', N'Terbiye', 'FINISHING')
    ) v([Code], [Name], [ProcessType]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030130713_AddOperationsAndRoutes'
)
BEGIN
    INSERT INTO [Boyahane].[RouteSteps] ([RouteId], [Sequence], [OperationId], [CreatedAt])
    SELECT r.[Id], v.[Seq], o.[Id], SYSUTCDATETIME() FROM (VALUES
    (N'KUMAS-BOYAMA', 1, N'ON-TERBIYE'),
    (N'KUMAS-BOYAMA', 2, N'BOYAMA'),
    (N'KUMAS-BOYAMA', 3, N'RENK-KONTROL'),
    (N'KUMAS-BOYAMA', 4, N'YIKAMA-KURUTMA'),
    (N'KUMAS-BOYAMA', 5, N'TERBIYE-APRE'),
    (N'KUMAS-BOYAMA', 6, N'FINAL-KALITE'),
    (N'KUMAS-BOYAMA', 7, N'PAKETLEME'),
    (N'IPLIK-BOYAMA', 1, N'BOYAMA'),
    (N'IPLIK-BOYAMA', 2, N'RENK-KONTROL'),
    (N'IPLIK-BOYAMA', 3, N'YIKAMA-KURUTMA'),
    (N'IPLIK-BOYAMA', 4, N'FINAL-KALITE'),
    (N'IPLIK-BOYAMA', 5, N'PAKETLEME'),
    (N'BASKI', 1, N'BASKI'),
    (N'BASKI', 2, N'YIKAMA-KURUTMA'),
    (N'BASKI', 3, N'FINAL-KALITE'),
    (N'BASKI', 4, N'PAKETLEME'),
    (N'TERBIYE', 1, N'TERBIYE-APRE'),
    (N'TERBIYE', 2, N'FINAL-KALITE'),
    (N'TERBIYE', 3, N'PAKETLEME')
    ) v([RouteCode], [Seq], [OperationCode])
    JOIN [Boyahane].[Routes] r ON r.[Code] = v.[RouteCode]
    JOIN [Boyahane].[Operations] o ON o.[Code] = v.[OperationCode];
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030130713_AddOperationsAndRoutes'
)
BEGIN
    EXEC(N'UPDATE s SET s.[OperationId] = o.[Id]
    FROM [Boyahane].[BatchSteps] s
    JOIN (VALUES (''PRETREATMENT'', N''ON-TERBIYE''), (''DYEING'', N''BOYAMA''), (''COLOR_CHECK'', N''RENK-KONTROL''), (''WASH_DRY'', N''YIKAMA-KURUTMA''), (''FINISHING'', N''TERBIYE-APRE''), (''PRINTING'', N''BASKI''), (''FINAL_QUALITY'', N''FINAL-KALITE''), (''PACKAGING'', N''PAKETLEME'')) m([StepType], [Code]) ON m.[StepType] = s.[StepType]
    JOIN [Boyahane].[Operations] o ON o.[Code] = m.[Code];');
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030130713_AddOperationsAndRoutes'
)
BEGIN
    EXEC(N'INSERT INTO [Boyahane].[MachineOperations] ([MachineId], [OperationId])
    SELECT m.[Id], o.[Id] FROM [Boyahane].[Machines] m
    JOIN [Boyahane].[Operations] o ON (o.[Code] = N''ON-TERBIYE'' AND m.[CanPretreat] = 1)
      OR (o.[Code] = N''BOYAMA'' AND m.[CanDye] = 1)
      OR (o.[Code] = N''YIKAMA-KURUTMA'' AND m.[CanWashDry] = 1)
      OR (o.[Code] = N''TERBIYE-APRE'' AND m.[CanFinish] = 1)
      OR (o.[Code] = N''BASKI'' AND m.[CanPrint] = 1);');
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030130713_AddOperationsAndRoutes'
)
BEGIN
    UPDATE b SET b.[RouteId] = r.[Id]
    FROM [Boyahane].[Batches] b
    CROSS APPLY (SELECT TOP 1 l.[ProcessType] FROM [Boyahane].[BatchItems] i JOIN [Boyahane].[OrderLines] l ON l.[Id] = i.[OrderLineId] WHERE i.[BatchId] = b.[Id] ORDER BY i.[Id]) p
    JOIN [Boyahane].[Routes] r ON r.[ProcessType] = p.[ProcessType] AND r.[IsDefault] = 1;
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030130713_AddOperationsAndRoutes'
)
BEGIN
    SET IDENTITY_INSERT [Core].[Permissions] ON;
    EXEC(N'INSERT INTO [Core].[Permissions] ([Id], [Code], [Module], [Description])
    SELECT v.[Id], v.[Code], v.[Module], v.[Description] FROM (VALUES
    (1022, N''Boyahane.Operation.View'', N''Boyahane'', N''Operasyonları ve rotaları görüntüleme''),
    (1023, N''Boyahane.Operation.Manage'', N''Boyahane'', N''Operasyon ve rota tanımları''),
    (1024, N''Boyahane.Report.View'', N''Boyahane'', N''Boyahane raporları'')
    ) v([Id], [Code], [Module], [Description])
    WHERE NOT EXISTS (SELECT 1 FROM [Core].[Permissions] p WHERE p.[Id] = v.[Id] OR p.[Code] = v.[Code])');
    SET IDENTITY_INSERT [Core].[Permissions] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030130713_AddOperationsAndRoutes'
)
BEGIN
    INSERT INTO [Core].[RolePermissions] ([RoleId], [PermissionId])
    SELECT r.[Id], p.[Id] FROM (VALUES
    (N'ADMIN', N'Boyahane.Operation.View'),
    (N'ADMIN', N'Boyahane.Operation.Manage'),
    (N'ADMIN', N'Boyahane.Report.View'),
    (N'BOYAHANE_MANAGER', N'Boyahane.Operation.View'),
    (N'BOYAHANE_MANAGER', N'Boyahane.Operation.Manage'),
    (N'BOYAHANE_MANAGER', N'Boyahane.Report.View'),
    (N'BOYAHANE_PLANNER', N'Boyahane.Operation.View'),
    (N'BOYAHANE_PLANNER', N'Boyahane.Operation.Manage'),
    (N'BOYAHANE_PLANNER', N'Boyahane.Report.View'),
    (N'BOYAHANE_SALES', N'Boyahane.Report.View')
    ) v([RoleCode], [PermissionCode])
    JOIN [Core].[Roles] r ON r.[Code] = v.[RoleCode]
    JOIN [Core].[Permissions] p ON p.[Code] = v.[PermissionCode]
    WHERE NOT EXISTS (SELECT 1 FROM [Core].[RolePermissions] rp WHERE rp.[RoleId] = r.[Id] AND rp.[PermissionId] = p.[Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030130713_AddOperationsAndRoutes'
)
BEGIN
    DECLARE @var3 nvarchar(max);
    SELECT @var3 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Boyahane].[BatchSteps]') AND [c].[name] = N'StepType');
    IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [Boyahane].[BatchSteps] DROP CONSTRAINT ' + @var3 + ';');
    ALTER TABLE [Boyahane].[BatchSteps] DROP COLUMN [StepType];
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030130713_AddOperationsAndRoutes'
)
BEGIN
    DECLARE @var4 nvarchar(max);
    SELECT @var4 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Boyahane].[Machines]') AND [c].[name] = N'CanPretreat');
    IF @var4 IS NOT NULL EXEC(N'ALTER TABLE [Boyahane].[Machines] DROP CONSTRAINT ' + @var4 + ';');
    ALTER TABLE [Boyahane].[Machines] DROP COLUMN [CanPretreat];
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030130713_AddOperationsAndRoutes'
)
BEGIN
    DECLARE @var5 nvarchar(max);
    SELECT @var5 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Boyahane].[Machines]') AND [c].[name] = N'CanDye');
    IF @var5 IS NOT NULL EXEC(N'ALTER TABLE [Boyahane].[Machines] DROP CONSTRAINT ' + @var5 + ';');
    ALTER TABLE [Boyahane].[Machines] DROP COLUMN [CanDye];
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030130713_AddOperationsAndRoutes'
)
BEGIN
    DECLARE @var6 nvarchar(max);
    SELECT @var6 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Boyahane].[Machines]') AND [c].[name] = N'CanWashDry');
    IF @var6 IS NOT NULL EXEC(N'ALTER TABLE [Boyahane].[Machines] DROP CONSTRAINT ' + @var6 + ';');
    ALTER TABLE [Boyahane].[Machines] DROP COLUMN [CanWashDry];
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030130713_AddOperationsAndRoutes'
)
BEGIN
    DECLARE @var7 nvarchar(max);
    SELECT @var7 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Boyahane].[Machines]') AND [c].[name] = N'CanFinish');
    IF @var7 IS NOT NULL EXEC(N'ALTER TABLE [Boyahane].[Machines] DROP CONSTRAINT ' + @var7 + ';');
    ALTER TABLE [Boyahane].[Machines] DROP COLUMN [CanFinish];
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030130713_AddOperationsAndRoutes'
)
BEGIN
    DECLARE @var8 nvarchar(max);
    SELECT @var8 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Boyahane].[Machines]') AND [c].[name] = N'CanPrint');
    IF @var8 IS NOT NULL EXEC(N'ALTER TABLE [Boyahane].[Machines] DROP CONSTRAINT ' + @var8 + ';');
    ALTER TABLE [Boyahane].[Machines] DROP COLUMN [CanPrint];
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030130713_AddOperationsAndRoutes'
)
BEGIN
    ALTER TABLE [Boyahane].[Batches] ADD CONSTRAINT [FK_Batches_Routes_RouteId] FOREIGN KEY ([RouteId]) REFERENCES [Boyahane].[Routes] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030130713_AddOperationsAndRoutes'
)
BEGIN
    ALTER TABLE [Boyahane].[BatchSteps] ADD CONSTRAINT [FK_BatchSteps_Operations_OperationId] FOREIGN KEY ([OperationId]) REFERENCES [Boyahane].[Operations] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030130713_AddOperationsAndRoutes'
)
BEGIN
    INSERT INTO [Boyahane].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261030130713_AddOperationsAndRoutes', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030190218_AddFinalMeasureAndBillingUnit'
)
BEGIN
    ALTER TABLE [Boyahane].[OrderLines] ADD [BillingUnit] nvarchar(30) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030190218_AddFinalMeasureAndBillingUnit'
)
BEGIN
    ALTER TABLE [Boyahane].[Customers] ADD [BillingUnit] nvarchar(30) NOT NULL DEFAULT N'ORDER_UNIT';
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030190218_AddFinalMeasureAndBillingUnit'
)
BEGIN
    ALTER TABLE [Boyahane].[BatchItems] ADD [FinalKilograms] decimal(12,2) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030190218_AddFinalMeasureAndBillingUnit'
)
BEGIN
    ALTER TABLE [Boyahane].[BatchItems] ADD [FinalMeters] decimal(12,2) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030190218_AddFinalMeasureAndBillingUnit'
)
BEGIN
    INSERT INTO [Boyahane].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261030190218_AddFinalMeasureAndBillingUnit', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030192023_AddPackingLists'
)
BEGIN
    CREATE TABLE [Boyahane].[PackingLists] (
        [Id] int NOT NULL IDENTITY,
        [PackingNo] nvarchar(30) NOT NULL,
        [OrderId] int NOT NULL,
        [PackingDate] date NOT NULL,
        [Notes] nvarchar(1000) NULL,
        [Status] nvarchar(30) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_PackingLists] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_PackingLists_Status] CHECK ([Status] IN ('DRAFT', 'CONFIRMED', 'CANCELLED')),
        CONSTRAINT [FK_PackingLists_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Boyahane].[Orders] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030192023_AddPackingLists'
)
BEGIN
    CREATE TABLE [Boyahane].[PackingItems] (
        [Id] int NOT NULL IDENTITY,
        [PackingListId] int NOT NULL,
        [PackageNo] nvarchar(40) NOT NULL,
        [BatchItemId] int NOT NULL,
        [Kilograms] decimal(12,2) NOT NULL,
        [Meters] decimal(12,2) NULL,
        [BoxCount] int NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_PackingItems] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_PackingItems_BoxCount] CHECK ([BoxCount] >= 1),
        CONSTRAINT [CK_PackingItems_Kilograms] CHECK ([Kilograms] > 0),
        CONSTRAINT [FK_PackingItems_BatchItems_BatchItemId] FOREIGN KEY ([BatchItemId]) REFERENCES [Boyahane].[BatchItems] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PackingItems_PackingLists_PackingListId] FOREIGN KEY ([PackingListId]) REFERENCES [Boyahane].[PackingLists] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030192023_AddPackingLists'
)
BEGIN
    CREATE UNIQUE INDEX [IX_PackingItems_PackageNo] ON [Boyahane].[PackingItems] ([PackageNo]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030192023_AddPackingLists'
)
BEGIN
    CREATE INDEX [IX_PackingItems_PackingListId] ON [Boyahane].[PackingItems] ([PackingListId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030192023_AddPackingLists'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_PackingItems_Active] ON [Boyahane].[PackingItems] ([BatchItemId]) WHERE [IsActive] = 1');
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030192023_AddPackingLists'
)
BEGIN
    CREATE INDEX [IX_PackingLists_OrderId] ON [Boyahane].[PackingLists] ([OrderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030192023_AddPackingLists'
)
BEGIN
    CREATE UNIQUE INDEX [IX_PackingLists_PackingNo] ON [Boyahane].[PackingLists] ([PackingNo]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030192023_AddPackingLists'
)
BEGIN
    SET IDENTITY_INSERT [Core].[Permissions] ON;
    EXEC(N'INSERT INTO [Core].[Permissions] ([Id], [Code], [Module], [Description])
    SELECT v.[Id], v.[Code], v.[Module], v.[Description] FROM (VALUES
    (1025, N''Boyahane.Packing.View'', N''Boyahane'', N''Çeki listelerini görüntüleme''),
    (1026, N''Boyahane.Packing.Manage'', N''Boyahane'', N''Çeki listesi hazırlama ve onaylama'')
    ) v([Id], [Code], [Module], [Description])
    WHERE NOT EXISTS (SELECT 1 FROM [Core].[Permissions] p WHERE p.[Id] = v.[Id] OR p.[Code] = v.[Code])');
    SET IDENTITY_INSERT [Core].[Permissions] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030192023_AddPackingLists'
)
BEGIN
    INSERT INTO [Core].[RolePermissions] ([RoleId], [PermissionId])
    SELECT r.[Id], p.[Id] FROM [Core].[Roles] r
    CROSS JOIN [Core].[Permissions] p
    WHERE r.[Code] IN (N'ADMIN', N'BOYAHANE_MANAGER') AND p.[Id] IN (1025, 1026)
      AND NOT EXISTS (SELECT 1 FROM [Core].[RolePermissions] rp WHERE rp.[RoleId] = r.[Id] AND rp.[PermissionId] = p.[Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030192023_AddPackingLists'
)
BEGIN
    INSERT INTO [Boyahane].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261030192023_AddPackingLists', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030193524_AddDespatchesAndInvoices'
)
BEGIN
    ALTER TABLE [Boyahane].[PackingLists] ADD [DespatchId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030193524_AddDespatchesAndInvoices'
)
BEGIN
    ALTER TABLE [Boyahane].[Customers] ADD [VatRate] decimal(5,2) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030193524_AddDespatchesAndInvoices'
)
BEGIN
    CREATE TABLE [Boyahane].[Invoices] (
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
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_Invoices] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Invoices_Status] CHECK ([Status] IN ('DRAFT', 'CONFIRMED', 'CANCELLED')),
        CONSTRAINT [CK_Invoices_VatRate] CHECK ([VatRate] IS NULL OR [VatRate] BETWEEN 0 AND 100),
        CONSTRAINT [CK_Invoices_Withholding] CHECK ([WithholdingPercent] IS NULL OR [WithholdingPercent] BETWEEN 0 AND 100),
        CONSTRAINT [FK_Invoices_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Boyahane].[Customers] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030193524_AddDespatchesAndInvoices'
)
BEGIN
    CREATE TABLE [Boyahane].[Despatches] (
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
        CONSTRAINT [FK_Despatches_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Boyahane].[Customers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Despatches_Invoices_InvoiceId] FOREIGN KEY ([InvoiceId]) REFERENCES [Boyahane].[Invoices] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030193524_AddDespatchesAndInvoices'
)
BEGIN
    CREATE TABLE [Boyahane].[InvoiceLines] (
        [Id] int NOT NULL IDENTITY,
        [InvoiceId] int NOT NULL,
        [OrderLineId] int NOT NULL,
        [Description] nvarchar(300) NOT NULL,
        [Quantity] decimal(14,2) NOT NULL,
        [Unit] nvarchar(30) NOT NULL,
        [UnitPrice] decimal(14,4) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_InvoiceLines] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_InvoiceLines_Unit] CHECK ([Unit] IN ('PIECE', 'METER', 'KILOGRAM', 'ROLL', 'BOX')),
        CONSTRAINT [FK_InvoiceLines_Invoices_InvoiceId] FOREIGN KEY ([InvoiceId]) REFERENCES [Boyahane].[Invoices] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_InvoiceLines_OrderLines_OrderLineId] FOREIGN KEY ([OrderLineId]) REFERENCES [Boyahane].[OrderLines] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030193524_AddDespatchesAndInvoices'
)
BEGIN
    CREATE INDEX [IX_PackingLists_DespatchId] ON [Boyahane].[PackingLists] ([DespatchId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030193524_AddDespatchesAndInvoices'
)
BEGIN
    CREATE INDEX [IX_Despatches_CustomerId] ON [Boyahane].[Despatches] ([CustomerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030193524_AddDespatchesAndInvoices'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Despatches_DespatchNo] ON [Boyahane].[Despatches] ([DespatchNo]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030193524_AddDespatchesAndInvoices'
)
BEGIN
    CREATE INDEX [IX_Despatches_InvoiceId] ON [Boyahane].[Despatches] ([InvoiceId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030193524_AddDespatchesAndInvoices'
)
BEGIN
    CREATE INDEX [IX_InvoiceLines_InvoiceId] ON [Boyahane].[InvoiceLines] ([InvoiceId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030193524_AddDespatchesAndInvoices'
)
BEGIN
    CREATE INDEX [IX_InvoiceLines_OrderLineId] ON [Boyahane].[InvoiceLines] ([OrderLineId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030193524_AddDespatchesAndInvoices'
)
BEGIN
    CREATE INDEX [IX_Invoices_CustomerId] ON [Boyahane].[Invoices] ([CustomerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030193524_AddDespatchesAndInvoices'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Invoices_InvoiceNo] ON [Boyahane].[Invoices] ([InvoiceNo]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030193524_AddDespatchesAndInvoices'
)
BEGIN
    ALTER TABLE [Boyahane].[PackingLists] ADD CONSTRAINT [FK_PackingLists_Despatches_DespatchId] FOREIGN KEY ([DespatchId]) REFERENCES [Boyahane].[Despatches] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030193524_AddDespatchesAndInvoices'
)
BEGIN
    SET IDENTITY_INSERT [Core].[Permissions] ON;
    EXEC(N'INSERT INTO [Core].[Permissions] ([Id], [Code], [Module], [Description])
    SELECT v.[Id], v.[Code], v.[Module], v.[Description] FROM (VALUES
    (1027, N''Boyahane.Shipment.View'', N''Boyahane'', N''İrsaliyeleri görüntüleme''),
    (1028, N''Boyahane.Shipment.Manage'', N''Boyahane'', N''İrsaliye hazırlama ve onaylama''),
    (1029, N''Boyahane.Invoice.View'', N''Boyahane'', N''Faturaları görüntüleme''),
    (1030, N''Boyahane.Invoice.Manage'', N''Boyahane'', N''Fatura hazırlama ve onaylama'')
    ) v([Id], [Code], [Module], [Description])
    WHERE NOT EXISTS (SELECT 1 FROM [Core].[Permissions] p WHERE p.[Id] = v.[Id] OR p.[Code] = v.[Code])');
    SET IDENTITY_INSERT [Core].[Permissions] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030193524_AddDespatchesAndInvoices'
)
BEGIN
    INSERT INTO [Core].[RolePermissions] ([RoleId], [PermissionId])
    SELECT r.[Id], p.[Id] FROM (VALUES
    (N'ADMIN', 1027), (N'ADMIN', 1028), (N'ADMIN', 1029), (N'ADMIN', 1030),
    (N'BOYAHANE_MANAGER', 1027), (N'BOYAHANE_MANAGER', 1028), (N'BOYAHANE_MANAGER', 1029), (N'BOYAHANE_MANAGER', 1030),
    (N'BOYAHANE_RECEIVING', 1025), (N'BOYAHANE_RECEIVING', 1026),
    (N'BOYAHANE_PLANNER', 1025), (N'BOYAHANE_PLANNER', 1026),
    (N'BOYAHANE_SALES', 1025)
    ) v([RoleCode], [PermissionId])
    JOIN [Core].[Roles] r ON r.[Code] = v.[RoleCode]
    JOIN [Core].[Permissions] p ON p.[Id] = v.[PermissionId]
    WHERE NOT EXISTS (SELECT 1 FROM [Core].[RolePermissions] rp WHERE rp.[RoleId] = r.[Id] AND rp.[PermissionId] = p.[Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030193524_AddDespatchesAndInvoices'
)
BEGIN
    INSERT INTO [Boyahane].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261030193524_AddDespatchesAndInvoices', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030200042_AddChemicalStock'
)
BEGIN
    CREATE TABLE [Boyahane].[ChemicalLots] (
        [Id] int NOT NULL IDENTITY,
        [LotNo] nvarchar(30) NOT NULL,
        [DyeChemicalId] int NOT NULL,
        [SupplierLotNo] nvarchar(50) NULL,
        [SupplierName] nvarchar(150) NULL,
        [ReceiptDate] date NOT NULL,
        [ExpiryDate] date NULL,
        [ReceivedKilograms] decimal(14,3) NOT NULL,
        [RemainingKilograms] decimal(14,3) NOT NULL,
        [UnitCost] decimal(14,4) NULL,
        [Currency] nvarchar(3) NOT NULL,
        [Notes] nvarchar(500) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_ChemicalLots] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_ChemicalLots_Received] CHECK ([ReceivedKilograms] > 0),
        CONSTRAINT [CK_ChemicalLots_Remaining] CHECK ([RemainingKilograms] >= 0),
        CONSTRAINT [FK_ChemicalLots_DyeChemicals_DyeChemicalId] FOREIGN KEY ([DyeChemicalId]) REFERENCES [Boyahane].[DyeChemicals] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030200042_AddChemicalStock'
)
BEGIN
    CREATE TABLE [Boyahane].[ChemicalMovements] (
        [Id] bigint NOT NULL IDENTITY,
        [LotId] int NOT NULL,
        [Type] nvarchar(30) NOT NULL,
        [Kilograms] decimal(14,3) NOT NULL,
        [Reference] nvarchar(200) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_ChemicalMovements] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_ChemicalMovements_Type] CHECK ([Type] IN ('RECEIPT', 'CONSUMPTION', 'ADJUSTMENT')),
        CONSTRAINT [FK_ChemicalMovements_ChemicalLots_LotId] FOREIGN KEY ([LotId]) REFERENCES [Boyahane].[ChemicalLots] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030200042_AddChemicalStock'
)
BEGIN
    CREATE TABLE [Boyahane].[ConsumptionLots] (
        [Id] bigint NOT NULL IDENTITY,
        [ConsumptionId] int NOT NULL,
        [LotId] int NOT NULL,
        [Kilograms] decimal(14,3) NOT NULL,
        CONSTRAINT [PK_ConsumptionLots] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ConsumptionLots_BatchConsumptions_ConsumptionId] FOREIGN KEY ([ConsumptionId]) REFERENCES [Boyahane].[BatchConsumptions] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ConsumptionLots_ChemicalLots_LotId] FOREIGN KEY ([LotId]) REFERENCES [Boyahane].[ChemicalLots] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030200042_AddChemicalStock'
)
BEGIN
    CREATE INDEX [IX_ChemicalLots_DyeChemicalId_ExpiryDate] ON [Boyahane].[ChemicalLots] ([DyeChemicalId], [ExpiryDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030200042_AddChemicalStock'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ChemicalLots_LotNo] ON [Boyahane].[ChemicalLots] ([LotNo]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030200042_AddChemicalStock'
)
BEGIN
    CREATE INDEX [IX_ChemicalMovements_LotId] ON [Boyahane].[ChemicalMovements] ([LotId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030200042_AddChemicalStock'
)
BEGIN
    CREATE INDEX [IX_ConsumptionLots_ConsumptionId] ON [Boyahane].[ConsumptionLots] ([ConsumptionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030200042_AddChemicalStock'
)
BEGIN
    CREATE INDEX [IX_ConsumptionLots_LotId] ON [Boyahane].[ConsumptionLots] ([LotId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030200042_AddChemicalStock'
)
BEGIN
    SET IDENTITY_INSERT [Core].[Permissions] ON;
    EXEC(N'INSERT INTO [Core].[Permissions] ([Id], [Code], [Module], [Description])
    SELECT v.[Id], v.[Code], v.[Module], v.[Description] FROM (VALUES
    (1031, N''Boyahane.ChemicalStock.View'', N''Boyahane'', N''Boya ve kimyasal stoğunu görüntüleme''),
    (1032, N''Boyahane.ChemicalStock.Manage'', N''Boyahane'', N''Boya ve kimyasal girişi, sayım düzeltmesi'')
    ) v([Id], [Code], [Module], [Description])
    WHERE NOT EXISTS (SELECT 1 FROM [Core].[Permissions] p WHERE p.[Id] = v.[Id] OR p.[Code] = v.[Code])');
    SET IDENTITY_INSERT [Core].[Permissions] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030200042_AddChemicalStock'
)
BEGIN
    INSERT INTO [Core].[RolePermissions] ([RoleId], [PermissionId])
    SELECT r.[Id], p.[Id] FROM (VALUES
    (N'ADMIN', 1031), (N'ADMIN', 1032), (N'BOYAHANE_MANAGER', 1031), (N'BOYAHANE_MANAGER', 1032),
    (N'BOYAHANE_RECEIVING', 1027), (N'BOYAHANE_RECEIVING', 1028),
    (N'BOYAHANE_PLANNER', 1027), (N'BOYAHANE_PLANNER', 1028),
    (N'BOYAHANE_SALES', 1029), (N'BOYAHANE_SALES', 1030)
    ) v([RoleCode], [PermissionId])
    JOIN [Core].[Roles] r ON r.[Code] = v.[RoleCode]
    JOIN [Core].[Permissions] p ON p.[Id] = v.[PermissionId]
    WHERE NOT EXISTS (SELECT 1 FROM [Core].[RolePermissions] rp WHERE rp.[RoleId] = r.[Id] AND rp.[PermissionId] = p.[Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030200042_AddChemicalStock'
)
BEGIN
    INSERT INTO [Boyahane].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261030200042_AddChemicalStock', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030201325_AddPurchasing'
)
BEGIN
    ALTER TABLE [Boyahane].[DyeChemicals] ADD [MinStockKilograms] decimal(14,3) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030201325_AddPurchasing'
)
BEGIN
    ALTER TABLE [Boyahane].[ChemicalLots] ADD [PurchaseOrderLineId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030201325_AddPurchasing'
)
BEGIN
    ALTER TABLE [Boyahane].[ChemicalLots] ADD [SupplierId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030201325_AddPurchasing'
)
BEGIN
    CREATE TABLE [Boyahane].[Suppliers] (
        [Id] int NOT NULL IDENTITY,
        [Code] nvarchar(30) NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [TaxNumber] nvarchar(20) NULL,
        [Phone] nvarchar(30) NULL,
        [Email] nvarchar(150) NULL,
        [Currency] nvarchar(3) NOT NULL,
        [LeadTimeDays] int NULL,
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
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030201325_AddPurchasing'
)
BEGIN
    CREATE TABLE [Boyahane].[PurchaseOrders] (
        [Id] int NOT NULL IDENTITY,
        [OrderNo] nvarchar(30) NOT NULL,
        [SupplierId] int NOT NULL,
        [OrderDate] date NOT NULL,
        [ExpectedDate] date NULL,
        [Currency] nvarchar(3) NOT NULL,
        [Notes] nvarchar(1000) NULL,
        [Status] nvarchar(30) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_PurchaseOrders] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_PurchaseOrders_Status] CHECK ([Status] IN ('DRAFT', 'CONFIRMED', 'PARTIALLY_RECEIVED', 'RECEIVED', 'CLOSED', 'CANCELLED')),
        CONSTRAINT [FK_PurchaseOrders_Suppliers_SupplierId] FOREIGN KEY ([SupplierId]) REFERENCES [Boyahane].[Suppliers] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030201325_AddPurchasing'
)
BEGIN
    CREATE TABLE [Boyahane].[PurchaseOrderLines] (
        [Id] int NOT NULL IDENTITY,
        [OrderId] int NOT NULL,
        [DyeChemicalId] int NOT NULL,
        [Kilograms] decimal(14,3) NOT NULL,
        [UnitPrice] decimal(14,4) NULL,
        [ReceivedKilograms] decimal(14,3) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_PurchaseOrderLines] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_PurchaseOrderLines_Kilograms] CHECK ([Kilograms] > 0),
        CONSTRAINT [FK_PurchaseOrderLines_DyeChemicals_DyeChemicalId] FOREIGN KEY ([DyeChemicalId]) REFERENCES [Boyahane].[DyeChemicals] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PurchaseOrderLines_PurchaseOrders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Boyahane].[PurchaseOrders] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030201325_AddPurchasing'
)
BEGIN
    CREATE INDEX [IX_ChemicalLots_PurchaseOrderLineId] ON [Boyahane].[ChemicalLots] ([PurchaseOrderLineId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030201325_AddPurchasing'
)
BEGIN
    CREATE INDEX [IX_ChemicalLots_SupplierId] ON [Boyahane].[ChemicalLots] ([SupplierId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030201325_AddPurchasing'
)
BEGIN
    CREATE INDEX [IX_PurchaseOrderLines_DyeChemicalId] ON [Boyahane].[PurchaseOrderLines] ([DyeChemicalId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030201325_AddPurchasing'
)
BEGIN
    CREATE INDEX [IX_PurchaseOrderLines_OrderId] ON [Boyahane].[PurchaseOrderLines] ([OrderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030201325_AddPurchasing'
)
BEGIN
    CREATE UNIQUE INDEX [IX_PurchaseOrders_OrderNo] ON [Boyahane].[PurchaseOrders] ([OrderNo]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030201325_AddPurchasing'
)
BEGIN
    CREATE INDEX [IX_PurchaseOrders_SupplierId] ON [Boyahane].[PurchaseOrders] ([SupplierId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030201325_AddPurchasing'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Suppliers_Code] ON [Boyahane].[Suppliers] ([Code]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030201325_AddPurchasing'
)
BEGIN
    ALTER TABLE [Boyahane].[ChemicalLots] ADD CONSTRAINT [FK_ChemicalLots_PurchaseOrderLines_PurchaseOrderLineId] FOREIGN KEY ([PurchaseOrderLineId]) REFERENCES [Boyahane].[PurchaseOrderLines] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030201325_AddPurchasing'
)
BEGIN
    ALTER TABLE [Boyahane].[ChemicalLots] ADD CONSTRAINT [FK_ChemicalLots_Suppliers_SupplierId] FOREIGN KEY ([SupplierId]) REFERENCES [Boyahane].[Suppliers] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030201325_AddPurchasing'
)
BEGIN
    SET IDENTITY_INSERT [Core].[Permissions] ON;
    EXEC(N'INSERT INTO [Core].[Permissions] ([Id], [Code], [Module], [Description])
    SELECT v.[Id], v.[Code], v.[Module], v.[Description] FROM (VALUES
    (1033, N''Boyahane.Purchase.View'', N''Boyahane'', N''Satın alma siparişlerini ve tedarikçileri görüntüleme''),
    (1034, N''Boyahane.Purchase.Manage'', N''Boyahane'', N''Satın alma siparişi, teslim alma, tedarikçi kartları'')
    ) v([Id], [Code], [Module], [Description])
    WHERE NOT EXISTS (SELECT 1 FROM [Core].[Permissions] p WHERE p.[Id] = v.[Id] OR p.[Code] = v.[Code])');
    SET IDENTITY_INSERT [Core].[Permissions] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030201325_AddPurchasing'
)
BEGIN
    INSERT INTO [Core].[RolePermissions] ([RoleId], [PermissionId])
    SELECT r.[Id], p.[Id] FROM [Core].[Roles] r
    CROSS JOIN [Core].[Permissions] p
    WHERE r.[Code] IN (N'ADMIN', N'BOYAHANE_MANAGER') AND p.[Id] IN (1033, 1034)
      AND NOT EXISTS (SELECT 1 FROM [Core].[RolePermissions] rp WHERE rp.[RoleId] = r.[Id] AND rp.[PermissionId] = p.[Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030201325_AddPurchasing'
)
BEGIN
    INSERT INTO [Boyahane].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261030201325_AddPurchasing', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030202929_AddPaymentsAndAccounts'
)
BEGIN
    CREATE TABLE [Boyahane].[Payments] (
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
        CONSTRAINT [CK_Payments_Method] CHECK ([Method] IN ('CASH', 'BANK_TRANSFER', 'CHECK')),
        CONSTRAINT [CK_Payments_Status] CHECK ([Status] IN ('ACTIVE', 'CANCELLED')),
        CONSTRAINT [FK_Payments_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Boyahane].[Customers] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030202929_AddPaymentsAndAccounts'
)
BEGIN
    CREATE TABLE [Boyahane].[PaymentAllocations] (
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
        CONSTRAINT [FK_PaymentAllocations_Invoices_InvoiceId] FOREIGN KEY ([InvoiceId]) REFERENCES [Boyahane].[Invoices] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PaymentAllocations_Payments_PaymentId] FOREIGN KEY ([PaymentId]) REFERENCES [Boyahane].[Payments] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030202929_AddPaymentsAndAccounts'
)
BEGIN
    CREATE INDEX [IX_PaymentAllocations_InvoiceId] ON [Boyahane].[PaymentAllocations] ([InvoiceId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030202929_AddPaymentsAndAccounts'
)
BEGIN
    CREATE UNIQUE INDEX [IX_PaymentAllocations_PaymentId_InvoiceId] ON [Boyahane].[PaymentAllocations] ([PaymentId], [InvoiceId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030202929_AddPaymentsAndAccounts'
)
BEGIN
    CREATE INDEX [IX_Payments_CustomerId_Currency_PaymentDate] ON [Boyahane].[Payments] ([CustomerId], [Currency], [PaymentDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030202929_AddPaymentsAndAccounts'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Payments_PaymentNo] ON [Boyahane].[Payments] ([PaymentNo]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030202929_AddPaymentsAndAccounts'
)
BEGIN
    SET IDENTITY_INSERT [Core].[Permissions] ON;
    EXEC(N'INSERT INTO [Core].[Permissions] ([Id], [Code], [Module], [Description])
    SELECT v.[Id], v.[Code], v.[Module], v.[Description] FROM (VALUES
    (1035, N''Boyahane.Account.View'', N''Boyahane'', N''Cari hesapları, ekstreyi ve tahsilatları görüntüleme''),
    (1036, N''Boyahane.Account.Manage'', N''Boyahane'', N''Tahsilat girişi, faturalara dağıtım ve iptal'')
    ) v([Id], [Code], [Module], [Description])
    WHERE NOT EXISTS (SELECT 1 FROM [Core].[Permissions] p WHERE p.[Id] = v.[Id] OR p.[Code] = v.[Code])');
    SET IDENTITY_INSERT [Core].[Permissions] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030202929_AddPaymentsAndAccounts'
)
BEGIN
    INSERT INTO [Core].[RolePermissions] ([RoleId], [PermissionId])
    SELECT r.[Id], p.[Id] FROM (VALUES
    (N'ADMIN', 1035), (N'ADMIN', 1036),
    (N'BOYAHANE_MANAGER', 1035), (N'BOYAHANE_MANAGER', 1036),
    (N'BOYAHANE_RECEIVING', 1031), (N'BOYAHANE_RECEIVING', 1032), (N'BOYAHANE_RECEIVING', 1033), (N'BOYAHANE_RECEIVING', 1034),
    (N'BOYAHANE_PLANNER', 1031), (N'BOYAHANE_PLANNER', 1033),
    (N'BOYAHANE_LAB', 1031)
    ) v([RoleCode], [PermissionId])
    JOIN [Core].[Roles] r ON r.[Code] = v.[RoleCode]
    JOIN [Core].[Permissions] p ON p.[Id] = v.[PermissionId]
    WHERE NOT EXISTS (SELECT 1 FROM [Core].[RolePermissions] rp WHERE rp.[RoleId] = r.[Id] AND rp.[PermissionId] = p.[Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030202929_AddPaymentsAndAccounts'
)
BEGIN
    INSERT INTO [Boyahane].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261030202929_AddPaymentsAndAccounts', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030205144_AddWarehousesAndOpeningBalances'
)
BEGIN
    ALTER TABLE [Boyahane].[Payments] DROP CONSTRAINT [CK_Payments_Method];
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030205144_AddWarehousesAndOpeningBalances'
)
BEGIN
    ALTER TABLE [Boyahane].[YarnLots] ADD [LocationId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030205144_AddWarehousesAndOpeningBalances'
)
BEGIN
    ALTER TABLE [Boyahane].[PackingItems] ADD [LocationId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030205144_AddWarehousesAndOpeningBalances'
)
BEGIN
    ALTER TABLE [Boyahane].[Invoices] ADD [OpeningAmount] decimal(16,2) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030205144_AddWarehousesAndOpeningBalances'
)
BEGIN
    ALTER TABLE [Boyahane].[FabricRolls] ADD [LocationId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030205144_AddWarehousesAndOpeningBalances'
)
BEGIN
    ALTER TABLE [Boyahane].[FabricReceipts] ADD [IsOpening] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030205144_AddWarehousesAndOpeningBalances'
)
BEGIN
    ALTER TABLE [Boyahane].[ChemicalLots] ADD [LocationId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030205144_AddWarehousesAndOpeningBalances'
)
BEGIN
    CREATE TABLE [Boyahane].[Warehouses] (
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
        CONSTRAINT [CK_Warehouses_Type] CHECK ([Type] IN ('RAW_FABRIC', 'CHEMICAL', 'FINISHED_GOODS', 'QUARANTINE'))
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030205144_AddWarehousesAndOpeningBalances'
)
BEGIN
    CREATE TABLE [Boyahane].[WarehouseLocations] (
        [Id] int NOT NULL IDENTITY,
        [WarehouseId] int NOT NULL,
        [Code] nvarchar(40) NOT NULL,
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
        CONSTRAINT [FK_WarehouseLocations_Warehouses_WarehouseId] FOREIGN KEY ([WarehouseId]) REFERENCES [Boyahane].[Warehouses] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030205144_AddWarehousesAndOpeningBalances'
)
BEGIN
    CREATE TABLE [Boyahane].[LocationMovements] (
        [Id] bigint NOT NULL IDENTITY,
        [GoodsType] nvarchar(30) NOT NULL,
        [GoodsId] int NOT NULL,
        [GoodsNo] nvarchar(40) NOT NULL,
        [Type] nvarchar(30) NOT NULL,
        [FromLocationId] int NULL,
        [ToLocationId] int NOT NULL,
        [Note] nvarchar(200) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_LocationMovements] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_LocationMovements_GoodsType] CHECK ([GoodsType] IN ('FABRIC_ROLL', 'YARN_LOT', 'CHEMICAL_LOT', 'PACKAGE')),
        CONSTRAINT [CK_LocationMovements_Type] CHECK ([Type] IN ('ADDRESS_CHANGE', 'TRANSFER')),
        CONSTRAINT [FK_LocationMovements_WarehouseLocations_FromLocationId] FOREIGN KEY ([FromLocationId]) REFERENCES [Boyahane].[WarehouseLocations] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_LocationMovements_WarehouseLocations_ToLocationId] FOREIGN KEY ([ToLocationId]) REFERENCES [Boyahane].[WarehouseLocations] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030205144_AddWarehousesAndOpeningBalances'
)
BEGIN
    CREATE INDEX [IX_YarnLots_LocationId] ON [Boyahane].[YarnLots] ([LocationId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030205144_AddWarehousesAndOpeningBalances'
)
BEGIN
    EXEC(N'ALTER TABLE [Boyahane].[Payments] ADD CONSTRAINT [CK_Payments_Method] CHECK ([Method] IN (''CASH'', ''BANK_TRANSFER'', ''CHECK'', ''OPENING''))');
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030205144_AddWarehousesAndOpeningBalances'
)
BEGIN
    CREATE INDEX [IX_PackingItems_LocationId] ON [Boyahane].[PackingItems] ([LocationId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030205144_AddWarehousesAndOpeningBalances'
)
BEGIN
    CREATE INDEX [IX_FabricRolls_LocationId] ON [Boyahane].[FabricRolls] ([LocationId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030205144_AddWarehousesAndOpeningBalances'
)
BEGIN
    CREATE INDEX [IX_ChemicalLots_LocationId] ON [Boyahane].[ChemicalLots] ([LocationId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030205144_AddWarehousesAndOpeningBalances'
)
BEGIN
    CREATE INDEX [IX_LocationMovements_FromLocationId] ON [Boyahane].[LocationMovements] ([FromLocationId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030205144_AddWarehousesAndOpeningBalances'
)
BEGIN
    CREATE INDEX [IX_LocationMovements_GoodsNo] ON [Boyahane].[LocationMovements] ([GoodsNo]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030205144_AddWarehousesAndOpeningBalances'
)
BEGIN
    CREATE INDEX [IX_LocationMovements_ToLocationId] ON [Boyahane].[LocationMovements] ([ToLocationId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030205144_AddWarehousesAndOpeningBalances'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_WarehouseLocations_WarehouseId_Code] ON [Boyahane].[WarehouseLocations] ([WarehouseId], [Code]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030205144_AddWarehousesAndOpeningBalances'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Warehouses_Code] ON [Boyahane].[Warehouses] ([Code]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030205144_AddWarehousesAndOpeningBalances'
)
BEGIN
    ALTER TABLE [Boyahane].[ChemicalLots] ADD CONSTRAINT [FK_ChemicalLots_WarehouseLocations_LocationId] FOREIGN KEY ([LocationId]) REFERENCES [Boyahane].[WarehouseLocations] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030205144_AddWarehousesAndOpeningBalances'
)
BEGIN
    ALTER TABLE [Boyahane].[FabricRolls] ADD CONSTRAINT [FK_FabricRolls_WarehouseLocations_LocationId] FOREIGN KEY ([LocationId]) REFERENCES [Boyahane].[WarehouseLocations] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030205144_AddWarehousesAndOpeningBalances'
)
BEGIN
    ALTER TABLE [Boyahane].[PackingItems] ADD CONSTRAINT [FK_PackingItems_WarehouseLocations_LocationId] FOREIGN KEY ([LocationId]) REFERENCES [Boyahane].[WarehouseLocations] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030205144_AddWarehousesAndOpeningBalances'
)
BEGIN
    ALTER TABLE [Boyahane].[YarnLots] ADD CONSTRAINT [FK_YarnLots_WarehouseLocations_LocationId] FOREIGN KEY ([LocationId]) REFERENCES [Boyahane].[WarehouseLocations] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030205144_AddWarehousesAndOpeningBalances'
)
BEGIN
    EXEC(N'
    INSERT INTO [Boyahane].[Warehouses] ([Code], [Name], [Type], [IsActive], [IsDeleted], [CreatedAt]) VALUES
    (N''HAM'', N''Ham Kumaş ve İplik Deposu'', N''RAW_FABRIC'', 1, 0, SYSUTCDATETIME()),
    (N''KIMYA'', N''Boya ve Kimyasal Deposu'', N''CHEMICAL'', 1, 0, SYSUTCDATETIME()),
    (N''MAMUL'', N''Mamul Deposu'', N''FINISHED_GOODS'', 1, 0, SYSUTCDATETIME()),
    (N''KARANTINA'', N''Karantina ve Red Deposu'', N''QUARANTINE'', 1, 0, SYSUTCDATETIME());
    INSERT INTO [Boyahane].[WarehouseLocations] ([WarehouseId], [Code], [Zone], [Description], [IsDefault], [IsActive], [IsDeleted], [CreatedAt])
    SELECT w.[Id], N''GENEL'', N''GENEL'', N''Adres seçilmeyen mal'', 1, 1, 0, SYSUTCDATETIME() FROM [Boyahane].[Warehouses] w;
    DECLARE @ham int = (SELECT l.[Id] FROM [Boyahane].[WarehouseLocations] l JOIN [Boyahane].[Warehouses] w ON w.[Id] = l.[WarehouseId] WHERE w.[Code] = N''HAM'');
    DECLARE @kimya int = (SELECT l.[Id] FROM [Boyahane].[WarehouseLocations] l JOIN [Boyahane].[Warehouses] w ON w.[Id] = l.[WarehouseId] WHERE w.[Code] = N''KIMYA'');
    DECLARE @mamul int = (SELECT l.[Id] FROM [Boyahane].[WarehouseLocations] l JOIN [Boyahane].[Warehouses] w ON w.[Id] = l.[WarehouseId] WHERE w.[Code] = N''MAMUL'');
    UPDATE [Boyahane].[FabricRolls] SET [LocationId] = @ham;
    UPDATE [Boyahane].[YarnLots] SET [LocationId] = @ham;
    UPDATE [Boyahane].[ChemicalLots] SET [LocationId] = @kimya;
    UPDATE i SET [LocationId] = @mamul FROM [Boyahane].[PackingItems] i JOIN [Boyahane].[PackingLists] p ON p.[Id] = i.[PackingListId] WHERE i.[IsActive] = 1 AND p.[Status] = N''CONFIRMED'';
    ');
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030205144_AddWarehousesAndOpeningBalances'
)
BEGIN
    UPDATE [Core].[Permissions] SET [Description] = N'Depoyu, adresleri ve boya-kimyasal stoğunu görüntüleme' WHERE [Id] = 1031;
    UPDATE [Core].[Permissions] SET [Description] = N'Depo ve adres tanımları, mal taşıma, boya-kimyasal girişi ve sayım düzeltmesi' WHERE [Id] = 1032;
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030205144_AddWarehousesAndOpeningBalances'
)
BEGIN
    INSERT INTO [Core].[RolePermissions] ([RoleId], [PermissionId])
    SELECT r.[Id], p.[Id] FROM [Core].[Roles] r
    CROSS JOIN [Core].[Permissions] p
    WHERE r.[Code] = N'BOYAHANE_SALES' AND p.[Id] IN (1035, 1036)
      AND NOT EXISTS (SELECT 1 FROM [Core].[RolePermissions] rp WHERE rp.[RoleId] = r.[Id] AND rp.[PermissionId] = p.[Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030205144_AddWarehousesAndOpeningBalances'
)
BEGIN
    INSERT INTO [Boyahane].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261030205144_AddWarehousesAndOpeningBalances', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030225038_AddFinalControlsAndGoodsReturns'
)
BEGIN
    ALTER TABLE [Boyahane].[PackingLists] ADD [SaleCustomerId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030225038_AddFinalControlsAndGoodsReturns'
)
BEGIN
    ALTER TABLE [Boyahane].[BatchItems] ADD [FinalOverrideBy] nvarchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030225038_AddFinalControlsAndGoodsReturns'
)
BEGIN
    ALTER TABLE [Boyahane].[BatchItems] ADD [FinalOverrideNote] nvarchar(500) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030225038_AddFinalControlsAndGoodsReturns'
)
BEGIN
    CREATE TABLE [Boyahane].[GoodsReturns] (
        [Id] int NOT NULL IDENTITY,
        [ReturnNo] nvarchar(30) NOT NULL,
        [Kind] nvarchar(30) NOT NULL,
        [CustomerId] int NULL,
        [ReturnDate] date NOT NULL,
        [VehiclePlate] nvarchar(20) NULL,
        [DriverName] nvarchar(100) NULL,
        [DeliveryAddress] nvarchar(500) NULL,
        [Notes] nvarchar(1000) NULL,
        [Status] nvarchar(30) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_GoodsReturns] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_GoodsReturns_Kind] CHECK ([Kind] IN ('CUSTOMER_RETURN', 'SCRAP')),
        CONSTRAINT [CK_GoodsReturns_Status] CHECK ([Status] IN ('DRAFT', 'CONFIRMED', 'CANCELLED')),
        CONSTRAINT [FK_GoodsReturns_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Boyahane].[Customers] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030225038_AddFinalControlsAndGoodsReturns'
)
BEGIN
    CREATE TABLE [Boyahane].[GoodsReturnItems] (
        [Id] int NOT NULL IDENTITY,
        [ReturnId] int NOT NULL,
        [RollId] int NULL,
        [YarnLotId] int NULL,
        [ItemNo] nvarchar(40) NOT NULL,
        [CardName] nvarchar(200) NOT NULL,
        [Kilograms] decimal(12,2) NOT NULL,
        [Meters] decimal(12,2) NULL,
        [Reason] nvarchar(1000) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_GoodsReturnItems] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_GoodsReturnItems_Item] CHECK (([RollId] IS NOT NULL AND [YarnLotId] IS NULL) OR ([RollId] IS NULL AND [YarnLotId] IS NOT NULL)),
        CONSTRAINT [FK_GoodsReturnItems_FabricRolls_RollId] FOREIGN KEY ([RollId]) REFERENCES [Boyahane].[FabricRolls] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_GoodsReturnItems_GoodsReturns_ReturnId] FOREIGN KEY ([ReturnId]) REFERENCES [Boyahane].[GoodsReturns] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_GoodsReturnItems_YarnLots_YarnLotId] FOREIGN KEY ([YarnLotId]) REFERENCES [Boyahane].[YarnLots] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030225038_AddFinalControlsAndGoodsReturns'
)
BEGIN
    CREATE INDEX [IX_PackingLists_SaleCustomerId] ON [Boyahane].[PackingLists] ([SaleCustomerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030225038_AddFinalControlsAndGoodsReturns'
)
BEGIN
    CREATE INDEX [IX_GoodsReturnItems_ReturnId] ON [Boyahane].[GoodsReturnItems] ([ReturnId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030225038_AddFinalControlsAndGoodsReturns'
)
BEGIN
    CREATE INDEX [IX_GoodsReturnItems_RollId] ON [Boyahane].[GoodsReturnItems] ([RollId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030225038_AddFinalControlsAndGoodsReturns'
)
BEGIN
    CREATE INDEX [IX_GoodsReturnItems_YarnLotId] ON [Boyahane].[GoodsReturnItems] ([YarnLotId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030225038_AddFinalControlsAndGoodsReturns'
)
BEGIN
    CREATE INDEX [IX_GoodsReturns_CustomerId] ON [Boyahane].[GoodsReturns] ([CustomerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030225038_AddFinalControlsAndGoodsReturns'
)
BEGIN
    CREATE UNIQUE INDEX [IX_GoodsReturns_ReturnNo] ON [Boyahane].[GoodsReturns] ([ReturnNo]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030225038_AddFinalControlsAndGoodsReturns'
)
BEGIN
    ALTER TABLE [Boyahane].[PackingLists] ADD CONSTRAINT [FK_PackingLists_Customers_SaleCustomerId] FOREIGN KEY ([SaleCustomerId]) REFERENCES [Boyahane].[Customers] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030225038_AddFinalControlsAndGoodsReturns'
)
BEGIN
    SET IDENTITY_INSERT [Core].[Permissions] ON;
    EXEC(N'INSERT INTO [Core].[Permissions] ([Id], [Code], [Module], [Description])
    SELECT v.[Id], v.[Code], v.[Module], v.[Description] FROM (VALUES
    (1037, N''Boyahane.Batch.FinalQuality'', N''Boyahane'', N''Final kalite kararını ofisten girme''),
    (1038, N''Boyahane.Batch.FinalOverride'', N''Boyahane'', N''Çıkış ölçüsü sınırını gerekçeyle aşma'')
    ) v([Id], [Code], [Module], [Description])
    WHERE NOT EXISTS (SELECT 1 FROM [Core].[Permissions] p WHERE p.[Id] = v.[Id] OR p.[Code] = v.[Code])');
    SET IDENTITY_INSERT [Core].[Permissions] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030225038_AddFinalControlsAndGoodsReturns'
)
BEGIN
    INSERT INTO [Core].[RolePermissions] ([RoleId], [PermissionId])
    SELECT r.[Id], p.[Id] FROM (VALUES
    (N'ADMIN', 1037), (N'ADMIN', 1038),
    (N'BOYAHANE_MANAGER', 1037), (N'BOYAHANE_MANAGER', 1038),
    (N'BOYAHANE_RECEIVING', 1037)
    ) v([RoleCode], [PermissionId])
    JOIN [Core].[Roles] r ON r.[Code] = v.[RoleCode]
    JOIN [Core].[Permissions] p ON p.[Id] = v.[PermissionId]
    WHERE NOT EXISTS (SELECT 1 FROM [Core].[RolePermissions] rp WHERE rp.[RoleId] = r.[Id] AND rp.[PermissionId] = p.[Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030225038_AddFinalControlsAndGoodsReturns'
)
BEGIN
    INSERT INTO [Boyahane].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261030225038_AddFinalControlsAndGoodsReturns', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031221801_AddDefaultBreakdownReasons'
)
BEGIN
    INSERT INTO [Boyahane].[StopReasons] ([Code], [Name], [Category], [SortOrder], [IsActive], [CreatedAt], [IsDeleted])
    SELECT v.[Code], v.[Name], 'BREAKDOWN', v.[SortOrder], 1, SYSUTCDATETIME(), 0 FROM (VALUES
    (N'POMPA', N'Pompa arızası', 10),
    (N'ISITMA', N'Isıtma / buhar arızası', 20),
    (N'MOTOR', N'Motor arızası', 30),
    (N'ELEKTRIK', N'Elektrik arızası', 40),
    (N'OTOMASYON', N'Otomasyon / kontrol arızası', 50),
    (N'SU-TESISAT', N'Su / tesisat sorunu', 60),
    (N'ARIZA-DIGER', N'Diğer arıza', 70)
    ) v([Code], [Name], [SortOrder])
    WHERE NOT EXISTS (SELECT 1 FROM [Boyahane].[StopReasons] r WHERE r.[Code] = v.[Code])
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031221801_AddDefaultBreakdownReasons'
)
BEGIN
    INSERT INTO [Boyahane].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261031221801_AddDefaultBreakdownReasons', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031224215_AddOperatorCardAndPauseReasons'
)
BEGIN
    DECLARE @var9 nvarchar(max);
    SELECT @var9 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Boyahane].[Operators]') AND [c].[name] = N'Name');
    IF @var9 IS NOT NULL EXEC(N'ALTER TABLE [Boyahane].[Operators] DROP CONSTRAINT ' + @var9 + ';');
    ALTER TABLE [Boyahane].[Operators] ALTER COLUMN [Name] nvarchar(201) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031224215_AddOperatorCardAndPauseReasons'
)
BEGIN
    ALTER TABLE [Boyahane].[Operators] ADD [CanPause] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031224215_AddOperatorCardAndPauseReasons'
)
BEGIN
    ALTER TABLE [Boyahane].[Operators] ADD [CanReportBreakdown] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031224215_AddOperatorCardAndPauseReasons'
)
BEGIN
    ALTER TABLE [Boyahane].[Operators] ADD [FirstName] nvarchar(100) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031224215_AddOperatorCardAndPauseReasons'
)
BEGIN
    ALTER TABLE [Boyahane].[Operators] ADD [LastName] nvarchar(100) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031224215_AddOperatorCardAndPauseReasons'
)
BEGIN
    CREATE TABLE [Boyahane].[OperatorMachines] (
        [OperatorId] int NOT NULL,
        [MachineId] int NOT NULL,
        CONSTRAINT [PK_OperatorMachines] PRIMARY KEY ([OperatorId], [MachineId]),
        CONSTRAINT [FK_OperatorMachines_Machines_MachineId] FOREIGN KEY ([MachineId]) REFERENCES [Boyahane].[Machines] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_OperatorMachines_Operators_OperatorId] FOREIGN KEY ([OperatorId]) REFERENCES [Boyahane].[Operators] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031224215_AddOperatorCardAndPauseReasons'
)
BEGIN
    CREATE TABLE [Boyahane].[OperatorOperations] (
        [OperatorId] int NOT NULL,
        [OperationId] int NOT NULL,
        CONSTRAINT [PK_OperatorOperations] PRIMARY KEY ([OperatorId], [OperationId]),
        CONSTRAINT [FK_OperatorOperations_Operations_OperationId] FOREIGN KEY ([OperationId]) REFERENCES [Boyahane].[Operations] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_OperatorOperations_Operators_OperatorId] FOREIGN KEY ([OperatorId]) REFERENCES [Boyahane].[Operators] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031224215_AddOperatorCardAndPauseReasons'
)
BEGIN
    CREATE TABLE [Boyahane].[OperatorPhotos] (
        [OperatorId] int NOT NULL,
        [Content] varbinary(max) NOT NULL,
        [ContentType] nvarchar(30) NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_OperatorPhotos] PRIMARY KEY ([OperatorId]),
        CONSTRAINT [FK_OperatorPhotos_Operators_OperatorId] FOREIGN KEY ([OperatorId]) REFERENCES [Boyahane].[Operators] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031224215_AddOperatorCardAndPauseReasons'
)
BEGIN
    CREATE INDEX [IX_OperatorMachines_MachineId] ON [Boyahane].[OperatorMachines] ([MachineId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031224215_AddOperatorCardAndPauseReasons'
)
BEGIN
    CREATE INDEX [IX_OperatorOperations_OperationId] ON [Boyahane].[OperatorOperations] ([OperationId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031224215_AddOperatorCardAndPauseReasons'
)
BEGIN
    EXEC(N'UPDATE [Boyahane].[Operators] SET
        [FirstName] = CASE WHEN CHARINDEX('' '', [Name]) > 0 THEN LEFT([Name], CHARINDEX('' '', [Name]) - 1) ELSE LEFT([Name], 100) END,
        [LastName] = CASE WHEN CHARINDEX('' '', [Name]) > 0 THEN LEFT(LTRIM(SUBSTRING([Name], CHARINDEX('' '', [Name]) + 1, 200)), 100) ELSE '''' END')
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031224215_AddOperatorCardAndPauseReasons'
)
BEGIN
    INSERT INTO [Boyahane].[OperatorOperations] ([OperatorId], [OperationId])
    SELECT o.[Id], p.[Id] FROM [Boyahane].[Operators] o CROSS JOIN [Boyahane].[Operations] p WHERE o.[IsDeleted] = 0 AND p.[IsDeleted] = 0
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031224215_AddOperatorCardAndPauseReasons'
)
BEGIN
    INSERT INTO [Boyahane].[StopReasons] ([Code], [Name], [Category], [SortOrder], [IsActive], [CreatedAt], [IsDeleted])
    SELECT v.[Code], v.[Name], 'PAUSE', v.[SortOrder], 1, SYSUTCDATETIME(), 0 FROM (VALUES
    (N'SU', N'Su', 10),
    (N'TUVALET', N'Tuvalet', 20),
    (N'KISISEL', N'Kişisel ihtiyaç', 30),
    (N'BEKLEME', N'Bekleme', 40),
    (N'DIGER', N'Diğer', 50)
    ) v([Code], [Name], [SortOrder])
    WHERE NOT EXISTS (SELECT 1 FROM [Boyahane].[StopReasons] r WHERE r.[Code] = v.[Code])
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031224215_AddOperatorCardAndPauseReasons'
)
BEGIN
    INSERT INTO [Boyahane].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261031224215_AddOperatorCardAndPauseReasons', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031230226_AddShiftsAndBreaks'
)
BEGIN
    ALTER TABLE [Boyahane].[TabletSessions] DROP CONSTRAINT [CK_TabletSessions_EndReason];
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031230226_AddShiftsAndBreaks'
)
BEGIN
    ALTER TABLE [Boyahane].[SessionPauses] DROP CONSTRAINT [CK_SessionPauses_Kind];
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031230226_AddShiftsAndBreaks'
)
BEGIN
    ALTER TABLE [Boyahane].[SessionPauses] ADD [BreakScheduleId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031230226_AddShiftsAndBreaks'
)
BEGIN
    ALTER TABLE [Boyahane].[Operators] ADD [BreakGroupId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031230226_AddShiftsAndBreaks'
)
BEGIN
    CREATE TABLE [Boyahane].[BreakGroups] (
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
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031230226_AddShiftsAndBreaks'
)
BEGIN
    CREATE TABLE [Boyahane].[Shifts] (
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
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031230226_AddShiftsAndBreaks'
)
BEGIN
    CREATE TABLE [Boyahane].[BreakSchedules] (
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
        CONSTRAINT [CK_BreakSchedules_Policy] CHECK ([Policy] IN ('AFTER_STEP', 'IMMEDIATE')),
        CONSTRAINT [CK_BreakSchedules_Times] CHECK ([StartTime] <> [EndTime]),
        CONSTRAINT [FK_BreakSchedules_BreakGroups_BreakGroupId] FOREIGN KEY ([BreakGroupId]) REFERENCES [Boyahane].[BreakGroups] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_BreakSchedules_Shifts_ShiftId] FOREIGN KEY ([ShiftId]) REFERENCES [Boyahane].[Shifts] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031230226_AddShiftsAndBreaks'
)
BEGIN
    EXEC(N'ALTER TABLE [Boyahane].[TabletSessions] ADD CONSTRAINT [CK_TabletSessions_EndReason] CHECK ([EndReason] IN (''LOGOUT'', ''SHIFT_ENDED'', ''TAKEN_OVER'', ''MOVED_AWAY'', ''CLOSED_BY_OFFICE''))');
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031230226_AddShiftsAndBreaks'
)
BEGIN
    CREATE INDEX [IX_SessionPauses_BreakScheduleId] ON [Boyahane].[SessionPauses] ([BreakScheduleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031230226_AddShiftsAndBreaks'
)
BEGIN
    EXEC(N'ALTER TABLE [Boyahane].[SessionPauses] ADD CONSTRAINT [CK_SessionPauses_Kind] CHECK ([Kind] IN (''WAITING_FOR_BATCH'', ''BREAK'', ''PAUSE'', ''MACHINE_BREAKDOWN''))');
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031230226_AddShiftsAndBreaks'
)
BEGIN
    CREATE INDEX [IX_Operators_BreakGroupId] ON [Boyahane].[Operators] ([BreakGroupId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031230226_AddShiftsAndBreaks'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_BreakGroups_Code] ON [Boyahane].[BreakGroups] ([Code]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031230226_AddShiftsAndBreaks'
)
BEGIN
    CREATE INDEX [IX_BreakSchedules_BreakGroupId] ON [Boyahane].[BreakSchedules] ([BreakGroupId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031230226_AddShiftsAndBreaks'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_BreakSchedules_Code] ON [Boyahane].[BreakSchedules] ([Code]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031230226_AddShiftsAndBreaks'
)
BEGIN
    CREATE INDEX [IX_BreakSchedules_ShiftId] ON [Boyahane].[BreakSchedules] ([ShiftId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031230226_AddShiftsAndBreaks'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Shifts_Code] ON [Boyahane].[Shifts] ([Code]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031230226_AddShiftsAndBreaks'
)
BEGIN
    ALTER TABLE [Boyahane].[Operators] ADD CONSTRAINT [FK_Operators_BreakGroups_BreakGroupId] FOREIGN KEY ([BreakGroupId]) REFERENCES [Boyahane].[BreakGroups] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031230226_AddShiftsAndBreaks'
)
BEGIN
    ALTER TABLE [Boyahane].[SessionPauses] ADD CONSTRAINT [FK_SessionPauses_BreakSchedules_BreakScheduleId] FOREIGN KEY ([BreakScheduleId]) REFERENCES [Boyahane].[BreakSchedules] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031230226_AddShiftsAndBreaks'
)
BEGIN
    INSERT INTO [Boyahane].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261031230226_AddShiftsAndBreaks', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102082339_AddOrderClose'
)
BEGIN
    ALTER TABLE [Boyahane].[Orders] DROP CONSTRAINT [CK_Orders_Status];
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102082339_AddOrderClose'
)
BEGIN
    ALTER TABLE [Boyahane].[Orders] ADD [CloseNote] nvarchar(500) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102082339_AddOrderClose'
)
BEGIN
    ALTER TABLE [Boyahane].[Orders] ADD [ClosedAt] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102082339_AddOrderClose'
)
BEGIN
    ALTER TABLE [Boyahane].[Orders] ADD [ClosedBy] nvarchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102082339_AddOrderClose'
)
BEGIN
    EXEC(N'ALTER TABLE [Boyahane].[Orders] ADD CONSTRAINT [CK_Orders_Status] CHECK ([Status] IN (''DRAFT'', ''CONFIRMED'', ''CANCELLED'', ''CLOSED''))');
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102082339_AddOrderClose'
)
BEGIN
    INSERT INTO [Boyahane].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261102082339_AddOrderClose', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102090122_AddDocumentsAndCustomerCodes'
)
BEGIN
    ALTER TABLE [Boyahane].[Customers] ADD [DocumentLanguage] nvarchar(30) NOT NULL DEFAULT N'TURKISH';
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102090122_AddDocumentsAndCustomerCodes'
)
BEGIN
    EXEC(N'ALTER TABLE [Boyahane].[Customers] ADD CONSTRAINT [CK_Customers_DocumentLanguage] CHECK ([DocumentLanguage] IN (''TURKISH'', ''ENGLISH''))');
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102090122_AddDocumentsAndCustomerCodes'
)
BEGIN
    INSERT INTO [Boyahane].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261102090122_AddDocumentsAndCustomerCodes', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102095052_AddColorPatternArchive'
)
BEGIN
    ALTER TABLE [Boyahane].[Operations] DROP CONSTRAINT [CK_Operations_Behavior];
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102095052_AddColorPatternArchive'
)
BEGIN
    ALTER TABLE [Boyahane].[BatchSteps] DROP CONSTRAINT [CK_BatchSteps_ColorResult];
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102095052_AddColorPatternArchive'
)
BEGIN
    ALTER TABLE [Boyahane].[BatchSteps] DROP CONSTRAINT [CK_BatchSteps_ReworkKind];
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102095052_AddColorPatternArchive'
)
BEGIN
    ALTER TABLE [Boyahane].[OrderLines] ADD [ColorCardId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102095052_AddColorPatternArchive'
)
BEGIN
    ALTER TABLE [Boyahane].[OrderLines] ADD [ColorwayId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102095052_AddColorPatternArchive'
)
BEGIN
    ALTER TABLE [Boyahane].[OrderLines] ADD [PatternId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102095052_AddColorPatternArchive'
)
BEGIN
    ALTER TABLE [Boyahane].[BatchSteps] ADD [StrippingRecipeId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102095052_AddColorPatternArchive'
)
BEGIN
    CREATE TABLE [Boyahane].[ColorCards] (
        [Id] int NOT NULL IDENTITY,
        [Code] nvarchar(30) NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [CustomerId] int NULL,
        [CustomerColorCode] nvarchar(50) NULL,
        [Pantone] nvarchar(50) NULL,
        [LabL] decimal(7,3) NULL,
        [LabA] decimal(7,3) NULL,
        [LabB] decimal(7,3) NULL,
        [Notes] nvarchar(500) NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_ColorCards] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ColorCards_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Boyahane].[Customers] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102095052_AddColorPatternArchive'
)
BEGIN
    CREATE TABLE [Boyahane].[Patterns] (
        [Id] int NOT NULL IDENTITY,
        [Code] nvarchar(30) NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [CustomerId] int NULL,
        [RepeatWidthCm] decimal(8,2) NULL,
        [RepeatHeightCm] decimal(8,2) NULL,
        [PrintType] nvarchar(30) NOT NULL,
        [ScreenCount] int NULL,
        [Notes] nvarchar(500) NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_Patterns] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Patterns_PrintType] CHECK ([PrintType] IN ('PIGMENT', 'REACTIVE', 'DISCHARGE', 'OTHER')),
        CONSTRAINT [CK_Patterns_ScreenCount] CHECK ([ScreenCount] IS NULL OR [ScreenCount] > 0),
        CONSTRAINT [FK_Patterns_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Boyahane].[Customers] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102095052_AddColorPatternArchive'
)
BEGIN
    CREATE TABLE [Boyahane].[StrippingRecipes] (
        [Id] int NOT NULL IDENTITY,
        [Code] nvarchar(30) NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [Notes] nvarchar(500) NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_StrippingRecipes] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102095052_AddColorPatternArchive'
)
BEGIN
    CREATE TABLE [Boyahane].[ColorCardImages] (
        [ColorCardId] int NOT NULL,
        [Content] varbinary(max) NOT NULL,
        [ContentType] nvarchar(20) NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_ColorCardImages] PRIMARY KEY ([ColorCardId]),
        CONSTRAINT [FK_ColorCardImages_ColorCards_ColorCardId] FOREIGN KEY ([ColorCardId]) REFERENCES [Boyahane].[ColorCards] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102095052_AddColorPatternArchive'
)
BEGIN
    CREATE TABLE [Boyahane].[PatternColorways] (
        [Id] int NOT NULL IDENTITY,
        [PatternId] int NOT NULL,
        [Code] nvarchar(30) NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_PatternColorways] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PatternColorways_Patterns_PatternId] FOREIGN KEY ([PatternId]) REFERENCES [Boyahane].[Patterns] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102095052_AddColorPatternArchive'
)
BEGIN
    CREATE TABLE [Boyahane].[PatternImages] (
        [PatternId] int NOT NULL,
        [Content] varbinary(max) NOT NULL,
        [ContentType] nvarchar(20) NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_PatternImages] PRIMARY KEY ([PatternId]),
        CONSTRAINT [FK_PatternImages_Patterns_PatternId] FOREIGN KEY ([PatternId]) REFERENCES [Boyahane].[Patterns] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102095052_AddColorPatternArchive'
)
BEGIN
    CREATE TABLE [Boyahane].[StrippingRecipeLines] (
        [Id] int NOT NULL IDENTITY,
        [RecipeId] int NOT NULL,
        [Sequence] int NOT NULL,
        [DyeChemicalId] int NOT NULL,
        [Amount] decimal(10,4) NOT NULL,
        [Unit] nvarchar(30) NOT NULL,
        [TemperatureCelsius] int NULL,
        [DurationMinutes] int NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_StrippingRecipeLines] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_StrippingRecipeLines_Amount] CHECK ([Amount] > 0),
        CONSTRAINT [CK_StrippingRecipeLines_Unit] CHECK ([Unit] IN ('PERCENT', 'GRAMS_PER_LITER')),
        CONSTRAINT [FK_StrippingRecipeLines_DyeChemicals_DyeChemicalId] FOREIGN KEY ([DyeChemicalId]) REFERENCES [Boyahane].[DyeChemicals] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_StrippingRecipeLines_StrippingRecipes_RecipeId] FOREIGN KEY ([RecipeId]) REFERENCES [Boyahane].[StrippingRecipes] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102095052_AddColorPatternArchive'
)
BEGIN
    CREATE TABLE [Boyahane].[ColorwayColors] (
        [Id] int NOT NULL IDENTITY,
        [ColorwayId] int NOT NULL,
        [Position] int NOT NULL,
        [ColorCardId] int NULL,
        [Name] nvarchar(100) NOT NULL,
        [PasteKilogramsPerMeter] decimal(10,4) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_ColorwayColors] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_ColorwayColors_Paste] CHECK ([PasteKilogramsPerMeter] IS NULL OR [PasteKilogramsPerMeter] > 0),
        CONSTRAINT [CK_ColorwayColors_Position] CHECK ([Position] > 0),
        CONSTRAINT [FK_ColorwayColors_ColorCards_ColorCardId] FOREIGN KEY ([ColorCardId]) REFERENCES [Boyahane].[ColorCards] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ColorwayColors_PatternColorways_ColorwayId] FOREIGN KEY ([ColorwayId]) REFERENCES [Boyahane].[PatternColorways] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102095052_AddColorPatternArchive'
)
BEGIN
    CREATE TABLE [Boyahane].[PrintRecipeLines] (
        [Id] int NOT NULL IDENTITY,
        [ColorwayColorId] int NOT NULL,
        [DyeChemicalId] int NOT NULL,
        [GramsPerKilogram] decimal(10,4) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_PrintRecipeLines] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_PrintRecipeLines_Amount] CHECK ([GramsPerKilogram] > 0),
        CONSTRAINT [FK_PrintRecipeLines_ColorwayColors_ColorwayColorId] FOREIGN KEY ([ColorwayColorId]) REFERENCES [Boyahane].[ColorwayColors] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PrintRecipeLines_DyeChemicals_DyeChemicalId] FOREIGN KEY ([DyeChemicalId]) REFERENCES [Boyahane].[DyeChemicals] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102095052_AddColorPatternArchive'
)
BEGIN
    CREATE INDEX [IX_OrderLines_ColorCardId] ON [Boyahane].[OrderLines] ([ColorCardId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102095052_AddColorPatternArchive'
)
BEGIN
    CREATE INDEX [IX_OrderLines_ColorwayId] ON [Boyahane].[OrderLines] ([ColorwayId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102095052_AddColorPatternArchive'
)
BEGIN
    CREATE INDEX [IX_OrderLines_PatternId] ON [Boyahane].[OrderLines] ([PatternId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102095052_AddColorPatternArchive'
)
BEGIN
    EXEC(N'ALTER TABLE [Boyahane].[Operations] ADD CONSTRAINT [CK_Operations_Behavior] CHECK ([Behavior] IN (''STANDARD'', ''DYEING'', ''COLOR_CHECK'', ''FINAL_QUALITY'', ''STRIPPING'', ''PRINTING''))');
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102095052_AddColorPatternArchive'
)
BEGIN
    CREATE INDEX [IX_BatchSteps_StrippingRecipeId] ON [Boyahane].[BatchSteps] ([StrippingRecipeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102095052_AddColorPatternArchive'
)
BEGIN
    EXEC(N'ALTER TABLE [Boyahane].[BatchSteps] ADD CONSTRAINT [CK_BatchSteps_ColorResult] CHECK ([ColorResult] IN (''APPROVED'', ''ADDITION'', ''REDO'', ''STRIPPING''))');
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102095052_AddColorPatternArchive'
)
BEGIN
    EXEC(N'ALTER TABLE [Boyahane].[BatchSteps] ADD CONSTRAINT [CK_BatchSteps_ReworkKind] CHECK ([ReworkKind] IN (''APPROVED'', ''ADDITION'', ''REDO'', ''STRIPPING''))');
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102095052_AddColorPatternArchive'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_ColorCards_Code] ON [Boyahane].[ColorCards] ([Code]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102095052_AddColorPatternArchive'
)
BEGIN
    CREATE INDEX [IX_ColorCards_CustomerId] ON [Boyahane].[ColorCards] ([CustomerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102095052_AddColorPatternArchive'
)
BEGIN
    CREATE INDEX [IX_ColorwayColors_ColorCardId] ON [Boyahane].[ColorwayColors] ([ColorCardId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102095052_AddColorPatternArchive'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ColorwayColors_ColorwayId_Position] ON [Boyahane].[ColorwayColors] ([ColorwayId], [Position]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102095052_AddColorPatternArchive'
)
BEGIN
    CREATE UNIQUE INDEX [IX_PatternColorways_PatternId_Code] ON [Boyahane].[PatternColorways] ([PatternId], [Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102095052_AddColorPatternArchive'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Patterns_Code] ON [Boyahane].[Patterns] ([Code]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102095052_AddColorPatternArchive'
)
BEGIN
    CREATE INDEX [IX_Patterns_CustomerId] ON [Boyahane].[Patterns] ([CustomerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102095052_AddColorPatternArchive'
)
BEGIN
    CREATE UNIQUE INDEX [IX_PrintRecipeLines_ColorwayColorId_DyeChemicalId] ON [Boyahane].[PrintRecipeLines] ([ColorwayColorId], [DyeChemicalId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102095052_AddColorPatternArchive'
)
BEGIN
    CREATE INDEX [IX_PrintRecipeLines_DyeChemicalId] ON [Boyahane].[PrintRecipeLines] ([DyeChemicalId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102095052_AddColorPatternArchive'
)
BEGIN
    CREATE INDEX [IX_StrippingRecipeLines_DyeChemicalId] ON [Boyahane].[StrippingRecipeLines] ([DyeChemicalId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102095052_AddColorPatternArchive'
)
BEGIN
    CREATE UNIQUE INDEX [IX_StrippingRecipeLines_RecipeId_Sequence] ON [Boyahane].[StrippingRecipeLines] ([RecipeId], [Sequence]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102095052_AddColorPatternArchive'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_StrippingRecipes_Code] ON [Boyahane].[StrippingRecipes] ([Code]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102095052_AddColorPatternArchive'
)
BEGIN
    ALTER TABLE [Boyahane].[BatchSteps] ADD CONSTRAINT [FK_BatchSteps_StrippingRecipes_StrippingRecipeId] FOREIGN KEY ([StrippingRecipeId]) REFERENCES [Boyahane].[StrippingRecipes] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102095052_AddColorPatternArchive'
)
BEGIN
    ALTER TABLE [Boyahane].[OrderLines] ADD CONSTRAINT [FK_OrderLines_ColorCards_ColorCardId] FOREIGN KEY ([ColorCardId]) REFERENCES [Boyahane].[ColorCards] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102095052_AddColorPatternArchive'
)
BEGIN
    ALTER TABLE [Boyahane].[OrderLines] ADD CONSTRAINT [FK_OrderLines_PatternColorways_ColorwayId] FOREIGN KEY ([ColorwayId]) REFERENCES [Boyahane].[PatternColorways] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102095052_AddColorPatternArchive'
)
BEGIN
    ALTER TABLE [Boyahane].[OrderLines] ADD CONSTRAINT [FK_OrderLines_Patterns_PatternId] FOREIGN KEY ([PatternId]) REFERENCES [Boyahane].[Patterns] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102095052_AddColorPatternArchive'
)
BEGIN
    INSERT INTO [Boyahane].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261102095052_AddColorPatternArchive', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102104802_AddQuotationsAndCapacityPlan'
)
BEGIN
    ALTER TABLE [Boyahane].[Batches] ADD [PlanSequence] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102104802_AddQuotationsAndCapacityPlan'
)
BEGIN
    CREATE TABLE [Boyahane].[CostSettings] (
        [Id] int NOT NULL IDENTITY,
        [Currency] char(3) NOT NULL DEFAULT 'TRY',
        [DefaultMachineHourlyCost] decimal(12,4) NOT NULL,
        [LaborCostPerMinute] decimal(12,4) NOT NULL,
        [DefaultLiquorRatio] decimal(6,2) NOT NULL,
        [DefaultBatchKilograms] decimal(10,2) NOT NULL,
        [OverheadPercent] decimal(5,2) NOT NULL,
        [FirePercent] decimal(5,2) NOT NULL,
        [MarginPercent] decimal(7,2) NOT NULL,
        [ChemicalCostMethod] nvarchar(30) NOT NULL,
        [FabricDyeingMinutes] decimal(10,2) NOT NULL,
        [YarnDyeingMinutes] decimal(10,2) NOT NULL,
        [FinishingMinutes] decimal(10,2) NOT NULL,
        [PrintingMinutes] decimal(10,2) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_CostSettings] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_CostSettings_Costs] CHECK ([DefaultMachineHourlyCost] >= 0 AND [LaborCostPerMinute] >= 0),
        CONSTRAINT [CK_CostSettings_Defaults] CHECK ([DefaultLiquorRatio] > 0 AND [DefaultBatchKilograms] > 0),
        CONSTRAINT [CK_CostSettings_Method] CHECK ([ChemicalCostMethod] IN ('LAST_LOT', 'WEIGHTED_AVERAGE')),
        CONSTRAINT [CK_CostSettings_Minutes] CHECK ([FabricDyeingMinutes] > 0 AND [YarnDyeingMinutes] > 0 AND [FinishingMinutes] > 0 AND [PrintingMinutes] > 0),
        CONSTRAINT [CK_CostSettings_Percents] CHECK ([OverheadPercent] BETWEEN 0 AND 100 AND [FirePercent] BETWEEN 0 AND 50 AND [MarginPercent] BETWEEN -100 AND 1000)
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102104802_AddQuotationsAndCapacityPlan'
)
BEGIN
    CREATE TABLE [Boyahane].[MachineCostRates] (
        [MachineId] int NOT NULL,
        [HourlyCost] decimal(12,4) NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        [UpdatedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_MachineCostRates] PRIMARY KEY ([MachineId]),
        CONSTRAINT [CK_MachineCostRates_HourlyCost] CHECK ([HourlyCost] >= 0),
        CONSTRAINT [FK_MachineCostRates_Machines_MachineId] FOREIGN KEY ([MachineId]) REFERENCES [Boyahane].[Machines] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102104802_AddQuotationsAndCapacityPlan'
)
BEGIN
    CREATE TABLE [Boyahane].[Quotations] (
        [Id] int NOT NULL IDENTITY,
        [QuotationNo] nvarchar(30) NOT NULL,
        [QuotationDate] date NOT NULL,
        [ValidUntil] date NOT NULL,
        [CustomerId] int NULL,
        [ProspectName] nvarchar(200) NULL,
        [Ownership] nvarchar(30) NOT NULL,
        [DueDate] date NULL,
        [Currency] char(3) NOT NULL DEFAULT 'TRY',
        [DocumentLanguage] nvarchar(30) NOT NULL,
        [Notes] nvarchar(1000) NULL,
        [Status] nvarchar(30) NOT NULL,
        [SentAt] datetime2 NULL,
        [DecidedAt] datetime2 NULL,
        [DecidedBy] nvarchar(100) NULL,
        [DecisionNote] nvarchar(500) NULL,
        [OrderId] int NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_Quotations] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Quotations_DocumentLanguage] CHECK ([DocumentLanguage] IN ('TURKISH', 'ENGLISH')),
        CONSTRAINT [CK_Quotations_Ownership] CHECK ([Ownership] IN ('COMMISSION', 'OWN_GOODS')),
        CONSTRAINT [CK_Quotations_Party] CHECK ([CustomerId] IS NOT NULL OR [ProspectName] IS NOT NULL),
        CONSTRAINT [CK_Quotations_Status] CHECK ([Status] IN ('DRAFT', 'SENT', 'ACCEPTED', 'REJECTED', 'EXPIRED')),
        CONSTRAINT [CK_Quotations_ValidUntil] CHECK ([ValidUntil] >= [QuotationDate]),
        CONSTRAINT [FK_Quotations_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Boyahane].[Customers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Quotations_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Boyahane].[Orders] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102104802_AddQuotationsAndCapacityPlan'
)
BEGIN
    CREATE TABLE [Boyahane].[QuotationLines] (
        [Id] int NOT NULL IDENTITY,
        [QuotationId] int NOT NULL,
        [ProcessType] nvarchar(30) NOT NULL,
        [FabricId] int NULL,
        [YarnId] int NULL,
        [ColorName] nvarchar(100) NULL,
        [ColorCode] nvarchar(50) NULL,
        [ColorCardId] int NULL,
        [PatternId] int NULL,
        [ColorwayId] int NULL,
        [Quantity] decimal(12,2) NOT NULL,
        [Unit] nvarchar(30) NOT NULL,
        [RecipeTrialId] int NULL,
        [MachineId] int NULL,
        [BatchKilogramsInput] decimal(10,2) NULL,
        [LiquorRatioInput] decimal(6,2) NULL,
        [ProcessMinutesInput] decimal(10,2) NULL,
        [FirePercentInput] decimal(5,2) NULL,
        [OverheadPercentInput] decimal(5,2) NULL,
        [MarginPercentInput] decimal(7,2) NULL,
        [UnitPriceInput] decimal(12,4) NULL,
        [BatchKilograms] decimal(10,2) NOT NULL,
        [LiquorRatio] decimal(6,2) NULL,
        [ProcessMinutes] decimal(10,2) NOT NULL,
        [MinutesSource] nvarchar(30) NOT NULL,
        [KilogramsPerMeter] decimal(10,4) NULL,
        [ChemicalCost] decimal(14,4) NOT NULL,
        [PrintCost] decimal(14,4) NOT NULL,
        [MachineCost] decimal(14,4) NOT NULL,
        [LaborCost] decimal(14,4) NOT NULL,
        [FirePercent] decimal(5,2) NOT NULL,
        [FireCost] decimal(14,4) NOT NULL,
        [OverheadPercent] decimal(5,2) NOT NULL,
        [OverheadCost] decimal(14,4) NOT NULL,
        [UnitCost] decimal(14,4) NOT NULL,
        [MarginPercent] decimal(9,2) NULL,
        [UnitPrice] decimal(12,4) NOT NULL,
        [CostWarning] nvarchar(500) NULL,
        [Notes] nvarchar(500) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_QuotationLines] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_QuotationLines_BatchKilograms] CHECK ([BatchKilograms] > 0),
        CONSTRAINT [CK_QuotationLines_Item] CHECK (([FabricId] IS NOT NULL AND [YarnId] IS NULL) OR ([FabricId] IS NULL AND [YarnId] IS NOT NULL)),
        CONSTRAINT [CK_QuotationLines_MinutesSource] CHECK ([MinutesSource] IN ('MANUAL', 'ROUTE', 'DEFAULT')),
        CONSTRAINT [CK_QuotationLines_ProcessType] CHECK ([ProcessType] IN ('FABRIC_DYEING', 'YARN_DYEING', 'FINISHING', 'PRINTING')),
        CONSTRAINT [CK_QuotationLines_Quantity] CHECK ([Quantity] > 0),
        CONSTRAINT [CK_QuotationLines_Unit] CHECK ([Unit] IN ('METER', 'KILOGRAM')),
        CONSTRAINT [FK_QuotationLines_ColorCards_ColorCardId] FOREIGN KEY ([ColorCardId]) REFERENCES [Boyahane].[ColorCards] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_QuotationLines_Fabrics_FabricId] FOREIGN KEY ([FabricId]) REFERENCES [Boyahane].[Fabrics] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_QuotationLines_LabTrials_RecipeTrialId] FOREIGN KEY ([RecipeTrialId]) REFERENCES [Boyahane].[LabTrials] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_QuotationLines_Machines_MachineId] FOREIGN KEY ([MachineId]) REFERENCES [Boyahane].[Machines] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_QuotationLines_PatternColorways_ColorwayId] FOREIGN KEY ([ColorwayId]) REFERENCES [Boyahane].[PatternColorways] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_QuotationLines_Patterns_PatternId] FOREIGN KEY ([PatternId]) REFERENCES [Boyahane].[Patterns] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_QuotationLines_Quotations_QuotationId] FOREIGN KEY ([QuotationId]) REFERENCES [Boyahane].[Quotations] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_QuotationLines_Yarns_YarnId] FOREIGN KEY ([YarnId]) REFERENCES [Boyahane].[Yarns] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102104802_AddQuotationsAndCapacityPlan'
)
BEGIN
    CREATE INDEX [IX_QuotationLines_ColorCardId] ON [Boyahane].[QuotationLines] ([ColorCardId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102104802_AddQuotationsAndCapacityPlan'
)
BEGIN
    CREATE INDEX [IX_QuotationLines_ColorwayId] ON [Boyahane].[QuotationLines] ([ColorwayId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102104802_AddQuotationsAndCapacityPlan'
)
BEGIN
    CREATE INDEX [IX_QuotationLines_FabricId] ON [Boyahane].[QuotationLines] ([FabricId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102104802_AddQuotationsAndCapacityPlan'
)
BEGIN
    CREATE INDEX [IX_QuotationLines_MachineId] ON [Boyahane].[QuotationLines] ([MachineId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102104802_AddQuotationsAndCapacityPlan'
)
BEGIN
    CREATE INDEX [IX_QuotationLines_PatternId] ON [Boyahane].[QuotationLines] ([PatternId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102104802_AddQuotationsAndCapacityPlan'
)
BEGIN
    CREATE INDEX [IX_QuotationLines_QuotationId] ON [Boyahane].[QuotationLines] ([QuotationId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102104802_AddQuotationsAndCapacityPlan'
)
BEGIN
    CREATE INDEX [IX_QuotationLines_RecipeTrialId] ON [Boyahane].[QuotationLines] ([RecipeTrialId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102104802_AddQuotationsAndCapacityPlan'
)
BEGIN
    CREATE INDEX [IX_QuotationLines_YarnId] ON [Boyahane].[QuotationLines] ([YarnId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102104802_AddQuotationsAndCapacityPlan'
)
BEGIN
    CREATE INDEX [IX_Quotations_CustomerId] ON [Boyahane].[Quotations] ([CustomerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102104802_AddQuotationsAndCapacityPlan'
)
BEGIN
    CREATE INDEX [IX_Quotations_OrderId] ON [Boyahane].[Quotations] ([OrderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102104802_AddQuotationsAndCapacityPlan'
)
BEGIN
    CREATE INDEX [IX_Quotations_QuotationDate] ON [Boyahane].[Quotations] ([QuotationDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102104802_AddQuotationsAndCapacityPlan'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Quotations_QuotationNo] ON [Boyahane].[Quotations] ([QuotationNo]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102104802_AddQuotationsAndCapacityPlan'
)
BEGIN
    CREATE INDEX [IX_Quotations_Status] ON [Boyahane].[Quotations] ([Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102104802_AddQuotationsAndCapacityPlan'
)
BEGIN
    SET IDENTITY_INSERT [Core].[Permissions] ON;
    EXEC(N'INSERT INTO [Core].[Permissions] ([Id], [Code], [Module], [Description])
    SELECT v.[Id], v.[Code], v.[Module], v.[Description] FROM (VALUES
    (1039, N''Boyahane.Quotation.View'', N''Boyahane'', N''Fiyat tekliflerini, ön maliyeti ve maliyet parametrelerini görüntüleme''),
    (1040, N''Boyahane.Quotation.Manage'', N''Boyahane'', N''Teklif hazırlama, gönderme, kabul / ret ve maliyet parametreleri'')
    ) v([Id], [Code], [Module], [Description])
    WHERE NOT EXISTS (SELECT 1 FROM [Core].[Permissions] p WHERE p.[Id] = v.[Id] OR p.[Code] = v.[Code])');
    SET IDENTITY_INSERT [Core].[Permissions] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102104802_AddQuotationsAndCapacityPlan'
)
BEGIN
    INSERT INTO [Core].[RolePermissions] ([RoleId], [PermissionId])
    SELECT v.[RoleId], v.[PermissionId] FROM (VALUES
    (1, 1039), (1, 1040)
    ) v([RoleId], [PermissionId])
    WHERE EXISTS (SELECT 1 FROM [Core].[Roles] r WHERE r.[Id] = v.[RoleId])
      AND EXISTS (SELECT 1 FROM [Core].[Permissions] p WHERE p.[Id] = v.[PermissionId])
      AND NOT EXISTS (SELECT 1 FROM [Core].[RolePermissions] rp WHERE rp.[RoleId] = v.[RoleId] AND rp.[PermissionId] = v.[PermissionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Boyahane].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102104802_AddQuotationsAndCapacityPlan'
)
BEGIN
    INSERT INTO [Boyahane].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261102104802_AddQuotationsAndCapacityPlan', N'10.0.12');
END;

COMMIT;
GO

