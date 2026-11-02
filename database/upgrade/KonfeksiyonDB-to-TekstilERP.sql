/*
    Tekstil ERP - KonfeksiyonDB (eski tek parça yapı, dbo) -> modüler yapı (Core + Konfeksiyon şemaları)

    Ne yapar:
      - [dbo] altındaki 27 çekirdek tabloyu [Core], 63 Konfeksiyon tablosunu [Konfeksiyon] şemasına taşır
        (ALTER SCHEMA ... TRANSFER: veri, index, foreign key, constraint ve trigger'lar olduğu gibi taşınır).
      - Her şemanın migration geçmişini oluşturur ve yeni temel migration'ları (InitialCore,
        InitialKonfeksiyon) uygulanmış olarak işaretler.
      - Eski geçmiş tablosunu [dbo].[__EFMigrationsHistory_KonfeksiyonDB] adıyla saklar.

    Ön koşul: Veritabanı eski zincirin son migration'ına (20261022221221_SplitSensitivePermissions) kadar
    güncel olmalıdır (eski database\upgrade\legacy\KonfeksiyonDB.sql ile). Tamamı tek transaction'dır; hata olursa hiçbir şey
    değişmez. Zaten yeni yapıdaysa hiçbir şey yapmaz.

    Sonrasında 00_Core.sql ve 10_Konfeksiyon.sql çalıştırılır (yeni migration'lar varsa uygular, yoksa atlar).
    Önce mutlaka yedek alın; database\upgrade\upgrade-from-KonfeksiyonDB.ps1 bunu sizin yerinize yapar.

    Bu dosya otomatik üretilmiştir.
*/

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;
SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

IF OBJECT_ID(N'[Core].[__EFMigrationsHistory]') IS NOT NULL
BEGIN
    PRINT N'Veritabanı zaten modüler yapıda; taşıma yapılmadı.';
