#Requires -Version 5.1
<#
.SYNOPSIS
    Comprehensive monitoring script for Scheduled Print Service

.DESCRIPTION
    Monitors service health, logs, configuration, database stats, and disk usage.
    Outputs detailed information to console and saves to a monitoring report file.

.PARAMETER OutputPath
    Path where monitoring reports will be saved. Defaults to E:\Share\server\servern\Software\ScheduledPrintService

.PARAMETER Tail
    Number of log lines to display (default: 50)

.PARAMETER CheckDiskSpace
    Include disk space analysis (default: true)

.PARAMETER CheckDatabase
    Include database statistics (default: true)

.PARAMETER ServicePath
    Override auto-detection and specify service installation path

.PARAMETER DataRoot
    Override auto-detection and specify data root path

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File monitor-service.ps1

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File monitor-service.ps1 -OutputPath "C:\Temp" -Tail 100

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File monitor-service.ps1 -ServicePath "C:\CustomPath\Service" -DataRoot "D:\Data"
#>

param(
    [string]$OutputPath = "E:\Share\server\servern\Software\ScheduledPrintService",
    [int]$Tail = 50,
    [switch]$CheckDiskSpace = $true,
    [switch]$CheckDatabase = $true,
    [string]$ServicePath = "",
    [string]$DataRoot = ""
)

# Configuration
$ServiceName = "Scheduled Print Service"

# Production paths (update these if your installation differs)
$ProductionServicePath = "C:\Program Files\Malchut\ScheduledPrintService"
$ProductionDataRoot = "E:\Share\server\servern\Software\ScheduledPrintService"

# Development paths
$DevProjectRoot = "C:\Users\User\source\repos\BrowserExtensions\scheduled-print-service"
$DevServicePath = Join-Path $DevProjectRoot "ScheduledPrintService"
$DevDataRoot = "$env:ProgramData\ScheduledPrintService"

# Auto-detect environment or use provided paths
if ($ServicePath -ne "" -and $DataRoot -ne "") {
    # User provided both paths
    $Environment = "CUSTOM"
    Write-Host "Using CUSTOM paths (user-specified)" -ForegroundColor Cyan
} elseif ($ServicePath -eq "" -and $DataRoot -eq "") {
    # Auto-detect: check if production paths exist
    if (Test-Path $ProductionServicePath) {
        $ServicePath = $ProductionServicePath
        $DataRoot = $ProductionDataRoot
        $Environment = "PRODUCTION"
        Write-Host "Detected PRODUCTION environment" -ForegroundColor Green
    } elseif (Test-Path $DevServicePath) {
        $ServicePath = $DevServicePath
        $DataRoot = $DevDataRoot
        $Environment = "DEVELOPMENT"
        Write-Host "Detected DEVELOPMENT environment" -ForegroundColor Yellow
    } else {
        Write-Warning "Could not detect service installation path"
        Write-Host "Please specify paths manually with -ServicePath and -DataRoot parameters" -ForegroundColor Yellow
        Write-Host "  Production Service: $ProductionServicePath" -ForegroundColor Gray
        Write-Host "  Production Data: $ProductionDataRoot" -ForegroundColor Gray
        Write-Host "  Development Service: $DevServicePath" -ForegroundColor Gray
        $ServicePath = $ProductionServicePath  # Default to production
        $DataRoot = $ProductionDataRoot
        $Environment = "UNKNOWN"
    }
} else {
    # Partial override - fill in the missing one
    if ($ServicePath -eq "") {
        $ServicePath = if (Test-Path $ProductionServicePath) { $ProductionServicePath } else { $DevServicePath }
    }
    if ($DataRoot -eq "") {
        $DataRoot = if (Test-Path $ProductionDataRoot) { $ProductionDataRoot } else { $DevDataRoot }
    }
    $Environment = "MIXED"
    Write-Host "Using MIXED paths (partial override)" -ForegroundColor Cyan
}

Write-Host "Service Path: $ServicePath" -ForegroundColor Gray
Write-Host "Data Root: $DataRoot" -ForegroundColor Gray

# Colors for console output
function Write-Header {
    param([string]$Text)
    Write-Host "`n========================================" -ForegroundColor Cyan
    Write-Host " $Text" -ForegroundColor Cyan
    Write-Host "========================================" -ForegroundColor Cyan
}

function Write-Success {
    param([string]$Text)
    Write-Host "[OK] $Text" -ForegroundColor Green
}

function Write-Warning {
    param([string]$Text)
    Write-Host "[WARN] $Text" -ForegroundColor Yellow
}

function Write-Error {
    param([string]$Text)
    Write-Host "[ERROR] $Text" -ForegroundColor Red
}

