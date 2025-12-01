# API 3 Configuration Guide - Personalized Orders

## Overview

API 3 (Personalized Orders API) has been configured with **independent control** over PDF saving and printing operations. You can now enable/disable these operations separately based on your needs.

## Sub-Action Architecture

API 3 executes three sub-actions in sequence:

| Order | Action Name | Type | Purpose |
|-------|------------|------|---------|
| 1 | Navigate to Custom Form | `NavigateOnly` | Performs GET request to static HTML page (no SPA navigation) |
| 2 | Save PDF to Disk | `SaveCapturedHtml` | Creates PDF from captured page and saves to disk |
| 3 | Print Saved PDF | `PrintSavedPdf` | Prints the saved PDF file |

### Key Features

- **Independent Control**: Enable/disable save and print operations separately
- **Execution Flow**: Navigate → Save → Print (each can be enabled/disabled)
- **File Persistence**: Saved PDFs are stored in `data/out/` directory
- **Error Handling**: Each action has `ContinueOnError: true` for resilience

## Configuration Management

### Using PowerShell Script (Recommended)

The easiest way to manage API 3 actions is using the PowerShell script:

```powershell
# View current status
.\scripts\manage-api3-actions.ps1 -Action status

# Enable both save and print (default)
.\scripts\manage-api3-actions.ps1 -Action both

# Save ONLY (no printing)
.\scripts\manage-api3-actions.ps1 -Action save-only

# Print ONLY (no saving to disk)
.\scripts\manage-api3-actions.ps1 -Action print-only

# Navigate ONLY (no save or print)
.\scripts\manage-api3-actions.ps1 -Action neither

# Fine-grained control
.\scripts\manage-api3-actions.ps1 -Action enable-save
.\scripts\manage-api3-actions.ps1 -Action disable-save
.\scripts\manage-api3-actions.ps1 -Action enable-print
.\scripts\manage-api3-actions.ps1 -Action disable-print
```

### Using SQL Directly

Alternatively, you can use SQL commands directly:

```sql
-- View current status
SELECT ActionNumber, ActionName, ActionType, IsEnabled
FROM SubAction sa
JOIN PrimaryApi pa ON sa.PrimaryApiId = pa.Id
WHERE pa.ApiNumber = 3
ORDER BY ExecutionOrder;

-- Enable Save PDF
UPDATE SubAction SET IsEnabled = 1 WHERE Id = 15;

-- Disable Save PDF
UPDATE SubAction SET IsEnabled = 0 WHERE Id = 15;

-- Enable Print PDF
UPDATE SubAction SET IsEnabled = 1 WHERE Id = 16;

-- Disable Print PDF
UPDATE SubAction SET IsEnabled = 0 WHERE Id = 16;
```

## Common Use Cases

### 1. Production Mode (Default)
**Save AND Print both enabled**
- PDFs are saved to disk for record-keeping
- PDFs are also sent to printer
- Use: Normal production workflow

```powershell
.\scripts\manage-api3-actions.ps1 -Action both
```

### 2. Testing Mode
**Save ONLY, Print disabled**
- PDFs saved to disk for review
- No physical printing (saves paper/toner)
- Use: Testing, development, verification

```powershell
.\scripts\manage-api3-actions.ps1 -Action save-only
```

### 3. Print-Only Mode
**Save disabled, Print enabled**
- PDFs sent directly to printer
- No disk storage (saves space)
- Use: High-volume environments where archival isn't needed

```powershell
.\scripts\manage-api3-actions.ps1 -Action print-only
```

### 4. Navigation Test Mode
**Both disabled**
- Only navigates to page and captures it
- No PDF creation, saving, or printing
- Use: Testing navigation and page loading

```powershell
.\scripts\manage-api3-actions.ps1 -Action neither
```

## Technical Details

### Sub-Action 1: NavigateOnly

**Configuration:**
```json
{
  "Endpoint": "https://mj.3plnext.com/{itemNotes}",
  "Method": "GET",
  "WaitForNetworkIdleMs": 3000,
  "ChainedArrayJsonPath": "data",
  "ChainedFilterField": "itemNotes",
  "ChainedFilterType": "IsFilePath",
  "UseChainedInput": true
}
```

**Behavior:**
- Performs simple GET request to static HTML page
- No SPA navigation or JavaScript interaction
- Waits for network idle before proceeding
- Filters items based on file path pattern (.html)

### Sub-Action 2: SaveCapturedHtml

