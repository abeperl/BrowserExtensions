#Requires -RunAsAdministrator

<#
.SYNOPSIS
    Quick diagnostic for ScheduledPrintService not creating logs when running as service
.DESCRIPTION
    Identifies the root cause of service logging issues
#>

param(
    [string]$ServiceName = "ScheduledPrintService",
    [string]$NetworkLogPath = "E:\Share\server\servern\Software\ScheduledPrintService\logs",
    [string]$ExeFolder = "C:\Program Files\Malchut\ScheduledPrintService"
)

Write-Host "=== ScheduledPrintService Quick Diagnostic ===" -ForegroundColor Cyan
Write-Host ""

# Get service info
$service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue

if (-not $service) {
    Write-Host "[!] PROBLEM: Service not installed" -ForegroundColor Red
    exit 1
}

$serviceWmi = Get-WmiObject Win32_Service | Where-Object { $_.Name -eq $ServiceName }
$serviceAccount = $serviceWmi.StartName

Write-Host "Service Status: $($service.Status)" -ForegroundColor $(if ($service.Status -eq "Running") { "Green" } else { "Yellow" })
Write-Host "Service Account: $serviceAccount" -ForegroundColor Gray
Write-Host ""

# Check #1: Network share issue with LocalSystem
if ($serviceAccount -eq "LocalSystem") {
    Write-Host "[!] ROOT CAUSE IDENTIFIED: LocalSystem + Network Share" -ForegroundColor Red
    Write-Host ""
    Write-Host "The Problem:" -ForegroundColor Yellow
    Write-Host "  - Service runs as LocalSystem account" -ForegroundColor White
    Write-Host "  - Log path is on E: drive (network share)" -ForegroundColor White
    Write-Host "  - LocalSystem CANNOT access network drives/shares" -ForegroundColor White
    Write-Host "  - Service cannot write logs, appears to hang" -ForegroundColor White
    Write-Host ""
    Write-Host "The Solution:" -ForegroundColor Green
    Write-Host "  Option 1 (RECOMMENDED): Move logs to local path" -ForegroundColor White
    Write-Host "    Run: .\fix-log-path.ps1" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "  Option 2: Change service account to domain user" -ForegroundColor White
    Write-Host "    Run: sc.exe config $ServiceName obj= DOMAIN\Username password= PASSWORD" -ForegroundColor Gray
    Write-Host "    Then: Restart-Service $ServiceName" -ForegroundColor Gray
    Write-Host ""
}

# Check #2: Log folder exists
Write-Host "Checking log folder: $NetworkLogPath" -ForegroundColor Yellow

if (Test-Path $NetworkLogPath) {
    Write-Host "[+] Folder exists" -ForegroundColor Green

    # Try to write as current user
    $testFile = Join-Path $NetworkLogPath "test_$(Get-Date -Format 'yyyyMMddHHmmss').tmp"
    try {
        "test" | Out-File $testFile -Force
        Remove-Item $testFile -Force
        Write-Host "[+] Current user CAN write (that's why manual run works)" -ForegroundColor Green
    } catch {
        Write-Host "[!] Current user CANNOT write" -ForegroundColor Red
    }
} else {
    Write-Host "[!] Folder does not exist" -ForegroundColor Red
}
Write-Host ""

# Check #3: Database location
$dbPath = Join-Path $ExeFolder "api_config.db"
Write-Host "Checking database: $dbPath" -ForegroundColor Yellow

if (Test-Path $dbPath) {
    Write-Host "[+] Database exists" -ForegroundColor Green
    $dbSize = (Get-Item $dbPath).Length
    Write-Host "    Size: $dbSize bytes" -ForegroundColor Gray
} else {
    Write-Host "[!] Database not found (may cause startup failure)" -ForegroundColor Red
}
Write-Host ""

# Check #4: Recent Event Viewer logs
Write-Host "Checking Event Viewer for service errors..." -ForegroundColor Yellow

try {
    $events = Get-EventLog -LogName Application -Source $ServiceName -Newest 5 -ErrorAction SilentlyContinue

    if ($events) {
        Write-Host "[+] Found recent event log entries:" -ForegroundColor Green
        foreach ($event in $events) {
            $color = if ($event.EntryType -eq "Error") { "Red" } elseif ($event.EntryType -eq "Warning") { "Yellow" } else { "Gray" }
            Write-Host "    [$($event.TimeGenerated)] $($event.EntryType): $($event.Message.Substring(0, [Math]::Min(100, $event.Message.Length)))" -ForegroundColor $color
        }
    } else {
        Write-Host "[!] No event log entries found" -ForegroundColor Yellow
        Write-Host "    (Service may not be logging to Event Viewer)" -ForegroundColor Gray
    }
} catch {
    Write-Host "[!] Could not read event log: $_" -ForegroundColor Yellow
}
Write-Host ""

# Summary
Write-Host "=== SUMMARY ===" -ForegroundColor Cyan
Write-Host ""

if ($serviceAccount -eq "LocalSystem") {
    Write-Host "DIAGNOSIS: Network share access issue" -ForegroundColor Red
    Write-Host ""
    Write-Host "Why manual run works:" -ForegroundColor White
    Write-Host "  - Runs as YOUR user account" -ForegroundColor Gray
    Write-Host "  - Your account has access to E: drive" -ForegroundColor Gray
    Write-Host "  - Logs are created successfully" -ForegroundColor Gray
    Write-Host ""
    Write-Host "Why service doesn't work:" -ForegroundColor White
    Write-Host "  - Runs as LocalSystem account" -ForegroundColor Gray
    Write-Host "  - LocalSystem cannot access mapped drives" -ForegroundColor Gray
    Write-Host "  - Cannot write logs, service fails silently" -ForegroundColor Gray
    Write-Host ""
    Write-Host "RECOMMENDED ACTION:" -ForegroundColor Green
    Write-Host "  Run: .\fix-log-path.ps1" -ForegroundColor Cyan
    Write-Host "  This will move logs to: C:\ProgramData\ScheduledPrintService\logs" -ForegroundColor Gray
} else {
    Write-Host "Service account: $serviceAccount" -ForegroundColor White
    Write-Host "Run: .\check-service-permissions.ps1" -ForegroundColor Cyan
    Write-Host "  To check if this account has permissions" -ForegroundColor Gray
}

Write-Host ""
Write-Host "=== Diagnostic Complete ===" -ForegroundColor Cyan
