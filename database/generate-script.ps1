# Migration'lardan idempotent kurulum / güncelleme script'lerini üretir (modül başına bir dosya):
#
#   00_Core.sql         Çekirdek (her kurulumda)          -> Core şeması
#   10_Konfeksiyon.sql  Konfeksiyon modülü                -> Konfeksiyon şeması
#   20_Boyahane.sql     Boyahane modülü                   -> Boyahane şeması
#   30_Dokuma.sql       Dokuma modülü                     -> Dokuma şeması
#   40_Orme.sql         Örme modülü                       -> Orme şeması
#
# Her script hem boş veritabanını kurar hem de mevcut kurulumu son sürüme yükseltir: uygulanmış migration'lar
# o şemanın __EFMigrationsHistory tablosundan anlaşılır ve atlanır. Müşteriye yalnızca 00_Core.sql ve lisansladığı
# modüllerin script'leri verilir; önce 00_Core.sql, sonra modüller sırayla çalıştırılır.
#
# Kullanım:  .\database\generate-script.ps1 [-Configuration Release]

param([string]$Configuration = 'Release')

$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent

$scripts = @(
    @{ File = '00_Core.sql';        Project = 'src\Core\ERP.Core.Infrastructure';                     Context = 'CoreMigrationsDbContext';        Title = 'Çekirdek (Core)' },
    @{ File = '10_Konfeksiyon.sql'; Project = 'src\Modules\Konfeksiyon\ERP.Konfeksiyon.Infrastructure'; Context = 'KonfeksiyonMigrationsDbContext'; Title = 'Konfeksiyon modülü' },
    @{ File = '20_Boyahane.sql';    Project = 'src\Modules\Boyahane\ERP.Boyahane.Infrastructure';       Context = 'BoyahaneMigrationsDbContext';    Title = 'Boyahane modülü' },
    @{ File = '30_Dokuma.sql';      Project = 'src\Modules\Dokuma\ERP.Dokuma.Infrastructure';           Context = 'DokumaMigrationsDbContext';      Title = 'Dokuma modülü' }
    @{ File = '40_Orme.sql';      Project = 'src\Modules\Orme\ERP.Orme.Infrastructure';           Context = 'OrmeMigrationsDbContext';      Title = 'Örme modülü' }
)

foreach ($s in $scripts) {
    $project = Join-Path $root $s.Project
    $output = Join-Path $PSScriptRoot $s.File
    $temp = [IO.Path]::GetTempFileName()

    # dotnet uyarıları stderr'e yazar; Windows PowerShell bunu hata sayıp durmasın, sonuca çıkış kodundan bakılır.
    $ErrorActionPreference = 'Continue'
    dotnet ef migrations script --idempotent `
        --project $project --startup-project $project `
        --context $s.Context --configuration $Configuration `
        --output $temp 2>&1 | Where-Object { "$_" -notmatch 'warning NU19' } | Out-Host
    $exitCode = $LASTEXITCODE
    $ErrorActionPreference = 'Stop'
    if ($exitCode -ne 0) { throw "dotnet ef migrations script başarısız oldu: $($s.Context)" }

    $header = @"
/*
    Tekstil ERP - $($s.Title) kurulum / güncelleme script'i
    Bu dosya otomatik üretilir (database\generate-script.ps1); elle düzenlemeyin.

    Hedef veritabanına (örn. TekstilERP) bağlanıp script'in tamamını çalıştırın. Script tekrar
    çalıştırılabilir: daha önce uygulanmış adımlar atlanır. Sıra: önce 00_Core.sql, sonra modüller.

    sqlcmd ile:  sqlcmd -S "(localdb)\MSSQLLocalDB" -d TekstilERP -E -b -f 65001 -i $($s.File)
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

"@

    $body = [IO.File]::ReadAllText($temp).TrimStart([char]0xFEFF)
    [IO.File]::WriteAllText($output, $header + $body, (New-Object Text.UTF8Encoding $true))
    [IO.File]::Delete($temp)
    Write-Host "Script üretildi: $output"
}
