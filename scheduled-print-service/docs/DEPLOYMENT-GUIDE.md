# Scheduled Print Service - Deployment Guide

## Architecture Overview

- **Service executable location**: `C:\Program Files\Malchut\ScheduledPrintService\ScheduledPrintService.exe`
- **Data/logs location**: `E:\Share\server\servern\Software\ScheduledPrintService` (configured in `appsettings.json`)

The separation allows the executable to live in Program Files while all data, logs, and output files are stored on the network share.

## Configuration

Edit `appsettings.json` in the executable directory (`C:\Program Files\Malchut\ScheduledPrintService\appsettings.json`):

```json
{
  "DataRoot": "E:\\Share\\server\\servern\\Software\\ScheduledPrintService"
}
```

### Configuration Priority

1. **Environment variable** `SCHEDULED_PRINT_DATA_ROOT` (highest priority - for dev overrides)
2. **appsettings.json** `DataRoot` setting (recommended for production)
3. **Fallback** to `%ProgramData%\ScheduledPrintService` (if neither above is set)

## Deployment Steps

### 1. Build and Publish (from repository)

```powershell
cd C:\Users\User\source\repos\BrowserExtensions\scheduled-print-service\ScheduledPrintService
dotnet publish -c Release -o publish
```

### 2. Deploy to Server

**Option A: Using deployment script (recommended)**

```powershell
# Run as Administrator
.\deploy-to-server.ps1
```

**Option B: Manual deployment**

```powershell
# Stop service
Stop-Service -Name "ScheduledPrintService"

# Copy files
Copy-Item -Path ".\publish\*" -Destination "C:\Program Files\Malchut\ScheduledPrintService" -Recurse -Force

# Start service
Start-Service -Name "ScheduledPrintService"
```

### 3. Verify Configuration

Check that `appsettings.json` on the server has the correct `DataRoot`:

```powershell
Get-Content "C:\Program Files\Malchut\ScheduledPrintService\appsettings.json" | Select-String -Pattern "DataRoot"
```

## Running the Service

### As Windows Service (Production)

```powershell
# Check status
Get-Service -Name "ScheduledPrintService"

# Start/Stop/Restart
Start-Service -Name "ScheduledPrintService"
Stop-Service -Name "ScheduledPrintService"
Restart-Service -Name "ScheduledPrintService"

# View Event Viewer logs
Get-EventLog -LogName Application -Source "ScheduledPrintService" -Newest 20
```

### Console Mode (Testing/Debugging)

```powershell
# Navigate to executable directory
cd "C:\Program Files\Malchut\ScheduledPrintService"

# Run directly (will use DataRoot from appsettings.json)
.\ScheduledPrintService.exe

# The service will run in console mode and you'll see output immediately
# Press Ctrl+C to stop
```

### Manual Mode (Run Once)

Edit `appsettings.json` temporarily or use command-line args (future feature):

```json
{
  "Api": {
    "ManualMode": true
  }
}
```

Then run in console mode as above.

## Viewing Logs

### File Logs

```powershell
# View today's log
$logPath = "E:\Share\server\servern\Software\ScheduledPrintService\logs\scheduled-print-service-$(Get-Date -Format yyyyMMdd).log"
Get-Content $logPath -Tail 50

# Tail logs (follow in real-time)
Get-Content $logPath -Tail 50 -Wait

# List all log files
Get-ChildItem "E:\Share\server\servern\Software\ScheduledPrintService\logs" -Filter "*.log"
```

### Event Viewer Logs

```powershell
Get-EventLog -LogName Application -Source "ScheduledPrintService" -Newest 20
```

## Checking Output Files

```powershell
# List generated PDFs
Get-ChildItem "E:\Share\server\servern\Software\ScheduledPrintService\out" -Filter "*.pdf"

# List processed IDs
Get-Content "E:\Share\server\servern\Software\ScheduledPrintService\processed-orders.txt"
```

## Troubleshooting

### Service won't start

1. Check Event Viewer:
   ```powershell
   Get-EventLog -LogName Application -Source "ScheduledPrintService" -Newest 5
   ```

2. Try console mode to see immediate errors:
   ```powershell
   cd "C:\Program Files\Malchut\ScheduledPrintService"
   .\ScheduledPrintService.exe
   ```

### No logs appearing

1. Verify `DataRoot` configuration:
   ```powershell
   Get-Content "C:\Program Files\Malchut\ScheduledPrintService\appsettings.json" | Select-String -Pattern "DataRoot"
   ```

2. Check permissions on data directory:
   ```powershell
   Test-Path "E:\Share\server\servern\Software\ScheduledPrintService\logs"
   icacls "E:\Share\server\servern\Software\ScheduledPrintService"
   ```

3. Run in console mode - logs will appear in console and file

### Service account permissions

If running as NETWORK SERVICE or specific user account, ensure the account has:
- **Read** access to `C:\Program Files\Malchut\ScheduledPrintService`
- **Write** access to `E:\Share\server\servern\Software\ScheduledPrintService` and all subdirectories

## File Structure

```
C:\Program Files\Malchut\ScheduledPrintService\     (Executable location)
├── ScheduledPrintService.exe
├── appsettings.json                                (Contains DataRoot setting)
├── *.dll
└── runtimes\

E:\Share\server\servern\Software\ScheduledPrintService\  (Data location)
├── logs\
│   └── scheduled-print-service-20251124.log
├── out\                                            (PDF output directory)
├── chromium-cache\                                 (Browser cache)
├── processed-orders.txt                            (Processed IDs)
└── ScheduledPrintService.db                        (API configs database)
```
