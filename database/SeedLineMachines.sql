/*
    Tekstil ERP - Konfeksiyon bant makineleri (örnek veri)

    Plan önerisinin bir bandı uygun sayması için modelin etkin rotasındaki her operasyonu yapacak,
    bantta takılı ve arızada / bakımda olmayan en az bir makine gerekir. Bu script, örnek bantlara
    eksik makineleri ekleyip atar:
        BANT-01 -> SW-001 (sweatshirt) ve TS-001 (tişört) rotaları
        BANT-02 -> PL-001 (polo)       rotası
        BANT-03 -> PN-001 (pantolon)   rotası

    SeedData.sql'den sonra çalıştırılır. Makine kodu ve bant ataması varsa atlanır; script tekrar
    çalıştırılabilir. Kimlikler kodlardan bulunur (operasyon, makine tipi, bant).

    sqlcmd ile:  sqlcmd -S "(localdb)\MSSQLLocalDB" -d TekstilERP -E -b -f 65001 -i SeedLineMachines.sql
*/

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

BEGIN TRANSACTION;

DECLARE @now datetime2 = SYSUTCDATETIME();

DECLARE @machines TABLE (
    LineCode nvarchar(50), MachineCode nvarchar(50), MachineName nvarchar(200), Brand nvarchar(100),
    TypeCode nvarchar(50), OperationCode nvarchar(50));

INSERT INTO @machines VALUES
    -- BANT-01: SW-001 için eksikler (kapüşon, temizleme, ütü, paket)
    (N'BANT-01', N'OV-09', N'Pegasus M952 Overlok',             N'Pegasus', N'OVERLOK', N'OP97'),
    (N'BANT-01', N'MN-01', N'Temizleme ve Kontrol Masası',      NULL,       N'MANUEL',  N'OP70'),
    (N'BANT-01', N'UT-03', N'Ramsey Vakumlu Ütü Masası',        N'Ramsey',  N'UTU',     N'OP75'),
    (N'BANT-01', N'MN-02', N'Paketleme Masası',                 NULL,       N'MANUEL',  N'OP80'),
    -- BANT-01: TS-001 için eksikler (ön cep, ütü paket)
    (N'BANT-01', N'DD-13', N'Juki DDL-9000C Düz Dikiş',         N'Juki',    N'DUZ',     N'OP05'),
    (N'BANT-01', N'UT-06', N'Ramsey Vakumlu Ütü Masası',        N'Ramsey',  N'UTU',     N'OP30'),
    -- BANT-02: PL-001 için eksikler (patlet, kol ağzı reçme, temizleme, ütü, paket)
    (N'BANT-02', N'DD-09', N'Juki DDL-9000C Düz Dikiş',         N'Juki',    N'DUZ',     N'OP55'),
    (N'BANT-02', N'RC-05', N'Yamato VC2700 Reçme',              N'Yamato',  N'RECME',   N'OP40'),
    (N'BANT-02', N'MN-03', N'Temizleme ve Kontrol Masası',      NULL,       N'MANUEL',  N'OP70'),
    (N'BANT-02', N'UT-04', N'Ramsey Vakumlu Ütü Masası',        N'Ramsey',  N'UTU',     N'OP75'),
    (N'BANT-02', N'MN-04', N'Paketleme Masası',                 NULL,       N'MANUEL',  N'OP80'),
    -- BANT-03: PN-001 için eksikler
    (N'BANT-03', N'DD-10', N'Juki DDL-9000C Düz Dikiş',         N'Juki',    N'DUZ',     N'OP05'),
    (N'BANT-03', N'DD-11', N'Juki DDL-9000C Düz Dikiş',         N'Juki',    N'DUZ',     N'OP95'),
    (N'BANT-03', N'OV-10', N'Pegasus M952 Overlok',             N'Pegasus', N'OVERLOK', N'OP25'),
    (N'BANT-03', N'DD-12', N'Juki DDL-9000C Düz Dikiş',         N'Juki',    N'DUZ',     N'OP90'),
    (N'BANT-03', N'IL-02', N'Brother HE-800C İlik',             N'Brother', N'ILIK',    N'OP60'),
    (N'BANT-03', N'DG-02', N'Brother BE-438HX Düğme',           N'Brother', N'DUGME',   N'OP65'),
    (N'BANT-03', N'MN-05', N'Temizleme ve Kontrol Masası',      NULL,       N'MANUEL',  N'OP70'),
    (N'BANT-03', N'UT-05', N'Ramsey Vakumlu Ütü Masası',        N'Ramsey',  N'UTU',     N'OP75'),
    (N'BANT-03', N'MN-06', N'Paketleme Masası',                 NULL,       N'MANUEL',  N'OP80');