function Write-Info {
    param([string]$Text)
    Write-Host "[INFO] $Text" -ForegroundColor Gray
}

# Initialize monitoring report
$Timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$ReportFile = Join-Path $OutputPath "monitoring-report-$Timestamp.txt"

# Ensure output directory exists
if (-not (Test-Path $OutputPath)) {
    New-Item -ItemType Directory -Path $OutputPath -Force | Out-Null
    Write-Success "Created output directory: $OutputPath"
}

# Start building report
$Report = @"
================================================================================
SCHEDULED PRINT SERVICE - MONITORING REPORT
Generated: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
Environment: $Environment
================================================================================

PATHS
-----
Service Installation: $ServicePath
Data Root: $DataRoot
Output/Reports: $OutputPath

"@

# Function to add to report
function Add-ToReport {
    param([string]$Text)
    $script:Report += "$Text`n"
}

# ============================================================================
# 1. SERVICE STATUS
# ============================================================================
Write-Header "Service Status"

try {
    $Service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue

    if ($null -eq $Service) {
        Write-Error "Service '$ServiceName' not found"
        Add-ToReport "SERVICE STATUS: NOT INSTALLED"
    } else {
        $StatusColor = if ($Service.Status -eq "Running") { "Green" } else { "Red" }
        Write-Host "Service Name: " -NoNewline; Write-Host $Service.Name -ForegroundColor White
        Write-Host "Display Name: " -NoNewline; Write-Host $Service.DisplayName -ForegroundColor White
        Write-Host "Status: " -NoNewline; Write-Host $Service.Status -ForegroundColor $StatusColor
        Write-Host "Start Type: " -NoNewline; Write-Host $Service.StartType -ForegroundColor White

        Add-ToReport @"
SERVICE STATUS
--------------
Name: $($Service.Name)
Display Name: $($Service.DisplayName)
Status: $($Service.Status)
Start Type: $($Service.StartType)
Can Stop: $($Service.CanStop)
Can Pause: $($Service.CanPauseAndContinue)

"@

        # Get service process details if running
        if ($Service.Status -eq "Running") {
            try {
                $ServiceProcess = Get-WmiObject Win32_Service | Where-Object { $_.Name -eq $ServiceName }
                if ($ServiceProcess) {
                    $Process = Get-Process -Id $ServiceProcess.ProcessId -ErrorAction SilentlyContinue
                    if ($Process) {
                        $MemoryMB = [math]::Round($Process.WorkingSet64 / 1MB, 2)
                        $CpuTime = $Process.TotalProcessorTime

                        Write-Host "Process ID: " -NoNewline; Write-Host $Process.Id -ForegroundColor White
                        Write-Host "Memory (MB): " -NoNewline; Write-Host $MemoryMB -ForegroundColor White
                        Write-Host "CPU Time: " -NoNewline; Write-Host $CpuTime -ForegroundColor White
                        Write-Host "Start Time: " -NoNewline; Write-Host $Process.StartTime -ForegroundColor White

                        $Uptime = (Get-Date) - $Process.StartTime
                        Write-Host "Uptime: " -NoNewline; Write-Host "$($Uptime.Days)d $($Uptime.Hours)h $($Uptime.Minutes)m" -ForegroundColor White

                        Add-ToReport @"
PROCESS DETAILS
---------------
Process ID: $($Process.Id)
Memory: $MemoryMB MB
CPU Time: $CpuTime
Start Time: $($Process.StartTime)
Uptime: $($Uptime.Days)d $($Uptime.Hours)h $($Uptime.Minutes)m
Threads: $($Process.Threads.Count)

"@
                    }
                }
            } catch {
                Write-Warning "Could not retrieve process details: $_"
            }
        }
    }
} catch {
    Write-Error "Error checking service status: $_"
    Add-ToReport "SERVICE STATUS ERROR: $_"
}

# ============================================================================
# 2. CONFIGURATION
# ============================================================================
Write-Header "Configuration"

