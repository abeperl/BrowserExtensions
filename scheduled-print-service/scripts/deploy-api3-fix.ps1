# Deploy API #3 IdJsonPath Fix
# This script:
# 1. Stops the service
# 2. Applies database migration
# 3. Publishes and deploys new code
# 4. Restarts the service

param(
    [string]$ServiceName = "ScheduledPrintService",
    [string]$PublishPath = "C:\ScheduledPrintService",
    [string]$BackupPath = "C:\ScheduledPrintService_Backup",
    [switch]$SkipBackup = $false
)

$ErrorActionPreference = "Stop"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Deploy API #3 IdJsonPath Fix" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Get script directory
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectDir = $ScriptDir
$DbPath = Join-Path $PublishPath "api_config.db"
$SqlScriptPath = Join-Path $ProjectDir "fix-api3-id-path.sql"

# Verify we're running as Administrator
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Host "ERROR: This script must be run as Administrator" -ForegroundColor Red
    Write-Host "Please right-click PowerShell and select 'Run as Administrator'" -ForegroundColor Yellow
    exit 1
}

Write-Host "Step 1: Checking service status..." -ForegroundColor Yellow
$service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if ($null -eq $service) {
    Write-Host "WARNING: Service '$ServiceName' not found. Proceeding without service stop/start." -ForegroundColor Yellow
    $serviceExists = $false
} else {
    $serviceExists = $true
    Write-Host "Service found: $($service.Status)" -ForegroundColor Green

    if ($service.Status -eq "Running") {
        Write-Host "Stopping service..." -ForegroundColor Yellow
        Stop-Service -Name $ServiceName -Force
        Start-Sleep -Seconds 3
        Write-Host "Service stopped" -ForegroundColor Green
    }
}

Write-Host ""
Write-Host "Step 2: Backing up existing deployment..." -ForegroundColor Yellow
if (-not $SkipBackup -and (Test-Path $PublishPath)) {
    if (Test-Path $BackupPath) {
        Write-Host "Removing old backup..." -ForegroundColor Gray
        Remove-Item -Path $BackupPath -Recurse -Force
    }

    Write-Host "Creating backup at: $BackupPath" -ForegroundColor Gray
    Copy-Item -Path $PublishPath -Destination $BackupPath -Recurse -Force
    Write-Host "Backup created successfully" -ForegroundColor Green
} else {
    Write-Host "Skipping backup (not found or disabled)" -ForegroundColor Gray
}

Write-Host ""
Write-Host "Step 3: Applying database migration..." -ForegroundColor Yellow
if (-not (Test-Path $DbPath)) {
    Write-Host "ERROR: Database not found at: $DbPath" -ForegroundColor Red
    exit 1
}

if (-not (Test-Path $SqlScriptPath)) {
    Write-Host "ERROR: SQL script not found at: $SqlScriptPath" -ForegroundColor Red
    exit 1
}

Write-Host "Database: $DbPath" -ForegroundColor Gray
Write-Host "SQL Script: $SqlScriptPath" -ForegroundColor Gray

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

# Create database backup before migration
$dbBackupPath = "$DbPath.backup-$(Get-Date -Format 'yyyyMMdd-HHmmss')"
Write-Host "Creating database backup: $dbBackupPath" -ForegroundColor Gray
Copy-Item -Path $DbPath -Destination $dbBackupPath -Force
Write-Host "Database backup created" -ForegroundColor Green

# Apply migration
Write-Host "Applying migration..." -ForegroundColor Gray
$migrationOutput = Get-Content $SqlScriptPath | & $sqlite3Path $DbPath 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Database migration failed!" -ForegroundColor Red
    Write-Host $migrationOutput -ForegroundColor Red

    # Restore from backup
    Write-Host "Restoring database from backup..." -ForegroundColor Yellow
    Copy-Item -Path $dbBackupPath -Destination $DbPath -Force
    Write-Host "Database restored" -ForegroundColor Green
    exit 1
}

Write-Host "Migration applied successfully" -ForegroundColor Green
Write-Host "Migration output:" -ForegroundColor Gray
Write-Host $migrationOutput -ForegroundColor White

Write-Host ""
Write-Host "Step 4: Publishing new code..." -ForegroundColor Yellow
Write-Host "Building project in Release mode..." -ForegroundColor Gray

Push-Location $ProjectDir
try {
    # Clean and build
    dotnet clean --configuration Release | Out-Null
    $buildOutput = dotnet build --configuration Release 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Host "ERROR: Build failed!" -ForegroundColor Red
        Write-Host $buildOutput -ForegroundColor Red
        exit 1
    }
    Write-Host "Build successful" -ForegroundColor Green

    # Publish
    Write-Host "Publishing to: $PublishPath" -ForegroundColor Gray
    $publishOutput = dotnet publish --configuration Release --output $PublishPath --no-build 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Host "ERROR: Publish failed!" -ForegroundColor Red
        Write-Host $publishOutput -ForegroundColor Red
        exit 1
    }
    Write-Host "Publish successful" -ForegroundColor Green
} finally {
    Pop-Location
}

Write-Host ""
Write-Host "Step 5: Restarting service..." -ForegroundColor Yellow
if ($serviceExists) {
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
    Write-Host "Service does not exist - skipping start" -ForegroundColor Gray
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Deployment Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Yellow
Write-Host "1. Monitor service logs for API #3 execution" -ForegroundColor White
Write-Host "2. Verify 'Could not extract ID' warnings are gone" -ForegroundColor White
Write-Host "3. Check that orderDetailsId values are being tracked" -ForegroundColor White
Write-Host ""
Write-Host "Log Location: $PublishPath\logs" -ForegroundColor Gray
Write-Host "Database Backup: $dbBackupPath" -ForegroundColor Gray
if (-not $SkipBackup) {
    Write-Host "Full Backup: $BackupPath" -ForegroundColor Gray
}
Write-Host ""
Write-Host "To view live logs, run:" -ForegroundColor Yellow
Write-Host "  Get-Content '$PublishPath\logs\log-$(Get-Date -Format 'yyyyMMdd').txt' -Wait -Tail 50" -ForegroundColor Cyan
Write-Host ""
