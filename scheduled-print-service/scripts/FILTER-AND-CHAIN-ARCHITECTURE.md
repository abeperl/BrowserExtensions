# Filter and Chain Architecture

## Overview

This document describes the new unified API architecture with filtered sub-actions and chained printer assignments.

## Previous Architecture (API 2 + API 4)

### Problems:
- **Duplicate API Calls**: Both API 2 and API 4 fetched the same datatable
- **Inefficient**: Server processed the same request twice
- **Filter Location**: Filtering happened AFTER fetching all rows
- **Maintenance**: Two separate APIs to manage for similar logic

```
API 2 Flow:
  1. Fetch all rows from datatable
  2. Filter in-memory: OrderId starts with "SS" or "SO"
  3. Navigate to each filtered row
  4. Print to Printer 1 (NPI84BD10)

API 4 Flow:
  1. Fetch all rows from datatable (same as API 2!)
  2. Filter in-memory: OrderId does NOT start with "SS" or "SO"
  3. Navigate to each filtered row
  4. Print to Printer 2 (4301)
```

## New Architecture (Unified API 2)

### Benefits:
- **Single API Call**: Fetches datatable once
- **Parallel Sub-Actions**: Multiple filter paths process simultaneously
- **Chained Actions**: Filter results automatically flow to printer actions
- **Flexible**: Easy to add new filter conditions and printers

```
API 2 Unified Flow:
  1. Fetch all rows from datatable (ONCE)

  Sub-Action 1 (Filter SS/SO):
    2a. Filter: OrderId starts with "SS" or "SO"
    3a. Navigate to filtered rows

  Sub-Action 2 (Print SS/SO - Chained from 1):
    4a. Receive results from Action 1
    5a. Print to Printer 1 (NPI84BD10)

  Sub-Action 3 (Filter Non-SS/SO):
    2b. Filter: OrderId does NOT start with "SS" or "SO"
    3b. Navigate to filtered rows

  Sub-Action 4 (Print Non-SS/SO - Chained from 3):
    4b. Receive results from Action 3
    5b. Print to Printer 2 (4301)
```

## Filter Types

### StartsWithAny
Checks if the field value starts with ANY of the provided values.

```json
{
  "ChainedFilterArrayIndex": 17,
  "ChainedFilterType": "StartsWithAny",
  "ChainedFilterValues": ["SS", "SO"]
}
```

