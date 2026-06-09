#Requires -Version 5.1
<#
.SYNOPSIS
    Diagnose why Scheduled Print Service isn't creating logs

.DESCRIPTION
    Checks permissions, environment variables, and Windows Event Viewer
    to determine why logs aren't being created.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File diagnose-logs.ps1
#>

$ServiceName = "Scheduled Print Service"
$DataRoot = "E:\Share\server\servern\Software\ScheduledPrintService"
$LogDir = "$DataRoot\logs"

Write-Host ""
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "Log Creation Diagnostic" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""

# 1. Check if log directory exists
Write-Host "[1/6] Checking Log Directory..." -ForegroundColor Yellow

if (Test-Path $LogDir) {
    Write-Host "  [OK] Directory exists: $LogDir" -ForegroundColor Green

    # Check if writable
    try {
        $TestFile = Join-Path $LogDir "test-write-$(Get-Date -Format 'yyyyMMddHHmmss').txt"
        "test" | Out-File $TestFile -Force
        if (Test-Path $TestFile) {
            Write-Host "  [OK] Directory is writable" -ForegroundColor Green
            Remove-Item $TestFile -Force
        } else {
            Write-Host "  [ERROR] Could not write to directory" -ForegroundColor Red
        }
    } catch {
        Write-Host "  [ERROR] Write test failed: $_" -ForegroundColor Red
    }
} else {
    Write-Host "  [WARN] Directory does not exist: $LogDir" -ForegroundColor Yellow
    Write-Host "         Creating directory..." -ForegroundColor Gray

    try {
        New-Item -ItemType Directory -Path $LogDir -Force | Out-Null
        Write-Host "  [OK] Created directory" -ForegroundColor Green
    } catch {
        Write-Host "  [ERROR] Could not create directory: $_" -ForegroundColor Red
    }
}
Write-Host ""

# 2. Check service account
Write-Host "[2/6] Checking Service Account..." -ForegroundColor Yellow

$Service = Get-WmiObject Win32_Service | Where-Object { $_.Name -eq $ServiceName }
if ($Service) {
    Write-Host "  Service Account: $($Service.StartName)" -ForegroundColor White

    if ($Service.StartName -eq "LocalSystem") {
        Write-Host "  [INFO] Running as LocalSystem (should have full access)" -ForegroundColor Gray
    } else {
        Write-Host "  [WARN] Running as $($Service.StartName)" -ForegroundColor Yellow
        Write-Host "         Verify this account has write access to: $LogDir" -ForegroundColor Gray
    }
}
Write-Host ""

# 3. Check Windows Event Viewer for service errors
Write-Host "[3/6] Checking Windows Event Viewer..." -ForegroundColor Yellow

try {
    # Check Application log for service-related errors in last hour
    $EventCutoff = (Get-Date).AddHours(-1)
    $Events = Get-WinEvent -FilterHashtable @{
        LogName = 'Application'
        StartTime = $EventCutoff
    } -ErrorAction SilentlyContinue | Where-Object {
        $_.Message -like "*Scheduled*Print*" -or
        $_.Message -like "*ScheduledPrintService*"
    }

    if ($Events) {
        Write-Host "  [FOUND] Recent service events:" -ForegroundColor Yellow
        $Events | Select-Object -First 5 | ForEach-Object {
            $LevelColor = switch ($_.Level) {
                1 { "Red" }      # Critical
                2 { "Red" }      # Error
                3 { "Yellow" }   # Warning
                default { "Gray" }
            }
            Write-Host "    [$($_.TimeCreated)] " -NoNewline -ForegroundColor Gray
            Write-Host "$($_.LevelDisplayName): " -NoNewline -ForegroundColor $LevelColor
            Write-Host $_.Message.Substring(0, [Math]::Min(100, $_.Message.Length)) -ForegroundColor Gray
        }
    } else {
        Write-Host "  [INFO] No recent events found for service" -ForegroundColor Gray
    }
} catch {
    Write-Host "  [WARN] Could not query Event Viewer: $_" -ForegroundColor Yellow
}
Write-Host ""

# 4. Search for log files in ALL possible locations
Write-Host "[4/6] Searching for Log Files Everywhere..." -ForegroundColor Yellow

$SearchPaths = @(
    "E:\Share\server\servern\Software\ScheduledPrintService",
    "C:\ProgramData\ScheduledPrintService",
    "C:\Program Files\Malchut\ScheduledPrintService",
    "C:\Windows\System32\config\systemprofile\AppData\Local\ScheduledPrintService"
)

