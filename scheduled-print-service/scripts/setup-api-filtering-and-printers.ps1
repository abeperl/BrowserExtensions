# Setup script for API Filtering and Per-API Printer Configuration
# Applies database migration and verifies configuration

param(
    [Parameter(Mandatory=$false)]
    [string]$DatabasePath = ".\api_config.db",

    [Parameter(Mandatory=$false)]
    [switch]$ApplyMigration,

    [Parameter(Mandatory=$false)]
    [switch]$VerifySetup,

    [Parameter(Mandatory=$false)]
    [switch]$EnableApi2Schedule,

    [Parameter(Mandatory=$false)]
    [switch]$EnableApi4Schedule,

    [Parameter(Mandatory=$false)]
    [switch]$TestApi2,

    [Parameter(Mandatory=$false)]
    [switch]$TestApi4,

    [Parameter(Mandatory=$false)]
    [string]$Api4Printer = "4301",

    [Parameter(Mandatory=$false)]
    [switch]$BackupDatabase
)

$ErrorActionPreference = "Stop"

# Colors
$ColorInfo = "Cyan"
$ColorSuccess = "Green"
$ColorWarning = "Yellow"
$ColorError = "Red"

# Check if sqlite3 is available
$sqlite3Path = $null
if (Test-Path ".\sqlite3.exe") {
    $sqlite3Path = ".\sqlite3.exe"
} elseif (Get-Command sqlite3 -ErrorAction SilentlyContinue) {
    $sqlite3Path = "sqlite3"
} else {
    Write-Host "ERROR: sqlite3 not found. Please install SQLite3 command-line tools or place sqlite3.exe in the current directory." -ForegroundColor $ColorError
    exit 1
}

Write-Host "`nAPI Filtering and Per-API Printer Setup Script" -ForegroundColor $ColorInfo
Write-Host "=============================================" -ForegroundColor $ColorInfo
Write-Host ""

# Check if database exists
if (-not (Test-Path $DatabasePath)) {
    Write-Host "ERROR: Database not found at: $DatabasePath" -ForegroundColor $ColorError
    exit 1
}

Write-Host "Using database: $DatabasePath" -ForegroundColor $ColorSuccess
Write-Host ""

# Function to execute SQL and display results
function Invoke-Sqlite {
    param(
        [string]$Query,
        [switch]$ShowResults
    )

    $output = & $sqlite3Path $DatabasePath $Query 2>&1

    if ($LASTEXITCODE -ne 0) {
        Write-Host "ERROR: SQL execution failed: $output" -ForegroundColor $ColorError
        return $false
    }

    if ($ShowResults -and $output) {
        Write-Host $output
    }

    return $true
}

# 1. Backup Database
if ($BackupDatabase) {
    Write-Host "Creating database backup..." -ForegroundColor $ColorWarning

    $timestamp = Get-Date -Format 'yyyyMMdd_HHmmss'
    $backupPath = "$DatabasePath.backup.$timestamp"

    Copy-Item $DatabasePath $backupPath

    if (Test-Path $backupPath) {
        Write-Host "[OK] Backup created: $backupPath" -ForegroundColor $ColorSuccess
    } else {
        Write-Host "ERROR: Backup failed!" -ForegroundColor $ColorError
        exit 1
    }
    Write-Host ""
}

# 2. Apply Migration
if ($ApplyMigration) {
    Write-Host "Applying database migration..." -ForegroundColor $ColorWarning

    if (-not (Test-Path ".\update-api-2-and-create-api-4.sql")) {
        Write-Host "ERROR: Migration script not found: .\update-api-2-and-create-api-4.sql" -ForegroundColor $ColorError
        exit 1
    }

    # Step 1: Add PrinterName column if it doesn't exist
    Write-Host "Checking if PrinterName column needs to be added..." -ForegroundColor $ColorInfo
    $tableInfo = & $sqlite3Path $DatabasePath "PRAGMA table_info(PrimaryApi);"
    $columnExists = $false
    foreach ($line in $tableInfo) {
        if ($line -match "PrinterName") {
            $columnExists = $true
            break
        }
    }

    if (-not $columnExists) {
        Write-Host "Adding PrinterName column..." -ForegroundColor $ColorInfo
        & $sqlite3Path $DatabasePath "ALTER TABLE PrimaryApi ADD COLUMN PrinterName TEXT;"
        if ($LASTEXITCODE -eq 0) {
            Write-Host "[OK] PrinterName column added" -ForegroundColor $ColorSuccess
        } else {
            Write-Host "ERROR: Failed to add PrinterName column" -ForegroundColor $ColorError
            exit 1
        }
    } else {
        Write-Host "[OK] PrinterName column already exists" -ForegroundColor $ColorSuccess
    }

    # Step 2: Execute migration
    Write-Host "Executing SQL migration..." -ForegroundColor $ColorInfo
    Get-Content ".\update-api-2-and-create-api-4.sql" -Raw | & $sqlite3Path $DatabasePath

    if ($LASTEXITCODE -eq 0) {
        Write-Host "[OK] Migration applied successfully!" -ForegroundColor $ColorSuccess
    } else {
        Write-Host "ERROR: Migration failed!" -ForegroundColor $ColorError
        exit 1
    }
    Write-Host ""
}

