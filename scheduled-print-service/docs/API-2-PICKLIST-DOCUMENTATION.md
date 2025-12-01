# API #2: Picklist Datatable API

## Overview

**API Number:** 2  
**Name:** Picklist Datatable API  
**Endpoint:** `POST https://mj.3plnext.com/api/Picklist/GetPicklistDatatable`  
**Status:** ✅ Enabled

## Purpose

Fetches picklist data from the 3PL system and processes each picklist by:
1. Navigating to the Manual Picking page for each picklist
2. Converting the HTML to PDF and saving it

## Configuration

### Headers
- **Authorization:** Bearer token (stored in database)
- **Content-Type:** application/json
- **Cookie:** Session cookies (userData, token, isRefreshedToken)
- **WarehouseId:** 1
- **X-Requested-With:** XMLHttpRequest
- All standard browser headers included

### Request Body (POST)
```json
{
  "draw": 1,
  "start": 0,
  "length": 25,
  "order": [{"column": 13, "dir": "desc"}],
  "search": {"value": "", "regex": false},
  "statusName": "0,2",
  "clientid": "1",
  "param1": "-1",
  "param2": "all",
  "orderType": "",
  "dateFrom": null,
  "dateTo": null,
  "columns": [...]
}
```

### Key Parameters
- **statusName:** "0,2" - Filter by picklist status
- **clientid:** "1" - Filter by client
- **length:** 25 - Number of records to fetch
- **start:** 0 - Starting record index

## Sub-Actions (2 total, both enabled)

### Sub-Action #1: Get Manual Picking Page URL
**Type:** GetUrlAndPrint  
**Status:** ✅ Enabled  
**Execution Order:** 1

**Configuration:**
```json
{
  "Endpoint": "https://mj.3plnext.com/#Outbound/ManualPicking?id={id}",
  "Method": "GET",
  "UseChainedInput": true,
  "ChainedArrayJsonPath": "data",
  "ChainedItemFieldPath": "[0]",
  "WaitForNetworkIdleMs": 3000,
  "MakeHiddenVisible": true,
  "ContinueOnError": true
}
```

**Purpose:**
- Takes the first element `[0]` from each item in the `data` array returned by the primary API
- Navigates to the Manual Picking page URL with the picklist ID
- Waits 3 seconds for network idle
- Makes hidden elements visible
- Captures the page for printing/saving

**Example:**
- Primary API returns: `{"data": [[6627, "Picklist #1", ...], [6628, "Picklist #2", ...]]}`
- For first item: Navigates to `https://mj.3plnext.com/#Outbound/ManualPicking?id=6627`
- For second item: Navigates to `https://mj.3plnext.com/#Outbound/ManualPicking?id=6628`

### Sub-Action #2: Convert HTML to PDF and Save
**Type:** GetHtmlAndPrint  
**Status:** ✅ Enabled  
**Execution Order:** 2

**Configuration:**
```json
{
  "Endpoint": "/api/Picklist/GetPicklistHtml/{id}",
  "Method": "GET",
  "HtmlJsonPath": "html",
  "UseChainedInput": true,
  "ChainedItemFieldPath": "[0]",
  "OutputFilePrefix": "picklist",
  "ContinueOnError": true
}
```

**Purpose:**
- Fetches HTML content for each picklist ID
- Extracts HTML from the `html` field in the response
- Converts HTML to PDF
- Saves with filename prefix "picklist"

**Example:**
- For picklist ID 6627: Calls `GET /api/Picklist/GetPicklistHtml/6627`
- Extracts HTML from response: `{"html": "<html>...</html>"}`
- Converts to PDF and saves as: `picklist-6627.pdf`

## Workflow

```
1. Call Primary API
   ↓
   POST /api/Picklist/GetPicklistDatatable
   Response: {
     "data": [
       [6627, "Picklist #1", ...],
       [6628, "Picklist #2", ...],
       ...
     ],
     "recordsTotal": 15
   }

2. For each picklist in data array:
   
   a. Sub-Action #1: Navigate and Print
      ↓
      Navigate to: https://mj.3plnext.com/#Outbound/ManualPicking?id=6627
      Wait 3 seconds
      Capture page
      Save/Print
   
   b. Sub-Action #2: Convert HTML to PDF
      ↓
      GET /api/Picklist/GetPicklistHtml/6627
      Extract HTML
      Convert to PDF
      Save as: picklist-6627.pdf

3. Continue for next picklist...
```

## Manual Execution

### Dry Run (Preview)
```powershell
cd c:\Users\User\source\repos\BrowserExtensions\scheduled-print-service\ScheduledPrintService
powershell -ExecutionPolicy Bypass -File run-api.ps1 -ApiNumber 2 -DryRun
```

### Execute
```powershell
powershell -ExecutionPolicy Bypass -File run-api.ps1 -ApiNumber 2
```

