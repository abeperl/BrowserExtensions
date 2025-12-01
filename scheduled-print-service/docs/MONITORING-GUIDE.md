# Scheduled Print Service - Monitoring Guide

## Quick Start

```powershell
# Run monitoring with auto-detection
cd C:\Users\User\source\repos\BrowserExtensions\scheduled-print-service
powershell -ExecutionPolicy Bypass -File monitor-service.ps1
```

The script will automatically detect your environment (Production or Development) and use the appropriate paths.

---

## Production Paths

The production service is installed at:

- **Service Installation**: `C:\Program Files\Malchut\ScheduledPrintService`
  - Contains: `appsettings.json`, `api_config.db`, service executables

- **Data Root**: `E:\Share\server\servern\Software\ScheduledPrintService`
  - Contains: `logs\`, `out\`, `processed-orders.txt`, `chromium-cache\`

- **Logs**: `E:\Share\server\servern\Software\ScheduledPrintService\logs`
  - Format: `scheduled-print-service-YYYYMMDD.log`
  - Rolling daily logs with Serilog

- **Output PDFs**: `E:\Share\server\servern\Software\ScheduledPrintService\out`
  - Generated PDF files from print jobs

- **Monitoring Reports**: `E:\Share\server\servern\Software\ScheduledPrintService`
  - Report files: `monitoring-report-YYYYMMDD_HHMMSS.txt`
  - Latest: `monitoring-report-latest.txt`

---

## Development Paths

Development environment paths (auto-detected if production not found):

- **Service Installation**: `C:\Users\User\source\repos\BrowserExtensions\scheduled-print-service\ScheduledPrintService`
- **Data Root**: `C:\ProgramData\ScheduledPrintService`

---

## Monitoring Script Features

### 1. Auto-Detection
The script automatically detects whether you're running in:
- **PRODUCTION**: Uses paths under `C:\Program Files\Malchut` and `E:\Share\...`
- **DEVELOPMENT**: Uses repo and `ProgramData` paths
- **CUSTOM**: When you specify manual paths

### 2. What It Monitors

#### Service Status
- Running/stopped state
- Process ID, memory usage, CPU time
- Uptime and start time

#### Configuration
- Reads `appsettings.json` from service installation
- Shows PDF, printer, scheduler, email, and demo settings
- Validates configuration completeness

#### Database
- Checks `api_config.db` existence and size
- Queries Primary APIs, Sub-Actions, Schedules (if sqlite3 available)
- Shows enabled APIs

#### Logs
- Displays recent log entries (default: last 50 lines)
- Color-coded by severity (errors, warnings, info)
- Counts errors and warnings
- Shows log file sizes

#### Print Output
- Lists recent PDF files
- Shows processed order counts
- Tracks print history

#### Disk Space
- Data directory size breakdown
- Drive space analysis with warnings
- Cache size monitoring

#### System Information
- OS version and architecture
- System uptime
- Memory usage

#### Health Check Summary
- Overall system health status
- Quick issue identification
- Pass/fail checks for key components

---

## Usage Examples

### Basic Monitoring
```powershell
powershell -ExecutionPolicy Bypass -File monitor-service.ps1
```

### Show More Log Lines
```powershell
powershell -ExecutionPolicy Bypass -File monitor-service.ps1 -Tail 100
```

### Save Reports to Different Location
```powershell
powershell -ExecutionPolicy Bypass -File monitor-service.ps1 -OutputPath "C:\Monitoring\Reports"
```

### Manual Path Override
```powershell
powershell -ExecutionPolicy Bypass -File monitor-service.ps1 `
    -ServicePath "C:\Program Files\Malchut\ScheduledPrintService" `
    -DataRoot "E:\Share\server\servern\Software\ScheduledPrintService"