$AppSettingsPath = Join-Path $ServicePath "appsettings.json"
if (Test-Path $AppSettingsPath) {
    try {
        $Config = Get-Content $AppSettingsPath -Raw | ConvertFrom-Json

        Write-Info "Log Level: $($Config.Serilog.MinimumLevel.Default)"
        Write-Info "Printer Mode: $($Config.Printer.Mode)"
        Write-Info "Printer Name: $($Config.Printer.PrinterName)"
        Write-Info "Demo Enabled: $($Config.Demo.Enabled)"
        Write-Info "Scheduler Enabled: $($Config.Scheduler.Enabled)"
        Write-Info "Email Enabled: $($Config.Email.Enabled)"

        Add-ToReport @"
CONFIGURATION (appsettings.json)
--------------------------------
Log Level: $($Config.Serilog.MinimumLevel.Default)

PDF Settings:
  Navigation Timeout: $($Config.Pdf.NavigationTimeoutSeconds)s
  Wait for Network Idle: $($Config.Pdf.WaitForNetworkIdleMs)ms
  Wait for Selector: $($Config.Pdf.WaitForSelector)
  Capture Screenshot: $($Config.Pdf.CaptureDiagnosticScreenshot)
  Cache Directory: $($Config.Pdf.CacheDirectory)

Printer Settings:
  Mode: $($Config.Printer.Mode)
  Printer Name: $($Config.Printer.PrinterName)
  Output Directory: $($Config.Printer.OutputDirectory)
  Spool Timeout: $($Config.Printer.SpoolTimeoutSeconds)s

Scheduler:
  Enabled: $($Config.Scheduler.Enabled)
  Interval: $($Config.Scheduler.IntervalSeconds)s
  URLs: $($Config.Scheduler.Urls.Count)

Email:
  Enabled: $($Config.Email.Enabled)
  SMTP Host: $($Config.Email.SmtpHost)
  From: $($Config.Email.From)
  To: $($Config.Email.To)

Demo:
  Enabled: $($Config.Demo.Enabled)
  URL: $($Config.Demo.Url)

"@
    } catch {
        Write-Warning "Could not parse configuration: $_"
        Add-ToReport "CONFIG ERROR: $_`n"
    }
} else {
    Write-Warning "Configuration file not found: $AppSettingsPath"
    Add-ToReport "CONFIG FILE NOT FOUND: $AppSettingsPath`n"
}

