# Setup script for Personalized Orders API Configuration
# This script helps apply the database migration and configure the new API

param(
    [Parameter(Mandatory=$false)]
    [string]$DatabasePath = ".\api_config.db",

    [Parameter(Mandatory=$false)]
    [string]$BearerToken = "",

    [Parameter(Mandatory=$false)]
    [string]$CookieToken = "",

    [Parameter(Mandatory=$false)]
    [string]$UserData = "",

    [Parameter(Mandatory=$false)]
    [switch]$ApplyMigration,

    [Parameter(Mandatory=$false)]
    [switch]$UpdateTokens,

    [Parameter(Mandatory=$false)]
    [switch]$VerifySetup,

    [Parameter(Mandatory=$false)]
    [switch]$EnableSchedule,

    [Parameter(Mandatory=$false)]
    [int]$IntervalSeconds = 3600
)

$ErrorActionPreference = "Stop"

# Check if sqlite3 is available
$sqlite3Path = $null
if (Test-Path ".\sqlite3.exe") {
    $sqlite3Path = ".\sqlite3.exe"
} elseif (Get-Command sqlite3 -ErrorAction SilentlyContinue) {
    $sqlite3Path = "sqlite3"
} else {
    Write-Error "sqlite3 not found. Please install SQLite3 command-line tools or place sqlite3.exe in the current directory."
    exit 1
}

Write-Host "Personalized Orders API Setup Script" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""

# Check if database exists
if (-not (Test-Path $DatabasePath)) {
    Write-Error "Database not found at: $DatabasePath"
    exit 1
}

Write-Host "Using database: $DatabasePath" -ForegroundColor Green
Write-Host ""

# Function to execute SQL and display results
function Invoke-Sqlite {
    param(
        [string]$Query,
        [switch]$ShowResults
    )

    $output = & $sqlite3Path $DatabasePath $Query 2>&1

    if ($LASTEXITCODE -ne 0) {
        Write-Error "SQL execution failed: $output"
        return $false
    }

    if ($ShowResults) {
        Write-Host $output
    }

    return $true
}

# 1. Apply Migration
if ($ApplyMigration) {
    Write-Host "Applying database migration..." -ForegroundColor Yellow

    if (-not (Test-Path ".\add-personalized-orders-api.sql")) {
        Write-Error "Migration script not found: .\add-personalized-orders-api.sql"
        exit 1
    }

    # Read migration script
    $migrationSql = Get-Content ".\add-personalized-orders-api.sql" -Raw

    # Execute migration
    Write-Host "Executing SQL migration..." -ForegroundColor Yellow
    Get-Content ".\add-personalized-orders-api.sql" -Raw | & $sqlite3Path $DatabasePath

    if ($LASTEXITCODE -eq 0) {
        Write-Host "[OK] Migration applied successfully!" -ForegroundColor Green
    } else {
        Write-Error "Migration failed!"
        exit 1
    }
    Write-Host ""
}

# 2. Update Tokens
if ($UpdateTokens) {
    Write-Host "Updating authentication tokens..." -ForegroundColor Yellow

    if ([string]::IsNullOrWhiteSpace($BearerToken)) {
        Write-Warning "Bearer token not provided. Use -BearerToken parameter."
    } else {
        Write-Host "Updating Bearer token..." -ForegroundColor Gray

        $sql = @"
UPDATE PrimaryApi
SET Headers = json_replace(
    json(Headers),
    '$.Authorization',
    'Bearer $BearerToken'
)
WHERE ApiNumber = 3;
"@

        if (Invoke-Sqlite -Query $sql) {
            Write-Host "[OK] Bearer token updated" -ForegroundColor Green
        }
    }

    if ([string]::IsNullOrWhiteSpace($CookieToken) -or [string]::IsNullOrWhiteSpace($UserData)) {
        Write-Warning "Cookie or UserData not provided. Use -CookieToken and -UserData parameters."
    } else {
        Write-Host "Updating Cookie..." -ForegroundColor Gray

        $cookieValue = "token=$CookieToken; userData=$UserData; isRefreshedToken=false"

        $sql = @"
UPDATE PrimaryApi
SET Headers = json_replace(
    json(Headers),
    '$.Cookie',
    '$cookieValue'
)
WHERE ApiNumber = 3;
"@

        if (Invoke-Sqlite -Query $sql) {
            Write-Host "[OK] Cookie updated" -ForegroundColor Green
        }
    }
    Write-Host ""
}

