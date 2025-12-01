# Manual API Execution Guide

This guide explains how to manually execute API calls from the database using API numbers.

## Overview

The scheduled print service now stores all API configurations in a SQLite database (`api_config.db`). Each primary API has a unique `ApiNumber` and can have multiple sub-actions. This allows for flexible configuration and execution without modifying code.

## Database Structure

### Tables

1. **PrimaryApi** - Main API endpoints with configuration
   - `ApiNumber` - Unique identifier for the API (e.g., 1, 2, 3)
   - `ApiName` - Descriptive name
   - `BaseUrl` - Base URL for the API
   - `Endpoint` - API endpoint path
   - `HttpMethod` - GET, POST, PUT, DELETE
   - `Headers` - JSON string of headers (including Authorization, Cookie)
   - `Params` - JSON string of query parameters or request body
   - `Payload` - JSON string for request payload
   - `IsEnabled` - 1 (enabled) or 0 (disabled)

2. **SubAction** - Actions to perform with API response data
   - `PrimaryApiId` - Links to PrimaryApi
   - `ActionNumber` - Unique number within the API
   - `ActionName` - Descriptive name
   - `ActionType` - Type of action (CallApi, GetHtmlAndPrint, Delay, CreatePicklistBatch, GetUrlAndPrint)
   - `Configuration` - JSON string with action-specific settings
   - `ExecutionOrder` - Order of execution
   - `IsEnabled` - 1 (enabled) or 0 (disabled)

3. **Schedule** - Cron-based schedules
   - `ScheduleName` - Descriptive name
   - `CronExpression` - Cron expression (e.g., `0 */15 * * * *`)
   - `IsEnabled` - 1 (enabled) or 0 (disabled)

4. **ScheduleApi** - Links schedules to APIs
   - `ScheduleId` - Links to Schedule
   - `ApiNumber` - Links to PrimaryApi by ApiNumber
   - `ExecutionOrder` - Order when multiple APIs in schedule

## Current Configuration (API Number 1)

### Primary API: Orders List API

**API Number:** 1  
**Endpoint:** `POST https://mj.3plnext.com/api/order/GetOrdersList`  
**Purpose:** Fetch orders from the 3PL system with specific status filters

**Headers:**
- Authorization: Bearer token (from database)
- Content-Type: application/json
- Cookie: Session cookies (userData, token, isRefreshedToken)

**Request Parameters:**
```json
{
  "Draw": 1,
  "Start": 0,
  "Length": 100,
  "ClientID": "0",
  "StatusName": "7",
  "ChannelId": 0,
  "PaymentMethod": 0,
  "DateFrom": "2018-01-07",
  "DateTo": null,
  ...
}
```

### Sub-Actions (5 total)

1. **Create Pending Order Picklist Batch** (Enabled)
   - Type: CreatePicklistBatch
   - Endpoint: `/api/PickList/CreatePendingOrderPicklist`
   - Batch Size: 25 orders
   - Creates picklists from order IDs

2. **Print Manual Picking Page** (Disabled)
   - Type: GetUrlAndPrint
   - URL: `https://mj.3plnext.com/#Outbound/ManualPicking?id={pickListId}`
   - Uses chained input from previous action

3. **Update Order Status** (Disabled)
   - Type: CallApi
   - Endpoint: `/api/order/UpdateStatus`
   - Updates order status after processing

4. **Wait before fetching label** (Disabled)
   - Type: Delay
   - Delay: 1000ms

5. **Print Shipping Label** (Disabled)
   - Type: GetHtmlAndPrint
   - Endpoint: `/api/order/GetShippingLabel/{id}`
   - Extracts HTML from response and prints

## Manual Execution Methods

### Method 1: Using PowerShell Script (Recommended)

Create a script to execute API Number 1:

