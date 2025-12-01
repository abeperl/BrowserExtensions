#Requires -RunAsAdministrator

<#
.SYNOPSIS
    Fixes DataRoot path to use local directory instead of network share
.DESCRIPTION
    Updates appsettings.json to use C:\ProgramData\ScheduledPrintService
#>

param(
    [string]$ServiceName = "ScheduledPrintService",
    [string]$ExeFolder = "C:\Program Files\Malchut\ScheduledPrintService",
    [string]$NewDataRoot = "C:\ProgramData\ScheduledPrintService"
)

Write-Host "=== Fix DataRoot Path ===" -ForegroundColor Cyan
Write-Host ""

# Stop service if running
$service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if ($service -and $service.Status -eq "Running") {
    Write-Host "Stopping service..." -ForegroundColor Yellow
    Stop-Service -Name $ServiceName -Force
    Start-Sleep -Seconds 2
    Write-Host "[+] Service stopped" -ForegroundColor Green
}
Write-Host ""

# Create new data root folder
Write-Host "Creating local data root: $NewDataRoot" -ForegroundColor Yellow
if (-not (Test-Path $NewDataRoot)) {
    New-Item -Path $NewDataRoot -ItemType Directory -Force | Out-Null
    Write-Host "[+] Folder created" -ForegroundColor Green
} else {
    Write-Host "[+] Folder already exists" -ForegroundColor Green
}

# Create logs subfolder
$logsFolder = Join-Path $NewDataRoot "logs"
if (-not (Test-Path $logsFolder)) {
    New-Item -Path $logsFolder -ItemType Directory -Force | Out-Null
    Write-Host "[+] Logs folder created" -ForegroundColor Green
}
Write-Host ""

# Grant permissions to service account
$serviceWmi = Get-WmiObject Win32_Service | Where-Object { $_.Name -eq $ServiceName }
$serviceAccount = $serviceWmi.StartName

Write-Host "Granting permissions to service account: $serviceAccount" -ForegroundColor Yellow

if ($serviceAccount -eq "LocalSystem") {
    icacls "$NewDataRoot" /grant "NT AUTHORITY\SYSTEM:(OI)(CI)F" /T | Out-Null
    Write-Host "[+] Permissions granted to NT AUTHORITY\SYSTEM" -ForegroundColor Green
} elseif ($serviceAccount -like ".\*") {
    # Local account
    $localUser = $serviceAccount -replace '^\.\\'
    icacls "$NewDataRoot" /grant "${env:COMPUTERNAME}\${localUser}:(OI)(CI)F" /T | Out-Null
    Write-Host "[+] Permissions granted to ${env:COMPUTERNAME}\$localUser" -ForegroundColor Green
} else {
    # Domain account or other
    icacls "$NewDataRoot" /grant "${serviceAccount}:(OI)(CI)F" /T | Out-Null
    Write-Host "[+] Permissions granted to $serviceAccount" -ForegroundColor Green
}
Write-Host ""

# Update appsettings.json
$appsettingsPath = Join-Path $ExeFolder "appsettings.json"

if (Test-Path $appsettingsPath) {
    Write-Host "Backing up appsettings.json..." -ForegroundColor Yellow
    Copy-Item $appsettingsPath "$appsettingsPath.backup" -Force
    Write-Host "[+] Backup created: $appsettingsPath.backup" -ForegroundColor Green
    Write-Host ""

    Write-Host "Updating appsettings.json..." -ForegroundColor Yellow

    try {
        $json = Get-Content $appsettingsPath -Raw | ConvertFrom-Json

        # Update DataRoot
        $oldDataRoot = $json.DataRoot
        $json.DataRoot = $NewDataRoot

        Write-Host "  Changed DataRoot:" -ForegroundColor Gray
        Write-Host "    From: $oldDataRoot" -ForegroundColor Red
        Write-Host "    To:   $NewDataRoot" -ForegroundColor Green

        # Save updated JSON with proper formatting
        $jsonFormatted = $json | ConvertTo-Json -Depth 10
        $jsonFormatted | Set-Content $appsettingsPath -Force

        Write-Host "[+] appsettings.json updated" -ForegroundColor Green

    } catch {
        Write-Host "[!] Error updating appsettings.json: $_" -ForegroundColor Red
        Write-Host "    Restoring backup..." -ForegroundColor Yellow
        Copy-Item "$appsettingsPath.backup" $appsettingsPath -Force
        exit 1
    }
} else {
    Write-Host "[!] appsettings.json not found at: $appsettingsPath" -ForegroundColor Red
    exit 1
}

Write-Host ""

# Also remove SCHEDULED_PRINT_DATA_ROOT environment variable if set
Write-Host "Checking for environment variable override..." -ForegroundColor Yellow
$envVar = [System.Environment]::GetEnvironmentVariable("SCHEDULED_PRINT_DATA_ROOT", "Machine")

if ($envVar) {
    Write-Host "[!] Found SCHEDULED_PRINT_DATA_ROOT environment variable: $envVar" -ForegroundColor Yellow
    Write-Host "    Do you want to remove it? (Y/N)" -ForegroundColor Yellow
    $response = Read-Host

    if ($response -eq "Y" -or $response -eq "y") {
        [System.Environment]::SetEnvironmentVariable("SCHEDULED_PRINT_DATA_ROOT", $null, "Machine")
        Write-Host "[+] Environment variable removed" -ForegroundColor Green
    }
} else {
    Write-Host "[+] No environment variable override found" -ForegroundColor Green
}

Write-Host ""
Write-Host "=== Summary ===" -ForegroundColor Cyan
Write-Host "Data Root: $NewDataRoot" -ForegroundColor White
Write-Host "Logs will be in: $logsFolder" -ForegroundColor White
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "1. Grant 'Log on as a service' permission: .\grant-logon-as-service.ps1" -ForegroundColor White
Write-Host "2. Start the service: Start-Service $ServiceName" -ForegroundColor White
Write-Host "3. Check logs: Get-Content '$logsFolder\*.log' -Tail 50" -ForegroundColor White
Write-Host ""
