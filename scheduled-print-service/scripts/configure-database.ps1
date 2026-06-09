#Requires -Version 5.1
<#
.SYNOPSIS
    Configure database for scheduled API execution

.DESCRIPTION
    - Enables the schedule
    - Assigns API #1 (Orders) and API #3 (Picklist) to the schedule
    - Updates API #3 batch size to get all records
    - Configures execution order

.PARAMETER DatabasePath
    Path to api_config.db

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File configure-database.ps1
#>

param(
    [string]$DatabasePath = "C:\Program Files\Malchut\ScheduledPrintService\api_config.db"
)

Write-Host ""
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "Configure Database for Scheduled Execution" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
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
    Write-Host "Make sure sqlite3.exe is in the current directory:" -ForegroundColor Yellow
    Write-Host "  Copy it to: C:\Program Files\Malchut\ScheduledPrintService\" -ForegroundColor Gray
    exit 1
}

Write-Host "Database: $DatabasePath" -ForegroundColor Gray
Write-Host ""

# 1. Enable the schedule
Write-Host "[1/5] Enabling schedule..." -ForegroundColor Yellow
& sqlite3 $DatabasePath "UPDATE Schedule SET IsEnabled = 1 WHERE Id = 1;" 2>&1
if ($LASTEXITCODE -eq 0) {
    Write-Host "[OK] Schedule enabled" -ForegroundColor Green
} else {
    Write-Error "Failed to enable schedule"
    exit 1
}
Write-Host ""

# 2. Clear existing schedule assignments
Write-Host "[2/5] Clearing existing schedule assignments..." -ForegroundColor Yellow
& sqlite3 $DatabasePath "DELETE FROM ScheduleApi WHERE ScheduleId = 1;" 2>&1
if ($LASTEXITCODE -eq 0) {
    Write-Host "[OK] Cleared existing assignments" -ForegroundColor Green
} else {
    Write-Error "Failed to clear assignments"
    exit 1
}
Write-Host ""

# 3. Assign API #1 and API #3 to schedule
Write-Host "[3/5] Assigning APIs to schedule..." -ForegroundColor Yellow

# API #1 (Orders) - Execute first
& sqlite3 $DatabasePath "INSERT INTO ScheduleApi (ScheduleId, ApiNumber, ExecutionOrder) VALUES (1, 1, 1);" 2>&1
if ($LASTEXITCODE -eq 0) {
    Write-Host "[OK] Assigned API #1 (Orders List) - Execution Order: 1" -ForegroundColor Green
} else {
    Write-Error "Failed to assign API #1"
    exit 1
}

# API #3 (Picklist) - Execute second
& sqlite3 $DatabasePath "INSERT INTO ScheduleApi (ScheduleId, ApiNumber, ExecutionOrder) VALUES (1, 3, 2);" 2>&1
if ($LASTEXITCODE -eq 0) {
    Write-Host "[OK] Assigned API #3 (Picklist) - Execution Order: 2" -ForegroundColor Green
} else {
    Write-Error "Failed to assign API #3"
    exit 1
}
Write-Host ""

# 4. Update API #3 batch size to get all records
Write-Host "[4/5] Updating API #3 batch size..." -ForegroundColor Yellow

# Get current payload
$CurrentPayload = & sqlite3 $DatabasePath "SELECT COALESCE(PrimaryPayload, '{}') FROM PrimaryApi WHERE ApiNumber = 3;" 2>&1
if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($CurrentPayload)) {
    Write-Host "[SKIP] API #3 payload is NULL or empty - batch size already controlled by Params field" -ForegroundColor Yellow
    Write-Host ""
    # Continue to verification
    $CurrentPayload = "{}"
} else {
    Write-Host "Current payload (first 100 chars): $($CurrentPayload.Substring(0, [Math]::Min(100, $CurrentPayload.Length)))..." -ForegroundColor Gray
}

# Update length to 9999 (get all records) - only if payload has content
if ($CurrentPayload -ne "{}") {
    try {
        $PayloadObj = $CurrentPayload | ConvertFrom-Json

        if ($null -ne $PayloadObj.length) {
            $OldLength = $PayloadObj.length
            $PayloadObj.length = 9999

            Write-Host "Changing batch size from $OldLength to 9999" -ForegroundColor Gray

            $NewPayload = $PayloadObj | ConvertTo-Json -Compress -Depth 10
            $EscapedPayload = $NewPayload -replace "'", "''"

            & sqlite3 $DatabasePath "UPDATE PrimaryApi SET PrimaryPayload = '$EscapedPayload' WHERE ApiNumber = 3;" 2>&1

            if ($LASTEXITCODE -eq 0) {
                Write-Host "[OK] API #3 batch size updated to 9999" -ForegroundColor Green
            } else {
                Write-Error "Failed to update API #3 batch size"
                exit 1
            }
        } else {
            Write-Host "[SKIP] Payload does not contain 'length' field" -ForegroundColor Yellow
        }
    } catch {
        Write-Host "[SKIP] Could not parse/update payload: $_" -ForegroundColor Yellow
    }
}
Write-Host ""

