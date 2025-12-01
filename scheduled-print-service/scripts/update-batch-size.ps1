#Requires -Version 5.1
<#
.SYNOPSIS
    Update API batch size from 25 to a larger number

.DESCRIPTION
    Updates the DefaultRequest payload in api_config.db to increase
    the "length" parameter from 25 to a specified value.

.PARAMETER BatchSize
    New batch size (default: 100, use 999999 for "all")

.PARAMETER DatabasePath
    Path to api_config.db

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File update-batch-size.ps1 -BatchSize 100
#>

param(
    [int]$BatchSize = 100,
    [string]$DatabasePath = "C:\Program Files\Malchut\ScheduledPrintService\api_config.db"
)

Write-Host ""
Write-Host "Update API Batch Size" -ForegroundColor Cyan
Write-Host "=====================" -ForegroundColor Cyan
Write-Host ""

if (-not (Test-Path $DatabasePath)) {
    Write-Error "Database not found: $DatabasePath"
    exit 1
}

Write-Host "Database: $DatabasePath" -ForegroundColor Gray
Write-Host "New Batch Size: $BatchSize" -ForegroundColor Yellow
Write-Host ""

# Check for sqlite3
$Sqlite3 = Get-Command sqlite3 -ErrorAction SilentlyContinue
if (-not $Sqlite3) {
    Write-Error "sqlite3 not found in PATH"
    Write-Host ""
    Write-Host "Install sqlite3 first:" -ForegroundColor Yellow
    Write-Host "  powershell -ExecutionPolicy Bypass -File install-sqlite3.ps1" -ForegroundColor Gray
    exit 1
}

# Get current configuration
Write-Host "Current API configuration:" -ForegroundColor Yellow
$CurrentPayload = & sqlite3 $DatabasePath "SELECT PrimaryPayload FROM PrimaryApi WHERE ApiNumber = 1;" 2>&1

if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to query database: $CurrentPayload"
    exit 1
}

Write-Host $CurrentPayload -ForegroundColor Gray
Write-Host ""

# Check if payload contains "length"
if ($CurrentPayload -notlike "*length*") {
    Write-Error "Could not find 'length' parameter in payload"
    exit 1
}

# Parse JSON and update
try {
    $PayloadObj = $CurrentPayload | ConvertFrom-Json
    $OldLength = $PayloadObj.length

    Write-Host "Current batch size (length): $OldLength" -ForegroundColor White
    Write-Host "New batch size: $BatchSize" -ForegroundColor Green

    # Update the value
    $PayloadObj.length = $BatchSize
    $NewPayload = $PayloadObj | ConvertTo-Json -Compress -Depth 10

    # Escape single quotes for SQL
    $EscapedPayload = $NewPayload -replace "'", "''"

    # Update database
    Write-Host ""
    Write-Host "Updating database..." -ForegroundColor Yellow

    $UpdateQuery = "UPDATE PrimaryApi SET PrimaryPayload = '$EscapedPayload' WHERE ApiNumber = 1;"
    & sqlite3 $DatabasePath $UpdateQuery 2>&1

    if ($LASTEXITCODE -eq 0) {
        Write-Host "[OK] Database updated successfully" -ForegroundColor Green
    } else {
        Write-Error "Failed to update database"
        exit 1
    }

    # Verify
    Write-Host ""
    Write-Host "Verifying change..." -ForegroundColor Yellow
    $VerifyPayload = & sqlite3 $DatabasePath "SELECT PrimaryPayload FROM PrimaryApi WHERE ApiNumber = 1;" 2>&1
    $VerifyObj = $VerifyPayload | ConvertFrom-Json

    Write-Host "Verified batch size: $($VerifyObj.length)" -ForegroundColor $(if ($VerifyObj.length -eq $BatchSize) { "Green" } else { "Red" })

    Write-Host ""
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "Update Complete!" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "The service will now fetch $BatchSize records per API call" -ForegroundColor White
    Write-Host ""
    Write-Host "Next steps:" -ForegroundColor Yellow
    Write-Host "  1. Restart the service: Restart-Service 'Scheduled Print Service'" -ForegroundColor Gray
    Write-Host "  2. Or wait for next poll cycle (3600 seconds)" -ForegroundColor Gray
    Write-Host ""

} catch {
    Write-Error "Failed to update payload: $_"
    exit 1
}
