# Eski KonfeksiyonDB'den (tek parça, dbo) yeni modüler TekstilERP veritabanını oluşturur.
#
#   1. Kaynak veritabanının COPY_ONLY yedeğini alır (kaynak değişmez, yedek zincirini bozmaz).
#   2. Yedeği hedef adla geri yükler.
#   3. Hedefte KonfeksiyonDB-to-TekstilERP.sql ve TekstilERP-proje-ayrimi.sql (şema taşıma), ardından 00_Core.sql ve 10_Konfeksiyon.sql
#      (ve -Modules ile verilen modüllerin script'leri) çalıştırılır.
#
# Kullanım:
#   .\database\upgrade\upgrade-from-KonfeksiyonDB.ps1
#   .\database\upgrade\upgrade-from-KonfeksiyonDB.ps1 -Target TekstilERP -Modules Boyahane,Dokuma
#   .\database\upgrade\upgrade-from-KonfeksiyonDB.ps1 -Server "SUNUCU\SQLEXPRESS" -Source KonfeksiyonDB -Target TekstilERP

param(
    [string]$Server = '(localdb)\MSSQLLocalDB',
    [string]$Source = 'KonfeksiyonDB',
    [string]$Target = 'TekstilERP',
    [string[]]$Modules = @(),
    [string]$BackupDirectory = $env:TEMP
)

$ErrorActionPreference = 'Stop'
$database = Split-Path $PSScriptRoot -Parent
$moduleScripts = @{ Boyahane = '20_Boyahane.sql'; Dokuma = '30_Dokuma.sql'; Orme = '40_Orme.sql' }

function Invoke-Sql([string]$db, [string]$sql) {
    $cn = New-Object System.Data.SqlClient.SqlConnection "Server=$Server;Database=$db;Integrated Security=True;TrustServerCertificate=True"
    $cn.Open()
    try {
        $cmd = $cn.CreateCommand()
        $cmd.CommandTimeout = 0
        $cmd.CommandText = $sql
        $table = New-Object System.Data.DataTable
        $table.Load($cmd.ExecuteReader())
        return , $table
    } finally { $cn.Close() }
}

function Invoke-SqlFile([string]$db, [string]$file) {
    Write-Host "  -> $(Split-Path $file -Leaf)"
    & sqlcmd -S $Server -E -b -f 65001 -d $db -i $file | Out-Host
    if ($LASTEXITCODE -ne 0) { throw "$file çalıştırılırken hata oluştu." }
}

foreach ($m in $Modules) {
    if (-not $moduleScripts.ContainsKey($m)) { throw "Bilinmeyen modül: $m (Boyahane, Dokuma, Orme)" }
}

if ((Invoke-Sql 'master' "SELECT DB_ID(N'$Target') AS Id").Rows[0].Id -isnot [DBNull]) {
    throw "$Target veritabanı zaten var. Üzerine yazılmaz; farklı bir -Target verin ya da mevcut olanı kaldırın."
}

$backup = Join-Path $BackupDirectory "$Source-$(Get-Date -Format 'yyyyMMdd-HHmmss').bak"
Write-Host "1/3 $Source yedekleniyor (COPY_ONLY): $backup"
Invoke-Sql 'master' "BACKUP DATABASE [$Source] TO DISK = N'$backup' WITH COPY_ONLY, INIT" | Out-Null

Write-Host "2/3 $Target olarak geri yükleniyor"
$dataPath = (Invoke-Sql 'master' "SELECT CAST(SERVERPROPERTY('InstanceDefaultDataPath') AS nvarchar(400)) AS P").Rows[0].P
$files = Invoke-Sql 'master' "RESTORE FILELISTONLY FROM DISK = N'$backup'"
$moves = foreach ($f in $files.Rows) {
    $ext = if ($f.Type -eq 'L') { '_log.ldf' } else { '.mdf' }
    "MOVE N'$($f.LogicalName)' TO N'$(Join-Path $dataPath ($Target + $ext))'"
}
Invoke-Sql 'master' "RESTORE DATABASE [$Target] FROM DISK = N'$backup' WITH $($moves -join ', ')" | Out-Null

Write-Host "3/3 $Target modüler yapıya taşınıyor"
Invoke-SqlFile $Target (Join-Path $PSScriptRoot 'KonfeksiyonDB-to-TekstilERP.sql')
Invoke-SqlFile $Target (Join-Path $PSScriptRoot 'TekstilERP-proje-ayrimi.sql')
Invoke-SqlFile $Target (Join-Path $database '00_Core.sql')
Invoke-SqlFile $Target (Join-Path $database '10_Konfeksiyon.sql')
foreach ($m in $Modules) { Invoke-SqlFile $Target (Join-Path $database $moduleScripts[$m]) }

Write-Host ""
Write-Host "Tamamlandı. $Source değişmedi; yeni veritabanı: $Target (yedek: $backup)"
