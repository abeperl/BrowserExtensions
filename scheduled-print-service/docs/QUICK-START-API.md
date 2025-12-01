# Quick Start Guide - Manual API Execution

## Prerequisites

1. SQLite3 installed and in PATH
2. Database created: `api_config.db`
3. Configuration migrated from appsettings.json

## Setup (One-time)

```powershell
cd c:\Users\User\source\repos\BrowserExtensions\scheduled-print-service\ScheduledPrintService

# 1. Create database
powershell -ExecutionPolicy Bypass -File create-database.ps1

# 2. Migrate configuration
powershell -ExecutionPolicy Bypass -File migrate-config-to-db.ps1
```

## Run API Number 1

### Dry Run (Preview Only)
```powershell
powershell -ExecutionPolicy Bypass -File run-api.ps1 -ApiNumber 1 -DryRun
```

### Execute API Call
```powershell
powershell -ExecutionPolicy Bypass -File run-api.ps1 -ApiNumber 1
```

### Execute with Verbose Output
```powershell
powershell -ExecutionPolicy Bypass -File run-api.ps1 -ApiNumber 1 -VerboseOutput
```

## Quick Database Queries

### View API Configuration
```powershell
sqlite3 api_config.db "SELECT ApiNumber, ApiName, BaseUrl, Endpoint FROM PrimaryApi;"
```

### View Sub-Actions
```powershell
sqlite3 api_config.db "SELECT sa.ActionNumber, sa.ActionName, sa.ActionType, sa.IsEnabled FROM SubAction sa JOIN PrimaryApi pa ON sa.PrimaryApiId = pa.Id WHERE pa.ApiNumber = 1;"
```

### Enable/Disable API
```powershell
# Disable API 1
sqlite3 api_config.db "UPDATE PrimaryApi SET IsEnabled = 0 WHERE ApiNumber = 1;"

# Enable API 1
sqlite3 api_config.db "UPDATE PrimaryApi SET IsEnabled = 1 WHERE ApiNumber = 1;"
```

### Enable/Disable Sub-Action
```powershell
# Enable sub-action 2 for API 1
sqlite3 api_config.db "UPDATE SubAction SET IsEnabled = 1 WHERE PrimaryApiId = 1 AND ActionNumber = 2;"

# Disable sub-action 1 for API 1
sqlite3 api_config.db "UPDATE SubAction SET IsEnabled = 0 WHERE PrimaryApiId = 1 AND ActionNumber = 1;"
```

## Current Configuration (API #1)

**Primary API:** Orders List API
- Fetches orders from 3PL system with status filter
- Endpoint: `POST https://mj.3plnext.com/api/order/GetOrdersList`

**Sub-Actions:**
1. ✅ Create Pending Order Picklist Batch (Enabled)
2. ❌ Print Manual Picking Page (Disabled)
3. ❌ Update Order Status (Disabled)
4. ❌ Wait before fetching label (Disabled)
5. ❌ Print Shipping Label (Disabled)

## Troubleshooting

### "sqlite3 not found"
Install SQLite3 from: https://www.sqlite.org/download.html

### "API Number X not found"
Check available APIs:
```powershell
sqlite3 api_config.db "SELECT ApiNumber, ApiName, IsEnabled FROM PrimaryApi;"
```

### "Token expired" (401 errors)
Update bearer token in database:
```powershell
sqlite3 api_config.db "UPDATE PrimaryApi SET Headers = json_replace(Headers, '$.Authorization', 'Bearer NEW_TOKEN') WHERE ApiNumber = 1;"
```

## Files Reference

- `api_config.db` - SQLite database with all configurations
- `api_config.sql` - Database schema
- `create-database.ps1` - Creates the database
- `migrate-config-to-db.ps1` - Migrates config from appsettings.json
- `run-api.ps1` - Executes API by number
- `MANUAL-API-EXECUTION.md` - Full documentation

## Next Steps

For detailed documentation, see: `MANUAL-API-EXECUTION.md`