**Configuration:**
```json
{
  "ContinueOnError": true
}
```

**Behavior:**
- Uses page captured from NavigateOnly action
- Injects CSS for portrait orientation
- Hides "Short Items" sections
- Highlights customer names
- Generates PDF from rendered page
- Saves to `data/out/{timestamp}_{jobname}.pdf`
- Stores file path for next action
- Disposes browser page after saving

**Output Location:**
- Directory: `scheduled-print-service/data/out/`
- Filename format: `20251130_143025123_Save PDF to Disk.pdf`

### Sub-Action 3: PrintSavedPdf

**Configuration:**
```json
{
  "ContinueOnError": true
}
```

**Behavior:**
- Reads PDF file from path stored by SaveCapturedHtml
- Sends PDF bytes to configured printer
- Uses printer name from PrimaryApi configuration
- Clears stored file path after printing

**Printer Configuration:**
Current printer: `NPI84BD10 (HP LaserJet M607)`

## Code Implementation

### New Action Types

Two new action types have been added to `SubActionExecutor.cs`:

1. **SaveCapturedHtml** (`ExecuteSaveCapturedHtmlAsync`)
   - Creates PDF from captured page
   - Saves to disk
   - Stores file path in `_lastSavedPdfPath`

2. **PrintSavedPdf** (`ExecutePrintSavedPdfAsync`)
   - Reads saved PDF file
   - Sends to printer
   - Clears `_lastSavedPdfPath`

### Execution Flow

```
┌─────────────────────────┐
│ 1. NavigateOnly         │
│ GET static HTML page    │
│ Store in _capturedPage  │
└───────────┬─────────────┘
            │
            ▼
┌─────────────────────────┐
│ 2. SaveCapturedHtml     │ ◄─── Can be disabled
│ Generate PDF            │
│ Save to disk            │
│ Store path              │
└───────────┬─────────────┘
            │
            ▼
┌─────────────────────────┐
│ 3. PrintSavedPdf        │ ◄─── Can be disabled
│ Read saved PDF          │
│ Send to printer         │
└─────────────────────────┘
```

## Error Handling

All actions have `ContinueOnError: true`, meaning:
- If Save fails, Print will be skipped (no file available)
- If Print fails, the saved file remains on disk
- Errors are logged but don't halt the entire workflow

## Monitoring

Check logs for action execution:

```
PDF saved to {Path}
PDF sent to printer {Printer}: {JobName}
Successfully saved page to PDF
Successfully printed saved PDF
```

## Troubleshooting

### Save PDF Not Working
- Check `data/out/` directory exists
- Verify disk space available
- Check logs for "Failed to inject PDF save styles"

### Print PDF Not Working
- Verify saved PDF file exists in `data/out/`
- Check printer name in PrimaryApi configuration
- Ensure printer is online and accessible
- Check logs for "Saved PDF file not found"

### Neither Save Nor Print Working
- Check if sub-actions are enabled in database
- Verify NavigateOnly action succeeded
- Check if `_capturedPage` is null in logs

## Database Schema

```sql
CREATE TABLE SubAction (
    Id INTEGER PRIMARY KEY,
    PrimaryApiId INTEGER NOT NULL,
    ActionNumber INTEGER NOT NULL,
    ActionName TEXT NOT NULL,
    ActionType TEXT NOT NULL,
    Configuration TEXT,
    ExecutionOrder INTEGER DEFAULT 0,
    IsEnabled INTEGER DEFAULT 1,
    CreatedAt TEXT DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TEXT DEFAULT CURRENT_TIMESTAMP
);
```

Current records for API 3:
```
Id=14: Navigate to Custom Form (NavigateOnly)
Id=15: Save PDF to Disk (SaveCapturedHtml)
Id=16: Print Saved PDF (PrintSavedPdf)
```

## Migration Notes

This configuration replaces the previous single `PrintCapturedHtml` action that performed both save and print operations atomically. The new architecture provides:

1. **Flexibility**: Independent control over save/print
2. **Testability**: Can test saving without printing
3. **Efficiency**: Can skip saving in print-only scenarios
4. **Clarity**: Explicit separation of concerns

## See Also

- `scripts/manage-api3-actions.ps1` - PowerShell management script
- `scripts/toggle-api3-actions.sql` - SQL command reference
- `ScheduledPrintService/Services/SubActionExecutor.cs:2209` - SaveCapturedHtml implementation
- `ScheduledPrintService/Services/SubActionExecutor.cs:2405` - PrintSavedPdf implementation
