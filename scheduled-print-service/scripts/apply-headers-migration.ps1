# Script to apply API Headers database migration
# This adds the ApiHeaders table and populates it with existing configuration

param(
    [Parameter(Mandatory=$false)]
    [string]$DbPath = "..\publish\api_config.db"
)

$ErrorActionPreference = "Stop"

Write-Host "Applying API Headers Migration..." -ForegroundColor Cyan
Write-Host ""

# Check if database exists
if (-not (Test-Path $DbPath)) {
    Write-Host "Error: Database not found at $DbPath" -ForegroundColor Red
    exit 1
}

# Check if migration file exists
$migrationFile = Join-Path $PSScriptRoot "..\docs\schema-migration-api-headers.sql"
if (-not (Test-Path $migrationFile)) {
    Write-Host "Error: Migration file not found at $migrationFile" -ForegroundColor Red
    exit 1
}

Write-Host "Database: $DbPath" -ForegroundColor Yellow
Write-Host "Migration file: $migrationFile" -ForegroundColor Yellow
Write-Host ""

# Show current state
Write-Host "Current ApiAuth configuration:" -ForegroundColor Cyan
& sqlite3 $DbPath "SELECT BaseUrl, Username FROM ApiAuth"
Write-Host ""

# Apply migration
Write-Host "Applying migration..." -ForegroundColor Yellow
try {
    Get-Content $migrationFile | & sqlite3 $DbPath
    Write-Host "Migration applied successfully!" -ForegroundColor Green
}
catch {
    Write-Host "Error applying migration: $_" -ForegroundColor Red
    exit 1
}

Write-Host ""

# Verify results
Write-Host "Verification - ApiHeaders table contents:" -ForegroundColor Cyan
& sqlite3 $DbPath @"
SELECT
    a.BaseUrl,
    a.Username,
    h.HeaderName,
    h.HeaderValue,
    h.IsEnabled
FROM ApiAuth a
LEFT JOIN ApiHeaders h ON a.Id = h.ApiAuthId
ORDER BY a.BaseUrl, h.HeaderName;
"@

Write-Host ""
Write-Host "Migration complete!" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "  1. Review the headers above to ensure they're correct"
Write-Host "  2. Rebuild and publish the service: cd .. && powershell -File scripts/publish.ps1"
Write-Host "  3. Restart the service to apply changes"
Write-Host ""