#Requires -Version 5.1
<#
.SYNOPSIS
    Simple database query tool for Scheduled Print Service

.DESCRIPTION
    Queries the api_config.db database and displays configuration in a readable format.
    No external tools required - uses native PowerShell or falls back to sqlite3.

.PARAMETER DatabasePath
    Path to api_config.db (auto-detects if not specified)

.PARAMETER ShowSubActions
    Show detailed sub-actions for each API

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File query-database.ps1

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File query-database.ps1 -ShowSubActions
#>

param(
    [string]$DatabasePath = "",
    [switch]$ShowSubActions = $false
)

# Auto-detect database location
if ($DatabasePath -eq "") {
    $PossiblePaths = @(
        "C:\Program Files\Malchut\ScheduledPrintService\api_config.db",
        "$PSScriptRoot\ScheduledPrintService\api_config.db",
        "$env:ProgramData\ScheduledPrintService\api_config.db"
    )

    foreach ($Path in $PossiblePaths) {
        if (Test-Path $Path) {
            $DatabasePath = $Path
            break
        }
    }

    if ($DatabasePath -eq "") {
        Write-Error "Could not find api_config.db. Please specify -DatabasePath"
        Write-Host "`nSearched locations:" -ForegroundColor Yellow
        $PossiblePaths | ForEach-Object {
            Write-Host "  - $_" -ForegroundColor Gray
        }
        exit 1
    }
}

if (-not (Test-Path $DatabasePath)) {
    Write-Error "Database not found: $DatabasePath"
    exit 1
}

Write-Host "Database: " -NoNewline
Write-Host $DatabasePath -ForegroundColor Cyan
Write-Host ""

# Try to load System.Data.SQLite
$UseNative = $false
$Sqlite3 = $null

try {
    # Try to load from GAC or current directory
    Add-Type -AssemblyName "System.Data.SQLite" -ErrorAction SilentlyContinue
    $UseNative = $true
    Write-Host "Using System.Data.SQLite (native PowerShell)" -ForegroundColor Green
} catch {
    # Check if sqlite3 is available
    # First check in the same directory as the database
    $DbDir = Split-Path -Parent $DatabasePath
    $LocalSqlite3 = Join-Path $DbDir "sqlite3.exe"

    if (Test-Path $LocalSqlite3) {
        $Sqlite3 = $LocalSqlite3
        Write-Host "Using sqlite3 at: $Sqlite3" -ForegroundColor Yellow
    } else {
        # Try PATH
        $Sqlite3Cmd = Get-Command sqlite3 -ErrorAction SilentlyContinue
        if ($Sqlite3Cmd) {
            $Sqlite3 = $Sqlite3Cmd.Source
            Write-Host "Using sqlite3 command line tool" -ForegroundColor Yellow
        } else {
            Write-Error "Neither System.Data.SQLite nor sqlite3 found!"
            Write-Host "`nExpected location: $LocalSqlite3" -ForegroundColor Yellow
            Write-Host "`nOptions:" -ForegroundColor Yellow
            Write-Host "  1. Download sqlite3: https://www.sqlite.org/download.html" -ForegroundColor Gray
            Write-Host "  2. Or install System.Data.SQLite from NuGet" -ForegroundColor Gray
            exit 1
        }
    }
}

Write-Host ""

# Query function
function Query-Database {
    param(
        [string]$Query
    )

    if ($UseNative) {
        try {
            $Connection = New-Object System.Data.SQLite.SQLiteConnection("Data Source=$DatabasePath;Version=3;Read Only=True;")
            $Connection.Open()
            $Command = $Connection.CreateCommand()
            $Command.CommandText = $Query
            $Reader = $Command.ExecuteReader()

            $Results = @()
            while ($Reader.Read()) {
                $Row = @{}
                for ($i = 0; $i -lt $Reader.FieldCount; $i++) {
                    $Row[$Reader.GetName($i)] = $Reader.GetValue($i)
                }
                $Results += [PSCustomObject]$Row
            }
            $Reader.Close()
            $Connection.Close()
            return $Results
        } catch {
            Write-Error "Query failed: $_"
            return @()
        }
    } else {
        $Output = & $Sqlite3 $DatabasePath $Query 2>&1
        if ($LASTEXITCODE -eq 0) {
            return $Output
        } else {
            Write-Error "Query failed: $Output"
            return @()
        }
    }
}