# 3. Verify Setup
if ($VerifySetup) {
    Write-Host "Verifying configuration..." -ForegroundColor Yellow
    Write-Host ""

    # Check PrimaryApi
    Write-Host "Primary API Configuration:" -ForegroundColor Cyan
    $sql = "SELECT ApiNumber, ApiName, Endpoint, IsEnabled FROM PrimaryApi WHERE ApiNumber = 3;"
    Invoke-Sqlite -Query $sql -ShowResults
    Write-Host ""

    # Check SubAction
    Write-Host "Sub-Action Configuration:" -ForegroundColor Cyan
    $sql = @"
SELECT s.ActionNumber, s.ActionName, s.ActionType, s.IsEnabled
FROM SubAction s
JOIN PrimaryApi p ON s.PrimaryApiId = p.Id
WHERE p.ApiNumber = 3;
"@
    Invoke-Sqlite -Query $sql -ShowResults
    Write-Host ""

    # Check Schedule
    Write-Host "Schedule Configuration:" -ForegroundColor Cyan
    $sql = "SELECT ScheduleName, CronExpression, IsEnabled FROM Schedule WHERE ScheduleName = 'Personalized Orders Print Schedule';"
    Invoke-Sqlite -Query $sql -ShowResults
    Write-Host ""

    Write-Host "[OK] Verification complete" -ForegroundColor Green
    Write-Host ""
}

# 4. Enable Schedule
if ($EnableSchedule) {
    Write-Host "Enabling schedule..." -ForegroundColor Yellow

    # Convert interval seconds to cron expression (simplified)
    $cronExpression = "0 * * * *"  # Default: every hour
    if ($IntervalSeconds -eq 1800) {
        $cronExpression = "*/30 * * * *"  # Every 30 minutes
    } elseif ($IntervalSeconds -eq 900) {
        $cronExpression = "*/15 * * * *"  # Every 15 minutes
    } elseif ($IntervalSeconds -eq 300) {
        $cronExpression = "*/5 * * * *"   # Every 5 minutes
    }

    $sql = @"
UPDATE Schedule
SET IsEnabled = 1, CronExpression = '$cronExpression', UpdatedAt = CURRENT_TIMESTAMP
WHERE ScheduleName = 'Personalized Orders Print Schedule';
"@

    if (Invoke-Sqlite -Query $sql) {
        Write-Host "[OK] Schedule enabled (cron: $cronExpression)" -ForegroundColor Green
        Write-Host ""
        Write-Host "IMPORTANT: Restart the ScheduledPrintService Windows service for changes to take effect." -ForegroundColor Yellow
        Write-Host "  Stop-Service -Name 'ScheduledPrintService'" -ForegroundColor Gray
        Write-Host "  Start-Service -Name 'ScheduledPrintService'" -ForegroundColor Gray
    }
    Write-Host ""
}

# Show help if no parameters
if (-not ($ApplyMigration -or $UpdateTokens -or $VerifySetup -or $EnableSchedule)) {
    Write-Host "Usage Examples:" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "1. Apply migration:" -ForegroundColor Cyan
    Write-Host "   .\setup-personalized-orders.ps1 -ApplyMigration" -ForegroundColor Gray
    Write-Host ""
    Write-Host "2. Update authentication tokens:" -ForegroundColor Cyan
    Write-Host "   .\setup-personalized-orders.ps1 -UpdateTokens -BearerToken 'eyJhbG...' -CookieToken 'eyJhbG...' -UserData '{...}'" -ForegroundColor Gray
    Write-Host ""
    Write-Host "3. Verify configuration:" -ForegroundColor Cyan
    Write-Host "   .\setup-personalized-orders.ps1 -VerifySetup" -ForegroundColor Gray
    Write-Host ""
    Write-Host "4. Enable schedule (runs every hour):" -ForegroundColor Cyan
    Write-Host "   .\setup-personalized-orders.ps1 -EnableSchedule" -ForegroundColor Gray
    Write-Host ""
    Write-Host "5. Enable schedule with custom interval (every 30 minutes):" -ForegroundColor Cyan
    Write-Host "   .\setup-personalized-orders.ps1 -EnableSchedule -IntervalSeconds 1800" -ForegroundColor Gray
    Write-Host ""
    Write-Host "6. Full setup (all steps):" -ForegroundColor Cyan
    Write-Host "   .\setup-personalized-orders.ps1 -ApplyMigration -UpdateTokens -BearerToken 'eyJhbG...' -CookieToken 'eyJhbG...' -UserData '{...}' -VerifySetup -EnableSchedule" -ForegroundColor Gray
    Write-Host ""
}

Write-Host "Setup script completed." -ForegroundColor Green