# 3. Verify Setup
if ($VerifySetup) {
    Write-Host "Verifying configuration..." -ForegroundColor $ColorWarning
    Write-Host ""

    # Check PrinterName column exists
    Write-Host "Checking PrinterName column..." -ForegroundColor $ColorInfo
    $sql = "PRAGMA table_info(PrimaryApi);"
    $tableInfo = & $sqlite3Path $DatabasePath $sql
    if ($tableInfo -like "*PrinterName*") {
        Write-Host "[OK] PrinterName column exists" -ForegroundColor $ColorSuccess
    } else {
        Write-Host "[ERROR] PrinterName column NOT found!" -ForegroundColor $ColorError
    }
    Write-Host ""

    # Check API 2 Configuration
    Write-Host "API 2 Configuration:" -ForegroundColor $ColorInfo
    $sql = "SELECT ApiNumber, ApiName, PrinterName, IsEnabled FROM PrimaryApi WHERE ApiNumber = 2;"
    Invoke-Sqlite -Query $sql -ShowResults
    Write-Host ""

    # Check API 2 Filter
    Write-Host "API 2 Filter Configuration:" -ForegroundColor $ColorInfo
    $sql = @'
SELECT s.ActionNumber, s.ActionName, s.ActionType,
       json_extract(s.Configuration, '$.ChainedFilterArrayIndex') AS FilterIndex,
       json_extract(s.Configuration, '$.ChainedFilterType') AS FilterType,
       json_extract(s.Configuration, '$.ChainedFilterValue') AS FilterValue
FROM SubAction s
JOIN PrimaryApi p ON s.PrimaryApiId = p.Id
WHERE p.ApiNumber = 2 AND s.ActionType = 'NavigateOnly';
'@
    Invoke-Sqlite -Query $sql -ShowResults
    Write-Host ""

    # Check API 4 Configuration
    Write-Host "API 4 Configuration:" -ForegroundColor $ColorInfo
    $sql = "SELECT ApiNumber, ApiName, PrinterName, IsEnabled FROM PrimaryApi WHERE ApiNumber = 4;"
    Invoke-Sqlite -Query $sql -ShowResults
    Write-Host ""

    # Check API 4 Filter
    Write-Host "API 4 Filter Configuration:" -ForegroundColor $ColorInfo
    $sql = @'
SELECT s.ActionNumber, s.ActionName, s.ActionType,
       json_extract(s.Configuration, '$.ChainedFilterArrayIndex') AS FilterIndex,
       json_extract(s.Configuration, '$.ChainedFilterType') AS FilterType,
       json_extract(s.Configuration, '$.ChainedFilterValue') AS FilterValue
FROM SubAction s
JOIN PrimaryApi p ON s.PrimaryApiId = p.Id
WHERE p.ApiNumber = 4 AND s.ActionType = 'NavigateOnly';
'@
    Invoke-Sqlite -Query $sql -ShowResults
    Write-Host ""

    # Check Schedules
    Write-Host "Schedule Configuration:" -ForegroundColor $ColorInfo
    $sql = @"
SELECT s.ScheduleName, sa.ApiNumber, s.CronExpression, s.IsEnabled FROM Schedule s JOIN ScheduleApi sa ON s.Id = sa.ScheduleId WHERE sa.ApiNumber IN (2, 4) ORDER BY sa.ApiNumber;
"@
    Invoke-Sqlite -Query $sql -ShowResults
    Write-Host ""

    Write-Host "[OK] Verification complete" -ForegroundColor $ColorSuccess
    Write-Host ""
}

# 4. Enable API 2 Schedule
if ($EnableApi2Schedule) {
    Write-Host "Enabling API 2 schedule..." -ForegroundColor $ColorWarning

    $sql = "UPDATE Schedule SET IsEnabled = 1 WHERE Id IN (SELECT ScheduleId FROM ScheduleApi WHERE ApiNumber = 2);"

    if (Invoke-Sqlite -Query $sql) {
        Write-Host "[OK] API 2 schedule enabled" -ForegroundColor $ColorSuccess
        Write-Host ""
        Write-Host "IMPORTANT: Restart the ScheduledPrintService Windows service for changes to take effect." -ForegroundColor $ColorWarning
        Write-Host "  Restart-Service -Name 'ScheduledPrintService'" -ForegroundColor $ColorInfo
    }
    Write-Host ""
}

# 5. Enable API 4 Schedule
if ($EnableApi4Schedule) {
    Write-Host "Enabling API 4 schedule..." -ForegroundColor $ColorWarning

    $sql = "UPDATE Schedule SET IsEnabled = 1 WHERE ScheduleName = 'Picklist Non-SS Print Schedule';"

    if (Invoke-Sqlite -Query $sql) {
        Write-Host "[OK] API 4 schedule enabled" -ForegroundColor $ColorSuccess
        Write-Host ""
        Write-Host "IMPORTANT: Restart the ScheduledPrintService Windows service for changes to take effect." -ForegroundColor $ColorWarning
        Write-Host "  Restart-Service -Name 'ScheduledPrintService'" -ForegroundColor $ColorInfo
    }
    Write-Host ""
}

