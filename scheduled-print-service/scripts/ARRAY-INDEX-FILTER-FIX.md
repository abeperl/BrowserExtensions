# Array Index Filter Fix

**Date:** 2025-12-02
**Issue:** Array index filtering not working - all orders skipped with "Array index key '17' not found in item data"
**Root Cause:** `JsonElementToDictionary` method only handled JSON objects, not arrays

## Problem Analysis

### Symptom
All 194 orders from API #2 were being skipped with this error:
```
[DBG] Array index key '17' not found in item data
[INF] [1/6] Navigate - SS/SO Orders skipped for order 7075 due to filter: StartsWithAny on array index 17
```

### API Response Structure
API #2 returns orders as **arrays**, not objects:
```json
[
  "6627",
  "2202500006627",
  "Packing1 Warehouse Status",
  "Malchut Judaica",
  "",
  "11/19/2025",
  "1",
  "1",
  "0",
  "11/19/2025 12:41 AM",
  "Pending",
  "2102500009682",
  "3902 14 Ave",
  "New York",
  "Brooklyn",
  "",
  "",
  "SS1748",  ← Array index 17: Order reference
  "0",
  "",
  "MJ",
  "1",
  "1.0000",
  "Abe Perl",
  "",
  "",
  "",
  "1",
  "8",
  "SS80-(3769340)"
]
```

### Filter Configuration
**Subaction 1-3 (SS/SO Orders):**
- Filter: `StartsWithAny ["SS", "SO"]` at array index 17
- Should match: "SS1748", "SS80", "SO1234", etc.

**Subaction 4-6 (Non-SS/SO Orders):**
- Filter: `NotStartsWithAny ["SS", "SO"]` at array index 17
- Should match: "SM4958", "MJ1234", etc.

### Root Cause

The `JsonElementToDictionary` method (line 2912 in `SubActionExecutor.cs`) only handled JSON **objects**:

**Before (BROKEN):**
```csharp
private Dictionary<string, object> JsonElementToDictionary(JsonElement element)
{
    var dict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

    if (element.ValueKind == JsonValueKind.Object)
    {
        foreach (var property in element.EnumerateObject())
        {
            dict[property.Name] = property.Value.ValueKind switch
            {
                JsonValueKind.String => property.Value.GetString() ?? string.Empty,
                JsonValueKind.Number => property.Value.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => string.Empty,
                _ => property.Value.GetRawText()
            };
        }
    }

    return dict;  // Returns EMPTY dictionary for array data!
}
```

**Problem:** When API returns array data, this method returned an **empty dictionary**, so array index 17 couldn't be accessed.

## Solution

Added array handling to `JsonElementToDictionary` method to create dictionary entries with numeric string keys ("0", "1", "2", etc.):

**After (FIXED):**
```csharp
private Dictionary<string, object> JsonElementToDictionary(JsonElement element)
{
    var dict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

    if (element.ValueKind == JsonValueKind.Object)
    {
        foreach (var property in element.EnumerateObject())
        {
            dict[property.Name] = property.Value.ValueKind switch
            {
                JsonValueKind.String => property.Value.GetString() ?? string.Empty,
                JsonValueKind.Number => property.Value.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => string.Empty,
                _ => property.Value.GetRawText()
            };
        }
    }
    else if (element.ValueKind == JsonValueKind.Array)
    {
        // Handle array data by creating dictionary with numeric string keys ("0", "1", "2", etc.)
        var arrayElements = element.EnumerateArray().ToList();
        for (int i = 0; i < arrayElements.Count; i++)
        {
            dict[i.ToString()] = arrayElements[i].ValueKind switch
            {
                JsonValueKind.String => arrayElements[i].GetString() ?? string.Empty,
                JsonValueKind.Number => arrayElements[i].GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => string.Empty,
                _ => arrayElements[i].GetRawText()
            };
        }
    }

    return dict;
}
```

## How It Works Now

### Array to Dictionary Conversion

**Input:** JSON array
```json
["6627", "2202500006627", ..., "SS1748", ...]
```

