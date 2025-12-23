# API Headers Database Migration

## Overview

This migration moves site-specific HTTP headers from hardcoded logic into database configuration, making the system more flexible and maintainable.

## Problem Solved

Previously, headers like `ClientId`, `StoreId`, and `WarehouseId` were hardcoded based on URL patterns:

```csharp
// OLD - Hardcoded logic
if (apiConfig.BaseUrl.Contains("malchus.3plnext.com"))
{
    _httpClient.DefaultRequestHeaders.Add("ClientId", "1");
    _httpClient.DefaultRequestHeaders.Add("StoreId", "1");
}
else if (apiConfig.WarehouseId > 0)
{
    _httpClient.DefaultRequestHeaders.Add("WarehouseId", apiConfig.WarehouseId.ToString());
}
```

This approach had several issues:
- Hardcoded values scattered across multiple files
- Difficult to change header values without code changes
- Not scalable for new APIs with different requirements

## Solution

The new approach stores headers in the database:

### 1. New Table: `ApiHeaders`

```sql
CREATE TABLE ApiHeaders (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    ApiAuthId INTEGER NOT NULL,
    HeaderName TEXT NOT NULL,
    HeaderValue TEXT NOT NULL,
    IsEnabled INTEGER NOT NULL DEFAULT 1,
    CreatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (ApiAuthId) REFERENCES ApiAuth(Id) ON DELETE CASCADE,
    UNIQUE(ApiAuthId, HeaderName)
);
```

### 2. Updated Code

Headers are now loaded from the database and applied dynamically:

```csharp
// NEW - Database-driven
var customHeaders = LoadCustomHeaders(baseUrl);
foreach (var header in customHeaders)
{
    request.Headers.Add(header.Key, header.Value);
}
```

## Migration Steps

### 1. Apply the Migration

```powershell
cd scheduled-print-service/scripts
.\apply-headers-migration.ps1
```

This script will:
- Create the `ApiHeaders` table
- Populate headers for existing APIs:
  - **mj.3plnext.com**: `WarehouseId: 4`
  - **malchus.3plnext.com**: `ClientId: 1`, `StoreId: 1`
- Display verification results

### 2. Rebuild the Service

```powershell
cd scheduled-print-service
powershell -ExecutionPolicy Bypass -File scripts/publish.ps1
```

### 3. Restart the Service

The service will now use headers from the database.

## Managing Headers

### View Headers

```sql
SELECT
    a.BaseUrl,
    h.HeaderName,
    h.HeaderValue,
    h.IsEnabled
FROM ApiAuth a
LEFT JOIN ApiHeaders h ON a.Id = h.ApiAuthId
ORDER BY a.BaseUrl, h.HeaderName;
```

### Add Header

```sql
INSERT INTO ApiHeaders (ApiAuthId, HeaderName, HeaderValue)
SELECT Id, 'CustomHeader', 'value'
FROM ApiAuth
WHERE BaseUrl = 'https://example.com';
```

### Update Header

```sql
UPDATE ApiHeaders
SET HeaderValue = 'new-value'
WHERE ApiAuthId = (SELECT Id FROM ApiAuth WHERE BaseUrl = 'https://example.com')
  AND HeaderName = 'CustomHeader';
```

### Disable Header

```sql
UPDATE ApiHeaders
SET IsEnabled = 0
WHERE ApiAuthId = (SELECT Id FROM ApiAuth WHERE BaseUrl = 'https://example.com')
  AND HeaderName = 'CustomHeader';
```

### Delete Header

```sql
DELETE FROM ApiHeaders
WHERE ApiAuthId = (SELECT Id FROM ApiAuth WHERE BaseUrl = 'https://example.com')
  AND HeaderName = 'CustomHeader';
```

## Benefits

1. **Centralized Configuration**: All headers in one place (database)
2. **No Code Changes**: Modify headers without rebuilding
3. **Per-API Flexibility**: Each API can have unique headers
4. **Easy Management**: Simple SQL queries to manage headers
5. **Backward Compatible**: Falls back to `WarehouseId` if no custom headers

## Files Modified

- `Models/ApiConfig.cs` - Added `CustomHeaders` property
- `Services/DatabaseApiConfigService.cs` - Added `LoadCustomHeaders()` method
- `Services/OrderApiService.cs` - Replaced hardcoded headers with database lookup
- `Services/TokenRenewalService.cs` - Replaced hardcoded headers with database lookup

## Rollback

If needed, you can rollback by:

1. Drop the table:
   ```sql
   DROP TABLE IF EXISTS ApiHeaders;
   ```

2. Restore the hardcoded logic in the code files

However, the new code maintains backward compatibility, so rollback should not be necessary.