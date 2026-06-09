# Create SQLite Database Script
# This script creates the api_config.db database and initializes the schema

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$dbPath = Join-Path $scriptDir "api_config.db"
$sqlPath = Join-Path $scriptDir "api_config.sql"

Write-Host "Creating SQLite database at: $dbPath" -ForegroundColor Cyan

# Remove existing database if it exists
if (Test-Path $dbPath) {
    Write-Host "Removing existing database..." -ForegroundColor Yellow
    Remove-Item $dbPath -Force
}

# Check if SQLite is available
$sqliteCmd = Get-Command sqlite3 -ErrorAction SilentlyContinue
if (-not $sqliteCmd) {
    Write-Host "ERROR: sqlite3 is not installed or not in PATH" -ForegroundColor Red
    Write-Host "Please install SQLite3 from: https://www.sqlite.org/download.html" -ForegroundColor Yellow
    exit 1
}

# Read the SQL schema file
if (-not (Test-Path $sqlPath)) {
    Write-Host "ERROR: SQL schema file not found at: $sqlPath" -ForegroundColor Red
    exit 1
}

$sqlContent = Get-Content $sqlPath -Raw

Write-Host "Executing SQL schema..." -ForegroundColor Cyan

# Execute SQL using sqlite3 with the SQL content piped in
$sqlContent | sqlite3 $dbPath

if ($LASTEXITCODE -eq 0) {
    Write-Host "✓ Database created successfully!" -ForegroundColor Green
    
    # Verify tables were created
    $tables = sqlite3 $dbPath "SELECT name FROM sqlite_master WHERE type='table';"
    Write-Host "`nCreated tables:" -ForegroundColor Cyan
    $tables -split "`n" | ForEach-Object { Write-Host "  - $_" -ForegroundColor White }
    
    # Show database info
    $fileInfo = Get-Item $dbPath
    Write-Host "`nDatabase file: $($fileInfo.FullName)" -ForegroundColor Cyan
    Write-Host "Database size: $($fileInfo.Length) bytes" -ForegroundColor Cyan
} else {
    Write-Host "✗ Error creating database (Exit code: $LASTEXITCODE)" -ForegroundColor Red
    exit 1
}
