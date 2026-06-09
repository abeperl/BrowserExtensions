# Migrate Database Only - API #3 IdJsonPath Fix
# This script ONLY applies the database migration without redeploying code
# Use this if the code is already deployed and you only need to update the configuration

param(
    [string]$ServiceName = "ScheduledPrintService",
    [string]$DbPath = "C:\ScheduledPrintService\api_config.db",
    [switch]$NoRestart = $false
)

$ErrorActionPreference = "Stop"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Database Migration: API #3 IdJsonPath" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Get script directory
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$SqlScriptPath = Join-Path $ScriptDir "fix-api3-id-path.sql"

# Verify we're running as Administrator
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Host "ERROR: This script must be run as Administrator" -ForegroundColor Red
    Write-Host "Please right-click PowerShell and select 'Run as Administrator'" -ForegroundColor Yellow
    exit 1
}

Write-Host "Database Path: $DbPath" -ForegroundColor Gray
Write-Host "SQL Script: $SqlScriptPath" -ForegroundColor Gray
Write-Host ""

# Verify database exists
if (-not (Test-Path $DbPath)) {
    Write-Host "ERROR: Database not found at: $DbPath" -ForegroundColor Red
    exit 1
}

# Verify SQL script exists
if (-not (Test-Path $SqlScriptPath)) {
    Write-Host "ERROR: SQL script not found at: $SqlScriptPath" -ForegroundColor Red
    exit 1
}

# Check if sqlite3 is available (check local folder first, then PATH)
$sqlite3Path = $null
if (Test-Path ".\sqlite3.exe") {
    $sqlite3Path = ".\sqlite3.exe"
    Write-Host "Using local sqlite3.exe" -ForegroundColor Gray
} elseif (Test-Path (Join-Path $PSScriptRoot "sqlite3.exe")) {
    $sqlite3Path = Join-Path $PSScriptRoot "sqlite3.exe"
    Write-Host "Using sqlite3.exe from script directory" -ForegroundColor Gray
} else {
    $sqlite3 = Get-Command sqlite3 -ErrorAction SilentlyContinue
    if ($null -ne $sqlite3) {
        $sqlite3Path = "sqlite3"
        Write-Host "Using sqlite3 from PATH" -ForegroundColor Gray
    } else {
        Write-Host "ERROR: sqlite3.exe not found in current folder, script folder, or PATH" -ForegroundColor Red
        Write-Host "Please ensure sqlite3.exe is in the same folder as this script" -ForegroundColor Yellow
        exit 1
    }
}

Write-Host "Step 1: Checking service status..." -ForegroundColor Yellow
$service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
$serviceWasRunning = $false

if ($null -eq $service) {
    Write-Host "WARNING: Service '$ServiceName' not found" -ForegroundColor Yellow
} else {
    Write-Host "Service found: $($service.Status)" -ForegroundColor Green

    if ($service.Status -eq "Running") {
        $serviceWasRunning = $true
        Write-Host "Stopping service..." -ForegroundColor Yellow
        Stop-Service -Name $ServiceName -Force
        Start-Sleep -Seconds 3
        Write-Host "Service stopped" -ForegroundColor Green
    }
}

Write-Host ""
Write-Host "Step 2: Creating database backup..." -ForegroundColor Yellow
$dbBackupPath = "$DbPath.backup-$(Get-Date -Format 'yyyyMMdd-HHmmss')"
Write-Host "Backup location: $dbBackupPath" -ForegroundColor Gray
Copy-Item -Path $DbPath -Destination $dbBackupPath -Force
Write-Host "Database backup created" -ForegroundColor Green

Write-Host ""
Write-Host "Step 3: Applying migration..." -ForegroundColor Yellow
Write-Host "Running SQL script..." -ForegroundColor Gray

$migrationOutput = Get-Content $SqlScriptPath | & $sqlite3Path $DbPath 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Database migration failed!" -ForegroundColor Red
    Write-Host $migrationOutput -ForegroundColor Red

    # Restore from backup
    Write-Host "Restoring database from backup..." -ForegroundColor Yellow
    Copy-Item -Path $dbBackupPath -Destination $DbPath -Force
    Write-Host "Database restored" -ForegroundColor Green

    if ($serviceWasRunning -and -not $NoRestart) {
        Write-Host "Restarting service..." -ForegroundColor Yellow
        Start-Service -Name $ServiceName
    }

    exit 1
}

Write-Host "Migration applied successfully!" -ForegroundColor Green
Write-Host ""
Write-Host "Migration Result:" -ForegroundColor Yellow
Write-Host $migrationOutput -ForegroundColor White

Write-Host ""
Write-Host "Step 4: Verifying configuration..." -ForegroundColor Yellow
$verifyQuery = "SELECT ApiNumber, ApiName, json_extract(Configuration, '$.IdJsonPath') as IdJsonPath FROM PrimaryApi WHERE ApiNumber = 3"
$verifyOutput = echo $verifyQuery | & $sqlite3Path $DbPath 2>&1

if ($LASTEXITCODE -eq 0) {
    Write-Host "Verification Result:" -ForegroundColor Gray
    Write-Host $verifyOutput -ForegroundColor Green
} else {
    Write-Host "WARNING: Could not verify configuration" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Step 5: Restarting service..." -ForegroundColor Yellow
if ($NoRestart) {
    Write-Host "Skipping service restart (NoRestart flag)" -ForegroundColor Gray
} elseif ($null -eq $service) {
    Write-Host "Service does not exist - skipping restart" -ForegroundColor Gray
} elseif ($serviceWasRunning) {
    Start-Service -Name $ServiceName
    Start-Sleep -Seconds 3

    $service = Get-Service -Name $ServiceName
    if ($service.Status -eq "Running") {
        Write-Host "Service started successfully" -ForegroundColor Green
    } else {
        Write-Host "WARNING: Service did not start. Status: $($service.Status)" -ForegroundColor Yellow
        Write-Host "Check the Windows Event Log for errors" -ForegroundColor Yellow
    }
} else {
    Write-Host "Service was not running before - leaving stopped" -ForegroundColor Gray
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Migration Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "What was changed:" -ForegroundColor Yellow
Write-Host "- API #3 IdJsonPath: [0] → orderDetailsId" -ForegroundColor White
Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Yellow
Write-Host "1. Monitor logs for API #3 execution" -ForegroundColor White
Write-Host "2. Verify 'Could not extract ID' warnings are gone" -ForegroundColor White
Write-Host "3. Confirm orderDetailsId values are being tracked" -ForegroundColor White
Write-Host ""
Write-Host "Database Backup: $dbBackupPath" -ForegroundColor Gray
Write-Host ""
Write-Host "To rollback this migration:" -ForegroundColor Yellow
Write-Host "  Copy-Item '$dbBackupPath' -Destination '$DbPath' -Force" -ForegroundColor Cyan
Write-Host ""
