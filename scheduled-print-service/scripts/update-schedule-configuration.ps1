#Requires -RunAsAdministrator

<#
.SYNOPSIS
    Updates the schedule configuration to run every 5 minutes from 9 AM to 9 PM, daily except Saturday
.DESCRIPTION
    - Safely updates Schedule 1 cron expression
    - Runs every 5 minutes between 9 AM and 9 PM (21:00)
    - Runs Sunday through Friday (excludes Saturday)
    - Cron format: 0 */5 9-21 * * 0-5
    - Creates backup before making changes
    - Production-safe with validation
#>

param(
    [string]$ServiceName = "ScheduledPrintService",
    [string]$ExeFolder = "C:\Program Files\Malchut\ScheduledPrintService",
    [string]$DataRoot = "C:\ProgramData\ScheduledPrintService"
)

Write-Host "=== Update Schedule Configuration ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "New Schedule Configuration:" -ForegroundColor Yellow
Write-Host "  - Every 5 minutes (instead of 15)" -ForegroundColor White
Write-Host "  - 9 AM to 9 PM only" -ForegroundColor White
Write-Host "  - Daily except Saturday" -ForegroundColor White
Write-Host "  - Cron: 0 */5 9-21 * * 0-5" -ForegroundColor White
Write-Host ""

# Confirm
$confirm = Read-Host "Continue with update? (y/n)"
if ($confirm -ne "y") {
    Write-Host "Update cancelled." -ForegroundColor Yellow
    exit 0
}
Write-Host ""

# Stop service
Write-Host "Stopping service..." -ForegroundColor Yellow
$service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if ($service -and $service.Status -eq "Running") {
    Stop-Service -Name $ServiceName -Force
    Start-Sleep -Seconds 2
    Write-Host "[+] Service stopped" -ForegroundColor Green
} else {
    Write-Host "[+] Service is not running" -ForegroundColor Gray
}
Write-Host ""

# Locate database
$dbPath = Join-Path $ExeFolder "api_config.db"
if (-not (Test-Path $dbPath)) {
    Write-Host "[!] Database not found at: $dbPath" -ForegroundColor Red
    exit 1
}

Write-Host "Database: $dbPath" -ForegroundColor Gray
Write-Host ""

# Create backup
$backupPath = Join-Path $DataRoot "backups"
if (-not (Test-Path $backupPath)) {
    New-Item -Path $backupPath -ItemType Directory -Force | Out-Null
}

$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$backupFile = Join-Path $backupPath "api_config_$timestamp.db"

Write-Host "Creating backup..." -ForegroundColor Yellow
Copy-Item $dbPath $backupFile -Force
Write-Host "[+] Backup created: $backupFile" -ForegroundColor Green
Write-Host ""

# Find sqlite3.exe
$sqlite3Path = $null
$localSqlite = Join-Path $ExeFolder "sqlite3.exe"

if (Test-Path $localSqlite) {
    $sqlite3Path = $localSqlite
} else {
    # Try common installation paths
    $possiblePaths = @(
        "C:\Program Files\sqlite3\sqlite3.exe",
        "C:\sqlite\sqlite3.exe"
    )
    foreach ($path in $possiblePaths) {
        if (Test-Path $path) {
            $sqlite3Path = $path
            break
        }
    }
}

if (-not $sqlite3Path) {
    Write-Host "[!] sqlite3.exe not found" -ForegroundColor Red
    Write-Host ""
    Write-Host "Please install sqlite3 or run manually:" -ForegroundColor Yellow
    Write-Host "  UPDATE Schedule SET CronExpression = '0 */5 9-21 * * 0-5', UpdatedAt = datetime('now') WHERE Id = 1;" -ForegroundColor Gray
    Write-Host ""
    exit 1
}

Write-Host "Using sqlite3: $sqlite3Path" -ForegroundColor Gray
Write-Host ""

# Show current configuration
Write-Host "Current Schedule Configuration:" -ForegroundColor Cyan
Write-Host "------------------------------" -ForegroundColor Gray
$currentConfig = & $sqlite3Path $dbPath "SELECT Id, ScheduleName, CronExpression, IsEnabled FROM Schedule WHERE Id = 1;"
$currentConfig | Write-Host -ForegroundColor White
Write-Host ""

