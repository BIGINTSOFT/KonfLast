/*
    Tekstil ERP - proje ayrımı: Konfeksiyon'a özgü tanımları Core'dan Konfeksiyon şemasına taşır.

    Ne yapar:
      - Müşteri, tedarikçi, model, ürün, renk, beden, fiyat listesi ve iskonto tablolarını [Core]'dan
        [Konfeksiyon]'a taşır (ALTER SCHEMA ... TRANSFER: veri, index ve foreign key'ler olduğu gibi taşınır).
      - [Core].[Roles] tablosuna ModuleKey sütununu ekler; Sistem Yöneticisi ve Üst Yönetim dışındaki sistem
        rollerini Konfeksiyon'a bağlar. Rollerin yetkileri değişmez.

    Yalnızca bu değişiklikten önce kurulmuş veritabanında çalıştırılır; yeni kurulumda gerekmez (kurulum
    script'leri tabloları zaten doğru şemada oluşturur). Tamamı tek transaction'dır: hata olursa hiçbir şey
    değişmez. Tekrar çalıştırılabilir: taşınmış tablolar atlanır. Önce mutlaka yedek alın.

    sqlcmd ile:  sqlcmd -S "(localdb)\MSSQLLocalDB" -d TekstilERP -E -b -f 65001 -i database\upgrade\TekstilERP-proje-ayrimi.sql
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

BEGIN TRANSACTION;

IF SCHEMA_ID(N'Konfeksiyon') IS NULL EXEC(N'CREATE SCHEMA [Konfeksiyon];');

DECLARE @table sysname, @sql nvarchar(400);
DECLARE tables_cursor CURSOR LOCAL FAST_FORWARD FOR
    SELECT name FROM (VALUES
        (N'Colors'), (N'CustomerAddresses'), (N'CustomerBuyers'), (N'CustomerDiscounts'), (N'Customers'), (N'Models'),
        (N'PriceListItems'), (N'PriceLists'), (N'ProductGroups'), (N'Products'), (N'Sizes'), (N'Suppliers')) AS t(name);
OPEN tables_cursor;
FETCH NEXT FROM tables_cursor INTO @table;
WHILE @@FETCH_STATUS = 0
BEGIN
    IF OBJECT_ID(N'[Core].' + QUOTENAME(@table), N'U') IS NOT NULL
    BEGIN
        SET @sql = N'ALTER SCHEMA [Konfeksiyon] TRANSFER [Core].' + QUOTENAME(@table) + N';';
        EXEC sys.sp_executesql @sql;
        PRINT N'Taşındı: ' + @table;
    END;
    FETCH NEXT FROM tables_cursor INTO @table;
END;
CLOSE tables_cursor;
DEALLOCATE tables_cursor;

IF COL_LENGTH(N'[Core].[Roles]', N'ModuleKey') IS NULL
BEGIN
    EXEC(N'ALTER TABLE [Core].[Roles] ADD [ModuleKey] nvarchar(50) NULL;');
    EXEC(N'UPDATE [Core].[Roles] SET [ModuleKey] = N''Konfeksiyon'' WHERE [Id] IN (2, 3, 4, 5, 6, 7, 8, 10);');
    PRINT N'Roller projelere bağlandı.';
END;

COMMIT TRANSACTION;
GO
