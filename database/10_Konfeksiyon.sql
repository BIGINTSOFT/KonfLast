/*
    Tekstil ERP - Konfeksiyon modülü kurulum / güncelleme script'i
    Bu dosya otomatik üretilir (database\generate-script.ps1); elle düzenlemeyin.

    Hedef veritabanına (örn. TekstilERP) bağlanıp script'in tamamını çalıştırın. Script tekrar
    çalıştırılabilir: daha önce uygulanmış adımlar atlanır. Sıra: önce 00_Core.sql, sonra modüller.

    sqlcmd ile:  sqlcmd -S "(localdb)\MSSQLLocalDB" -d TekstilERP -E -b -f 65001 -i 10_Konfeksiyon.sql
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
IF OBJECT_ID(N'[Konfeksiyon].[__EFMigrationsHistory]') IS NULL
BEGIN
    IF SCHEMA_ID(N'Konfeksiyon') IS NULL EXEC(N'CREATE SCHEMA [Konfeksiyon];');
    CREATE TABLE [Konfeksiyon].[__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    IF SCHEMA_ID(N'Konfeksiyon') IS NULL EXEC(N'CREATE SCHEMA [Konfeksiyon];');
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE TABLE [Konfeksiyon].[ApprovalFlows] (
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
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE TABLE [Konfeksiyon].[BreakGroups] (
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
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE TABLE [Konfeksiyon].[Colors] (
        [Id] int NOT NULL IDENTITY,
        [ColorCode] nvarchar(30) NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [HexCode] varchar(7) NULL,
        [IsActive] bit NOT NULL,
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
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE TABLE [Konfeksiyon].[Customers] (
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
        [RequireUniqueCustomerOrderNo] bit NOT NULL,
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
        CONSTRAINT [CK_Customers_PartyType] CHECK ([PartyType] IN ('COMPANY', 'INDIVIDUAL'))
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE TABLE [Konfeksiyon].[DefectTypes] (
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
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE TABLE [Konfeksiyon].[Factories] (
        [Id] int NOT NULL IDENTITY,
        [Code] nvarchar(30) NOT NULL,
        [Name] nvarchar(150) NOT NULL,
        [AddressLine] nvarchar(500) NULL,
        [District] nvarchar(100) NULL,
        [City] nvarchar(100) NULL,
        [PostalCode] nvarchar(10) NULL,
        [CountryCode] char(2) NOT NULL,
        [Phone] nvarchar(30) NULL,
        [Email] nvarchar(256) NULL,
        [ManagerName] nvarchar(100) NULL,
        [SgkWorkplaceNo] nvarchar(40) NULL,
        [IndoorArea] int NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_Factories] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Factories_CountryCode] CHECK (LEN([CountryCode]) = 2),
        CONSTRAINT [CK_Factories_IndoorArea] CHECK ([IndoorArea] IS NULL OR [IndoorArea] > 0)
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE TABLE [Konfeksiyon].[MachineTypes] (
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
        CONSTRAINT [PK_MachineTypes] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE TABLE [Konfeksiyon].[PayrollSettings] (
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
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE TABLE [Konfeksiyon].[ProductGroups] (
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
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE TABLE [Konfeksiyon].[RfidCards] (
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
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE TABLE [Konfeksiyon].[RfidReadLogs] (
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
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE TABLE [Konfeksiyon].[Shifts] (
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
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE TABLE [Konfeksiyon].[Sizes] (
        [Id] int NOT NULL IDENTITY,
        [SizeCode] nvarchar(30) NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [SortOrder] int NOT NULL,
        [IsActive] bit NOT NULL,
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
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE TABLE [Konfeksiyon].[StopReasons] (
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
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE TABLE [Konfeksiyon].[Suppliers] (
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
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE TABLE [Konfeksiyon].[Warehouses] (
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
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE TABLE [Konfeksiyon].[ApprovalFlowSteps] (
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
        CONSTRAINT [FK_ApprovalFlowSteps_ApprovalFlows_ApprovalFlowId] FOREIGN KEY ([ApprovalFlowId]) REFERENCES [Konfeksiyon].[ApprovalFlows] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ApprovalFlowSteps_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [Core].[Roles] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE TABLE [Konfeksiyon].[Operators] (
        [Id] int NOT NULL IDENTITY,
        [OperatorCode] nvarchar(30) NOT NULL,
        [FirstName] nvarchar(100) NOT NULL,
        [LastName] nvarchar(100) NOT NULL,
        [IsActive] bit NOT NULL,
        [BreakGroupId] int NULL,
        [Role] nvarchar(30) NOT NULL,
        [CanPause] bit NOT NULL,
        [CanReportBreakdown] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_Operators] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Operators_Role] CHECK ([Role] IN ('OPERATOR', 'LINE_CHIEF', 'MAINTENANCE')),
        CONSTRAINT [FK_Operators_BreakGroups_BreakGroupId] FOREIGN KEY ([BreakGroupId]) REFERENCES [Konfeksiyon].[BreakGroups] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE TABLE [Konfeksiyon].[CustomerAddresses] (
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
        CONSTRAINT [FK_CustomerAddresses_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Konfeksiyon].[Customers] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE TABLE [Konfeksiyon].[CustomerBuyers] (
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
        CONSTRAINT [FK_CustomerBuyers_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Konfeksiyon].[Customers] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE TABLE [Konfeksiyon].[Models] (
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
        CONSTRAINT [FK_Models_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Konfeksiyon].[Customers] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE TABLE [Konfeksiyon].[PriceLists] (
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
        CONSTRAINT [FK_PriceLists_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Konfeksiyon].[Customers] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE TABLE [Konfeksiyon].[ProductionAreas] (
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
        CONSTRAINT [FK_ProductionAreas_Factories_FactoryId] FOREIGN KEY ([FactoryId]) REFERENCES [Konfeksiyon].[Factories] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE TABLE [Konfeksiyon].[Operations] (
        [Id] int NOT NULL IDENTITY,
        [OperationCode] nvarchar(30) NOT NULL,
        [Name] nvarchar(150) NOT NULL,
        [Description] nvarchar(500) NULL,
        [Sam] decimal(9,4) NOT NULL,
        [PieceRate] decimal(18,4) NULL,
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
        CONSTRAINT [FK_Operations_MachineTypes_MachineTypeId] FOREIGN KEY ([MachineTypeId]) REFERENCES [Konfeksiyon].[MachineTypes] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE TABLE [Konfeksiyon].[PayrollBonusTiers] (
        [Id] int NOT NULL IDENTITY,
        [PayrollSettingId] int NOT NULL,
        [MinEfficiencyPercent] decimal(7,2) NOT NULL,
        [BonusPercent] decimal(7,2) NOT NULL,
        CONSTRAINT [PK_PayrollBonusTiers] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_PayrollBonusTiers_Percents] CHECK ([MinEfficiencyPercent] > 0 AND [BonusPercent] > 0),
        CONSTRAINT [FK_PayrollBonusTiers_PayrollSettings_PayrollSettingId] FOREIGN KEY ([PayrollSettingId]) REFERENCES [Konfeksiyon].[PayrollSettings] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE TABLE [Konfeksiyon].[BreakSchedules] (
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
        CONSTRAINT [FK_BreakSchedules_BreakGroups_BreakGroupId] FOREIGN KEY ([BreakGroupId]) REFERENCES [Konfeksiyon].[BreakGroups] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_BreakSchedules_Shifts_ShiftId] FOREIGN KEY ([ShiftId]) REFERENCES [Konfeksiyon].[Shifts] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE TABLE [Konfeksiyon].[PurchaseOrders] (
        [Id] int NOT NULL IDENTITY,
        [OrderNo] nvarchar(30) NOT NULL,
        [SupplierId] int NOT NULL,
        [OrderDate] date NOT NULL,
        [ExpectedDate] date NULL,
        [Currency] varchar(3) NOT NULL,
        [ExchangeRate] decimal(18,6) NULL,
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
        CONSTRAINT [CK_PurchaseOrders_ExchangeRate] CHECK ([ExchangeRate] IS NULL OR [ExchangeRate] > 0),
        CONSTRAINT [CK_PurchaseOrders_ExpectedDate] CHECK ([ExpectedDate] IS NULL OR [ExpectedDate] >= [OrderDate]),
        CONSTRAINT [CK_PurchaseOrders_Status] CHECK ([Status] IN ('DRAFT', 'APPROVED', 'PARTIALLY_RECEIVED', 'RECEIVED', 'CLOSED', 'CANCELLED')),
        CONSTRAINT [FK_PurchaseOrders_Suppliers_SupplierId] FOREIGN KEY ([SupplierId]) REFERENCES [Konfeksiyon].[Suppliers] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE TABLE [Konfeksiyon].[WarehouseLocations] (
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
        CONSTRAINT [FK_WarehouseLocations_Warehouses_WarehouseId] FOREIGN KEY ([WarehouseId]) REFERENCES [Konfeksiyon].[Warehouses] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE TABLE [Konfeksiyon].[ApprovalFlowStepUsers] (
        [ApprovalFlowStepId] int NOT NULL,
        [UserId] int NOT NULL,
        CONSTRAINT [PK_ApprovalFlowStepUsers] PRIMARY KEY ([ApprovalFlowStepId], [UserId]),
        CONSTRAINT [FK_ApprovalFlowStepUsers_ApprovalFlowSteps_ApprovalFlowStepId] FOREIGN KEY ([ApprovalFlowStepId]) REFERENCES [Konfeksiyon].[ApprovalFlowSteps] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ApprovalFlowStepUsers_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Core].[Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE TABLE [Konfeksiyon].[OperatorCards] (
        [Id] int NOT NULL IDENTITY,
        [OperatorId] int NOT NULL,
        [RfidCardId] int NOT NULL,
        [AssignedAt] datetime2 NOT NULL,
        [RevokedAt] datetime2 NULL,
        CONSTRAINT [PK_OperatorCards] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_OperatorCards_Period] CHECK ([RevokedAt] IS NULL OR [RevokedAt] >= [AssignedAt]),
        CONSTRAINT [FK_OperatorCards_Operators_OperatorId] FOREIGN KEY ([OperatorId]) REFERENCES [Konfeksiyon].[Operators] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_OperatorCards_RfidCards_RfidCardId] FOREIGN KEY ([RfidCardId]) REFERENCES [Konfeksiyon].[RfidCards] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE TABLE [Konfeksiyon].[OperatorPhotos] (
        [OperatorId] int NOT NULL,
        [Content] varbinary(max) NOT NULL,
        [ContentType] varchar(30) NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_OperatorPhotos] PRIMARY KEY ([OperatorId]),
        CONSTRAINT [FK_OperatorPhotos_Operators_OperatorId] FOREIGN KEY ([OperatorId]) REFERENCES [Konfeksiyon].[Operators] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE TABLE [Konfeksiyon].[SalesOrders] (
        [Id] int NOT NULL IDENTITY,
        [OrderNo] nvarchar(30) NOT NULL,
        [CustomerId] int NOT NULL,
        [CustomerBuyerId] int NULL,
        [DeliveryAddressId] int NULL,
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
        [ApprovalRound] int NOT NULL,
        [ClosedAt] datetime2 NULL,
        [ClosedBy] nvarchar(100) NULL,
        [CloseNote] nvarchar(500) NULL,
        [ClosedAutomatically] bit NOT NULL,
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
        CONSTRAINT [FK_SalesOrders_CustomerAddresses_DeliveryAddressId] FOREIGN KEY ([DeliveryAddressId]) REFERENCES [Konfeksiyon].[CustomerAddresses] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_SalesOrders_CustomerBuyers_CustomerBuyerId] FOREIGN KEY ([CustomerBuyerId]) REFERENCES [Konfeksiyon].[CustomerBuyers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_SalesOrders_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Konfeksiyon].[Customers] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE TABLE [Konfeksiyon].[ProductionRoutes] (
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
        CONSTRAINT [FK_ProductionRoutes_Models_ModelId] FOREIGN KEY ([ModelId]) REFERENCES [Konfeksiyon].[Models] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE TABLE [Konfeksiyon].[Products] (
        [Id] int NOT NULL IDENTITY,
        [ProductCode] nvarchar(30) NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [Description] nvarchar(500) NULL,
        [ModelId] int NOT NULL,
        [ProductGroupId] int NULL,
        [TargetGroup] nvarchar(30) NULL,
        [Season] nvarchar(20) NULL,
        [CustomerArticleCode] nvarchar(50) NULL,
        [Composition] nvarchar(200) NULL,
        [FabricWeight] int NULL,
        [CareInstructions] nvarchar(500) NULL,
        [OriginCountryCode] char(2) NOT NULL,
        [GtipCode] varchar(12) NULL,
        [VatRate] decimal(5,2) NOT NULL,
        [SalesUnit] nvarchar(30) NOT NULL,
        [ListPrice] decimal(18,4) NULL,
        [Currency] char(3) NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_Products] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Products_FabricWeight] CHECK ([FabricWeight] IS NULL OR [FabricWeight] > 0),
        CONSTRAINT [CK_Products_ListPrice] CHECK ([ListPrice] IS NULL OR [ListPrice] >= 0),
        CONSTRAINT [CK_Products_SalesUnit] CHECK ([SalesUnit] IN ('PIECE', 'METER', 'KILOGRAM', 'ROLL', 'BOX')),
        CONSTRAINT [CK_Products_TargetGroup] CHECK ([TargetGroup] IN ('UNISEX', 'MEN', 'WOMEN', 'KIDS', 'BABY')),
        CONSTRAINT [CK_Products_VatRate] CHECK ([VatRate] >= 0 AND [VatRate] <= 100),
        CONSTRAINT [FK_Products_Models_ModelId] FOREIGN KEY ([ModelId]) REFERENCES [Konfeksiyon].[Models] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Products_ProductGroups_ProductGroupId] FOREIGN KEY ([ProductGroupId]) REFERENCES [Konfeksiyon].[ProductGroups] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE TABLE [Konfeksiyon].[ProductionLines] (
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
        CONSTRAINT [FK_ProductionLines_ProductionAreas_ProductionAreaId] FOREIGN KEY ([ProductionAreaId]) REFERENCES [Konfeksiyon].[ProductionAreas] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE TABLE [Konfeksiyon].[Machines] (
        [Id] int NOT NULL IDENTITY,
        [MachineCode] nvarchar(30) NOT NULL,
        [MachineName] nvarchar(150) NOT NULL,
        [MachineTypeId] int NOT NULL,
        [OperationId] int NULL,
        [Status] nvarchar(30) NOT NULL,
        [Brand] nvarchar(100) NULL,
        [ModelNo] nvarchar(100) NULL,
        [SerialNo] nvarchar(100) NULL,
        [ManufactureYear] int NULL,
        [AssetNo] nvarchar(50) NULL,
        [PurchaseDate] date NULL,
        [SupplierId] int NULL,
        [WarrantyEndDate] date NULL,
        [MaintenanceIntervalDays] int NULL,
        [LastMaintenanceDate] date NULL,
        [Notes] nvarchar(500) NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_Machines] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Machines_MaintenanceInterval] CHECK ([MaintenanceIntervalDays] IS NULL OR [MaintenanceIntervalDays] > 0),
        CONSTRAINT [CK_Machines_Status] CHECK ([Status] IN ('AVAILABLE', 'RUNNING', 'IDLE', 'BREAKDOWN', 'MAINTENANCE', 'OFFLINE')),
        CONSTRAINT [CK_Machines_Warranty] CHECK ([WarrantyEndDate] IS NULL OR [PurchaseDate] IS NULL OR [WarrantyEndDate] >= [PurchaseDate]),
        CONSTRAINT [FK_Machines_MachineTypes_MachineTypeId] FOREIGN KEY ([MachineTypeId]) REFERENCES [Konfeksiyon].[MachineTypes] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Machines_Operations_OperationId] FOREIGN KEY ([OperationId]) REFERENCES [Konfeksiyon].[Operations] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Machines_Suppliers_SupplierId] FOREIGN KEY ([SupplierId]) REFERENCES [Konfeksiyon].[Suppliers] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE TABLE [Konfeksiyon].[OperatorOperationQualifications] (
        [OperatorId] int NOT NULL,
        [OperationId] int NOT NULL,
        CONSTRAINT [PK_OperatorOperationQualifications] PRIMARY KEY ([OperatorId], [OperationId]),
        CONSTRAINT [FK_OperatorOperationQualifications_Operations_OperationId] FOREIGN KEY ([OperationId]) REFERENCES [Konfeksiyon].[Operations] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_OperatorOperationQualifications_Operators_OperatorId] FOREIGN KEY ([OperatorId]) REFERENCES [Konfeksiyon].[Operators] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE TABLE [Konfeksiyon].[GoodsReceipts] (
        [Id] bigint NOT NULL IDENTITY,
        [RequestId] uniqueidentifier NOT NULL,
        [PurchaseOrderId] int NOT NULL,
        [WarehouseId] int NOT NULL,
        [DocumentNo] nvarchar(50) NULL,
        [Note] nvarchar(300) NULL,
        [ReceivedAt] datetime2 NOT NULL,
        [ReceivedBy] nvarchar(100) NOT NULL,
        CONSTRAINT [PK_GoodsReceipts] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_GoodsReceipts_PurchaseOrders_PurchaseOrderId] FOREIGN KEY ([PurchaseOrderId]) REFERENCES [Konfeksiyon].[PurchaseOrders] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_GoodsReceipts_Warehouses_WarehouseId] FOREIGN KEY ([WarehouseId]) REFERENCES [Konfeksiyon].[Warehouses] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE TABLE [Konfeksiyon].[StockCounts] (
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
        CONSTRAINT [FK_StockCounts_WarehouseLocations_LocationId] FOREIGN KEY ([LocationId]) REFERENCES [Konfeksiyon].[WarehouseLocations] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_StockCounts_Warehouses_WarehouseId] FOREIGN KEY ([WarehouseId]) REFERENCES [Konfeksiyon].[Warehouses] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE TABLE [Konfeksiyon].[SalesOrderApprovalEvents] (
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
        CONSTRAINT [FK_SalesOrderApprovalEvents_SalesOrders_SalesOrderId] FOREIGN KEY ([SalesOrderId]) REFERENCES [Konfeksiyon].[SalesOrders] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE TABLE [Konfeksiyon].[SalesOrderApprovalSteps] (
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
        CONSTRAINT [FK_SalesOrderApprovalSteps_SalesOrders_SalesOrderId] FOREIGN KEY ([SalesOrderId]) REFERENCES [Konfeksiyon].[SalesOrders] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE TABLE [Konfeksiyon].[ProductionRouteOperations] (
        [Id] int NOT NULL IDENTITY,
        [ProductionRouteId] int NOT NULL,
        [SequenceNo] int NOT NULL,
        [OperationId] int NOT NULL,
        [Sam] decimal(9,4) NOT NULL,
        CONSTRAINT [PK_ProductionRouteOperations] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_ProductionRouteOperations_Sam] CHECK ([Sam] >= 0),
        CONSTRAINT [CK_ProductionRouteOperations_SequenceNo] CHECK ([SequenceNo] > 0),
        CONSTRAINT [FK_ProductionRouteOperations_Operations_OperationId] FOREIGN KEY ([OperationId]) REFERENCES [Konfeksiyon].[Operations] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ProductionRouteOperations_ProductionRoutes_ProductionRouteId] FOREIGN KEY ([ProductionRouteId]) REFERENCES [Konfeksiyon].[ProductionRoutes] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE TABLE [Konfeksiyon].[CustomerDiscounts] (
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
        CONSTRAINT [FK_CustomerDiscounts_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Konfeksiyon].[Customers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_CustomerDiscounts_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Konfeksiyon].[Products] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE TABLE [Konfeksiyon].[PriceListItems] (
        [Id] int NOT NULL IDENTITY,
        [PriceListId] int NOT NULL,
        [ProductId] int NOT NULL,
        [ColorId] int NULL,
        [SizeId] int NULL,
        [UnitPrice] decimal(18,4) NOT NULL,
        CONSTRAINT [PK_PriceListItems] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_PriceListItems_UnitPrice] CHECK ([UnitPrice] >= 0),
        CONSTRAINT [FK_PriceListItems_Colors_ColorId] FOREIGN KEY ([ColorId]) REFERENCES [Konfeksiyon].[Colors] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PriceListItems_PriceLists_PriceListId] FOREIGN KEY ([PriceListId]) REFERENCES [Konfeksiyon].[PriceLists] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PriceListItems_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Konfeksiyon].[Products] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PriceListItems_Sizes_SizeId] FOREIGN KEY ([SizeId]) REFERENCES [Konfeksiyon].[Sizes] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE TABLE [Konfeksiyon].[ProductionOrders] (
        [Id] int NOT NULL IDENTITY,
        [OrderNo] nvarchar(30) NOT NULL,
        [CustomerId] int NOT NULL,
        [ProductId] int NOT NULL,
        [MaterialWarehouseId] int NULL,
        [SalesOrderId] int NULL,
        [CustomerOrderNo] nvarchar(50) NULL,
        [OrderDate] date NOT NULL,
        [DueDate] date NOT NULL,
        [Status] nvarchar(30) NOT NULL,
        [Notes] nvarchar(1000) NULL,
        [OperationSequence] nvarchar(30) NOT NULL,
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
        CONSTRAINT [CK_ProductionOrders_OperationSequence] CHECK ([OperationSequence] IN ('SEQUENTIAL', 'FREE', 'ROUTE')),
        CONSTRAINT [CK_ProductionOrders_Status] CHECK ([Status] IN ('DRAFT', 'RELEASED', 'IN_PRODUCTION', 'COMPLETED', 'CANCELLED')),
        CONSTRAINT [FK_ProductionOrders_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Konfeksiyon].[Customers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ProductionOrders_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Konfeksiyon].[Products] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ProductionOrders_SalesOrders_SalesOrderId] FOREIGN KEY ([SalesOrderId]) REFERENCES [Konfeksiyon].[SalesOrders] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ProductionOrders_Warehouses_MaterialWarehouseId] FOREIGN KEY ([MaterialWarehouseId]) REFERENCES [Konfeksiyon].[Warehouses] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE TABLE [Konfeksiyon].[SalesOrderLines] (
        [Id] int NOT NULL IDENTITY,
        [SalesOrderId] int NOT NULL,
        [ProductId] int NOT NULL,
        [ColorId] int NOT NULL,
        [SizeId] int NOT NULL,
        [Quantity] int NOT NULL,
        [UnitPrice] decimal(18,4) NULL,
        [ShippedFromReservation] decimal(18,3) NOT NULL,
        CONSTRAINT [PK_SalesOrderLines] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_SalesOrderLines_Quantity] CHECK ([Quantity] > 0),
        CONSTRAINT [CK_SalesOrderLines_UnitPrice] CHECK ([UnitPrice] IS NULL OR [UnitPrice] >= 0),
        CONSTRAINT [FK_SalesOrderLines_Colors_ColorId] FOREIGN KEY ([ColorId]) REFERENCES [Konfeksiyon].[Colors] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_SalesOrderLines_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Konfeksiyon].[Products] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_SalesOrderLines_SalesOrders_SalesOrderId] FOREIGN KEY ([SalesOrderId]) REFERENCES [Konfeksiyon].[SalesOrders] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_SalesOrderLines_Sizes_SizeId] FOREIGN KEY ([SizeId]) REFERENCES [Konfeksiyon].[Sizes] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE TABLE [Konfeksiyon].[StockItems] (
        [Id] int NOT NULL IDENTITY,
        [Code] nvarchar(100) NOT NULL,
        [Name] nvarchar(300) NOT NULL,
        [ItemType] nvarchar(30) NOT NULL,
        [Unit] nvarchar(30) NOT NULL,
        [ProductId] int NULL,
        [ColorId] int NULL,
        [SizeId] int NULL,
        [UnitCost] decimal(18,4) NULL,
        [IsLotTracked] bit NOT NULL,
        [MinimumStock] decimal(18,3) NULL,
        [ReorderQuantity] decimal(18,3) NULL,
        [SupplierId] int NULL,
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
        CONSTRAINT [CK_StockItems_Reorder] CHECK (([MinimumStock] IS NULL OR [MinimumStock] >= 0) AND ([ReorderQuantity] IS NULL OR [ReorderQuantity] > 0)),
        CONSTRAINT [CK_StockItems_Unit] CHECK ([Unit] IN ('PIECE', 'METER', 'KILOGRAM', 'ROLL', 'BOX')),
        CONSTRAINT [FK_StockItems_Colors_ColorId] FOREIGN KEY ([ColorId]) REFERENCES [Konfeksiyon].[Colors] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_StockItems_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Konfeksiyon].[Products] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_StockItems_Sizes_SizeId] FOREIGN KEY ([SizeId]) REFERENCES [Konfeksiyon].[Sizes] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_StockItems_Suppliers_SupplierId] FOREIGN KEY ([SupplierId]) REFERENCES [Konfeksiyon].[Suppliers] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE TABLE [Konfeksiyon].[LineMachineAssignments] (
        [Id] int NOT NULL IDENTITY,
        [ProductionLineId] int NOT NULL,
        [MachineId] int NOT NULL,
        [Position] int NOT NULL,
        [AssignedAt] datetime2 NOT NULL,
        [UnassignedAt] datetime2 NULL,
        CONSTRAINT [PK_LineMachineAssignments] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_LineMachineAssignments_Period] CHECK ([UnassignedAt] IS NULL OR [UnassignedAt] >= [AssignedAt]),
        CONSTRAINT [CK_LineMachineAssignments_Position] CHECK ([Position] > 0),
        CONSTRAINT [FK_LineMachineAssignments_Machines_MachineId] FOREIGN KEY ([MachineId]) REFERENCES [Konfeksiyon].[Machines] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_LineMachineAssignments_ProductionLines_ProductionLineId] FOREIGN KEY ([ProductionLineId]) REFERENCES [Konfeksiyon].[ProductionLines] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE TABLE [Konfeksiyon].[MachineOperations] (
        [MachineId] int NOT NULL,
        [OperationId] int NOT NULL,
        CONSTRAINT [PK_MachineOperations] PRIMARY KEY ([MachineId], [OperationId]),
        CONSTRAINT [FK_MachineOperations_Machines_MachineId] FOREIGN KEY ([MachineId]) REFERENCES [Konfeksiyon].[Machines] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_MachineOperations_Operations_OperationId] FOREIGN KEY ([OperationId]) REFERENCES [Konfeksiyon].[Operations] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE TABLE [Konfeksiyon].[MachineStatusLogs] (
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
        CONSTRAINT [FK_MachineStatusLogs_Machines_MachineId] FOREIGN KEY ([MachineId]) REFERENCES [Konfeksiyon].[Machines] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE TABLE [Konfeksiyon].[OperatorMachines] (
        [OperatorId] int NOT NULL,
        [MachineId] int NOT NULL,
        CONSTRAINT [PK_OperatorMachines] PRIMARY KEY ([OperatorId], [MachineId]),
        CONSTRAINT [FK_OperatorMachines_Machines_MachineId] FOREIGN KEY ([MachineId]) REFERENCES [Konfeksiyon].[Machines] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_OperatorMachines_Operators_OperatorId] FOREIGN KEY ([OperatorId]) REFERENCES [Konfeksiyon].[Operators] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE TABLE [Konfeksiyon].[Tablets] (
        [Id] int NOT NULL IDENTITY,
        [TabletCode] nvarchar(30) NOT NULL,
        [Name] nvarchar(150) NULL,
        [MachineId] int NULL,
        [IsActive] bit NOT NULL,
        [DeviceIdentifier] nvarchar(200) NULL,
        [DeviceKeyHash] varchar(64) NULL,
        [LastSeenAt] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_Tablets] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Tablets_Machines_MachineId] FOREIGN KEY ([MachineId]) REFERENCES [Konfeksiyon].[Machines] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE TABLE [Konfeksiyon].[OperationDependencies] (
        [RouteOperationId] int NOT NULL,
        [PredecessorId] int NOT NULL,
        CONSTRAINT [PK_OperationDependencies] PRIMARY KEY ([RouteOperationId], [PredecessorId]),
        CONSTRAINT [CK_OperationDependencies_NotSelf] CHECK ([RouteOperationId] <> [PredecessorId]),
        CONSTRAINT [FK_OperationDependencies_ProductionRouteOperations_PredecessorId] FOREIGN KEY ([PredecessorId]) REFERENCES [Konfeksiyon].[ProductionRouteOperations] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_OperationDependencies_ProductionRouteOperations_RouteOperationId] FOREIGN KEY ([RouteOperationId]) REFERENCES [Konfeksiyon].[ProductionRouteOperations] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE TABLE [Konfeksiyon].[Bundles] (
        [Id] int NOT NULL IDENTITY,
        [BundleNo] varchar(20) NOT NULL,
        [ProductionOrderId] int NOT NULL,
        [ColorId] int NOT NULL,
        [SizeId] int NOT NULL,
        [Quantity] int NOT NULL,
        [ProductionRouteId] int NOT NULL,
        [Status] nvarchar(30) NOT NULL,
        [SequenceMode] nvarchar(30) NOT NULL,
        [RowVersion] rowversion NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_Bundles] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Bundles_Quantity] CHECK ([Quantity] > 0),
        CONSTRAINT [CK_Bundles_SequenceMode] CHECK ([SequenceMode] IN ('SEQUENTIAL', 'FREE', 'ROUTE')),
        CONSTRAINT [CK_Bundles_Status] CHECK ([Status] IN ('CREATED', 'WAITING', 'IN_PRODUCTION', 'PARTIALLY_COMPLETED', 'COMPLETED', 'QUALITY_HOLD', 'REWORK', 'CANCELLED', 'CLOSED')),
        CONSTRAINT [FK_Bundles_Colors_ColorId] FOREIGN KEY ([ColorId]) REFERENCES [Konfeksiyon].[Colors] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Bundles_ProductionOrders_ProductionOrderId] FOREIGN KEY ([ProductionOrderId]) REFERENCES [Konfeksiyon].[ProductionOrders] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Bundles_ProductionRoutes_ProductionRouteId] FOREIGN KEY ([ProductionRouteId]) REFERENCES [Konfeksiyon].[ProductionRoutes] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Bundles_Sizes_SizeId] FOREIGN KEY ([SizeId]) REFERENCES [Konfeksiyon].[Sizes] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE TABLE [Konfeksiyon].[LineProductionPlans] (
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
        CONSTRAINT [FK_LineProductionPlans_ProductionLines_ProductionLineId] FOREIGN KEY ([ProductionLineId]) REFERENCES [Konfeksiyon].[ProductionLines] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_LineProductionPlans_ProductionOrders_ProductionOrderId] FOREIGN KEY ([ProductionOrderId]) REFERENCES [Konfeksiyon].[ProductionOrders] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE TABLE [Konfeksiyon].[ProductionOrderItems] (
        [Id] int NOT NULL IDENTITY,
        [ProductionOrderId] int NOT NULL,
        [ColorId] int NOT NULL,
        [SizeId] int NOT NULL,
        [Quantity] int NOT NULL,
        CONSTRAINT [PK_ProductionOrderItems] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_ProductionOrderItems_Quantity] CHECK ([Quantity] > 0),
        CONSTRAINT [FK_ProductionOrderItems_Colors_ColorId] FOREIGN KEY ([ColorId]) REFERENCES [Konfeksiyon].[Colors] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ProductionOrderItems_ProductionOrders_ProductionOrderId] FOREIGN KEY ([ProductionOrderId]) REFERENCES [Konfeksiyon].[ProductionOrders] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ProductionOrderItems_Sizes_SizeId] FOREIGN KEY ([SizeId]) REFERENCES [Konfeksiyon].[Sizes] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE TABLE [Konfeksiyon].[Shipments] (
        [Id] int NOT NULL IDENTITY,
        [ShipmentNo] nvarchar(30) NOT NULL,
        [CustomerId] int NOT NULL,
        [WarehouseId] int NOT NULL,
        [ProductionOrderId] int NULL,
        [SalesOrderId] int NULL,
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
        CONSTRAINT [FK_Shipments_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Konfeksiyon].[Customers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Shipments_ProductionOrders_ProductionOrderId] FOREIGN KEY ([ProductionOrderId]) REFERENCES [Konfeksiyon].[ProductionOrders] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Shipments_SalesOrders_SalesOrderId] FOREIGN KEY ([SalesOrderId]) REFERENCES [Konfeksiyon].[SalesOrders] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Shipments_Warehouses_WarehouseId] FOREIGN KEY ([WarehouseId]) REFERENCES [Konfeksiyon].[Warehouses] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE TABLE [Konfeksiyon].[ProductMaterials] (
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
        CONSTRAINT [FK_ProductMaterials_Colors_ColorId] FOREIGN KEY ([ColorId]) REFERENCES [Konfeksiyon].[Colors] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ProductMaterials_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Konfeksiyon].[Products] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ProductMaterials_Sizes_SizeId] FOREIGN KEY ([SizeId]) REFERENCES [Konfeksiyon].[Sizes] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ProductMaterials_StockItems_StockItemId] FOREIGN KEY ([StockItemId]) REFERENCES [Konfeksiyon].[StockItems] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE TABLE [Konfeksiyon].[PurchaseOrderItems] (
        [Id] int NOT NULL IDENTITY,
        [PurchaseOrderId] int NOT NULL,
        [StockItemId] int NOT NULL,
        [Quantity] decimal(18,3) NOT NULL,
        [UnitPrice] decimal(18,4) NULL,
        [ProductionOrderId] int NULL,
        [ReceivedQuantity] decimal(18,3) NOT NULL,
        CONSTRAINT [PK_PurchaseOrderItems] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_PurchaseOrderItems_Quantities] CHECK ([Quantity] > 0 AND [ReceivedQuantity] >= 0 AND [ReceivedQuantity] <= [Quantity]),
        CONSTRAINT [CK_PurchaseOrderItems_UnitPrice] CHECK ([UnitPrice] IS NULL OR [UnitPrice] >= 0),
        CONSTRAINT [FK_PurchaseOrderItems_ProductionOrders_ProductionOrderId] FOREIGN KEY ([ProductionOrderId]) REFERENCES [Konfeksiyon].[ProductionOrders] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PurchaseOrderItems_PurchaseOrders_PurchaseOrderId] FOREIGN KEY ([PurchaseOrderId]) REFERENCES [Konfeksiyon].[PurchaseOrders] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PurchaseOrderItems_StockItems_StockItemId] FOREIGN KEY ([StockItemId]) REFERENCES [Konfeksiyon].[StockItems] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE TABLE [Konfeksiyon].[SalesOrderReservations] (
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
        CONSTRAINT [FK_SalesOrderReservations_SalesOrderLines_SalesOrderLineId] FOREIGN KEY ([SalesOrderLineId]) REFERENCES [Konfeksiyon].[SalesOrderLines] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_SalesOrderReservations_SalesOrders_SalesOrderId] FOREIGN KEY ([SalesOrderId]) REFERENCES [Konfeksiyon].[SalesOrders] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_SalesOrderReservations_StockItems_StockItemId] FOREIGN KEY ([StockItemId]) REFERENCES [Konfeksiyon].[StockItems] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_SalesOrderReservations_Warehouses_WarehouseId] FOREIGN KEY ([WarehouseId]) REFERENCES [Konfeksiyon].[Warehouses] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE TABLE [Konfeksiyon].[StockLots] (
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
        CONSTRAINT [FK_StockLots_PurchaseOrders_PurchaseOrderId] FOREIGN KEY ([PurchaseOrderId]) REFERENCES [Konfeksiyon].[PurchaseOrders] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_StockLots_StockItems_StockItemId] FOREIGN KEY ([StockItemId]) REFERENCES [Konfeksiyon].[StockItems] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE TABLE [Konfeksiyon].[StockReservations] (
        [Id] int NOT NULL IDENTITY,
        [ProductionOrderId] int NOT NULL,
        [WarehouseId] int NOT NULL,
        [StockItemId] int NOT NULL,
        [Quantity] decimal(18,3) NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        [UpdatedBy] nvarchar(100) NOT NULL,
        CONSTRAINT [PK_StockReservations] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_StockReservations_Quantity] CHECK ([Quantity] > 0),
        CONSTRAINT [FK_StockReservations_ProductionOrders_ProductionOrderId] FOREIGN KEY ([ProductionOrderId]) REFERENCES [Konfeksiyon].[ProductionOrders] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_StockReservations_StockItems_StockItemId] FOREIGN KEY ([StockItemId]) REFERENCES [Konfeksiyon].[StockItems] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_StockReservations_Warehouses_WarehouseId] FOREIGN KEY ([WarehouseId]) REFERENCES [Konfeksiyon].[Warehouses] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE TABLE [Konfeksiyon].[WorkSessions] (
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
        CONSTRAINT [CK_WorkSessions_EndReason] CHECK ([EndReason] IS NULL OR [EndReason] IN ('LOGOUT', 'TAKEN_OVER', 'MOVED_TO_ANOTHER_MACHINE', 'CLOSED_BY_SUPERVISOR', 'SHIFT_ENDED')),
        CONSTRAINT [CK_WorkSessions_Period] CHECK ([LogoutAt] IS NULL OR [LogoutAt] >= [LoginAt]),
        CONSTRAINT [CK_WorkSessions_Status] CHECK ([Status] IN ('ACTIVE', 'LOGGED_OUT', 'BREAK', 'PAUSED', 'MACHINE_BREAKDOWN', 'WAITING_FOR_BUNDLE')),
        CONSTRAINT [FK_WorkSessions_Machines_MachineId] FOREIGN KEY ([MachineId]) REFERENCES [Konfeksiyon].[Machines] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_WorkSessions_Operators_OperatorId] FOREIGN KEY ([OperatorId]) REFERENCES [Konfeksiyon].[Operators] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_WorkSessions_Tablets_TabletId] FOREIGN KEY ([TabletId]) REFERENCES [Konfeksiyon].[Tablets] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE TABLE [Konfeksiyon].[BundleCardAssignments] (
        [Id] int NOT NULL IDENTITY,
        [RfidCardId] int NOT NULL,
        [BundleId] int NOT NULL,
        [AssignedAt] datetime2 NOT NULL,
        [ReleasedAt] datetime2 NULL,
        CONSTRAINT [PK_BundleCardAssignments] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_BundleCardAssignments_Period] CHECK ([ReleasedAt] IS NULL OR [ReleasedAt] >= [AssignedAt]),
        CONSTRAINT [FK_BundleCardAssignments_Bundles_BundleId] FOREIGN KEY ([BundleId]) REFERENCES [Konfeksiyon].[Bundles] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_BundleCardAssignments_RfidCards_RfidCardId] FOREIGN KEY ([RfidCardId]) REFERENCES [Konfeksiyon].[RfidCards] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE TABLE [Konfeksiyon].[BundleOperations] (
        [Id] bigint NOT NULL IDENTITY,
        [BundleId] int NOT NULL,
        [RouteOperationId] int NOT NULL,
        [OperationId] int NOT NULL,
        [SequenceNo] int NOT NULL,
        [Sam] decimal(9,4) NOT NULL,
        [UnitRate] decimal(18,4) NULL,
        [Status] nvarchar(30) NOT NULL,
        [PlannedQuantity] int NOT NULL,
        [ProducedQuantity] int NOT NULL,
        [RejectedQuantity] int NOT NULL,
        [ReworkQuantity] int NOT NULL,
        [MachineId] int NULL,
        [OperatorId] int NULL,
        [StartedAt] datetime2 NULL,
        [CompletedAt] datetime2 NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_BundleOperations] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_BundleOperations_Quantities] CHECK ([PlannedQuantity] > 0 AND [ProducedQuantity] >= 0 AND [RejectedQuantity] >= 0 AND [ProducedQuantity] + [RejectedQuantity] <= [PlannedQuantity]),
        CONSTRAINT [CK_BundleOperations_ReworkQuantity] CHECK ([ReworkQuantity] >= 0),
        CONSTRAINT [CK_BundleOperations_Status] CHECK ([Status] IN ('WAITING', 'READY', 'IN_PROGRESS', 'PAUSED', 'COMPLETED', 'QUALITY_HOLD', 'CANCELLED')),
        CONSTRAINT [FK_BundleOperations_Bundles_BundleId] FOREIGN KEY ([BundleId]) REFERENCES [Konfeksiyon].[Bundles] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_BundleOperations_Machines_MachineId] FOREIGN KEY ([MachineId]) REFERENCES [Konfeksiyon].[Machines] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_BundleOperations_Operations_OperationId] FOREIGN KEY ([OperationId]) REFERENCES [Konfeksiyon].[Operations] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_BundleOperations_Operators_OperatorId] FOREIGN KEY ([OperatorId]) REFERENCES [Konfeksiyon].[Operators] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_BundleOperations_ProductionRouteOperations_RouteOperationId] FOREIGN KEY ([RouteOperationId]) REFERENCES [Konfeksiyon].[ProductionRouteOperations] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE TABLE [Konfeksiyon].[ShipmentItems] (
        [Id] int NOT NULL IDENTITY,
        [ShipmentId] int NOT NULL,
        [StockItemId] int NOT NULL,
        [Quantity] decimal(18,3) NOT NULL,
        CONSTRAINT [PK_ShipmentItems] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_ShipmentItems_Quantity] CHECK ([Quantity] > 0),
        CONSTRAINT [FK_ShipmentItems_Shipments_ShipmentId] FOREIGN KEY ([ShipmentId]) REFERENCES [Konfeksiyon].[Shipments] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ShipmentItems_StockItems_StockItemId] FOREIGN KEY ([StockItemId]) REFERENCES [Konfeksiyon].[StockItems] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE TABLE [Konfeksiyon].[GoodsReceiptLines] (
        [Id] bigint NOT NULL IDENTITY,
        [GoodsReceiptId] bigint NOT NULL,
        [PurchaseOrderItemId] int NOT NULL,
        [Quantity] decimal(18,3) NOT NULL,
        CONSTRAINT [PK_GoodsReceiptLines] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_GoodsReceiptLines_Quantity] CHECK ([Quantity] > 0),
        CONSTRAINT [FK_GoodsReceiptLines_GoodsReceipts_GoodsReceiptId] FOREIGN KEY ([GoodsReceiptId]) REFERENCES [Konfeksiyon].[GoodsReceipts] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_GoodsReceiptLines_PurchaseOrderItems_PurchaseOrderItemId] FOREIGN KEY ([PurchaseOrderItemId]) REFERENCES [Konfeksiyon].[PurchaseOrderItems] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE TABLE [Konfeksiyon].[StockCountLines] (
        [Id] int NOT NULL IDENTITY,
        [StockCountId] int NOT NULL,
        [LocationId] int NOT NULL,
        [StockItemId] int NOT NULL,
        [LotId] int NULL,
        [ExpectedQuantity] decimal(18,3) NOT NULL,
        [CountedQuantity] decimal(18,3) NULL,
        CONSTRAINT [PK_StockCountLines] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_StockCountLines_Quantities] CHECK ([ExpectedQuantity] >= 0 AND ([CountedQuantity] IS NULL OR [CountedQuantity] >= 0)),
        CONSTRAINT [FK_StockCountLines_StockCounts_StockCountId] FOREIGN KEY ([StockCountId]) REFERENCES [Konfeksiyon].[StockCounts] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_StockCountLines_StockItems_StockItemId] FOREIGN KEY ([StockItemId]) REFERENCES [Konfeksiyon].[StockItems] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_StockCountLines_StockLots_LotId] FOREIGN KEY ([LotId]) REFERENCES [Konfeksiyon].[StockLots] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_StockCountLines_WarehouseLocations_LocationId] FOREIGN KEY ([LocationId]) REFERENCES [Konfeksiyon].[WarehouseLocations] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE TABLE [Konfeksiyon].[Stocks] (
        [Id] int NOT NULL IDENTITY,
        [WarehouseId] int NOT NULL,
        [LocationId] int NOT NULL,
        [StockItemId] int NOT NULL,
        [LotId] int NULL,
        [Quantity] decimal(18,3) NOT NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_Stocks] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Stocks_Quantity] CHECK ([Quantity] >= 0),
        CONSTRAINT [FK_Stocks_StockItems_StockItemId] FOREIGN KEY ([StockItemId]) REFERENCES [Konfeksiyon].[StockItems] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Stocks_StockLots_LotId] FOREIGN KEY ([LotId]) REFERENCES [Konfeksiyon].[StockLots] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Stocks_WarehouseLocations_LocationId] FOREIGN KEY ([LocationId]) REFERENCES [Konfeksiyon].[WarehouseLocations] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Stocks_Warehouses_WarehouseId] FOREIGN KEY ([WarehouseId]) REFERENCES [Konfeksiyon].[Warehouses] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE TABLE [Konfeksiyon].[StockTransactions] (
        [Id] bigint NOT NULL IDENTITY,
        [RequestId] uniqueidentifier NOT NULL,
        [Type] nvarchar(30) NOT NULL,
        [WarehouseId] int NOT NULL,
        [LocationId] int NOT NULL,
        [StockItemId] int NOT NULL,
        [Quantity] decimal(18,3) NOT NULL,
        [BalanceAfter] decimal(18,3) NOT NULL,
        [LocationBalanceAfter] decimal(18,3) NOT NULL,
        [LotId] int NULL,
        [LotBalanceAfter] decimal(18,3) NULL,
        [UnitCost] decimal(18,4) NULL,
        [CounterWarehouseId] int NULL,
        [CounterLocationId] int NULL,
        [BundleId] int NULL,
        [ProductionOrderId] int NULL,
        [PurchaseOrderId] int NULL,
        [ShipmentId] int NULL,
        [DocumentNo] nvarchar(50) NULL,
        [Note] nvarchar(300) NULL,
        [OccurredAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NOT NULL,
        CONSTRAINT [PK_StockTransactions] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_StockTransactions_Quantity] CHECK ([Quantity] <> 0 AND [BalanceAfter] >= 0 AND [LocationBalanceAfter] >= 0),
        CONSTRAINT [CK_StockTransactions_Type] CHECK ([Type] IN ('RECEIPT', 'ISSUE', 'TRANSFER_OUT', 'TRANSFER_IN', 'ADJUSTMENT', 'PURCHASE_RECEIPT', 'SHIPMENT', 'PRODUCTION_RECEIPT', 'PRODUCTION_ISSUE', 'PRODUCTION_RETURN', 'RELOCATION_OUT', 'RELOCATION_IN')),
        CONSTRAINT [FK_StockTransactions_Bundles_BundleId] FOREIGN KEY ([BundleId]) REFERENCES [Konfeksiyon].[Bundles] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_StockTransactions_ProductionOrders_ProductionOrderId] FOREIGN KEY ([ProductionOrderId]) REFERENCES [Konfeksiyon].[ProductionOrders] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_StockTransactions_PurchaseOrders_PurchaseOrderId] FOREIGN KEY ([PurchaseOrderId]) REFERENCES [Konfeksiyon].[PurchaseOrders] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_StockTransactions_Shipments_ShipmentId] FOREIGN KEY ([ShipmentId]) REFERENCES [Konfeksiyon].[Shipments] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_StockTransactions_StockItems_StockItemId] FOREIGN KEY ([StockItemId]) REFERENCES [Konfeksiyon].[StockItems] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_StockTransactions_StockLots_LotId] FOREIGN KEY ([LotId]) REFERENCES [Konfeksiyon].[StockLots] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_StockTransactions_WarehouseLocations_CounterLocationId] FOREIGN KEY ([CounterLocationId]) REFERENCES [Konfeksiyon].[WarehouseLocations] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_StockTransactions_WarehouseLocations_LocationId] FOREIGN KEY ([LocationId]) REFERENCES [Konfeksiyon].[WarehouseLocations] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_StockTransactions_Warehouses_CounterWarehouseId] FOREIGN KEY ([CounterWarehouseId]) REFERENCES [Konfeksiyon].[Warehouses] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_StockTransactions_Warehouses_WarehouseId] FOREIGN KEY ([WarehouseId]) REFERENCES [Konfeksiyon].[Warehouses] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE TABLE [Konfeksiyon].[WorkSessionPauses] (
        [Id] bigint NOT NULL IDENTITY,
        [WorkSessionId] bigint NOT NULL,
        [Type] nvarchar(30) NOT NULL,
        [StopReasonId] int NULL,
        [BreakScheduleId] int NULL,
        [Note] nvarchar(200) NULL,
        [StartedAt] datetime2 NOT NULL,
        [EndedAt] datetime2 NULL,
        CONSTRAINT [PK_WorkSessionPauses] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_WorkSessionPauses_Period] CHECK ([EndedAt] IS NULL OR [EndedAt] >= [StartedAt]),
        CONSTRAINT [CK_WorkSessionPauses_Type] CHECK ([Type] IN ('BREAK', 'PAUSE', 'MACHINE_BREAKDOWN', 'WAITING_FOR_BUNDLE')),
        CONSTRAINT [FK_WorkSessionPauses_BreakSchedules_BreakScheduleId] FOREIGN KEY ([BreakScheduleId]) REFERENCES [Konfeksiyon].[BreakSchedules] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_WorkSessionPauses_StopReasons_StopReasonId] FOREIGN KEY ([StopReasonId]) REFERENCES [Konfeksiyon].[StopReasons] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_WorkSessionPauses_WorkSessions_WorkSessionId] FOREIGN KEY ([WorkSessionId]) REFERENCES [Konfeksiyon].[WorkSessions] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE TABLE [Konfeksiyon].[BundleOperationEvents] (
        [Id] bigint NOT NULL IDENTITY,
        [RequestId] uniqueidentifier NOT NULL,
        [EventType] nvarchar(30) NOT NULL,
        [BundleId] int NOT NULL,
        [BundleOperationId] bigint NOT NULL,
        [MachineId] int NOT NULL,
        [OperatorId] int NOT NULL,
        [WorkSessionId] bigint NULL,
        [TabletId] int NULL,
        [EnteredBy] nvarchar(100) NULL,
        [ProducedQuantity] int NOT NULL,
        [RejectedQuantity] int NOT NULL,
        [RunSeconds] int NULL,
        [EfficiencyPercent] int NULL,
        [OccurredAt] datetime2 NOT NULL,
        CONSTRAINT [PK_BundleOperationEvents] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_BundleOperationEvents_EventType] CHECK ([EventType] IN ('OPERATION_STARTED', 'OPERATION_RESUMED', 'OPERATION_PAUSED', 'OPERATION_COMPLETED', 'QUANTITY_REPORTED', 'REJECTION_REPORTED')),
        CONSTRAINT [CK_BundleOperationEvents_Quantities] CHECK ([ProducedQuantity] >= 0 AND [RejectedQuantity] >= 0),
        CONSTRAINT [FK_BundleOperationEvents_BundleOperations_BundleOperationId] FOREIGN KEY ([BundleOperationId]) REFERENCES [Konfeksiyon].[BundleOperations] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_BundleOperationEvents_Bundles_BundleId] FOREIGN KEY ([BundleId]) REFERENCES [Konfeksiyon].[Bundles] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_BundleOperationEvents_Machines_MachineId] FOREIGN KEY ([MachineId]) REFERENCES [Konfeksiyon].[Machines] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_BundleOperationEvents_Operators_OperatorId] FOREIGN KEY ([OperatorId]) REFERENCES [Konfeksiyon].[Operators] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_BundleOperationEvents_WorkSessions_WorkSessionId] FOREIGN KEY ([WorkSessionId]) REFERENCES [Konfeksiyon].[WorkSessions] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE TABLE [Konfeksiyon].[QualityInspections] (
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
        CONSTRAINT [FK_QualityInspections_BundleOperations_BundleOperationId] FOREIGN KEY ([BundleOperationId]) REFERENCES [Konfeksiyon].[BundleOperations] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_QualityInspections_Bundles_BundleId] FOREIGN KEY ([BundleId]) REFERENCES [Konfeksiyon].[Bundles] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE TABLE [Konfeksiyon].[QualityRecords] (
        [Id] bigint NOT NULL IDENTITY,
        [QualityInspectionId] bigint NOT NULL,
        [DefectTypeId] int NOT NULL,
        [Quantity] int NOT NULL,
        CONSTRAINT [PK_QualityRecords] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_QualityRecords_Quantity] CHECK ([Quantity] > 0),
        CONSTRAINT [FK_QualityRecords_DefectTypes_DefectTypeId] FOREIGN KEY ([DefectTypeId]) REFERENCES [Konfeksiyon].[DefectTypes] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_QualityRecords_QualityInspections_QualityInspectionId] FOREIGN KEY ([QualityInspectionId]) REFERENCES [Konfeksiyon].[QualityInspections] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE TABLE [Konfeksiyon].[ReworkTasks] (
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
        CONSTRAINT [FK_ReworkTasks_BundleOperations_BundleOperationId] FOREIGN KEY ([BundleOperationId]) REFERENCES [Konfeksiyon].[BundleOperations] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ReworkTasks_Bundles_BundleId] FOREIGN KEY ([BundleId]) REFERENCES [Konfeksiyon].[Bundles] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ReworkTasks_QualityInspections_QualityInspectionId] FOREIGN KEY ([QualityInspectionId]) REFERENCES [Konfeksiyon].[QualityInspections] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'CreatedAt', N'CreatedBy', N'DeletedAt', N'DeletedBy', N'IsActive', N'IsDeleted', N'Name', N'UpdatedAt', N'UpdatedBy') AND [object_id] = OBJECT_ID(N'[Konfeksiyon].[DefectTypes]'))
        SET IDENTITY_INSERT [Konfeksiyon].[DefectTypes] ON;
    EXEC(N'INSERT INTO [Konfeksiyon].[DefectTypes] ([Id], [Code], [CreatedAt], [CreatedBy], [DeletedAt], [DeletedBy], [IsActive], [IsDeleted], [Name], [UpdatedAt], [UpdatedBy])
    VALUES (1, N''DIKIS'', ''2026-01-01T00:00:00.0000000Z'', N''system'', NULL, NULL, CAST(1 AS bit), CAST(0 AS bit), N''Dikiş hatası'', NULL, NULL),
    (2, N''OLCU'', ''2026-01-01T00:00:00.0000000Z'', N''system'', NULL, NULL, CAST(1 AS bit), CAST(0 AS bit), N''Ölçü hatası'', NULL, NULL),
    (3, N''LEKE'', ''2026-01-01T00:00:00.0000000Z'', N''system'', NULL, NULL, CAST(1 AS bit), CAST(0 AS bit), N''Leke'', NULL, NULL),
    (4, N''SOKUK'', ''2026-01-01T00:00:00.0000000Z'', N''system'', NULL, NULL, CAST(1 AS bit), CAST(0 AS bit), N''Sökük'', NULL, NULL),
    (5, N''KUMAS'', ''2026-01-01T00:00:00.0000000Z'', N''system'', NULL, NULL, CAST(1 AS bit), CAST(0 AS bit), N''Kumaş hatası'', NULL, NULL),
    (6, N''YANLIS-RENK'', ''2026-01-01T00:00:00.0000000Z'', N''system'', NULL, NULL, CAST(1 AS bit), CAST(0 AS bit), N''Yanlış renk'', NULL, NULL),
    (7, N''YANLIS-BEDEN'', ''2026-01-01T00:00:00.0000000Z'', N''system'', NULL, NULL, CAST(1 AS bit), CAST(0 AS bit), N''Yanlış beden'', NULL, NULL),
    (8, N''DIGER'', ''2026-01-01T00:00:00.0000000Z'', N''system'', NULL, NULL, CAST(1 AS bit), CAST(0 AS bit), N''Diğer'', NULL, NULL)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'CreatedAt', N'CreatedBy', N'DeletedAt', N'DeletedBy', N'IsActive', N'IsDeleted', N'Name', N'UpdatedAt', N'UpdatedBy') AND [object_id] = OBJECT_ID(N'[Konfeksiyon].[DefectTypes]'))
        SET IDENTITY_INSERT [Konfeksiyon].[DefectTypes] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CreatedAt', N'CreatedBy', N'Currency', N'MinuteRate', N'UpdatedAt', N'UpdatedBy') AND [object_id] = OBJECT_ID(N'[Konfeksiyon].[PayrollSettings]'))
        SET IDENTITY_INSERT [Konfeksiyon].[PayrollSettings] ON;
    EXEC(N'INSERT INTO [Konfeksiyon].[PayrollSettings] ([Id], [CreatedAt], [CreatedBy], [Currency], [MinuteRate], [UpdatedAt], [UpdatedBy])
    VALUES (1, ''2026-01-01T00:00:00.0000000Z'', N''system'', ''TRY'', 0.0, NULL, NULL)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CreatedAt', N'CreatedBy', N'Currency', N'MinuteRate', N'UpdatedAt', N'UpdatedBy') AND [object_id] = OBJECT_ID(N'[Konfeksiyon].[PayrollSettings]'))
        SET IDENTITY_INSERT [Konfeksiyon].[PayrollSettings] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Category', N'Code', N'CreatedAt', N'CreatedBy', N'DeletedAt', N'DeletedBy', N'IsActive', N'IsDeleted', N'Name', N'SortOrder', N'UpdatedAt', N'UpdatedBy') AND [object_id] = OBJECT_ID(N'[Konfeksiyon].[StopReasons]'))
        SET IDENTITY_INSERT [Konfeksiyon].[StopReasons] ON;
    EXEC(N'INSERT INTO [Konfeksiyon].[StopReasons] ([Id], [Category], [Code], [CreatedAt], [CreatedBy], [DeletedAt], [DeletedBy], [IsActive], [IsDeleted], [Name], [SortOrder], [UpdatedAt], [UpdatedBy])
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
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Category', N'Code', N'CreatedAt', N'CreatedBy', N'DeletedAt', N'DeletedBy', N'IsActive', N'IsDeleted', N'Name', N'SortOrder', N'UpdatedAt', N'UpdatedBy') AND [object_id] = OBJECT_ID(N'[Konfeksiyon].[StopReasons]'))
        SET IDENTITY_INSERT [Konfeksiyon].[StopReasons] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'CreatedAt', N'CreatedBy', N'DeletedAt', N'DeletedBy', N'IsActive', N'IsDeleted', N'Name', N'Type', N'UpdatedAt', N'UpdatedBy') AND [object_id] = OBJECT_ID(N'[Konfeksiyon].[Warehouses]'))
        SET IDENTITY_INSERT [Konfeksiyon].[Warehouses] ON;
    EXEC(N'INSERT INTO [Konfeksiyon].[Warehouses] ([Id], [Code], [CreatedAt], [CreatedBy], [DeletedAt], [DeletedBy], [IsActive], [IsDeleted], [Name], [Type], [UpdatedAt], [UpdatedBy])
    VALUES (1, N''HAMMADDE'', ''2026-01-01T00:00:00.0000000Z'', N''system'', NULL, NULL, CAST(1 AS bit), CAST(0 AS bit), N''Hammadde Deposu'', N''RAW_MATERIAL'', NULL, NULL),
    (2, N''MAMUL'', ''2026-01-01T00:00:00.0000000Z'', N''system'', NULL, NULL, CAST(1 AS bit), CAST(0 AS bit), N''Mamul Deposu'', N''FINISHED_GOODS'', NULL, NULL)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'CreatedAt', N'CreatedBy', N'DeletedAt', N'DeletedBy', N'IsActive', N'IsDeleted', N'Name', N'Type', N'UpdatedAt', N'UpdatedBy') AND [object_id] = OBJECT_ID(N'[Konfeksiyon].[Warehouses]'))
        SET IDENTITY_INSERT [Konfeksiyon].[Warehouses] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ApprovalFlows_DocumentType] ON [Konfeksiyon].[ApprovalFlows] ([DocumentType]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ApprovalFlowSteps_ApprovalFlowId_Sequence] ON [Konfeksiyon].[ApprovalFlowSteps] ([ApprovalFlowId], [Sequence]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_ApprovalFlowSteps_RoleId] ON [Konfeksiyon].[ApprovalFlowSteps] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_ApprovalFlowStepUsers_UserId] ON [Konfeksiyon].[ApprovalFlowStepUsers] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_BreakGroups_Code] ON [Konfeksiyon].[BreakGroups] ([Code]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_BreakSchedules_BreakGroupId] ON [Konfeksiyon].[BreakSchedules] ([BreakGroupId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_BreakSchedules_Code] ON [Konfeksiyon].[BreakSchedules] ([Code]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_BreakSchedules_ShiftId] ON [Konfeksiyon].[BreakSchedules] ([ShiftId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_BundleCardAssignments_BundleId_AssignedAt] ON [Konfeksiyon].[BundleCardAssignments] ([BundleId], [AssignedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_BundleCardAssignments_RfidCardId_AssignedAt] ON [Konfeksiyon].[BundleCardAssignments] ([RfidCardId], [AssignedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_BundleCardAssignments_CurrentPerBundle] ON [Konfeksiyon].[BundleCardAssignments] ([BundleId]) WHERE [ReleasedAt] IS NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_BundleCardAssignments_CurrentPerCard] ON [Konfeksiyon].[BundleCardAssignments] ([RfidCardId]) WHERE [ReleasedAt] IS NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_BundleOperationEvents_BundleId_Id] ON [Konfeksiyon].[BundleOperationEvents] ([BundleId], [Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_BundleOperationEvents_BundleOperationId_Id] ON [Konfeksiyon].[BundleOperationEvents] ([BundleOperationId], [Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_BundleOperationEvents_MachineId_OccurredAt] ON [Konfeksiyon].[BundleOperationEvents] ([MachineId], [OccurredAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_BundleOperationEvents_OccurredAt_EventType] ON [Konfeksiyon].[BundleOperationEvents] ([OccurredAt], [EventType]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_BundleOperationEvents_OperatorId_OccurredAt] ON [Konfeksiyon].[BundleOperationEvents] ([OperatorId], [OccurredAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE UNIQUE INDEX [IX_BundleOperationEvents_RequestId_EventType] ON [Konfeksiyon].[BundleOperationEvents] ([RequestId], [EventType]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_BundleOperationEvents_WorkSessionId] ON [Konfeksiyon].[BundleOperationEvents] ([WorkSessionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE UNIQUE INDEX [IX_BundleOperations_BundleId_RouteOperationId] ON [Konfeksiyon].[BundleOperations] ([BundleId], [RouteOperationId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_BundleOperations_BundleId_SequenceNo] ON [Konfeksiyon].[BundleOperations] ([BundleId], [SequenceNo]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_BundleOperations_MachineId_StartedAt] ON [Konfeksiyon].[BundleOperations] ([MachineId], [StartedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_BundleOperations_OperationId_Status] ON [Konfeksiyon].[BundleOperations] ([OperationId], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_BundleOperations_OperatorId_StartedAt] ON [Konfeksiyon].[BundleOperations] ([OperatorId], [StartedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_BundleOperations_RouteOperationId] ON [Konfeksiyon].[BundleOperations] ([RouteOperationId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_BundleOperations_InProgressPerBundle] ON [Konfeksiyon].[BundleOperations] ([BundleId]) WHERE [Status] = ''IN_PROGRESS''');
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_BundleOperations_InProgressPerMachine] ON [Konfeksiyon].[BundleOperations] ([MachineId]) WHERE [Status] = ''IN_PROGRESS'' AND [MachineId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_BundleOperations_InProgressPerOperator] ON [Konfeksiyon].[BundleOperations] ([OperatorId]) WHERE [Status] = ''IN_PROGRESS'' AND [OperatorId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Bundles_BundleNo] ON [Konfeksiyon].[Bundles] ([BundleNo]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_Bundles_ColorId] ON [Konfeksiyon].[Bundles] ([ColorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_Bundles_ProductionOrderId_ColorId_SizeId_Status] ON [Konfeksiyon].[Bundles] ([ProductionOrderId], [ColorId], [SizeId], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_Bundles_ProductionRouteId] ON [Konfeksiyon].[Bundles] ([ProductionRouteId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_Bundles_SizeId] ON [Konfeksiyon].[Bundles] ([SizeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_Bundles_Status] ON [Konfeksiyon].[Bundles] ([Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Colors_ColorCode] ON [Konfeksiyon].[Colors] ([ColorCode]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_CustomerAddresses_CustomerId] ON [Konfeksiyon].[CustomerAddresses] ([CustomerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_CustomerAddresses_DefaultDelivery] ON [Konfeksiyon].[CustomerAddresses] ([CustomerId]) WHERE [IsDefaultDelivery] = 1');
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_CustomerAddresses_DefaultInvoice] ON [Konfeksiyon].[CustomerAddresses] ([CustomerId]) WHERE [IsDefaultInvoice] = 1');
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE UNIQUE INDEX [IX_CustomerBuyers_CustomerId_Code] ON [Konfeksiyon].[CustomerBuyers] ([CustomerId], [Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_CustomerDiscounts_CustomerId_ProductId_ValidFrom] ON [Konfeksiyon].[CustomerDiscounts] ([CustomerId], [ProductId], [ValidFrom]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_CustomerDiscounts_ProductId] ON [Konfeksiyon].[CustomerDiscounts] ([ProductId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Customers_CustomerCode] ON [Konfeksiyon].[Customers] ([CustomerCode]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_Customers_TaxNumber] ON [Konfeksiyon].[Customers] ([TaxNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_DefectTypes_Code] ON [Konfeksiyon].[DefectTypes] ([Code]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Factories_Code] ON [Konfeksiyon].[Factories] ([Code]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_GoodsReceiptLines_GoodsReceiptId] ON [Konfeksiyon].[GoodsReceiptLines] ([GoodsReceiptId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_GoodsReceiptLines_PurchaseOrderItemId] ON [Konfeksiyon].[GoodsReceiptLines] ([PurchaseOrderItemId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_GoodsReceipts_PurchaseOrderId_Id] ON [Konfeksiyon].[GoodsReceipts] ([PurchaseOrderId], [Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE UNIQUE INDEX [IX_GoodsReceipts_RequestId] ON [Konfeksiyon].[GoodsReceipts] ([RequestId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_GoodsReceipts_WarehouseId] ON [Konfeksiyon].[GoodsReceipts] ([WarehouseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_LineMachineAssignments_MachineId_AssignedAt] ON [Konfeksiyon].[LineMachineAssignments] ([MachineId], [AssignedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_LineMachineAssignments_ProductionLineId_UnassignedAt_Position] ON [Konfeksiyon].[LineMachineAssignments] ([ProductionLineId], [UnassignedAt], [Position]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_LineMachineAssignments_CurrentPerMachine] ON [Konfeksiyon].[LineMachineAssignments] ([MachineId]) WHERE [UnassignedAt] IS NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_LineProductionPlans_ProductionLineId_PlannedStart_PlannedEnd] ON [Konfeksiyon].[LineProductionPlans] ([ProductionLineId], [PlannedStart], [PlannedEnd]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_LineProductionPlans_ProductionOrderId_Status] ON [Konfeksiyon].[LineProductionPlans] ([ProductionOrderId], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_MachineOperations_OperationId] ON [Konfeksiyon].[MachineOperations] ([OperationId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Machines_MachineCode] ON [Konfeksiyon].[Machines] ([MachineCode]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_Machines_MachineTypeId] ON [Konfeksiyon].[Machines] ([MachineTypeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_Machines_OperationId] ON [Konfeksiyon].[Machines] ([OperationId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_Machines_Status] ON [Konfeksiyon].[Machines] ([Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_Machines_SupplierId] ON [Konfeksiyon].[Machines] ([SupplierId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_MachineStatusLogs_MachineId_StartedAt] ON [Konfeksiyon].[MachineStatusLogs] ([MachineId], [StartedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_MachineStatusLogs_Status_StartedAt] ON [Konfeksiyon].[MachineStatusLogs] ([Status], [StartedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_MachineStatusLogs_OpenPerMachine] ON [Konfeksiyon].[MachineStatusLogs] ([MachineId]) WHERE [EndedAt] IS NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_MachineTypes_Code] ON [Konfeksiyon].[MachineTypes] ([Code]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_Models_CustomerId] ON [Konfeksiyon].[Models] ([CustomerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Models_ModelCode] ON [Konfeksiyon].[Models] ([ModelCode]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_OperationDependencies_PredecessorId] ON [Konfeksiyon].[OperationDependencies] ([PredecessorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_Operations_MachineTypeId] ON [Konfeksiyon].[Operations] ([MachineTypeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Operations_OperationCode] ON [Konfeksiyon].[Operations] ([OperationCode]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_OperatorCards_OperatorId_AssignedAt] ON [Konfeksiyon].[OperatorCards] ([OperatorId], [AssignedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_OperatorCards_CurrentPerCard] ON [Konfeksiyon].[OperatorCards] ([RfidCardId]) WHERE [RevokedAt] IS NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_OperatorCards_CurrentPerOperator] ON [Konfeksiyon].[OperatorCards] ([OperatorId]) WHERE [RevokedAt] IS NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_OperatorMachines_MachineId] ON [Konfeksiyon].[OperatorMachines] ([MachineId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_OperatorOperationQualifications_OperationId] ON [Konfeksiyon].[OperatorOperationQualifications] ([OperationId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_Operators_BreakGroupId] ON [Konfeksiyon].[Operators] ([BreakGroupId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Operators_OperatorCode] ON [Konfeksiyon].[Operators] ([OperatorCode]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE UNIQUE INDEX [IX_PayrollBonusTiers_PayrollSettingId_MinEfficiencyPercent] ON [Konfeksiyon].[PayrollBonusTiers] ([PayrollSettingId], [MinEfficiencyPercent]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_PriceListItems_ColorId] ON [Konfeksiyon].[PriceListItems] ([ColorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE UNIQUE INDEX [IX_PriceListItems_PriceListId_ProductId_ColorId_SizeId] ON [Konfeksiyon].[PriceListItems] ([PriceListId], [ProductId], [ColorId], [SizeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_PriceListItems_ProductId] ON [Konfeksiyon].[PriceListItems] ([ProductId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_PriceListItems_SizeId] ON [Konfeksiyon].[PriceListItems] ([SizeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_PriceLists_Code] ON [Konfeksiyon].[PriceLists] ([Code]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_PriceLists_CustomerId_ValidFrom] ON [Konfeksiyon].[PriceLists] ([CustomerId], [ValidFrom]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_ProductGroups_Code] ON [Konfeksiyon].[ProductGroups] ([Code]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_ProductionAreas_Code] ON [Konfeksiyon].[ProductionAreas] ([Code]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_ProductionAreas_FactoryId] ON [Konfeksiyon].[ProductionAreas] ([FactoryId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_ProductionLines_LineCode] ON [Konfeksiyon].[ProductionLines] ([LineCode]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_ProductionLines_ProductionAreaId] ON [Konfeksiyon].[ProductionLines] ([ProductionAreaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_ProductionOrderItems_ColorId] ON [Konfeksiyon].[ProductionOrderItems] ([ColorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ProductionOrderItems_ProductionOrderId_ColorId_SizeId] ON [Konfeksiyon].[ProductionOrderItems] ([ProductionOrderId], [ColorId], [SizeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_ProductionOrderItems_SizeId] ON [Konfeksiyon].[ProductionOrderItems] ([SizeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_ProductionOrders_CustomerId] ON [Konfeksiyon].[ProductionOrders] ([CustomerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_ProductionOrders_DueDate] ON [Konfeksiyon].[ProductionOrders] ([DueDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_ProductionOrders_MaterialWarehouseId] ON [Konfeksiyon].[ProductionOrders] ([MaterialWarehouseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ProductionOrders_OrderNo] ON [Konfeksiyon].[ProductionOrders] ([OrderNo]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_ProductionOrders_ProductId] ON [Konfeksiyon].[ProductionOrders] ([ProductId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_ProductionOrders_SalesOrderId] ON [Konfeksiyon].[ProductionOrders] ([SalesOrderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_ProductionOrders_Status_DueDate] ON [Konfeksiyon].[ProductionOrders] ([Status], [DueDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_ProductionRouteOperations_OperationId] ON [Konfeksiyon].[ProductionRouteOperations] ([OperationId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ProductionRouteOperations_ProductionRouteId_SequenceNo] ON [Konfeksiyon].[ProductionRouteOperations] ([ProductionRouteId], [SequenceNo]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ProductionRoutes_ModelId_Version] ON [Konfeksiyon].[ProductionRoutes] ([ModelId], [Version]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_ProductionRoutes_ActivePerModel] ON [Konfeksiyon].[ProductionRoutes] ([ModelId]) WHERE [Status] = ''ACTIVE'' AND [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_ProductMaterials_ColorId] ON [Konfeksiyon].[ProductMaterials] ([ColorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_ProductMaterials_ProductId_StockItemId] ON [Konfeksiyon].[ProductMaterials] ([ProductId], [StockItemId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_ProductMaterials_SizeId] ON [Konfeksiyon].[ProductMaterials] ([SizeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_ProductMaterials_StockItemId] ON [Konfeksiyon].[ProductMaterials] ([StockItemId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_Products_ModelId] ON [Konfeksiyon].[Products] ([ModelId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Products_ProductCode] ON [Konfeksiyon].[Products] ([ProductCode]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_Products_ProductGroupId] ON [Konfeksiyon].[Products] ([ProductGroupId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_Products_Season] ON [Konfeksiyon].[Products] ([Season]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_PurchaseOrderItems_ProductionOrderId] ON [Konfeksiyon].[PurchaseOrderItems] ([ProductionOrderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE UNIQUE INDEX [IX_PurchaseOrderItems_PurchaseOrderId_StockItemId] ON [Konfeksiyon].[PurchaseOrderItems] ([PurchaseOrderId], [StockItemId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_PurchaseOrderItems_StockItemId] ON [Konfeksiyon].[PurchaseOrderItems] ([StockItemId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE UNIQUE INDEX [IX_PurchaseOrders_OrderNo] ON [Konfeksiyon].[PurchaseOrders] ([OrderNo]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_PurchaseOrders_Status_ExpectedDate] ON [Konfeksiyon].[PurchaseOrders] ([Status], [ExpectedDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_PurchaseOrders_SupplierId] ON [Konfeksiyon].[PurchaseOrders] ([SupplierId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_QualityInspections_BundleId_Id] ON [Konfeksiyon].[QualityInspections] ([BundleId], [Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_QualityInspections_BundleOperationId] ON [Konfeksiyon].[QualityInspections] ([BundleOperationId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_QualityInspections_InspectedAt_Result] ON [Konfeksiyon].[QualityInspections] ([InspectedAt], [Result]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_QualityRecords_DefectTypeId] ON [Konfeksiyon].[QualityRecords] ([DefectTypeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_QualityRecords_QualityInspectionId] ON [Konfeksiyon].[QualityRecords] ([QualityInspectionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_ReworkTasks_BundleId_Id] ON [Konfeksiyon].[ReworkTasks] ([BundleId], [Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_ReworkTasks_BundleOperationId_Status] ON [Konfeksiyon].[ReworkTasks] ([BundleOperationId], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_ReworkTasks_QualityInspectionId] ON [Konfeksiyon].[ReworkTasks] ([QualityInspectionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_ReworkTasks_Status_Id] ON [Konfeksiyon].[ReworkTasks] ([Status], [Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_RfidCards_Uid] ON [Konfeksiyon].[RfidCards] ([Uid]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_RfidReadLogs_ReadAt] ON [Konfeksiyon].[RfidReadLogs] ([ReadAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE UNIQUE INDEX [IX_RfidReadLogs_ReadId] ON [Konfeksiyon].[RfidReadLogs] ([ReadId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_RfidReadLogs_TabletId_Uid_Kind_ReadAt] ON [Konfeksiyon].[RfidReadLogs] ([TabletId], [Uid], [Kind], [ReadAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_RfidReadLogs_Uid_ReadAt] ON [Konfeksiyon].[RfidReadLogs] ([Uid], [ReadAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_SalesOrderApprovalEvents_SalesOrderId_CreatedAt] ON [Konfeksiyon].[SalesOrderApprovalEvents] ([SalesOrderId], [CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE UNIQUE INDEX [IX_SalesOrderApprovalSteps_SalesOrderId_Round_Sequence] ON [Konfeksiyon].[SalesOrderApprovalSteps] ([SalesOrderId], [Round], [Sequence]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    EXEC(N'CREATE INDEX [IX_SalesOrderApprovalSteps_Status] ON [Konfeksiyon].[SalesOrderApprovalSteps] ([Status]) WHERE [Status] = ''PENDING''');
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_SalesOrderLines_ColorId] ON [Konfeksiyon].[SalesOrderLines] ([ColorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_SalesOrderLines_ProductId] ON [Konfeksiyon].[SalesOrderLines] ([ProductId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE UNIQUE INDEX [IX_SalesOrderLines_SalesOrderId_ProductId_ColorId_SizeId] ON [Konfeksiyon].[SalesOrderLines] ([SalesOrderId], [ProductId], [ColorId], [SizeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_SalesOrderLines_SizeId] ON [Konfeksiyon].[SalesOrderLines] ([SizeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_SalesOrderReservations_SalesOrderId] ON [Konfeksiyon].[SalesOrderReservations] ([SalesOrderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE UNIQUE INDEX [IX_SalesOrderReservations_SalesOrderLineId_WarehouseId] ON [Konfeksiyon].[SalesOrderReservations] ([SalesOrderLineId], [WarehouseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_SalesOrderReservations_StockItemId] ON [Konfeksiyon].[SalesOrderReservations] ([StockItemId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_SalesOrderReservations_WarehouseId_StockItemId] ON [Konfeksiyon].[SalesOrderReservations] ([WarehouseId], [StockItemId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_SalesOrders_CustomerBuyerId] ON [Konfeksiyon].[SalesOrders] ([CustomerBuyerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_SalesOrders_CustomerId] ON [Konfeksiyon].[SalesOrders] ([CustomerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_SalesOrders_DeliveryAddressId] ON [Konfeksiyon].[SalesOrders] ([DeliveryAddressId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE UNIQUE INDEX [IX_SalesOrders_OrderNo] ON [Konfeksiyon].[SalesOrders] ([OrderNo]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_SalesOrders_Status_DueDate] ON [Konfeksiyon].[SalesOrders] ([Status], [DueDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Shifts_Code] ON [Konfeksiyon].[Shifts] ([Code]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ShipmentItems_ShipmentId_StockItemId] ON [Konfeksiyon].[ShipmentItems] ([ShipmentId], [StockItemId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_ShipmentItems_StockItemId] ON [Konfeksiyon].[ShipmentItems] ([StockItemId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_Shipments_CustomerId] ON [Konfeksiyon].[Shipments] ([CustomerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_Shipments_ProductionOrderId_Status] ON [Konfeksiyon].[Shipments] ([ProductionOrderId], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_Shipments_SalesOrderId_Status] ON [Konfeksiyon].[Shipments] ([SalesOrderId], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Shipments_ShipmentNo] ON [Konfeksiyon].[Shipments] ([ShipmentNo]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_Shipments_ShippedAt] ON [Konfeksiyon].[Shipments] ([ShippedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_Shipments_Status_ShipDate] ON [Konfeksiyon].[Shipments] ([Status], [ShipDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_Shipments_WarehouseId] ON [Konfeksiyon].[Shipments] ([WarehouseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Sizes_SizeCode] ON [Konfeksiyon].[Sizes] ([SizeCode]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_StockCountLines_LocationId] ON [Konfeksiyon].[StockCountLines] ([LocationId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_StockCountLines_LotId] ON [Konfeksiyon].[StockCountLines] ([LotId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE UNIQUE INDEX [IX_StockCountLines_StockCountId_LocationId_StockItemId_LotId] ON [Konfeksiyon].[StockCountLines] ([StockCountId], [LocationId], [StockItemId], [LotId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_StockCountLines_StockItemId] ON [Konfeksiyon].[StockCountLines] ([StockItemId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE UNIQUE INDEX [IX_StockCounts_CountNo] ON [Konfeksiyon].[StockCounts] ([CountNo]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_StockCounts_LocationId] ON [Konfeksiyon].[StockCounts] ([LocationId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_StockCounts_WarehouseId_Status] ON [Konfeksiyon].[StockCounts] ([WarehouseId], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_StockItems_Code] ON [Konfeksiyon].[StockItems] ([Code]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_StockItems_ColorId] ON [Konfeksiyon].[StockItems] ([ColorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_StockItems_ItemType] ON [Konfeksiyon].[StockItems] ([ItemType]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_StockItems_SizeId] ON [Konfeksiyon].[StockItems] ([SizeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_StockItems_SupplierId] ON [Konfeksiyon].[StockItems] ([SupplierId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_StockItems_FinishedGood] ON [Konfeksiyon].[StockItems] ([ProductId], [ColorId], [SizeId]) WHERE [ProductId] IS NOT NULL AND [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_StockLots_BatchNo] ON [Konfeksiyon].[StockLots] ([BatchNo]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE UNIQUE INDEX [IX_StockLots_LotNo] ON [Konfeksiyon].[StockLots] ([LotNo]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_StockLots_PurchaseOrderId] ON [Konfeksiyon].[StockLots] ([PurchaseOrderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_StockLots_StockItemId_Id] ON [Konfeksiyon].[StockLots] ([StockItemId], [Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE UNIQUE INDEX [IX_StockReservations_ProductionOrderId_WarehouseId_StockItemId] ON [Konfeksiyon].[StockReservations] ([ProductionOrderId], [WarehouseId], [StockItemId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_StockReservations_StockItemId] ON [Konfeksiyon].[StockReservations] ([StockItemId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_StockReservations_WarehouseId_StockItemId] ON [Konfeksiyon].[StockReservations] ([WarehouseId], [StockItemId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Stocks_LocationId_StockItemId_LotId] ON [Konfeksiyon].[Stocks] ([LocationId], [StockItemId], [LotId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_Stocks_LotId] ON [Konfeksiyon].[Stocks] ([LotId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_Stocks_StockItemId] ON [Konfeksiyon].[Stocks] ([StockItemId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_Stocks_WarehouseId_StockItemId] ON [Konfeksiyon].[Stocks] ([WarehouseId], [StockItemId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_StockTransactions_BundleId_Type] ON [Konfeksiyon].[StockTransactions] ([BundleId], [Type]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_StockTransactions_CounterLocationId] ON [Konfeksiyon].[StockTransactions] ([CounterLocationId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_StockTransactions_CounterWarehouseId] ON [Konfeksiyon].[StockTransactions] ([CounterWarehouseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_StockTransactions_LocationId_Id] ON [Konfeksiyon].[StockTransactions] ([LocationId], [Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_StockTransactions_LotId_Id] ON [Konfeksiyon].[StockTransactions] ([LotId], [Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_StockTransactions_OccurredAt_Type] ON [Konfeksiyon].[StockTransactions] ([OccurredAt], [Type]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_StockTransactions_ProductionOrderId] ON [Konfeksiyon].[StockTransactions] ([ProductionOrderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_StockTransactions_PurchaseOrderId] ON [Konfeksiyon].[StockTransactions] ([PurchaseOrderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE UNIQUE INDEX [IX_StockTransactions_RequestId_Type] ON [Konfeksiyon].[StockTransactions] ([RequestId], [Type]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_StockTransactions_ShipmentId] ON [Konfeksiyon].[StockTransactions] ([ShipmentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_StockTransactions_StockItemId_WarehouseId_Id] ON [Konfeksiyon].[StockTransactions] ([StockItemId], [WarehouseId], [Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_StockTransactions_WarehouseId_Id] ON [Konfeksiyon].[StockTransactions] ([WarehouseId], [Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_StockTransactions_ReceiptPerBundle] ON [Konfeksiyon].[StockTransactions] ([BundleId]) WHERE [BundleId] IS NOT NULL AND [Type] = ''PRODUCTION_RECEIPT''');
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_StopReasons_Category_SortOrder] ON [Konfeksiyon].[StopReasons] ([Category], [SortOrder]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_StopReasons_Code] ON [Konfeksiyon].[StopReasons] ([Code]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Suppliers_SupplierCode] ON [Konfeksiyon].[Suppliers] ([SupplierCode]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Tablets_TabletCode] ON [Konfeksiyon].[Tablets] ([TabletCode]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_Tablets_DeviceIdentifier] ON [Konfeksiyon].[Tablets] ([DeviceIdentifier]) WHERE [DeviceIdentifier] IS NOT NULL AND [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_Tablets_Machine] ON [Konfeksiyon].[Tablets] ([MachineId]) WHERE [MachineId] IS NOT NULL AND [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_WarehouseLocations_WarehouseId_Code] ON [Konfeksiyon].[WarehouseLocations] ([WarehouseId], [Code]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_WarehouseLocations_Default] ON [Konfeksiyon].[WarehouseLocations] ([WarehouseId]) WHERE [IsDefault] = 1 AND [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Warehouses_Code] ON [Konfeksiyon].[Warehouses] ([Code]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_WorkSessionPauses_BreakScheduleId_StartedAt] ON [Konfeksiyon].[WorkSessionPauses] ([BreakScheduleId], [StartedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_WorkSessionPauses_StopReasonId] ON [Konfeksiyon].[WorkSessionPauses] ([StopReasonId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_WorkSessionPauses_Type_StartedAt] ON [Konfeksiyon].[WorkSessionPauses] ([Type], [StartedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_WorkSessionPauses_WorkSessionId_StartedAt] ON [Konfeksiyon].[WorkSessionPauses] ([WorkSessionId], [StartedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_WorkSessionPauses_OpenPerSession] ON [Konfeksiyon].[WorkSessionPauses] ([WorkSessionId]) WHERE [EndedAt] IS NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_WorkSessions_LoginAt] ON [Konfeksiyon].[WorkSessions] ([LoginAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_WorkSessions_MachineId_LoginAt] ON [Konfeksiyon].[WorkSessions] ([MachineId], [LoginAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_WorkSessions_OperatorId_LoginAt] ON [Konfeksiyon].[WorkSessions] ([OperatorId], [LoginAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    CREATE INDEX [IX_WorkSessions_TabletId] ON [Konfeksiyon].[WorkSessions] ([TabletId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_WorkSessions_OpenPerMachine] ON [Konfeksiyon].[WorkSessions] ([MachineId]) WHERE [LogoutAt] IS NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_WorkSessions_OpenPerOperator] ON [Konfeksiyon].[WorkSessions] ([OperatorId]) WHERE [LogoutAt] IS NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    SET IDENTITY_INSERT [Core].[Permissions] ON;
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
    SET IDENTITY_INSERT [Core].[Permissions] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    INSERT INTO [Core].[RolePermissions] ([RoleId], [PermissionId])
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
      AND NOT EXISTS (SELECT 1 FROM [Core].[RolePermissions] rp WHERE rp.[RoleId] = v.[RoleId] AND rp.[PermissionId] = v.[PermissionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    EXEC(N'CREATE TRIGGER [Konfeksiyon].[TR_BundleOperationEvents_AppendOnly]
        ON [Konfeksiyon].[BundleOperationEvents]
        INSTEAD OF UPDATE, DELETE
        AS
        BEGIN
            SET NOCOUNT ON;
            THROW 51000, ''BundleOperationEvents is append-only: production events cannot be updated or deleted.'', 1;
        END');
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    EXEC(N'CREATE TRIGGER [Konfeksiyon].[TR_StockTransactions_AppendOnly]
        ON [Konfeksiyon].[StockTransactions]
        INSTEAD OF UPDATE, DELETE
        AS
        BEGIN
            SET NOCOUNT ON;
            THROW 51000, ''StockTransactions is append-only: stock movements cannot be updated or deleted.'', 1;
        END');
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    INSERT INTO [Konfeksiyon].[WarehouseLocations]
        ([WarehouseId], [Code], [Zone], [Description], [IsDefault], [IsActive], [CreatedAt], [CreatedBy], [IsDeleted])
    SELECT w.[Id], N'GENEL', N'GENEL', N'Adres belirtilmeyen stok', 1, 1, SYSUTCDATETIME(), N'system', 0
    FROM [Konfeksiyon].[Warehouses] AS w
    WHERE NOT EXISTS (SELECT 1 FROM [Konfeksiyon].[WarehouseLocations] l WHERE l.[WarehouseId] = w.[Id] AND l.[IsDefault] = 1);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030065037_InitialKonfeksiyon'
)
BEGIN
    INSERT INTO [Konfeksiyon].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261030065037_InitialKonfeksiyon', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102072240_AddInvoicingAndAccounts'
)
BEGIN
    ALTER TABLE [Konfeksiyon].[Shipments] ADD [InvoiceId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102072240_AddInvoicingAndAccounts'
)
BEGIN
    ALTER TABLE [Konfeksiyon].[Customers] ADD [PaymentTermDays] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102072240_AddInvoicingAndAccounts'
)
BEGIN
    CREATE TABLE [Konfeksiyon].[CustomerPayments] (
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
        CONSTRAINT [PK_CustomerPayments] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_CustomerPayments_Amount] CHECK ([Amount] > 0),
        CONSTRAINT [CK_CustomerPayments_Method] CHECK ([Method] IN ('CASH', 'BANK_TRANSFER', 'CHECK', 'OPENING')),
        CONSTRAINT [CK_CustomerPayments_Status] CHECK ([Status] IN ('ACTIVE', 'CANCELLED')),
        CONSTRAINT [FK_CustomerPayments_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Konfeksiyon].[Customers] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102072240_AddInvoicingAndAccounts'
)
BEGIN
    CREATE TABLE [Konfeksiyon].[Invoices] (
        [Id] int NOT NULL IDENTITY,
        [InvoiceNo] nvarchar(30) NOT NULL,
        [CustomerId] int NOT NULL,
        [InvoiceDate] date NOT NULL,
        [DueDate] date NULL,
        [Currency] nvarchar(3) NOT NULL,
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
        CONSTRAINT [CK_Invoices_Withholding] CHECK ([WithholdingPercent] IS NULL OR [WithholdingPercent] BETWEEN 0 AND 100),
        CONSTRAINT [FK_Invoices_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Konfeksiyon].[Customers] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102072240_AddInvoicingAndAccounts'
)
BEGIN
    CREATE TABLE [Konfeksiyon].[InvoiceLines] (
        [Id] int NOT NULL IDENTITY,
        [InvoiceId] int NOT NULL,
        [StockItemId] int NOT NULL,
        [SalesOrderLineId] int NULL,
        [Description] nvarchar(300) NOT NULL,
        [Quantity] decimal(18,3) NOT NULL,
        [UnitPrice] decimal(18,4) NULL,
        [DiscountPercent] decimal(5,2) NULL,
        [VatRate] decimal(5,2) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_InvoiceLines] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_InvoiceLines_Discount] CHECK ([DiscountPercent] IS NULL OR [DiscountPercent] BETWEEN 0 AND 100),
        CONSTRAINT [CK_InvoiceLines_Quantity] CHECK ([Quantity] > 0),
        CONSTRAINT [CK_InvoiceLines_VatRate] CHECK ([VatRate] BETWEEN 0 AND 100),
        CONSTRAINT [FK_InvoiceLines_Invoices_InvoiceId] FOREIGN KEY ([InvoiceId]) REFERENCES [Konfeksiyon].[Invoices] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_InvoiceLines_SalesOrderLines_SalesOrderLineId] FOREIGN KEY ([SalesOrderLineId]) REFERENCES [Konfeksiyon].[SalesOrderLines] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_InvoiceLines_StockItems_StockItemId] FOREIGN KEY ([StockItemId]) REFERENCES [Konfeksiyon].[StockItems] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102072240_AddInvoicingAndAccounts'
)
BEGIN
    CREATE TABLE [Konfeksiyon].[PaymentAllocations] (
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
        CONSTRAINT [FK_PaymentAllocations_CustomerPayments_PaymentId] FOREIGN KEY ([PaymentId]) REFERENCES [Konfeksiyon].[CustomerPayments] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PaymentAllocations_Invoices_InvoiceId] FOREIGN KEY ([InvoiceId]) REFERENCES [Konfeksiyon].[Invoices] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102072240_AddInvoicingAndAccounts'
)
BEGIN
    CREATE INDEX [IX_Shipments_InvoiceId] ON [Konfeksiyon].[Shipments] ([InvoiceId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102072240_AddInvoicingAndAccounts'
)
BEGIN
    CREATE INDEX [IX_CustomerPayments_CustomerId_Currency_PaymentDate] ON [Konfeksiyon].[CustomerPayments] ([CustomerId], [Currency], [PaymentDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102072240_AddInvoicingAndAccounts'
)
BEGIN
    CREATE UNIQUE INDEX [IX_CustomerPayments_PaymentNo] ON [Konfeksiyon].[CustomerPayments] ([PaymentNo]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102072240_AddInvoicingAndAccounts'
)
BEGIN
    CREATE INDEX [IX_InvoiceLines_InvoiceId] ON [Konfeksiyon].[InvoiceLines] ([InvoiceId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102072240_AddInvoicingAndAccounts'
)
BEGIN
    CREATE INDEX [IX_InvoiceLines_SalesOrderLineId] ON [Konfeksiyon].[InvoiceLines] ([SalesOrderLineId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102072240_AddInvoicingAndAccounts'
)
BEGIN
    CREATE INDEX [IX_InvoiceLines_StockItemId] ON [Konfeksiyon].[InvoiceLines] ([StockItemId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102072240_AddInvoicingAndAccounts'
)
BEGIN
    CREATE INDEX [IX_Invoices_CustomerId_Status] ON [Konfeksiyon].[Invoices] ([CustomerId], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102072240_AddInvoicingAndAccounts'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Invoices_InvoiceNo] ON [Konfeksiyon].[Invoices] ([InvoiceNo]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102072240_AddInvoicingAndAccounts'
)
BEGIN
    CREATE INDEX [IX_PaymentAllocations_InvoiceId] ON [Konfeksiyon].[PaymentAllocations] ([InvoiceId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102072240_AddInvoicingAndAccounts'
)
BEGIN
    CREATE UNIQUE INDEX [IX_PaymentAllocations_PaymentId_InvoiceId] ON [Konfeksiyon].[PaymentAllocations] ([PaymentId], [InvoiceId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102072240_AddInvoicingAndAccounts'
)
BEGIN
    ALTER TABLE [Konfeksiyon].[Shipments] ADD CONSTRAINT [FK_Shipments_Invoices_InvoiceId] FOREIGN KEY ([InvoiceId]) REFERENCES [Konfeksiyon].[Invoices] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102072240_AddInvoicingAndAccounts'
)
BEGIN
    SET IDENTITY_INSERT [Core].[Permissions] ON;
    EXEC(N'INSERT INTO [Core].[Permissions] ([Id], [Code], [Module], [Description])
    SELECT v.[Id], v.[Code], v.[Module], v.[Description] FROM (VALUES
    (93, N''Invoice.View'', N''Invoice'', N''Satış faturalarını görüntüleme''),
    (94, N''Invoice.Manage'', N''Invoice'', N''Satış faturası hazırlama, onaylama ve iptal''),
    (95, N''Account.View'', N''Account'', N''Cari hesapları, ekstreyi ve tahsilatları görüntüleme''),
    (96, N''Account.Manage'', N''Account'', N''Tahsilat girişi, faturalara dağıtım, iptal ve açılış bakiyesi'')
    ) v([Id], [Code], [Module], [Description])
    WHERE NOT EXISTS (SELECT 1 FROM [Core].[Permissions] p WHERE p.[Id] = v.[Id] OR p.[Code] = v.[Code])');
    SET IDENTITY_INSERT [Core].[Permissions] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102072240_AddInvoicingAndAccounts'
)
BEGIN
    INSERT INTO [Core].[RolePermissions] ([RoleId], [PermissionId])
    SELECT v.[RoleId], v.[PermissionId] FROM (VALUES
    (1, 93), (1, 94), (1, 95), (1, 96)
    ) v([RoleId], [PermissionId])
    WHERE EXISTS (SELECT 1 FROM [Core].[Roles] r WHERE r.[Id] = v.[RoleId])
      AND EXISTS (SELECT 1 FROM [Core].[Permissions] p WHERE p.[Id] = v.[PermissionId])
      AND NOT EXISTS (SELECT 1 FROM [Core].[RolePermissions] rp WHERE rp.[RoleId] = v.[RoleId] AND rp.[PermissionId] = v.[PermissionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102072240_AddInvoicingAndAccounts'
)
BEGIN
    INSERT INTO [Konfeksiyon].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261102072240_AddInvoicingAndAccounts', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102091220_AddCartonsAndDocumentLanguage'
)
BEGIN
    ALTER TABLE [Konfeksiyon].[Customers] ADD [DocumentLanguage] nvarchar(30) NOT NULL DEFAULT N'TURKISH';
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102091220_AddCartonsAndDocumentLanguage'
)
BEGIN
    CREATE TABLE [Konfeksiyon].[AssortmentTemplates] (
        [Id] int NOT NULL IDENTITY,
        [Code] nvarchar(30) NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [CustomerId] int NULL,
        [Notes] nvarchar(500) NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_AssortmentTemplates] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AssortmentTemplates_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Konfeksiyon].[Customers] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102091220_AddCartonsAndDocumentLanguage'
)
BEGIN
    CREATE TABLE [Konfeksiyon].[AssortmentTemplateLines] (
        [Id] int NOT NULL IDENTITY,
        [AssortmentTemplateId] int NOT NULL,
        [SizeId] int NOT NULL,
        [Ratio] int NOT NULL,
        CONSTRAINT [PK_AssortmentTemplateLines] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_AssortmentTemplateLines_Ratio] CHECK ([Ratio] > 0),
        CONSTRAINT [FK_AssortmentTemplateLines_AssortmentTemplates_AssortmentTemplateId] FOREIGN KEY ([AssortmentTemplateId]) REFERENCES [Konfeksiyon].[AssortmentTemplates] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_AssortmentTemplateLines_Sizes_SizeId] FOREIGN KEY ([SizeId]) REFERENCES [Konfeksiyon].[Sizes] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102091220_AddCartonsAndDocumentLanguage'
)
BEGIN
    CREATE TABLE [Konfeksiyon].[ShipmentCartons] (
        [Id] int NOT NULL IDENTITY,
        [ShipmentId] int NOT NULL,
        [CartonNo] int NOT NULL,
        [AssortmentTemplateId] int NULL,
        [NetKg] decimal(10,3) NULL,
        [GrossKg] decimal(10,3) NULL,
        [LoadedAt] datetime2 NULL,
        [LoadedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_ShipmentCartons] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_ShipmentCartons_CartonNo] CHECK ([CartonNo] > 0),
        CONSTRAINT [CK_ShipmentCartons_Loaded] CHECK (([LoadedAt] IS NULL AND [LoadedBy] IS NULL) OR ([LoadedAt] IS NOT NULL AND [LoadedBy] IS NOT NULL)),
        CONSTRAINT [CK_ShipmentCartons_Weight] CHECK (([NetKg] IS NULL OR [NetKg] >= 0) AND ([GrossKg] IS NULL OR [GrossKg] >= 0)),
        CONSTRAINT [FK_ShipmentCartons_AssortmentTemplates_AssortmentTemplateId] FOREIGN KEY ([AssortmentTemplateId]) REFERENCES [Konfeksiyon].[AssortmentTemplates] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ShipmentCartons_Shipments_ShipmentId] FOREIGN KEY ([ShipmentId]) REFERENCES [Konfeksiyon].[Shipments] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102091220_AddCartonsAndDocumentLanguage'
)
BEGIN
    CREATE TABLE [Konfeksiyon].[ShipmentCartonLines] (
        [Id] int NOT NULL IDENTITY,
        [ShipmentCartonId] int NOT NULL,
        [StockItemId] int NOT NULL,
        [Quantity] decimal(18,3) NOT NULL,
        CONSTRAINT [PK_ShipmentCartonLines] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_ShipmentCartonLines_Quantity] CHECK ([Quantity] > 0),
        CONSTRAINT [FK_ShipmentCartonLines_ShipmentCartons_ShipmentCartonId] FOREIGN KEY ([ShipmentCartonId]) REFERENCES [Konfeksiyon].[ShipmentCartons] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ShipmentCartonLines_StockItems_StockItemId] FOREIGN KEY ([StockItemId]) REFERENCES [Konfeksiyon].[StockItems] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102091220_AddCartonsAndDocumentLanguage'
)
BEGIN
    EXEC(N'ALTER TABLE [Konfeksiyon].[Customers] ADD CONSTRAINT [CK_Customers_DocumentLanguage] CHECK ([DocumentLanguage] IN (''TURKISH'', ''ENGLISH''))');
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102091220_AddCartonsAndDocumentLanguage'
)
BEGIN
    CREATE UNIQUE INDEX [IX_AssortmentTemplateLines_AssortmentTemplateId_SizeId] ON [Konfeksiyon].[AssortmentTemplateLines] ([AssortmentTemplateId], [SizeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102091220_AddCartonsAndDocumentLanguage'
)
BEGIN
    CREATE INDEX [IX_AssortmentTemplateLines_SizeId] ON [Konfeksiyon].[AssortmentTemplateLines] ([SizeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102091220_AddCartonsAndDocumentLanguage'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_AssortmentTemplates_Code] ON [Konfeksiyon].[AssortmentTemplates] ([Code]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102091220_AddCartonsAndDocumentLanguage'
)
BEGIN
    CREATE INDEX [IX_AssortmentTemplates_CustomerId] ON [Konfeksiyon].[AssortmentTemplates] ([CustomerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102091220_AddCartonsAndDocumentLanguage'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ShipmentCartonLines_ShipmentCartonId_StockItemId] ON [Konfeksiyon].[ShipmentCartonLines] ([ShipmentCartonId], [StockItemId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102091220_AddCartonsAndDocumentLanguage'
)
BEGIN
    CREATE INDEX [IX_ShipmentCartonLines_StockItemId] ON [Konfeksiyon].[ShipmentCartonLines] ([StockItemId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102091220_AddCartonsAndDocumentLanguage'
)
BEGIN
    CREATE INDEX [IX_ShipmentCartons_AssortmentTemplateId] ON [Konfeksiyon].[ShipmentCartons] ([AssortmentTemplateId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102091220_AddCartonsAndDocumentLanguage'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ShipmentCartons_ShipmentId_CartonNo] ON [Konfeksiyon].[ShipmentCartons] ([ShipmentId], [CartonNo]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102091220_AddCartonsAndDocumentLanguage'
)
BEGIN
    INSERT INTO [Konfeksiyon].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261102091220_AddCartonsAndDocumentLanguage', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102094628_AddCuttingPlans'
)
BEGIN
    ALTER TABLE [Konfeksiyon].[Bundles] ADD [CuttingPlanId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102094628_AddCuttingPlans'
)
BEGIN
    CREATE TABLE [Konfeksiyon].[CuttingPlans] (
        [Id] int NOT NULL IDENTITY,
        [PlanNo] nvarchar(30) NOT NULL,
        [ProductionOrderId] int NOT NULL,
        [ColorId] int NOT NULL,
        [MarkerLength] decimal(9,3) NOT NULL,
        [EndAllowance] decimal(9,3) NOT NULL,
        [PlannedPlies] int NOT NULL,
        [ActualPlies] int NULL,
        [Status] nvarchar(30) NOT NULL,
        [Notes] nvarchar(500) NULL,
        [CutAt] datetime2 NULL,
        [CutBy] nvarchar(100) NULL,
        [RowVersion] rowversion NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_CuttingPlans] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_CuttingPlans_ActualPlies] CHECK ([ActualPlies] IS NULL OR [ActualPlies] > 0),
        CONSTRAINT [CK_CuttingPlans_EndAllowance] CHECK ([EndAllowance] >= 0),
        CONSTRAINT [CK_CuttingPlans_MarkerLength] CHECK ([MarkerLength] > 0),
        CONSTRAINT [CK_CuttingPlans_PlannedPlies] CHECK ([PlannedPlies] > 0),
        CONSTRAINT [CK_CuttingPlans_Status] CHECK ([Status] IN ('DRAFT', 'CUT', 'CANCELLED')),
        CONSTRAINT [FK_CuttingPlans_Colors_ColorId] FOREIGN KEY ([ColorId]) REFERENCES [Konfeksiyon].[Colors] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_CuttingPlans_ProductionOrders_ProductionOrderId] FOREIGN KEY ([ProductionOrderId]) REFERENCES [Konfeksiyon].[ProductionOrders] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102094628_AddCuttingPlans'
)
BEGIN
    CREATE TABLE [Konfeksiyon].[CuttingPlanLots] (
        [Id] int NOT NULL IDENTITY,
        [CuttingPlanId] int NOT NULL,
        [StockLotId] int NOT NULL,
        [SortOrder] int NOT NULL,
        [UsedQuantity] decimal(18,3) NULL,
        CONSTRAINT [PK_CuttingPlanLots] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_CuttingPlanLots_UsedQuantity] CHECK ([UsedQuantity] IS NULL OR [UsedQuantity] >= 0),
        CONSTRAINT [FK_CuttingPlanLots_CuttingPlans_CuttingPlanId] FOREIGN KEY ([CuttingPlanId]) REFERENCES [Konfeksiyon].[CuttingPlans] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_CuttingPlanLots_StockLots_StockLotId] FOREIGN KEY ([StockLotId]) REFERENCES [Konfeksiyon].[StockLots] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102094628_AddCuttingPlans'
)
BEGIN
    CREATE TABLE [Konfeksiyon].[CuttingPlanSizes] (
        [Id] int NOT NULL IDENTITY,
        [CuttingPlanId] int NOT NULL,
        [SizeId] int NOT NULL,
        [Ratio] int NOT NULL,
        [CutQuantity] int NULL,
        CONSTRAINT [PK_CuttingPlanSizes] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_CuttingPlanSizes_CutQuantity] CHECK ([CutQuantity] IS NULL OR [CutQuantity] >= 0),
        CONSTRAINT [CK_CuttingPlanSizes_Ratio] CHECK ([Ratio] > 0),
        CONSTRAINT [FK_CuttingPlanSizes_CuttingPlans_CuttingPlanId] FOREIGN KEY ([CuttingPlanId]) REFERENCES [Konfeksiyon].[CuttingPlans] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_CuttingPlanSizes_Sizes_SizeId] FOREIGN KEY ([SizeId]) REFERENCES [Konfeksiyon].[Sizes] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102094628_AddCuttingPlans'
)
BEGIN
    CREATE INDEX [IX_Bundles_CuttingPlanId] ON [Konfeksiyon].[Bundles] ([CuttingPlanId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102094628_AddCuttingPlans'
)
BEGIN
    CREATE UNIQUE INDEX [IX_CuttingPlanLots_CuttingPlanId_StockLotId] ON [Konfeksiyon].[CuttingPlanLots] ([CuttingPlanId], [StockLotId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102094628_AddCuttingPlans'
)
BEGIN
    CREATE INDEX [IX_CuttingPlanLots_StockLotId] ON [Konfeksiyon].[CuttingPlanLots] ([StockLotId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102094628_AddCuttingPlans'
)
BEGIN
    CREATE INDEX [IX_CuttingPlans_ColorId] ON [Konfeksiyon].[CuttingPlans] ([ColorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102094628_AddCuttingPlans'
)
BEGIN
    CREATE UNIQUE INDEX [IX_CuttingPlans_PlanNo] ON [Konfeksiyon].[CuttingPlans] ([PlanNo]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102094628_AddCuttingPlans'
)
BEGIN
    CREATE INDEX [IX_CuttingPlans_ProductionOrderId_Status] ON [Konfeksiyon].[CuttingPlans] ([ProductionOrderId], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102094628_AddCuttingPlans'
)
BEGIN
    CREATE INDEX [IX_CuttingPlans_Status] ON [Konfeksiyon].[CuttingPlans] ([Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102094628_AddCuttingPlans'
)
BEGIN
    CREATE UNIQUE INDEX [IX_CuttingPlanSizes_CuttingPlanId_SizeId] ON [Konfeksiyon].[CuttingPlanSizes] ([CuttingPlanId], [SizeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102094628_AddCuttingPlans'
)
BEGIN
    CREATE INDEX [IX_CuttingPlanSizes_SizeId] ON [Konfeksiyon].[CuttingPlanSizes] ([SizeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102094628_AddCuttingPlans'
)
BEGIN
    ALTER TABLE [Konfeksiyon].[Bundles] ADD CONSTRAINT [FK_Bundles_CuttingPlans_CuttingPlanId] FOREIGN KEY ([CuttingPlanId]) REFERENCES [Konfeksiyon].[CuttingPlans] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102094628_AddCuttingPlans'
)
BEGIN
    SET IDENTITY_INSERT [Core].[Permissions] ON;
    EXEC(N'INSERT INTO [Core].[Permissions] ([Id], [Code], [Module], [Description])
    SELECT v.[Id], v.[Code], v.[Module], v.[Description] FROM (VALUES
    (97, N''CuttingPlan.View'', N''CuttingPlan'', N''Kesim planlarını görüntüleme''),
    (98, N''CuttingPlan.Manage'', N''CuttingPlan'', N''Kesim planı oluşturma ve kesim kaydı'')
    ) v([Id], [Code], [Module], [Description])
    WHERE NOT EXISTS (SELECT 1 FROM [Core].[Permissions] p WHERE p.[Id] = v.[Id] OR p.[Code] = v.[Code])');
    SET IDENTITY_INSERT [Core].[Permissions] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102094628_AddCuttingPlans'
)
BEGIN
    INSERT INTO [Core].[RolePermissions] ([RoleId], [PermissionId])
    SELECT v.[RoleId], v.[PermissionId] FROM (VALUES
    (1, 97), (1, 98)
    ) v([RoleId], [PermissionId])
    WHERE EXISTS (SELECT 1 FROM [Core].[Roles] r WHERE r.[Id] = v.[RoleId])
      AND EXISTS (SELECT 1 FROM [Core].[Permissions] p WHERE p.[Id] = v.[PermissionId])
      AND NOT EXISTS (SELECT 1 FROM [Core].[RolePermissions] rp WHERE rp.[RoleId] = v.[RoleId] AND rp.[PermissionId] = v.[PermissionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102094628_AddCuttingPlans'
)
BEGIN
    INSERT INTO [Konfeksiyon].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261102094628_AddCuttingPlans', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102110059_AddSubcontractingAndQuotations'
)
BEGIN
    ALTER TABLE [Konfeksiyon].[ProductionRouteOperations] ADD [IsSubcontracted] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102110059_AddSubcontractingAndQuotations'
)
BEGIN
    ALTER TABLE [Konfeksiyon].[ProductionRouteOperations] ADD [SubcontractorId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102110059_AddSubcontractingAndQuotations'
)
BEGIN
    ALTER TABLE [Konfeksiyon].[Bundles] ADD [AtSubcontractor] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102110059_AddSubcontractingAndQuotations'
)
BEGIN
    CREATE TABLE [Konfeksiyon].[CostSettings] (
        [Id] int NOT NULL,
        [LaborCostPerMinute] decimal(18,4) NOT NULL,
        [EnergyCostPerMinute] decimal(18,4) NOT NULL,
        [Currency] char(3) NOT NULL,
        [OverheadPercent] decimal(7,2) NOT NULL,
        [DefaultFirePercent] decimal(7,2) NOT NULL,
        [DefaultMarginPercent] decimal(7,2) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_CostSettings] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_CostSettings_Percents] CHECK ([OverheadPercent] >= 0 AND [DefaultFirePercent] >= 0 AND [DefaultMarginPercent] >= 0),
        CONSTRAINT [CK_CostSettings_Rates] CHECK ([LaborCostPerMinute] >= 0 AND [EnergyCostPerMinute] >= 0)
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102110059_AddSubcontractingAndQuotations'
)
BEGIN
    CREATE TABLE [Konfeksiyon].[Quotations] (
        [Id] int NOT NULL IDENTITY,
        [QuotationNo] nvarchar(30) NOT NULL,
        [CustomerId] int NULL,
        [ProspectName] nvarchar(200) NULL,
        [QuotationDate] date NOT NULL,
        [ValidUntil] date NOT NULL,
        [Currency] char(3) NOT NULL,
        [DocumentLanguage] nvarchar(30) NOT NULL,
        [Status] nvarchar(30) NOT NULL,
        [Notes] nvarchar(2000) NULL,
        [LaborCostPerMinute] decimal(18,4) NOT NULL,
        [EnergyCostPerMinute] decimal(18,4) NOT NULL,
        [SentAt] datetime2 NULL,
        [DecidedAt] datetime2 NULL,
        [DecidedBy] nvarchar(100) NULL,
        [SalesOrderId] int NULL,
        [RowVersion] rowversion NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_Quotations] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Quotations_DocumentLanguage] CHECK ([DocumentLanguage] IN ('TURKISH', 'ENGLISH')),
        CONSTRAINT [CK_Quotations_Party] CHECK ([CustomerId] IS NOT NULL OR [ProspectName] IS NOT NULL),
        CONSTRAINT [CK_Quotations_Status] CHECK ([Status] IN ('DRAFT', 'SENT', 'ACCEPTED', 'REJECTED', 'EXPIRED')),
        CONSTRAINT [CK_Quotations_Validity] CHECK ([ValidUntil] >= [QuotationDate]),
        CONSTRAINT [FK_Quotations_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Konfeksiyon].[Customers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Quotations_SalesOrders_SalesOrderId] FOREIGN KEY ([SalesOrderId]) REFERENCES [Konfeksiyon].[SalesOrders] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102110059_AddSubcontractingAndQuotations'
)
BEGIN
    CREATE TABLE [Konfeksiyon].[SubcontractPrices] (
        [Id] int NOT NULL IDENTITY,
        [SupplierId] int NOT NULL,
        [OperationId] int NOT NULL,
        [ProductId] int NULL,
        [UnitPrice] decimal(18,4) NOT NULL,
        [Currency] char(3) NOT NULL,
        [ValidFrom] date NOT NULL,
        [Notes] nvarchar(500) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_SubcontractPrices] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_SubcontractPrices_UnitPrice] CHECK ([UnitPrice] >= 0),
        CONSTRAINT [FK_SubcontractPrices_Operations_OperationId] FOREIGN KEY ([OperationId]) REFERENCES [Konfeksiyon].[Operations] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_SubcontractPrices_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Konfeksiyon].[Products] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_SubcontractPrices_Suppliers_SupplierId] FOREIGN KEY ([SupplierId]) REFERENCES [Konfeksiyon].[Suppliers] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102110059_AddSubcontractingAndQuotations'
)
BEGIN
    CREATE TABLE [Konfeksiyon].[SubcontractSends] (
        [Id] int NOT NULL IDENTITY,
        [SendNo] nvarchar(30) NOT NULL,
        [SupplierId] int NOT NULL,
        [SendDate] date NOT NULL,
        [OperationId] int NOT NULL,
        [Status] nvarchar(30) NOT NULL,
        [Carrier] nvarchar(200) NULL,
        [Notes] nvarchar(1000) NULL,
        [RowVersion] rowversion NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_SubcontractSends] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_SubcontractSends_Status] CHECK ([Status] IN ('OPEN', 'PARTIALLY_RETURNED', 'CLOSED', 'CANCELLED')),
        CONSTRAINT [FK_SubcontractSends_Operations_OperationId] FOREIGN KEY ([OperationId]) REFERENCES [Konfeksiyon].[Operations] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_SubcontractSends_Suppliers_SupplierId] FOREIGN KEY ([SupplierId]) REFERENCES [Konfeksiyon].[Suppliers] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102110059_AddSubcontractingAndQuotations'
)
BEGIN
    CREATE TABLE [Konfeksiyon].[QuotationLines] (
        [Id] int NOT NULL IDENTITY,
        [QuotationId] int NOT NULL,
        [LineNo] int NOT NULL,
        [ProductId] int NULL,
        [Description] nvarchar(200) NULL,
        [ModelReference] nvarchar(100) NULL,
        [Colors] nvarchar(200) NULL,
        [Quantity] int NOT NULL,
        [ManualMinutes] decimal(9,4) NULL,
        [ManualSubcontractCost] decimal(18,4) NULL,
        [PriceOverridden] bit NOT NULL,
        [FirePercent] decimal(7,2) NOT NULL,
        [OverheadPercent] decimal(7,2) NOT NULL,
        [MarginPercent] decimal(9,2) NOT NULL,
        [RouteMinutes] decimal(9,4) NULL,
        [RouteSubcontractCost] decimal(18,4) NOT NULL,
        [MaterialCost] decimal(18,4) NOT NULL,
        [LaborMinutes] decimal(9,4) NOT NULL,
        [LaborCost] decimal(18,4) NOT NULL,
        [EnergyCost] decimal(18,4) NOT NULL,
        [SubcontractCost] decimal(18,4) NOT NULL,
        [FireCost] decimal(18,4) NOT NULL,
        [OverheadCost] decimal(18,4) NOT NULL,
        [UnitCost] decimal(18,4) NOT NULL,
        [UnitPrice] decimal(18,4) NOT NULL,
        [Warnings] nvarchar(2000) NULL,
        CONSTRAINT [PK_QuotationLines] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_QuotationLines_Item] CHECK ([ProductId] IS NOT NULL OR [Description] IS NOT NULL),
        CONSTRAINT [CK_QuotationLines_Quantity] CHECK ([Quantity] > 0),
        CONSTRAINT [FK_QuotationLines_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Konfeksiyon].[Products] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_QuotationLines_Quotations_QuotationId] FOREIGN KEY ([QuotationId]) REFERENCES [Konfeksiyon].[Quotations] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102110059_AddSubcontractingAndQuotations'
)
BEGIN
    CREATE TABLE [Konfeksiyon].[SubcontractReturns] (
        [Id] int NOT NULL IDENTITY,
        [ReturnNo] nvarchar(30) NOT NULL,
        [SubcontractSendId] int NOT NULL,
        [SupplierId] int NOT NULL,
        [ReturnDate] date NOT NULL,
        [SupplierDocumentNo] nvarchar(50) NULL,
        [Notes] nvarchar(1000) NULL,
        [PaidDate] date NULL,
        [PaidBy] nvarchar(100) NULL,
        [PaymentNote] nvarchar(500) NULL,
        [RowVersion] rowversion NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_SubcontractReturns] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_SubcontractReturns_SubcontractSends_SubcontractSendId] FOREIGN KEY ([SubcontractSendId]) REFERENCES [Konfeksiyon].[SubcontractSends] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_SubcontractReturns_Suppliers_SupplierId] FOREIGN KEY ([SupplierId]) REFERENCES [Konfeksiyon].[Suppliers] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102110059_AddSubcontractingAndQuotations'
)
BEGIN
    CREATE TABLE [Konfeksiyon].[SubcontractSendLines] (
        [Id] int NOT NULL IDENTITY,
        [SubcontractSendId] int NOT NULL,
        [BundleId] int NULL,
        [BundleOperationId] bigint NULL,
        [ProductionOrderId] int NULL,
        [ProductId] int NOT NULL,
        [ColorId] int NOT NULL,
        [SizeId] int NOT NULL,
        [Quantity] int NOT NULL,
        [ReturnedGood] int NOT NULL,
        [ReturnedDefective] int NOT NULL,
        [ReturnedMissing] int NOT NULL,
        [UnitPrice] decimal(18,4) NULL,
        [Currency] char(3) NOT NULL,
        CONSTRAINT [PK_SubcontractSendLines] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_SubcontractSendLines_Quantity] CHECK ([Quantity] > 0),
        CONSTRAINT [CK_SubcontractSendLines_Returned] CHECK ([ReturnedGood] >= 0 AND [ReturnedDefective] >= 0 AND [ReturnedMissing] >= 0 AND [ReturnedGood] + [ReturnedDefective] + [ReturnedMissing] <= [Quantity]),
        CONSTRAINT [CK_SubcontractSendLines_UnitPrice] CHECK ([UnitPrice] IS NULL OR [UnitPrice] >= 0),
        CONSTRAINT [FK_SubcontractSendLines_BundleOperations_BundleOperationId] FOREIGN KEY ([BundleOperationId]) REFERENCES [Konfeksiyon].[BundleOperations] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_SubcontractSendLines_Bundles_BundleId] FOREIGN KEY ([BundleId]) REFERENCES [Konfeksiyon].[Bundles] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_SubcontractSendLines_Colors_ColorId] FOREIGN KEY ([ColorId]) REFERENCES [Konfeksiyon].[Colors] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_SubcontractSendLines_ProductionOrders_ProductionOrderId] FOREIGN KEY ([ProductionOrderId]) REFERENCES [Konfeksiyon].[ProductionOrders] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_SubcontractSendLines_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Konfeksiyon].[Products] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_SubcontractSendLines_Sizes_SizeId] FOREIGN KEY ([SizeId]) REFERENCES [Konfeksiyon].[Sizes] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_SubcontractSendLines_SubcontractSends_SubcontractSendId] FOREIGN KEY ([SubcontractSendId]) REFERENCES [Konfeksiyon].[SubcontractSends] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102110059_AddSubcontractingAndQuotations'
)
BEGIN
    CREATE TABLE [Konfeksiyon].[QuotationLineMaterials] (
        [Id] int NOT NULL IDENTITY,
        [QuotationLineId] int NOT NULL,
        [SortOrder] int NOT NULL,
        [StockItemId] int NULL,
        [Description] nvarchar(200) NOT NULL,
        [Unit] nvarchar(30) NULL,
        [QuantityPerPiece] decimal(18,4) NOT NULL,
        [UnitCost] decimal(18,4) NOT NULL,
        [UnitCostOverridden] bit NOT NULL,
        CONSTRAINT [PK_QuotationLineMaterials] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_QuotationLineMaterials_Values] CHECK ([QuantityPerPiece] >= 0 AND [UnitCost] >= 0),
        CONSTRAINT [FK_QuotationLineMaterials_QuotationLines_QuotationLineId] FOREIGN KEY ([QuotationLineId]) REFERENCES [Konfeksiyon].[QuotationLines] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_QuotationLineMaterials_StockItems_StockItemId] FOREIGN KEY ([StockItemId]) REFERENCES [Konfeksiyon].[StockItems] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102110059_AddSubcontractingAndQuotations'
)
BEGIN
    CREATE TABLE [Konfeksiyon].[SubcontractReturnLines] (
        [Id] int NOT NULL IDENTITY,
        [SubcontractReturnId] int NOT NULL,
        [SubcontractSendLineId] int NOT NULL,
        [GoodQuantity] int NOT NULL,
        [DefectiveQuantity] int NOT NULL,
        [MissingQuantity] int NOT NULL,
        [DefectTypeId] int NULL,
        [UnitPrice] decimal(18,4) NULL,
        [Currency] char(3) NOT NULL,
        CONSTRAINT [PK_SubcontractReturnLines] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_SubcontractReturnLines_Quantities] CHECK ([GoodQuantity] >= 0 AND [DefectiveQuantity] >= 0 AND [MissingQuantity] >= 0 AND [GoodQuantity] + [DefectiveQuantity] + [MissingQuantity] > 0),
        CONSTRAINT [CK_SubcontractReturnLines_UnitPrice] CHECK ([UnitPrice] IS NULL OR [UnitPrice] >= 0),
        CONSTRAINT [FK_SubcontractReturnLines_DefectTypes_DefectTypeId] FOREIGN KEY ([DefectTypeId]) REFERENCES [Konfeksiyon].[DefectTypes] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_SubcontractReturnLines_SubcontractReturns_SubcontractReturnId] FOREIGN KEY ([SubcontractReturnId]) REFERENCES [Konfeksiyon].[SubcontractReturns] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_SubcontractReturnLines_SubcontractSendLines_SubcontractSendLineId] FOREIGN KEY ([SubcontractSendLineId]) REFERENCES [Konfeksiyon].[SubcontractSendLines] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102110059_AddSubcontractingAndQuotations'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CreatedAt', N'CreatedBy', N'Currency', N'DefaultFirePercent', N'DefaultMarginPercent', N'EnergyCostPerMinute', N'LaborCostPerMinute', N'OverheadPercent', N'UpdatedAt', N'UpdatedBy') AND [object_id] = OBJECT_ID(N'[Konfeksiyon].[CostSettings]'))
        SET IDENTITY_INSERT [Konfeksiyon].[CostSettings] ON;
    EXEC(N'INSERT INTO [Konfeksiyon].[CostSettings] ([Id], [CreatedAt], [CreatedBy], [Currency], [DefaultFirePercent], [DefaultMarginPercent], [EnergyCostPerMinute], [LaborCostPerMinute], [OverheadPercent], [UpdatedAt], [UpdatedBy])
    VALUES (1, ''2026-01-01T00:00:00.0000000Z'', N''system'', ''TRY'', 0.0, 0.0, 0.0, 0.0, 0.0, NULL, NULL)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CreatedAt', N'CreatedBy', N'Currency', N'DefaultFirePercent', N'DefaultMarginPercent', N'EnergyCostPerMinute', N'LaborCostPerMinute', N'OverheadPercent', N'UpdatedAt', N'UpdatedBy') AND [object_id] = OBJECT_ID(N'[Konfeksiyon].[CostSettings]'))
        SET IDENTITY_INSERT [Konfeksiyon].[CostSettings] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102110059_AddSubcontractingAndQuotations'
)
BEGIN
    CREATE INDEX [IX_ProductionRouteOperations_SubcontractorId] ON [Konfeksiyon].[ProductionRouteOperations] ([SubcontractorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102110059_AddSubcontractingAndQuotations'
)
BEGIN
    CREATE INDEX [IX_QuotationLineMaterials_QuotationLineId] ON [Konfeksiyon].[QuotationLineMaterials] ([QuotationLineId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102110059_AddSubcontractingAndQuotations'
)
BEGIN
    CREATE INDEX [IX_QuotationLineMaterials_StockItemId] ON [Konfeksiyon].[QuotationLineMaterials] ([StockItemId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102110059_AddSubcontractingAndQuotations'
)
BEGIN
    CREATE INDEX [IX_QuotationLines_ProductId] ON [Konfeksiyon].[QuotationLines] ([ProductId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102110059_AddSubcontractingAndQuotations'
)
BEGIN
    CREATE UNIQUE INDEX [IX_QuotationLines_QuotationId_LineNo] ON [Konfeksiyon].[QuotationLines] ([QuotationId], [LineNo]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102110059_AddSubcontractingAndQuotations'
)
BEGIN
    CREATE INDEX [IX_Quotations_CustomerId_QuotationDate] ON [Konfeksiyon].[Quotations] ([CustomerId], [QuotationDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102110059_AddSubcontractingAndQuotations'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Quotations_QuotationNo] ON [Konfeksiyon].[Quotations] ([QuotationNo]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102110059_AddSubcontractingAndQuotations'
)
BEGIN
    CREATE INDEX [IX_Quotations_SalesOrderId] ON [Konfeksiyon].[Quotations] ([SalesOrderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102110059_AddSubcontractingAndQuotations'
)
BEGIN
    CREATE INDEX [IX_Quotations_Status] ON [Konfeksiyon].[Quotations] ([Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102110059_AddSubcontractingAndQuotations'
)
BEGIN
    CREATE INDEX [IX_SubcontractPrices_OperationId] ON [Konfeksiyon].[SubcontractPrices] ([OperationId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102110059_AddSubcontractingAndQuotations'
)
BEGIN
    CREATE INDEX [IX_SubcontractPrices_ProductId] ON [Konfeksiyon].[SubcontractPrices] ([ProductId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102110059_AddSubcontractingAndQuotations'
)
BEGIN
    CREATE UNIQUE INDEX [IX_SubcontractPrices_SupplierId_OperationId_ProductId_ValidFrom] ON [Konfeksiyon].[SubcontractPrices] ([SupplierId], [OperationId], [ProductId], [ValidFrom]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102110059_AddSubcontractingAndQuotations'
)
BEGIN
    CREATE INDEX [IX_SubcontractReturnLines_DefectTypeId] ON [Konfeksiyon].[SubcontractReturnLines] ([DefectTypeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102110059_AddSubcontractingAndQuotations'
)
BEGIN
    CREATE INDEX [IX_SubcontractReturnLines_SubcontractReturnId] ON [Konfeksiyon].[SubcontractReturnLines] ([SubcontractReturnId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102110059_AddSubcontractingAndQuotations'
)
BEGIN
    CREATE INDEX [IX_SubcontractReturnLines_SubcontractSendLineId] ON [Konfeksiyon].[SubcontractReturnLines] ([SubcontractSendLineId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102110059_AddSubcontractingAndQuotations'
)
BEGIN
    CREATE INDEX [IX_SubcontractReturns_ReturnDate] ON [Konfeksiyon].[SubcontractReturns] ([ReturnDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102110059_AddSubcontractingAndQuotations'
)
BEGIN
    CREATE UNIQUE INDEX [IX_SubcontractReturns_ReturnNo] ON [Konfeksiyon].[SubcontractReturns] ([ReturnNo]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102110059_AddSubcontractingAndQuotations'
)
BEGIN
    CREATE INDEX [IX_SubcontractReturns_SubcontractSendId] ON [Konfeksiyon].[SubcontractReturns] ([SubcontractSendId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102110059_AddSubcontractingAndQuotations'
)
BEGIN
    CREATE INDEX [IX_SubcontractReturns_SupplierId_ReturnDate] ON [Konfeksiyon].[SubcontractReturns] ([SupplierId], [ReturnDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102110059_AddSubcontractingAndQuotations'
)
BEGIN
    CREATE INDEX [IX_SubcontractSendLines_BundleId] ON [Konfeksiyon].[SubcontractSendLines] ([BundleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102110059_AddSubcontractingAndQuotations'
)
BEGIN
    CREATE INDEX [IX_SubcontractSendLines_BundleOperationId] ON [Konfeksiyon].[SubcontractSendLines] ([BundleOperationId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102110059_AddSubcontractingAndQuotations'
)
BEGIN
    CREATE INDEX [IX_SubcontractSendLines_ColorId] ON [Konfeksiyon].[SubcontractSendLines] ([ColorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102110059_AddSubcontractingAndQuotations'
)
BEGIN
    CREATE INDEX [IX_SubcontractSendLines_ProductId] ON [Konfeksiyon].[SubcontractSendLines] ([ProductId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102110059_AddSubcontractingAndQuotations'
)
BEGIN
    CREATE INDEX [IX_SubcontractSendLines_ProductionOrderId] ON [Konfeksiyon].[SubcontractSendLines] ([ProductionOrderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102110059_AddSubcontractingAndQuotations'
)
BEGIN
    CREATE INDEX [IX_SubcontractSendLines_SizeId] ON [Konfeksiyon].[SubcontractSendLines] ([SizeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102110059_AddSubcontractingAndQuotations'
)
BEGIN
    CREATE INDEX [IX_SubcontractSendLines_SubcontractSendId] ON [Konfeksiyon].[SubcontractSendLines] ([SubcontractSendId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102110059_AddSubcontractingAndQuotations'
)
BEGIN
    CREATE INDEX [IX_SubcontractSends_OperationId] ON [Konfeksiyon].[SubcontractSends] ([OperationId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102110059_AddSubcontractingAndQuotations'
)
BEGIN
    CREATE UNIQUE INDEX [IX_SubcontractSends_SendNo] ON [Konfeksiyon].[SubcontractSends] ([SendNo]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102110059_AddSubcontractingAndQuotations'
)
BEGIN
    CREATE INDEX [IX_SubcontractSends_Status] ON [Konfeksiyon].[SubcontractSends] ([Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102110059_AddSubcontractingAndQuotations'
)
BEGIN
    CREATE INDEX [IX_SubcontractSends_SupplierId_Status] ON [Konfeksiyon].[SubcontractSends] ([SupplierId], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102110059_AddSubcontractingAndQuotations'
)
BEGIN
    ALTER TABLE [Konfeksiyon].[ProductionRouteOperations] ADD CONSTRAINT [FK_ProductionRouteOperations_Suppliers_SubcontractorId] FOREIGN KEY ([SubcontractorId]) REFERENCES [Konfeksiyon].[Suppliers] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102110059_AddSubcontractingAndQuotations'
)
BEGIN
    SET IDENTITY_INSERT [Core].[Permissions] ON;
    EXEC(N'INSERT INTO [Core].[Permissions] ([Id], [Code], [Module], [Description])
    SELECT v.[Id], v.[Code], v.[Module], v.[Description] FROM (VALUES
    (99, N''Quotation.View'', N''Quotation'', N''Teklifleri ve ön maliyet dökümünü görüntüleme''),
    (100, N''Quotation.Manage'', N''Quotation'', N''Teklif hazırlama, gönderme, kabul ve ön maliyet parametreleri''),
    (101, N''Subcontract.View'', N''Subcontract'', N''Fason gönderim / dönüş, fason fiyatı ve hakedişleri görüntüleme''),
    (102, N''Subcontract.Manage'', N''Subcontract'', N''Fason gönderim / dönüş fişi, fason fiyatı ve hakediş ödemesi'')
    ) v([Id], [Code], [Module], [Description])
    WHERE NOT EXISTS (SELECT 1 FROM [Core].[Permissions] p WHERE p.[Id] = v.[Id] OR p.[Code] = v.[Code])');
    SET IDENTITY_INSERT [Core].[Permissions] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102110059_AddSubcontractingAndQuotations'
)
BEGIN
    INSERT INTO [Core].[RolePermissions] ([RoleId], [PermissionId])
    SELECT v.[RoleId], v.[PermissionId] FROM (VALUES
    (1, 99), (1, 100), (1, 101), (1, 102)
    ) v([RoleId], [PermissionId])
    WHERE EXISTS (SELECT 1 FROM [Core].[Roles] r WHERE r.[Id] = v.[RoleId])
      AND EXISTS (SELECT 1 FROM [Core].[Permissions] p WHERE p.[Id] = v.[PermissionId])
      AND NOT EXISTS (SELECT 1 FROM [Core].[RolePermissions] rp WHERE rp.[RoleId] = v.[RoleId] AND rp.[PermissionId] = v.[PermissionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102110059_AddSubcontractingAndQuotations'
)
BEGIN
    INSERT INTO [Konfeksiyon].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261102110059_AddSubcontractingAndQuotations', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104084708_AddManualReportAttribution'
)
BEGIN
    DECLARE @var nvarchar(max);
    SELECT @var = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Konfeksiyon].[BundleOperationEvents]') AND [c].[name] = N'OperatorId');
    IF @var IS NOT NULL EXEC(N'ALTER TABLE [Konfeksiyon].[BundleOperationEvents] DROP CONSTRAINT ' + @var + ';');
    ALTER TABLE [Konfeksiyon].[BundleOperationEvents] ALTER COLUMN [OperatorId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104084708_AddManualReportAttribution'
)
BEGIN
    ALTER TABLE [Konfeksiyon].[BundleOperationEvents] ADD [CountsForPerformance] bit NOT NULL DEFAULT CAST(1 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104084708_AddManualReportAttribution'
)
BEGIN
    INSERT INTO [Konfeksiyon].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261104084708_AddManualReportAttribution', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104131219_AddShipmentPickingAndReservations'
)
BEGIN
    ALTER TABLE [Konfeksiyon].[Shipments] DROP CONSTRAINT [CK_Shipments_Status];
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104131219_AddShipmentPickingAndReservations'
)
BEGIN
    DROP INDEX [IX_SalesOrderReservations_SalesOrderLineId_WarehouseId] ON [Konfeksiyon].[SalesOrderReservations];
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104131219_AddShipmentPickingAndReservations'
)
BEGIN
    ALTER TABLE [Konfeksiyon].[Shipments] ADD [DespatchUuid] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104131219_AddShipmentPickingAndReservations'
)
BEGIN
    ALTER TABLE [Konfeksiyon].[Shipments] ADD [DriverIdentityNo] nvarchar(11) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104131219_AddShipmentPickingAndReservations'
)
BEGIN
    ALTER TABLE [Konfeksiyon].[Shipments] ADD [DriverName] nvarchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104131219_AddShipmentPickingAndReservations'
)
BEGIN
    ALTER TABLE [Konfeksiyon].[Shipments] ADD [PickedAt] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104131219_AddShipmentPickingAndReservations'
)
BEGIN
    ALTER TABLE [Konfeksiyon].[Shipments] ADD [PickedBy] nvarchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104131219_AddShipmentPickingAndReservations'
)
BEGIN
    ALTER TABLE [Konfeksiyon].[Shipments] ADD [PickingBy] nvarchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104131219_AddShipmentPickingAndReservations'
)
BEGIN
    ALTER TABLE [Konfeksiyon].[Shipments] ADD [PickingStartedAt] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104131219_AddShipmentPickingAndReservations'
)
BEGIN
    ALTER TABLE [Konfeksiyon].[Shipments] ADD [VehiclePlate] nvarchar(20) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104131219_AddShipmentPickingAndReservations'
)
BEGIN
    ALTER TABLE [Konfeksiyon].[ShipmentItems] ADD [CheckedQuantity] decimal(18,3) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104131219_AddShipmentPickingAndReservations'
)
BEGIN
    ALTER TABLE [Konfeksiyon].[ShipmentItems] ADD [PickedQuantity] decimal(18,3) NOT NULL DEFAULT 0.0;
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104131219_AddShipmentPickingAndReservations'
)
BEGIN
    ALTER TABLE [Konfeksiyon].[ShipmentItems] ADD [RequestedQuantity] decimal(18,3) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104131219_AddShipmentPickingAndReservations'
)
BEGIN
    ALTER TABLE [Konfeksiyon].[SalesOrderReservations] ADD [FromProduction] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104131219_AddShipmentPickingAndReservations'
)
BEGIN
    CREATE TABLE [Konfeksiyon].[ShipmentPicks] (
        [Id] bigint NOT NULL IDENTITY,
        [RequestId] uniqueidentifier NOT NULL,
        [ShipmentId] int NOT NULL,
        [ShipmentItemId] int NOT NULL,
        [LocationId] int NOT NULL,
        [Quantity] decimal(18,3) NOT NULL,
        [PickedAt] datetime2 NOT NULL,
        [PickedBy] nvarchar(100) NOT NULL,
        CONSTRAINT [PK_ShipmentPicks] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_ShipmentPicks_Quantity] CHECK ([Quantity] > 0),
        CONSTRAINT [FK_ShipmentPicks_ShipmentItems_ShipmentItemId] FOREIGN KEY ([ShipmentItemId]) REFERENCES [Konfeksiyon].[ShipmentItems] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ShipmentPicks_Shipments_ShipmentId] FOREIGN KEY ([ShipmentId]) REFERENCES [Konfeksiyon].[Shipments] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ShipmentPicks_WarehouseLocations_LocationId] FOREIGN KEY ([LocationId]) REFERENCES [Konfeksiyon].[WarehouseLocations] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104131219_AddShipmentPickingAndReservations'
)
BEGIN
    EXEC(N'ALTER TABLE [Konfeksiyon].[Shipments] ADD CONSTRAINT [CK_Shipments_Status] CHECK ([Status] IN (''DRAFT'', ''PICKING'', ''PICKED'', ''SHIPPED'', ''CANCELLED''))');
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104131219_AddShipmentPickingAndReservations'
)
BEGIN
    CREATE UNIQUE INDEX [IX_SalesOrderReservations_SalesOrderLineId_WarehouseId_FromProduction] ON [Konfeksiyon].[SalesOrderReservations] ([SalesOrderLineId], [WarehouseId], [FromProduction]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104131219_AddShipmentPickingAndReservations'
)
BEGIN
    CREATE INDEX [IX_ShipmentPicks_LocationId] ON [Konfeksiyon].[ShipmentPicks] ([LocationId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104131219_AddShipmentPickingAndReservations'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ShipmentPicks_RequestId] ON [Konfeksiyon].[ShipmentPicks] ([RequestId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104131219_AddShipmentPickingAndReservations'
)
BEGIN
    CREATE INDEX [IX_ShipmentPicks_ShipmentId] ON [Konfeksiyon].[ShipmentPicks] ([ShipmentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104131219_AddShipmentPickingAndReservations'
)
BEGIN
    CREATE INDEX [IX_ShipmentPicks_ShipmentItemId] ON [Konfeksiyon].[ShipmentPicks] ([ShipmentItemId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Konfeksiyon].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104131219_AddShipmentPickingAndReservations'
)
BEGIN
    INSERT INTO [Konfeksiyon].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261104131219_AddShipmentPickingAndReservations', N'10.0.12');
END;

COMMIT;
GO

