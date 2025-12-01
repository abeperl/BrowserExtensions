# Check database for schedules and API configurations
param(
    [string]$DatabasePath = "E:\Share\server\servern\Software\ScheduledPrintService\api_config.db"
)

Write-Host "Checking database: $DatabasePath" -ForegroundColor Cyan

if (-not (Test-Path $DatabasePath)) {
    Write-Warning "Database not found at: $DatabasePath"
    Write-Host "Looking in alternate locations..." -ForegroundColor Yellow
    
    $altPaths = @(
        "C:\Program Files\Malchut\ScheduledPrintService\api_config.db",
        "$env:ProgramData\ScheduledPrintService\api_config.db",
        ".\api_config.db"
    )
    
    foreach ($path in $altPaths) {
        if (Test-Path $path) {
            Write-Host "Found database at: $path" -ForegroundColor Green
            $DatabasePath = $path
            break
        }
    }
    
    if (-not (Test-Path $DatabasePath)) {
        Write-Error "Database file not found in any location"
        exit 1
    }
}

# Load SQLite assembly
Add-Type -Path "C:\Windows\Microsoft.NET\assembly\GAC_MSIL\Microsoft.Data.Sqlite\v4.0_8.0.0.0__adb9793829ddae60\Microsoft.Data.Sqlite.dll" -ErrorAction SilentlyContinue

# Query using sqlite3 command-line if available, otherwise use .NET
if (Get-Command sqlite3 -ErrorAction SilentlyContinue) {
    Write-Host "`n=== Schedules ===" -ForegroundColor Cyan
    sqlite3 $DatabasePath "SELECT * FROM Schedule;"
    
    Write-Host "`n=== Schedule-API Mappings ===" -ForegroundColor Cyan
    sqlite3 $DatabasePath "SELECT * FROM ScheduleApi;"
    
    Write-Host "`n=== Primary APIs ===" -ForegroundColor Cyan
    sqlite3 $DatabasePath "SELECT ApiNumber, ApiName, IsEnabled FROM PrimaryApi;"
} else {
    Write-Host "`nUsing PowerShell to query database..." -ForegroundColor Yellow
    
    # Simple text-based query
    Write-Host "`n=== Checking Tables ===" -ForegroundColor Cyan
    $tables = @("Schedule", "ScheduleApi", "PrimaryApi")
    
    foreach ($table in $tables) {
        Write-Host "`nTable: $table" -ForegroundColor Yellow
        # Use basic SQLite query via .NET if possible, or recommend manual check
        Write-Host "  (Install sqlite3 command-line tool for detailed output)"
    }
    
    Write-Host "`nTo install sqlite3: winget install SQLite.SQLite" -ForegroundColor Cyan
}

Write-Host "`n=== Summary ===" -ForegroundColor Cyan
Write-Host "Database location: $DatabasePath"
Write-Host "To add schedules, run the provided SQL scripts or use the database management tools"
