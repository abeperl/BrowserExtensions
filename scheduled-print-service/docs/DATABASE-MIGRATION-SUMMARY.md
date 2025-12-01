# Database Migration Complete ✅

The API configuration has been successfully migrated from `appsettings.json` to SQLite database (`api_config.db`).

## What Changed

### Before
- API configuration stored in `appsettings.json`
- Single API endpoint hardcoded
- Sub-actions defined in JSON configuration file
- Required code changes to modify configuration

### After
- ✅ All API configurations stored in SQLite database
- ✅ Support for multiple primary APIs (each with unique ApiNumber)
- ✅ Each API can have multiple sub-actions
- ✅ Schedules can trigger multiple APIs
- ✅ Easy configuration management via SQL queries
- ✅ No code changes needed to modify settings

## Database Schema

### Tables Created

1. **PrimaryApi** - Main API endpoints
   - Stores URL, method, headers, parameters, payload
   - Each API has unique `ApiNumber`

2. **SubAction** - Actions to perform with API data
   - Linked to Primary API
   - Stores configuration as JSON
   - Supports execution ordering

3. **Schedule** - Cron-based schedules
   - Defines when APIs should be called
   - Uses standard cron expressions

4. **ScheduleApi** - Links schedules to APIs
   - Many-to-many relationship
   - One schedule can trigger multiple APIs
   - Supports execution ordering

## Current Configuration (Migrated)

### API #1: Orders List API
- **Endpoint:** `POST https://mj.3plnext.com/api/order/GetOrdersList`
- **Status:** ✅ Enabled
- **Headers:** Authorization (Bearer token), Cookie
- **Parameters:** Status filter, date range, warehouse ID
- **Sub-Actions:** 5 total (1 enabled)

#### Sub-Actions
1. ✅ **Create Pending Order Picklist Batch** (Enabled)
   - Type: CreatePicklistBatch
   - Batch Size: 25 orders
   - Creates picklists from order IDs

2. ❌ **Print Manual Picking Page** (Disabled)
   - Type: GetUrlAndPrint
   - Uses chained input from previous action

3. ❌ **Update Order Status** (Disabled)
   - Type: CallApi
   - Updates order status after processing

4. ❌ **Wait before fetching label** (Disabled)
   - Type: Delay
   - 1000ms delay

5. ❌ **Print Shipping Label** (Disabled)
   - Type: GetHtmlAndPrint
   - Extracts HTML from response

### Schedule #1: Default Order Processing Schedule
- **Cron:** `0 */15 * * * *` (Every 15 minutes)
- **Status:** ❌ Disabled
- **Linked APIs:** API #1

## Files Created

| File | Purpose |
|------|---------|
| `api_config.sql` | Database schema definition |
| `api_config.db` | SQLite database file |
| `create-database.ps1` | Creates database from schema |
| `migrate-config-to-db.ps1` | Migrates config from appsettings.json |
| `run-api.ps1` | Executes API by number manually |
| `MANUAL-API-EXECUTION.md` | Detailed documentation |
| `QUICK-START-API.md` | Quick reference guide |
| `DATABASE-MIGRATION-SUMMARY.md` | This file |

## Quick Start

### Run API #1 Manually

```powershell
# Dry run (preview only)
powershell -ExecutionPolicy Bypass -File run-api.ps1 -ApiNumber 1 -DryRun

# Execute
powershell -ExecutionPolicy Bypass -File run-api.ps1 -ApiNumber 1

# With verbose output
powershell -ExecutionPolicy Bypass -File run-api.ps1 -ApiNumber 1 -VerboseOutput
```

### View Configuration

```powershell
# Open database
sqlite3 api_config.db

# View all APIs
SELECT ApiNumber, ApiName, IsEnabled FROM PrimaryApi;

# View sub-actions for API 1
SELECT ActionNumber, ActionName, ActionType, IsEnabled 
FROM SubAction 
WHERE PrimaryApiId = 1 
ORDER BY ExecutionOrder;

# View schedules
SELECT ScheduleName, CronExpression, IsEnabled FROM Schedule;
```

### Modify Configuration

