# API Filtering and Per-API Printer Setup Guide

## Overview

This guide covers the enhancements to the Scheduled Print Service:

1. **Array Index Filtering**: Filter picklist items based on array index values
2. **Per-API Printer Configuration**: Configure different printers for each API
3. **API 2 Update**: Filter for items where index 17 starts with "SS"
4. **API 4 Creation**: New API for items where index 17 does NOT start with "SS", printing to printer "4301"

## What's New

### 1. Array Index Filtering

The filtering system now supports filtering on array indices for APIs that return array-of-arrays data structures.

**New Filter Properties**:
- `ChainedFilterArrayIndex`: Index position to filter (e.g., 17 for `data[x][17]`)
- `ChainedFilterType`: Filter type (StartsWith, NotStartsWith, Equals, NotEquals, Contains, NotContains)
- `ChainedFilterValue`: Value to compare against

**Example API Response Structure**:
```json
{
  "data": [
    [value0, value1, ... , value17, ...],  // Item 1
    [value0, value1, ... , value17, ...],  // Item 2
    ...
  ]
}
```

### 2. Per-API Printer Configuration

Each API can now specify a custom printer. If not specified, the service uses the default printer from configuration.

**PrinterName Column**: Added to `PrimaryApi` table
- `NULL` = Use default printer
- Specific name (e.g., "4301") = Use that printer

## Prerequisites

- Scheduled Print Service must be installed and running
- Access to the production database (api_config.db)
- Printers must be installed and accessible on the server
- Printer "4301" must be configured for API 4

## Installation Steps

### 1. Backup Database

**CRITICAL**: Always backup the production database before applying migrations.

```powershell
# Navigate to service directory
cd C:\path\to\ScheduledPrintService

# Create backup
Copy-Item api_config.db api_config.db.backup.$(Get-Date -Format 'yyyyMMdd_HHmmss')
```

### 2. Apply Database Migration

```powershell
# Apply the migration
sqlite3 api_config.db < update-api-2-and-create-api-4.sql
```

**What this does**:
1. Adds `PrinterName` column to `PrimaryApi` table
2. Updates API 2 NavigateOnly action with filter for index 17 starts with "SS"
3. Creates API 4 as copy of API 2
4. Sets filter for API 4 to NOT start with "SS"
5. Sets printer "4301" for API 4
6. Creates disabled schedule for API 4

### 3. Verify Migration

```sql
-- Check PrinterName column was added
PRAGMA table_info(PrimaryApi);

-- Check API 2 has filter
SELECT
    s.ActionNumber,
    s.ActionName,
    s.ActionType,
    json_extract(s.Configuration, '$.ChainedFilterArrayIndex') AS FilterIndex,
    json_extract(s.Configuration, '$.ChainedFilterType') AS FilterType,
    json_extract(s.Configuration, '$.ChainedFilterValue') AS FilterValue
FROM SubAction s
JOIN PrimaryApi p ON s.PrimaryApiId = p.Id
WHERE p.ApiNumber = 2;

-- Expected output:
-- ActionNumber: 1
-- ActionName: Get Manual Picking Page URL
-- ActionType: NavigateOnly
-- FilterIndex: 17
-- FilterType: StartsWith
-- FilterValue: SS

-- Check API 4 configuration
SELECT ApiNumber, ApiName, PrinterName, IsEnabled
FROM PrimaryApi
WHERE ApiNumber = 4;

-- Expected output:
-- ApiNumber: 4
-- ApiName: Picklist Datatable API (Non-SS)
-- PrinterName: 4301
-- IsEnabled: 1

-- Check API 4 has inverse filter
SELECT
    s.ActionNumber,
    s.ActionName,
    json_extract(s.Configuration, '$.ChainedFilterArrayIndex') AS FilterIndex,
    json_extract(s.Configuration, '$.ChainedFilterType') AS FilterType,
    json_extract(s.Configuration, '$.ChainedFilterValue') AS FilterValue
FROM SubAction s
JOIN PrimaryApi p ON s.PrimaryApiId = p.Id
WHERE p.ApiNumber = 4
  AND s.ActionType = 'NavigateOnly';

-- Expected output:
-- FilterIndex: 17
-- FilterType: NotStartsWith
-- FilterValue: SS
```

### 4. Test Configuration

Before enabling schedules, test each API manually:

```powershell
# Test API 2 (SS items only)
cd C:\path\to\ScheduledPrintService
dotnet run --api-number 2

# Check logs for:
# - "Item filtered out by StartsWith" for non-SS items
# - "Printing PDF" for SS items
# - Printer used should be default printer

# Test API 4 (Non-SS items, printer 4301)
dotnet run --api-number 4

# Check logs for:
# - "Item filtered out by NotStartsWith" for SS items
# - "Printing PDF" for non-SS items
# - "Spooling job to printer '4301'" confirming correct printer
```

### 5. Enable Schedules

