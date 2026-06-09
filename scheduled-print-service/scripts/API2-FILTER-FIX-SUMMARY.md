# API #2 Filter Conflict Fix

**Date:** 2025-12-02
**Issue:** API #2 processed 193 orders but skipped all 83 kept orders
**Root Cause:** Primary API filter conflicted with subaction filters

## Problem Analysis

### Before Fix

**Primary API Filter:**
- Type: `StartsWithAny`
- Value: `"SS,SO"`
- Array Index: `17`
- **Effect:** Filters OUT 110 orders, keeps only 83 orders that start with "SS" or "SO"

**Subaction Filters:**
- Actions 1-3 (SS/SO Orders): `StartsWithAny ["SS","SO"]` - Wants SS/SO orders
- Actions 4-6 (Non-SS/SO Orders): `NotStartsWithAny ["SS","SO"]` - Wants Non-SS/SO orders

**The Conflict:**
```
API Response: 193 orders
  ↓ Primary API Filter (StartsWithAny SS,SO)
  ↓ Keeps: 83 SS/SO orders
  ↓ Filters OUT: 110 Non-SS/SO orders

Processing:
  → Actions 1-3: Process 83 SS/SO orders ✓ (matches filter)
  → Actions 4-6: Process 0 orders ✗ (wants Non-SS/SO but all were filtered out!)

Result: 110 orders never processed!
```

### Logs Analysis

```
API #2 returned 193 orders
Primary API filter applied: 193 total items, 110 filtered out, 83 kept
Batch processing complete: 83 orders skipped
```

**Why 110 filtered out:** Orders that DON'T start with "SS" or "SO" at array index 17
**Why 83 skipped:** All 83 were SS/SO orders, so Actions 4-6 skipped them (looking for Non-SS/SO)

## Solution

**Remove the primary API filter** and let subactions handle filtering independently.

### After Fix

**Primary API Filter:** REMOVED (processes all 193 orders)

**Subaction Filters:** Remain unchanged
- Actions 1-3: Filter for SS/SO orders
- Actions 4-6: Filter for Non-SS/SO orders

**New Flow:**
```
API Response: 193 orders (no filtering)

Processing:
  → Actions 1-3: Process ~83 SS/SO orders ✓
  → Actions 4-6: Process ~110 Non-SS/SO orders ✓

Result: All 193 orders processed correctly!
```

## Configuration Changes

### Primary API Configuration

**Before:**
```json
{
  "ChainedArrayJsonPath": "data",
  "ChainedFilterArrayIndex": 17,
  "ChainedFilterType": "StartsWithAny",
  "ChainedFilterValue": "SS,SO"
}
```

**After:**
```json
{
  "ChainedArrayJsonPath": "data"
}
```

### Subaction Configuration

No changes to subactions - they already had the correct filters:

**Action 1 (Navigate - SS/SO):**
```json
{
  "ChainedFilterType": "StartsWithAny",
  "ChainedFilterValues": ["SS", "SO"],
  "ChainedFilterArrayIndex": 17
}
```

**Action 4 (Navigate - Non-SS/SO):**
```json
{
  "ChainedFilterType": "NotStartsWithAny",
  "ChainedFilterValues": ["SS", "SO"],
  "ChainedFilterArrayIndex": 17
}
```

## Expected Behavior After Fix

### Next API #2 Run:

1. **Primary API Call:** Fetches all 193 orders (no filtering)
2. **Action 1-3 Processing:**
   - Filters: Orders where `data[x][17]` starts with "SS" or "SO"
   - Processes: ~83 orders
   - Generates: `picklist-wholesale_*.pdf` files
   - Prints to: `L2710DW` printer
3. **Action 4-6 Processing:**
   - Filters: Orders where `data[x][17]` does NOT start with "SS" or "SO"
   - Processes: ~110 orders
   - Generates: `picklist-stores_*.pdf` files
   - Prints to: `L2710DW` printer
4. **Result:** All 193 orders processed, no skips

### Log Output Should Show:

```
[INF] API #2 returned 193 orders
[DBG] Processing order: 7139
[INF] [1/6] Navigate - SS/SO Orders for order 7139
[INF] [2/6] Save PDF - SS/SO Orders completed successfully
[INF] [3/6] Print PDF - SS/SO Orders completed successfully
[INF] [4/6] Navigate - Non SS/SO Orders skipped for order 7139 due to filter: NotStartsWithAny on array index 17
[INF] [5/6] Save PDF - Non SS/SO Orders skipped (no page captured)
[INF] [6/6] Print PDF - Non-SS/SO Orders skipped (no PDF saved)

[DBG] Processing order: 2202500001234
[INF] [1/6] Navigate - SS/SO Orders skipped for order 2202500001234 due to filter: StartsWithAny on array index 17
[INF] [4/6] Navigate - Non SS/SO Orders for order 2202500001234
[INF] [5/6] Save PDF - Non SS/SO Orders completed successfully
[INF] [6/6] Print PDF - Non-SS/SO Orders completed successfully
```

## Performance Impact

### Before Fix
- API Response Size: 83 orders
- Processing: 83 orders (but 110 never reached the API)
- Result: Incomplete processing

### After Fix
- API Response Size: 193 orders (↑ 132% larger)
- Processing: 193 orders (all orders processed)
- Additional Time: ~10-15 seconds per schedule run (estimate)
- Result: Complete processing

**Trade-off:** Slightly larger API response and longer processing time in exchange for processing ALL orders correctly.

## When to Use Primary API Filters vs Subaction Filters

### Use Primary API Filters When:
- ALL subactions need the same filter
- You want to reduce API response payload size
- Performance optimization is critical
- Example: Filter by date range that applies to all subactions

### Use Subaction Filters When:
- Different subactions need different filters (like API #2)
- You want maximum flexibility
- API response size is manageable
- Example: Split processing by order type (SS/SO vs Non-SS/SO)

## Related Files

- `scripts/fix-api2-filter-conflict.sql` - Migration script
- `Services/OrderApiService.cs:390-426` - Primary API filter implementation
- `Services/SubActionExecutor.cs:185-200` - Subaction filter implementation
- `Services/SubActionExecutor.cs:803-895` - Filter evaluation logic

## Testing

To verify the fix is working:

1. Check logs for "Primary API filter applied" message:
   - Should NOT appear for API #2
   - Only appears when `PrimaryFilterType` is configured

2. Check logs for subaction filter messages:
   - Should see "[X/6] Navigate - SS/SO Orders" executed for SS/SO orders
   - Should see "[X/6] Navigate - SS/SO Orders skipped" for Non-SS/SO orders
   - Should see "[X/6] Navigate - Non SS/SO Orders" executed for Non-SS/SO orders
   - Should see "[X/6] Navigate - Non SS/SO Orders skipped" for SS/SO orders

3. Verify PDF output:
   - `picklist-wholesale_*.pdf` files for SS/SO orders
   - `picklist-stores_*.pdf` files for Non-SS/SO orders
   - Total PDFs should match total orders (193)
