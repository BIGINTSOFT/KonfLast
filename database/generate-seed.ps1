# Bir veritabanındaki tanım/örnek verilerden seed script'i üretir: database\SeedData.sql
# Script, kurulum script'leri (00_Core.sql, 10_Konfeksiyon.sql ...) çalıştırılmış bir veritabanına uygulanır.
# Her satır birincil anahtarına göre kontrol edilir; var olan satırlar atlanır, bu yüzden
# script tekrar çalıştırılabilir. Tamamı tek transaction içinde çalışır.
#
# Kullanım:  .\database\generate-seed.ps1 [-Server "(localdb)\MSSQLLocalDB"] [-Database TekstilERP]

param(
    [string]$Server = '(localdb)\MSSQLLocalDB',
    [string]$Database = 'TekstilERP'
)

$ErrorActionPreference = 'Stop'
$output = Join-Path $PSScriptRoot 'SeedData.sql'

# Migration'ların yönettiği, kullanıcıya özgü ya da hareket/log niteliğindeki tablolar alınmaz.
# (Yönetici kullanıcıyı uygulama ilk açılışta InitialAdminSeeder ile oluşturur.)
$excluded = @(
    '__EFMigrationsHistory', 'Permissions', 'Roles', 'RolePermissions',
    'Users', 'UserRoles', 'UserShortcuts', 'RefreshTokens', 'AuditLogs'
)
# Migration'ın eklediği satırı da seed'deki değerle güncellenecek tablolar.
$upsert = @('PayrollSettings')

$conn = New-Object System.Data.SqlClient.SqlConnection "Server=$Server;Database=$Database;Trusted_Connection=True;TrustServerCertificate=True"
$conn.Open()

function Invoke-Query([string]$sql) {
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = $sql
    $table = New-Object System.Data.DataTable
    $table.Load($cmd.ExecuteReader())
    return , $table
}

$tables = Invoke-Query @"
SELECT t.name, SCHEMA_NAME(t.schema_id) + '.' + t.name AS FullName, OBJECTPROPERTY(t.object_id, 'TableHasIdentity') AS HasIdentity
FROM sys.tables t
WHERE SCHEMA_NAME(t.schema_id) <> 'dbo'
  AND EXISTS (SELECT 1 FROM sys.partitions p WHERE p.object_id = t.object_id AND p.index_id IN (0, 1) AND p.rows > 0)
"@
$foreignKeys = Invoke-Query @"
SELECT DISTINCT OBJECT_SCHEMA_NAME(parent_object_id) + '.' + OBJECT_NAME(parent_object_id) AS Child,
       OBJECT_SCHEMA_NAME(referenced_object_id) + '.' + OBJECT_NAME(referenced_object_id) AS Parent
FROM sys.foreign_keys
WHERE parent_object_id <> referenced_object_id
"@

# Tablolar şemalara ayrıdır (Core, Konfeksiyon, Boyahane, Dokuma); aynı ad farklı şemalarda bulunabilir.
$names = @($tables.Rows | Where-Object { $excluded -notcontains $_.name } | ForEach-Object { $_.FullName } | Sort-Object)

# Yabancı anahtarlara göre sıralama: önce bağımlı olunan tablolar.
$ordered = New-Object System.Collections.Generic.List[string]
$remaining = New-Object System.Collections.Generic.List[string]
$names | ForEach-Object { $remaining.Add($_) }
while ($remaining.Count -gt 0) {
    $ready = @($remaining | Where-Object {
        $name = $_
        -not ($foreignKeys.Rows | Where-Object { $_.Child -eq $name -and $remaining.Contains($_.Parent) })
    })
    if ($ready.Count -eq 0) { throw "Tablolar arasında döngüsel bağımlılık var: $($remaining -join ', ')" }
    foreach ($name in $ready) { $ordered.Add($name); [void]$remaining.Remove($name) }
}

function Format-Value($value, [string]$type) {
    if ($value -is [DBNull]) { return 'NULL' }
    $inv = [Globalization.CultureInfo]::InvariantCulture
    switch ($type) {
        'bit'              { if ($value) { return '1' } else { return '0' } }
        'datetime2'        { return "'" + $value.ToString('yyyy-MM-ddTHH:mm:ss.fffffff', $inv) + "'" }
        'datetime'         { return "'" + $value.ToString('yyyy-MM-ddTHH:mm:ss.fff', $inv) + "'" }
        'date'             { return "'" + $value.ToString('yyyy-MM-dd', $inv) + "'" }
        'datetimeoffset'   { return "'" + $value.ToString('yyyy-MM-ddTHH:mm:ss.fffffffzzz', $inv) + "'" }
        'time'             { return "'" + $value.ToString('hh\:mm\:ss\.fffffff', $inv) + "'" }
        'uniqueidentifier' { return "'" + $value.ToString() + "'" }
        'varbinary'        { return '0x' + [BitConverter]::ToString($value).Replace('-', '') }
        { $_ -in 'char', 'varchar' } { return "'" + $value.Replace("'", "''") + "'" }
        { $_ -in 'nchar', 'nvarchar' } { return "N'" + $value.Replace("'", "''") + "'" }
        default            { return ([IFormattable]$value).ToString($null, $inv) }
    }
}