Once testing is successful:

```sql
-- Enable API 2 schedule (if not already enabled)
UPDATE Schedule
SET IsEnabled = 1
WHERE ScheduleName LIKE '%Picklist%'
  AND ScheduleName NOT LIKE '%Non-SS%';

-- Enable API 4 schedule
UPDATE Schedule
SET IsEnabled = 1
WHERE ScheduleName = 'Picklist Non-SS Print Schedule';

-- Verify both schedules
SELECT ScheduleName, IntervalSeconds, IsEnabled
FROM Schedule
WHERE ScheduleName LIKE '%Picklist%';
```

### 6. Restart Service

```powershell
Restart-Service -Name "ScheduledPrintService"

# Check service status
Get-Service -Name "ScheduledPrintService"

# Monitor logs
Get-EventLog -LogName Application -Source "ScheduledPrintService" -Newest 20
```

## How It Works

### Data Flow for API 2 (SS Items)

1. **Fetch Data**: GET `/api/Picklist/GetPicklistDatatable`
2. **Response**: Array of arrays `data[[...], [...], ...]`
3. **Extract Items**: Each item is an array
4. **Filter**: Check if `item[17]` starts with "SS"
   - If YES → Process item
   - If NO → Skip item (logged as "Item filtered out")
5. **Navigate**: Load picklist page in browser
6. **Print**: Convert to PDF and send to default printer

### Data Flow for API 4 (Non-SS Items)

1. **Fetch Data**: Same endpoint as API 2
2. **Response**: Same data structure
3. **Extract Items**: Each item is an array
4. **Filter**: Check if `item[17]` does NOT start with "SS"
   - If YES (not SS) → Process item
   - If NO (is SS) → Skip item
5. **Navigate**: Load picklist page in browser
6. **Print**: Convert to PDF and send to **printer "4301"**

### Filter Logic Examples

```javascript
// API 2 Filter Configuration
{
  "ChainedFilterArrayIndex": 17,
  "ChainedFilterType": "StartsWith",
  "ChainedFilterValue": "SS"
}

// Example items:
["val0", "val1", ..., "SS12345", ...]  // ✓ PROCESSED by API 2
["val0", "val1", ..., "REF9876", ...]  // ✗ SKIPPED by API 2

// API 4 Filter Configuration
{
  "ChainedFilterArrayIndex": 17,
  "ChainedFilterType": "NotStartsWith",
  "ChainedFilterValue": "SS"
}

// Example items:
["val0", "val1", ..., "SS12345", ...]  // ✗ SKIPPED by API 4
["val0", "val1", ..., "REF9876", ...]  // ✓ PROCESSED by API 4
```

## Advanced Configuration

### Change Filter Value

To filter on different values:

```sql
-- Example: Change API 2 to filter for "REF" instead of "SS"
UPDATE SubAction
SET Configuration = json_set(
    json(Configuration),
    '$.ChainedFilterValue', 'REF'
)
WHERE PrimaryApiId = (SELECT Id FROM PrimaryApi WHERE ApiNumber = 2)
  AND ActionType = 'NavigateOnly';
```

### Change Printer

To change the printer for an API:

```sql
-- Change API 4 printer from "4301" to "5200"
UPDATE PrimaryApi
SET PrinterName = '5200'
WHERE ApiNumber = 4;

-- Set API 2 to use specific printer (currently uses default)
UPDATE PrimaryApi
SET PrinterName = 'MainPrinter'
WHERE ApiNumber = 2;

-- Revert API to default printer
UPDATE PrimaryApi
SET PrinterName = NULL
WHERE ApiNumber = 2;
```

### Change Filter Index

To filter on a different array index:

```sql
-- Example: Filter on index 10 instead of 17
UPDATE SubAction
SET Configuration = json_set(
    json(Configuration),
    '$.ChainedFilterArrayIndex', 10
)
WHERE PrimaryApiId = (SELECT Id FROM PrimaryApi WHERE ApiNumber = 2)
  AND ActionType = 'NavigateOnly';
```

### Add Additional Filter Types

Available filter types:
- `StartsWith` - Field starts with value
- `NotStartsWith` - Field does NOT start with value
- `Equals` - Field equals value exactly
- `NotEquals` - Field does NOT equal value
- `Contains` - Field contains value anywhere
- `NotContains` - Field does NOT contain value
- `NotEmpty` - Field is not empty
- `IsFilePath` - Special filter for file paths (has .html, not JSON)

```sql
-- Example: Filter for exact match instead of starts with
UPDATE SubAction
SET Configuration = json_set(
    json_set(
        json(Configuration),
        '$.ChainedFilterType', 'Equals'
    ),
    '$.ChainedFilterValue', 'SS12345'
)
WHERE PrimaryApiId = (SELECT Id FROM PrimaryApi WHERE ApiNumber = 2)
  AND ActionType = 'NavigateOnly';
```

