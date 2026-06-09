#Requires -Version 5.1
<#
.SYNOPSIS
    Run Scheduled Print Service manually in console mode

.DESCRIPTION
    Stops the Windows service and runs the service executable in console mode
    so you can see real-time console output and verify logging

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File run-service-console.ps1
#>

param(
    [string]$ServicePath = "C:\Program Files\Malchut\ScheduledPrintService"
)

Write-Host ""
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "Run Service in Console Mode" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""

# Check if service path exists
if (-not (Test-Path $ServicePath)) {
    Write-Error "Service path not found: $ServicePath"
    exit 1
}

$ExePath = Join-Path $ServicePath "ScheduledPrintService.exe"
if (-not (Test-Path $ExePath)) {
    Write-Error "Service executable not found: $ExePath"
    exit 1
}

# Stop the Windows service if running
Write-Host "[1/3] Checking Windows service status..." -ForegroundColor Yellow
$Service = Get-Service -Name "Scheduled Print Service" -ErrorAction SilentlyContinue

if ($Service) {
    if ($Service.Status -eq "Running") {
        Write-Host "  Stopping Windows service..." -ForegroundColor Gray
        Stop-Service -Name "Scheduled Print Service" -Force
        Start-Sleep -Seconds 2
        Write-Host "  [OK] Service stopped" -ForegroundColor Green
    } else {
        Write-Host "  Service is not running" -ForegroundColor Gray
    }
} else {
    Write-Host "  Service not installed (OK for testing)" -ForegroundColor Gray
}
Write-Host ""

# Display log folder location
Write-Host "[2/3] Checking log folder..." -ForegroundColor Yellow
$DataRoot = [Environment]::GetEnvironmentVariable("SCHEDULED_PRINT_DATA_ROOT", "Machine")
if ([string]::IsNullOrWhiteSpace($DataRoot)) {
    $DataRoot = "E:\Share\server\servern\Software\ScheduledPrintService"
}
$LogFolder = Join-Path $DataRoot "logs"
Write-Host "  Data Root: $DataRoot" -ForegroundColor Gray
Write-Host "  Log Folder: $LogFolder" -ForegroundColor Gray

if (Test-Path $LogFolder) {
    $LogCount = (Get-ChildItem $LogFolder -Filter "*.log" | Measure-Object).Count
    Write-Host "  [OK] Folder exists ($LogCount log file(s))" -ForegroundColor Green
} else {
    Write-Host "  [WARNING] Folder does not exist - will be created on startup" -ForegroundColor Yellow
}
Write-Host ""

# Run the service in console mode
Write-Host "[3/3] Starting service in console mode..." -ForegroundColor Yellow
Write-Host ""
Write-Host "==========================================" -ForegroundColor Green
Write-Host "Service Output (Press Ctrl+C to stop)" -ForegroundColor Green
Write-Host "==========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Watching for logs in: $LogFolder" -ForegroundColor Cyan
Write-Host ""

# Change to service directory so it can find appsettings.json
Push-Location $ServicePath

try {
    # Run the executable
    & $ExePath
} catch {
    Write-Host ""
    Write-Error "Error running service: $_"
} finally {
    Pop-Location
}

Write-Host ""
Write-Host "Service stopped." -ForegroundColor Yellow
Write-Host ""

# Check if logs were created
if (Test-Path $LogFolder) {
    $NewLogCount = (Get-ChildItem $LogFolder -Filter "*.log" | Measure-Object).Count
    Write-Host "Log files in $LogFolder : $NewLogCount" -ForegroundColor Cyan

    $TodayLog = Get-ChildItem $LogFolder -Filter "scheduled-print-service-*.log" |
                Sort-Object LastWriteTime -Descending |
                Select-Object -First 1

    if ($TodayLog) {
        Write-Host ""
        Write-Host "Most recent log file: $($TodayLog.Name)" -ForegroundColor Cyan
        Write-Host "Last modified: $($TodayLog.LastWriteTime)" -ForegroundColor Gray
        Write-Host "Size: $([Math]::Round($TodayLog.Length / 1KB, 2)) KB" -ForegroundColor Gray
        Write-Host ""
        Write-Host "To view the log:" -ForegroundColor Yellow
        Write-Host "  notepad `"$($TodayLog.FullName)`"" -ForegroundColor White
    }
}

Write-Host ""
Write-Host "To restart the Windows service:" -ForegroundColor Yellow
Write-Host "  Start-Service -Name 'Scheduled Print Service'" -ForegroundColor White
Write-Host ""