# 5. Verify configuration
Write-Host "[5/5] Verifying configuration..." -ForegroundColor Yellow
Write-Host ""

# Check schedule
$Schedule = & sqlite3 $DatabasePath "SELECT Id, ScheduleName, IsEnabled, CronExpression FROM Schedule WHERE Id = 1;" 2>&1
$ScheduleParts = $Schedule -split '\|'
$ScheduleEnabled = $ScheduleParts[2]

Write-Host "Schedule Configuration:" -ForegroundColor Cyan
Write-Host "  Name: $($ScheduleParts[1])" -ForegroundColor White
Write-Host "  Enabled: " -NoNewline
if ($ScheduleEnabled -eq "1") {
    Write-Host "YES" -ForegroundColor Green
} else {
    Write-Host "NO" -ForegroundColor Red
}
Write-Host "  Cron: $($ScheduleParts[3]) (every 15 minutes)" -ForegroundColor White
Write-Host ""

# Check assigned APIs
Write-Host "Assigned APIs:" -ForegroundColor Cyan
$AssignedApis = & sqlite3 $DatabasePath @"
SELECT
    sa.ApiNumber,
    pa.ApiName,
    sa.ExecutionOrder,
    pa.IsEnabled
FROM ScheduleApi sa
JOIN PrimaryApi pa ON sa.ApiNumber = pa.ApiNumber
WHERE sa.ScheduleId = 1
ORDER BY sa.ExecutionOrder;
"@ 2>&1

$AssignedApis -split "`n" | Where-Object { $_ } | ForEach-Object {
    $Parts = $_ -split '\|'
    $ApiNum = $Parts[0]
    $ApiName = $Parts[1]
    $Order = $Parts[2]
    $Enabled = $Parts[3]

    Write-Host "  #$Order - " -NoNewline -ForegroundColor Gray
    Write-Host "API #$ApiNum" -NoNewline -ForegroundColor Cyan
    Write-Host ": $ApiName" -NoNewline -ForegroundColor White

    if ($Enabled -eq "1") {
        Write-Host " [ENABLED]" -ForegroundColor Green
    } else {
        Write-Host " [DISABLED]" -ForegroundColor Red
    }
}

Write-Host ""

# Check batch sizes
$Api1Length = & sqlite3 $DatabasePath "SELECT json_extract(PrimaryPayload, '$.length') FROM PrimaryApi WHERE ApiNumber = 1;" 2>&1
$Api3Length = & sqlite3 $DatabasePath "SELECT json_extract(PrimaryPayload, '$.length') FROM PrimaryApi WHERE ApiNumber = 3;" 2>&1

Write-Host "Batch Sizes:" -ForegroundColor Cyan
Write-Host "  API #1 (Orders): $Api1Length records per call" -ForegroundColor White
Write-Host "  API #3 (Picklist): $Api3Length records per call" -ForegroundColor White
Write-Host ""

# Summary
Write-Host "==========================================" -ForegroundColor Green
Write-Host "Configuration Complete!" -ForegroundColor Green
Write-Host "==========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Schedule: " -NoNewline
Write-Host "ENABLED" -ForegroundColor Green
Write-Host "Frequency: Every 15 minutes" -ForegroundColor White
Write-Host "Execution Order:" -ForegroundColor Yellow
Write-Host "  1. API #1 - Orders List (fetch $Api1Length orders)" -ForegroundColor White
Write-Host "  2. API #3 - Picklist (fetch $Api3Length picklists)" -ForegroundColor White
Write-Host ""
Write-Host "Database Configuration Complete!" -ForegroundColor Green
Write-Host ""
Write-Host "The DatabaseSchedulerService will now:" -ForegroundColor Cyan
Write-Host "  - Read schedules from the database" -ForegroundColor White
Write-Host "  - Execute API #1 and API #3 every 15 minutes" -ForegroundColor White
Write-Host "  - Skip already-processed orders" -ForegroundColor White
Write-Host "  - Log all execution details" -ForegroundColor White
Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Cyan
Write-Host "  1. Build and publish the updated service:" -ForegroundColor Gray
Write-Host "     dotnet publish -c Release -o publish" -ForegroundColor White
Write-Host "  2. Deploy to production folder and restart service" -ForegroundColor Gray
Write-Host "  3. Monitor execution:" -ForegroundColor Gray
Write-Host "     powershell -File monitor-service.ps1" -ForegroundColor White
Write-Host ""