# ============================================================================
# 3. DATABASE STATUS
# ============================================================================
if ($CheckDatabase) {
    Write-Header "Database Status"

    $DatabasePath = Join-Path $ServicePath "api_config.db"
    if (Test-Path $DatabasePath) {
        try {
            $DbSize = (Get-Item $DatabasePath).Length
            $DbSizeKB = [math]::Round($DbSize / 1KB, 2)
            $DbModified = (Get-Item $DatabasePath).LastWriteTime

            Write-Success "Database found: $DatabasePath"
            Write-Info "Size: $DbSizeKB KB"
            Write-Info "Last Modified: $DbModified"

            # Try PowerShell-based SQLite query first (no external tools needed)
            try {
                Add-Type -Path "System.Data.SQLite.dll" -ErrorAction SilentlyContinue
            } catch {
                # DLL not available, try loading from NuGet or system
            }

            $DbQuerySuccess = $false
            $DbInfo = @{
                PrimaryApiCount = 0
                SubActionCount = 0
                ScheduleCount = 0
                EnabledApis = @()
                ScheduledApis = @()
            }

            # Method 1: Try using System.Data.SQLite if available
            try {
                $Connection = New-Object System.Data.SQLite.SQLiteConnection("Data Source=$DatabasePath;Version=3;Read Only=True;")
                $Connection.Open()

                # Query counts
                $Command = $Connection.CreateCommand()
                $Command.CommandText = "SELECT COUNT(*) FROM PrimaryApi"
                $DbInfo.PrimaryApiCount = $Command.ExecuteScalar()

                $Command.CommandText = "SELECT COUNT(*) FROM SubAction"
                $DbInfo.SubActionCount = $Command.ExecuteScalar()

                $Command.CommandText = "SELECT COUNT(*) FROM Schedule"
                $DbInfo.ScheduleCount = $Command.ExecuteScalar()

                # Get enabled APIs
                $Command.CommandText = "SELECT ApiNumber, ApiName, IsEnabled FROM PrimaryApi"
                $Reader = $Command.ExecuteReader()
                while ($Reader.Read()) {
                    $ApiInfo = @{
                        ApiNumber = $Reader["ApiNumber"]
                        ApiName = $Reader["ApiName"]
                        IsEnabled = $Reader["IsEnabled"]
                    }
                    if ($ApiInfo.IsEnabled -eq 1) {
                        $DbInfo.EnabledApis += "$($ApiInfo.ApiNumber) - $($ApiInfo.ApiName)"
                    }
                }
                $Reader.Close()

                # Get schedules with APIs
                $Command.CommandText = @"
SELECT
    s.Id,
    s.ScheduleName,
    s.CronExpression,
    s.IsEnabled,
    sa.ApiNumber,
    sa.ExecutionOrder
FROM Schedule s
LEFT JOIN ScheduleApi sa ON s.Id = sa.ScheduleId
ORDER BY s.Id, sa.ExecutionOrder
"@
                $Reader = $Command.ExecuteReader()
                $CurrentSchedule = $null
                while ($Reader.Read()) {
                    $ScheduleId = $Reader["Id"]
                    if ($CurrentSchedule -eq $null -or $CurrentSchedule.Id -ne $ScheduleId) {
                        if ($CurrentSchedule -ne $null) {
                            $DbInfo.ScheduledApis += $CurrentSchedule
                        }
                        $CurrentSchedule = @{
                            Id = $ScheduleId
                            Name = $Reader["ScheduleName"]
                            Cron = $Reader["CronExpression"]
                            Enabled = $Reader["IsEnabled"]
                            Apis = @()
                        }
                    }
                    if (-not $Reader.IsDBNull(4)) {  # Has API
                        $CurrentSchedule.Apis += "$($Reader["ApiNumber"]) (Order: $($Reader["ExecutionOrder"]))"
                    }
                }
                if ($CurrentSchedule -ne $null) {
                    $DbInfo.ScheduledApis += $CurrentSchedule
                }
                $Reader.Close()

                $Connection.Close()
                $DbQuerySuccess = $true
                Write-Success "Database queried successfully (using System.Data.SQLite)"
            }
            catch {
                # Method 2: Try sqlite3 command line
                $Sqlite3Path = Get-Command sqlite3 -ErrorAction SilentlyContinue
                if ($Sqlite3Path) {
                    try {
                        Write-Info "Using sqlite3 command line..."

                        $DbInfo.PrimaryApiCount = & sqlite3 $DatabasePath "SELECT COUNT(*) FROM PrimaryApi;" 2>$null
                        $DbInfo.SubActionCount = & sqlite3 $DatabasePath "SELECT COUNT(*) FROM SubAction;" 2>$null
                        $DbInfo.ScheduleCount = & sqlite3 $DatabasePath "SELECT COUNT(*) FROM Schedule;" 2>$null

                        # Get enabled APIs
                        $EnabledApisRaw = & sqlite3 $DatabasePath "SELECT ApiNumber || ' - ' || ApiName FROM PrimaryApi WHERE IsEnabled = 1;" 2>$null
                        $DbInfo.EnabledApis = $EnabledApisRaw -split "`n" | Where-Object { $_ }

                        # Get schedules
                        $SchedulesRaw = & sqlite3 $DatabasePath "SELECT Id, ScheduleName, CronExpression, IsEnabled FROM Schedule;" 2>$null
                        foreach ($ScheduleLine in ($SchedulesRaw -split "`n" | Where-Object { $_ })) {
                            $Parts = $ScheduleLine -split '\|'
                            if ($Parts.Count -ge 4) {
                                $ScheduleId = $Parts[0]
                                $ScheduleApis = & sqlite3 $DatabasePath "SELECT ApiNumber || ' (Order: ' || ExecutionOrder || ')' FROM ScheduleApi WHERE ScheduleId = $ScheduleId ORDER BY ExecutionOrder;" 2>$null
                                $DbInfo.ScheduledApis += @{
                                    Id = $ScheduleId
                                    Name = $Parts[1]
                                    Cron = $Parts[2]
                                    Enabled = $Parts[3]
                                    Apis = ($ScheduleApis -split "`n" | Where-Object { $_ })
                                }
                            }
                        }

                        $DbQuerySuccess = $true
                        Write-Success "Database queried successfully (using sqlite3)"
                    }
                    catch {
                        Write-Warning "sqlite3 query failed: $_"
                    }
                }
            }

            # Display results
            if ($DbQuerySuccess) {
                Write-Host "`nDatabase Statistics:" -ForegroundColor Yellow
                Write-Info "Primary APIs: $($DbInfo.PrimaryApiCount)"
                Write-Info "Sub-Actions: $($DbInfo.SubActionCount)"
                Write-Info "Schedules: $($DbInfo.ScheduleCount)"

                if ($DbInfo.EnabledApis.Count -gt 0) {
                    Write-Host "`nEnabled APIs:" -ForegroundColor Yellow
                    $DbInfo.EnabledApis | ForEach-Object {
                        Write-Host "  - $_" -ForegroundColor White
                    }
                } else {
                    Write-Warning "No enabled APIs found!"
                }

                if ($DbInfo.ScheduledApis.Count -gt 0) {
                    Write-Host "`nScheduled API Workflows:" -ForegroundColor Yellow
                    foreach ($Schedule in $DbInfo.ScheduledApis) {
                        $EnabledText = if ($Schedule.Enabled -eq 1) { "ENABLED" } else { "DISABLED" }
                        $EnabledColor = if ($Schedule.Enabled -eq 1) { "Green" } else { "Red" }

                        Write-Host "  Schedule #$($Schedule.Id): " -NoNewline -ForegroundColor Cyan
                        Write-Host $Schedule.Name -NoNewline -ForegroundColor White
                        Write-Host " [$EnabledText]" -ForegroundColor $EnabledColor
                        Write-Host "    Cron: " -NoNewline -ForegroundColor Gray
                        Write-Host $Schedule.Cron -ForegroundColor White

                        if ($Schedule.Apis.Count -gt 0) {
                            Write-Host "    APIs to execute:" -ForegroundColor Gray
                            $Schedule.Apis | ForEach-Object {
                                Write-Host "      - $_" -ForegroundColor White
                            }
                        } else {
                            Write-Host "    No APIs assigned" -ForegroundColor Yellow
                        }
                    }
                } else {
                    Write-Warning "No schedules configured!"
                }

                Add-ToReport @"
DATABASE STATUS
---------------
Path: $DatabasePath
Size: $DbSizeKB KB
Last Modified: $DbModified

Statistics:
  Primary APIs: $($DbInfo.PrimaryApiCount)
  Sub-Actions: $($DbInfo.SubActionCount)
  Schedules: $($DbInfo.ScheduleCount)

Enabled APIs:
$(if ($DbInfo.EnabledApis.Count -eq 0) { "  (none)" } else { $DbInfo.EnabledApis | ForEach-Object { "  - $_" } | Out-String })

Scheduled Workflows:
$(if ($DbInfo.ScheduledApis.Count -eq 0) { "  (none)" } else {
    foreach ($Schedule in $DbInfo.ScheduledApis) {
        $EnabledText = if ($Schedule.Enabled -eq 1) { "ENABLED" } else { "DISABLED" }
        "  Schedule #$($Schedule.Id): $($Schedule.Name) [$EnabledText]"
        "    Cron: $($Schedule.Cron)"
        if ($Schedule.Apis.Count -gt 0) {
            "    APIs: " + ($Schedule.Apis -join ", ")
        } else {
            "    (No APIs assigned)"
        }
        ""
    }
})

"@
            } else {
                Write-Warning "Could not query database - install System.Data.SQLite or sqlite3"
                Write-Host "`nTo query database manually:" -ForegroundColor Yellow
                Write-Host "  1. Download sqlite3: https://www.sqlite.org/download.html" -ForegroundColor Gray
                Write-Host "  2. Run: sqlite3 '$DatabasePath' 'SELECT * FROM Schedule;'" -ForegroundColor Gray

                Add-ToReport @"
DATABASE STATUS
---------------
Path: $DatabasePath
Size: $DbSizeKB KB
Last Modified: $DbModified

(Could not query database - System.Data.SQLite or sqlite3 not available)

To query manually:
  1. Download sqlite3 from https://www.sqlite.org/download.html
  2. Run: sqlite3 "$DatabasePath" "SELECT * FROM Schedule;"

"@
            }
        } catch {
            Write-Error "Error querying database: $_"
            Add-ToReport "DATABASE ERROR: $_`n"
        }
    } else {
        Write-Warning "Database not found: $DatabasePath"
        Add-ToReport "DATABASE NOT FOUND: $DatabasePath`n"
    }
}

