#Requires -RunAsAdministrator

<#
.SYNOPSIS
    Views current database configuration
.DESCRIPTION
    Shows schedules, APIs, and their relationships
#>

param(
    [string]$DbPath = "C:\Program Files\Malchut\ScheduledPrintService\api_config.db"
)

Write-Host "=== Database Configuration ===" -ForegroundColor Cyan
Write-Host ""

if (-not (Test-Path $DbPath)) {
    Write-Host "[!] Database not found at: $DbPath" -ForegroundColor Red
    exit 1
}

Write-Host "Database: $DbPath" -ForegroundColor Gray
Write-Host ""

# Create SQL query
$sql = @"
.mode column
.headers on
.width 5 30 20 10 20

-- Show all schedules
SELECT '=== SCHEDULES ===';
SELECT Id, ScheduleName, CronExpression, IsEnabled, UpdatedAt FROM Schedule;

-- Show all APIs
SELECT '';
SELECT '=== APIS ===';
SELECT Id, ApiNumber, ApiName, BaseUrl, Endpoint, IsEnabled FROM PrimaryApi;

-- Show schedule-API relationships
SELECT '';
SELECT '=== SCHEDULE-API LINKS ===';
SELECT
    s.Id as ScheduleId,
    s.ScheduleName,
    s.CronExpression,
    s.IsEnabled as SchedEnabled,
    sa.ApiNumber,
    a.ApiName,
    a.IsEnabled as ApiEnabled
FROM Schedule s
LEFT JOIN ScheduleApi sa ON s.Id = sa.ScheduleId
LEFT JOIN PrimaryApi a ON sa.ApiNumber = a.ApiNumber
ORDER BY s.Id, sa.ApiNumber;

-- Show enabled schedules only
SELECT '';
SELECT '=== ACTIVE CONFIGURATION ===';
SELECT
    s.Id as ScheduleId,
    s.ScheduleName,
    s.CronExpression,
    COUNT(sa.ApiNumber) as ApiCount
FROM Schedule s
LEFT JOIN ScheduleApi sa ON s.Id = sa.ScheduleId
WHERE s.IsEnabled = 1
GROUP BY s.Id, s.ScheduleName, s.CronExpression;
"@

# Save to temp file without BOM
$sqlFile = "$env:TEMP\view_config.sql"
$sql | Out-File $sqlFile -Encoding ASCII -NoNewline

# Find sqlite3 - check current directory first
$exeDir = Split-Path -Parent $DbPath
$sqlite3Path = $null

# Check if sqlite3.exe is in the same folder as the database
$localSqlite = Join-Path $exeDir "sqlite3.exe"
if (Test-Path $localSqlite) {
    $sqlite3Path = $localSqlite
}

if (-not $sqlite3Path) {
    # Try other common locations
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
    Write-Host "Using: $sqlite3Path" -ForegroundColor Gray
    Write-Host ""
    & $sqlite3Path $DbPath ".read $sqlFile" 2>&1
} else {
    Write-Host "[!] sqlite3.exe not found" -ForegroundColor Red
    Write-Host ""
    Write-Host "Expected location: $localSqlite" -ForegroundColor Yellow
    Write-Host "Or download from: https://www.sqlite.org/download.html" -ForegroundColor Gray
}

# Clean up
Remove-Item $sqlFile -Force -ErrorAction SilentlyContinue

Write-Host ""
