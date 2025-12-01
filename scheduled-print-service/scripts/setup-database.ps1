# Database Setup for Scheduled Print Service
# Creates SQLite database with schema for API configurations and schedules

param(
    [string]$DbPath = "C:\Program Files\Malchut\ScheduledPrintService\api_config.db",
    [switch]$Recreate,
    [switch]$Force
)

Write-Host "`nCreating database at: $DbPath" -ForegroundColor Cyan

# Check if sqlite3 is available (check local directory first)
$sqlite3Path = if (Test-Path ".\sqlite3.exe") { ".\sqlite3.exe" } 
               elseif (Get-Command sqlite3 -ErrorAction SilentlyContinue) { "sqlite3" }
               else { $null }

if (-not $sqlite3Path) {
    Write-Host "[X] ERROR: sqlite3 not found" -ForegroundColor Red
    Write-Host "Install with: winget install SQLite.SQLite" -ForegroundColor Yellow
    exit 1
}

# Optional recreation logic
if (Test-Path $DbPath) {
    if ($Recreate) {
        if (-not $Force) {
            Write-Host "[!] Database already exists at $DbPath" -ForegroundColor Yellow
            Write-Host "    Use -Recreate -Force to delete and recreate, or omit -Recreate to keep data." -ForegroundColor Yellow
            Write-Host "    Example: powershell -ExecutionPolicy Bypass -File setup-database.ps1 -Recreate -Force" -ForegroundColor White
            Write-Host "[OK] Skipping deletion; existing data preserved." -ForegroundColor Green
        } else {
            Write-Host "[!] FORCE RECREATE: deleting existing database..." -ForegroundColor Yellow
            Remove-Item $DbPath -Force
        }
    } else {
        Write-Host "[OK] Existing database detected; will apply CREATE TABLE IF NOT EXISTS (non-destructive)." -ForegroundColor Green
    }
}

# Create database and tables
$sql = @"
-- Legacy-compatible schema (matches data access expectations)
CREATE TABLE IF NOT EXISTS PrimaryApi (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    ApiNumber INTEGER NOT NULL,
    ApiName TEXT NOT NULL,
    BaseUrl TEXT NOT NULL,
    Endpoint TEXT NOT NULL,
    HttpMethod TEXT NOT NULL DEFAULT 'GET',
    Headers TEXT,
    Params TEXT,
    Payload TEXT,
    IsEnabled INTEGER NOT NULL DEFAULT 1,
    CreatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
);
CREATE UNIQUE INDEX IF NOT EXISTS idx_primaryapi_apinumber ON PrimaryApi(ApiNumber);

CREATE TABLE IF NOT EXISTS SubAction (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    PrimaryApiId INTEGER NOT NULL,
    ActionNumber INTEGER NOT NULL,
    ActionName TEXT NOT NULL,
    ActionType TEXT NOT NULL,
    Configuration TEXT,
    ExecutionOrder INTEGER NOT NULL DEFAULT 0,
    IsEnabled INTEGER NOT NULL DEFAULT 1,
    CreatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (PrimaryApiId) REFERENCES PrimaryApi(Id)
);
CREATE INDEX IF NOT EXISTS idx_subaction_primaryapi ON SubAction(PrimaryApiId, ExecutionOrder);

CREATE TABLE IF NOT EXISTS Schedule (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    ScheduleName TEXT NOT NULL,
    CronExpression TEXT NOT NULL,
    IsEnabled INTEGER NOT NULL DEFAULT 1,
    CreatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS ScheduleApi (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    ScheduleId INTEGER NOT NULL,
    ApiNumber INTEGER NOT NULL,
    ExecutionOrder INTEGER NOT NULL DEFAULT 1,
    FOREIGN KEY (ScheduleId) REFERENCES Schedule(Id),
    FOREIGN KEY (ApiNumber) REFERENCES PrimaryApi(ApiNumber)
);
CREATE INDEX IF NOT EXISTS idx_scheduleapi_map ON ScheduleApi(ScheduleId, ExecutionOrder);

CREATE TABLE IF NOT EXISTS ProcessedOrder (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    ApiNumber INTEGER NOT NULL,
    OrderId TEXT NOT NULL,
    ProcessedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(ApiNumber, OrderId)
);
CREATE INDEX IF NOT EXISTS idx_processedorder_api ON ProcessedOrder(ApiNumber, OrderId);
"@

# Execute SQL
$sql | & $sqlite3Path $DbPath

if ($LASTEXITCODE -eq 0) {
    Write-Host "[OK] Database schema ensured successfully" -ForegroundColor Green
    Write-Host "`nNext steps:" -ForegroundColor Cyan
    Write-Host "  - Restore data from backup file (restore-db-from-source.ps1) if needed" -ForegroundColor White
    Write-Host "  - Check status: .\check-db-status.ps1`n" -ForegroundColor White
} else {
    Write-Host "[X] ERROR: Failed to apply schema" -ForegroundColor Red
    exit 1
}