# ============================================================================
# 4. LOG FILES
# ============================================================================
Write-Header "Recent Log Entries"

# Try multiple possible log locations
$LogDir = Join-Path $DataRoot "logs"
$AlternateLogDirs = @(
    "E:\Share\server\servern\Software\ScheduledPrintService\logs",
    "$env:ProgramData\ScheduledPrintService\logs",
    (Join-Path $ServicePath "logs")
)

# Find first existing log directory
if (-not (Test-Path $LogDir)) {
    foreach ($AltDir in $AlternateLogDirs) {
        if (Test-Path $AltDir) {
            $LogDir = $AltDir
            Write-Info "Using alternate log directory: $LogDir"
            break
        }
    }
}

if (Test-Path $LogDir) {
    $LogFiles = Get-ChildItem $LogDir -Filter "*.log" | Sort-Object LastWriteTime -Descending

    if ($LogFiles.Count -gt 0) {
        $LatestLog = $LogFiles[0]
        Write-Success "Latest log: $($LatestLog.Name)"
        Write-Info "Size: $([math]::Round($LatestLog.Length / 1KB, 2)) KB"
        Write-Info "Modified: $($LatestLog.LastWriteTime)"

        Add-ToReport @"
LOG FILES
---------
Latest Log: $($LatestLog.Name)
Size: $([math]::Round($LatestLog.Length / 1KB, 2)) KB
Modified: $($LatestLog.LastWriteTime)

Recent Log Entries (last $Tail lines):
$("-" * 80)

"@

        Write-Host "`nLast $Tail log entries:" -ForegroundColor Yellow
        $LogContent = Get-Content $LatestLog.FullName -Tail $Tail
        $LogContent | ForEach-Object {
            $Line = $_
            if ($Line -match "\[ERR\]" -or $Line -match "Error" -or $Line -match "Exception") {
                Write-Host $Line -ForegroundColor Red
            } elseif ($Line -match "\[WRN\]" -or $Line -match "Warning") {
                Write-Host $Line -ForegroundColor Yellow
            } elseif ($Line -match "\[INF\]" -or $Line -match "Information") {
                Write-Host $Line -ForegroundColor White
            } else {
                Write-Host $Line -ForegroundColor Gray
            }
        }

        Add-ToReport ($LogContent -join "`n")
        Add-ToReport "`n$("-" * 80)`n"

        # Count errors and warnings
        $ErrorCount = ($LogContent | Select-String -Pattern "\[ERR\]|Error|Exception" -AllMatches).Matches.Count
        $WarningCount = ($LogContent | Select-String -Pattern "\[WRN\]|Warning" -AllMatches).Matches.Count

        Write-Host "`nLog Summary:" -ForegroundColor Yellow
        Write-Host "Errors: " -NoNewline
        if ($ErrorCount -gt 0) { Write-Host $ErrorCount -ForegroundColor Red } else { Write-Host "0" -ForegroundColor Green }
        Write-Host "Warnings: " -NoNewline
        if ($WarningCount -gt 0) { Write-Host $WarningCount -ForegroundColor Yellow } else { Write-Host "0" -ForegroundColor Green }

        Add-ToReport @"
LOG SUMMARY
-----------
Errors in last $Tail lines: $ErrorCount
Warnings in last $Tail lines: $WarningCount

"@
    } else {
        Write-Warning "No log files found in $LogDir"
        Add-ToReport "NO LOG FILES FOUND in $LogDir`n"
    }
} else {
    Write-Warning "Log directory not found"
    Write-Host "Searched locations:" -ForegroundColor Yellow
    Write-Host "  - $LogDir" -ForegroundColor Gray
    foreach ($AltDir in $AlternateLogDirs) {
        Write-Host "  - $AltDir" -ForegroundColor Gray
    }
    Add-ToReport @"
LOG DIRECTORY NOT FOUND
Searched locations:
  - $LogDir
$(foreach ($AltDir in $AlternateLogDirs) { "  - $AltDir" })

"@
}

