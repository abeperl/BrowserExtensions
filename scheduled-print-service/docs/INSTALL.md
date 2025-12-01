# Scheduled Print Service - Server Installation Guide

This guide provides step-by-step instructions for installing and configuring the Scheduled Print Service on a Windows Server.

## Prerequisites

### Required Software
- **Windows Server 2019 or later** (or Windows 10/11)
- **.NET 8 Runtime** (or SDK for building from source)
  - Download from: https://dotnet.microsoft.com/download/dotnet/8.0
  - For servers, install the **ASP.NET Core Runtime** (includes .NET Runtime)
- **Administrator privileges** for service installation

### System Requirements
- ~150MB disk space for Chromium browser cache
- Additional space for PDF output files (depends on usage)
- Network access if printing from remote URLs
- PDF-capable printer (if using Windows printer mode)

## Installation Steps

### Step 1: Obtain the Application Files

#### Option A: Build from Source (Development Machine)

On your development machine with .NET 8 SDK installed:

```powershell
# Navigate to the project directory
cd C:\path\to\BrowserExtensions\scheduled-print-service

# Publish the application (self-contained includes .NET runtime)
.\publish.ps1 -Configuration Release -Runtime win-x64 -SelfContained

# Published files will be in: publish/Release/win-x64/
```

**Copy the entire `publish/Release/win-x64/` folder to your server.**

#### Option B: Use Pre-Built Binaries

If you have pre-built binaries, copy them to your server at:
```
C:\Program Files\ScheduledPrintService\
```

### Step 2: Configure the Application

1. **Navigate to the installation directory:**
   ```powershell
   cd "C:\Program Files\ScheduledPrintService"
   ```

2. **Edit `appsettings.json`** to configure the service:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  },
  "Pdf": {
    "ChromiumDownloadMode": "Auto",
    "CacheDirectory": "chromium-cache",
    "Landscape": false,
    "PrintBackground": true
  },
  "Printer": {
    "Mode": "File",
    "OutputDirectory": "out",
    "PrinterName": "",
    "FallbackPrinterName": ""
  },
  "Scheduler": {
    "Enabled": true,
    "IntervalSeconds": 300,
    "Urls": [
      "https://example.com/print-job-1",
      "https://example.com/print-job-2"
    ],
    "PrintedStorePath": "printed-urls.txt"
  },
  "Email": {
    "Enabled": false,
    "SmtpHost": "smtp.example.com",
    "SmtpPort": 587,
    "UseSsl": true,
    "Username": "",
    "Password": "",
    "From": "scheduled-print@example.com",
    "To": "admin@example.com"
  },
  "Demo": {
    "Enabled": false,
    "Url": "https://example.com",
    "OutputFilePrefix": "demo"
  }
}
```

**Key Configuration Settings:**

- **Printer.Mode**:
  - `"File"` - Saves PDFs to OutputDirectory (recommended for testing)
  - `"Windows"` - Sends to Windows printer (requires PDF-capable printer)

- **Scheduler.Enabled**: `true` to enable automatic periodic printing

- **Scheduler.IntervalSeconds**: Polling interval (e.g., 300 = 5 minutes)

- **Scheduler.Urls**: Array of URLs to fetch and print

- **Email**: Configure SMTP for failure notifications (optional)

### Step 3: Set Data Directory (Optional)

By default, the service stores files in `%ProgramData%\ScheduledPrintService`:
- PDFs: `out/`
- Logs: `logs/`
- Chromium cache: `chromium-cache/`
- Tracking: `printed-urls.txt`

To use a custom location, set an environment variable:

```powershell
# Set system-wide environment variable (requires Administrator)
[Environment]::SetEnvironmentVariable(
  "SCHEDULED_PRINT_DATA_ROOT",
  "C:\ScheduledPrintData",
  "Machine"
)
```

### Step 4: Install the Windows Service

Run PowerShell **as Administrator**:

```powershell
# Navigate to installation directory
cd "C:\Program Files\ScheduledPrintService"

# Install the service
.\install-service.ps1 -ExePath "C:\Program Files\ScheduledPrintService\ScheduledPrintService.exe"
```

**Advanced Installation Options:**

```powershell
# Custom service name and startup type
.\install-service.ps1 `
  -ExePath "C:\Program Files\ScheduledPrintService\ScheduledPrintService.exe" `
  -Name "ScheduledPrintService" `
  -DisplayName "Scheduled Print Service" `
  -Description "Renders HTML to PDF and prints via configured output mode." `
  -StartupType "AutomaticDelayedStart"
```

**Startup Types:**
- `AutomaticDelayedStart` - Starts automatically after boot (delayed, recommended)
- `Automatic` - Starts automatically at boot
- `Manual` - Must be started manually
- `Disabled` - Cannot be started

### Step 5: Verify Service Installation