```

### Skip Disk Space Check (Faster)
```powershell
powershell -ExecutionPolicy Bypass -File monitor-service.ps1 -CheckDiskSpace:$false
```

### Skip Database Queries
```powershell
powershell -ExecutionPolicy Bypass -File monitor-service.ps1 -CheckDatabase:$false
```

---

## Output

The monitoring script produces:

1. **Console Output**: Color-coded, real-time display
   - Green: Success/healthy
   - Yellow: Warnings
   - Red: Errors
   - White/Gray: Info

2. **Report File**: `monitoring-report-YYYYMMDD_HHMMSS.txt`
   - Complete monitoring report
   - Timestamped for historical tracking
   - Saved to output path

3. **Latest Report**: `monitoring-report-latest.txt`
   - Always points to most recent report
   - Easy to reference without timestamp

---

## Troubleshooting

### "Service not found"
- Check if service is installed: `Get-Service "Scheduled Print Service"`
- Verify installation path exists

### "Configuration file not found"
- Check: `C:\Program Files\Malchut\ScheduledPrintService\appsettings.json`
- If missing, service may not be properly installed

### "Database not found"
- Check: `C:\Program Files\Malchut\ScheduledPrintService\api_config.db`
- Run migration script if needed: `migrate-config-to-db.ps1`

### "Log directory not found"
- Check: `E:\Share\server\servern\Software\ScheduledPrintService\logs`
- Verify service has write permissions
- Check service configuration for `SCHEDULED_PRINT_DATA_ROOT` environment variable

### "sqlite3 not found"
- Database statistics require `sqlite3.exe` in PATH
- Download from: https://www.sqlite.org/download.html
- Extract to `C:\Windows\System32` or add to PATH

### Service Shows Wrong Paths
Use manual override:
```powershell
powershell -ExecutionPolicy Bypass -File monitor-service.ps1 `
    -ServicePath "C:\Your\Service\Path" `
    -DataRoot "E:\Your\Data\Path"
```

---

## Scheduled Monitoring

### Windows Task Scheduler Setup

1. **Create Scheduled Task**:
   ```powershell
   $Action = New-ScheduledTaskAction -Execute "PowerShell.exe" `
       -Argument "-ExecutionPolicy Bypass -File C:\Users\User\source\repos\BrowserExtensions\scheduled-print-service\monitor-service.ps1"

   $Trigger = New-ScheduledTaskTrigger -Daily -At 8AM

   $Settings = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries

   Register-ScheduledTask -TaskName "Monitor Scheduled Print Service" `
       -Action $Action -Trigger $Trigger -Settings $Settings `
       -Description "Daily monitoring of Scheduled Print Service"
   ```

2. **Run on Service Start**:
   ```powershell
   $Trigger = New-ScheduledTaskTrigger -AtStartup

   Register-ScheduledTask -TaskName "Monitor Print Service on Startup" `
       -Action $Action -Trigger $Trigger -Settings $Settings
   ```

3. **Run Every Hour**:
   ```powershell
   $Trigger = New-ScheduledTaskTrigger -Once -At (Get-Date) -RepetitionInterval (New-TimeSpan -Hours 1)

   Register-ScheduledTask -TaskName "Hourly Print Service Monitor" `
       -Action $Action -Trigger $Trigger -Settings $Settings
   ```

---

## Monitoring Metrics

### Key Metrics to Watch

#### Service Health
- ✅ Status: Running
- ✅ Uptime: > 1 day
- ⚠️ Memory: < 500 MB (normal), > 1 GB (investigate)
- ⚠️ Errors: 0 in recent logs

#### Performance
- ⚠️ PDF render time: < 10 seconds typical
- ⚠️ API response time: < 5 seconds
- ✅ Success rate: > 95%

#### Disk Space
- ✅ Free space: > 20%
- ⚠️ Warning: < 20% free
- ❌ Critical: < 10% free

#### Logs
- ✅ No errors in last 50 lines
- ⚠️ 1-5 warnings acceptable
- ❌ Multiple errors require investigation

---

## Integration with Monitoring Systems

### Export to CSV for Analysis

