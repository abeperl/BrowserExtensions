# Deployment Notes - Token Caching Fix

**Date:** 2025-12-02
**Version:** Token Caching Optimization

## Changes in This Release

### Fixed: Token Renewal for Every API Call

**Problem:** Service was renewing authentication token for every API call, even when executing multiple APIs in the same schedule run.

**Solution:**
- Modified token loading to use cached tokens from ApiAuth table
- Added JWT expiration parsing to store accurate token expiration time
- Tokens now cached and reused for their full 5-hour lifetime

**Impact:**
- 67% reduction in token renewal overhead
- Only 1 login per schedule run instead of 3
- Better performance and reduced server load

See `scripts/TOKEN-CACHING-FIX.md` for detailed technical documentation.

## Deployment Steps

### Option 1: Manual Deployment (Recommended)

1. **Stop the Windows Service**
   ```powershell
   Stop-Service -Name "ScheduledPrintService"
   ```

2. **Backup Current Installation**
   ```powershell
   Copy-Item "C:\ScheduledPrintService" "C:\ScheduledPrintService_backup_$(Get-Date -Format 'yyyyMMdd_HHmmss')" -Recurse
   ```

3. **Copy New Files**
   ```powershell
   Copy-Item "C:\Users\User\source\repos\BrowserExtensions\scheduled-print-service\publish\*" `
       -Destination "C:\ScheduledPrintService\" -Recurse -Force
   ```

4. **Start the Service**
   ```powershell
   Start-Service -Name "ScheduledPrintService"
   ```

5. **Verify Service Started**
   ```powershell
   Get-Service -Name "ScheduledPrintService"
   Get-Content "C:\ProgramData\ScheduledPrintService\logs\log.txt" -Tail 20
   ```

### Option 2: Using PowerShell Script

Run as Administrator:

```powershell
# Stop service
Stop-Service -Name "ScheduledPrintService" -ErrorAction SilentlyContinue

# Backup
$backupPath = "C:\ScheduledPrintService_backup_$(Get-Date -Format 'yyyyMMdd_HHmmss')"
Copy-Item "C:\ScheduledPrintService" $backupPath -Recurse -Force
Write-Host "Backup created at: $backupPath"

# Deploy
Copy-Item "C:\Users\User\source\repos\BrowserExtensions\scheduled-print-service\publish\*" `
    -Destination "C:\ScheduledPrintService\" -Recurse -Force

# Start service
Start-Service -Name "ScheduledPrintService"

# Verify
Start-Sleep -Seconds 3
$service = Get-Service -Name "ScheduledPrintService"
Write-Host "Service Status: $($service.Status)"

if ($service.Status -eq "Running") {
    Write-Host "Deployment successful!" -ForegroundColor Green
    Get-Content "C:\ProgramData\ScheduledPrintService\logs\log.txt" -Tail 10
} else {
    Write-Host "Service failed to start. Check logs." -ForegroundColor Red
}
```

## Verification Steps

After deployment, verify the fix is working:

1. **Check Service Status**
   ```powershell
   Get-Service -Name "ScheduledPrintService"
   ```

2. **Monitor First Schedule Run**
   ```powershell
   Get-Content "C:\ProgramData\ScheduledPrintService\logs\log.txt" -Wait
   ```

3. **Look for Token Caching in Logs**

   You should see:
   ```
   [INF] Loading API configuration for ApiNumber=1 from database
   [DBG] Using cached token from ApiAuth table, expires at 2025-12-02T17:00:00
   ```

   Or on first run after token expiration:
   ```
   [INF] Attempting to renew authentication token
   [DBG] Token expires at 2025-12-02T17:00:00 (from JWT exp claim)
   [INF] Loading API configuration for ApiNumber=2 from database
   [DBG] Using cached token from ApiAuth table, expires at 2025-12-02T17:00:00
   ```

4. **Verify Database Token Cache**
   ```powershell
   sqlite3 "C:\ScheduledPrintService\api_config.db" `
       "SELECT BaseUrl, TokenExpiresAt FROM ApiAuth WHERE BaseUrl = 'https://mj.3plnext.com';"
   ```

## Expected Behavior Changes

### Before This Fix
- Every API call received 401 Unauthorized
- Token renewed 3 times per schedule run (once per API)
- ~900ms spent on token renewal per run

### After This Fix
- First API call may receive 401 if token expired
- Token renewed once and cached for 5 hours
- Subsequent APIs reuse cached token
- ~300ms spent on token renewal per run (only when needed)

## Rollback Instructions

If issues occur, rollback to previous version:

1. **Stop Service**
   ```powershell
   Stop-Service -Name "ScheduledPrintService"
   ```

2. **Restore Backup**
   ```powershell
   # Find backup directory
   Get-ChildItem "C:\" -Filter "ScheduledPrintService_backup_*" | Sort-Object Name -Descending | Select-Object -First 1

   # Restore from backup (replace timestamp)
   Copy-Item "C:\ScheduledPrintService_backup_YYYYMMDD_HHMMSS\*" `
       -Destination "C:\ScheduledPrintService\" -Recurse -Force
   ```

3. **Start Service**
   ```powershell
   Start-Service -Name "ScheduledPrintService"
   ```

## Files Modified

- `Services/DatabaseApiConfigService.cs` - Token caching logic
- `Services/TokenRenewalService.cs` - JWT expiration parsing
- All other files unchanged

## Database Changes

No database schema changes required. Uses existing `ApiAuth` table.

## Configuration Changes

No configuration file changes required.

## Testing Recommendations

1. Run service and monitor first schedule execution
2. Verify only 1 token renewal occurs for multiple APIs
3. Check logs for "Using cached token" messages
4. Monitor for any authentication failures
5. Verify token expiration time is accurate (~5 hours from renewal)

## Support

If issues occur:
1. Check service logs: `C:\ProgramData\ScheduledPrintService\logs\log.txt`
2. Verify database: `C:\ScheduledPrintService\api_config.db`
3. Check service status: `Get-Service -Name "ScheduledPrintService"`
4. Review: `scripts/TOKEN-CACHING-FIX.md` for technical details

## Performance Monitoring

Monitor these metrics after deployment:
- Token renewal frequency (should be once per ~5 hours)
- API execution time (should be faster)
- Number of login requests (should be reduced by 67%)
- Service logs for any authentication errors