# ============================================================================
# 5. OUTPUT FILES & PRINT HISTORY
# ============================================================================
Write-Header "Print Output & History"

# Try multiple possible output locations
$OutDir = Join-Path $DataRoot "out"
$AlternateOutDirs = @(
    "E:\Share\server\servern\Software\ScheduledPrintService\out",
    "$env:ProgramData\ScheduledPrintService\out",
    (Join-Path $ServicePath "out")
)

# Find first existing output directory
if (-not (Test-Path $OutDir)) {
    foreach ($AltDir in $AlternateOutDirs) {
        if (Test-Path $AltDir) {
            $OutDir = $AltDir
            Write-Info "Using alternate output directory: $OutDir"
            break
        }
    }
}

if (Test-Path $OutDir) {
    $PdfFiles = Get-ChildItem $OutDir -Filter "*.pdf" | Sort-Object LastWriteTime -Descending

    Write-Info "PDF Output Directory: $OutDir"
    Write-Info "Total PDFs: $($PdfFiles.Count)"

    if ($PdfFiles.Count -gt 0) {
        $RecentPdfs = $PdfFiles | Select-Object -First 10
        Write-Host "`nRecent PDF Files (last 10):" -ForegroundColor Yellow

        $PdfList = @()
        $RecentPdfs | ForEach-Object {
            $SizeMB = [math]::Round($_.Length / 1MB, 2)
            $Line = "  $($_.Name) - $SizeMB MB - $($_.LastWriteTime)"
            Write-Host $Line -ForegroundColor White
            $PdfList += $Line
        }

        Add-ToReport @"
PRINT OUTPUT
------------
Output Directory: $OutDir
Total PDFs: $($PdfFiles.Count)

Recent PDFs (last 10):
$($PdfList -join "`n")

"@
    } else {
        Write-Info "No PDF files found"
        Add-ToReport "PRINT OUTPUT: No PDFs found`n"
    }
} else {
    Write-Warning "Output directory not found"
    Write-Host "Searched locations:" -ForegroundColor Yellow
    Write-Host "  - $OutDir" -ForegroundColor Gray
    foreach ($AltDir in $AlternateOutDirs) {
        Write-Host "  - $AltDir" -ForegroundColor Gray
    }
    Add-ToReport @"
OUTPUT DIRECTORY NOT FOUND
Searched locations:
  - $OutDir
$(foreach ($AltDir in $AlternateOutDirs) { "  - $AltDir" })

"@
}

