# Personalized Orders Print Setup Guide

## Overview

This guide explains how to set up the Personalized Orders API configuration for the Scheduled Print Service. This feature automatically:

1. Fetches personalized order items from the 3PL API
2. Filters for items with custom form file paths in the `itemNotes` field
3. Converts each custom form HTML to PDF
4. Automatically prints the PDFs

## Prerequisites

- Scheduled Print Service must be installed and running
- Access to the production database (api_config.db)
- Valid authentication credentials for the 3PL API
- Printer configured for PDF printing

## Installation Steps

### 1. Apply Database Migration

**IMPORTANT**: This migration must be run on the PRODUCTION database. Review the script carefully before executing.

```powershell
# Navigate to the service directory
cd C:\path\to\ScheduledPrintService

# Apply the migration using sqlite3
sqlite3 api_config.db < add-personalized-orders-api.sql
```

Alternatively, run the SQL statements manually:

```powershell
sqlite3 api_config.db
```

Then paste the contents of `add-personalized-orders-api.sql` and execute.

### 2. Update Authentication Token

The migration script includes placeholder tokens. You must update these with valid credentials:

```sql
-- Update the Bearer token in PrimaryApi Headers
UPDATE PrimaryApi
SET Headers = json_replace(
    Headers,
    '$.Authorization',
    'Bearer YOUR_ACTUAL_TOKEN_HERE'
)
WHERE ApiNumber = 3;

-- Update Cookie in PrimaryApi Headers
UPDATE PrimaryApi
SET Headers = json_replace(
    Headers,
    '$.Cookie',
    'token=YOUR_ACTUAL_TOKEN_HERE; userData=YOUR_USER_DATA_HERE; isRefreshedToken=false'
)
WHERE ApiNumber = 3;
```

**Note**: The token renewal service will automatically refresh tokens, so you only need to set an initially valid token.

### 3. Verify Configuration

Check that the API configuration was created correctly:

```sql
-- View Primary API configuration
SELECT ApiNumber, ApiName, Endpoint, IsEnabled
FROM PrimaryApi
WHERE ApiNumber = 3;

-- View Sub-Action configuration
SELECT s.ActionName, s.ActionType, s.IsEnabled, s.Configuration
FROM SubAction s
JOIN PrimaryApi p ON s.PrimaryApiId = p.Id
WHERE p.ApiNumber = 3;

-- View Schedule configuration
SELECT ScheduleName, ScheduleType, IntervalSeconds, IsEnabled, Description
FROM Schedule
WHERE ScheduleName = 'Personalized Orders Print Schedule';
```

Expected results:
- Primary API: `Personalized Orders API` with endpoint `/api/Order/GetPersonalizedOrderItems`
- Sub-Action: `Print Custom Forms` with type `GetUrlAndPrint`
- Schedule: Disabled by default (IsEnabled = 0)

### 4. Test Configuration (Manual Mode)

Before enabling the schedule, test manually:

```powershell
# Run the service with specific API number (manual mode)
cd C:\path\to\ScheduledPrintService

# Run as console application for testing
dotnet run --api-number 3

# OR if already published
.\ScheduledPrintService.exe --api-number 3
```

This will:
1. Fetch personalized order items once
2. Filter for items with file paths in itemNotes
3. Print each custom form
4. Exit after completion

Check the logs for any errors or issues.

### 5. Enable Automatic Schedule (Production)

Once testing is successful, enable the schedule:

```sql
-- Enable the schedule to run automatically every hour
UPDATE Schedule
SET IsEnabled = 1
WHERE ScheduleName = 'Personalized Orders Print Schedule';
```

You can also adjust the interval:

```sql
-- Change to run every 30 minutes (1800 seconds)
UPDATE Schedule
SET IntervalSeconds = 1800
WHERE ScheduleName = 'Personalized Orders Print Schedule';

-- Change to run every 2 hours (7200 seconds)
UPDATE Schedule
SET IntervalSeconds = 7200
WHERE ScheduleName = 'Personalized Orders Print Schedule';
```

### 6. Restart Service

After making changes, restart the Windows service:

```powershell
# Stop the service
Stop-Service -Name "ScheduledPrintService"

# Start the service
Start-Service -Name "ScheduledPrintService"

# Check service status
Get-Service -Name "ScheduledPrintService"
```

## How It Works

### Data Flow

1. **API Request**: Service sends POST request to `/api/Order/GetPersonalizedOrderItems` with payload:
   ```json
   {"SKu":"","ClientId":"1","searchvalue":""}
   ```

2. **Response Processing**: Service receives array of personalized order items in `data` field

3. **Filtering**: Service filters items where:
   - `itemNotes` field is NOT empty
   - `itemNotes` contains ".html" (file path)
   - `itemNotes` does NOT start with "{" (excludes JSON data)

4. **URL Construction**: For each filtered item, constructs URL:
   ```
   https://mj.3plnext.com/{itemNotes}
   ```
   Example: `https://mj.3plnext.com/Store/CustomForms/SW4425/SW4425_63725.html`

5. **HTML Fetch & Print**:
   - Navigates to URL using Puppeteer (headless Chrome)
   - Waits 3 seconds for page load
   - Converts HTML to PDF
   - Sends PDF to configured printer

