#Requires -Version 5.1
#Requires -RunAsAdministrator
<#
.SYNOPSIS
    Fix the data root path for Scheduled Print Service

.DESCRIPTION
    Updates the Windows service environment variable to use the correct data root path.
    Changes from: E:\Share\server\servern\Software\Logs
    To: E:\Share\server\servern\Software\ScheduledPrintService

.EXAMPLE
    # Run as Administrator
    powershell -ExecutionPolicy Bypass -File fix-data-root-path.ps1
#>

$ServiceName = "Scheduled Print Service"
$NewDataRoot = "E:\Share\server\servern\Software\ScheduledPrintService"

Write-Host ""
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "Fix Data Root Path" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""

# Check if running as admin
$IsAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $IsAdmin) {
    Write-Error "This script must be run as Administrator"
    Write-Host ""
    Write-Host "Right-click PowerShell and select 'Run as Administrator', then run:" -ForegroundColor Yellow
    Write-Host "  cd '$PSScriptRoot'" -ForegroundColor Gray
    Write-Host "  .\fix-data-root-path.ps1" -ForegroundColor Gray
    exit 1
}

# Ensure data root directory exists
Write-Host "Ensuring data root exists: $NewDataRoot" -ForegroundColor Yellow
if (-not (Test-Path $NewDataRoot)) {
    New-Item -ItemType Directory -Path $NewDataRoot -Force | Out-Null
    Write-Host "[OK] Created directory" -ForegroundColor Green
} else {
    Write-Host "[OK] Directory exists" -ForegroundColor Green
}

# Create subdirectories
$Subdirs = @("logs", "out", "chromium-cache")
foreach ($Subdir in $Subdirs) {
    $SubdirPath = Join-Path $NewDataRoot $Subdir
    if (-not (Test-Path $SubdirPath)) {
        New-Item -ItemType Directory -Path $SubdirPath -Force | Out-Null
        Write-Host "[OK] Created $Subdir directory" -ForegroundColor Green
    }
}

Write-Host ""

# Get current service configuration
Write-Host "Getting current service configuration..." -ForegroundColor Yellow
$Service = Get-WmiObject Win32_Service | Where-Object { $_.Name -eq $ServiceName }

if (-not $Service) {
    Write-Error "Service '$ServiceName' not found"
    exit 1
}

Write-Host "Current service path: $($Service.PathName)" -ForegroundColor Gray
Write-Host ""

# Set environment variable at machine level
Write-Host "Setting SCHEDULED_PRINT_DATA_ROOT environment variable..." -ForegroundColor Yellow
[Environment]::SetEnvironmentVariable("SCHEDULED_PRINT_DATA_ROOT", $NewDataRoot, "Machine")
Write-Host "[OK] Machine environment variable set" -ForegroundColor Green

# Verify
$VerifyVar = [Environment]::GetEnvironmentVariable("SCHEDULED_PRINT_DATA_ROOT", "Machine")
Write-Host "Verified value: $VerifyVar" -ForegroundColor Gray
Write-Host ""

# Stop service
Write-Host "Stopping service..." -ForegroundColor Yellow
Stop-Service -Name $ServiceName -Force
Start-Sleep -Seconds 2
Write-Host "[OK] Service stopped" -ForegroundColor Green
Write-Host ""

# Start service
Write-Host "Starting service..." -ForegroundColor Yellow
Start-Service -Name $ServiceName
Start-Sleep -Seconds 3

$ServiceStatus = (Get-Service -Name $ServiceName).Status
if ($ServiceStatus -eq "Running") {
    Write-Host "[OK] Service started successfully" -ForegroundColor Green
} else {
    Write-Error "Service status: $ServiceStatus"
}

Write-Host ""
Write-Host "==========================================" -ForegroundColor Green
Write-Host "Configuration Updated!" -ForegroundColor Green
Write-Host "==========================================" -ForegroundColor Green
Write-Host ""
Write-Host "New data root: $NewDataRoot" -ForegroundColor Cyan
Write-Host ""
Write-Host "Logs will now be written to:" -ForegroundColor Yellow
Write-Host "  $NewDataRoot\logs\" -ForegroundColor White
Write-Host ""
Write-Host "Check logs in a few seconds:" -ForegroundColor Yellow
Write-Host "  Get-Content '$NewDataRoot\logs\scheduled-print-service-$(Get-Date -Format 'yyyyMMdd').log' -Tail 20" -ForegroundColor Gray
Write-Host ""
