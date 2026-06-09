# Quick Start: API Headers Configuration

## TL;DR

Run this to move from hardcoded headers to database configuration:

```powershell
cd scheduled-print-service/scripts
.\apply-headers-migration.ps1
cd ..
powershell -ExecutionPolicy Bypass -File scripts/publish.ps1
# Restart service
```

## What This Does

Replaces this hardcoded logic:
```csharp
if (url.Contains("malchus")) {
    headers.Add("ClientId", "1");
    headers.Add("StoreId", "1");
}
```

With database configuration:
```sql
INSERT INTO ApiHeaders (ApiAuthId, HeaderName, HeaderValue)
VALUES (1, 'ClientId', '1'), (1, 'StoreId', '1');
```

## Current Configuration After Migration

| Base URL | Header | Value |
|----------|--------|-------|
| https://mj.3plnext.com | WarehouseId | 4 |
| https://malchus.3plnext.com | ClientId | 1 |
| https://malchus.3plnext.com | StoreId | 1 |

## Common Tasks

### Add a new header for an API

```sql
-- Add 'CustomerId: 123' header for mj.3plnext.com
INSERT INTO ApiHeaders (ApiAuthId, HeaderName, HeaderValue)
SELECT Id, 'CustomerId', '123'
FROM ApiAuth
WHERE BaseUrl = 'https://mj.3plnext.com';
```

### Change a header value

```sql
-- Change StoreId from 1 to 2 for malchus
UPDATE ApiHeaders
SET HeaderValue = '2'
WHERE HeaderName = 'StoreId'
  AND ApiAuthId = (SELECT Id FROM ApiAuth WHERE BaseUrl = 'https://malchus.3plnext.com');
```

### View all headers

```sql
SELECT a.BaseUrl, h.HeaderName, h.HeaderValue
FROM ApiAuth a
JOIN ApiHeaders h ON a.Id = h.ApiAuthId
WHERE h.IsEnabled = 1;
```

## No Rebuild Required

Header changes take effect on the next API call - no service restart needed!

The service loads headers fresh from the database for each API configuration load.

## Documentation

See [API-HEADERS-MIGRATION.md](API-HEADERS-MIGRATION.md) for complete details.