## Monitoring

### Check What's Being Processed

```powershell
# View recent logs
Get-EventLog -LogName Application -Source "ScheduledPrintService" -Newest 50 |
    Where-Object { $_.Message -like "*filtered*" -or $_.Message -like "*Printing*" }

# Logs will show:
# - "Item filtered out by StartsWith" (API 2 skipping non-SS)
# - "Item filtered out by NotStartsWith" (API 4 skipping SS)
# - "Printing PDF: ... to printer 4301" (API 4 printing)
# - "Printing PDF: ... to printer default" (API 2 printing)
```

### Verify Printer Usage

```powershell
# Check Windows print queue for printer 4301
Get-PrintJob -PrinterName "4301"

# Check all print jobs
Get-PrintJob -PrinterName *
```

### Database Queries

```sql
-- Count items that would be processed by each API
-- (Requires sample data query)

-- Check current API configurations
SELECT
    ApiNumber,
    ApiName,
    PrinterName,
    IsEnabled
FROM PrimaryApi
WHERE ApiNumber IN (2, 4);

-- Check schedule status
SELECT
    s.ScheduleName,
    sa.ApiNumber,
    s.IntervalSeconds,
    s.IsEnabled
FROM Schedule s
JOIN ScheduleApi sa ON s.Id = sa.ScheduleId
WHERE sa.ApiNumber IN (2, 4);
```

## Troubleshooting

### Problem: Printer "4301" not found

**Solution**:
1. Verify printer is installed:
   ```powershell
   Get-Printer | Select-Object Name
   ```
2. If printer name is different, update database:
   ```sql
   UPDATE PrimaryApi
   SET PrinterName = 'ActualPrinterName'
   WHERE ApiNumber = 4;
   ```

### Problem: All items are filtered out

**Solution**:
1. Check filter configuration:
   ```sql
   SELECT json_extract(Configuration, '$') AS Config
   FROM SubAction
   WHERE PrimaryApiId = (SELECT Id FROM PrimaryApi WHERE ApiNumber = 2)
     AND ActionType = 'NavigateOnly';
   ```
2. Verify data structure matches expectations (index 17 exists)
3. Check filter value matches actual data

### Problem: Items printing to wrong printer

**Solution**:
1. Check PrinterName in database:
   ```sql
   SELECT ApiNumber, PrinterName FROM PrimaryApi WHERE ApiNumber = 4;
   ```
2. Verify printer exists in Windows
3. Restart service after configuration changes

### Problem: Both APIs processing same items

**Solution**:
1. Verify filters are inverse:
   ```sql
   SELECT
       p.ApiNumber,
       json_extract(s.Configuration, '$.ChainedFilterType') AS FilterType,
       json_extract(s.Configuration, '$.ChainedFilterValue') AS FilterValue
   FROM SubAction s
   JOIN PrimaryApi p ON s.PrimaryApiId = p.Id
   WHERE p.ApiNumber IN (2, 4) AND s.ActionType = 'NavigateOnly';
   ```
2. API 2 should have: StartsWith / SS
3. API 4 should have: NotStartsWith / SS

## Rollback

To revert changes:

```sql
-- Disable API 4
UPDATE PrimaryApi SET IsEnabled = 0 WHERE ApiNumber = 4;
UPDATE Schedule SET IsEnabled = 0 WHERE ScheduleName = 'Picklist Non-SS Print Schedule';

-- Remove filter from API 2
UPDATE SubAction
SET Configuration = json_remove(
    json_remove(
        json_remove(
            json(Configuration),
            '$.ChainedFilterArrayIndex'
        ),
        '$.ChainedFilterType'
    ),
    '$.ChainedFilterValue'
)
WHERE PrimaryApiId = (SELECT Id FROM PrimaryApi WHERE ApiNumber = 2)
  AND ActionType = 'NavigateOnly';

-- Complete rollback (removes API 4 entirely)
DELETE FROM ScheduleApi WHERE ApiNumber = 4;
DELETE FROM Schedule WHERE ScheduleName = 'Picklist Non-SS Print Schedule';
DELETE FROM SubAction WHERE PrimaryApiId = (SELECT Id FROM PrimaryApi WHERE ApiNumber = 4);
DELETE FROM PrimaryApi WHERE ApiNumber = 4;

-- Remove PrinterName column (requires table rebuild in SQLite)
-- Backup database first!
-- This is complex - only do if absolutely necessary
```

## Summary

**API 2**:
- Filters: `data[x][17]` starts with "SS"
- Printer: Default (or configured via PrinterName)
- Purpose: Process SS-prefixed items

**API 4**:
- Filters: `data[x][17]` does NOT start with "SS"
- Printer: "4301"
- Purpose: Process non-SS items separately

**New Features**:
- Array index filtering
- Per-API printer configuration
- Flexible filter types (StartsWith, NotStartsWith, Equals, etc.)