```powershell
# Check service status
Get-Service -Name ScheduledPrintService

# View service details
Get-Service -Name ScheduledPrintService | Format-List *

# Check if service is running
if ((Get-Service -Name ScheduledPrintService).Status -eq 'Running') {
    Write-Host "Service is running successfully!" -ForegroundColor Green
} else {
    Write-Host "Service is not running. Check logs." -ForegroundColor Red
}
```

### Step 6: Check Logs and Output

**View Logs:**
```powershell
# Navigate to logs directory
cd "$env:ProgramData\ScheduledPrintService\logs"

# View latest log file
Get-Content -Path (Get-ChildItem -Filter "*.log" | Sort-Object LastWriteTime -Descending | Select-Object -First 1).FullName -Tail 50
```

**Check PDF Output:**
```powershell
# Navigate to output directory
cd "$env:ProgramData\ScheduledPrintService\out"

# List generated PDFs
Get-ChildItem -Filter "*.pdf" | Sort-Object LastWriteTime -Descending | Select-Object Name, LastWriteTime, Length
```

## Service Management

### Start Service
```powershell
Start-Service -Name ScheduledPrintService
```

### Stop Service
```powershell
Stop-Service -Name ScheduledPrintService
```

### Restart Service
```powershell
Restart-Service -Name ScheduledPrintService
```

### Check Service Status
```powershell
Get-Service -Name ScheduledPrintService | Select-Object Status, StartType, Name, DisplayName
```

### View Service Event Logs
```powershell
# View Application event log entries
Get-EventLog -LogName Application -Source ScheduledPrintService -Newest 20
```

## Configuration Updates

After modifying `appsettings.json`, restart the service:

```powershell
Restart-Service -Name ScheduledPrintService
```

## Troubleshooting

### Service Won't Start

1. **Check logs:**
   ```powershell
   Get-Content "$env:ProgramData\ScheduledPrintService\logs\*.log" -Tail 100
   ```

2. **Verify .NET Runtime:**
   ```powershell
   dotnet --list-runtimes
   # Should show: Microsoft.NETCore.App 8.0.x
   ```

3. **Check file permissions:**
   - Service account needs read/write access to:
     - Installation directory
     - `%ProgramData%\ScheduledPrintService\`

4. **Validate configuration:**
   - Ensure `appsettings.json` is valid JSON
   - Check paths in configuration exist or are writable

### Chromium Download Issues

If Chromium fails to download:

```json
{
  "Pdf": {
    "ChromiumDownloadMode": "Manual",
    "CacheDirectory": "C:\\path\\to\\chromium-cache"
  }
}
```

Then manually download Chromium for PuppeteerSharp and place in the cache directory.

### PDFs Not Generated

1. **Check Printer.Mode** is set correctly
2. **Verify OutputDirectory** exists and is writable
3. **Check logs** for rendering errors
4. **Test URLs** are accessible from the server

### Service Crashes or Stops

1. **Check Windows Event Viewer:**
   - Application logs
   - System logs

2. **Enable verbose logging:**
   ```json
   {
     "Logging": {
       "LogLevel": {
         "Default": "Debug"
       }
     }
   }
   ```

3. **Test in console mode** (not as service):
   ```powershell
   cd "C:\Program Files\ScheduledPrintService"
   .\ScheduledPrintService.exe
   # Press Ctrl+C to stop
   ```

## Uninstallation

Run PowerShell **as Administrator**:

```powershell
# Stop and remove the service
cd "C:\Program Files\ScheduledPrintService"
.\uninstall-service.ps1

# Remove application files
Remove-Item -Path "C:\Program Files\ScheduledPrintService" -Recurse -Force

# Remove data files (optional)
Remove-Item -Path "$env:ProgramData\ScheduledPrintService" -Recurse -Force

# Remove environment variable (if set)
[Environment]::SetEnvironmentVariable("SCHEDULED_PRINT_DATA_ROOT", $null, "Machine")
```

## Production Deployment Checklist

- [ ] .NET 8 Runtime installed on server
- [ ] Application files copied to `C:\Program Files\ScheduledPrintService\`
- [ ] `appsettings.json` configured with production settings
- [ ] Data directory permissions verified
- [ ] Service installed with appropriate startup type
- [ ] Service started and running
- [ ] Logs checked for errors
- [ ] Test PDF generated successfully
- [ ] Email notifications configured (if needed)
- [ ] Monitoring/alerting configured
- [ ] Documentation updated with server-specific details

## Security Considerations

1. **Service Account**: By default, runs as LocalSystem. Consider using a dedicated service account with minimal permissions.

2. **File Permissions**: Restrict access to:
   - Installation directory
   - Data directory
   - Configuration files (contain sensitive data)

3. **Network Security**: If printing from remote URLs, ensure firewall rules allow outbound HTTPS.

4. **Credential Storage**: Store SMTP passwords securely (consider Azure Key Vault, Windows Credential Manager, etc.).

## Support

For issues or questions:
- Check logs in `%ProgramData%\ScheduledPrintService\logs\`
- Review configuration in `appsettings.json`
- Test in console mode for detailed debugging
- Refer to README.md for feature documentation
