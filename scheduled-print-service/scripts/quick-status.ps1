#Requires -Version 5.1
<#
.SYNOPSIS
    Quick status check for Scheduled Print Service (no sqlite3 required)

.DESCRIPTION
    Checks service status, configuration, and explains why logs/output might be missing.
    Works without database access by reading appsettings.json.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File quick-status.ps1
#>

$ServiceName = "Scheduled Print Service"
$ConfigPath = "C:\Program Files\Malchut\ScheduledPrintService\appsettings.json"
$DatabasePath = "C:\Program Files\Malchut\ScheduledPrintService\api_config.db"
$DataRoot = "E:\Share\server\servern\Software\ScheduledPrintService"

Write-Host ""
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "Scheduled Print Service - Quick Status" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""

# 1. Service Status
Write-Host "[1/6] Checking Service Status..." -ForegroundColor Yellow
$Service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue

if ($null -eq $Service) {
    Write-Host "  [ERROR] Service not installed!" -ForegroundColor Red
} else {
    if ($Service.Status -eq "Running") {
        Write-Host "  [OK] Service is RUNNING" -ForegroundColor Green

        # Get process info
        $ServiceProcess = Get-WmiObject Win32_Service | Where-Object { $_.Name -eq $ServiceName }
        if ($ServiceProcess) {
            $Process = Get-Process -Id $ServiceProcess.ProcessId -ErrorAction SilentlyContinue
            if ($Process) {
                $Uptime = (Get-Date) - $Process.StartTime
                $MemoryMB = [math]::Round($Process.WorkingSet64 / 1MB, 2)
                Write-Host "       Uptime: $($Uptime.Days)d $($Uptime.Hours)h $($Uptime.Minutes)m" -ForegroundColor Gray
                Write-Host "       Memory: $MemoryMB MB" -ForegroundColor Gray
            }
        }
    } else {
        Write-Host "  [WARN] Service is $($Service.Status)" -ForegroundColor Yellow
    }
}
Write-Host ""

# 2. Configuration File
Write-Host "[2/6] Checking Configuration..." -ForegroundColor Yellow

if (Test-Path $ConfigPath) {
    Write-Host "  [OK] Config found: $ConfigPath" -ForegroundColor Green

    try {
        $Config = Get-Content $ConfigPath -Raw | ConvertFrom-Json

        # Check each feature
        $DemoEnabled = $Config.Demo.Enabled
        $SchedulerEnabled = $Config.Scheduler.Enabled
        $ApiEnabledInConfig = if ($Config.Api.PSObject.Properties.Name -contains "Enabled") { $Config.Api.Enabled } else { $false }

        Write-Host ""
        Write-Host "  Features Enabled in appsettings.json:" -ForegroundColor Cyan

        $FeaturesEnabled = 0

        if ($DemoEnabled) {
            Write-Host "    [ENABLED]  Demo Mode" -ForegroundColor Green
            Write-Host "               URL: $($Config.Demo.Url)" -ForegroundColor Gray
            $FeaturesEnabled++
        } else {
            Write-Host "    [DISABLED] Demo Mode" -ForegroundColor Red
        }

        if ($SchedulerEnabled) {
            Write-Host "    [ENABLED]  URL Scheduler" -ForegroundColor Green
            Write-Host "               Interval: $($Config.Scheduler.IntervalSeconds)s" -ForegroundColor Gray
            Write-Host "               URLs: $($Config.Scheduler.Urls.Count)" -ForegroundColor Gray
            $FeaturesEnabled++
        } else {
            Write-Host "    [DISABLED] URL Scheduler" -ForegroundColor Red
        }

        if ($ApiEnabledInConfig) {
            Write-Host "    [ENABLED]  API Polling (from config)" -ForegroundColor Green
            $FeaturesEnabled++
        } else {
            Write-Host "    [DISABLED] API Polling (in appsettings.json)" -ForegroundColor Red
        }

        Write-Host ""
        if ($FeaturesEnabled -eq 0) {
            Write-Host "  [!!!] ALL FEATURES ARE DISABLED IN appsettings.json!" -ForegroundColor Red
            Write-Host "        This is why there are no logs or output files." -ForegroundColor Yellow
            Write-Host ""
            Write-Host "  To fix: Enable at least one feature:" -ForegroundColor Cyan
            Write-Host "    1. Edit: $ConfigPath" -ForegroundColor Gray
            Write-Host "    2. Set Demo.Enabled = true (for testing)" -ForegroundColor Gray
            Write-Host "    3. Or enable Scheduler or API features" -ForegroundColor Gray
            Write-Host "    4. Restart service: Restart-Service '$ServiceName'" -ForegroundColor Gray
        }

    } catch {
        Write-Host "  [ERROR] Could not parse config: $_" -ForegroundColor Red
    }
} else {
    Write-Host "  [ERROR] Config not found: $ConfigPath" -ForegroundColor Red
}
Write-Host ""