-- "Ütü Paket" (OP30) operasyonunun makine tipi tanımlı değilse ütü masası olarak işaretlenir; makinesi bu tipte eklenir.
UPDATE [Konfeksiyon].[Operations]
SET [MachineTypeId] = (SELECT [Id] FROM [Konfeksiyon].[MachineTypes] WHERE [Code] = N'UTU' AND [IsDeleted] = 0),
    [UpdatedAt] = @now, [UpdatedBy] = N'seed'
WHERE [OperationCode] = N'OP30' AND [MachineTypeId] IS NULL
  AND EXISTS (SELECT 1 FROM [Konfeksiyon].[MachineTypes] WHERE [Code] = N'UTU' AND [IsDeleted] = 0);

-- Önkoşul: bantlar, makine tipleri ve operasyonlar SeedData.sql ile kurulmuş olmalı.
IF EXISTS (
    SELECT 1 FROM @machines m
    WHERE NOT EXISTS (SELECT 1 FROM [Konfeksiyon].[ProductionLines] l WHERE l.[LineCode] = m.LineCode AND l.[IsDeleted] = 0)
       OR NOT EXISTS (SELECT 1 FROM [Konfeksiyon].[MachineTypes] t WHERE t.[Code] = m.TypeCode AND t.[IsDeleted] = 0)
       OR NOT EXISTS (SELECT 1 FROM [Konfeksiyon].[Operations] o WHERE o.[OperationCode] = m.OperationCode AND o.[IsDeleted] = 0))
BEGIN
    ROLLBACK TRANSACTION;
    THROW 50001, N'Bant, makine tipi ya da operasyon tanımı eksik; önce SeedData.sql çalıştırılmalı.', 1;
END;

INSERT INTO [Konfeksiyon].[Machines] ([MachineCode], [MachineName], [Brand], [MachineTypeId], [OperationId], [Status], [IsActive],
    [CreatedAt], [CreatedBy], [IsDeleted])
SELECT m.MachineCode, m.MachineName, m.Brand, t.[Id], o.[Id], N'OFFLINE', 1, @now, N'seed', 0
FROM @machines m
JOIN [Konfeksiyon].[MachineTypes] t ON t.[Code] = m.TypeCode AND t.[IsDeleted] = 0
JOIN [Konfeksiyon].[Operations] o ON o.[OperationCode] = m.OperationCode AND o.[IsDeleted] = 0
WHERE NOT EXISTS (SELECT 1 FROM [Konfeksiyon].[Machines] x WHERE x.[MachineCode] = m.MachineCode);

-- Makine başka bir banda takılı değilse banda, sıranın sonuna atanır.
INSERT INTO [Konfeksiyon].[LineMachineAssignments] ([ProductionLineId], [MachineId], [Position], [AssignedAt], [UnassignedAt])
SELECT l.[Id], mc.[Id],
    ISNULL((SELECT MAX(a.[Position]) FROM [Konfeksiyon].[LineMachineAssignments] a WHERE a.[ProductionLineId] = l.[Id] AND a.[UnassignedAt] IS NULL), 0)
        + ROW_NUMBER() OVER (PARTITION BY l.[Id] ORDER BY m.MachineCode),
    @now, NULL
FROM @machines m
JOIN [Konfeksiyon].[ProductionLines] l ON l.[LineCode] = m.LineCode AND l.[IsDeleted] = 0
JOIN [Konfeksiyon].[Machines] mc ON mc.[MachineCode] = m.MachineCode
WHERE NOT EXISTS (SELECT 1 FROM [Konfeksiyon].[LineMachineAssignments] a WHERE a.[MachineId] = mc.[Id] AND a.[UnassignedAt] IS NULL);

COMMIT TRANSACTION;
GO