$LogsFound = @()
foreach ($SearchPath in $SearchPaths) {
    if (Test-Path $SearchPath) {
        $FoundLogs = Get-ChildItem $SearchPath -Filter "*.log" -Recurse -ErrorAction SilentlyContinue
        if ($FoundLogs) {
            Write-Host "  [FOUND] Logs in: $SearchPath" -ForegroundColor Green
            $FoundLogs | ForEach-Object {
                Write-Host "          $($_.FullName) ($([math]::Round($_.Length/1KB, 2)) KB)" -ForegroundColor Gray
                $LogsFound += $_.FullName
            }
        }
    }
}

if ($LogsFound.Count -eq 0) {
    Write-Host "  [WARN] No log files found in any standard location" -ForegroundColor Yellow
}
Write-Host ""

# 5. Check service process for environment variables
Write-Host "[5/6] Checking Service Environment..." -ForegroundColor Yellow

$Service = Get-WmiObject Win32_Service | Where-Object { $_.Name -eq $ServiceName }
if ($Service -and $Service.ProcessId -ne 0) {
    Write-Host "  Process ID: $($Service.ProcessId)" -ForegroundColor Gray
    Write-Host "  Executable: $($Service.PathName)" -ForegroundColor Gray

    # Try to get environment variables (requires admin)
    Write-Host ""
    Write-Host "  [INFO] Environment variables are not directly accessible" -ForegroundColor Gray
    Write-Host "         The service might be using SCHEDULED_PRINT_DATA_ROOT variable" -ForegroundColor Gray
}
Write-Host ""

# 6. Recommend next steps
Write-Host "[6/6] Recommendations..." -ForegroundColor Yellow
Write-Host ""

if ($LogsFound.Count -gt 0) {
    Write-Host "  FOUND LOGS!" -ForegroundColor Green
    Write-Host "  The service IS creating logs, just not where expected." -ForegroundColor Yellow
    Write-Host ""
    Write-Host "  Log locations:" -ForegroundColor Cyan
    $LogsFound | ForEach-Object {
        Write-Host "    - $_" -ForegroundColor White
    }
    Write-Host ""
    Write-Host "  Check these files for details:" -ForegroundColor Yellow
    Write-Host "    Get-Content '$($LogsFound[0])' -Tail 50" -ForegroundColor Gray
} else {
    Write-Host "  No logs found anywhere. This suggests:" -ForegroundColor Red
    Write-Host ""
    Write-Host "  Possible causes:" -ForegroundColor Cyan
    Write-Host "    1. Service failed to initialize Serilog" -ForegroundColor White
    Write-Host "    2. Permission issue preventing log creation" -ForegroundColor White
    Write-Host "    3. Service is in an error state but appears running" -ForegroundColor White
    Write-Host ""
    Write-Host "  Troubleshooting steps:" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "    Step 1: Restart service and check Event Viewer immediately" -ForegroundColor Yellow
    Write-Host "      Restart-Service '$ServiceName'" -ForegroundColor Gray
    Write-Host "      Start-Sleep -Seconds 5" -ForegroundColor Gray
    Write-Host "      Get-WinEvent -FilterHashtable @{LogName='Application'; StartTime=(Get-Date).AddMinutes(-1)} | Where-Object {`$_.Message -like '*Scheduled*'}" -ForegroundColor Gray
    Write-Host ""
    Write-Host "    Step 2: Try running service executable manually (for debugging)" -ForegroundColor Yellow
    Write-Host "      cd 'C:\Program Files\Malchut\ScheduledPrintService'" -ForegroundColor Gray
    Write-Host "      .\ScheduledPrintService.exe" -ForegroundColor Gray
    Write-Host "      (Press Ctrl+C to stop after checking output)" -ForegroundColor Gray
    Write-Host ""
    Write-Host "    Step 3: Check if Chromium download is blocking startup" -ForegroundColor Yellow
    Write-Host "      The service might be stuck downloading Chromium on first run" -ForegroundColor Gray
    Write-Host "      Check if chromium-cache directory exists and has content" -ForegroundColor Gray
    Write-Host ""
    Write-Host "    Step 4: Grant explicit permissions to log directory" -ForegroundColor Yellow
    Write-Host "      icacls '$LogDir' /grant 'NT AUTHORITY\SYSTEM:(OI)(CI)F'" -ForegroundColor Gray
    Write-Host ""
}

Write-Host ""
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""