```sql
-- Enable/disable API
UPDATE PrimaryApi SET IsEnabled = 1 WHERE ApiNumber = 1;

-- Enable/disable sub-action
UPDATE SubAction SET IsEnabled = 1 WHERE PrimaryApiId = 1 AND ActionNumber = 2;

-- Enable schedule
UPDATE Schedule SET IsEnabled = 1 WHERE Id = 1;

-- Update bearer token
UPDATE PrimaryApi 
SET Headers = json_replace(Headers, '$.Authorization', 'Bearer NEW_TOKEN')
WHERE ApiNumber = 1;
```

## Adding New APIs

### Add API #2

```sql
INSERT INTO PrimaryApi (
    ApiNumber, ApiName, BaseUrl, Endpoint, HttpMethod, 
    Headers, Params, IsEnabled
) VALUES (
    2,
    'Inventory Sync API',
    'https://api.example.com',
    '/v1/inventory',
    'GET',
    '{"Authorization":"Bearer token","Content-Type":"application/json"}',
    '{}',
    1
);

-- Add sub-action
INSERT INTO SubAction (
    PrimaryApiId, ActionNumber, ActionName, ActionType,
    Configuration, ExecutionOrder, IsEnabled
) VALUES (
    (SELECT Id FROM PrimaryApi WHERE ApiNumber = 2),
    1,
    'Process Inventory',
    'CallApi',
    '{"Endpoint":"/v1/process","Method":"POST"}',
    1,
    1
);

-- Link to schedule
INSERT INTO ScheduleApi (ScheduleId, ApiNumber, ExecutionOrder)
VALUES (1, 2, 2);
```

## Benefits

### ✅ Flexibility
- Add/remove APIs without code changes
- Enable/disable features via SQL
- Test different configurations quickly

### ✅ Scalability
- Support unlimited APIs
- Each API can have unlimited sub-actions
- Multiple schedules can trigger same API

### ✅ Maintainability
- Configuration separate from code
- Easy to backup/restore (just copy .db file)
- Version control friendly (schema in .sql file)

### ✅ Debugging
- Query execution history (future feature)
- View exact configuration used
- Test APIs independently

## Next Steps

### Immediate
1. ✅ Database created and populated
2. ✅ Manual execution script working
3. ✅ Documentation complete

### Future Enhancements
1. ⏳ Update service code to read from database
2. ⏳ Implement sub-action execution logic
3. ⏳ Add execution logging table
4. ⏳ Create web UI for configuration management
5. ⏳ Add API health checks
6. ⏳ Support for API response validation

## Migration Verification

Run these commands to verify migration:

```powershell
# Count records
sqlite3 api_config.db "SELECT COUNT(*) as PrimaryApis FROM PrimaryApi;"
sqlite3 api_config.db "SELECT COUNT(*) as SubActions FROM SubAction;"
sqlite3 api_config.db "SELECT COUNT(*) as Schedules FROM Schedule;"

# Expected output:
# PrimaryApis: 1
# SubActions: 5
# Schedules: 1

# Test execution (dry run)
powershell -ExecutionPolicy Bypass -File run-api.ps1 -ApiNumber 1 -DryRun

# Expected: Shows API configuration and 1 enabled sub-action
```

## Troubleshooting

### Database locked
```powershell
# Close all sqlite3 processes
Get-Process | Where-Object {$_.Name -like "*sqlite*"} | Stop-Process -Force
```

### Token expired
```sql
UPDATE PrimaryApi 
SET Headers = json_replace(Headers, '$.Authorization', 'Bearer NEW_TOKEN')
WHERE ApiNumber = 1;
```

### Reset database
```powershell
# Backup first!
Copy-Item api_config.db api_config_backup.db

# Delete and recreate
Remove-Item api_config.db
powershell -ExecutionPolicy Bypass -File create-database.ps1
powershell -ExecutionPolicy Bypass -File migrate-config-to-db.ps1
```

## Support

For detailed information, see:
- `MANUAL-API-EXECUTION.md` - Full documentation
- `QUICK-START-API.md` - Quick reference
- `api_config.sql` - Database schema

## Summary

✅ **Migration successful!**  
✅ **API #1 configured and ready to run**  
✅ **Documentation complete**  
✅ **Manual execution script working**

You can now run API #1 manually using:
```powershell
powershell -ExecutionPolicy Bypass -File run-api.ps1 -ApiNumber 1
```
