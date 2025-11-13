# Scheduled Print Service (Prototype)

Prototype Windows service for unattended HTML → PDF rendering and output. Uses PuppeteerSharp to render either a URL or raw HTML, then:
- File mode: writes PDFs to an output directory (default)
- Windows mode: spools raw PDF bytes to a named Windows printer (requires PDF-capable printer)

Includes a simple scheduler that fetches HTML from configured URLs periodically, avoids duplicates, and can email on failures.

## Features
* Headless Chromium auto-download (cache configurable)
* Render URL or HTML to PDF (background + landscape configurable)
* File-based PDF output for easy validation
* Single-run demo mode (prints configured Demo.Url then exits)
* Serilog logging to console + rolling file

## Configuration (`appsettings.json`)
```jsonc
{
  "Pdf": { "ChromiumDownloadMode": "Auto", "CacheDirectory": "chromium-cache" },
  "Printer": { "Mode": "File", "OutputDirectory": "out" },
  "Demo": { "Enabled": true, "Url": "https://example.com", "OutputFilePrefix": "demo" }
}
```

## Run (Development)
```powershell
dotnet restore .\scheduled-print-service\ScheduledPrintService\ScheduledPrintService.csproj
dotnet run --project .\scheduled-print-service\ScheduledPrintService\ScheduledPrintService.csproj
```

By default, artifacts (out/logs/chromium-cache/printed-urls.txt) are placed under `%ProgramData%/ScheduledPrintService`.

To keep development artifacts inside this repo under `scheduled-print-service/`, set an environment override before running:

```powershell
# for current session
$env:SCHEDULED_PRINT_DATA_ROOT = (Resolve-Path ".\scheduled-print-service").Path
"Data root set to $env:SCHEDULED_PRINT_DATA_ROOT"

# reset to default ProgramData behavior
Remove-Item Env:\SCHEDULED_PRINT_DATA_ROOT -ErrorAction SilentlyContinue
```

PDFs appear under `<DataRoot>/out`, logs under `<DataRoot>/logs`, Chromium cache under `<DataRoot>/<CacheDirectory>`.

## Configuration
Settings live in `ScheduledPrintService\appsettings.json` and are copied to the output folder at build.

Key sections:
- Pdf: Chromium download/cache and PDF page options.
- Printer:
  - `Mode`: `File` or `Windows`
  - `OutputDirectory`: used in File mode
  - `PrinterName`/`FallbackPrinterName`: used in Windows mode
- Scheduler:
  - `Enabled`: set true to run periodic jobs
  - `IntervalSeconds`: poll interval
  - `Urls`: array of URLs to fetch and print
  - `PrintedStorePath`: file storing printed keys to avoid duplicates
- Email: SMTP settings; send once per cycle on failures when `Enabled=true`.

Example toggles:
```jsonc
{
  "Printer": { "Mode": "File", "OutputDirectory": "out" },
  "Scheduler": { "Enabled": true, "IntervalSeconds": 120, "Urls": ["https://example.com"] },
  "Demo": { "Enabled": false }
}
```

## Windows Printing (optional)
Set `Printer:Mode` to `Windows` and specify `PrinterName`. This sends raw PDF bytes to the Windows spooler. Your printer must natively support PDF (many modern network printers do). If not, keep `Mode=File` or introduce a PDF-to-XPS/PS converter.

## Planned Next Steps
1. API polling & ID tracking (SQLite) for production job queue.
2. Real printer integration (Windows spooler / IP-based).
3. Failure notification (SMTP / webhook).
4. Health endpoint and service installer script.

## Notes
* This prototype intentionally avoids solution file modification for minimal impact.
* Ensure adequate disk space for Chromium download (~150MB).
* If config changes don't seem to take effect, confirm `appsettings.json` is present in the output folder and that logs show: `Config Flags => Demo.Enabled=... Scheduler.Enabled=...`.
* For dev cleanup, you can run `scheduled-print-service/dev-move-artifacts.ps1` to move any stray `logs/`, `out/`, `chromium-cache/`, `printed-urls.txt`, `output.txt`, `error.txt` from repo root into `scheduled-print-service/`.
* IMPORTANT: When using the helper scripts, invoke them from the existing shell (e.g. `powershell -ExecutionPolicy Bypass -File .\scheduled-print-service\dev-use-local-data.ps1` OR simply `./scheduled-print-service/dev-use-local-data.ps1`). Avoid starting a new pwsh instance that then exits, or the environment variable will not persist for the subsequent run.

## API polling and batch picklist sub-action
When `Api.Enabled=true`, the service polls `GetOrdersList` and executes configured `SubActions`.

To create pending order picklists in batches of ~10 IDs, add this sub-action to `Api.SubActions` in `appsettings.json`:

```jsonc
{
  "Type": "CreatePicklistBatch",
  "Name": "Create Pending Order Picklist Batch",
  "Endpoint": "/api/PickList/CreatePendingOrderPicklist",
  "Method": "POST",
  "BatchSize": 10,
  "QuickShip": false,
  "ContinueOnError": true
}
```

This runs once per poll cycle, batching the current list of order IDs and POSTing a payload like:

```json
{ "orderId": [2470,2482,2479,2478,2477,2476,2473,2472,2471,2468], "QuickShip": false }
```

Authentication headers, WarehouseId, and cookies are taken from `Api` settings.