END
ELSE
BEGIN
    IF OBJECT_ID(N'[dbo].[__EFMigrationsHistory]') IS NULL
        OR NOT EXISTS (SELECT 1 FROM [dbo].[__EFMigrationsHistory] WHERE [MigrationId] = N'20261022221221_SplitSensitivePermissions')
        THROW 50000, N'Bu script son sürümdeki KonfeksiyonDB yapısını taşır. Önce database\upgrade\legacy\KonfeksiyonDB.sql ile son sürüme getirin.', 1;

    BEGIN TRANSACTION;

    IF SCHEMA_ID(N'Core') IS NULL EXEC(N'CREATE SCHEMA [Core];');
    IF SCHEMA_ID(N'Konfeksiyon') IS NULL EXEC(N'CREATE SCHEMA [Konfeksiyon];');

    -- Çekirdek tablolar
    ALTER SCHEMA [Core] TRANSFER [dbo].[AuditLogs];
    ALTER SCHEMA [Core] TRANSFER [dbo].[Colors];
    ALTER SCHEMA [Core] TRANSFER [dbo].[Currencies];
    ALTER SCHEMA [Core] TRANSFER [dbo].[CustomerAddresses];
    ALTER SCHEMA [Core] TRANSFER [dbo].[CustomerBuyers];
    ALTER SCHEMA [Core] TRANSFER [dbo].[CustomerDiscounts];
    ALTER SCHEMA [Core] TRANSFER [dbo].[Customers];
    ALTER SCHEMA [Core] TRANSFER [dbo].[DocumentSequences];
    ALTER SCHEMA [Core] TRANSFER [dbo].[ExchangeRates];
    ALTER SCHEMA [Core] TRANSFER [dbo].[IntegrationDeliveries];
    ALTER SCHEMA [Core] TRANSFER [dbo].[IntegrationMessages];
    ALTER SCHEMA [Core] TRANSFER [dbo].[Models];
    ALTER SCHEMA [Core] TRANSFER [dbo].[PasswordResetTokens];
    ALTER SCHEMA [Core] TRANSFER [dbo].[Permissions];
    ALTER SCHEMA [Core] TRANSFER [dbo].[PriceListItems];
    ALTER SCHEMA [Core] TRANSFER [dbo].[PriceLists];
    ALTER SCHEMA [Core] TRANSFER [dbo].[ProductGroups];
    ALTER SCHEMA [Core] TRANSFER [dbo].[Products];
    ALTER SCHEMA [Core] TRANSFER [dbo].[RefreshTokens];
    ALTER SCHEMA [Core] TRANSFER [dbo].[RolePermissions];
    ALTER SCHEMA [Core] TRANSFER [dbo].[Roles];
    ALTER SCHEMA [Core] TRANSFER [dbo].[Sizes];
    ALTER SCHEMA [Core] TRANSFER [dbo].[Suppliers];
    ALTER SCHEMA [Core] TRANSFER [dbo].[UserFormDrafts];
    ALTER SCHEMA [Core] TRANSFER [dbo].[UserRoles];
    ALTER SCHEMA [Core] TRANSFER [dbo].[UserShortcuts];
    ALTER SCHEMA [Core] TRANSFER [dbo].[Users];

    -- Konfeksiyon tabloları
    ALTER SCHEMA [Konfeksiyon] TRANSFER [dbo].[ApprovalFlowStepUsers];
    ALTER SCHEMA [Konfeksiyon] TRANSFER [dbo].[ApprovalFlowSteps];
    ALTER SCHEMA [Konfeksiyon] TRANSFER [dbo].[ApprovalFlows];
    ALTER SCHEMA [Konfeksiyon] TRANSFER [dbo].[BreakGroups];
    ALTER SCHEMA [Konfeksiyon] TRANSFER [dbo].[BreakSchedules];
    ALTER SCHEMA [Konfeksiyon] TRANSFER [dbo].[BundleCardAssignments];
    ALTER SCHEMA [Konfeksiyon] TRANSFER [dbo].[BundleOperationEvents];
    ALTER SCHEMA [Konfeksiyon] TRANSFER [dbo].[BundleOperations];
    ALTER SCHEMA [Konfeksiyon] TRANSFER [dbo].[Bundles];
    ALTER SCHEMA [Konfeksiyon] TRANSFER [dbo].[DefectTypes];
    ALTER SCHEMA [Konfeksiyon] TRANSFER [dbo].[Factories];
    ALTER SCHEMA [Konfeksiyon] TRANSFER [dbo].[GoodsReceiptLines];
    ALTER SCHEMA [Konfeksiyon] TRANSFER [dbo].[GoodsReceipts];
    ALTER SCHEMA [Konfeksiyon] TRANSFER [dbo].[LineMachineAssignments];
    ALTER SCHEMA [Konfeksiyon] TRANSFER [dbo].[LineProductionPlans];
    ALTER SCHEMA [Konfeksiyon] TRANSFER [dbo].[MachineOperations];
    ALTER SCHEMA [Konfeksiyon] TRANSFER [dbo].[MachineStatusLogs];
    ALTER SCHEMA [Konfeksiyon] TRANSFER [dbo].[MachineTypes];
    ALTER SCHEMA [Konfeksiyon] TRANSFER [dbo].[Machines];
    ALTER SCHEMA [Konfeksiyon] TRANSFER [dbo].[OperationDependencies];
    ALTER SCHEMA [Konfeksiyon] TRANSFER [dbo].[Operations];
    ALTER SCHEMA [Konfeksiyon] TRANSFER [dbo].[OperatorCards];
    ALTER SCHEMA [Konfeksiyon] TRANSFER [dbo].[OperatorMachines];
    ALTER SCHEMA [Konfeksiyon] TRANSFER [dbo].[OperatorOperationQualifications];
    ALTER SCHEMA [Konfeksiyon] TRANSFER [dbo].[OperatorPhotos];
    ALTER SCHEMA [Konfeksiyon] TRANSFER [dbo].[Operators];
    ALTER SCHEMA [Konfeksiyon] TRANSFER [dbo].[PayrollBonusTiers];
    ALTER SCHEMA [Konfeksiyon] TRANSFER [dbo].[PayrollSettings];
    ALTER SCHEMA [Konfeksiyon] TRANSFER [dbo].[ProductMaterials];
    ALTER SCHEMA [Konfeksiyon] TRANSFER [dbo].[ProductionAreas];
    ALTER SCHEMA [Konfeksiyon] TRANSFER [dbo].[ProductionLines];
    ALTER SCHEMA [Konfeksiyon] TRANSFER [dbo].[ProductionOrderItems];
    ALTER SCHEMA [Konfeksiyon] TRANSFER [dbo].[ProductionOrders];
    ALTER SCHEMA [Konfeksiyon] TRANSFER [dbo].[ProductionRouteOperations];
    ALTER SCHEMA [Konfeksiyon] TRANSFER [dbo].[ProductionRoutes];
    ALTER SCHEMA [Konfeksiyon] TRANSFER [dbo].[PurchaseOrderItems];
    ALTER SCHEMA [Konfeksiyon] TRANSFER [dbo].[PurchaseOrders];
    ALTER SCHEMA [Konfeksiyon] TRANSFER [dbo].[QualityInspections];
    ALTER SCHEMA [Konfeksiyon] TRANSFER [dbo].[QualityRecords];
    ALTER SCHEMA [Konfeksiyon] TRANSFER [dbo].[ReworkTasks];
    ALTER SCHEMA [Konfeksiyon] TRANSFER [dbo].[RfidCards];
    ALTER SCHEMA [Konfeksiyon] TRANSFER [dbo].[RfidReadLogs];
    ALTER SCHEMA [Konfeksiyon] TRANSFER [dbo].[SalesOrderApprovalEvents];
    ALTER SCHEMA [Konfeksiyon] TRANSFER [dbo].[SalesOrderApprovalSteps];
    ALTER SCHEMA [Konfeksiyon] TRANSFER [dbo].[SalesOrderLines];
    ALTER SCHEMA [Konfeksiyon] TRANSFER [dbo].[SalesOrderReservations];
    ALTER SCHEMA [Konfeksiyon] TRANSFER [dbo].[SalesOrders];
    ALTER SCHEMA [Konfeksiyon] TRANSFER [dbo].[Shifts];
    ALTER SCHEMA [Konfeksiyon] TRANSFER [dbo].[ShipmentItems];
    ALTER SCHEMA [Konfeksiyon] TRANSFER [dbo].[Shipments];
    ALTER SCHEMA [Konfeksiyon] TRANSFER [dbo].[StockCountLines];
    ALTER SCHEMA [Konfeksiyon] TRANSFER [dbo].[StockCounts];
    ALTER SCHEMA [Konfeksiyon] TRANSFER [dbo].[StockItems];
    ALTER SCHEMA [Konfeksiyon] TRANSFER [dbo].[StockLots];
    ALTER SCHEMA [Konfeksiyon] TRANSFER [dbo].[StockReservations];
    ALTER SCHEMA [Konfeksiyon] TRANSFER [dbo].[StockTransactions];
    ALTER SCHEMA [Konfeksiyon] TRANSFER [dbo].[Stocks];
    ALTER SCHEMA [Konfeksiyon] TRANSFER [dbo].[StopReasons];
    ALTER SCHEMA [Konfeksiyon] TRANSFER [dbo].[Tablets];
    ALTER SCHEMA [Konfeksiyon] TRANSFER [dbo].[WarehouseLocations];
    ALTER SCHEMA [Konfeksiyon] TRANSFER [dbo].[Warehouses];
    ALTER SCHEMA [Konfeksiyon] TRANSFER [dbo].[WorkSessionPauses];
    ALTER SCHEMA [Konfeksiyon] TRANSFER [dbo].[WorkSessions];

    EXEC(N'CREATE TABLE [Core].[__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId]));
    INSERT INTO [Core].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N''20261030064926_InitialCore'', N''10.0.12'');');

    EXEC(N'CREATE TABLE [Konfeksiyon].[__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId]));
    INSERT INTO [Konfeksiyon].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N''20261030065037_InitialKonfeksiyon'', N''10.0.12'');');

    EXEC sp_rename N'[dbo].[__EFMigrationsHistory]', N'__EFMigrationsHistory_KonfeksiyonDB';

    COMMIT TRANSACTION;
    PRINT N'Taşıma tamamlandı: 27 tablo [Core], 63 tablo [Konfeksiyon] şemasında.';
END
GO