### Filter Configuration

The filter is configured in the SubAction with these properties:

```json
{
  "ChainedFilterField": "itemNotes",
  "ChainedFilterType": "IsFilePath",
  "ChainedFilterValue": null
}
```

**Filter Types Available**:
- `NotEmpty` - Field must not be empty
- `Contains` - Field must contain specified value
- `NotContains` - Field must NOT contain specified value
- `StartsWith` - Field must start with specified value
- `NotStartsWith` - Field must NOT start with specified value
- `IsFilePath` - Field must be a file path (.html extension, not JSON)

### Example Data

Items that WILL be processed:
```json
{
  "itemNotes": "Store\\CustomForms\\SW4425\\SW4425_63725.html",
  // ... other fields
}
```

Items that will be SKIPPED:
```json
// Empty itemNotes
{"itemNotes": ""}

// JSON data in itemNotes
{"itemNotes": "{\"sku\":\"BMX825BG\",\"title\":\"Bar Mitzvah Bag...\"}"}
```

## Monitoring

### Check Logs

The service logs all activity to the Windows Event Log and console output:

```powershell
# View recent service logs
Get-EventLog -LogName Application -Source "ScheduledPrintService" -Newest 50

# Or check console logs if running in console mode
# Logs will show:
# - "Fetching URL with Puppeteer: https://mj.3plnext.com/Store/..."
# - "Printing PDF: Print Custom Forms-unknown (12345 bytes)"
# - "Item filtered out by IsFilePath on field 'itemNotes'" (for skipped items)
```

### Monitor Database

Track processed items to avoid duplicates:

```sql
-- View processed order details IDs
-- (The service tracks processed IDs in processed-orders.txt by default)
```

### Troubleshooting

**Problem**: No items are being printed

**Solutions**:
1. Check that items exist with file paths in itemNotes:
   ```sql
   -- Run the API request manually and check response
   ```
2. Verify filter is working correctly by checking logs
3. Ensure printer is configured and accessible
4. Check authentication token is valid

**Problem**: Service crashes or fails

**Solutions**:
1. Check Windows Event Log for errors
2. Verify database schema is correct
3. Ensure Chromium is installed for Puppeteer
4. Check network connectivity to 3PL API

**Problem**: PDFs are blank or incomplete

**Solutions**:
1. Increase `WaitForNetworkIdleMs` in SubAction configuration
2. Check that HTML files are accessible via URL
3. Verify authentication cookies are being passed correctly

## Advanced Configuration

### Change Print Delay

If pages need more time to load, adjust the wait time:

```sql
UPDATE SubAction
SET Configuration = json_replace(
    Configuration,
    '$.WaitForNetworkIdleMs',
    5000  -- Wait 5 seconds instead of 3
)
WHERE ActionName = 'Print Custom Forms';
```

### Add Additional Filters

You can combine multiple filters by adding another sub-action:

```sql
-- Example: Only print items with specific SKU pattern
INSERT INTO SubAction (...) VALUES (
    3,
    2,  -- Second sub-action
    'Additional Filter',
    'GetUrlAndPrint',
    '{"ChainedFilterField":"sku","ChainedFilterType":"StartsWith","ChainedFilterValue":"BMX",...}',
    2,
    1
);
```

### Change Printer Settings

The service uses the default printer configured in Windows. To change:

1. Open Windows Settings → Printers & Scanners
2. Set desired printer as default
3. Restart the service

## Rollback

To disable or remove this feature:

```sql
-- Disable the schedule
UPDATE Schedule
SET IsEnabled = 0
WHERE ScheduleName = 'Personalized Orders Print Schedule';

-- Disable the sub-action
UPDATE SubAction
SET IsEnabled = 0
WHERE ActionName = 'Print Custom Forms';

-- Or completely remove (use with caution)
DELETE FROM ScheduleApi WHERE ApiNumber = 3;
DELETE FROM Schedule WHERE ScheduleName = 'Personalized Orders Print Schedule';
DELETE FROM SubAction WHERE PrimaryApiId = (SELECT Id FROM PrimaryApi WHERE ApiNumber = 3);
DELETE FROM PrimaryApi WHERE ApiNumber = 3;
```

## Support

For issues or questions:
1. Check the Windows Event Log
2. Review service logs in console mode
3. Verify database configuration
4. Check authentication tokens are current

## Schema Reference

### PrimaryApi Table
- `ApiNumber`: Unique identifier (3 for Personalized Orders)
- `Endpoint`: API endpoint path
- `HttpMethod`: POST
- `Headers`: JSON containing Authorization, Cookie, etc.
- `Payload`: Request body JSON

### SubAction Table
- `ActionType`: GetUrlAndPrint (uses Puppeteer)
- `Configuration`: JSON with all action settings
- `ExecutionOrder`: Order of execution (1 = first)
- `ChainedFilterField`: Field to filter on (itemNotes)
- `ChainedFilterType`: Type of filter (IsFilePath)

### Schedule Table
- `ScheduleType`: Interval (time-based)
- `IntervalSeconds`: How often to run (3600 = 1 hour)
- `IsEnabled`: Whether schedule is active