# Check processed orders file (try multiple locations)
$ProcessedOrdersPath = Join-Path $DataRoot "processed-orders.txt"
$AlternateProcessedPaths = @(
    "E:\Share\server\servern\Software\ScheduledPrintService\processed-orders.txt",
    "$env:ProgramData\ScheduledPrintService\processed-orders.txt",
    (Join-Path $ServicePath "processed-orders.txt")
)

if (-not (Test-Path $ProcessedOrdersPath)) {
    foreach ($AltPath in $AlternateProcessedPaths) {
        if (Test-Path $AltPath) {
            $ProcessedOrdersPath = $AltPath
            break
        }
    }
}

if (Test-Path $ProcessedOrdersPath) {
    $ProcessedOrders = Get-Content $ProcessedOrdersPath
    Write-Info "Processed Orders: $($ProcessedOrders.Count) IDs tracked"

    Add-ToReport @"
PROCESSED ORDERS
----------------
File: $ProcessedOrdersPath
Total IDs: $($ProcessedOrders.Count)

"@
} else {
    Write-Info "No processed orders file found"
    Add-ToReport "PROCESSED ORDERS: No tracking file found`n"
}

# ============================================================================
# 6. DISK SPACE & CACHE
# ============================================================================
if ($CheckDiskSpace) {
    Write-Header "Disk Space & Cache"

    # Check data root directory size
    if (Test-Path $DataRoot) {
        try {
            $DataDirSize = (Get-ChildItem $DataRoot -Recurse -ErrorAction SilentlyContinue |
                           Measure-Object -Property Length -Sum).Sum
            $DataDirSizeMB = [math]::Round($DataDirSize / 1MB, 2)

            Write-Info "Data Root: $DataRoot"
            Write-Info "Total Size: $DataDirSizeMB MB"

            # Check subdirectories
            $Subdirs = Get-ChildItem $DataRoot -Directory -ErrorAction SilentlyContinue
            foreach ($Dir in $Subdirs) {
                $DirSize = (Get-ChildItem $Dir.FullName -Recurse -ErrorAction SilentlyContinue |
                           Measure-Object -Property Length -Sum).Sum
                $DirSizeMB = [math]::Round($DirSize / 1MB, 2)
                Write-Info "  $($Dir.Name): $DirSizeMB MB"
            }

            Add-ToReport @"
DISK SPACE
----------
Data Root: $DataRoot
Total Size: $DataDirSizeMB MB

Subdirectories:
$(foreach ($Dir in $Subdirs) {
    $DirSize = (Get-ChildItem $Dir.FullName -Recurse -ErrorAction SilentlyContinue |
               Measure-Object -Property Length -Sum).Sum
    $DirSizeMB = [math]::Round($DirSize / 1MB, 2)
    "  $($Dir.Name): $DirSizeMB MB"
})

"@
        } catch {
            Write-Warning "Could not calculate directory size: $_"
        }
    }

    # Check drive space
    try {
        $Drive = (Get-Item $DataRoot).PSDrive
        $FreeSpaceGB = [math]::Round($Drive.Free / 1GB, 2)
        $UsedSpaceGB = [math]::Round($Drive.Used / 1GB, 2)
        $TotalSpaceGB = [math]::Round(($Drive.Free + $Drive.Used) / 1GB, 2)
        $PercentFree = [math]::Round(($Drive.Free / ($Drive.Free + $Drive.Used)) * 100, 1)

        Write-Host "`nDrive $($Drive.Name) Space:" -ForegroundColor Yellow
        Write-Info "Total: $TotalSpaceGB GB"
        Write-Info "Used: $UsedSpaceGB GB"
        Write-Info "Free: $FreeSpaceGB GB ($PercentFree%)"

        if ($PercentFree -lt 10) {
            Write-Error "Low disk space! Less than 10% free"
        } elseif ($PercentFree -lt 20) {
            Write-Warning "Disk space getting low (less than 20% free)"
        }

        Add-ToReport @"
DRIVE SPACE ($($Drive.Name))
-----------
Total: $TotalSpaceGB GB
Used: $UsedSpaceGB GB
Free: $FreeSpaceGB GB ($PercentFree%)

"@
    } catch {
        Write-Warning "Could not check drive space: $_"
    }
}

