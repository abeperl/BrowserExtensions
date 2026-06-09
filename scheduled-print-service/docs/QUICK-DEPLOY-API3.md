# Quick Deploy: API #3 Fix

## TL;DR

API #3 can't extract IDs from records. This fix updates the database configuration and code.

## Quick Deploy (30 seconds)

### On Server (as Administrator):

```powershell
# Navigate to project directory
cd C:\path\to\ScheduledPrintService

# Option A: Full deployment (recommended)
.\deploy-api3-fix.ps1

# Option B: Database only (faster, if code already deployed)
.\migrate-database-only.ps1
```

### Verify It Worked:

```powershell
# Check service is running
Get-Service ScheduledPrintService

# Check logs - should NOT see "Could not extract ID" warnings
Get-Content C:\ScheduledPrintService\logs\log-$(Get-Date -Format 'yyyyMMdd').txt -Tail 20
```

## What Changed

**Database:** API #3 Configuration now includes `"IdJsonPath": "orderDetailsId"`

**Code:** `DatabaseApiConfigService.cs` now reads `IdJsonPath` from database

## Files

- `deploy-api3-fix.ps1` - Deploy everything
- `migrate-database-only.ps1` - Just database migration
- `fix-api3-id-path.sql` - The SQL migration
- `DEPLOYMENT-API3-FIX.md` - Full deployment guide
- `API-CONFIG-IDJSONPATH.md` - Technical documentation

## Rollback

Backups are automatic. To rollback:

```powershell
Stop-Service ScheduledPrintService
Copy-Item C:\ScheduledPrintService_Backup\* C:\ScheduledPrintService\ -Recurse -Force
Start-Service ScheduledPrintService
```

## Done! ✅
