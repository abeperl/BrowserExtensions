<#
    Restore database data from a source SQLite file into the target production database.

    This performs a full file replacement after creating a timestamped backup of the target.
    Use when the target schema matches the legacy schema (Id / Configuration columns, etc.).

    PARAMETERS:
      -SourceDb  Path to source database file containing desired data (required)
      -TargetDb  Path to target database file (default: production path)
      -Force     Skip interactive confirmation

    EXAMPLE:
      powershell -ExecutionPolicy Bypass -File restore-db-from-source.ps1 -SourceDb "C:\temp\api_config.db"

      powershell -ExecutionPolicy Bypass -File restore-db-from-source.ps1 -SourceDb .\bin\Debug\net8.0-windows10.0.19041.0\api_config.db -Force

    AFTER RESTORE:
      Run .\check-db-status.ps1 to verify Primary APIs, SubActions, and Schedules.
      Restart the Windows service if running: Restart-Service ScheduledPrintService
#>
param(
    [Parameter(Mandatory=$true)][string]$SourceDb,
    [string]$TargetDb = "C:\Program Files\Malchut\ScheduledPrintService\api_config.db",
    [switch]$Force
)

Write-Host "`n=== Database Restore Utility ===" -ForegroundColor Cyan
Write-Host "Source : $SourceDb" -ForegroundColor White
Write-Host "Target : $TargetDb" -ForegroundColor White

if (-not (Test-Path $SourceDb)) {
    Write-Host "[X] Source database not found: $SourceDb" -ForegroundColor Red
    exit 1
}

$targetExists = Test-Path $TargetDb
if ($targetExists) {
    Write-Host "[!] Target database exists and will be replaced" -ForegroundColor Yellow
    if (-not $Force) {
        $confirm = Read-Host "Type YES to confirm replacement"
        if ($confirm -ne 'YES') {
            Write-Host "[OK] Operation cancelled by user" -ForegroundColor Green
            exit 0
        }
    }

    # Backup existing target
    $backupName = "api_config-backup-$(Get-Date -Format yyyyMMdd-HHmmss).db"
    $backupPath = Join-Path (Split-Path $TargetDb -Parent) $backupName
    Copy-Item $TargetDb $backupPath -Force
    Write-Host "[OK] Backup created: $backupPath" -ForegroundColor Green
} else {
    Write-Host "[!] Target database does not exist; will create new file" -ForegroundColor Yellow
}

try {
    Copy-Item $SourceDb $TargetDb -Force
    Write-Host "[OK] Restore complete" -ForegroundColor Green
}
catch {
    Write-Host "[X] Failed to copy: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Post-restore quick validation (requires sqlite3 if present)
$sqlite3Path = if (Test-Path ".\sqlite3.exe") { ".\sqlite3.exe" } elseif (Get-Command sqlite3 -ErrorAction SilentlyContinue) { "sqlite3" } else { $null }
if ($sqlite3Path) {
    Write-Host "\n[INFO] Basic validation queries" -ForegroundColor Cyan
    Write-Host "PrimaryApi count:" -ForegroundColor Yellow
    & $sqlite3Path $TargetDb "SELECT COUNT(*) FROM PrimaryApi;"
    Write-Host "SubAction count:" -ForegroundColor Yellow
    & $sqlite3Path $TargetDb "SELECT COUNT(*) FROM SubAction;"
    Write-Host "Enabled schedules:" -ForegroundColor Yellow
    & $sqlite3Path $TargetDb "SELECT COUNT(*) FROM Schedule WHERE IsEnabled=1;"
} else {
    Write-Host "[!] sqlite3 not found; skipping validation queries" -ForegroundColor Yellow
}

Write-Host "\nNext:" -ForegroundColor Cyan
Write-Host "  - Run .\check-db-status.ps1" -ForegroundColor White
Write-Host "  - Restart service if running: Restart-Service ScheduledPrintService" -ForegroundColor White
Write-Host "  - If satisfied, you may delete old backup files after retention period" -ForegroundColor White
Write-Host "\nDone." -ForegroundColor Green
