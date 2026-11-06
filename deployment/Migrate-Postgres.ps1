param(
    [string]$Project = 'ilearn',
    [string]$Database = 'ilearn',
    [string]$EnvFile,
    [string]$OverrideFile
)
$ErrorActionPreference = 'Stop'
if ($Project -notmatch '^[a-z0-9][a-z0-9-]*$' -or $Database -notmatch '^[a-z][a-z0-9_]*$') { throw 'Invalid local project/database name.' }
$workspace = Split-Path $PSScriptRoot -Parent
$compose = @('compose', '-p', $Project)
if ($EnvFile) { $compose += @('--env-file', $EnvFile) }
$compose += @('-f', (Join-Path $workspace 'compose.yaml'))
if ($OverrideFile) { $compose += @('-f', $OverrideFile) }
function Invoke-Docker([string[]]$Arguments) {
    $result = & docker @Arguments
    if ($LASTEXITCODE -ne 0) { throw "Docker operation failed: $($Arguments[0])" }
    return $result
}
$container = "$Project-postgres-1"
$source = (Invoke-Docker @('inspect', $container) | ConvertFrom-Json)[0]
$oldVolume = ($source.Mounts | Where-Object Destination -eq '/var/lib/postgresql').Name
$newVolume = "${Project}_ilearn-postgres-v2"
$checkpoint = Join-Path $workspace "ILearn/App_Data/backups/security-$Project-verified.json"
if ($oldVolume -eq $newVolume) {
    if (!(Test-Path -LiteralPath $checkpoint)) { throw 'Target volume is present but migration has no verified checkpoint. Inspect the preserved backup before starting the application.' }
    $verified = Get-Content -LiteralPath $checkpoint -Raw | ConvertFrom-Json
    if (!$verified.Verified -or $verified.Database -ne $Database -or $verified.TargetVolume -ne $newVolume) { throw 'Migration checkpoint does not match this database.' }
    Write-Output 'Database migration was already verified; no data changed.'; exit 0
}
if ($oldVolume -ne "${Project}_ilearn-postgres") { throw "Unexpected source volume: $oldVolume" }
$existing = & docker volume inspect $newVolume 2>$null
if ($LASTEXITCODE -eq 0) { throw 'Target volume already exists. Refusing to overwrite an unverified migration.' }
Invoke-Docker ($compose + @('stop', 'ilearn')) | Out-Null
$snapshot = @'
SET timezone='UTC';
SELECT format('SELECT %L,count(*),md5(COALESCE(string_agg(md5(row_to_json(t)::text),'''' ORDER BY md5(row_to_json(t)::text) COLLATE "C"),'''')) FROM public.%I t;',tablename,tablename)
FROM pg_tables WHERE schemaname='public' ORDER BY tablename COLLATE "C"
\gexec
'@
function Read-Snapshot {
    $result = $snapshot | & docker exec -i $container psql -X -q -t -A -v ON_ERROR_STOP=1 -U ilearn -d $Database
    if ($LASTEXITCODE -ne 0) { throw 'Database snapshot failed.' }
    return ($result -join "`n").Trim()
}
$before = Read-Snapshot
if (!$before) { throw 'No application tables found; use fresh database initialization instead.' }
$backupDirectory = Join-Path $workspace ('ILearn/App_Data/backups/security-' + $Project + '-' + [DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss'))
New-Item -ItemType Directory -Path $backupDirectory -Force | Out-Null
& (Join-Path $PSScriptRoot 'Protect-LocalSecrets.ps1')
$backup = Join-Path $backupDirectory 'database.dump'
Invoke-Docker @('exec', $container, 'pg_dump', '-U', 'ilearn', '-d', $Database, '-Fc', '-f', '/tmp/ilearn-migration.dump') | Out-Null
Invoke-Docker @('cp', "${container}:/tmp/ilearn-migration.dump", $backup) | Out-Null
if ((Get-Item -LiteralPath $backup).Length -lt 100) { throw 'Backup is missing or empty.' }
[IO.File]::WriteAllText((Join-Path $backupDirectory 'snapshot-before.txt'), $before)
Get-FileHash -LiteralPath $backup -Algorithm SHA256 | Select-Object Hash | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $backupDirectory 'checksum.json')
Invoke-Docker ($compose + @('up', '-d', '--build', '--wait', '--wait-timeout', '90', 'postgres')) | Out-Null
$target = (Invoke-Docker @('inspect', $container) | ConvertFrom-Json)[0]
if (($target.Mounts | Where-Object Destination -eq '/var/lib/postgresql').Name -ne $newVolume) { throw 'Refusing restore: target volume is not the new isolated volume.' }
Invoke-Docker @('cp', $backup, "${container}:/tmp/ilearn-migration.dump") | Out-Null
Invoke-Docker @('exec', $container, 'pg_restore', '-U', 'ilearn', '-d', $Database, '--clean', '--if-exists', '--no-owner', '--no-privileges', '--exit-on-error', '/tmp/ilearn-migration.dump') | Out-Null
$after = Read-Snapshot
[IO.File]::WriteAllText((Join-Path $backupDirectory 'snapshot-after.txt'), $after)
if ($before -ne $after) { throw 'Database contents differ after restore. Application remains stopped; the old volume and backup are preserved.' }
@{ Verified = $true; Database = $Database; TargetVolume = $newVolume; SourceVolume = $oldVolume; Backup = $backupDirectory } | ConvertTo-Json | Set-Content -LiteralPath $checkpoint
Invoke-Docker ($compose + @('up', '-d', '--build', 'database-setup', 'ilearn')) | Out-Null
Write-Output "PASS: all public table row counts and content hashes match. Old volume retained: $oldVolume"
Write-Output "Private backup: $backupDirectory"
