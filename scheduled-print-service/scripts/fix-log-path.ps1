#Requires -RunAsAdministrator

<#
.SYNOPSIS
    Fixes log path issue by moving from network share to local path
.DESCRIPTION
    Changes appsettings.json to use local log path instead of network share
    This solves the LocalSystem account access issue
#>

param(
    [string]$ServiceName = "ScheduledPrintService",
    [string]$ExeFolder = "C:\Program Files\Malchut\ScheduledPrintService",
    [string]$NewLogPath = "C:\ProgramData\ScheduledPrintService\logs"
)

Write-Host "=== Fix Log Path for ScheduledPrintService ===" -ForegroundColor Cyan
Write-Host ""

# Check if service is running
$service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue

if ($service -and $service.Status -eq "Running") {
    Write-Host "Stopping service..." -ForegroundColor Yellow
    Stop-Service -Name $ServiceName -Force
    Start-Sleep -Seconds 2
    Write-Host "[+] Service stopped" -ForegroundColor Green
    Write-Host ""
}

# Create new log folder
Write-Host "Creating local log folder: $NewLogPath" -ForegroundColor Yellow
if (-not (Test-Path $NewLogPath)) {
    New-Item -Path $NewLogPath -ItemType Directory -Force | Out-Null
    Write-Host "[+] Log folder created" -ForegroundColor Green
} else {
    Write-Host "[+] Log folder already exists" -ForegroundColor Green
}
Write-Host ""

# Grant full permissions to SYSTEM account
Write-Host "Granting permissions to NT AUTHORITY\SYSTEM..." -ForegroundColor Yellow
icacls "$NewLogPath" /grant "NT AUTHORITY\SYSTEM:(OI)(CI)F" /T
Write-Host "[+] Permissions granted" -ForegroundColor Green
Write-Host ""

# Update appsettings.json
$appsettingsPath = Join-Path $ExeFolder "appsettings.json"

if (Test-Path $appsettingsPath) {
    Write-Host "Updating appsettings.json..." -ForegroundColor Yellow
    Write-Host "Path: $appsettingsPath" -ForegroundColor Gray

    try {
        $json = Get-Content $appsettingsPath -Raw | ConvertFrom-Json

        # Update Serilog file path
        if ($json.Serilog.WriteTo) {
            foreach ($sink in $json.Serilog.WriteTo) {
                if ($sink.Name -eq "File" -and $sink.Args.path) {
                    $oldPath = $sink.Args.path
                    $sink.Args.path = "$NewLogPath\log-.txt"
                    Write-Host "  Changed: $oldPath" -ForegroundColor Gray
                    Write-Host "  To:      $($sink.Args.path)" -ForegroundColor Green
                }
            }
        }

        # Update DataRootPath if present
        if ($json.AppSettings -and $json.AppSettings.DataRootPath) {
            $oldDataRoot = $json.AppSettings.DataRootPath
            $newDataRoot = "C:\ProgramData\ScheduledPrintService"
            $json.AppSettings.DataRootPath = $newDataRoot

            Write-Host "  Changed DataRootPath: $oldDataRoot" -ForegroundColor Gray
            Write-Host "  To:                   $newDataRoot" -ForegroundColor Green

            # Create data root folder
            if (-not (Test-Path $newDataRoot)) {
                New-Item -Path $newDataRoot -ItemType Directory -Force | Out-Null
                icacls "$newDataRoot" /grant "NT AUTHORITY\SYSTEM:(OI)(CI)F" /T
            }
        }

        # Save updated JSON
        $json | ConvertTo-Json -Depth 10 | Set-Content $appsettingsPath -Force
        Write-Host "[+] appsettings.json updated" -ForegroundColor Green

    } catch {
        Write-Host "[!] Error updating appsettings.json: $_" -ForegroundColor Red
        exit 1
    }
} else {
    Write-Host "[!] appsettings.json not found at: $appsettingsPath" -ForegroundColor Red
    exit 1
}
Write-Host ""

# Restart service
if ($service) {
    Write-Host "Starting service..." -ForegroundColor Yellow
    Start-Service -Name $ServiceName
    Start-Sleep -Seconds 2

    $service = Get-Service -Name $ServiceName
    if ($service.Status -eq "Running") {
        Write-Host "[+] Service started successfully" -ForegroundColor Green
    } else {
        Write-Host "[!] Service failed to start. Status: $($service.Status)" -ForegroundColor Red
        Write-Host "    Check Event Viewer for errors" -ForegroundColor Yellow
    }
} else {
    Write-Host "[!] Service not found. Install it first." -ForegroundColor Yellow
}
Write-Host ""

Write-Host "=== Next Steps ===" -ForegroundColor Cyan
Write-Host "1. Check if logs are being created: $NewLogPath" -ForegroundColor White
Write-Host "2. Monitor service: .\monitor-service.ps1" -ForegroundColor White
Write-Host "3. View logs: Get-Content '$NewLogPath\log-*.txt' -Tail 50" -ForegroundColor White
Write-Host ""
Write-Host "[+] Fix complete!" -ForegroundColor Green