# 3. Database
Write-Host "[3/6] Checking Database..." -ForegroundColor Yellow

if (Test-Path $DatabasePath) {
    $DbSize = (Get-Item $DatabasePath).Length
    $DbSizeKB = [math]::Round($DbSize / 1KB, 2)
    Write-Host "  [OK] Database found: $DatabasePath" -ForegroundColor Green
    Write-Host "       Size: $DbSizeKB KB" -ForegroundColor Gray
    Write-Host ""
    Write-Host "  [INFO] Database exists but service doesn't use it yet" -ForegroundColor Yellow
    Write-Host "         The service still reads from appsettings.json" -ForegroundColor Gray
    Write-Host "         (Database integration is a future enhancement)" -ForegroundColor Gray
} else {
    Write-Host "  [WARN] Database not found: $DatabasePath" -ForegroundColor Yellow
}
Write-Host ""

# 4. Logs
Write-Host "[4/6] Checking Logs..." -ForegroundColor Yellow

$LogLocations = @(
    "$DataRoot\logs",
    "C:\ProgramData\ScheduledPrintService\logs"
)

$LogsFound = $false
foreach ($LogDir in $LogLocations) {
    if (Test-Path $LogDir) {
        $LogFiles = Get-ChildItem $LogDir -Filter "*.log" -ErrorAction SilentlyContinue
        if ($LogFiles.Count -gt 0) {
            $LatestLog = $LogFiles | Sort-Object LastWriteTime -Descending | Select-Object -First 1
            Write-Host "  [OK] Logs found: $LogDir" -ForegroundColor Green
            Write-Host "       Latest: $($LatestLog.Name)" -ForegroundColor Gray
            Write-Host "       Size: $([math]::Round($LatestLog.Length / 1KB, 2)) KB" -ForegroundColor Gray
            Write-Host "       Modified: $($LatestLog.LastWriteTime)" -ForegroundColor Gray

            # Check last few lines
            $LastLines = Get-Content $LatestLog.FullName -Tail 5 -ErrorAction SilentlyContinue
            if ($LastLines) {
                Write-Host ""
                Write-Host "       Last 5 log entries:" -ForegroundColor Cyan
                $LastLines | ForEach-Object {
                    Write-Host "         $_" -ForegroundColor Gray
                }
            }

            $LogsFound = $true
            break
        }
    }
}

if (-not $LogsFound) {
    Write-Host "  [WARN] No log files found in:" -ForegroundColor Yellow
    foreach ($LogDir in $LogLocations) {
        Write-Host "         - $LogDir" -ForegroundColor Gray
    }
    Write-Host ""
    Write-Host "  Possible reasons:" -ForegroundColor Cyan
    Write-Host "    1. Service hasn't run yet (all features disabled)" -ForegroundColor Gray
    Write-Host "    2. Service failed to start (check Event Viewer)" -ForegroundColor Gray
    Write-Host "    3. Logs in different location" -ForegroundColor Gray
}
Write-Host ""

# 5. Output Files
Write-Host "[5/6] Checking Output Files..." -ForegroundColor Yellow

