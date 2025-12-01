# Deployment Guide: API #3 IdJsonPath Fix

## Overview

This deployment fixes the issue where API #3 (Personalized Orders API) fails to extract IDs from response records, resulting in warnings:
```
[WRN] Could not extract ID from record using path: [0]
```

## What's Included

### Changes
1. **Database Migration** (`fix-api3-id-path.sql`)
   - Updates API #3 Configuration to set `IdJsonPath` to `"orderDetailsId"`
   - Changes from default `[0]` (array index) to object property access

2. **Code Enhancement** (`Services/DatabaseApiConfigService.cs`)
   - Adds parsing logic to load `IdJsonPath` from database Configuration column
   - Lines 162-170

3. **Documentation** (`API-CONFIG-IDJSONPATH.md`)
   - Comprehensive guide for configuring IdJsonPath for other APIs

### Deployment Scripts
- `deploy-api3-fix.ps1` - Full deployment (database + code)
- `migrate-database-only.ps1` - Database migration only (faster, if code already deployed)

## Deployment Options

### Option 1: Full Deployment (Recommended for Production)

This deploys both database changes AND new code. Use when deploying to a fresh server or ensuring everything is up-to-date.

```powershell
# Run as Administrator
cd C:\Users\User\source\repos\BrowserExtensions\scheduled-print-service\ScheduledPrintService

.\deploy-api3-fix.ps1
```

**What it does:**
1. Stops the ScheduledPrintService
2. Creates full backup of existing deployment
3. Applies database migration with backup
4. Builds and publishes new code
5. Restarts the service

**Parameters:**
```powershell
# Custom service name
.\deploy-api3-fix.ps1 -ServiceName "MyServiceName"

# Custom publish location
.\deploy-api3-fix.ps1 -PublishPath "D:\Services\ScheduledPrintService"

# Skip backup (not recommended)
.\deploy-api3-fix.ps1 -SkipBackup

# Custom backup location
.\deploy-api3-fix.ps1 -BackupPath "D:\Backups\ScheduledPrintService"
```

### Option 2: Database Migration Only (Faster)