# 1. Show Primary APIs
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "PRIMARY APIs" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

$Apis = Query-Database "SELECT * FROM PrimaryApi ORDER BY ApiNumber"

if ($UseNative) {
    foreach ($Api in $Apis) {
        $EnabledText = if ($Api.IsEnabled -eq 1) { "ENABLED" } else { "DISABLED" }
        $EnabledColor = if ($Api.IsEnabled -eq 1) { "Green" } else { "Red" }

        Write-Host "`nAPI #$($Api.ApiNumber): " -NoNewline -ForegroundColor Cyan
        Write-Host $Api.ApiName -NoNewline -ForegroundColor White
        Write-Host " [$EnabledText]" -ForegroundColor $EnabledColor

        Write-Host "  Endpoint: " -NoNewline -ForegroundColor Gray
        Write-Host $Api.PrimaryEndpoint -ForegroundColor White

        Write-Host "  Method: " -NoNewline -ForegroundColor Gray
        Write-Host $Api.PrimaryHttpMethod -ForegroundColor White

        Write-Host "  Base URL: " -NoNewline -ForegroundColor Gray
        Write-Host $Api.BaseUrl -ForegroundColor White

        # Show sub-actions count
        $SubActionCount = (Query-Database "SELECT COUNT(*) as Count FROM SubAction WHERE PrimaryApiId = $($Api.Id)")[0].Count
        $EnabledSubActions = (Query-Database "SELECT COUNT(*) as Count FROM SubAction WHERE PrimaryApiId = $($Api.Id) AND IsEnabled = 1")[0].Count

        Write-Host "  Sub-Actions: " -NoNewline -ForegroundColor Gray
        Write-Host "$EnabledSubActions enabled / $SubActionCount total" -ForegroundColor White

        if ($ShowSubActions -and $SubActionCount -gt 0) {
            $SubActions = Query-Database "SELECT * FROM SubAction WHERE PrimaryApiId = $($Api.Id) ORDER BY ActionNumber"
            Write-Host "`n  Sub-Actions Detail:" -ForegroundColor Yellow
            foreach ($SubAction in $SubActions) {
                $SubEnabledText = if ($SubAction.IsEnabled -eq 1) { "ENABLED" } else { "DISABLED" }
                $SubEnabledColor = if ($SubAction.IsEnabled -eq 1) { "Green" } else { "Red" }

                Write-Host "    [$SubEnabledText] " -NoNewline -ForegroundColor $SubEnabledColor
                Write-Host "#$($SubAction.ActionNumber): $($SubAction.ActionName)" -ForegroundColor White
                Write-Host "       Type: $($SubAction.ActionType)" -ForegroundColor Gray
                Write-Host "       Endpoint: $($SubAction.Endpoint)" -ForegroundColor Gray
            }
        }
    }
} else {
    Write-Host $Apis -ForegroundColor White
}

Write-Host ""

# 2. Show Schedules
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "SCHEDULES" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

$Schedules = Query-Database "SELECT * FROM Schedule ORDER BY Id"

