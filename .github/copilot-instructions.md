# GitHub Copilot Instructions for BrowserExtensions

## Repository Overview

Multi-project repository containing browser extensions, mobile apps, and Windows services for warehouse management automation. Active PR: **net9-upgrade** (downgrading from .NET 10 to .NET 9).

## Project Structure

### 1. **scheduled-print-service** (ASP.NET Core Windows Service, .NET 8) 🔥
- **Location**: `scheduled-print-service/ScheduledPrintService/`
- **Purpose**: Headless browser automation for printing SPA pages (PuppeteerSharp) + API polling with configurable action chains
- **Architecture**: Hosted services pattern with dependency injection
- **Key Services**: 
  - `ApiPollSchedulerService`: Polls API, extracts IDs, executes sub-action chains
  - `SubActionExecutor`: Executes configurable actions (CallApi, GetUrlAndPrint, NavigateOnly, PrintCapturedPage, CreatePicklistBatch, Delay)
  - `TokenRenewalService`: Singleton for automatic auth token renewal on 401
  - `PdfBrowserManager`: Manages PuppeteerSharp browser lifecycle with auth injection
- **Critical Pattern**: **Two-stage print workflow** - `NavigateOnly` navigates & waits for SPA data load, then `PrintCapturedPage` prints the kept-alive page (avoids re-rendering losing XHR state)
- **Configuration**: `appsettings.json` with complex `Api.SubActions[]` array supporting action chaining via `ChainedArrayJsonPath`/`UseChainedInput`
- **Data Paths**: Uses `DataPaths.DataRoot` (defaults to `%ProgramData%\ScheduledPrintService`, overrideable via `SCHEDULED_PRINT_DATA_ROOT` env var)

**Critical Concepts**:
```csharp
// Sub-action chaining: CreatePicklistBatch extracts IDs, then GetUrlAndPrint uses {pickListId} tokens
{
  "Type": "CreatePicklistBatch",
  "ChainedArrayJsonPath": "data",  // Extract array from response
  "ChainedItemFieldPath": "pickListId"  // Field to pass to chained actions
},
{
  "Type": "GetUrlAndPrint",
  "UseChainedInput": true,  // Receives context from previous action
  "Endpoint": "https://example.com/#page?id={pickListId}",  // Token replaced
  "WaitForNetworkIdleMs": 3000,
  "MakeHiddenVisible": true  // Converts hidden inputs to visible before PDF
}
```

**Dev Workflow**:
```powershell
# Set local data root (keeps artifacts in repo during dev)
$env:SCHEDULED_PRINT_DATA_ROOT = (Resolve-Path ".\scheduled-print-service").Path

# Run in console mode
dotnet run --project .\scheduled-print-service\ScheduledPrintService\ScheduledPrintService.csproj

# Install as Windows service (Administrator)
cd scheduled-print-service
.\install-service.ps1 -ExePath "C:\path\to\ScheduledPrintService.exe"

# View logs
Get-Content "$env:ProgramData\ScheduledPrintService\logs\scheduled-print-service-$(Get-Date -Format yyyyMMdd).log" -Tail 50 -Wait
```

**Key Documentation**: `CHAINING-GUIDE.md`, `API-POLLING-GUIDE.md`, `TOKEN-RENEWAL.md`, `MANUAL-MODE-USAGE.md`

### 2. **DataFlow-Mobile** (.NET MAUI, .NET 9)
- **Location**: `DataFlow-Mobile/`
- **Architecture**: MVVM with Repository pattern, Entity Framework Core + SQLite
- **DI Registration**: All services in `MauiProgram.cs` - follow existing patterns (interfaces in `Services/Interfaces/`)
- **HTTP Resilience**: Uses `AddStandardResilienceHandler()` (no Polly, no manual timeouts)
- **Key Pattern**: Generic repository with UnitOfWork (`IGenericRepository<T>`, `IUnitOfWork`)
- **Testing**: xUnit tests in `DataFlow.Mobile.Tests/`

**Critical Build Commands**:
```powershell
# Build for Android (common target)
dotnet build DataFlow-Mobile/DataFlow.Mobile/DataFlow.Mobile.csproj -t:Run -f net9.0-android

# Restore
dotnet restore DataFlow-Mobile/DataFlow.Mobile/DataFlow.Mobile.csproj
```

