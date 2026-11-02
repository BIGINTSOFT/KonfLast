/*
    Tekstil ERP - Örme modülü kurulum / güncelleme script'i
    Bu dosya otomatik üretilir (database\generate-script.ps1); elle düzenlemeyin.

    Hedef veritabanına (örn. TekstilERP) bağlanıp script'in tamamını çalıştırın. Script tekrar
    çalıştırılabilir: daha önce uygulanmış adımlar atlanır. Sıra: önce 00_Core.sql, sonra modüller.

    sqlcmd ile:  sqlcmd -S "(localdb)\MSSQLLocalDB" -d TekstilERP -E -b -f 65001 -i 40_Orme.sql
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
IF OBJECT_ID(N'[Orme].[__EFMigrationsHistory]') IS NULL
BEGIN
    IF SCHEMA_ID(N'Orme') IS NULL EXEC(N'CREATE SCHEMA [Orme];');
    CREATE TABLE [Orme].[__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101200356_InitialOrme'
)
BEGIN
    IF SCHEMA_ID(N'Orme') IS NULL EXEC(N'CREATE SCHEMA [Orme];');
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101200356_InitialOrme'
)
BEGIN
    CREATE TABLE [Orme].[Customers] (
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
        [FirstQualityMaxPoints] decimal(8,2) NULL,
        [SecondQualityMaxPoints] decimal(8,2) NULL,
        [SecondQualityDiscountPercent] decimal(5,2) NULL,
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
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101200356_InitialOrme'
)
BEGIN
    CREATE TABLE [Orme].[Fabrics] (
        [Id] int NOT NULL IDENTITY,
        [FabricCode] nvarchar(30) NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [KnitName] nvarchar(100) NULL,
        [BedType] nvarchar(30) NOT NULL,
        [DiameterInch] int NULL,
        [Gauge] int NULL,
        [Composition] nvarchar(200) NULL,
        [RawWidthCm] decimal(6,2) NULL,
        [RawWeightGsm] int NOT NULL,
        [WastePercent] decimal(5,2) NULL,
        [ExpectedKilogramsPerHour] decimal(8,2) NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_Fabrics] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Fabrics_BedType] CHECK ([BedType] IN ('SINGLE', 'DOUBLE')),
        CONSTRAINT [CK_Fabrics_ExpectedKilogramsPerHour] CHECK ([ExpectedKilogramsPerHour] IS NULL OR [ExpectedKilogramsPerHour] > 0),
        CONSTRAINT [CK_Fabrics_RawWeightGsm] CHECK ([RawWeightGsm] > 0),
        CONSTRAINT [CK_Fabrics_RawWidthCm] CHECK ([RawWidthCm] IS NULL OR [RawWidthCm] > 0),
        CONSTRAINT [CK_Fabrics_WastePercent] CHECK ([WastePercent] IS NULL OR [WastePercent] BETWEEN 0 AND 100)
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101200356_InitialOrme'
)
BEGIN
    CREATE TABLE [Orme].[Machines] (
        [Id] int NOT NULL IDENTITY,
        [Code] nvarchar(30) NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [BedType] nvarchar(30) NOT NULL,
        [DiameterInch] int NOT NULL,
        [Gauge] int NOT NULL,
        [NeedleCount] int NULL,
        [FeederCount] int NULL,
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
        CONSTRAINT [PK_Machines] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Machines_BedType] CHECK ([BedType] IN ('SINGLE', 'DOUBLE')),
        CONSTRAINT [CK_Machines_DiameterInch] CHECK ([DiameterInch] > 0),
        CONSTRAINT [CK_Machines_FeederCount] CHECK ([FeederCount] IS NULL OR [FeederCount] > 0),
        CONSTRAINT [CK_Machines_Gauge] CHECK ([Gauge] > 0),
        CONSTRAINT [CK_Machines_NeedleCount] CHECK ([NeedleCount] IS NULL OR [NeedleCount] > 0),
        CONSTRAINT [CK_Machines_NominalSpeedRpm] CHECK ([NominalSpeedRpm] IS NULL OR [NominalSpeedRpm] > 0)
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101200356_InitialOrme'
)
BEGIN
    CREATE TABLE [Orme].[Suppliers] (
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
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101200356_InitialOrme'
)
BEGIN
    CREATE TABLE [Orme].[Yarns] (
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
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101200356_InitialOrme'
)
BEGIN
    CREATE TABLE [Orme].[CustomerAddresses] (
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
        CONSTRAINT [FK_CustomerAddresses_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Orme].[Customers] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101200356_InitialOrme'
)
BEGIN
    CREATE TABLE [Orme].[CustomerContacts] (
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
        CONSTRAINT [FK_CustomerContacts_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Orme].[Customers] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101200356_InitialOrme'
)
BEGIN
    CREATE TABLE [Orme].[FabricYarns] (
        [Id] int NOT NULL IDENTITY,
        [FabricId] int NOT NULL,
        [Sequence] int NOT NULL,
        [YarnId] int NOT NULL,
        [Percent] decimal(5,2) NOT NULL,
        [StitchLengthMm] decimal(5,2) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_FabricYarns] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_FabricYarns_Percent] CHECK ([Percent] > 0 AND [Percent] <= 100),
        CONSTRAINT [CK_FabricYarns_Sequence] CHECK ([Sequence] >= 1),
        CONSTRAINT [CK_FabricYarns_StitchLengthMm] CHECK ([StitchLengthMm] IS NULL OR [StitchLengthMm] > 0),
        CONSTRAINT [FK_FabricYarns_Fabrics_FabricId] FOREIGN KEY ([FabricId]) REFERENCES [Orme].[Fabrics] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_FabricYarns_Yarns_YarnId] FOREIGN KEY ([YarnId]) REFERENCES [Orme].[Yarns] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101200356_InitialOrme'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_CustomerAddresses_DefaultDelivery] ON [Orme].[CustomerAddresses] ([CustomerId]) WHERE [IsDefaultDelivery] = 1');
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101200356_InitialOrme'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_CustomerAddresses_DefaultInvoice] ON [Orme].[CustomerAddresses] ([CustomerId]) WHERE [IsDefaultInvoice] = 1');
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101200356_InitialOrme'
)
BEGIN
    CREATE INDEX [IX_CustomerContacts_CustomerId] ON [Orme].[CustomerContacts] ([CustomerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101200356_InitialOrme'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Customers_CustomerCode] ON [Orme].[Customers] ([CustomerCode]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101200356_InitialOrme'
)
BEGIN
    CREATE INDEX [IX_Customers_TaxNumber] ON [Orme].[Customers] ([TaxNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101200356_InitialOrme'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Fabrics_FabricCode] ON [Orme].[Fabrics] ([FabricCode]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101200356_InitialOrme'
)
BEGIN
    CREATE UNIQUE INDEX [IX_FabricYarns_FabricId_Sequence] ON [Orme].[FabricYarns] ([FabricId], [Sequence]);
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101200356_InitialOrme'
)
BEGIN
    CREATE INDEX [IX_FabricYarns_YarnId] ON [Orme].[FabricYarns] ([YarnId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101200356_InitialOrme'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Machines_Code] ON [Orme].[Machines] ([Code]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101200356_InitialOrme'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Suppliers_Code] ON [Orme].[Suppliers] ([Code]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101200356_InitialOrme'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Yarns_YarnCode] ON [Orme].[Yarns] ([YarnCode]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101200356_InitialOrme'
)
BEGIN
    SET IDENTITY_INSERT [Core].[Permissions] ON;
    EXEC(N'INSERT INTO [Core].[Permissions] ([Id], [Code], [Module], [Description])
    SELECT v.[Id], v.[Code], v.[Module], v.[Description] FROM (VALUES
    (3001, N''Orme.View'', N''Orme'', N''Örme ekranlarını görüntüleme''),
    (3002, N''Orme.Customer.View'', N''Orme'', N''Örme müşterilerini görüntüleme''),
    (3003, N''Orme.Customer.Manage'', N''Orme'', N''Örme müşterilerini oluşturma ve düzenleme''),
    (3004, N''Orme.Material.View'', N''Orme'', N''İplik ve kumaş kartlarını görüntüleme''),
    (3005, N''Orme.Material.Manage'', N''Orme'', N''İplik ve kumaş kartlarını oluşturma ve düzenleme''),
    (3006, N''Orme.Machine.View'', N''Orme'', N''Örme makinelerini görüntüleme''),
    (3007, N''Orme.Machine.Manage'', N''Orme'', N''Örme makinelerini oluşturma ve düzenleme''),
    (3008, N''Orme.Purchase.View'', N''Orme'', N''Tedarikçileri görüntüleme''),
    (3009, N''Orme.Purchase.Manage'', N''Orme'', N''Tedarikçi kartlarını oluşturma ve düzenleme'')
    ) v([Id], [Code], [Module], [Description])
    WHERE NOT EXISTS (SELECT 1 FROM [Core].[Permissions] p WHERE p.[Id] = v.[Id] OR p.[Code] = v.[Code])');
    SET IDENTITY_INSERT [Core].[Permissions] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101200356_InitialOrme'
)
BEGIN
    INSERT INTO [Core].[RolePermissions] ([RoleId], [PermissionId])
    SELECT v.[RoleId], v.[PermissionId] FROM (VALUES
    (1, 3001), (1, 3002), (1, 3003), (1, 3004), (1, 3005), (1, 3006), (1, 3007), (1, 3008), (1, 3009)
    ) v([RoleId], [PermissionId])
    WHERE EXISTS (SELECT 1 FROM [Core].[Roles] r WHERE r.[Id] = v.[RoleId])
      AND EXISTS (SELECT 1 FROM [Core].[Permissions] p WHERE p.[Id] = v.[PermissionId])
      AND NOT EXISTS (SELECT 1 FROM [Core].[RolePermissions] rp WHERE rp.[RoleId] = v.[RoleId] AND rp.[PermissionId] = v.[PermissionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101200356_InitialOrme'
)
BEGIN
    INSERT INTO [Orme].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261101200356_InitialOrme', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101201757_AddOrdersAndYarnLots'
)
BEGIN
    CREATE TABLE [Orme].[Orders] (
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
        CONSTRAINT [FK_Orders_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Orme].[Customers] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101201757_AddOrdersAndYarnLots'
)
BEGIN
    CREATE TABLE [Orme].[YarnLots] (
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
        CONSTRAINT [FK_YarnLots_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Orme].[Customers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_YarnLots_Suppliers_SupplierId] FOREIGN KEY ([SupplierId]) REFERENCES [Orme].[Suppliers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_YarnLots_Yarns_YarnId] FOREIGN KEY ([YarnId]) REFERENCES [Orme].[Yarns] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101201757_AddOrdersAndYarnLots'
)
BEGIN
    CREATE TABLE [Orme].[OrderLines] (
        [Id] int NOT NULL IDENTITY,
        [OrderId] int NOT NULL,
        [FabricId] int NOT NULL,
        [Kilograms] decimal(12,2) NOT NULL,
        [UnitPrice] decimal(18,4) NULL,
        [Notes] nvarchar(500) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_OrderLines] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_OrderLines_Kilograms] CHECK ([Kilograms] > 0),
        CONSTRAINT [CK_OrderLines_UnitPrice] CHECK ([UnitPrice] IS NULL OR [UnitPrice] >= 0),
        CONSTRAINT [FK_OrderLines_Fabrics_FabricId] FOREIGN KEY ([FabricId]) REFERENCES [Orme].[Fabrics] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_OrderLines_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orme].[Orders] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101201757_AddOrdersAndYarnLots'
)
BEGIN
    CREATE INDEX [IX_OrderLines_FabricId] ON [Orme].[OrderLines] ([FabricId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101201757_AddOrdersAndYarnLots'
)
BEGIN
    CREATE INDEX [IX_OrderLines_OrderId] ON [Orme].[OrderLines] ([OrderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101201757_AddOrdersAndYarnLots'
)
BEGIN
    CREATE INDEX [IX_Orders_CustomerId] ON [Orme].[Orders] ([CustomerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101201757_AddOrdersAndYarnLots'
)
BEGIN
    CREATE INDEX [IX_Orders_OrderDate] ON [Orme].[Orders] ([OrderDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101201757_AddOrdersAndYarnLots'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Orders_OrderNo] ON [Orme].[Orders] ([OrderNo]);
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101201757_AddOrdersAndYarnLots'
)
BEGIN
    CREATE INDEX [IX_YarnLots_CustomerId] ON [Orme].[YarnLots] ([CustomerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101201757_AddOrdersAndYarnLots'
)
BEGIN
    CREATE UNIQUE INDEX [IX_YarnLots_LotNo] ON [Orme].[YarnLots] ([LotNo]);
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101201757_AddOrdersAndYarnLots'
)
BEGIN
    CREATE INDEX [IX_YarnLots_SupplierId] ON [Orme].[YarnLots] ([SupplierId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101201757_AddOrdersAndYarnLots'
)
BEGIN
    CREATE INDEX [IX_YarnLots_YarnId_CustomerId] ON [Orme].[YarnLots] ([YarnId], [CustomerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101201757_AddOrdersAndYarnLots'
)
BEGIN
    SET IDENTITY_INSERT [Core].[Permissions] ON;
    EXEC(N'INSERT INTO [Core].[Permissions] ([Id], [Code], [Module], [Description])
    SELECT v.[Id], v.[Code], v.[Module], v.[Description] FROM (VALUES
    (3010, N''Orme.Order.View'', N''Orme'', N''Örme siparişlerini ve iplik ihtiyacını görüntüleme''),
    (3011, N''Orme.Order.Manage'', N''Orme'', N''Örme siparişlerini oluşturma, onaylama ve iptal etme''),
    (3012, N''Orme.Receipt.View'', N''Orme'', N''İplik girişlerini görüntüleme''),
    (3013, N''Orme.Receipt.Manage'', N''Orme'', N''İplik girişi yapma, düzenleme ve silme'')
    ) v([Id], [Code], [Module], [Description])
    WHERE NOT EXISTS (SELECT 1 FROM [Core].[Permissions] p WHERE p.[Id] = v.[Id] OR p.[Code] = v.[Code])');
    SET IDENTITY_INSERT [Core].[Permissions] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101201757_AddOrdersAndYarnLots'
)
BEGIN
    INSERT INTO [Core].[RolePermissions] ([RoleId], [PermissionId])
    SELECT v.[RoleId], v.[PermissionId] FROM (VALUES
    (1, 3010), (1, 3011), (1, 3012), (1, 3013)
    ) v([RoleId], [PermissionId])
    WHERE EXISTS (SELECT 1 FROM [Core].[Roles] r WHERE r.[Id] = v.[RoleId])
      AND EXISTS (SELECT 1 FROM [Core].[Permissions] p WHERE p.[Id] = v.[PermissionId])
      AND NOT EXISTS (SELECT 1 FROM [Core].[RolePermissions] rp WHERE rp.[RoleId] = v.[RoleId] AND rp.[PermissionId] = v.[PermissionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101201757_AddOrdersAndYarnLots'
)
BEGIN
    INSERT INTO [Orme].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261101201757_AddOrdersAndYarnLots', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101203850_AddKnitting'
)
BEGIN
    CREATE TABLE [Orme].[KnitOrders] (
        [Id] int NOT NULL IDENTITY,
        [KnitOrderNo] nvarchar(30) NOT NULL,
        [OrderLineId] int NOT NULL,
        [PlannedDate] date NOT NULL,
        [PlannedKilograms] decimal(12,2) NOT NULL,
        [CompletedOn] date NULL,
        [Notes] nvarchar(1000) NULL,
        [Status] nvarchar(30) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_KnitOrders] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_KnitOrders_PlannedKilograms] CHECK ([PlannedKilograms] > 0),
        CONSTRAINT [CK_KnitOrders_Status] CHECK ([Status] IN ('OPEN', 'COMPLETED', 'CANCELLED')),
        CONSTRAINT [FK_KnitOrders_OrderLines_OrderLineId] FOREIGN KEY ([OrderLineId]) REFERENCES [Orme].[OrderLines] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101203850_AddKnitting'
)
BEGIN
    CREATE TABLE [Orme].[FabricRolls] (
        [Id] int NOT NULL IDENTITY,
        [RollNo] nvarchar(30) NOT NULL,
        [KnitOrderId] int NOT NULL,
        [MachineId] int NOT NULL,
        [CutOn] date NOT NULL,
        [Kilograms] decimal(12,2) NOT NULL,
        [Meters] decimal(12,2) NULL,
        [Notes] nvarchar(500) NULL,
        [Status] nvarchar(30) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_FabricRolls] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_FabricRolls_Kilograms] CHECK ([Kilograms] > 0),
        CONSTRAINT [CK_FabricRolls_Meters] CHECK ([Meters] IS NULL OR [Meters] > 0),
        CONSTRAINT [CK_FabricRolls_Status] CHECK ([Status] IN ('PENDING_INSPECTION', 'FIRST_QUALITY', 'SECOND_QUALITY', 'REJECTED')),
        CONSTRAINT [FK_FabricRolls_KnitOrders_KnitOrderId] FOREIGN KEY ([KnitOrderId]) REFERENCES [Orme].[KnitOrders] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_FabricRolls_Machines_MachineId] FOREIGN KEY ([MachineId]) REFERENCES [Orme].[Machines] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101203850_AddKnitting'
)
BEGIN
    CREATE TABLE [Orme].[KnitOrderMachines] (
        [Id] int NOT NULL IDENTITY,
        [KnitOrderId] int NOT NULL,
        [MachineId] int NOT NULL,
        [AssignedOn] date NOT NULL,
        [ReleasedOn] date NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_KnitOrderMachines] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_KnitOrderMachines_Released] CHECK ([ReleasedOn] IS NULL OR [ReleasedOn] >= [AssignedOn]),
        CONSTRAINT [FK_KnitOrderMachines_KnitOrders_KnitOrderId] FOREIGN KEY ([KnitOrderId]) REFERENCES [Orme].[KnitOrders] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_KnitOrderMachines_Machines_MachineId] FOREIGN KEY ([MachineId]) REFERENCES [Orme].[Machines] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101203850_AddKnitting'
)
BEGIN
    CREATE TABLE [Orme].[YarnIssues] (
        [Id] int NOT NULL IDENTITY,
        [KnitOrderId] int NOT NULL,
        [YarnLotId] int NOT NULL,
        [IssuedOn] date NOT NULL,
        [Kilograms] decimal(12,2) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_YarnIssues] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_YarnIssues_Kilograms] CHECK ([Kilograms] > 0),
        CONSTRAINT [FK_YarnIssues_KnitOrders_KnitOrderId] FOREIGN KEY ([KnitOrderId]) REFERENCES [Orme].[KnitOrders] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_YarnIssues_YarnLots_YarnLotId] FOREIGN KEY ([YarnLotId]) REFERENCES [Orme].[YarnLots] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101203850_AddKnitting'
)
BEGIN
    CREATE TABLE [Orme].[YarnTakeBacks] (
        [Id] int NOT NULL IDENTITY,
        [KnitOrderId] int NOT NULL,
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
        CONSTRAINT [FK_YarnTakeBacks_KnitOrders_KnitOrderId] FOREIGN KEY ([KnitOrderId]) REFERENCES [Orme].[KnitOrders] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_YarnTakeBacks_YarnLots_YarnLotId] FOREIGN KEY ([YarnLotId]) REFERENCES [Orme].[YarnLots] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101203850_AddKnitting'
)
BEGIN
    CREATE INDEX [IX_FabricRolls_KnitOrderId] ON [Orme].[FabricRolls] ([KnitOrderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101203850_AddKnitting'
)
BEGIN
    CREATE INDEX [IX_FabricRolls_MachineId] ON [Orme].[FabricRolls] ([MachineId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101203850_AddKnitting'
)
BEGIN
    CREATE UNIQUE INDEX [IX_FabricRolls_RollNo] ON [Orme].[FabricRolls] ([RollNo]);
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101203850_AddKnitting'
)
BEGIN
    CREATE INDEX [IX_FabricRolls_Status] ON [Orme].[FabricRolls] ([Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101203850_AddKnitting'
)
BEGIN
    CREATE INDEX [IX_KnitOrderMachines_KnitOrderId] ON [Orme].[KnitOrderMachines] ([KnitOrderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101203850_AddKnitting'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_KnitOrderMachines_RunningMachine] ON [Orme].[KnitOrderMachines] ([MachineId]) WHERE [ReleasedOn] IS NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101203850_AddKnitting'
)
BEGIN
    CREATE UNIQUE INDEX [IX_KnitOrders_KnitOrderNo] ON [Orme].[KnitOrders] ([KnitOrderNo]);
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101203850_AddKnitting'
)
BEGIN
    CREATE INDEX [IX_KnitOrders_OrderLineId] ON [Orme].[KnitOrders] ([OrderLineId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101203850_AddKnitting'
)
BEGIN
    CREATE INDEX [IX_YarnIssues_KnitOrderId] ON [Orme].[YarnIssues] ([KnitOrderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101203850_AddKnitting'
)
BEGIN
    CREATE INDEX [IX_YarnIssues_YarnLotId] ON [Orme].[YarnIssues] ([YarnLotId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101203850_AddKnitting'
)
BEGIN
    CREATE INDEX [IX_YarnTakeBacks_KnitOrderId] ON [Orme].[YarnTakeBacks] ([KnitOrderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101203850_AddKnitting'
)
BEGIN
    CREATE INDEX [IX_YarnTakeBacks_YarnLotId] ON [Orme].[YarnTakeBacks] ([YarnLotId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101203850_AddKnitting'
)
BEGIN
    SET IDENTITY_INSERT [Core].[Permissions] ON;
    EXEC(N'INSERT INTO [Core].[Permissions] ([Id], [Code], [Module], [Description])
    SELECT v.[Id], v.[Code], v.[Module], v.[Description] FROM (VALUES
    (3014, N''Orme.Knitting.View'', N''Orme'', N''Örme emirlerini, makine panosunu ve ham topları görüntüleme''),
    (3015, N''Orme.Knitting.Manage'', N''Orme'', N''Örme emri açma, makine atama, iplik verme ve top kaydetme'')
    ) v([Id], [Code], [Module], [Description])
    WHERE NOT EXISTS (SELECT 1 FROM [Core].[Permissions] p WHERE p.[Id] = v.[Id] OR p.[Code] = v.[Code])');
    SET IDENTITY_INSERT [Core].[Permissions] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101203850_AddKnitting'
)
BEGIN
    INSERT INTO [Core].[RolePermissions] ([RoleId], [PermissionId])
    SELECT v.[RoleId], v.[PermissionId] FROM (VALUES
    (1, 3014), (1, 3015)
    ) v([RoleId], [PermissionId])
    WHERE EXISTS (SELECT 1 FROM [Core].[Roles] r WHERE r.[Id] = v.[RoleId])
      AND EXISTS (SELECT 1 FROM [Core].[Permissions] p WHERE p.[Id] = v.[PermissionId])
      AND NOT EXISTS (SELECT 1 FROM [Core].[RolePermissions] rp WHERE rp.[RoleId] = v.[RoleId] AND rp.[PermissionId] = v.[PermissionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101203850_AddKnitting'
)
BEGIN
    INSERT INTO [Orme].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261101203850_AddKnitting', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102043248_AddRollInspection'
)
BEGIN
    ALTER TABLE [Orme].[FabricRolls] ADD [CalculatedStatus] nvarchar(30) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102043248_AddRollInspection'
)
BEGIN
    ALTER TABLE [Orme].[FabricRolls] ADD [InspectedMeters] decimal(12,2) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102043248_AddRollInspection'
)
BEGIN
    ALTER TABLE [Orme].[FabricRolls] ADD [InspectedMetersEstimated] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102043248_AddRollInspection'
)
BEGIN
    ALTER TABLE [Orme].[FabricRolls] ADD [InspectedOn] date NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102043248_AddRollInspection'
)
BEGIN
    ALTER TABLE [Orme].[FabricRolls] ADD [InspectedWeightGsm] decimal(7,2) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102043248_AddRollInspection'
)
BEGIN
    ALTER TABLE [Orme].[FabricRolls] ADD [InspectedWidthCm] decimal(6,2) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102043248_AddRollInspection'
)
BEGIN
    ALTER TABLE [Orme].[FabricRolls] ADD [InspectionNotes] nvarchar(500) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102043248_AddRollInspection'
)
BEGIN
    ALTER TABLE [Orme].[FabricRolls] ADD [OverrideNote] nvarchar(500) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102043248_AddRollInspection'
)
BEGIN
    ALTER TABLE [Orme].[FabricRolls] ADD [PointsPer100M2] decimal(8,2) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102043248_AddRollInspection'
)
BEGIN
    ALTER TABLE [Orme].[FabricRolls] ADD [SpecDeviation] nvarchar(200) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102043248_AddRollInspection'
)
BEGIN
    ALTER TABLE [Orme].[FabricRolls] ADD [TotalPoints] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102043248_AddRollInspection'
)
BEGIN
    CREATE TABLE [Orme].[DefectTypes] (
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
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102043248_AddRollInspection'
)
BEGIN
    CREATE TABLE [Orme].[QualitySettings] (
        [Id] int NOT NULL IDENTITY,
        [FirstQualityMaxPoints] decimal(8,2) NOT NULL,
        [SecondQualityMaxPoints] decimal(8,2) NOT NULL,
        [WidthTolerancePercent] decimal(5,2) NOT NULL,
        [WeightTolerancePercent] decimal(5,2) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_QualitySettings] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_QualitySettings_Limits] CHECK ([FirstQualityMaxPoints] >= 0 AND [SecondQualityMaxPoints] >= [FirstQualityMaxPoints]),
        CONSTRAINT [CK_QualitySettings_Tolerances] CHECK ([WidthTolerancePercent] BETWEEN 0 AND 100 AND [WeightTolerancePercent] BETWEEN 0 AND 100)
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102043248_AddRollInspection'
)
BEGIN
    CREATE TABLE [Orme].[RollDefects] (
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
        CONSTRAINT [FK_RollDefects_DefectTypes_DefectTypeId] FOREIGN KEY ([DefectTypeId]) REFERENCES [Orme].[DefectTypes] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_RollDefects_FabricRolls_RollId] FOREIGN KEY ([RollId]) REFERENCES [Orme].[FabricRolls] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102043248_AddRollInspection'
)
BEGIN
    EXEC(N'ALTER TABLE [Orme].[FabricRolls] ADD CONSTRAINT [CK_FabricRolls_CalculatedStatus] CHECK ([CalculatedStatus] IS NULL OR [CalculatedStatus] IN (''PENDING_INSPECTION'', ''FIRST_QUALITY'', ''SECOND_QUALITY'', ''REJECTED''))');
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102043248_AddRollInspection'
)
BEGIN
    EXEC(N'ALTER TABLE [Orme].[FabricRolls] ADD CONSTRAINT [CK_FabricRolls_InspectedMeters] CHECK ([InspectedMeters] IS NULL OR [InspectedMeters] > 0)');
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102043248_AddRollInspection'
)
BEGIN
    EXEC(N'ALTER TABLE [Orme].[FabricRolls] ADD CONSTRAINT [CK_FabricRolls_InspectedWeightGsm] CHECK ([InspectedWeightGsm] IS NULL OR [InspectedWeightGsm] > 0)');
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102043248_AddRollInspection'
)
BEGIN
    EXEC(N'ALTER TABLE [Orme].[FabricRolls] ADD CONSTRAINT [CK_FabricRolls_InspectedWidthCm] CHECK ([InspectedWidthCm] IS NULL OR [InspectedWidthCm] > 0)');
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102043248_AddRollInspection'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_DefectTypes_Code] ON [Orme].[DefectTypes] ([Code]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102043248_AddRollInspection'
)
BEGIN
    CREATE INDEX [IX_RollDefects_DefectTypeId] ON [Orme].[RollDefects] ([DefectTypeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102043248_AddRollInspection'
)
BEGIN
    CREATE INDEX [IX_RollDefects_RollId] ON [Orme].[RollDefects] ([RollId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102043248_AddRollInspection'
)
BEGIN
    EXEC(N'INSERT INTO [Orme].[DefectTypes] ([Code], [Name], [AlwaysFourPoints], [IsActive], [CreatedAt], [IsDeleted])
    SELECT v.[Code], v.[Name], v.[AlwaysFourPoints], 1, SYSUTCDATETIME(), 0 FROM (VALUES
    (N''IGNE-CIZGISI'', N''İğne çizgisi'', 0),
    (N''ILMEK-KACIGI'', N''İlmek kaçığı'', 0),
    (N''BARE'', N''Bare (enine çizgi)'', 0),
    (N''KALIN-YER'', N''Düğüm / kalın yer'', 0),
    (N''IPLIK-KOPUGU'', N''İplik kopuğu'', 0),
    (N''YAG-LEKESI'', N''Yağ lekesi'', 0),
    (N''LEKE'', N''Leke'', 0),
    (N''DELIK'', N''Delik'', 1)
    ) v([Code], [Name], [AlwaysFourPoints])
    WHERE NOT EXISTS (SELECT 1 FROM [Orme].[DefectTypes] d WHERE d.[Code] = v.[Code])');
    EXEC(N'INSERT INTO [Orme].[QualitySettings] ([FirstQualityMaxPoints], [SecondQualityMaxPoints], [WidthTolerancePercent], [WeightTolerancePercent], [CreatedAt])
    SELECT 20, 40, 3, 5, SYSUTCDATETIME() WHERE NOT EXISTS (SELECT 1 FROM [Orme].[QualitySettings])');
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102043248_AddRollInspection'
)
BEGIN
    SET IDENTITY_INSERT [Core].[Permissions] ON;
    EXEC(N'INSERT INTO [Core].[Permissions] ([Id], [Code], [Module], [Description])
    SELECT v.[Id], v.[Code], v.[Module], v.[Description] FROM (VALUES
    (3016, N''Orme.Inspection.View'', N''Orme'', N''Ham kontrolü görüntüleme''),
    (3017, N''Orme.Inspection.Manage'', N''Orme'', N''Ham kontrol yapma, hata türleri ve kalite sınırları''),
    (3018, N''Orme.Inspection.Override'', N''Orme'', N''Hesaplanan kaliteyi gerekçeyle değiştirme'')
    ) v([Id], [Code], [Module], [Description])
    WHERE NOT EXISTS (SELECT 1 FROM [Core].[Permissions] p WHERE p.[Id] = v.[Id] OR p.[Code] = v.[Code])');
    SET IDENTITY_INSERT [Core].[Permissions] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102043248_AddRollInspection'
)
BEGIN
    INSERT INTO [Core].[RolePermissions] ([RoleId], [PermissionId])
    SELECT v.[RoleId], v.[PermissionId] FROM (VALUES
    (1, 3016), (1, 3017), (1, 3018)
    ) v([RoleId], [PermissionId])
    WHERE EXISTS (SELECT 1 FROM [Core].[Roles] r WHERE r.[Id] = v.[RoleId])
      AND EXISTS (SELECT 1 FROM [Core].[Permissions] p WHERE p.[Id] = v.[PermissionId])
      AND NOT EXISTS (SELECT 1 FROM [Core].[RolePermissions] rp WHERE rp.[RoleId] = v.[RoleId] AND rp.[PermissionId] = v.[PermissionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102043248_AddRollInspection'
)
BEGIN
    INSERT INTO [Orme].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261102043248_AddRollInspection', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102045042_AddShippingAndAccounts'
)
BEGIN
    CREATE TABLE [Orme].[Invoices] (
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
        CONSTRAINT [FK_Invoices_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Orme].[Customers] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102045042_AddShippingAndAccounts'
)
BEGIN
    CREATE TABLE [Orme].[Payments] (
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
        CONSTRAINT [FK_Payments_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Orme].[Customers] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102045042_AddShippingAndAccounts'
)
BEGIN
    CREATE TABLE [Orme].[YarnReturns] (
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
        CONSTRAINT [FK_YarnReturns_YarnLots_YarnLotId] FOREIGN KEY ([YarnLotId]) REFERENCES [Orme].[YarnLots] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102045042_AddShippingAndAccounts'
)
BEGIN
    CREATE TABLE [Orme].[Despatches] (
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
        CONSTRAINT [FK_Despatches_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Orme].[Customers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Despatches_Invoices_InvoiceId] FOREIGN KEY ([InvoiceId]) REFERENCES [Orme].[Invoices] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102045042_AddShippingAndAccounts'
)
BEGIN
    CREATE TABLE [Orme].[InvoiceLines] (
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
        CONSTRAINT [FK_InvoiceLines_Invoices_InvoiceId] FOREIGN KEY ([InvoiceId]) REFERENCES [Orme].[Invoices] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_InvoiceLines_OrderLines_OrderLineId] FOREIGN KEY ([OrderLineId]) REFERENCES [Orme].[OrderLines] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102045042_AddShippingAndAccounts'
)
BEGIN
    CREATE TABLE [Orme].[PaymentAllocations] (
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
        CONSTRAINT [FK_PaymentAllocations_Invoices_InvoiceId] FOREIGN KEY ([InvoiceId]) REFERENCES [Orme].[Invoices] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PaymentAllocations_Payments_PaymentId] FOREIGN KEY ([PaymentId]) REFERENCES [Orme].[Payments] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102045042_AddShippingAndAccounts'
)
BEGIN
    CREATE TABLE [Orme].[PackingLists] (
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
        CONSTRAINT [FK_PackingLists_Customers_SaleCustomerId] FOREIGN KEY ([SaleCustomerId]) REFERENCES [Orme].[Customers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PackingLists_Despatches_DespatchId] FOREIGN KEY ([DespatchId]) REFERENCES [Orme].[Despatches] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PackingLists_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orme].[Orders] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102045042_AddShippingAndAccounts'
)
BEGIN
    CREATE TABLE [Orme].[PackingItems] (
        [Id] int NOT NULL IDENTITY,
        [PackingListId] int NOT NULL,
        [RollId] int NOT NULL,
        [Kilograms] decimal(12,2) NOT NULL,
        [Meters] decimal(12,2) NOT NULL,
        [MetersEstimated] bit NOT NULL,
        [Grade] nvarchar(30) NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_PackingItems] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_PackingItems_Grade] CHECK ([Grade] IN ('FIRST_QUALITY', 'SECOND_QUALITY')),
        CONSTRAINT [CK_PackingItems_Kilograms] CHECK ([Kilograms] > 0),
        CONSTRAINT [CK_PackingItems_Meters] CHECK ([Meters] > 0),
        CONSTRAINT [FK_PackingItems_FabricRolls_RollId] FOREIGN KEY ([RollId]) REFERENCES [Orme].[FabricRolls] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PackingItems_PackingLists_PackingListId] FOREIGN KEY ([PackingListId]) REFERENCES [Orme].[PackingLists] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102045042_AddShippingAndAccounts'
)
BEGIN
    CREATE INDEX [IX_Despatches_CustomerId] ON [Orme].[Despatches] ([CustomerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102045042_AddShippingAndAccounts'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Despatches_DespatchNo] ON [Orme].[Despatches] ([DespatchNo]);
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102045042_AddShippingAndAccounts'
)
BEGIN
    CREATE INDEX [IX_Despatches_InvoiceId] ON [Orme].[Despatches] ([InvoiceId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102045042_AddShippingAndAccounts'
)
BEGIN
    CREATE INDEX [IX_InvoiceLines_InvoiceId] ON [Orme].[InvoiceLines] ([InvoiceId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102045042_AddShippingAndAccounts'
)
BEGIN
    CREATE INDEX [IX_InvoiceLines_OrderLineId] ON [Orme].[InvoiceLines] ([OrderLineId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102045042_AddShippingAndAccounts'
)
BEGIN
    CREATE INDEX [IX_Invoices_CustomerId] ON [Orme].[Invoices] ([CustomerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102045042_AddShippingAndAccounts'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Invoices_InvoiceNo] ON [Orme].[Invoices] ([InvoiceNo]);
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102045042_AddShippingAndAccounts'
)
BEGIN
    CREATE INDEX [IX_PackingItems_PackingListId] ON [Orme].[PackingItems] ([PackingListId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102045042_AddShippingAndAccounts'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_PackingItems_Active] ON [Orme].[PackingItems] ([RollId]) WHERE [IsActive] = 1');
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102045042_AddShippingAndAccounts'
)
BEGIN
    CREATE INDEX [IX_PackingLists_DespatchId] ON [Orme].[PackingLists] ([DespatchId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102045042_AddShippingAndAccounts'
)
BEGIN
    CREATE INDEX [IX_PackingLists_OrderId] ON [Orme].[PackingLists] ([OrderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102045042_AddShippingAndAccounts'
)
BEGIN
    CREATE UNIQUE INDEX [IX_PackingLists_PackingNo] ON [Orme].[PackingLists] ([PackingNo]);
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102045042_AddShippingAndAccounts'
)
BEGIN
    CREATE INDEX [IX_PackingLists_SaleCustomerId] ON [Orme].[PackingLists] ([SaleCustomerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102045042_AddShippingAndAccounts'
)
BEGIN
    CREATE INDEX [IX_PaymentAllocations_InvoiceId] ON [Orme].[PaymentAllocations] ([InvoiceId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102045042_AddShippingAndAccounts'
)
BEGIN
    CREATE UNIQUE INDEX [IX_PaymentAllocations_PaymentId_InvoiceId] ON [Orme].[PaymentAllocations] ([PaymentId], [InvoiceId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102045042_AddShippingAndAccounts'
)
BEGIN
    CREATE INDEX [IX_Payments_CustomerId_Currency_PaymentDate] ON [Orme].[Payments] ([CustomerId], [Currency], [PaymentDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102045042_AddShippingAndAccounts'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Payments_PaymentNo] ON [Orme].[Payments] ([PaymentNo]);
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102045042_AddShippingAndAccounts'
)
BEGIN
    CREATE UNIQUE INDEX [IX_YarnReturns_ReturnNo] ON [Orme].[YarnReturns] ([ReturnNo]);
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102045042_AddShippingAndAccounts'
)
BEGIN
    CREATE INDEX [IX_YarnReturns_YarnLotId] ON [Orme].[YarnReturns] ([YarnLotId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102045042_AddShippingAndAccounts'
)
BEGIN
    SET IDENTITY_INSERT [Core].[Permissions] ON;
    EXEC(N'INSERT INTO [Core].[Permissions] ([Id], [Code], [Module], [Description])
    SELECT v.[Id], v.[Code], v.[Module], v.[Description] FROM (VALUES
    (3019, N''Orme.Packing.View'', N''Orme'', N''Çeki listelerini görüntüleme''),
    (3020, N''Orme.Packing.Manage'', N''Orme'', N''Çeki listesi hazırlama, onaylama ve Boyahane''''ye gönderme''),
    (3021, N''Orme.Shipment.View'', N''Orme'', N''İrsaliyeleri görüntüleme''),
    (3022, N''Orme.Shipment.Manage'', N''Orme'', N''İrsaliye hazırlama ve onaylama''),
    (3023, N''Orme.Invoice.View'', N''Orme'', N''Faturaları görüntüleme''),
    (3024, N''Orme.Invoice.Manage'', N''Orme'', N''Fatura hazırlama ve onaylama''),
    (3025, N''Orme.Account.View'', N''Orme'', N''Cari hesapları, ekstreyi ve tahsilatları görüntüleme''),
    (3026, N''Orme.Account.Manage'', N''Orme'', N''Tahsilat girişi, faturalara dağıtım ve iptal'')
    ) v([Id], [Code], [Module], [Description])
    WHERE NOT EXISTS (SELECT 1 FROM [Core].[Permissions] p WHERE p.[Id] = v.[Id] OR p.[Code] = v.[Code])');
    SET IDENTITY_INSERT [Core].[Permissions] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102045042_AddShippingAndAccounts'
)
BEGIN
    INSERT INTO [Core].[RolePermissions] ([RoleId], [PermissionId])
    SELECT v.[RoleId], v.[PermissionId] FROM (VALUES
    (1, 3019), (1, 3020), (1, 3021), (1, 3022), (1, 3023), (1, 3024), (1, 3025), (1, 3026)
    ) v([RoleId], [PermissionId])
    WHERE EXISTS (SELECT 1 FROM [Core].[Roles] r WHERE r.[Id] = v.[RoleId])
      AND EXISTS (SELECT 1 FROM [Core].[Permissions] p WHERE p.[Id] = v.[PermissionId])
      AND NOT EXISTS (SELECT 1 FROM [Core].[RolePermissions] rp WHERE rp.[RoleId] = v.[RoleId] AND rp.[PermissionId] = v.[PermissionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102045042_AddShippingAndAccounts'
)
BEGIN
    INSERT INTO [Orme].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261102045042_AddShippingAndAccounts', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102050704_AddFloorAndReports'
)
BEGIN
    ALTER TABLE [Orme].[FabricRolls] ADD [ClientRequestId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102050704_AddFloorAndReports'
)
BEGIN
    ALTER TABLE [Orme].[FabricRolls] ADD [CutByOperatorId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102050704_AddFloorAndReports'
)
BEGIN
    CREATE TABLE [Orme].[BreakGroups] (
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
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102050704_AddFloorAndReports'
)
BEGIN
    CREATE TABLE [Orme].[RfidCards] (
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
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102050704_AddFloorAndReports'
)
BEGIN
    CREATE TABLE [Orme].[Shifts] (
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
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102050704_AddFloorAndReports'
)
BEGIN
    CREATE TABLE [Orme].[StopReasons] (
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
        CONSTRAINT [CK_StopReasons_Category] CHECK ([Category] IN ('PAUSE', 'STOP', 'BREAKDOWN', 'PLANNED'))
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102050704_AddFloorAndReports'
)
BEGIN
    CREATE TABLE [Orme].[Tablets] (
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
        CONSTRAINT [FK_Tablets_Machines_MachineId] FOREIGN KEY ([MachineId]) REFERENCES [Orme].[Machines] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102050704_AddFloorAndReports'
)
BEGIN
    CREATE TABLE [Orme].[Operators] (
        [Id] int NOT NULL IDENTITY,
        [Code] nvarchar(30) NOT NULL,
        [FirstName] nvarchar(100) NOT NULL,
        [LastName] nvarchar(100) NOT NULL,
        [Name] nvarchar(201) NOT NULL,
        [Role] nvarchar(30) NOT NULL,
        [CardUid] nvarchar(10) NULL,
        [BreakGroupId] int NULL,
        [CanPause] bit NOT NULL,
        [CanReportBreakdown] bit NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_Operators] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Operators_Role] CHECK ([Role] IN ('KNITTER', 'SUPERVISOR', 'MAINTENANCE')),
        CONSTRAINT [FK_Operators_BreakGroups_BreakGroupId] FOREIGN KEY ([BreakGroupId]) REFERENCES [Orme].[BreakGroups] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102050704_AddFloorAndReports'
)
BEGIN
    CREATE TABLE [Orme].[BreakSchedules] (
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
        CONSTRAINT [FK_BreakSchedules_BreakGroups_BreakGroupId] FOREIGN KEY ([BreakGroupId]) REFERENCES [Orme].[BreakGroups] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_BreakSchedules_Shifts_ShiftId] FOREIGN KEY ([ShiftId]) REFERENCES [Orme].[Shifts] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102050704_AddFloorAndReports'
)
BEGIN
    CREATE TABLE [Orme].[MachineStops] (
        [Id] bigint NOT NULL IDENTITY,
        [MachineId] int NOT NULL,
        [KnitOrderId] int NULL,
        [ReasonId] int NOT NULL,
        [Category] nvarchar(30) NOT NULL,
        [Note] nvarchar(500) NULL,
        [StartedAt] datetime2 NOT NULL,
        [EndedAt] datetime2 NULL,
        [StartedBy] nvarchar(150) NOT NULL,
        [EndedBy] nvarchar(150) NULL,
        [TabletId] int NULL,
        CONSTRAINT [PK_MachineStops] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_MachineStops_Category] CHECK ([Category] IN ('PAUSE', 'STOP', 'BREAKDOWN', 'PLANNED')),
        CONSTRAINT [FK_MachineStops_KnitOrders_KnitOrderId] FOREIGN KEY ([KnitOrderId]) REFERENCES [Orme].[KnitOrders] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_MachineStops_Machines_MachineId] FOREIGN KEY ([MachineId]) REFERENCES [Orme].[Machines] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_MachineStops_StopReasons_ReasonId] FOREIGN KEY ([ReasonId]) REFERENCES [Orme].[StopReasons] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102050704_AddFloorAndReports'
)
BEGIN
    CREATE TABLE [Orme].[OperatorMachines] (
        [OperatorId] int NOT NULL,
        [MachineId] int NOT NULL,
        CONSTRAINT [PK_OperatorMachines] PRIMARY KEY ([OperatorId], [MachineId]),
        CONSTRAINT [FK_OperatorMachines_Machines_MachineId] FOREIGN KEY ([MachineId]) REFERENCES [Orme].[Machines] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_OperatorMachines_Operators_OperatorId] FOREIGN KEY ([OperatorId]) REFERENCES [Orme].[Operators] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102050704_AddFloorAndReports'
)
BEGIN
    CREATE TABLE [Orme].[OperatorPhotos] (
        [OperatorId] int NOT NULL,
        [Content] varbinary(max) NOT NULL,
        [ContentType] nvarchar(30) NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_OperatorPhotos] PRIMARY KEY ([OperatorId]),
        CONSTRAINT [FK_OperatorPhotos_Operators_OperatorId] FOREIGN KEY ([OperatorId]) REFERENCES [Orme].[Operators] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102050704_AddFloorAndReports'
)
BEGIN
    CREATE TABLE [Orme].[TabletSessions] (
        [Id] bigint NOT NULL IDENTITY,
        [TabletId] int NOT NULL,
        [OperatorId] int NOT NULL,
        [StartedAt] datetime2 NOT NULL,
        [EndedAt] datetime2 NULL,
        [EndReason] nvarchar(30) NULL,
        CONSTRAINT [PK_TabletSessions] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_TabletSessions_EndReason] CHECK ([EndReason] IN ('LOGOUT', 'SHIFT_ENDED', 'TAKEN_OVER', 'MOVED_AWAY', 'CLOSED_BY_OFFICE')),
        CONSTRAINT [FK_TabletSessions_Operators_OperatorId] FOREIGN KEY ([OperatorId]) REFERENCES [Orme].[Operators] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_TabletSessions_Tablets_TabletId] FOREIGN KEY ([TabletId]) REFERENCES [Orme].[Tablets] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102050704_AddFloorAndReports'
)
BEGIN
    CREATE TABLE [Orme].[SessionPauses] (
        [Id] bigint NOT NULL IDENTITY,
        [SessionId] bigint NOT NULL,
        [Kind] nvarchar(30) NOT NULL,
        [ReasonId] int NULL,
        [BreakScheduleId] int NULL,
        [Note] nvarchar(500) NULL,
        [StartedAt] datetime2 NOT NULL,
        [EndedAt] datetime2 NULL,
        CONSTRAINT [PK_SessionPauses] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_SessionPauses_Kind] CHECK ([Kind] IN ('WAITING_FOR_ORDER', 'BREAK', 'PAUSE')),
        CONSTRAINT [FK_SessionPauses_BreakSchedules_BreakScheduleId] FOREIGN KEY ([BreakScheduleId]) REFERENCES [Orme].[BreakSchedules] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_SessionPauses_StopReasons_ReasonId] FOREIGN KEY ([ReasonId]) REFERENCES [Orme].[StopReasons] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_SessionPauses_TabletSessions_SessionId] FOREIGN KEY ([SessionId]) REFERENCES [Orme].[TabletSessions] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102050704_AddFloorAndReports'
)
BEGIN
    CREATE INDEX [IX_FabricRolls_CutByOperatorId] ON [Orme].[FabricRolls] ([CutByOperatorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102050704_AddFloorAndReports'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_FabricRolls_ClientRequestId] ON [Orme].[FabricRolls] ([ClientRequestId]) WHERE [ClientRequestId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102050704_AddFloorAndReports'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_BreakGroups_Code] ON [Orme].[BreakGroups] ([Code]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102050704_AddFloorAndReports'
)
BEGIN
    CREATE INDEX [IX_BreakSchedules_BreakGroupId] ON [Orme].[BreakSchedules] ([BreakGroupId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102050704_AddFloorAndReports'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_BreakSchedules_Code] ON [Orme].[BreakSchedules] ([Code]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102050704_AddFloorAndReports'
)
BEGIN
    CREATE INDEX [IX_BreakSchedules_ShiftId] ON [Orme].[BreakSchedules] ([ShiftId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102050704_AddFloorAndReports'
)
BEGIN
    CREATE INDEX [IX_MachineStops_KnitOrderId] ON [Orme].[MachineStops] ([KnitOrderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102050704_AddFloorAndReports'
)
BEGIN
    CREATE INDEX [IX_MachineStops_MachineId_StartedAt] ON [Orme].[MachineStops] ([MachineId], [StartedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102050704_AddFloorAndReports'
)
BEGIN
    CREATE INDEX [IX_MachineStops_ReasonId] ON [Orme].[MachineStops] ([ReasonId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102050704_AddFloorAndReports'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_MachineStops_Open] ON [Orme].[MachineStops] ([MachineId]) WHERE [EndedAt] IS NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102050704_AddFloorAndReports'
)
BEGIN
    CREATE INDEX [IX_OperatorMachines_MachineId] ON [Orme].[OperatorMachines] ([MachineId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102050704_AddFloorAndReports'
)
BEGIN
    CREATE INDEX [IX_Operators_BreakGroupId] ON [Orme].[Operators] ([BreakGroupId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102050704_AddFloorAndReports'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Operators_Code] ON [Orme].[Operators] ([Code]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102050704_AddFloorAndReports'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_Operators_CardUid] ON [Orme].[Operators] ([CardUid]) WHERE [CardUid] IS NOT NULL AND [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102050704_AddFloorAndReports'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_RfidCards_Uid] ON [Orme].[RfidCards] ([Uid]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102050704_AddFloorAndReports'
)
BEGIN
    CREATE INDEX [IX_SessionPauses_BreakScheduleId] ON [Orme].[SessionPauses] ([BreakScheduleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102050704_AddFloorAndReports'
)
BEGIN
    CREATE INDEX [IX_SessionPauses_ReasonId] ON [Orme].[SessionPauses] ([ReasonId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102050704_AddFloorAndReports'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_SessionPauses_Open] ON [Orme].[SessionPauses] ([SessionId]) WHERE [EndedAt] IS NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102050704_AddFloorAndReports'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Shifts_Code] ON [Orme].[Shifts] ([Code]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102050704_AddFloorAndReports'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_StopReasons_Code] ON [Orme].[StopReasons] ([Code]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102050704_AddFloorAndReports'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Tablets_TabletCode] ON [Orme].[Tablets] ([TabletCode]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102050704_AddFloorAndReports'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_Tablets_Device] ON [Orme].[Tablets] ([DeviceIdentifier]) WHERE [DeviceIdentifier] IS NOT NULL AND [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102050704_AddFloorAndReports'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_Tablets_Machine] ON [Orme].[Tablets] ([MachineId]) WHERE [MachineId] IS NOT NULL AND [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102050704_AddFloorAndReports'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_TabletSessions_Open] ON [Orme].[TabletSessions] ([TabletId]) WHERE [EndedAt] IS NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102050704_AddFloorAndReports'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_TabletSessions_OperatorOpen] ON [Orme].[TabletSessions] ([OperatorId]) WHERE [EndedAt] IS NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102050704_AddFloorAndReports'
)
BEGIN
    ALTER TABLE [Orme].[FabricRolls] ADD CONSTRAINT [FK_FabricRolls_Operators_CutByOperatorId] FOREIGN KEY ([CutByOperatorId]) REFERENCES [Orme].[Operators] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102050704_AddFloorAndReports'
)
BEGIN
    EXEC(N'INSERT INTO [Orme].[StopReasons] ([Code], [Name], [Category], [SortOrder], [IsActive], [CreatedAt], [IsDeleted])
    SELECT v.[Code], v.[Name], ''PAUSE'', v.[SortOrder], 1, SYSUTCDATETIME(), 0 FROM (VALUES
    (N''SU'', N''Su'', 10),
    (N''TUVALET'', N''Tuvalet'', 20),
    (N''KISISEL'', N''Kişisel ihtiyaç'', 30),
    (N''BEKLEME'', N''Bekleme'', 40),
    (N''DIGER'', N''Diğer'', 50)
    ) v([Code], [Name], [SortOrder])
    WHERE NOT EXISTS (SELECT 1 FROM [Orme].[StopReasons] r WHERE r.[Code] = v.[Code])');
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102050704_AddFloorAndReports'
)
BEGIN
    SET IDENTITY_INSERT [Core].[Permissions] ON;
    EXEC(N'INSERT INTO [Core].[Permissions] ([Id], [Code], [Module], [Description])
    SELECT v.[Id], v.[Code], v.[Module], v.[Description] FROM (VALUES
    (3027, N''Orme.Floor.View'', N''Orme'', N''Örme personelini, kartları, tabletleri ve oturumları görüntüleme''),
    (3028, N''Orme.Floor.Manage'', N''Orme'', N''Personel, kart, tablet ve duruş nedeni tanımlama; ofisten duruş ve oturum işlemleri''),
    (3029, N''Orme.Report.View'', N''Orme'', N''Makine verimi, sipariş takibi ve kalite raporları'')
    ) v([Id], [Code], [Module], [Description])
    WHERE NOT EXISTS (SELECT 1 FROM [Core].[Permissions] p WHERE p.[Id] = v.[Id] OR p.[Code] = v.[Code])');
    SET IDENTITY_INSERT [Core].[Permissions] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102050704_AddFloorAndReports'
)
BEGIN
    INSERT INTO [Core].[RolePermissions] ([RoleId], [PermissionId])
    SELECT v.[RoleId], v.[PermissionId] FROM (VALUES
    (1, 3027), (1, 3028), (1, 3029)
    ) v([RoleId], [PermissionId])
    WHERE EXISTS (SELECT 1 FROM [Core].[Roles] r WHERE r.[Id] = v.[RoleId])
      AND EXISTS (SELECT 1 FROM [Core].[Permissions] p WHERE p.[Id] = v.[PermissionId])
      AND NOT EXISTS (SELECT 1 FROM [Core].[RolePermissions] rp WHERE rp.[RoleId] = v.[RoleId] AND rp.[PermissionId] = v.[PermissionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102050704_AddFloorAndReports'
)
BEGIN
    INSERT INTO [Orme].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261102050704_AddFloorAndReports', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102052313_AddDefaultRoles'
)
BEGIN
    SET IDENTITY_INSERT [Core].[Roles] ON;
    EXEC(N'INSERT INTO [Core].[Roles] ([Id], [Code], [Name], [IsSystem], [ModuleKey])
    SELECT v.[Id], v.[Code], v.[Name], v.[IsSystem], v.[ModuleKey] FROM (VALUES
    (301, N''ORME_MANAGER'', N''Örme Yöneticisi'', 1, N''Orme''),
    (302, N''ORME_SALES'', N''Örme Satış ve Planlama'', 1, N''Orme''),
    (303, N''ORME_PRODUCTION'', N''Örme Üretim Sorumlusu'', 1, N''Orme''),
    (304, N''ORME_QUALITY'', N''Örme Kalite Kontrol'', 1, N''Orme''),
    (305, N''ORME_QUALITY_LEAD'', N''Örme Kalite Sorumlusu'', 1, N''Orme''),
    (306, N''ORME_WAREHOUSE'', N''Örme Depo ve Sevkiyat'', 1, N''Orme''),
    (307, N''ORME_ACCOUNTING'', N''Örme Muhasebe'', 1, N''Orme'')
    ) v([Id], [Code], [Name], [IsSystem], [ModuleKey])
    WHERE NOT EXISTS (SELECT 1 FROM [Core].[Roles] r WHERE r.[Id] = v.[Id] OR r.[Code] = v.[Code])');
    SET IDENTITY_INSERT [Core].[Roles] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102052313_AddDefaultRoles'
)
BEGIN
    INSERT INTO [Core].[RolePermissions] ([RoleId], [PermissionId])
    SELECT r.[Id], p.[Id] FROM (VALUES
    (N'ORME_MANAGER', N'Orme.View'),
    (N'ORME_MANAGER', N'Orme.Customer.View'),
    (N'ORME_MANAGER', N'Orme.Customer.Manage'),
    (N'ORME_MANAGER', N'Orme.Material.View'),
    (N'ORME_MANAGER', N'Orme.Material.Manage'),
    (N'ORME_MANAGER', N'Orme.Machine.View'),
    (N'ORME_MANAGER', N'Orme.Machine.Manage'),
    (N'ORME_MANAGER', N'Orme.Order.View'),
    (N'ORME_MANAGER', N'Orme.Order.Manage'),
    (N'ORME_MANAGER', N'Orme.Receipt.View'),
    (N'ORME_MANAGER', N'Orme.Receipt.Manage'),
    (N'ORME_MANAGER', N'Orme.Knitting.View'),
    (N'ORME_MANAGER', N'Orme.Knitting.Manage'),
    (N'ORME_MANAGER', N'Orme.Inspection.View'),
    (N'ORME_MANAGER', N'Orme.Inspection.Manage'),
    (N'ORME_MANAGER', N'Orme.Inspection.Override'),
    (N'ORME_MANAGER', N'Orme.Packing.View'),
    (N'ORME_MANAGER', N'Orme.Packing.Manage'),
    (N'ORME_MANAGER', N'Orme.Shipment.View'),
    (N'ORME_MANAGER', N'Orme.Shipment.Manage'),
    (N'ORME_MANAGER', N'Orme.Invoice.View'),
    (N'ORME_MANAGER', N'Orme.Invoice.Manage'),
    (N'ORME_MANAGER', N'Orme.Account.View'),
    (N'ORME_MANAGER', N'Orme.Account.Manage'),
    (N'ORME_MANAGER', N'Orme.Floor.View'),
    (N'ORME_MANAGER', N'Orme.Floor.Manage'),
    (N'ORME_MANAGER', N'Orme.Report.View'),
    (N'ORME_MANAGER', N'Orme.Purchase.View'),
    (N'ORME_MANAGER', N'Orme.Purchase.Manage'),
    (N'ORME_SALES', N'Orme.View'),
    (N'ORME_SALES', N'Orme.Customer.View'),
    (N'ORME_SALES', N'Orme.Customer.Manage'),
    (N'ORME_SALES', N'Orme.Order.View'),
    (N'ORME_SALES', N'Orme.Order.Manage'),
    (N'ORME_SALES', N'Orme.Report.View'),
    (N'ORME_SALES', N'Orme.Material.View'),
    (N'ORME_SALES', N'Orme.Machine.View'),
    (N'ORME_SALES', N'Orme.Knitting.View'),
    (N'ORME_PRODUCTION', N'Orme.View'),
    (N'ORME_PRODUCTION', N'Orme.Knitting.View'),
    (N'ORME_PRODUCTION', N'Orme.Knitting.Manage'),
    (N'ORME_PRODUCTION', N'Orme.Floor.View'),
    (N'ORME_PRODUCTION', N'Orme.Floor.Manage'),
    (N'ORME_PRODUCTION', N'Orme.Report.View'),
    (N'ORME_PRODUCTION', N'Orme.Order.View'),
    (N'ORME_PRODUCTION', N'Orme.Receipt.View'),
    (N'ORME_QUALITY', N'Orme.View'),
    (N'ORME_QUALITY', N'Orme.Inspection.View'),
    (N'ORME_QUALITY', N'Orme.Inspection.Manage'),
    (N'ORME_QUALITY', N'Orme.Report.View'),
    (N'ORME_QUALITY', N'Orme.Knitting.View'),
    (N'ORME_QUALITY_LEAD', N'Orme.View'),
    (N'ORME_QUALITY_LEAD', N'Orme.Inspection.View'),
    (N'ORME_QUALITY_LEAD', N'Orme.Inspection.Manage'),
    (N'ORME_QUALITY_LEAD', N'Orme.Inspection.Override'),
    (N'ORME_QUALITY_LEAD', N'Orme.Report.View'),
    (N'ORME_QUALITY_LEAD', N'Orme.Knitting.View'),
    (N'ORME_WAREHOUSE', N'Orme.View'),
    (N'ORME_WAREHOUSE', N'Orme.Receipt.View'),
    (N'ORME_WAREHOUSE', N'Orme.Receipt.Manage'),
    (N'ORME_WAREHOUSE', N'Orme.Packing.View'),
    (N'ORME_WAREHOUSE', N'Orme.Packing.Manage'),
    (N'ORME_WAREHOUSE', N'Orme.Shipment.View'),
    (N'ORME_WAREHOUSE', N'Orme.Shipment.Manage'),
    (N'ORME_ACCOUNTING', N'Orme.View'),
    (N'ORME_ACCOUNTING', N'Orme.Invoice.View'),
    (N'ORME_ACCOUNTING', N'Orme.Invoice.Manage'),
    (N'ORME_ACCOUNTING', N'Orme.Account.View'),
    (N'ORME_ACCOUNTING', N'Orme.Account.Manage'),
    (N'ORME_ACCOUNTING', N'Orme.Order.View'),
    (N'ORME_ACCOUNTING', N'Orme.Shipment.View')
    ) v([RoleCode], [PermissionCode])
    JOIN [Core].[Roles] r ON r.[Code] = v.[RoleCode]
    JOIN [Core].[Permissions] p ON p.[Code] = v.[PermissionCode]
    WHERE NOT EXISTS (SELECT 1 FROM [Core].[RolePermissions] rp WHERE rp.[RoleId] = r.[Id] AND rp.[PermissionId] = p.[Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102052313_AddDefaultRoles'
)
BEGIN
    INSERT INTO [Orme].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261102052313_AddDefaultRoles', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102090043_AddDocumentsAndCustomerCodes'
)
BEGIN
    ALTER TABLE [Orme].[OrderLines] ADD [CustomerFabricCode] nvarchar(50) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102090043_AddDocumentsAndCustomerCodes'
)
BEGIN
    ALTER TABLE [Orme].[Customers] ADD [DocumentLanguage] nvarchar(30) NOT NULL DEFAULT N'TURKISH';
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102090043_AddDocumentsAndCustomerCodes'
)
BEGIN
    EXEC(N'ALTER TABLE [Orme].[Customers] ADD CONSTRAINT [CK_Customers_DocumentLanguage] CHECK ([DocumentLanguage] IN (''TURKISH'', ''ENGLISH''))');
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102090043_AddDocumentsAndCustomerCodes'
)
BEGIN
    INSERT INTO [Orme].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261102090043_AddDocumentsAndCustomerCodes', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102094801_AddPurchasingAndCapacityPlan'
)
BEGIN
    ALTER TABLE [Orme].[YarnLots] ADD [PurchaseOrderLineId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102094801_AddPurchasingAndCapacityPlan'
)
BEGIN
    ALTER TABLE [Orme].[KnitOrders] ADD [PlanSequence] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102094801_AddPurchasingAndCapacityPlan'
)
BEGIN
    ALTER TABLE [Orme].[KnitOrders] ADD [PlannedMachineId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102094801_AddPurchasingAndCapacityPlan'
)
BEGIN
    CREATE TABLE [Orme].[PurchaseOrders] (
        [Id] int NOT NULL IDENTITY,
        [OrderNo] nvarchar(30) NOT NULL,
        [SupplierId] int NOT NULL,
        [OrderDate] date NOT NULL,
        [Currency] char(3) NOT NULL DEFAULT 'TRY',
        [Notes] nvarchar(1000) NULL,
        [Status] nvarchar(30) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_PurchaseOrders] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_PurchaseOrders_Status] CHECK ([Status] IN ('DRAFT', 'APPROVED', 'PARTIALLY_RECEIVED', 'RECEIVED', 'CLOSED', 'CANCELLED')),
        CONSTRAINT [FK_PurchaseOrders_Suppliers_SupplierId] FOREIGN KEY ([SupplierId]) REFERENCES [Orme].[Suppliers] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102094801_AddPurchasingAndCapacityPlan'
)
BEGIN
    CREATE TABLE [Orme].[PurchaseOrderLines] (
        [Id] int NOT NULL IDENTITY,
        [OrderId] int NOT NULL,
        [YarnId] int NOT NULL,
        [Kilograms] decimal(12,2) NOT NULL,
        [UnitPrice] decimal(18,4) NULL,
        [DueDate] date NULL,
        [Notes] nvarchar(500) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_PurchaseOrderLines] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_PurchaseOrderLines_Kilograms] CHECK ([Kilograms] > 0),
        CONSTRAINT [CK_PurchaseOrderLines_UnitPrice] CHECK ([UnitPrice] IS NULL OR [UnitPrice] >= 0),
        CONSTRAINT [FK_PurchaseOrderLines_PurchaseOrders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orme].[PurchaseOrders] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PurchaseOrderLines_Yarns_YarnId] FOREIGN KEY ([YarnId]) REFERENCES [Orme].[Yarns] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102094801_AddPurchasingAndCapacityPlan'
)
BEGIN
    EXEC(N'CREATE INDEX [IX_YarnLots_PurchaseOrderLineId] ON [Orme].[YarnLots] ([PurchaseOrderLineId]) WHERE [PurchaseOrderLineId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102094801_AddPurchasingAndCapacityPlan'
)
BEGIN
    EXEC(N'CREATE INDEX [IX_KnitOrders_PlannedMachineId_PlanSequence] ON [Orme].[KnitOrders] ([PlannedMachineId], [PlanSequence]) WHERE [PlannedMachineId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102094801_AddPurchasingAndCapacityPlan'
)
BEGIN
    EXEC(N'ALTER TABLE [Orme].[KnitOrders] ADD CONSTRAINT [CK_KnitOrders_PlanSequence] CHECK ([PlanSequence] IS NULL OR [PlanSequence] >= 1)');
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102094801_AddPurchasingAndCapacityPlan'
)
BEGIN
    CREATE INDEX [IX_PurchaseOrderLines_OrderId] ON [Orme].[PurchaseOrderLines] ([OrderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102094801_AddPurchasingAndCapacityPlan'
)
BEGIN
    CREATE INDEX [IX_PurchaseOrderLines_YarnId] ON [Orme].[PurchaseOrderLines] ([YarnId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102094801_AddPurchasingAndCapacityPlan'
)
BEGIN
    CREATE UNIQUE INDEX [IX_PurchaseOrders_OrderNo] ON [Orme].[PurchaseOrders] ([OrderNo]);
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102094801_AddPurchasingAndCapacityPlan'
)
BEGIN
    CREATE INDEX [IX_PurchaseOrders_Status] ON [Orme].[PurchaseOrders] ([Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102094801_AddPurchasingAndCapacityPlan'
)
BEGIN
    CREATE INDEX [IX_PurchaseOrders_SupplierId] ON [Orme].[PurchaseOrders] ([SupplierId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102094801_AddPurchasingAndCapacityPlan'
)
BEGIN
    ALTER TABLE [Orme].[KnitOrders] ADD CONSTRAINT [FK_KnitOrders_Machines_PlannedMachineId] FOREIGN KEY ([PlannedMachineId]) REFERENCES [Orme].[Machines] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102094801_AddPurchasingAndCapacityPlan'
)
BEGIN
    ALTER TABLE [Orme].[YarnLots] ADD CONSTRAINT [FK_YarnLots_PurchaseOrderLines_PurchaseOrderLineId] FOREIGN KEY ([PurchaseOrderLineId]) REFERENCES [Orme].[PurchaseOrderLines] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102094801_AddPurchasingAndCapacityPlan'
)
BEGIN
    UPDATE [Core].[Permissions] SET [Description] = N'Tedarikçileri, iplik satın alma siparişlerini ve iplik ihtiyaç planını görüntüleme' WHERE [Code] = N'Orme.Purchase.View';
    UPDATE [Core].[Permissions] SET [Description] = N'Tedarikçi kartları, iplik satın alma siparişi ve teslim alma' WHERE [Code] = N'Orme.Purchase.Manage';
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102094801_AddPurchasingAndCapacityPlan'
)
BEGIN
    SET IDENTITY_INSERT [Core].[Permissions] ON;
    EXEC(N'INSERT INTO [Core].[Permissions] ([Id], [Code], [Module], [Description])
    SELECT v.[Id], v.[Code], v.[Module], v.[Description] FROM (VALUES
    (3030, N''Orme.Planning.View'', N''Orme'', N''Makine kapasite planını görüntüleme''),
    (3031, N''Orme.Planning.Manage'', N''Orme'', N''Emirleri makine kuyruğuna alma, sırasını ve makinesini değiştirme'')
    ) v([Id], [Code], [Module], [Description])
    WHERE NOT EXISTS (SELECT 1 FROM [Core].[Permissions] p WHERE p.[Id] = v.[Id] OR p.[Code] = v.[Code])');
    SET IDENTITY_INSERT [Core].[Permissions] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102094801_AddPurchasingAndCapacityPlan'
)
BEGIN
    INSERT INTO [Core].[RolePermissions] ([RoleId], [PermissionId])
    SELECT v.[RoleId], v.[PermissionId] FROM (VALUES
    (1, 3030), (1, 3031)
    ) v([RoleId], [PermissionId])
    WHERE EXISTS (SELECT 1 FROM [Core].[Roles] r WHERE r.[Id] = v.[RoleId])
      AND EXISTS (SELECT 1 FROM [Core].[Permissions] p WHERE p.[Id] = v.[PermissionId])
      AND NOT EXISTS (SELECT 1 FROM [Core].[RolePermissions] rp WHERE rp.[RoleId] = v.[RoleId] AND rp.[PermissionId] = v.[PermissionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102094801_AddPurchasingAndCapacityPlan'
)
BEGIN
    INSERT INTO [Orme].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261102094801_AddPurchasingAndCapacityPlan', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102104736_AddQuotations'
)
BEGIN
    CREATE TABLE [Orme].[CostSettings] (
        [Id] int NOT NULL IDENTITY,
        [MachineHourlyCost] decimal(18,4) NOT NULL,
        [LaborHourlyCost] decimal(18,4) NOT NULL,
        [OverheadPercent] decimal(7,2) NOT NULL,
        [DefaultWastePercent] decimal(5,2) NOT NULL,
        [DefaultMarginPercent] decimal(7,2) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_CostSettings] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_CostSettings_HourlyCosts] CHECK ([MachineHourlyCost] >= 0 AND [LaborHourlyCost] >= 0),
        CONSTRAINT [CK_CostSettings_Percents] CHECK ([OverheadPercent] BETWEEN 0 AND 1000 AND [DefaultWastePercent] BETWEEN 0 AND 100 AND [DefaultMarginPercent] BETWEEN 0 AND 1000)
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102104736_AddQuotations'
)
BEGIN
    CREATE TABLE [Orme].[MachineCostRates] (
        [Id] int NOT NULL IDENTITY,
        [MachineId] int NOT NULL,
        [HourlyCost] decimal(18,4) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_MachineCostRates] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_MachineCostRates_HourlyCost] CHECK ([HourlyCost] >= 0),
        CONSTRAINT [FK_MachineCostRates_Machines_MachineId] FOREIGN KEY ([MachineId]) REFERENCES [Orme].[Machines] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102104736_AddQuotations'
)
BEGIN
    CREATE TABLE [Orme].[Quotations] (
        [Id] int NOT NULL IDENTITY,
        [QuotationNo] nvarchar(30) NOT NULL,
        [QuotationDate] date NOT NULL,
        [ValidUntil] date NOT NULL,
        [CustomerId] int NULL,
        [ProspectName] nvarchar(200) NULL,
        [Currency] char(3) NOT NULL DEFAULT 'TRY',
        [DocumentLanguage] nvarchar(30) NULL,
        [Status] nvarchar(30) NOT NULL,
        [Notes] nvarchar(1000) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_Quotations] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Quotations_DocumentLanguage] CHECK ([DocumentLanguage] IN ('TURKISH', 'ENGLISH')),
        CONSTRAINT [CK_Quotations_Party] CHECK ([CustomerId] IS NOT NULL OR [ProspectName] IS NOT NULL),
        CONSTRAINT [CK_Quotations_Status] CHECK ([Status] IN ('DRAFT', 'SENT', 'ACCEPTED', 'REJECTED', 'EXPIRED')),
        CONSTRAINT [CK_Quotations_ValidUntil] CHECK ([ValidUntil] >= [QuotationDate]),
        CONSTRAINT [FK_Quotations_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Orme].[Customers] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102104736_AddQuotations'
)
BEGIN
    CREATE TABLE [Orme].[QuotationLines] (
        [Id] int NOT NULL IDENTITY,
        [QuotationId] int NOT NULL,
        [Sequence] int NOT NULL,
        [FabricId] int NOT NULL,
        [Kilograms] decimal(12,2) NOT NULL,
        [IsCommission] bit NOT NULL,
        [MachineId] int NULL,
        [WastePercent] decimal(5,2) NOT NULL,
        [KilogramsPerHour] decimal(8,2) NULL,
        [MachineHourlyCost] decimal(18,4) NOT NULL,
        [LaborHourlyCost] decimal(18,4) NOT NULL,
        [OverheadPercent] decimal(7,2) NOT NULL,
        [MarginPercent] decimal(9,2) NOT NULL,
        [UnitPrice] decimal(18,4) NOT NULL,
        [CustomerFabricCode] nvarchar(50) NULL,
        [Notes] nvarchar(500) NULL,
        [OrderLineId] int NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_QuotationLines] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_QuotationLines_Costs] CHECK ([MachineHourlyCost] >= 0 AND [LaborHourlyCost] >= 0),
        CONSTRAINT [CK_QuotationLines_Kilograms] CHECK ([Kilograms] > 0),
        CONSTRAINT [CK_QuotationLines_KilogramsPerHour] CHECK ([KilogramsPerHour] IS NULL OR [KilogramsPerHour] > 0),
        CONSTRAINT [CK_QuotationLines_Percents] CHECK ([WastePercent] BETWEEN 0 AND 100 AND [OverheadPercent] BETWEEN 0 AND 1000 AND [MarginPercent] >= -100),
        CONSTRAINT [CK_QuotationLines_UnitPrice] CHECK ([UnitPrice] >= 0),
        CONSTRAINT [FK_QuotationLines_Fabrics_FabricId] FOREIGN KEY ([FabricId]) REFERENCES [Orme].[Fabrics] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_QuotationLines_Machines_MachineId] FOREIGN KEY ([MachineId]) REFERENCES [Orme].[Machines] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_QuotationLines_OrderLines_OrderLineId] FOREIGN KEY ([OrderLineId]) REFERENCES [Orme].[OrderLines] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_QuotationLines_Quotations_QuotationId] FOREIGN KEY ([QuotationId]) REFERENCES [Orme].[Quotations] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102104736_AddQuotations'
)
BEGIN
    CREATE TABLE [Orme].[QuotationLineYarns] (
        [Id] int NOT NULL IDENTITY,
        [LineId] int NOT NULL,
        [YarnId] int NOT NULL,
        [Percent] decimal(5,2) NOT NULL,
        [UnitPrice] decimal(18,4) NULL,
        [PriceSource] nvarchar(30) NOT NULL,
        [PriceReference] nvarchar(100) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_QuotationLineYarns] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_QuotationLineYarns_Percent] CHECK ([Percent] > 0 AND [Percent] <= 100),
        CONSTRAINT [CK_QuotationLineYarns_PriceSource] CHECK ([PriceSource] IN ('NONE', 'PURCHASE_ORDER', 'YARN_LOT', 'MANUAL')),
        CONSTRAINT [CK_QuotationLineYarns_UnitPrice] CHECK ([UnitPrice] IS NULL OR [UnitPrice] >= 0),
        CONSTRAINT [FK_QuotationLineYarns_QuotationLines_LineId] FOREIGN KEY ([LineId]) REFERENCES [Orme].[QuotationLines] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_QuotationLineYarns_Yarns_YarnId] FOREIGN KEY ([YarnId]) REFERENCES [Orme].[Yarns] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102104736_AddQuotations'
)
BEGIN
    CREATE UNIQUE INDEX [IX_MachineCostRates_MachineId] ON [Orme].[MachineCostRates] ([MachineId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102104736_AddQuotations'
)
BEGIN
    CREATE INDEX [IX_QuotationLines_FabricId] ON [Orme].[QuotationLines] ([FabricId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102104736_AddQuotations'
)
BEGIN
    CREATE INDEX [IX_QuotationLines_MachineId] ON [Orme].[QuotationLines] ([MachineId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102104736_AddQuotations'
)
BEGIN
    EXEC(N'CREATE INDEX [IX_QuotationLines_OrderLineId] ON [Orme].[QuotationLines] ([OrderLineId]) WHERE [OrderLineId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102104736_AddQuotations'
)
BEGIN
    CREATE INDEX [IX_QuotationLines_QuotationId_Sequence] ON [Orme].[QuotationLines] ([QuotationId], [Sequence]);
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102104736_AddQuotations'
)
BEGIN
    CREATE INDEX [IX_QuotationLineYarns_LineId] ON [Orme].[QuotationLineYarns] ([LineId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102104736_AddQuotations'
)
BEGIN
    CREATE INDEX [IX_QuotationLineYarns_YarnId] ON [Orme].[QuotationLineYarns] ([YarnId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102104736_AddQuotations'
)
BEGIN
    CREATE INDEX [IX_Quotations_CustomerId] ON [Orme].[Quotations] ([CustomerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102104736_AddQuotations'
)
BEGIN
    CREATE INDEX [IX_Quotations_QuotationDate] ON [Orme].[Quotations] ([QuotationDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102104736_AddQuotations'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Quotations_QuotationNo] ON [Orme].[Quotations] ([QuotationNo]);
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102104736_AddQuotations'
)
BEGIN
    CREATE INDEX [IX_Quotations_Status_ValidUntil] ON [Orme].[Quotations] ([Status], [ValidUntil]);
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102104736_AddQuotations'
)
BEGIN
    SET IDENTITY_INSERT [Core].[Permissions] ON;
    EXEC(N'INSERT INTO [Core].[Permissions] ([Id], [Code], [Module], [Description])
    SELECT v.[Id], v.[Code], v.[Module], v.[Description] FROM (VALUES
    (3032, N''Orme.Quotation.View'', N''Orme'', N''Teklifleri, ön maliyeti ve maliyet parametrelerini görüntüleme''),
    (3033, N''Orme.Quotation.Manage'', N''Orme'', N''Teklif hazırlama, gönderme, kabul / ret ve maliyet parametrelerini düzenleme'')
    ) v([Id], [Code], [Module], [Description])
    WHERE NOT EXISTS (SELECT 1 FROM [Core].[Permissions] p WHERE p.[Id] = v.[Id] OR p.[Code] = v.[Code])');
    SET IDENTITY_INSERT [Core].[Permissions] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102104736_AddQuotations'
)
BEGIN
    INSERT INTO [Core].[RolePermissions] ([RoleId], [PermissionId])
    SELECT v.[RoleId], v.[PermissionId] FROM (VALUES
    (1, 3032), (1, 3033)
    ) v([RoleId], [PermissionId])
    WHERE EXISTS (SELECT 1 FROM [Core].[Roles] r WHERE r.[Id] = v.[RoleId])
      AND EXISTS (SELECT 1 FROM [Core].[Permissions] p WHERE p.[Id] = v.[PermissionId])
      AND NOT EXISTS (SELECT 1 FROM [Core].[RolePermissions] rp WHERE rp.[RoleId] = v.[RoleId] AND rp.[PermissionId] = v.[PermissionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Orme].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102104736_AddQuotations'
)
BEGIN
    INSERT INTO [Orme].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261102104736_AddQuotations', N'10.0.12');
END;

COMMIT;
GO