**Output:** Dictionary with numeric string keys
```csharp
{
    "0" => "6627",
    "1" => "2202500006627",
    ...
    "17" => "SS1748",
    ...
}
```

### Filter Evaluation

When filter specifies `ChainedFilterArrayIndex: 17`:
1. `JsonElementToDictionary` converts array to dictionary with keys "0", "1", ..., "17", ...
2. Filter accesses `dict["17"]` → gets "SS1748"
3. Filter checks if "SS1748" starts with "SS" or "SO" → **TRUE** ✓
4. Order is processed by SS/SO subactions

## Expected Behavior After Fix

### Next API #2 Run:

1. **Primary API Call:** Fetches all 194 orders (no primary filter)
2. **Order Processing:**
   - Order with reference "SS1748" at index 17:
     - Actions 1-3 (SS/SO): **PROCESS** ✓ (starts with "SS")
     - Actions 4-6 (Non-SS/SO): **SKIP** (starts with "SS")

   - Order with reference "SM4958" at index 17:
     - Actions 1-3 (SS/SO): **SKIP** (doesn't start with "SS" or "SO")
     - Actions 4-6 (Non-SS/SO): **PROCESS** ✓ (doesn't start with "SS" or "SO")

3. **Result:** All 194 orders processed correctly, split between SS/SO and Non-SS/SO subactions

### Log Output Should Show:

```
[INF] API #2 returned 194 orders
[DBG] Processing order: 7075
[INF] [1/6] Navigate - SS/SO Orders for order 7075
[INF] [2/6] Save PDF - SS/SO Orders completed successfully
[INF] [3/6] Print PDF - SS/SO Orders completed successfully
[INF] [4/6] Navigate - Non SS/SO Orders skipped for order 7075 due to filter: NotStartsWithAny on array index 17
[INF] [5/6] Save PDF - Non SS/SO Orders skipped (no page captured)
[INF] [6/6] Print PDF - Non-SS/SO Orders skipped (no PDF saved)

[DBG] Processing order: 7139
[INF] [1/6] Navigate - SS/SO Orders skipped for order 7139 due to filter: StartsWithAny on array index 17
[INF] [4/6] Navigate - Non SS/SO Orders for order 7139
[INF] [5/6] Save PDF - Non SS/SO Orders completed successfully
[INF] [6/6] Print PDF - Non-SS/SO Orders completed successfully
```

## Files Modified

- `Services/SubActionExecutor.cs` (lines 2912-2950)

## When to Use Array Index Filtering

### Use Array Index Filtering When:
- API returns data as arrays instead of objects
- Array elements have consistent positions (e.g., index 17 = order reference)
- You need to filter by specific array element values
- Example: API #2 with order references at index 17

### Use Field Name Filtering When:
- API returns data as objects with named properties
- You need to filter by property values
- Example: API #3 with `CustomerName`, `OrderNumber` properties

## Testing

To verify the fix is working:

1. **Check array conversion:**
   - Enable debug logging for `JsonElementToDictionary`
   - Verify dictionary contains keys "0", "1", ..., "17", etc. for array data

2. **Check filter evaluation:**
   - Verify logs show "Navigate - SS/SO Orders" executed for orders with "SS*" or "SO*" at index 17
   - Verify logs show "Navigate - Non SS/SO Orders" executed for other orders
   - Verify NO MORE "Array index key '17' not found in item data" errors

3. **Verify PDF output:**
   - `picklist-wholesale_*.pdf` files for SS/SO orders
   - `picklist-stores_*.pdf` files for Non-SS/SO orders
   - Total PDFs should match total orders (194)

## Related Files

- `Services/SubActionExecutor.cs:2912-2950` - Fixed array handling in `JsonElementToDictionary`
- `Services/SubActionExecutor.cs:866-883` - Filter evaluation logic that uses the dictionary
- `scripts/API2-FILTER-FIX-SUMMARY.md` - Previous fix for API #2 filter conflict
- `scripts/fix-api2-filter-conflict.sql` - Migration that removed primary API filter
