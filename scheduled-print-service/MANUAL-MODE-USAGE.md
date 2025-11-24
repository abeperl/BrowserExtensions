# Manual Mode Usage Guide

## Overview

The scheduled-print-service now supports **manual mode**, which allows you to run a specific API configuration from the database without waiting for the schedule. This is perfect for debugging, testing, or running APIs on-demand.

## Command-Line Arguments

### `--manual` or `-m`
Enables manual mode. When set, the service will:
- Run once and exit immediately
- Ignore all schedules
- Process only the specified API

### `--api-number <number>` or `-a <number>`
Specifies which API configuration to load from the database.

## Usage Examples

### Run API #2 Manually
```powershell
cd scheduled-print-service\ScheduledPrintService
dotnet run -- --manual --api-number 2
```

### Run API #1 Manually
```powershell
dotnet run -- --manual --api-number 1
```

### Using Short Flags
```powershell
dotnet run -- -m -a 2
```

## VS Code Debugging

The `.vscode/launch.json` file includes debug configurations for manual mode:

1. **Debug Scheduled Print Service (API #2 - Picklist)**
   - Press F5 in VS Code
   - Select "Debug Scheduled Print Service (API #2 - Picklist)"
   - Service will run in debug mode with API #2

## How It Works

### Without Manual Mode (Normal Operation)
- Reads configuration from `appsettings.json` (legacy)
- Runs on schedule indefinitely
- Polls API on configured interval (e.g., every 3600 seconds)

### With Manual Mode (--manual --api-number N)
1. Parses command-line arguments in `Program.cs`
2. Uses `DatabaseApiConfigService` to load API configuration from `api_config.db`
3. Loads:
   - Primary API settings (BaseUrl, BearerToken, Headers, Params)
   - All sub-actions for that API
4. Runs API polling once
5. Exits immediately after processing

## Configuration Source

### Manual Mode
- Loads from: `api_config.db` (SQLite database)
- Location: `ScheduledPrintService\api_config.db`
- Tables used: `PrimaryApi`, `SubAction`

### Legacy Mode (No Arguments)
- Loads from: `appsettings.json`
- Note: The `Api` section has been removed from `appsettings.json` since all configurations are now in the database

## Database Query

To see available APIs for manual mode:

```sql
SELECT ApiNumber, ApiName, IsEnabled 
FROM PrimaryApi 
ORDER BY ApiNumber;
```

Example output:
```
ApiNumber | ApiName                      | IsEnabled
----------|------------------------------|----------
1         | Orders List API              | 1
2         | Picklist Datatable API       | 1
```

## Log Output

Manual mode logs show:
```
[INFO] Loading API configuration for ApiNumber=2 from database
[INFO] Loaded API configuration: 2 sub-actions (2 enabled)
[INFO] Manual mode enabled with API #2 from database
[INFO] Manual mode: True
...
[INFO] Manual mode enabled - exiting after single run
```

## Troubleshooting

### "Database file not found"
- Ensure `api_config.db` exists in the project directory
- The database is automatically copied to the build output directory
- Rebuild the project: `dotnet build`

### "API #X not found in database"
- Check available APIs with the SQL query above
- Verify the API number is correct
- Ensure the API is enabled (`IsEnabled = 1`)

### Service doesn't exit
- Confirm `--manual` flag is set
- Check logs for "Manual mode: True"
- Look for "Manual mode enabled - exiting after single run"

## Related Documentation

- `DATABASE-SCHEMA-DIAGRAM.md` - Database structure
- `MANUAL-API-EXECUTION.md` - PowerShell script for manual execution
- `API-2-PICKLIST-DOCUMENTATION.md` - API #2 specific details
- `.vscode/launch.json` - Debug configurations