### 3. **html-printer-service** (ASP.NET Core Windows Service, .NET 8)
- **Location**: `html-printer-service/HTMLZebraPrinterService/`
- **Purpose**: HTML → ZPL conversion + direct Zebra printer TCP/IP printing
- **Architecture**: Dependency injection, hosted service pattern (`PrintQueueService`)
- **Configuration**: `appsettings.json` (Printer IP, Label dimensions, Retry logic, Sentry DSN)
- **Key Files**: `Program.cs`, `Services/` (HtmlParser, ZplGenerator, PrinterCommunicator)

**Service Management**:
```powershell
# Install (as Administrator)
cd html-printer-service
.\install.ps1

# Logs location
Get-Content "HTMLZebraPrinterService\bin\Debug\net8.0\logs\service-*.txt" -Tail 50
```

### 4. **word-template-extension** (Browser Extension + Python Native Host)
- **Extension**: Manifest v3 (Chrome/Edge), extracts web data into Word templates
- **Native Host**: Python 3.7+ using `python-docx`, communicates via native messaging
- **Build Script**: `build-packages.ps1` - creates store packages, optionally builds PyInstaller executable
- **Template Syntax**: `{{PLACEHOLDER_NAME}}` in .docx files

**Development Workflow**:
```powershell
# Build distribution packages
cd word-template-extension
powershell -ExecutionPolicy Bypass -File build-packages.ps1 -Version "1.0.1"

# Install native host (Windows, as Administrator)
cd native-host
.\install.bat
```

### 5. **scan-overlay-extension** (Browser Extension)
- **Purpose**: Scanning workflows with overlays, audio feedback, configurable selectors
- **Key Files**: `background.js`, `content.js`, `overlay.js`, `settings.js`
- **Configuration**: XPath/CSS selectors via settings page
- **Audio**: MP3 files generated using `generate-audio.html`

### 6. **css-override-extension** (Browser Extension)
- **Purpose**: Inject custom CSS by URL pattern (Override/Replace modes)
- **Pattern**: Manifest v3, simple rule management UI

### 7. **css-js-toinject/** (Injectable Scripts)
- **Purpose**: Site-specific enhancements for internal 3PL website (hash-based SPA routing)
- **Router**: `router.js` (main) and `malchus-router.js` (domain-specific) detect URL hash fragments, load feature-specific modules
- **Pattern**: Each route registers actions that inject CSS/JS when URL hash matches
- **Key Features**:
  - `auto-print-buttons.js`: Sequential printing (Carton Label → Packing Slip)
  - `status-dropdown.js`: Auto-fill status scan fields
  - `table-item-linker.js`: SKU/Qty clickable items
  - `placard-text-enhancer.css`: Doubles text size on shipping placards
  - `item-line-id.js`: Adds Item Line ID column to tables via MutationObserver

## Development Conventions

### PowerShell Scripts
- **Execution Policy**: Always use `-ExecutionPolicy Bypass` for scripts
- **Admin Rights**: Install scripts (`install.bat`, `install.ps1`) require Administrator elevation
- **Build Scripts**: Located at project roots (e.g., `build-packages.ps1`)
- **Environment Variables**: Use `$env:VARIABLE_NAME` to persist settings across commands (e.g., `$env:SCHEDULED_PRINT_DATA_ROOT`)

### .NET Projects
- **Current Migration**: Downgrading DataFlow-Mobile from .NET 10 → .NET 9 (see `Apply-DataFlowNet9Upgrade.ps1`)
- **HTTP Clients**: Use `AddStandardResilienceHandler()` - DO NOT use Polly directly or set `client.Timeout`
- **Service Registration**: Group by type in `MauiProgram.cs` (Repository → Data → API → ViewModels → Pages)
- **File-Scoped Namespaces**: Use `namespace DataFlow.Mobile.Services;` (no braces)
- **Singletons for State**: `TokenRenewalService` is singleton - use for shared auth state across services
- **Hosted Services**: Use `BackgroundService` base class for long-running services (ApiPollSchedulerService, PrintSchedulerService)