# Create SQL update command
$sqlCommands = @"
-- Update Schedule 1: Every 5 minutes, 9 AM to 9 PM, daily except Saturday
UPDATE Schedule
SET CronExpression = '0 */5 9-21 * * 0-5',
    UpdatedAt = datetime('now')
WHERE Id = 1;

-- Verify update
SELECT 'Updated Schedule Configuration:';
SELECT Id, ScheduleName, CronExpression, IsEnabled, UpdatedAt FROM Schedule WHERE Id = 1;
"@

# Save SQL to temp file
$sqlFile = "$env:TEMP\update_schedule_$timestamp.sql"
$sqlCommands | Out-File $sqlFile -Encoding ASCII -NoNewline

# Execute SQL
Write-Host "Updating schedule in database..." -ForegroundColor Yellow
$output = & $sqlite3Path $dbPath ".read $sqlFile" 2>&1

Write-Host ""
Write-Host "Update Result:" -ForegroundColor Cyan
Write-Host "--------------" -ForegroundColor Gray
$output | Write-Host -ForegroundColor White
Write-Host ""

# Clean up
Remove-Item $sqlFile -Force -ErrorAction SilentlyContinue

Write-Host "[+] Database updated successfully" -ForegroundColor Green
Write-Host ""

# Show schedule details
Write-Host "Schedule Details:" -ForegroundColor Cyan
Write-Host "  Cron Expression: 0 */5 9-21 * * 0-5" -ForegroundColor White
Write-Host "  Meaning:" -ForegroundColor White
Write-Host "    - Field 1 (seconds): 0 - Run at the start of the minute" -ForegroundColor Gray
Write-Host "    - Field 2 (minutes): */5 - Every 5 minutes" -ForegroundColor Gray
Write-Host "    - Field 3 (hours): 9-21 - Between 9 AM and 9 PM" -ForegroundColor Gray
Write-Host "    - Field 4 (day): * - Every day of month" -ForegroundColor Gray
Write-Host "    - Field 5 (month): * - Every month" -ForegroundColor Gray
Write-Host "    - Field 6 (day-of-week): 0-5 - Sunday(0) through Friday(5), excludes Saturday(6)" -ForegroundColor Gray
Write-Host ""
Write-Host "  Run Times:" -ForegroundColor White
Write-Host "    - 9:00 AM, 9:05 AM, 9:10 AM, ... 8:55 PM (last run at 8:55 PM)" -ForegroundColor Gray
Write-Host "    - Does NOT run on Saturdays" -ForegroundColor Gray
Write-Host "    - Does NOT run between 9:00 PM and 8:59 AM" -ForegroundColor Gray
Write-Host ""

# Start service
Write-Host "Starting service..." -ForegroundColor Yellow
Start-Service -Name $ServiceName
Start-Sleep -Seconds 3

$service = Get-Service -Name $ServiceName

if ($service.Status -eq "Running") {
    Write-Host "[+] Service started successfully" -ForegroundColor Green
} else {
    Write-Host "[!] Service failed to start. Status: $($service.Status)" -ForegroundColor Red
    Write-Host ""
    Write-Host "Check logs:" -ForegroundColor Yellow
    Write-Host "  Get-Content '$DataRoot\logs\*.log' -Tail 50" -ForegroundColor Gray
    exit 1
}

Write-Host ""
Write-Host "=== Update Complete ===" -ForegroundColor Green
Write-Host ""
Write-Host "Monitor service:" -ForegroundColor Yellow
Write-Host "  Get-Content '$DataRoot\logs\*.log' -Tail 50 -Wait" -ForegroundColor Gray
Write-Host ""
Write-Host "Verify schedule:" -ForegroundColor Yellow
Write-Host "  .\query-database.ps1" -ForegroundColor Gray
Write-Host ""
Write-Host "Restore from backup (if needed):" -ForegroundColor Yellow
Write-Host "  Stop-Service $ServiceName" -ForegroundColor Gray
Write-Host "  Copy-Item '$backupFile' '$dbPath' -Force" -ForegroundColor Gray
Write-Host "  Start-Service $ServiceName" -ForegroundColor Gray
Write-Host ""
