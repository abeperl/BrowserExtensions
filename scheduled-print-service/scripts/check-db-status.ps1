# Quick Status Check for Scheduled Print Service Database
# Shows what's configured and what's missing

param(
    [string]$DbPath = "C:\Program Files\Malchut\ScheduledPrintService\api_config.db"
)

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "  Database Status Check" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan

# Check if sqlite3 is available (check local directory first)
$sqlite3Path = if (Test-Path ".\sqlite3.exe") { ".\sqlite3.exe" } 
               elseif (Get-Command sqlite3 -ErrorAction SilentlyContinue) { "sqlite3" }
               else { $null }

if (-not $sqlite3Path) {
    Write-Host "[X] sqlite3 not found" -ForegroundColor Red
    Write-Host "Install: winget install SQLite.SQLite`n" -ForegroundColor Yellow
    
    # Try to at least check if file exists
    if (Test-Path $DbPath) {
        Write-Host "[OK] Database file exists: $DbPath" -ForegroundColor Green
    } else {
        Write-Host "[X] Database file NOT found: $DbPath" -ForegroundColor Red
    }
    exit 1
}

# Check if database exists
if (-not (Test-Path $DbPath)) {
    Write-Host "[X] Database NOT found: $DbPath`n" -ForegroundColor Red
    Write-Host "Create it with: .\setup-database.ps1`n" -ForegroundColor Yellow
    exit 1
}

Write-Host "[OK] Database found: $DbPath`n" -ForegroundColor Green

# Check tables
Write-Host "=== Tables ===" -ForegroundColor Cyan
$tables = & $sqlite3Path $DbPath "SELECT name FROM sqlite_master WHERE type='table';"
if ($tables) {
    $tables -split "`n" | Where-Object { $_ } | ForEach-Object {
        Write-Host "  [OK] $_" -ForegroundColor Green
    }
} else {
    Write-Host "  [X] No tables found" -ForegroundColor Red
}

# Check Primary APIs
Write-Host "`n=== Primary APIs ===" -ForegroundColor Cyan
$apis = & $sqlite3Path $DbPath "SELECT ApiNumber, ApiName, IsEnabled FROM PrimaryApi;"
if ($apis) {
    Write-Host "  API# | Enabled | Name" -ForegroundColor Yellow
    Write-Host "  -----|---------|-----" -ForegroundColor Yellow
    $apis -split "`n" | Where-Object { $_ } | ForEach-Object {
        $parts = $_ -split '\|'
        $enabled = if ($parts[2] -eq '1') { '[OK]' } else { '[X]' }
        Write-Host "  $($parts[0].PadRight(4)) | $enabled   | $($parts[1])" -ForegroundColor White
    }
} else {
    Write-Host "  [X] No APIs configured" -ForegroundColor Red
    Write-Host "  Add with: .\add-picklist-api.ps1" -ForegroundColor Yellow
}

# Check Schedules
Write-Host "`n=== Schedules ===" -ForegroundColor Cyan
$schedules = & $sqlite3Path $DbPath "SELECT Id, ScheduleName, CronExpression, IsEnabled FROM Schedule;"
if ($schedules) {
    Write-Host "  ID | Enabled | Cron Expression    | Name" -ForegroundColor Yellow
    Write-Host "  ---|---------|--------------------|---------" -ForegroundColor Yellow
    $schedules -split "`n" | Where-Object { $_ } | ForEach-Object {
        $parts = $_ -split '\|'
        $enabled = if ($parts[3] -eq '1') { '[OK]' } else { '[X]' }
        Write-Host "  $($parts[0].PadRight(2)) | $enabled   | $($parts[2].PadRight(18)) | $($parts[1])" -ForegroundColor White
    }
    
    # Show schedule-API mappings
    Write-Host "`n  Schedule-API Mappings:" -ForegroundColor Yellow
    $mappings = & $sqlite3Path $DbPath @"
SELECT s.ScheduleName, sa.ApiNumber, p.ApiName
FROM ScheduleApi sa
JOIN Schedule s ON sa.ScheduleId = s.Id
JOIN PrimaryApi p ON sa.ApiNumber = p.ApiNumber
ORDER BY s.Id, sa.ExecutionOrder;
"@
    if ($mappings) {
        $mappings -split "`n" | Where-Object { $_ } | ForEach-Object {
            $parts = $_ -split '\|'
            Write-Host "    $($parts[0]) -> API #$($parts[1]): $($parts[2])" -ForegroundColor White
        }
    } else {
        Write-Host "    [X] No APIs assigned to schedules" -ForegroundColor Red
    }
} else {
    Write-Host "  [X] No schedules configured" -ForegroundColor Red
    Write-Host @"
  
  Create schedule with:
    sqlite3 '$DbPath' "
    INSERT INTO Schedule (ScheduleName, CronExpression, IsEnabled)
    VALUES ('Every 5 Minutes', '0 */5 * * * *', 1);
    
    INSERT INTO ScheduleApi (ScheduleId, ApiNumber, ExecutionOrder)
    VALUES (1, 1, 1);
    "
"@ -ForegroundColor Yellow
}

# Summary
Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "  Summary" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan

$apiCount = if ($apis) { ($apis -split "`n" | Where-Object { $_ }).Count } else { 0 }
$scheduleCount = if ($schedules) { ($schedules -split "`n" | Where-Object { $_ }).Count } else { 0 }

if ($apiCount -gt 0 -and $scheduleCount -gt 0) {
    Write-Host "[OK] Database is configured with $apiCount API(s) and $scheduleCount schedule(s)" -ForegroundColor Green
    Write-Host "  Service should start and execute on schedule`n" -ForegroundColor Green
} elseif ($apiCount -gt 0) {
    Write-Host "[!] APIs configured but NO schedules" -ForegroundColor Yellow
    Write-Host "  Add schedules to enable automated execution`n" -ForegroundColor Yellow
} elseif ($scheduleCount -gt 0) {
    Write-Host "[!] Schedules configured but NO APIs" -ForegroundColor Yellow
    Write-Host "  Add APIs to execute`n" -ForegroundColor Yellow
} else {
    Write-Host "[X] Database exists but has NO APIs or schedules configured" -ForegroundColor Red
    Write-Host "  The service will remain idle`n" -ForegroundColor Yellow
}
