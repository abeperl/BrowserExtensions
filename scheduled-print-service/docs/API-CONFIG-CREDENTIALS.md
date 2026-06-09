# API Configuration & Credentials Setup

## Problem Solved
When running API #2 (or any API from the database) in manual mode, the service was failing with:
```
[WRN] Cannot renew token: UserEmail or Password not configured
[FTL] Token renewal failed - stopping service
```

## Root Cause
API configuration was migrated to the `api_config.db` database (tables: `PrimaryApi`, `SubAction`), but **login credentials** for token renewal were not included in the database for security reasons. When the service received a 401 Unauthorized response, it tried to renew the token but couldn't find credentials.

## Solution
Login credentials are now loaded from `appsettings.json` as a fallback when using database-driven API config. This keeps sensitive credentials out of the database while still supporting multi-API configurations.

### Configuration Steps

1. **Add credentials to `appsettings.json`:**
   ```json
   {
     "Api": {
       "UserEmail": "your-email@example.com",
       "Password": "your-password-here"
     }
   }
   ```

2. **Run manual mode for any API number:**
   ```powershell
   dotnet run -- --manual --api-number 2
   ```
   OR use the launch configuration: **Debug Scheduled Print Service (API #2 - Picklist)**

3. **The service will:**
   - Load API endpoint, method, headers, params, and payload from the database
   - Load credentials from `appsettings.json`
   - Use the configured credentials for token renewal if 401 Unauthorized is received

## Security Notes
- **Never commit credentials to source control.** Add `appsettings.json` to `.gitignore` or use environment variables/secrets management in production.
- Credentials in `appsettings.json` apply to **all APIs** loaded from the database.
- If different APIs require different credentials, consider:
  - Adding `UserEmail` and `Password` columns to the `PrimaryApi` table
  - Using a separate credentials configuration table keyed by `ApiNumber`

## Database Schema Reference
```sql
-- PrimaryApi stores API endpoint and request configuration
CREATE TABLE PrimaryApi (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    ApiNumber INTEGER UNIQUE NOT NULL,
    ApiName TEXT NOT NULL,
    BaseUrl TEXT NOT NULL,
    Endpoint TEXT NOT NULL,          -- e.g., /api/Picklist/GetPicklistDatatable
    HttpMethod TEXT NOT NULL,        -- e.g., POST, GET
    Headers TEXT NOT NULL,           -- JSON: {"Authorization": "Bearer ...", "Cookie": "...", "WarehouseId": "1"}
    Params TEXT NOT NULL,            -- JSON: request payload parameters
    Payload TEXT,                    -- Optional raw JSON payload (overrides Params if provided)
    IsEnabled INTEGER NOT NULL DEFAULT 1
);

-- SubAction stores chained sub-actions for each API
CREATE TABLE SubAction (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    PrimaryApiId INTEGER NOT NULL,
    ActionNumber INTEGER NOT NULL,
    ActionName TEXT NOT NULL,
    ActionType TEXT NOT NULL,        -- e.g., CreatePicklistBatch, GetUrlAndPrint
    Configuration TEXT NOT NULL,     -- JSON: sub-action-specific settings
    ExecutionOrder INTEGER NOT NULL,
    IsEnabled INTEGER NOT NULL DEFAULT 1,
    FOREIGN KEY (PrimaryApiId) REFERENCES PrimaryApi(Id)
);

-- ProcessedItem tracks which items have been processed for each API
CREATE TABLE ProcessedItem (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    ApiNumber INTEGER NOT NULL,
    ItemId TEXT NOT NULL,
    ProcessedAt TEXT NOT NULL,
    UNIQUE(ApiNumber, ItemId)
);
```

## Code Changes Made
1. Added `PrimaryEndpoint`, `PrimaryHttpMethod`, `PrimaryPayload` to `ApiConfig` model
2. Populated these fields from database in `DatabaseApiConfigService`
3. Modified `OrderApiService.GetOrdersListAsync` to use dynamic endpoint/method instead of hardcoded `/api/order/GetOrdersList`
4. Added credential fallback: `Program.cs` now reads `Api:UserEmail` and `Api:Password` from `appsettings.json` and injects them into the database-loaded config
5. Updated logging to show "Calling primary endpoint {Endpoint} ({Method})" for visibility

## Verification
After adding credentials to `appsettings.json`, you should see:
```
[INF] Loading API configuration for ApiNumber=2 from database
[INF] Loaded API configuration: 2 sub-actions (2 enabled)
[INF] Manual mode enabled with API #2 from database
[INF] Calling primary endpoint /api/Picklist/GetPicklistDatatable (POST)...
```

If you still see "Cannot renew token", verify:
- `appsettings.json` in the **bin output folder** (not just repository root) contains the credentials
- Credentials are valid for the target API server
- The `Api` section is correctly formatted JSON

## Related Files
- `Models/ApiConfig.cs` - configuration model
- `Services/DatabaseApiConfigService.cs` - loads config from database
- `Services/TokenRenewalService.cs` - handles 401 and token refresh
- `Services/OrderApiService.cs` - makes primary API calls
- `Program.cs` - DI setup and credential fallback logic