$OutLocations = @(
    "$DataRoot\out",
    "C:\ProgramData\ScheduledPrintService\out"
)

$OutputFound = $false
foreach ($OutDir in $OutLocations) {
    if (Test-Path $OutDir) {
        $PdfFiles = Get-ChildItem $OutDir -Filter "*.pdf" -ErrorAction SilentlyContinue
        if ($PdfFiles.Count -gt 0) {
            Write-Host "  [OK] Output found: $OutDir" -ForegroundColor Green
            Write-Host "       PDF Count: $($PdfFiles.Count)" -ForegroundColor Gray

            $Latest = $PdfFiles | Sort-Object LastWriteTime -Descending | Select-Object -First 3
            Write-Host "       Recent files:" -ForegroundColor Cyan
            $Latest | ForEach-Object {
                Write-Host "         $($_.Name) ($($_.LastWriteTime))" -ForegroundColor Gray
            }

            $OutputFound = $true
            break
        }
    }
}

if (-not $OutputFound) {
    Write-Host "  [INFO] No PDF output files found" -ForegroundColor Yellow
    Write-Host "         This is expected if service hasn't processed any jobs yet" -ForegroundColor Gray
}
Write-Host ""

# 6. Diagnosis
Write-Host "[6/6] Diagnosis..." -ForegroundColor Yellow
Write-Host ""

if ($Service -and $Service.Status -eq "Running" -and $FeaturesEnabled -eq 0) {
    Write-Host "  DIAGNOSIS:" -ForegroundColor Red
    Write-Host "  =========" -ForegroundColor Red
    Write-Host "  The service is running but ALL FEATURES ARE DISABLED in appsettings.json" -ForegroundColor Yellow
    Write-Host "  This is why there are no logs or output files." -ForegroundColor Yellow
    Write-Host ""
    Write-Host "  SOLUTION:" -ForegroundColor Green
    Write-Host "  ========" -ForegroundColor Green
    Write-Host "  1. Edit the configuration file:" -ForegroundColor White
    Write-Host "     notepad `"$ConfigPath`"" -ForegroundColor Gray
    Write-Host ""
    Write-Host "  2. Enable Demo mode for testing:" -ForegroundColor White
    Write-Host '     Change "Demo": { "Enabled": false } to "Enabled": true' -ForegroundColor Gray
    Write-Host ""
    Write-Host "  3. Restart the service:" -ForegroundColor White
    Write-Host "     Restart-Service '$ServiceName'" -ForegroundColor Gray
    Write-Host ""
    Write-Host "  4. Wait 10 seconds and check for output:" -ForegroundColor White
    Write-Host "     Get-ChildItem '$DataRoot\out' -Filter *.pdf" -ForegroundColor Gray
    Write-Host ""

} elseif ($Service -and $Service.Status -ne "Running") {
    Write-Host "  DIAGNOSIS:" -ForegroundColor Red
    Write-Host "  =========" -ForegroundColor Red
    Write-Host "  The service is not running ($($Service.Status))" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "  SOLUTION:" -ForegroundColor Green
    Write-Host "  ========" -ForegroundColor Green
    Write-Host "  Start-Service '$ServiceName'" -ForegroundColor Gray
    Write-Host ""

} elseif ($null -eq $Service) {
    Write-Host "  DIAGNOSIS:" -ForegroundColor Red
    Write-Host "  =========" -ForegroundColor Red
    Write-Host "  The service is not installed" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "  SOLUTION:" -ForegroundColor Green
    Write-Host "  ========" -ForegroundColor Green
    Write-Host "  Run the service installer script" -ForegroundColor Gray
    Write-Host ""

} else {
    Write-Host "  Service appears to be configured and running." -ForegroundColor Green

    if ($LogsFound -and $OutputFound) {
        Write-Host "  Logs and output files are being generated." -ForegroundColor Green
    } elseif ($LogsFound -and -not $OutputFound) {
        Write-Host "  Logs exist but no output files yet - check logs for details." -ForegroundColor Yellow
    }
}

Write-Host ""
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "Status Check Complete" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""
