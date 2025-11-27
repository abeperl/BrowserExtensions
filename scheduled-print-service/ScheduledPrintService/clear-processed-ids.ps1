#Requires -Version 5.1
<#
.SYNOPSIS
    Clear processed order IDs from database

.DESCRIPTION
    Clears the ProcessedItemIds table so orders can be reprocessed.
    Use with caution - this will cause all orders to be processed again.

.PARAMETER ApiNumber
    Which API to clear IDs for (default: 0 for legacy, 1 for API #1)

.PARAMETER DatabasePath
    Path to api_config.db

.PARAMETER Confirm
    Require confirmation before clearing

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File clear-processed-ids.ps1 -ApiNumber 0
#>

param(
    [int]$ApiNumber = 0,
    [string]$DatabasePath = "C:\Program Files\Malchut\ScheduledPrintService\api_config.db",
    [switch]$Confirm = $true
)

Write-Host ""
Write-Host "Clear Processed Order IDs" -ForegroundColor Cyan
Write-Host "=========================" -ForegroundColor Cyan
Write-Host ""

if (-not (Test-Path $DatabasePath)) {
    Write-Error "Database not found: $DatabasePath"
    exit 1
}

# Check for sqlite3
$Sqlite3 = Get-Command sqlite3 -ErrorAction SilentlyContinue
if (-not $Sqlite3) {
    Write-Error "sqlite3 not found in PATH"
    Write-Host ""
    Write-Host "Install sqlite3 first:" -ForegroundColor Yellow
    Write-Host "  powershell -ExecutionPolicy Bypass -File install-sqlite3.ps1" -ForegroundColor Gray
    exit 1
}

# Count current IDs
Write-Host "Checking current processed IDs..." -ForegroundColor Yellow
$CurrentCount = & sqlite3 $DatabasePath "SELECT COUNT(*) FROM ProcessedItemIds WHERE ApiNumber = $ApiNumber;" 2>&1

if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to query database: $CurrentCount"
    exit 1
}

Write-Host "Database: $DatabasePath" -ForegroundColor Gray
Write-Host "API Number: $ApiNumber" -ForegroundColor Gray
Write-Host "Current Processed IDs: $CurrentCount" -ForegroundColor Yellow
Write-Host ""

if ($CurrentCount -eq 0) {
    Write-Host "[INFO] No processed IDs to clear" -ForegroundColor Gray
    exit 0
}

# Show sample IDs
Write-Host "Sample processed IDs:" -ForegroundColor Yellow
$SampleIds = & sqlite3 $DatabasePath "SELECT ItemId FROM ProcessedItemIds WHERE ApiNumber = $ApiNumber LIMIT 10;" 2>&1
$SampleIds -split "`n" | ForEach-Object {
    if ($_) {
        Write-Host "  - $_" -ForegroundColor Gray
    }
}

if ($CurrentCount -gt 10) {
    Write-Host "  ... and $($CurrentCount - 10) more" -ForegroundColor Gray
}

Write-Host ""

# Confirmation
if ($Confirm) {
    Write-Host "WARNING: This will delete all $CurrentCount processed IDs for API #$ApiNumber" -ForegroundColor Red
    Write-Host "Orders will be reprocessed on the next poll cycle." -ForegroundColor Yellow
    Write-Host ""
    $Response = Read-Host "Are you sure you want to continue? (yes/no)"

    if ($Response -ne "yes") {
        Write-Host "Cancelled" -ForegroundColor Yellow
        exit 0
    }
}

# Delete
Write-Host ""
Write-Host "Deleting processed IDs..." -ForegroundColor Yellow

$DeleteQuery = "DELETE FROM ProcessedItemIds WHERE ApiNumber = $ApiNumber;"
& sqlite3 $DatabasePath $DeleteQuery 2>&1

if ($LASTEXITCODE -eq 0) {
    Write-Host "[OK] Deleted $CurrentCount processed IDs" -ForegroundColor Green

    # Verify
    $VerifyCount = & sqlite3 $DatabasePath "SELECT COUNT(*) FROM ProcessedItemIds WHERE ApiNumber = $ApiNumber;" 2>&1
    Write-Host "Remaining IDs: $VerifyCount" -ForegroundColor Gray
} else {
    Write-Error "Failed to delete processed IDs"
    exit 1
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "Cleared Successfully!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Orders will be reprocessed on next poll cycle" -ForegroundColor White
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "  1. Restart service to trigger immediate poll:" -ForegroundColor Gray
Write-Host "     Restart-Service 'Scheduled Print Service'" -ForegroundColor Gray
Write-Host "  2. Or wait for next automatic poll (3600 seconds)" -ForegroundColor Gray
Write-Host ""
