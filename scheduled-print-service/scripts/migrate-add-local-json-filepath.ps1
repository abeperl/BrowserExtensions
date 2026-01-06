# Migration Script: Add LocalJsonFilePath column to PrimaryApi table
# This script safely adds the column only if it doesn't already exist

param(
    [string]$DatabasePath = ".\api_config.db"
)

# Resolve the database path
$dbPath = Resolve-Path $DatabasePath -ErrorAction SilentlyContinue
if (-not $dbPath) {
    Write-Error "Database file not found: $DatabasePath"
    exit 1
}

Write-Host "Migrating database: $dbPath" -ForegroundColor Cyan

# Check if sqlite3 is available
$sqlite3 = Get-Command sqlite3 -ErrorAction SilentlyContinue
if (-not $sqlite3) {
    Write-Error "sqlite3 command not found. Please install SQLite or add it to your PATH."
    Write-Host "You can download SQLite from: https://www.sqlite.org/download.html" -ForegroundColor Yellow
    exit 1
}

# Check if column already exists
$checkQuery = "PRAGMA table_info(PrimaryApi);"
$columns = sqlite3 $dbPath $checkQuery

$hasLocalJsonFilePath = $columns | Where-Object { $_ -match "\|LocalJsonFilePath\|" }

if ($hasLocalJsonFilePath) {
    Write-Host "Column 'LocalJsonFilePath' already exists. No migration needed." -ForegroundColor Green
    exit 0
}

# Add the column
Write-Host "Adding 'LocalJsonFilePath' column to PrimaryApi table..." -ForegroundColor Yellow

$alterQuery = "ALTER TABLE PrimaryApi ADD COLUMN LocalJsonFilePath TEXT;"
sqlite3 $dbPath $alterQuery

if ($LASTEXITCODE -eq 0) {
    Write-Host "Migration completed successfully!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Usage example:" -ForegroundColor Cyan
    Write-Host "  UPDATE PrimaryApi SET LocalJsonFilePath = 'data/orders.json' WHERE ApiNumber = 1;" -ForegroundColor White
} else {
    Write-Error "Migration failed with exit code: $LASTEXITCODE"
    exit 1
}