```powershell
# run-api.ps1
param(
    [Parameter(Mandatory=$true)]
    [int]$ApiNumber,
    
    [switch]$DryRun
)

$dbPath = ".\api_config.db"

# Query the database for API configuration
$apiQuery = "SELECT * FROM PrimaryApi WHERE ApiNumber = $ApiNumber AND IsEnabled = 1;"
$api = sqlite3 $dbPath $apiQuery -json | ConvertFrom-Json

if (-not $api) {
    Write-Host "ERROR: API Number $ApiNumber not found or disabled" -ForegroundColor Red
    exit 1
}

Write-Host "Executing API #$ApiNumber : $($api.ApiName)" -ForegroundColor Cyan
Write-Host "Endpoint: $($api.HttpMethod) $($api.BaseUrl)$($api.Endpoint)" -ForegroundColor White

if ($DryRun) {
    Write-Host "DRY RUN MODE - Not executing" -ForegroundColor Yellow
    exit 0
}

# Parse headers and params from JSON
$headers = $api.Headers | ConvertFrom-Json
$params = $api.Params | ConvertFrom-Json

# Execute the API call
try {
    $response = Invoke-RestMethod `
        -Uri "$($api.BaseUrl)$($api.Endpoint)" `
        -Method $api.HttpMethod `
        -Headers $headers `
        -Body ($params | ConvertTo-Json -Depth 10) `
        -ContentType "application/json"
    
    Write-Host "✓ API call successful" -ForegroundColor Green
    
    # Get enabled sub-actions
    $subActionsQuery = "SELECT * FROM SubAction WHERE PrimaryApiId = $($api.Id) AND IsEnabled = 1 ORDER BY ExecutionOrder;"
    $subActions = sqlite3 $dbPath $subActionsQuery -json | ConvertFrom-Json
    
    Write-Host "Executing $($subActions.Count) sub-actions..." -ForegroundColor Cyan
    
    # TODO: Implement sub-action execution logic
    # This would parse the Configuration JSON and execute each action
    
} catch {
    Write-Host "✗ API call failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
```

**Usage:**
```powershell
# Execute API Number 1
powershell -ExecutionPolicy Bypass -File run-api.ps1 -ApiNumber 1

# Dry run (show config without executing)
powershell -ExecutionPolicy Bypass -File run-api.ps1 -ApiNumber 1 -DryRun
```

### Method 2: Using SQLite Command Line

View the API configuration:

```bash
# Connect to database
sqlite3 api_config.db

# View API Number 1 details
SELECT ApiNumber, ApiName, BaseUrl, Endpoint, HttpMethod, IsEnabled 
FROM PrimaryApi 
WHERE ApiNumber = 1;

# View sub-actions for API Number 1
SELECT 
    sa.ActionNumber,
    sa.ActionName,
    sa.ActionType,
    sa.ExecutionOrder,
    sa.IsEnabled
FROM SubAction sa
JOIN PrimaryApi pa ON sa.PrimaryApiId = pa.Id
WHERE pa.ApiNumber = 1
ORDER BY sa.ExecutionOrder;

# View schedule linking
SELECT 
    s.ScheduleName,
    s.CronExpression,
    s.IsEnabled,
    sa.ApiNumber,
    sa.ExecutionOrder
FROM ScheduleApi sa
JOIN Schedule s ON sa.ScheduleId = s.Id
WHERE sa.ApiNumber = 1;
```

### Method 3: Direct Service Configuration

To run the service in manual mode for API Number 1:

1. Modify the service code to read from database
2. Set `ManualMode = true` in service configuration
3. Specify API number to execute
4. Run the service once:

```powershell
cd c:\Users\User\source\repos\BrowserExtensions\scheduled-print-service\ScheduledPrintService\bin\Debug\net8.0-windows10.0.19041.0

# Run with API Number 1
.\ScheduledPrintService.exe --manual --api-number 1
```

## Querying the Database

### View All APIs
```sql
SELECT ApiNumber, ApiName, BaseUrl, Endpoint, IsEnabled 
FROM PrimaryApi;
```

### View API Details
```sql
SELECT * FROM PrimaryApi WHERE ApiNumber = 1;
```

### View Sub-Actions
```sql
SELECT 
    sa.ActionNumber,
    sa.ActionName,
    sa.ActionType,
    sa.IsEnabled,
    LENGTH(sa.Configuration) as ConfigSize
FROM SubAction sa
JOIN PrimaryApi pa ON sa.PrimaryApiId = pa.Id
WHERE pa.ApiNumber = 1
ORDER BY sa.ExecutionOrder;
```

### View Sub-Action Configuration
```sql
SELECT Configuration 
FROM SubAction 
WHERE Id = 1;
```

### View Schedules
```sql
SELECT * FROM Schedule;
```

### View Schedule-API Links
```sql
SELECT 
    s.ScheduleName,
    pa.ApiNumber,
    pa.ApiName,
    sa.ExecutionOrder
FROM ScheduleApi sa
JOIN Schedule s ON sa.ScheduleId = s.Id
JOIN PrimaryApi pa ON sa.ApiNumber = pa.ApiNumber
ORDER BY s.Id, sa.ExecutionOrder;
```

## Modifying Configuration

### Enable/Disable API
```sql
-- Disable API Number 1
UPDATE PrimaryApi SET IsEnabled = 0 WHERE ApiNumber = 1;

-- Enable API Number 1
UPDATE PrimaryApi SET IsEnabled = 1 WHERE ApiNumber = 1;
```

### Enable/Disable Sub-Action
```sql
-- Enable sub-action 2 (Print Manual Picking Page)
UPDATE SubAction 
SET IsEnabled = 1 
WHERE PrimaryApiId = 1 AND ActionNumber = 2;

-- Disable sub-action 1
UPDATE SubAction 
SET IsEnabled = 0 
WHERE PrimaryApiId = 1 AND ActionNumber = 1;
```

### Update Headers (e.g., refresh bearer token)
```sql
UPDATE PrimaryApi 
SET Headers = json_set(Headers, '$.Authorization', 'Bearer NEW_TOKEN_HERE')
WHERE ApiNumber = 1;
```

### Update Request Parameters
```sql
-- Change StatusName filter
UPDATE PrimaryApi 
SET Params = json_set(Params, '$.StatusName', '7,8,9')
WHERE ApiNumber = 1;
```

## Adding New APIs

### Add API Number 2
```sql
INSERT INTO PrimaryApi (
    ApiNumber, 
    ApiName, 
    BaseUrl, 
    Endpoint, 
    HttpMethod, 
    Headers, 
    Params, 
    IsEnabled
) VALUES (
    2,
    'Inventory Sync API',
    'https://api.example.com',
    '/v1/inventory/sync',
    'GET',
    '{"Authorization":"Bearer token","Content-Type":"application/json"}',
    '{"warehouseId":1}',
    1
);

-- Add sub-action for API 2
INSERT INTO SubAction (
    PrimaryApiId,
    ActionNumber,
    ActionName,
    ActionType,
    Configuration,
    ExecutionOrder,
    IsEnabled
) VALUES (
    (SELECT Id FROM PrimaryApi WHERE ApiNumber = 2),
    1,
    'Process Inventory Data',
    'CallApi',
    '{"Endpoint":"/v1/inventory/update","Method":"POST"}',
    1,
    1
);
```

### Link API to Schedule
```sql
-- Add API Number 2 to existing schedule
INSERT INTO ScheduleApi (ScheduleId, ApiNumber, ExecutionOrder)
SELECT Id, 2, 2
FROM Schedule 
WHERE ScheduleName = 'Default Order Processing Schedule';
```

## Testing

### Verify Database Integrity
```sql
-- Check for orphaned sub-actions
SELECT * FROM SubAction 
WHERE PrimaryApiId NOT IN (SELECT Id FROM PrimaryApi);

-- Check for invalid schedule links
SELECT * FROM ScheduleApi 
WHERE ApiNumber NOT IN (SELECT ApiNumber FROM PrimaryApi);
```

### Test Query Performance
```sql
-- Get all APIs for a schedule (this is what the service runs)
SELECT 
    pa.*
FROM ScheduleApi sa
JOIN PrimaryApi pa ON sa.ApiNumber = pa.ApiNumber
WHERE sa.ScheduleId = 1 
  AND pa.IsEnabled = 1
ORDER BY sa.ExecutionOrder;
```

## Backup and Restore

### Backup Database
```powershell
# Backup with timestamp
$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
Copy-Item api_config.db "api_config_backup_$timestamp.db"
```

### Export to SQL
```bash
sqlite3 api_config.db .dump > api_config_backup.sql
```

### Restore from SQL
```bash
sqlite3 api_config_new.db < api_config_backup.sql
```

## Troubleshooting

### Database Locked
If you get "database is locked" errors:
```powershell
# Check for open connections
Get-Process | Where-Object {$_.Name -like "*sqlite*"}

# Kill if necessary
Stop-Process -Name sqlite3 -Force
```

### Token Expired
If API calls fail with 401:
```sql
-- Update bearer token
UPDATE PrimaryApi 
SET Headers = json_replace(Headers, '$.Authorization', 'Bearer NEW_TOKEN')
WHERE ApiNumber = 1;
```

### View API Call History
(Future enhancement - add execution log table)
```sql
CREATE TABLE ExecutionLog (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    ApiNumber INTEGER,
    ExecutedAt TEXT DEFAULT CURRENT_TIMESTAMP,
    Success INTEGER,
    ErrorMessage TEXT,
    ResponseTime INTEGER
);
```

## Next Steps

1. Implement PowerShell execution script (`run-api.ps1`)
2. Update service code to read from database instead of appsettings.json
3. Add command-line arguments for manual execution
4. Create execution log table for audit trail
5. Build admin UI for managing configurations