if ($UseNative) {
    if ($Schedules.Count -eq 0) {
        Write-Host "No schedules configured" -ForegroundColor Yellow
    } else {
        foreach ($Schedule in $Schedules) {
            $EnabledText = if ($Schedule.IsEnabled -eq 1) { "ENABLED" } else { "DISABLED" }
            $EnabledColor = if ($Schedule.IsEnabled -eq 1) { "Green" } else { "Red" }

            Write-Host "`nSchedule #$($Schedule.Id): " -NoNewline -ForegroundColor Cyan
            Write-Host $Schedule.ScheduleName -NoNewline -ForegroundColor White
            Write-Host " [$EnabledText]" -ForegroundColor $EnabledColor

            Write-Host "  Cron Expression: " -NoNewline -ForegroundColor Gray
            Write-Host $Schedule.CronExpression -ForegroundColor White

            # Explain cron (simple cases)
            $CronParts = $Schedule.CronExpression -split ' '
            if ($CronParts.Count -ge 5) {
                $Explanation = ""
                if ($CronParts[0] -eq "*" -and $CronParts[1] -eq "*") {
                    $Explanation = "Runs every hour"
                } elseif ($CronParts[0] -match "^\d+$" -and $CronParts[1] -eq "*") {
                    $Explanation = "Runs every hour at minute $($CronParts[0])"
                } elseif ($CronParts[0] -match "^\d+$" -and $CronParts[1] -match "^\d+$") {
                    $Explanation = "Runs at $($CronParts[1]):$($CronParts[0].PadLeft(2,'0')) daily"
                } elseif ($CronParts[0] -match "^\*/(\d+)$") {
                    $Explanation = "Runs every $($Matches[1]) minutes"
                }

                if ($Explanation -ne "") {
                    Write-Host "  Meaning: " -NoNewline -ForegroundColor Gray
                    Write-Host $Explanation -ForegroundColor White
                }
            }

            # Show assigned APIs
            $ScheduleApis = Query-Database "SELECT sa.*, pa.ApiName FROM ScheduleApi sa JOIN PrimaryApi pa ON sa.ApiNumber = pa.ApiNumber WHERE sa.ScheduleId = $($Schedule.Id) ORDER BY sa.ExecutionOrder"

            if ($ScheduleApis.Count -gt 0) {
                Write-Host "  APIs to execute (in order):" -ForegroundColor Gray
                foreach ($ScheduleApi in $ScheduleApis) {
                    Write-Host "    $($ScheduleApi.ExecutionOrder). " -NoNewline -ForegroundColor Gray
                    Write-Host "API #$($ScheduleApi.ApiNumber): $($ScheduleApi.ApiName)" -ForegroundColor White
                }
            } else {
                Write-Host "  No APIs assigned" -ForegroundColor Yellow
            }
        }
    }
} else {
    Write-Host $Schedules -ForegroundColor White
}

Write-Host ""

# 3. Summary
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "SUMMARY" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

if ($UseNative) {
    $TotalApis = $Apis.Count
    $EnabledApis = ($Apis | Where-Object { $_.IsEnabled -eq 1 }).Count
    $TotalSchedules = $Schedules.Count
    $EnabledSchedules = ($Schedules | Where-Object { $_.IsEnabled -eq 1 }).Count

    $AllSubActions = Query-Database "SELECT * FROM SubAction"
    $TotalSubActions = $AllSubActions.Count
    $EnabledSubActions = ($AllSubActions | Where-Object { $_.IsEnabled -eq 1 }).Count

    Write-Host ""
    Write-Host "Primary APIs: " -NoNewline
    Write-Host "$EnabledApis enabled " -NoNewline -ForegroundColor Green
    Write-Host "/ $TotalApis total" -ForegroundColor Gray

    Write-Host "Sub-Actions: " -NoNewline
    Write-Host "$EnabledSubActions enabled " -NoNewline -ForegroundColor Green
    Write-Host "/ $TotalSubActions total" -ForegroundColor Gray

    Write-Host "Schedules: " -NoNewline
    Write-Host "$EnabledSchedules enabled " -NoNewline -ForegroundColor Green
    Write-Host "/ $TotalSchedules total" -ForegroundColor Gray

    Write-Host ""

    # Warnings
    if ($EnabledApis -eq 0) {
        Write-Host "[WARNING] No APIs are enabled!" -ForegroundColor Yellow
    }

    if ($EnabledSchedules -eq 0) {
        Write-Host "[WARNING] No schedules are enabled!" -ForegroundColor Yellow
    } else {
        $SchedulesWithNoApis = Query-Database "SELECT COUNT(*) as Count FROM Schedule s WHERE s.IsEnabled = 1 AND NOT EXISTS (SELECT 1 FROM ScheduleApi sa WHERE sa.ScheduleId = s.Id)"
        if ($SchedulesWithNoApis[0].Count -gt 0) {
            Write-Host "[WARNING] $($SchedulesWithNoApis[0].Count) enabled schedule(s) have no APIs assigned!" -ForegroundColor Yellow
        }
    }

    if ($EnabledApis -gt 0 -and $EnabledSchedules -gt 0) {
        Write-Host "[OK] Configuration looks good - APIs are scheduled to run" -ForegroundColor Green
    }
}

Write-Host ""