### SPA Navigation Pattern (scheduled-print-service)
**Critical for hash-based SPAs**: Pages load via JavaScript after hash changes, requiring multi-stage waits:
1. Navigate to root URL to establish domain context
2. Inject auth token into localStorage/sessionStorage IMMEDIATELY after domain load
3. Set hash via `window.location.href` (triggers SPA routing)
4. Wait 1500ms for hash detection + initial render
5. Wait `WaitForNetworkIdleMs` for XHR data loading
6. Optionally wait for `WaitForSelector` and `AdditionalWaitSelectors`
7. Poll `DataReadyRowSelector` count until `MinimumDataRows` met (max `DataLoadRetryMs`)
8. Optional `PostSelectorStableMs` stabilization delay

**Do NOT pass CancellationToken to critical waits** - prevents interruption during route detection.

### Browser Extensions
- **Manifest Version**: All extensions use Manifest v3
- **Content Scripts**: Injected via `manifest.json` or programmatic injection
- **Native Messaging**: Word extension uses JSON protocol with Python host

### Testing
- **MAUI Tests**: xUnit in `DataFlow.Mobile.Tests/`
- **Manual Testing**: Browser extensions load unpacked during development
- **Service Tests**: PowerShell diagnostic scripts (e.g., `diagnose.ps1`, `test-https.ps1`)
- **Service Console Mode**: Run scheduled-print-service via `dotnet run` (not as Windows service) for debugging

## Critical Workflows

### Run Scheduled Print Service (Development)
```powershell
# Set local data root to keep artifacts in repo
$env:SCHEDULED_PRINT_DATA_ROOT = (Resolve-Path ".\scheduled-print-service").Path

# Run in console mode (not as service)
dotnet run --project .\scheduled-print-service\ScheduledPrintService\ScheduledPrintService.csproj

# Watch logs in real-time
Get-Content "$env:SCHEDULED_PRINT_DATA_ROOT\logs\scheduled-print-service-$(Get-Date -Format yyyyMMdd).log" -Tail 50 -Wait
```

### Install Scheduled Print Service (Production)
```powershell
# Build and publish
cd scheduled-print-service
.\publish.ps1 -Configuration Release -Runtime win-x64 -SelfContained

# Install as Windows service (as Administrator)
.\install-service.ps1 -ExePath "C:\path\to\publish\ScheduledPrintService.exe"

# Manage service
Start-Service ScheduledPrintService
Stop-Service ScheduledPrintService
Restart-Service ScheduledPrintService

# View production logs
Get-Content "$env:ProgramData\ScheduledPrintService\logs\scheduled-print-service-$(Get-Date -Format yyyyMMdd).log" -Tail 100
```

### Manual Mode (scheduled-print-service)
Run once and exit (for debugging sub-actions):
```powershell
# Edit appsettings.json: "ManualMode": true
dotnet run --project .\scheduled-print-service\ScheduledPrintService\ScheduledPrintService.csproj

# Or with command-line args (future)
dotnet run --project .\scheduled-print-service\ScheduledPrintService\ScheduledPrintService.csproj -- --manual --api-number 1
```

### Build DataFlow-Mobile for Android
```powershell
dotnet build DataFlow-Mobile/DataFlow.Mobile/DataFlow.Mobile.csproj -f net9.0-android
```

### Install HTML Printer Service
```powershell
# As Administrator
cd html-printer-service
.\install.ps1
Restart-Service HTMLZebraPrinterService
```

### Package Word Template Extension
```powershell
cd word-template-extension
powershell -ExecutionPolicy Bypass -File build-packages.ps1 -Version "1.0.2" -IncludeSource
# Output: dist/word-template-extension-chrome-v1.0.2.zip
```

### Inject CSS-JS Scripts (Manual)
Load `css-js-toinject/router.js` or `malchus-router.js` via browser console or extension to enable feature routing.

## Common Patterns

### Adding a Service (DataFlow-Mobile)
1. Create interface in `Services/Interfaces/IMyService.cs`
2. Implement in `Services/MyService.cs`
3. Register in `MauiProgram.RegisterServices()`:
   ```csharp
   services.AddScoped<IMyService, MyService>();
   ```

### Adding a Sub-Action (scheduled-print-service)
Edit `appsettings.json` in `Api.SubActions` array:
```json
{
  "Type": "GetUrlAndPrint",
  "Name": "Print Order Details",
  "Enabled": true,
  "Endpoint": "https://example.com/#order/{id}",
  "UseChainedInput": false,
  "WaitForNetworkIdleMs": 3000,
  "MakeHiddenVisible": true,
  "ContinueOnError": true
}
```

