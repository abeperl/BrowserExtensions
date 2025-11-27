#Requires -RunAsAdministrator

<#
.SYNOPSIS
    Checks and removes SCHEDULED_PRINT_DATA_ROOT environment variable
.DESCRIPTION
    The application reads SCHEDULED_PRINT_DATA_ROOT which overrides appsettings.json
#>

param(
    [string]$ServiceName = "ScheduledPrintService"
)

Write-Host "=== Check Environment Variable Override ===" -ForegroundColor Cyan
Write-Host ""

# Check Machine-level environment variable
Write-Host "Checking SCHEDULED_PRINT_DATA_ROOT environment variable..." -ForegroundColor Yellow

$machineVar = [System.Environment]::GetEnvironmentVariable("SCHEDULED_PRINT_DATA_ROOT", "Machine")
$userVar = [System.Environment]::GetEnvironmentVariable("SCHEDULED_PRINT_DATA_ROOT", "User")
$processVar = [System.Environment]::GetEnvironmentVariable("SCHEDULED_PRINT_DATA_ROOT", "Process")

if ($machineVar) {
    Write-Host "[!] Found MACHINE-level variable:" -ForegroundColor Red
    Write-Host "    Value: $machineVar" -ForegroundColor Gray
    Write-Host "    This OVERRIDES appsettings.json!" -ForegroundColor Yellow
    Write-Host ""

    Write-Host "Removing machine-level variable..." -ForegroundColor Yellow
    [System.Environment]::SetEnvironmentVariable("SCHEDULED_PRINT_DATA_ROOT", $null, "Machine")
    Write-Host "[+] Machine-level variable removed" -ForegroundColor Green
    $removed = $true
}

if ($userVar) {
    Write-Host "[!] Found USER-level variable:" -ForegroundColor Red
    Write-Host "    Value: $userVar" -ForegroundColor Gray
    Write-Host ""

    Write-Host "Removing user-level variable..." -ForegroundColor Yellow
    [System.Environment]::SetEnvironmentVariable("SCHEDULED_PRINT_DATA_ROOT", $null, "User")
    Write-Host "[+] User-level variable removed" -ForegroundColor Green
    $removed = $true
}

if ($processVar) {
    Write-Host "[!] Process-level variable exists:" -ForegroundColor Yellow
    Write-Host "    Value: $processVar" -ForegroundColor Gray
    Write-Host "    (This is just for current process)" -ForegroundColor Gray
}

if (-not $machineVar -and -not $userVar) {
    Write-Host "[+] No SCHEDULED_PRINT_DATA_ROOT environment variable found" -ForegroundColor Green
    Write-Host "    appsettings.json should be used" -ForegroundColor Gray
}

Write-Host ""

# If we removed variables, restart the service
if ($removed) {
    Write-Host "Environment variable removed. Service needs restart to pick up changes." -ForegroundColor Yellow
    Write-Host ""

    $service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
    if ($service) {
        Write-Host "Restarting service..." -ForegroundColor Yellow

        if ($service.Status -eq "Running") {
            Stop-Service -Name $ServiceName -Force
            Start-Sleep -Seconds 2
        }

        Start-Service -Name $ServiceName
        Start-Sleep -Seconds 3

        $service = Get-Service -Name $ServiceName

        if ($service.Status -eq "Running") {
            Write-Host "[+] Service restarted successfully" -ForegroundColor Green
        } else {
            Write-Host "[!] Service failed to start. Status: $($service.Status)" -ForegroundColor Red
        }
    }
}

Write-Host ""
Write-Host "=== Current Configuration ===" -ForegroundColor Cyan

# Show appsettings.json DataRoot
$appsettingsPath = "C:\Program Files\Malchut\ScheduledPrintService\appsettings.json"
if (Test-Path $appsettingsPath) {
    $json = Get-Content $appsettingsPath -Raw | ConvertFrom-Json
    Write-Host "appsettings.json DataRoot: $($json.DataRoot)" -ForegroundColor White
}

Write-Host "Expected logs location: C:\ProgramData\ScheduledPrintService\logs" -ForegroundColor White
Write-Host ""

# Check if logs are being created
Write-Host "Waiting 5 seconds to see if logs are created..." -ForegroundColor Yellow
Start-Sleep -Seconds 5

$logsPath = "C:\ProgramData\ScheduledPrintService\logs"
$logFiles = Get-ChildItem -Path $logsPath -Filter "*.log" -ErrorAction SilentlyContinue

if ($logFiles) {
    Write-Host "[+] Log files found!" -ForegroundColor Green
    foreach ($file in $logFiles) {
        Write-Host "    $($file.Name) - Size: $($file.Length) bytes - Modified: $($file.LastWriteTime)" -ForegroundColor Gray
    }
    Write-Host ""
    Write-Host "View logs with:" -ForegroundColor Yellow
    Write-Host "  Get-Content '$logsPath\*.log' -Tail 50" -ForegroundColor Gray
} else {
    Write-Host "[!] No log files found yet" -ForegroundColor Red
    Write-Host ""
    Write-Host "Troubleshooting:" -ForegroundColor Yellow
    Write-Host "1. Check if service is actually running:" -ForegroundColor White
    Write-Host "   Get-Service ScheduledPrintService" -ForegroundColor Gray
    Write-Host ""
    Write-Host "2. Check Event Viewer for errors:" -ForegroundColor White
    Write-Host "   Get-EventLog -LogName Application -Source ScheduledPrintService -Newest 5" -ForegroundColor Gray
    Write-Host ""
    Write-Host "3. Try running manually to see console output:" -ForegroundColor White
    Write-Host "   cd 'C:\Program Files\Malchut\ScheduledPrintService'" -ForegroundColor Gray
    Write-Host "   .\ScheduledPrintService.exe" -ForegroundColor Gray
}

Write-Host ""
