# GitHub Copilot Instructions for BrowserExtensions

## Repository Overview

Multi-project repository containing browser extensions, mobile apps, and Windows services. Active PR: **net9-upgrade** (downgrading from .NET 10 to .NET 9).

## Project Structure

### 1. **DataFlow-Mobile** (.NET MAUI, .NET 9)
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

### 2. **html-printer-service** (ASP.NET Core Windows Service, .NET 8)
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

### 3. **word-template-extension** (Browser Extension + Python Native Host)
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

### 4. **scan-overlay-extension** (Browser Extension)
- **Purpose**: Scanning workflows with overlays, audio feedback, configurable selectors
- **Key Files**: `background.js`, `content.js`, `overlay.js`, `settings.js`
- **Configuration**: XPath/CSS selectors via settings page
- **Audio**: MP3 files generated using `generate-audio.html`

### 5. **css-override-extension** (Browser Extension)
- **Purpose**: Inject custom CSS by URL pattern (Override/Replace modes)
- **Pattern**: Manifest v3, simple rule management UI

### 6. **css-js-toinject/** (Injectable Scripts)
- **Purpose**: Site-specific enhancements for internal 3PL website
- **Router**: `router.js` detects URL hash fragments, loads feature-specific modules
- **Key Features**:
  - `auto-print-buttons.js`: Sequential printing (Carton Label → Packing Slip)
  - `status-dropdown.js`: Auto-fill status scan fields
  - `table-item-linker.js`: SKU/Qty clickable items
  - `placard-text-enhancer.css`: Doubles text size on shipping placards

## Development Conventions

### PowerShell Scripts
- **Execution Policy**: Always use `-ExecutionPolicy Bypass` for scripts
- **Admin Rights**: Install scripts (`install.bat`, `install.ps1`) require Administrator elevation
- **Build Scripts**: Located at project roots (e.g., `build-packages.ps1`)

### .NET Projects
- **Current Migration**: Downgrading DataFlow-Mobile from .NET 10 → .NET 9 (see `Apply-DataFlowNet9Upgrade.ps1`)
- **HTTP Clients**: Use `AddStandardResilienceHandler()` - DO NOT use Polly directly or set `client.Timeout`
- **Service Registration**: Group by type in `MauiProgram.cs` (Repository → Data → API → ViewModels → Pages)
- **File-Scoped Namespaces**: Use `namespace DataFlow.Mobile.Services;` (no braces)

### Browser Extensions
- **Manifest Version**: All extensions use Manifest v3
- **Content Scripts**: Injected via `manifest.json` or programmatic injection
- **Native Messaging**: Word extension uses JSON protocol with Python host

### Testing
- **MAUI Tests**: xUnit in `DataFlow.Mobile.Tests/`
- **Manual Testing**: Browser extensions load unpacked during development
- **Service Tests**: PowerShell diagnostic scripts (e.g., `diagnose.ps1`, `test-https.ps1`)

## Critical Workflows

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
Load `css-js-toinject/router.js` via browser console or extension to enable feature routing.

## Common Patterns

### Adding a Service (DataFlow-Mobile)
1. Create interface in `Services/Interfaces/IMyService.cs`
2. Implement in `Services/MyService.cs`
3. Register in `MauiProgram.RegisterServices()`:
   ```csharp
   services.AddScoped<IMyService, MyService>();
   ```

### Adding a Route (css-js-toinject)
Edit `router.js`, add to `ROUTES` array:
```javascript
{
    name: 'My Feature Route',
    pattern: /^#path\/to\/page$/i,
    action: () => {
        console.log('Matched route');
        // Inject scripts/styles
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