# ============================================================================
# 7. SYSTEM INFORMATION
# ============================================================================
Write-Header "System Information"

$OSInfo = Get-CimInstance Win32_OperatingSystem
$CompInfo = Get-CimInstance Win32_ComputerSystem

Write-Info "Computer: $($CompInfo.Name)"
Write-Info "OS: $($OSInfo.Caption) $($OSInfo.Version)"
Write-Info "Architecture: $($OSInfo.OSArchitecture)"
Write-Info "Last Boot: $($OSInfo.LastBootUpTime)"

$Uptime = (Get-Date) - $OSInfo.LastBootUpTime
Write-Info "System Uptime: $($Uptime.Days)d $($Uptime.Hours)h $($Uptime.Minutes)m"

$MemoryGB = [math]::Round($CompInfo.TotalPhysicalMemory / 1GB, 2)
$FreeMemoryGB = [math]::Round($OSInfo.FreePhysicalMemory / 1MB, 2)
Write-Info "Total Memory: $MemoryGB GB"
Write-Info "Free Memory: $FreeMemoryGB GB"

Add-ToReport @"
SYSTEM INFORMATION
------------------
Computer: $($CompInfo.Name)
OS: $($OSInfo.Caption) $($OSInfo.Version)
Architecture: $($OSInfo.OSArchitecture)
Last Boot: $($OSInfo.LastBootUpTime)
System Uptime: $($Uptime.Days)d $($Uptime.Hours)h $($Uptime.Minutes)m
Total Memory: $MemoryGB GB
Free Memory: $FreeMemoryGB GB

"@

# ============================================================================
# 8. HEALTH CHECK SUMMARY
# ============================================================================
Write-Header "Health Check Summary"

$HealthIssues = @()
$HealthOK = @()

# Check service
if ($null -ne $Service -and $Service.Status -eq "Running") {
    $HealthOK += "Service is running"
    Write-Success "Service is running"
} else {
    $HealthIssues += "Service is not running"
    Write-Error "Service is not running"
}

# Check logs
if ($LogFiles.Count -gt 0 -and $ErrorCount -eq 0) {
    $HealthOK += "No errors in recent logs"
    Write-Success "No errors in recent logs"
} elseif ($ErrorCount -gt 0) {
    $HealthIssues += "$ErrorCount errors found in recent logs"
    Write-Warning "$ErrorCount errors found in recent logs"
}

# Check disk space
if ($PercentFree -gt 20) {
    $HealthOK += "Disk space OK ($PercentFree% free)"
    Write-Success "Disk space OK ($PercentFree% free)"
} else {
    $HealthIssues += "Disk space low ($PercentFree% free)"
    Write-Warning "Disk space low ($PercentFree% free)"
}

# Check database
if (Test-Path $DatabasePath) {
    $HealthOK += "Database file exists"
    Write-Success "Database file exists"
} else {
    $HealthIssues += "Database file not found"
    Write-Warning "Database file not found"
}

Add-ToReport @"
HEALTH CHECK SUMMARY
--------------------
Status: $(if ($HealthIssues.Count -eq 0) { "HEALTHY" } else { "ISSUES DETECTED" })

OK:
$($HealthOK | ForEach-Object { "  [OK] $_" } | Out-String)

Issues:
$(if ($HealthIssues.Count -eq 0) { "  None" } else { $HealthIssues | ForEach-Object { "  [!] $_" } | Out-String })

"@

# ============================================================================
# SAVE REPORT
# ============================================================================
Write-Header "Saving Report"

try {
    Add-ToReport @"
================================================================================
END OF REPORT
Generated: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
================================================================================
"@

    $Report | Out-File -FilePath $ReportFile -Encoding UTF8
    Write-Success "Report saved: $ReportFile"

    # Also create a "latest" symlink/copy
    $LatestReportFile = Join-Path $OutputPath "monitoring-report-latest.txt"
    $Report | Out-File -FilePath $LatestReportFile -Encoding UTF8
    Write-Success "Latest report: $LatestReportFile"

    # Display summary
    Write-Host "`n" -NoNewline
    Write-Host "Report Size: " -NoNewline
    Write-Host "$([math]::Round((Get-Item $ReportFile).Length / 1KB, 2)) KB" -ForegroundColor White

} catch {
    Write-Error "Failed to save report: $_"
}

Write-Host "`n"
Write-Host "Monitoring complete!" -ForegroundColor Green
Write-Host "Output saved to: $OutputPath" -ForegroundColor Cyan
Write-Host "`n"