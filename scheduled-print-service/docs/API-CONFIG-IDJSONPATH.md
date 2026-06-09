# API Configuration: IdJsonPath

## Overview

The `IdJsonPath` configuration determines how the service extracts unique identifiers from API response records. This is critical for:
- Tracking processed records
- Avoiding duplicate processing
- Identifying which items to process

## Configuration Location

The `IdJsonPath` is stored in the `Configuration` column of the `PrimaryApi` table as a JSON property.

**Example:**
```sql
UPDATE PrimaryApi
SET Configuration = json_set(
    COALESCE(Configuration, '{}'),
    '$.IdJsonPath',
    'orderDetailsId'
)
WHERE ApiNumber = 3;
```

## Supported Path Formats

### Property Name (Object-based responses)

Use a property name when each item in the response array is an object.

**Example Response:**
```json
{
  "data": [
    {
      "orderId": 2472,
      "orderDetailsId": 11996,
      "sku": "BMX825BG"
    }
  ]
}
```

**Configuration:**
```json
{
  "IdJsonPath": "orderDetailsId"
}
```

This extracts `11996` from the `orderDetailsId` field.

### Array Index (Array-based responses)

Use `[N]` when each item in the response is an array, where N is the zero-based index.

**Example Response:**
```json
{
  "data": [
    [12345, "SS-Order-001", "2025-11-28", ...],
    [12346, "SS-Order-002", "2025-11-28", ...]
  ]
}
```

**Configuration:**
```json
{
  "IdJsonPath": "[0]"
}
```

This extracts `12345` from the first element of each array item.

## Default Value

If `IdJsonPath` is not configured, the default value is `"[0]"` (first array element), which is defined in `Models/ApiConfig.cs:32`.

## Current API Configurations

| API # | API Name | Endpoint | IdJsonPath | Notes |
|-------|----------|----------|------------|-------|
| 1 | Orders List API | /api/order/GetOrdersList | `[0]` (default) | Uses array-based responses |
| 2 | Picklist Datatable API | /api/Picklist/GetPicklistDatatable | `[0]` (default) | Uses array-based responses with filter |
| 3 | Personalized Orders API | /api/Order/GetPersonalizedOrderItems | `orderDetailsId` | Uses object-based responses |
| 4 | Picklist Datatable API (Non-SS) | /api/Picklist/GetPicklistDatatable | `[0]` (default) | Uses array-based responses with filter |

## Troubleshooting

### Symptom: "Could not extract ID from record"

**Log Example:**
```
[WRN] Could not extract ID from record using path: [0]
```

**Cause:** The `IdJsonPath` doesn't match the actual response structure.

**Solution:**
1. Check the API response structure in the logs (look for "Response preview:")
2. Determine if items are objects or arrays
3. Update the Configuration accordingly

**For object-based responses:**
```sql
UPDATE PrimaryApi
SET Configuration = json_set(
    COALESCE(Configuration, '{}'),
    '$.IdJsonPath',
    'propertyName'
)
WHERE ApiNumber = X;
```

**For array-based responses:**
```sql
UPDATE PrimaryApi
SET Configuration = json_set(
    COALESCE(Configuration, '{}'),
    '$.IdJsonPath',
    '[0]'
)
WHERE ApiNumber = X;
```

## Code Implementation

The `IdJsonPath` is parsed in `Services/DatabaseApiConfigService.cs` around line 162:

```csharp
// Parse IdJsonPath if present in configuration
if (root.TryGetProperty("IdJsonPath", out var idJsonPathElement))
{
    var idJsonPath = idJsonPathElement.GetString();
    if (!string.IsNullOrWhiteSpace(idJsonPath))
    {
        config.IdJsonPath = idJsonPath;
    }
}
```

The extraction logic is in `Services/OrderApiService.cs` around line 413:

```csharp
private string ExtractIdFromJsonPath(JsonElement element, string jsonPath)
{
    // Supports both "[N]" for array indices and "propertyName" for object properties
    if (jsonPath.StartsWith("[") && jsonPath.EndsWith("]"))
    {
        // Array index extraction
        var indexStr = jsonPath.Trim('[', ']');
        if (int.TryParse(indexStr, out var index) && element.ValueKind == JsonValueKind.Array)
        {
            var array = element.EnumerateArray().ToList();
            if (index < array.Count)
            {
                return array[index].ToString();
            }
        }
    }
    else
    {
        // Property name extraction
        if (element.TryGetProperty(jsonPath, out var propElement))
        {
            return propElement.ToString();
        }
    }
    return string.Empty;
}
```

## Fix Applied (2025-11-28)

**Issue:** API #3 (Personalized Orders API) was failing to extract IDs because it was using the default `[0]` array index, but the response contains objects with an `orderDetailsId` property.

**Fix:** Updated API #3 configuration to use `orderDetailsId` as the IdJsonPath:
- SQL script: `fix-api3-id-path.sql`
- Code change: Added IdJsonPath parsing in `DatabaseApiConfigService.cs:162-170`

**Testing:** After applying the fix, restart the service and verify that log warnings disappear.