**Available SubAction Types**:
- `CallApi`: Make HTTP request
- `GetHtmlAndPrint`: Fetch HTML from API and print
- `GetUrlAndPrint`: Navigate Puppeteer browser to URL and print
- `NavigateOnly`: Navigate and keep page alive (two-stage print)
- `PrintCapturedPage`: Print previously captured page
- `CreatePicklistBatch`: Batch create picklists and trigger chained actions
- `Delay`: Wait specified milliseconds

### Adding a Route (css-js-toinject)
Edit `router.js` or `malchus-router.js`, add to `ROUTES` array:
```javascript
{
    name: 'My Feature Route',
    pattern: /^#path\/to\/page$/i,
    action: () => {
        console.log('Matched route');
        // Inject scripts/styles dynamically
        // Use MutationObserver for DOM watching
    }
}
```

### Configuring API Polling (scheduled-print-service)
Edit `appsettings.json`:
```json
{
  "Api": {
    "Enabled": true,
    "BaseUrl": "https://example.com",
    "BearerToken": "your-token",
    "WarehouseId": 1,
    "UserEmail": "user@example.com",
    "Password": "password",
    "Cookies": {},
    "PrimaryEndpoint": "/api/orders/list",
    "PrimaryHttpMethod": "POST",
    "IdJsonPath": "[0]",
    "ProcessedIdsPath": "processed-orders.txt",
    "SubActions": []
  },
  "Scheduler": {
    "Enabled": true,
    "IntervalSeconds": 300
  }
}
```

### Configuring Printer Service
Edit `HTMLZebraPrinterService/appsettings.json`:
```json
{
  "Printer": { "IpAddress": "192.168.1.244", "Port": 9100 },
  "Label": { "WidthInches": 4.0, "HeightInches": 6.0, "DPI": 300 }
}
```

## Troubleshooting

### .NET MAUI Build Failures
- Verify target framework: `net9.0-android` (not `net10.0-android`)
- Check Android SDK licenses: `AcceptAndroidSDKLicenses=true` in `.csproj`
- Clean: `dotnet clean && dotnet build`

### Scheduled Print Service Issues
- **Service won't start**: Check Event Viewer → Application logs for errors
- **No PDFs generated**: Verify `Printer.Mode` is `File` and `OutputDirectory` exists
- **SPA data missing**: Increase `WaitForNetworkIdleMs` (try 5000-10000ms)
- **Token expired (401)**: Service auto-renews if `UserEmail`/`Password` configured
- **Chained actions not running**: Verify `UseChainedInput: true` and action order

**Check logs**:
```powershell
# Development (local data root)
Get-Content "scheduled-print-service\logs\scheduled-print-service-$(Get-Date -Format yyyyMMdd).log" -Tail 100

# Production
Get-Content "$env:ProgramData\ScheduledPrintService\logs\scheduled-print-service-$(Get-Date -Format yyyyMMdd).log" -Tail 100
```

### Word Extension Native Host Issues
- Run `native-host/diagnose.ps1` to check Python, packages, registry
- Verify manifest path in registry: `HKEY_CURRENT_USER\Software\Google\Chrome\NativeMessagingHosts\com.wordtemplate.nativehost`

### HTML Printer Service Not Starting
- Check Event Viewer → Windows Logs → Application
- Verify .NET 8 runtime: `dotnet --version`
- Test printer connectivity: `Test-NetConnection -ComputerName 192.168.1.244 -Port 9100`

## File Locations

- **Solution File**: `BrowserExtensions.sln` (DataFlow-Mobile only)
- **Root Instructions**: `CLAUDE.md` (detailed project documentation)
- **Task Files**: VSCode tasks in `.vscode/tasks.json` (e.g., "Zip Extension Files")

## Notes for AI Agents

- **Minimal Changes**: Preserve existing patterns unless explicitly refactoring (per `CLAUDE.md`)
- **PowerShell Default**: Use PowerShell syntax for commands (Windows environment)
- **No Generic Advice**: Follow project-specific conventions above
- **Active PR Context**: Currently migrating .NET 10 → .NET 9 (see `Apply-DataFlowNet9Upgrade.ps1`)