# 6. Test API 2
if ($TestApi2) {
    Write-Host "Testing API 2 (SS items)..." -ForegroundColor $ColorWarning
    Write-Host "This will run the service once in manual mode." -ForegroundColor $ColorInfo
    Write-Host ""

    # Check if we can run dotnet
    $dotnetPath = Get-Command dotnet -ErrorAction SilentlyContinue
    if ($dotnetPath) {
        Write-Host "Running: dotnet run --api-number 2" -ForegroundColor $ColorInfo
        Write-Host "Press Ctrl+C to stop" -ForegroundColor $ColorWarning
        Write-Host ""
        & dotnet run --api-number 2
    } else {
        Write-Host "dotnet command not found. Please run manually:" -ForegroundColor $ColorWarning
        Write-Host "  dotnet run --api-number 2" -ForegroundColor $ColorInfo
    }
    Write-Host ""
}

# 7. Test API 4
if ($TestApi4) {
    Write-Host "Testing API 4 (Non-SS items, printer $Api4Printer)..." -ForegroundColor $ColorWarning
    Write-Host "This will run the service once in manual mode." -ForegroundColor $ColorInfo
    Write-Host ""

    # Check if printer exists
    $printers = Get-Printer -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Name
    if ($printers -notcontains $Api4Printer) {
        Write-Host "WARNING: Printer '$Api4Printer' not found on this system!" -ForegroundColor $ColorWarning
        Write-Host "Available printers:" -ForegroundColor $ColorInfo
        Get-Printer | Select-Object Name, DriverName | Format-Table
        Write-Host ""
        $continue = Read-Host "Continue anyway? (y/n)"
        if ($continue -ne 'y') {
            Write-Host "Test cancelled." -ForegroundColor $ColorWarning
            exit 0
        }
    }

    # Check if we can run dotnet
    $dotnetPath = Get-Command dotnet -ErrorAction SilentlyContinue
    if ($dotnetPath) {
        Write-Host "Running: dotnet run --api-number 4" -ForegroundColor $ColorInfo
        Write-Host "Press Ctrl+C to stop" -ForegroundColor $ColorWarning
        Write-Host ""
        & dotnet run --api-number 4
    } else {
        Write-Host "dotnet command not found. Please run manually:" -ForegroundColor $ColorWarning
        Write-Host "  dotnet run --api-number 4" -ForegroundColor $ColorInfo
    }
    Write-Host ""
}

# Show help if no parameters
if (-not ($ApplyMigration -or $VerifySetup -or $EnableApi2Schedule -or $EnableApi4Schedule -or $TestApi2 -or $TestApi4 -or $BackupDatabase)) {
    Write-Host "Usage Examples:" -ForegroundColor $ColorWarning
    Write-Host ""
    Write-Host "1. Backup database:" -ForegroundColor $ColorInfo
    Write-Host "   .\setup-api-filtering-and-printers.ps1 -BackupDatabase" -ForegroundColor Gray
    Write-Host ""
    Write-Host "2. Apply migration:" -ForegroundColor $ColorInfo
    Write-Host "   .\setup-api-filtering-and-printers.ps1 -ApplyMigration" -ForegroundColor Gray
    Write-Host ""
    Write-Host "3. Verify configuration:" -ForegroundColor $ColorInfo
    Write-Host "   .\setup-api-filtering-and-printers.ps1 -VerifySetup" -ForegroundColor Gray
    Write-Host ""
    Write-Host "4. Test API 2 (SS items):" -ForegroundColor $ColorInfo
    Write-Host "   .\setup-api-filtering-and-printers.ps1 -TestApi2" -ForegroundColor Gray
    Write-Host ""
    Write-Host "5. Test API 4 (Non-SS items, printer 4301):" -ForegroundColor $ColorInfo
    Write-Host "   .\setup-api-filtering-and-printers.ps1 -TestApi4" -ForegroundColor Gray
    Write-Host ""
    Write-Host "6. Enable API 4 schedule:" -ForegroundColor $ColorInfo
    Write-Host "   .\setup-api-filtering-and-printers.ps1 -EnableApi4Schedule" -ForegroundColor Gray
    Write-Host ""
    Write-Host "7. Full setup (backup, migrate, verify):" -ForegroundColor $ColorInfo
    Write-Host "   .\setup-api-filtering-and-printers.ps1 -BackupDatabase -ApplyMigration -VerifySetup" -ForegroundColor Gray
    Write-Host ""
    Write-Host "8. Change API 4 printer name:" -ForegroundColor $ColorInfo
    Write-Host "   .\setup-api-filtering-and-printers.ps1 -ApplyMigration -Api4Printer '5200'" -ForegroundColor Gray
    Write-Host ""
}

Write-Host "Setup script completed." -ForegroundColor $ColorSuccess