```powershell
# Parse monitoring report and export key metrics
$Report = Get-Content "E:\Share\server\servern\Software\ScheduledPrintService\monitoring-report-latest.txt" -Raw

# Extract metrics (customize as needed)
$Metrics = [PSCustomObject]@{
    Timestamp = Get-Date
    ServiceStatus = if ($Report -match "Status: (\w+)") { $Matches[1] } else { "Unknown" }
    MemoryMB = if ($Report -match "Memory: ([\d.]+) MB") { [double]$Matches[1] } else { 0 }
    ErrorCount = if ($Report -match "Errors in last \d+ lines: (\d+)") { [int]$Matches[1] } else { 0 }
    WarningCount = if ($Report -match "Warnings in last \d+ lines: (\d+)") { [int]$Matches[1] } else { 0 }
}

$Metrics | Export-Csv -Path "C:\Monitoring\service-metrics.csv" -Append -NoTypeInformation
```

### Send Email Alert on Errors

```powershell
# Check monitoring report for issues
$Report = Get-Content "E:\Share\server\servern\Software\ScheduledPrintService\monitoring-report-latest.txt" -Raw

if ($Report -match "Service is not running" -or $Report -match "Errors in last \d+ lines: ([5-9]|\d{2,})") {
    # Send email alert
    Send-MailMessage -From "monitoring@company.com" `
        -To "admin@company.com" `
        -Subject "ALERT: Scheduled Print Service Issues Detected" `
        -Body $Report `
        -SmtpServer "smtp.company.com"
}
```

### Webhook to Monitoring Dashboard

```powershell
# Post metrics to monitoring API
$Metrics = @{
    service = "ScheduledPrintService"
    status = "running"
    memory_mb = 450
    errors = 0
    warnings = 2
}

Invoke-RestMethod -Uri "https://monitoring.company.com/api/metrics" `
    -Method Post `
    -Body ($Metrics | ConvertTo-Json) `
    -ContentType "application/json"
```

---

## Advanced Usage

### Compare Historical Reports

```powershell
# Compare today vs yesterday
$Today = Get-Content "monitoring-report-latest.txt"
$Yesterday = Get-ChildItem "monitoring-report-*.txt" |
    Sort-Object LastWriteTime -Descending |
    Select-Object -Skip 1 -First 1 |
    Get-Content

Compare-Object $Yesterday $Today
```

### Monitor Specific Metric Over Time

```powershell
# Track memory usage over time
$Reports = Get-ChildItem "monitoring-report-*.txt" | Sort-Object LastWriteTime

$MemoryHistory = $Reports | ForEach-Object {
    $Content = Get-Content $_.FullName -Raw
    if ($Content -match "Memory: ([\d.]+) MB") {
        [PSCustomObject]@{
            Timestamp = $_.LastWriteTime
            MemoryMB = [double]$Matches[1]
        }
    }
}

$MemoryHistory | Format-Table -AutoSize
```

### Automated Cleanup

```powershell
# Delete reports older than 30 days
$OldReports = Get-ChildItem "monitoring-report-*.txt" |
    Where-Object { $_.LastWriteTime -lt (Get-Date).AddDays(-30) }

$OldReports | Remove-Item -Force
Write-Host "Deleted $($OldReports.Count) old monitoring reports"
```

---

## Related Documentation

- **Main README**: `scheduled-print-service/README.md`
- **Installation Guide**: `scheduled-print-service/INSTALL.md`
- **Project Improvements**: `scheduled-print-service/PROJECT-IMPROVEMENTS.md`
- **Manual API Execution**: `scheduled-print-service/ScheduledPrintService/MANUAL-API-EXECUTION.md`
- **Database Schema**: `scheduled-print-service/ScheduledPrintService/DATABASE-SCHEMA-DIAGRAM.md`

---

## Support

For issues or questions:

1. Check monitoring report for detailed error messages
2. Review logs at `E:\Share\server\servern\Software\ScheduledPrintService\logs`
3. Verify all paths are correct using manual override parameters
4. Check Windows Event Viewer for service-related errors

---

**Last Updated:** November 24, 2025
**Version:** 1.0
**Script Location:** `scheduled-print-service/monitor-service.ps1`
