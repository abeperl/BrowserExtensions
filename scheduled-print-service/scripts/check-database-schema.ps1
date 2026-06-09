#Requires -RunAsAdministrator

<#
.SYNOPSIS
    Checks database schema and tables
.DESCRIPTION
    Shows what tables exist in the database
#>

param(
    [string]$DbPath = "C:\Program Files\Malchut\ScheduledPrintService\api_config.db"
)

Write-Host "=== Database Schema Check ===" -ForegroundColor Cyan
Write-Host ""

if (-not (Test-Path $DbPath)) {
    Write-Host "[!] Database not found at: $DbPath" -ForegroundColor Red
    exit 1
}

Write-Host "Database: $DbPath" -ForegroundColor Gray
Write-Host ""

# Find sqlite3.exe
$exeDir = Split-Path -Parent $DbPath
$localSqlite = Join-Path $exeDir "sqlite3.exe"
$sqlite3Path = $null

if (Test-Path $localSqlite) {
    $sqlite3Path = $localSqlite
}

if (-not $sqlite3Path) {
    Write-Host "[!] sqlite3.exe not found at: $localSqlite" -ForegroundColor Red
    exit 1
}

Write-Host "Using: $sqlite3Path" -ForegroundColor Gray
Write-Host ""

# Check tables
Write-Host "=== Tables ===" -ForegroundColor Cyan
& $sqlite3Path $DbPath ".tables"
Write-Host ""

# Check schema
Write-Host "=== Schema ===" -ForegroundColor Cyan
& $sqlite3Path $DbPath ".schema"
Write-Host ""

# Count records in each table
Write-Host "=== Record Counts ===" -ForegroundColor Cyan
$tables = & $sqlite3Path $DbPath ".tables" | Out-String
$tableList = $tables -split '\s+' | Where-Object { $_ -ne '' }

foreach ($table in $tableList) {
    $count = & $sqlite3Path $DbPath "SELECT COUNT(*) FROM $table;"
    Write-Host "$table : $count rows" -ForegroundColor Gray
}

Write-Host ""