If the code with the fix is already deployed (or you're testing just the database change):

```powershell
# Run as Administrator
cd C:\Users\User\source\repos\BrowserExtensions\scheduled-print-service\ScheduledPrintService

.\migrate-database-only.ps1
```

**What it does:**
1. Stops the service
2. Creates database backup
3. Applies migration
4. Restarts the service

**Parameters:**
```powershell
# Custom database path
.\migrate-database-only.ps1 -DbPath "D:\Data\api_config.db"

# Don't restart service after migration
.\migrate-database-only.ps1 -NoRestart

# Custom service name
.\migrate-database-only.ps1 -ServiceName "MyServiceName"
```

### Option 3: Manual Deployment

If you prefer manual control or automated scripts don't work:

#### Step 1: Stop the Service
```powershell
Stop-Service -Name ScheduledPrintService -Force
```

#### Step 2: Backup Database
```powershell
Copy-Item "C:\ScheduledPrintService\api_config.db" -Destination "C:\ScheduledPrintService\api_config.db.backup" -Force
```

#### Step 3: Apply Migration
```powershell
Get-Content "fix-api3-id-path.sql" | sqlite3 "C:\ScheduledPrintService\api_config.db"
```

#### Step 4: Build and Publish (if deploying code)
```powershell
dotnet clean --configuration Release
dotnet build --configuration Release
dotnet publish --configuration Release --output "C:\ScheduledPrintService"
```

#### Step 5: Start Service
```powershell
Start-Service -Name ScheduledPrintService
```

## Pre-Deployment Checklist

- [ ] Running as Administrator
- [ ] Service exists and is accessible
- [ ] sqlite3.exe is in PATH or installed
- [ ] Sufficient disk space for backups
- [ ] .NET 8.0 SDK installed (for code deployment)
- [ ] No other processes accessing the database

## Post-Deployment Verification

### 1. Check Service Status
```powershell
Get-Service -Name ScheduledPrintService
```

Expected: `Status: Running`

### 2. Verify Database Configuration
```powershell
sqlite3 "C:\ScheduledPrintService\api_config.db" "SELECT ApiNumber, json_extract(Configuration, '$.IdJsonPath') as IdJsonPath FROM PrimaryApi WHERE ApiNumber = 3"
```

Expected output:
```
3|orderDetailsId
```

### 3. Monitor Logs
```powershell
# View live logs
Get-Content "C:\ScheduledPrintService\logs\log-$(Get-Date -Format 'yyyyMMdd').txt" -Wait -Tail 50

# Or check for specific warnings
Get-Content "C:\ScheduledPrintService\logs\log-*.txt" | Select-String "Could not extract ID"
```

**Success indicators:**
- ✅ No warnings: `Could not extract ID from record using path: [0]`
- ✅ Logs show: `Loaded API configuration: 1 sub-actions (1 enabled)`
- ✅ Records are processed without ID extraction errors

### 4. Check Processed Orders
```powershell
sqlite3 "C:\ScheduledPrintService\api_config.db" "SELECT COUNT(*) FROM ProcessedOrders WHERE ApiNumber = 3"
```

If the count increases over time, IDs are being tracked successfully.

## Rollback Procedure

### If Migration Fails
The scripts automatically restore the database backup on failure.

### Manual Rollback

#### Database Only
```powershell
# Find backup (timestamped)
Get-ChildItem "C:\ScheduledPrintService\api_config.db.backup-*" | Sort-Object LastWriteTime -Descending | Select-Object -First 1

# Restore (replace with actual backup filename)
Stop-Service -Name ScheduledPrintService -Force
Copy-Item "C:\ScheduledPrintService\api_config.db.backup-20251128-123456" -Destination "C:\ScheduledPrintService\api_config.db" -Force
Start-Service -Name ScheduledPrintService
```

#### Full Deployment Rollback
```powershell
Stop-Service -Name ScheduledPrintService -Force

# Restore full deployment
Remove-Item "C:\ScheduledPrintService" -Recurse -Force
Copy-Item "C:\ScheduledPrintService_Backup" -Destination "C:\ScheduledPrintService" -Recurse -Force

Start-Service -Name ScheduledPrintService
```

## Troubleshooting

### "Service did not start"
**Check Event Log:**
```powershell
Get-EventLog -LogName Application -Source "ScheduledPrintService" -Newest 10
```

**Common causes:**
- Missing dependencies (check bin folder)
- Database file locked by another process
- Configuration file issues

### "sqlite3.exe not found"
**Install SQLite:**
1. Download from: https://www.sqlite.org/download.html
2. Extract `sqlite3.exe` to `C:\Windows\System32`
3. Or add to PATH

### "Permission denied"
- Ensure running as Administrator
- Stop any processes accessing the database
- Check file permissions on deployment folder

### Migration Applied But Still Seeing Warnings
1. Verify service restarted (doesn't reload config while running)
2. Check database was actually updated:
   ```powershell
   sqlite3 "C:\ScheduledPrintService\api_config.db" "SELECT Configuration FROM PrimaryApi WHERE ApiNumber = 3"
   ```
3. Check code changes are deployed (if using Option 2, deploy code separately)

## Additional Notes

### Database Schema
The `Configuration` column stores JSON with various settings. Current structure for API #3:
```json
{
  "IdJsonPath": "orderDetailsId"
}
```

### Code Location
The parsing logic is in `Services/DatabaseApiConfigService.cs`:
- Method: `LoadApiConfig(int apiNumber)`
- Lines: 162-170

### Testing in Development
Test the migration locally before deploying to production:
```powershell
# Copy production database
Copy-Item "\\server\C$\ScheduledPrintService\api_config.db" -Destination ".\api_config_test.db"

# Test migration
Get-Content "fix-api3-id-path.sql" | sqlite3 ".\api_config_test.db"

# Verify result
sqlite3 ".\api_config_test.db" "SELECT * FROM PrimaryApi WHERE ApiNumber = 3"
```

## Support

For issues or questions:
1. Check logs in `C:\ScheduledPrintService\logs`
2. Review `API-CONFIG-IDJSONPATH.md` for configuration details
3. Check git history for this change (commit message will reference this fix)

## Related Documentation

- `API-CONFIG-IDJSONPATH.md` - Detailed configuration guide
- `fix-api3-id-path.sql` - The actual SQL migration
- `Services/DatabaseApiConfigService.cs` - Code implementation
- `Services/OrderApiService.cs` - ID extraction logic