### With Verbose Output
```powershell
powershell -ExecutionPolicy Bypass -File run-api.ps1 -ApiNumber 2 -VerboseOutput
```

## Database Queries

### View API Configuration
```sql
SELECT * FROM PrimaryApi WHERE ApiNumber = 2;
```

### View Sub-Actions
```sql
SELECT ActionNumber, ActionName, ActionType, IsEnabled
FROM SubAction
WHERE PrimaryApiId = (SELECT Id FROM PrimaryApi WHERE ApiNumber = 2)
ORDER BY ExecutionOrder;
```

### Enable/Disable API
```sql
-- Disable
UPDATE PrimaryApi SET IsEnabled = 0 WHERE ApiNumber = 2;

-- Enable
UPDATE PrimaryApi SET IsEnabled = 1 WHERE ApiNumber = 2;
```

### Enable/Disable Sub-Actions
```sql
-- Disable Sub-Action #1
UPDATE SubAction 
SET IsEnabled = 0 
WHERE PrimaryApiId = (SELECT Id FROM PrimaryApi WHERE ApiNumber = 2)
AND ActionNumber = 1;

-- Enable Sub-Action #2
UPDATE SubAction 
SET IsEnabled = 1 
WHERE PrimaryApiId = (SELECT Id FROM PrimaryApi WHERE ApiNumber = 2)
AND ActionNumber = 2;
```

### Update Bearer Token
```sql
UPDATE PrimaryApi 
SET Headers = json_set(Headers, '$.Authorization', 'Bearer NEW_TOKEN_HERE')
WHERE ApiNumber = 2;
```

### Update Request Parameters
```sql
-- Change status filter
UPDATE PrimaryApi 
SET Params = json_set(Params, '$.statusName', '0,1,2')
WHERE ApiNumber = 2;

-- Change fetch limit
UPDATE PrimaryApi 
SET Params = json_set(Params, '$.length', 50)
WHERE ApiNumber = 2;
```

## Adding to Schedule

### Link API #2 to Existing Schedule
```sql
INSERT INTO ScheduleApi (ScheduleId, ApiNumber, ExecutionOrder)
VALUES (1, 2, 2);
```

### Create New Schedule for API #2
```sql
-- Create schedule
INSERT INTO Schedule (ScheduleName, CronExpression, IsEnabled)
VALUES ('Picklist Processing Schedule', '0 */30 * * * *', 1);

-- Link API #2 to new schedule
INSERT INTO ScheduleApi (ScheduleId, ApiNumber, ExecutionOrder)
SELECT Id, 2, 1
FROM Schedule
WHERE ScheduleName = 'Picklist Processing Schedule';
```

## Expected Response Format

### Primary API Response
```json
{
  "data": [
    [6627, "column2", "column3", ...],
    [6628, "column2", "column3", ...],
    [6629, "column2", "column3", ...]
  ],
  "recordsTotal": 150,
  "recordsFiltered": 15,
  "draw": 1
}
```

The first element `[0]` of each array item is the picklist ID used in subsequent sub-actions.

### Sub-Action #2 Response (HTML)
```json
{
  "html": "<html><head>...</head><body>Picklist content...</body></html>",
  "success": true
}
```

## Troubleshooting

### Token Expired
If you get 401 errors, update the bearer token:
```sql
UPDATE PrimaryApi 
SET Headers = json_replace(Headers, '$.Authorization', 'Bearer NEW_TOKEN')
WHERE ApiNumber = 2;
```

### No Picklists Returned
Check the status filter:
```sql
-- View current status filter
SELECT json_extract(Params, '$.statusName') as StatusFilter
FROM PrimaryApi
WHERE ApiNumber = 2;

-- Update to include more statuses
UPDATE PrimaryApi 
SET Params = json_set(Params, '$.statusName', '0,1,2,3')
WHERE ApiNumber = 2;
```

### Sub-Action Not Executing
Verify sub-action is enabled:
```sql
SELECT ActionNumber, ActionName, IsEnabled
FROM SubAction
WHERE PrimaryApiId = (SELECT Id FROM PrimaryApi WHERE ApiNumber = 2);
```

## Notes

- The bearer token and cookies must be kept in sync
- Token expires after a certain period (check `exp` claim in JWT)
- The `WarehouseId` header must match the user's warehouse permissions
- Status codes in `statusName` parameter:
  - 0: Pending
  - 1: In Progress
  - 2: Completed
  - (check system for full list)

## Related Files

- **Database:** `api_config.db`
- **Script:** `add-picklist-api.ps1` - Used to create this API
- **Executor:** `run-api.ps1` - Executes the API manually
- **Schema:** `api_config.sql` - Database schema definition

## See Also

- `MANUAL-API-EXECUTION.md` - General manual execution guide
- `QUICK-START-API.md` - Quick reference for all APIs
- `DATABASE-SCHEMA-DIAGRAM.md` - Database structure diagrams
