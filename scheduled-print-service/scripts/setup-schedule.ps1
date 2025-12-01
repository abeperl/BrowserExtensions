#Requires -RunAsAdministrator

<#
.SYNOPSIS
    Configures database schedules and output folder
.DESCRIPTION
    - Creates/enables Schedule 1 to run every 15 minutes
    - Attaches API 1 and API 3 to Schedule 1
    - Sets output folder to C:\ProgramData\ScheduledPrintService\out
#>

param(
    [string]$ServiceName = "ScheduledPrintService",
    [string]$ExeFolder = "C:\Program Files\Malchut\ScheduledPrintService",
    [string]$DataRoot = "C:\ProgramData\ScheduledPrintService",
    [string]$OutFolder = "C:\ProgramData\ScheduledPrintService\out"
)

Write-Host "=== Setup Schedule Configuration ===" -ForegroundColor Cyan
Write-Host ""

# Stop service
Write-Host "Stopping service..." -ForegroundColor Yellow
$service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if ($service -and $service.Status -eq "Running") {
    Stop-Service -Name $ServiceName -Force
    Start-Sleep -Seconds 2
}
Write-Host "[+] Service stopped" -ForegroundColor Green
Write-Host ""

# Create output folder
Write-Host "Creating output folder: $OutFolder" -ForegroundColor Yellow
if (-not (Test-Path $OutFolder)) {
    New-Item -Path $OutFolder -ItemType Directory -Force | Out-Null
    Write-Host "[+] Folder created" -ForegroundColor Green
} else {
    Write-Host "[+] Folder already exists" -ForegroundColor Green
}

# Grant permissions
Write-Host "Granting permissions..." -ForegroundColor Yellow
$serviceWmi = Get-WmiObject Win32_Service | Where-Object { $_.Name -eq $ServiceName }
$serviceAccount = $serviceWmi.StartName

if ($serviceAccount -like ".\*") {
    $localUser = $serviceAccount -replace '^\.\\'
    icacls "$OutFolder" /grant "${env:COMPUTERNAME}\${localUser}:(OI)(CI)F" /T | Out-Null
} else {
    icacls "$OutFolder" /grant "NT AUTHORITY\SYSTEM:(OI)(CI)F" /T | Out-Null
}
Write-Host "[+] Permissions granted" -ForegroundColor Green
Write-Host ""

# Update appsettings.json for output folder
Write-Host "Updating appsettings.json..." -ForegroundColor Yellow
$appsettingsPath = Join-Path $ExeFolder "appsettings.json"

if (Test-Path $appsettingsPath) {
    Copy-Item $appsettingsPath "$appsettingsPath.backup2" -Force

    $json = Get-Content $appsettingsPath -Raw | ConvertFrom-Json

    # Update Printer.OutputDirectory
    if ($json.Printer) {
        $oldOut = $json.Printer.OutputDirectory
        $json.Printer.OutputDirectory = $OutFolder
        Write-Host "  Updated Printer.OutputDirectory:" -ForegroundColor Gray
        Write-Host "    From: $oldOut" -ForegroundColor Red
        Write-Host "    To:   $OutFolder" -ForegroundColor Green
    }

    $json | ConvertTo-Json -Depth 10 | Set-Content $appsettingsPath -Force
    Write-Host "[+] appsettings.json updated" -ForegroundColor Green
}
Write-Host ""

# Configure database
Write-Host "Configuring database..." -ForegroundColor Yellow
$dbPath = Join-Path $ExeFolder "api_config.db"

if (-not (Test-Path $dbPath)) {
    Write-Host "[!] Database not found at: $dbPath" -ForegroundColor Red
    exit 1
}

Write-Host "  Database: $dbPath" -ForegroundColor Gray

# Create SQL commands
$sqlCommands = @"
-- Enable Schedule 1 (every 15 minutes)
INSERT OR REPLACE INTO Schedule (Id, ScheduleName, CronExpression, IsEnabled, CreatedAt, UpdatedAt)
VALUES (1, 'Every 15 Minutes', '*/15 * * * *', 1, datetime('now'), datetime('now'));

-- Link API 1 to Schedule 1
INSERT OR REPLACE INTO ScheduleApi (ScheduleId, ApiNumber, ExecutionOrder)
VALUES (1, 1, 1);

-- Link API 3 to Schedule 1
INSERT OR REPLACE INTO ScheduleApi (ScheduleId, ApiNumber, ExecutionOrder)
VALUES (1, 3, 2);

-- Show current configuration
SELECT 'Schedules:';
SELECT * FROM Schedule;

SELECT '';
SELECT 'Schedule-API Links:';
SELECT s.Id as ScheduleId, s.ScheduleName, s.CronExpression, s.IsEnabled, sa.ApiNumber, sa.ExecutionOrder
FROM Schedule s
LEFT JOIN ScheduleApi sa ON s.Id = sa.ScheduleId
WHERE s.IsEnabled = 1;

SELECT '';
SELECT 'APIs:';
SELECT * FROM PrimaryApi;
"@

# Save SQL to temp file without BOM
$sqlFile = "$env:TEMP\setup_schedule.sql"
$sqlCommands | Out-File $sqlFile -Encoding ASCII -NoNewline

# Execute SQL using sqlite3
Write-Host "  Executing SQL commands..." -ForegroundColor Gray

# Find sqlite3.exe - check exe folder first
$sqlite3Path = $null
$localSqlite = Join-Path $ExeFolder "sqlite3.exe"

if (Test-Path $localSqlite) {
    $sqlite3Path = $localSqlite
}

if (-not $sqlite3Path) {
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

if ($sqlite3Path) {
    Write-Host "  Using sqlite3 at: $sqlite3Path" -ForegroundColor Gray

    # Execute SQL
    $output = & $sqlite3Path $dbPath ".read $sqlFile" 2>&1

    Write-Host ""
    Write-Host "Database Configuration:" -ForegroundColor Cyan
    Write-Host "----------------------" -ForegroundColor Gray
    $output | Write-Host -ForegroundColor White
    Write-Host ""

    Write-Host "[+] Database configured successfully" -ForegroundColor Green
} else {
    Write-Host "[!] sqlite3.exe not found" -ForegroundColor Red
    Write-Host ""
    Write-Host "Expected location: $localSqlite" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Manual SQL to run:" -ForegroundColor Yellow
    Write-Host $sqlCommands -ForegroundColor Gray
    Write-Host ""
    Write-Host "Download sqlite3:" -ForegroundColor Yellow
    Write-Host "  https://www.sqlite.org/download.html" -ForegroundColor Gray
}

# Clean up
Remove-Item $sqlFile -Force -ErrorAction SilentlyContinue

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
}

Write-Host ""
Write-Host "=== Configuration Summary ===" -ForegroundColor Cyan
Write-Host "Schedule 1: Every 15 minutes (*/15 * * * *)" -ForegroundColor White
Write-Host "APIs: 1, 3" -ForegroundColor White
Write-Host "Output Folder: $OutFolder" -ForegroundColor White
Write-Host "Logs Folder: $DataRoot\logs" -ForegroundColor White
Write-Host ""

Write-Host "Monitor logs:" -ForegroundColor Yellow
Write-Host "  Get-Content '$DataRoot\logs\*.log' -Tail 50 -Wait" -ForegroundColor Gray
Write-Host ""

Write-Host "Check schedule status:" -ForegroundColor Yellow
Write-Host "  .\query-database.ps1" -ForegroundColor Gray
Write-Host ""
