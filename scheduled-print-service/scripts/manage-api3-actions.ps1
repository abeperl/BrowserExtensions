#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Manage API 3 sub-actions (Save PDF and Print PDF)

.DESCRIPTION
    This script allows you to view and toggle the Save PDF and Print PDF sub-actions
    for API 3 (Personalized Orders API) independently.

.PARAMETER Action
    The action to perform: status, enable-save, disable-save, enable-print, disable-print,
    save-only, print-only, both, neither

.EXAMPLE
    .\manage-api3-actions.ps1 -Action status
    Shows current status of sub-actions

.EXAMPLE
    .\manage-api3-actions.ps1 -Action save-only
    Enables Save PDF, disables Print PDF

.EXAMPLE
    .\manage-api3-actions.ps1 -Action both
    Enables both Save PDF and Print PDF (default configuration)
#>

param(
    [Parameter(Mandatory=$true)]
    [ValidateSet('status', 'enable-save', 'disable-save', 'enable-print', 'disable-print', 'save-only', 'print-only', 'both', 'neither')]
    [string]$Action
)

$ErrorActionPreference = "Stop"

# Determine database path
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$dataDir = Join-Path (Split-Path -Parent $scriptDir) "data"
$dbPath = Join-Path $dataDir "api_config.db"

if (-not (Test-Path $dbPath)) {
    Write-Error "Database not found at: $dbPath"
    exit 1
}

# Function to execute SQL query
function Invoke-SqlQuery {
    param([string]$Query)
    $output = sqlite3 $dbPath $Query 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Error "SQL query failed: $output"
        exit 1
    }
    return $output
}

# Function to show status
function Show-Status {
    Write-Host "`nCurrent Status of API 3 Sub-Actions:" -ForegroundColor Cyan
    Write-Host "=" * 80 -ForegroundColor Cyan

    $query = @"
SELECT
    sa.ActionNumber || '|' ||
    sa.ActionName || '|' ||
    sa.ActionType || '|' ||
    CASE WHEN sa.IsEnabled = 1 THEN 'ENABLED' ELSE 'DISABLED' END
FROM SubAction sa
JOIN PrimaryApi pa ON sa.PrimaryApiId = pa.Id
WHERE pa.ApiNumber = 3
ORDER BY sa.ExecutionOrder;
"@

    $results = Invoke-SqlQuery $query

    Write-Host ("{0,-5} {1,-30} {2,-20} {3,-10}" -f "Order", "Name", "Type", "Status") -ForegroundColor Yellow
    Write-Host ("-" * 80) -ForegroundColor Yellow

    foreach ($line in $results) {
        $parts = $line -split '\|'
        $status = $parts[3]
        $color = if ($status -eq 'ENABLED') { 'Green' } else { 'Red' }
        Write-Host ("{0,-5} {1,-30} {2,-20} {3,-10}" -f $parts[0], $parts[1], $parts[2], $parts[3]) -ForegroundColor $color
    }

    Write-Host "`n"
}

# Execute action
switch ($Action) {
    'status' {
        Show-Status
    }
    'enable-save' {
        Invoke-SqlQuery "UPDATE SubAction SET IsEnabled = 1, UpdatedAt = CURRENT_TIMESTAMP WHERE Id = 15;"
        Write-Host "✓ Save PDF action ENABLED" -ForegroundColor Green
        Show-Status
    }
    'disable-save' {
        Invoke-SqlQuery "UPDATE SubAction SET IsEnabled = 0, UpdatedAt = CURRENT_TIMESTAMP WHERE Id = 15;"
        Write-Host "✓ Save PDF action DISABLED" -ForegroundColor Yellow
        Show-Status
    }
    'enable-print' {
        Invoke-SqlQuery "UPDATE SubAction SET IsEnabled = 1, UpdatedAt = CURRENT_TIMESTAMP WHERE Id = 16;"
        Write-Host "✓ Print PDF action ENABLED" -ForegroundColor Green
        Show-Status
    }
    'disable-print' {
        Invoke-SqlQuery "UPDATE SubAction SET IsEnabled = 0, UpdatedAt = CURRENT_TIMESTAMP WHERE Id = 16;"
        Write-Host "✓ Print PDF action DISABLED" -ForegroundColor Yellow
        Show-Status
    }
    'save-only' {
        Invoke-SqlQuery "BEGIN TRANSACTION; UPDATE SubAction SET IsEnabled = 1, UpdatedAt = CURRENT_TIMESTAMP WHERE Id = 15; UPDATE SubAction SET IsEnabled = 0, UpdatedAt = CURRENT_TIMESTAMP WHERE Id = 16; COMMIT;"
        Write-Host "✓ Configured for SAVE ONLY (Print disabled)" -ForegroundColor Green
        Show-Status
    }
    'print-only' {
        Invoke-SqlQuery "BEGIN TRANSACTION; UPDATE SubAction SET IsEnabled = 0, UpdatedAt = CURRENT_TIMESTAMP WHERE Id = 15; UPDATE SubAction SET IsEnabled = 1, UpdatedAt = CURRENT_TIMESTAMP WHERE Id = 16; COMMIT;"
        Write-Host "✓ Configured for PRINT ONLY (Save disabled)" -ForegroundColor Green
        Show-Status
    }
    'both' {
        Invoke-SqlQuery "BEGIN TRANSACTION; UPDATE SubAction SET IsEnabled = 1, UpdatedAt = CURRENT_TIMESTAMP WHERE Id = 15; UPDATE SubAction SET IsEnabled = 1, UpdatedAt = CURRENT_TIMESTAMP WHERE Id = 16; COMMIT;"
        Write-Host "✓ Configured for BOTH Save and Print" -ForegroundColor Green
        Show-Status
    }
    'neither' {
        Invoke-SqlQuery "BEGIN TRANSACTION; UPDATE SubAction SET IsEnabled = 0, UpdatedAt = CURRENT_TIMESTAMP WHERE Id = 15; UPDATE SubAction SET IsEnabled = 0, UpdatedAt = CURRENT_TIMESTAMP WHERE Id = 16; COMMIT;"
        Write-Host "✓ Both Save and Print DISABLED (Navigate only)" -ForegroundColor Yellow
        Show-Status
    }
}

Write-Host "Database: $dbPath" -ForegroundColor DarkGray
