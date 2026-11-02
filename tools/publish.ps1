# Müşteriye özel kurulum paketi hazırlar. Pakete yalnızca seçilen modüllerin dosyaları girer:
# lisanslanmayan modülün kodu (DLL) ve veritabanı script'i kurulumda hiç yer almaz.
#
#   <Output>\api\                 IIS'e kurulacak API (modules\ altında yalnızca seçilen modüller)
#   <Output>\api\license.lic      -License verildiyse müşterinin lisansı
#   <Output>\database\            00_Core.sql + seçilen modüllerin script'leri (çalıştırma sırasıyla)
#   <Output>\web\                 -Web verilirse web arayüzü (erp-web\dist)
#   <Output>\KURULUM.txt          Kurulum adımları
#
# Kullanım:
#   .\tools\publish.ps1 -Modules Konfeksiyon -Output dist\OrnekTekstil -License C:\lisanslar\ornek.lic
#   .\tools\publish.ps1 -Modules Boyahane,Dokuma -Output dist\OrnekBoya -Web

param(
    [Parameter(Mandatory)][string[]]$Modules,
    [Parameter(Mandatory)][string]$Output,
    [string]$License,
    [switch]$Web,
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$known = [ordered]@{ Konfeksiyon = '10_Konfeksiyon.sql'; Boyahane = '20_Boyahane.sql'; Dokuma = '30_Dokuma.sql'; Orme = '40_Orme.sql' }

$Modules = @($Modules | ForEach-Object { $_ -split ',' } | ForEach-Object { $_.Trim() } | Where-Object { $_ })
foreach ($m in $Modules) {
    if (-not $known.Contains($m)) { throw "Bilinmeyen modül: $m (geçerli: $($known.Keys -join ', '))" }
}
$Modules = @($known.Keys | Where-Object { $Modules -contains $_ })   # kurulum sırası

if (Test-Path $Output) { throw "$Output zaten var; boş bir klasör verin." }
$api = Join-Path $Output 'api'
$db = Join-Path $Output 'database'

function Invoke-Dotnet([string[]]$arguments) {
    # dotnet uyarıları stderr'e yazar; Windows PowerShell bunu hata saymasın, sonuca çıkış kodundan bakılır.
    $ErrorActionPreference = 'Continue'
    & dotnet @arguments 2>&1 | Where-Object { "$_" -match 'error|Hata' } | Out-Host
    if ($LASTEXITCODE -ne 0) { throw "dotnet $($arguments[0]) başarısız oldu." }
}

Write-Host "API yayınlanıyor ($Configuration)..."
Invoke-Dotnet @('publish', (Join-Path $root 'src\ERP.Api\ERP.Api.csproj'), '-c', $Configuration, '-o', $api, '--nologo')

# Yayın çıktısında modül olmamalı (API modüllere derleme anında bağlı değildir); yine de garanti altına al.
Get-ChildItem $api -Filter 'ERP.*.dll' | Where-Object { $_.Name -notmatch '^ERP\.(Api|Core\.)' } | Remove-Item
if (Test-Path (Join-Path $api 'modules')) { Remove-Item (Join-Path $api 'modules') -Recurse -Force }
New-Item -ItemType Directory -Force (Join-Path $api 'modules') | Out-Null

foreach ($m in $Modules) {
    $project = Join-Path $root "src\Modules\$m\ERP.$m.Infrastructure\ERP.$m.Infrastructure.csproj"
    Write-Host "Modül derleniyor: $m"
    Invoke-Dotnet @('build', $project, '-c', $Configuration, '--nologo')
    $bin = Join-Path $root "src\Modules\$m\ERP.$m.Infrastructure\bin\$Configuration\net10.0"
    Get-ChildItem $bin -File | Where-Object { $_.Name -like "ERP.$m.*" -and $_.Extension -in ".dll", ".pdb", ".xml" } | Copy-Item -Destination (Join-Path $api 'modules')
}

New-Item -ItemType Directory -Force $db | Out-Null
Copy-Item (Join-Path $root 'database\00_Core.sql') $db
foreach ($m in $Modules) { Copy-Item (Join-Path $root "database\$($known[$m])") $db }

if ($License) {
    Copy-Item $License (Join-Path $api 'license.lic')
}

if ($Web) {
    Write-Host 'Web arayüzü derleniyor...'
    Push-Location (Join-Path $root 'erp-web')
    try {
        $ErrorActionPreference = 'Continue'
        & npm run build 2>&1 | Out-Host
        if ($LASTEXITCODE -ne 0) { throw 'npm run build başarısız oldu.' }
        $ErrorActionPreference = 'Stop'
    } finally { Pop-Location }
    Copy-Item (Join-Path $root 'erp-web\dist') (Join-Path $Output 'web') -Recurse
}

$scripts = @('00_Core.sql') + @($Modules | ForEach-Object { $known[$_] })
$steps = @"
Tekstil ERP kurulum paketi
Modüller: $($Modules -join ', ')
Hazırlanma: $(Get-Date -Format 'dd.MM.yyyy HH:mm')

1. Veritabanı: SQL Server'da boş bir veritabanı oluşturun (örn. TekstilERP) ve database klasöründeki
   script'leri bu sırayla çalıştırın:
$(($scripts | ForEach-Object { "     sqlcmd -S <SUNUCU> -d TekstilERP -E -b -f 65001 -i database\$_" }) -join "`r`n")
   Script'ler tekrar çalıştırılabilir; güncelleme paketinde de aynı sırayla çalıştırılır.

2. API: api klasörünü IIS'e kurun. appsettings.json'da ConnectionStrings:DefaultConnection, Jwt:Key ve
   Cors:AllowedOrigins değerlerini müşteriye göre girin.

3. Lisans: api\license.lic dosyası $(if ($License) { 'pakete eklendi.' } else { 'EKLENMEDİ; lisansı api klasörüne license.lic adıyla koyun.' })
   Lisans yoksa ya da geçersizse API yalnızca çekirdekle açılır; durum /api/v1/system/modules adresinde görünür.

4. İlk açılış: API ilk açılışta yönetici kullanıcısını (InitialAdmin ayarları) oluşturur.
"@
[IO.File]::WriteAllText((Join-Path $Output 'KURULUM.txt'), $steps, (New-Object Text.UTF8Encoding $true))

Write-Host ""
Write-Host "Paket hazır: $(Resolve-Path $Output)"
Get-ChildItem (Join-Path $api 'modules') | ForEach-Object { Write-Host "  modules\$($_.Name)" }