**Example Matches:**
- `"SS1234"` ✓ (starts with "SS")
- `"SO5678"` ✓ (starts with "SO")
- `"SM9999"` ✗ (doesn't start with "SS" or "SO")

### NotStartsWithAny
Checks if the field value does NOT start with ANY of the provided values.

```json
{
  "ChainedFilterArrayIndex": 17,
  "ChainedFilterType": "NotStartsWithAny",
  "ChainedFilterValues": ["SS", "SO"]
}
```

**Example Matches:**
- `"SM1234"` ✓ (doesn't start with "SS" or "SO")
- `"AB5678"` ✓ (doesn't start with "SS" or "SO")
- `"SS9999"` ✗ (starts with "SS")

### Other Available Filter Types
These can be used for future enhancements:

- **StartsWith**: Single value check (legacy)
  ```json
  {"ChainedFilterType": "StartsWith", "ChainedFilterValue": "SS"}
  ```

- **NotStartsWith**: Single value inverse check
  ```json
  {"ChainedFilterType": "NotStartsWith", "ChainedFilterValue": "SS"}
  ```

- **Equals**: Exact match
  ```json
  {"ChainedFilterType": "Equals", "ChainedFilterValue": "SS1234"}
  ```

- **Contains**: Substring match
  ```json
  {"ChainedFilterType": "Contains", "ChainedFilterValue": "RUSH"}
  ```

## Sub-Action Chaining

### How Chaining Works

**ChainedFromActionNumber** specifies which action's output becomes this action's input.

```json
{
  "ActionNumber": 2,
  "ActionType": "PrintCapturedHtml",
  "Configuration": {
    "ChainedFromActionNumber": 1,
    "UseChainedInput": true,
    ...
  }
}
```

### Execution Flow

1. **Primary API** fetches data (returns array of rows)
2. **Action 1** (Filter) processes the array, filters rows, navigates
3. **Action 2** (Print) receives Action 1's filtered results, prints each
4. **Action 3** (Filter) processes the original array independently, filters different rows
5. **Action 4** (Print) receives Action 3's filtered results, prints each

### Key Points

- **Independent Branches**: Actions 1→2 and 3→4 run independently
- **No Cross-Contamination**: Action 1's filter doesn't affect Action 3
- **Original Data Preserved**: Each filter action starts with full dataset
- **Chained Actions Only See Filtered Results**: Action 2 only processes rows that passed Action 1's filter

## Data Flow Example

### Sample Datatable Response
```json
{
  "data": [
    [1, "Item1", ..., "SS1234", ...],  // Row 0: index 17 = "SS1234"
    [2, "Item2", ..., "SO5678", ...],  // Row 1: index 17 = "SO5678"
    [3, "Item3", ..., "SM9999", ...],  // Row 2: index 17 = "SM9999"
    [4, "Item4", ..., "AB1111", ...]   // Row 3: index 17 = "AB1111"
  ]
}
```

### Filter Processing

**Action 1 (StartsWithAny ["SS", "SO"]):**
- Row 0: "SS1234" → ✓ Match
- Row 1: "SO5678" → ✓ Match
- Row 2: "SM9999" → ✗ No match
- Row 3: "AB1111" → ✗ No match

**Action 1 Output:** Rows 0, 1 (2 rows)

**Action 2 (Chained from 1):**
- Receives: Rows 0, 1
- Navigates to `ManualPicking?id={row[0]}`
- Prints to Printer 1 (NPI84BD10)

---

**Action 3 (NotStartsWithAny ["SS", "SO"]):**
- Row 0: "SS1234" → ✗ No match
- Row 1: "SO5678" → ✗ No match
- Row 2: "SM9999" → ✓ Match
- Row 3: "AB1111" → ✓ Match

**Action 3 Output:** Rows 2, 3 (2 rows)

**Action 4 (Chained from 3):**
- Receives: Rows 2, 3
- Navigates to `ManualPicking?id={row[0]}`
- Prints to Printer 2 (4301)

## Configuration Reference

### Complete Sub-Action Configuration

```json
{
  "ActionNumber": 1,
  "ActionName": "Navigate - SS/SO Orders",
  "ActionType": "NavigateOnly",
  "Configuration": {
    // Array handling
    "ChainedArrayJsonPath": "data",
    "UseChainedInput": true,
    "ChainedItemFieldPath": "[0]",

    // Navigation
    "Endpoint": "https://mj.3plnext.com/#Outbound/ManualPicking?id={id}",
    "Method": "GET",
    "WaitForNetworkIdleMs": 3000,
    "MakeHiddenVisible": true,

    // Filtering
    "ChainedFilterArrayIndex": 17,
    "ChainedFilterType": "StartsWithAny",
    "ChainedFilterValues": ["SS", "SO"],

    // Error handling
    "ContinueOnError": true
  }
}
```

### Printer Sub-Action Configuration

```json
{
  "ActionNumber": 2,
  "ActionName": "Print SS/SO Orders - Printer 1",
  "ActionType": "PrintCapturedHtml",
  "Configuration": {
    // Chaining
    "ChainedFromActionNumber": 1,
    "UseChainedInput": true,
    "ChainedItemFieldPath": "[0]",

    // API call
    "Method": "GET",
    "Endpoint": "/api/Picklist/GetPicklistHtml/{id}",
    "HtmlJsonPath": "html",

    // Printing
    "PrinterName": "NPI84BD10 (HP LaserJet M607)",
    "OutputFilePrefix": "picklist-ss-so",

    // Error handling
    "ContinueOnError": true
  }
}
```

## Migration Instructions

### Before Running Migration

1. **Backup Database:**
   ```bash
   copy "C:\Users\User\source\repos\BrowserExtensions\scheduled-print-service\data\api_config.db" "api_config.db.backup"
   ```

2. **Verify Current State:**
   ```sql
   SELECT ApiNumber, ApiName, PrinterName FROM PrimaryApi WHERE ApiNumber IN (2, 4);
   ```

### Run Migration

```bash
sqlite3 "C:\Users\User\source\repos\BrowserExtensions\scheduled-print-service\data\api_config.db" < "C:\Users\User\source\repos\BrowserExtensions\scheduled-print-service\scripts\consolidate-apis-with-chained-filters.sql"
```

### Verify Migration

Run the verification queries included in the migration script:

```sql
-- Check unified API 2
SELECT ApiNumber, ApiName FROM PrimaryApi WHERE ApiNumber = 2;

-- Check sub-actions
SELECT ActionNumber, ActionName, ActionType, ExecutionOrder
FROM SubAction s
JOIN PrimaryApi p ON s.PrimaryApiId = p.Id
WHERE p.ApiNumber = 2
ORDER BY ExecutionOrder;

-- Verify API 4 deleted
SELECT COUNT(*) FROM PrimaryApi WHERE ApiNumber = 4;
-- Should return 0
```

## Updating Printer Names

To change printer assignments after migration:

```sql
-- Update Printer 1 (SS/SO orders)
UPDATE SubAction
SET Configuration = json_set(Configuration, '$.PrinterName', 'NewPrinterName1')
WHERE PrimaryApiId = (SELECT Id FROM PrimaryApi WHERE ApiNumber = 2)
  AND ActionNumber = 2;

-- Update Printer 2 (Non-SS/SO orders)
UPDATE SubAction
SET Configuration = json_set(Configuration, '$.PrinterName', 'NewPrinterName2')
WHERE PrimaryApiId = (SELECT Id FROM PrimaryApi WHERE ApiNumber = 2)
  AND ActionNumber = 4;
```

## Adding New Filter Branches

To add a third filter condition (e.g., for "RUSH" orders to Printer 3):

```sql
-- Add filter action
INSERT INTO SubAction (PrimaryApiId, ActionNumber, ActionName, ActionType, Configuration, ExecutionOrder, IsEnabled)
VALUES (
  (SELECT Id FROM PrimaryApi WHERE ApiNumber = 2),
  5,
  'Navigate - RUSH Orders',
  'NavigateOnly',
  json('{
    "ChainedArrayJsonPath": "data",
    "ChainedFilterArrayIndex": 17,
    "ChainedFilterType": "StartsWith",
    "ChainedFilterValue": "RUSH",
    ...
  }'),
  5,
  1
);

-- Add printer action chained from filter
INSERT INTO SubAction (PrimaryApiId, ActionNumber, ActionName, ActionType, Configuration, ExecutionOrder, IsEnabled)
VALUES (
  (SELECT Id FROM PrimaryApi WHERE ApiNumber = 2),
  6,
  'Print RUSH Orders - Printer 3',
  'PrintCapturedHtml',
  json('{
    "ChainedFromActionNumber": 5,
    "PrinterName": "Printer3Name",
    ...
  }'),
  6,
  1
);
```

## Troubleshooting

### No Rows Being Processed
- Check `ChainedFilterArrayIndex` matches the correct column index
- Verify filter values match actual data (case-sensitive)
- Check API response structure in logs

### Rows Going to Wrong Printer
- Verify `ChainedFromActionNumber` points to correct filter action
- Check printer names are exact matches
- Review filter types (StartsWith vs NotStartsWith)

### Performance Issues
- Reduce `WaitForNetworkIdleMs` if pages load quickly
- Check network logs for slow API responses
- Consider adding index on frequently filtered columns

## Future Enhancements

1. **Dynamic Filter Values**: Load filter values from configuration table
2. **Complex Filters**: AND/OR combinations, regex patterns
3. **Conditional Chaining**: Choose printer based on additional criteria
4. **Filter Metrics**: Track how many rows match each filter
5. **Filter Testing**: Dry-run mode to see filter results without printing