$sb = New-Object System.Text.StringBuilder
[void]$sb.AppendLine(@"
/*
    Tekstil ERP - örnek/tanım verileri (seed)
    Bu dosya otomatik üretilir (database\generate-seed.ps1, kaynak: $Database); elle düzenlemeyin.

    Önce kurulum script'leri (00_Core.sql, 10_Konfeksiyon.sql) çalıştırılmalıdır. Var olan satırlar (birincil anahtara göre) atlanır;
    script tekrar çalıştırılabilir. Kullanıcı, rol ve yetki verileri bu script'te yoktur.

    sqlcmd ile:  sqlcmd -S "(localdb)\MSSQLLocalDB" -d TekstilERP -E -b -f 65001 -i SeedData.sql
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
"@)

$total = 0
foreach ($name in $ordered) {
    $schema, $table = $name -split '\.'
    $qualified = "[$schema].[$table]"
    $columns = Invoke-Query @"
SELECT c.name, ty.name AS TypeName,
       CAST(CASE WHEN EXISTS (SELECT 1 FROM sys.indexes i JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
                              WHERE i.object_id = c.object_id AND i.is_primary_key = 1 AND ic.column_id = c.column_id) THEN 1 ELSE 0 END AS bit) AS IsKey
FROM sys.columns c JOIN sys.types ty ON ty.user_type_id = c.user_type_id
WHERE c.object_id = OBJECT_ID('$name') AND c.is_computed = 0 AND ty.name NOT IN ('timestamp', 'rowversion')
ORDER BY c.column_id
"@
    $cols = @($columns.Rows)
    $keys = @($cols | Where-Object { $_.IsKey })
    $values = @($cols | Where-Object { -not $_.IsKey })
    $colList = ($cols | ForEach-Object { "[$($_.name)]" }) -join ', '
    $orderBy = ($keys | ForEach-Object { "[$($_.name)]" }) -join ', '
    $rows = Invoke-Query "SELECT $colList FROM $qualified ORDER BY $orderBy"
    $hasIdentity = ($tables.Rows | Where-Object { $_.FullName -eq $name }).HasIdentity -eq 1

    [void]$sb.AppendLine()
    [void]$sb.AppendLine("-- $name ($($rows.Rows.Count))")
    if ($hasIdentity) { [void]$sb.AppendLine("SET IDENTITY_INSERT $qualified ON;") }
    foreach ($row in $rows.Rows) {
        $literals = @{}
        foreach ($c in $cols) { $literals[$c.name] = Format-Value $row[$c.name] $c.TypeName }
        $where = ($keys | ForEach-Object { "[$($_.name)] = $($literals[$_.name])" }) -join ' AND '
        $insert = "INSERT INTO $qualified ($colList) VALUES (" + (($cols | ForEach-Object { $literals[$_.name] }) -join ', ') + ');'
        if ($upsert -contains $table -and $values.Count -gt 0) {
            $set = ($values | ForEach-Object { "[$($_.name)] = $($literals[$_.name])" }) -join ', '
            [void]$sb.AppendLine("IF EXISTS (SELECT 1 FROM $qualified WHERE $where) UPDATE $qualified SET $set WHERE $where;")
            [void]$sb.AppendLine("ELSE $insert")
        } else {
            [void]$sb.AppendLine("IF NOT EXISTS (SELECT 1 FROM $qualified WHERE $where) $insert")
        }
    }
    if ($hasIdentity) { [void]$sb.AppendLine("SET IDENTITY_INSERT $qualified OFF;") }
    $total += $rows.Rows.Count
}

[void]$sb.AppendLine()
[void]$sb.AppendLine('COMMIT TRANSACTION;')
[void]$sb.AppendLine('GO')
$conn.Close()

[IO.File]::WriteAllText($output, $sb.ToString(), (New-Object Text.UTF8Encoding $true))
Write-Host "Seed script'i üretildi: $output ($($ordered.Count) tablo, $total satır)"
