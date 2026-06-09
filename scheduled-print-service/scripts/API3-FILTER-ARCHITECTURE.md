# API 3 Filter and Chain Architecture

## Overview

API 3 handles **Personalized Orders** with custom form HTML files. This document describes the unified filter architecture that routes orders to different printers based on order ID prefixes while maintaining the IsFilePath validation.

## Previous Architecture (Single Branch)

### Problems:
- **Single Printer**: All personalized orders printed to same printer
- **No Order Type Distinction**: SS/SO orders mixed with other orders
- **Limited Flexibility**: Hard to route different order types differently

```
API 3 Original Flow:
  1. Fetch all personalized order items
  2. Filter: itemNotes IsFilePath (has .html, not JSON)
  3. Navigate to each custom form URL
  4. Save PDF to disk
  5. Print to single printer
```

## New Architecture (Multi-Branch with Combined Filters)

### Benefits:
- **Single API Call**: Fetches all items once
- **Multiple Filter Branches**: Each branch has its own filter criteria
- **Combined Filtering**: IsFilePath + Order ID prefix filtering
- **Flexible Routing**: Different printers for different order types
- **Independent Chains**: Each branch: Navigate → Save → Print

```
API 3 Unified Flow:
  1. Fetch all personalized order items (ONCE)

  Branch 1 (SS/SO Orders):
    2a. Filter: IsFilePath + orderId starts with "SS" or "SO"
    3a. Navigate to custom form URLs
    4a. Save PDFs with prefix "personalized-ss-so"
    5a. Print to Printer 1

  Branch 2 (Other Orders):
    2b. Filter: IsFilePath + orderId does NOT start with "SS" or "SO"
    3b. Navigate to custom form URLs
    4b. Save PDFs with prefix "personalized-other"
    5b. Print to Printer 2
```

## Filter Types for API 3

### Primary Filter: IsFilePath

Ensures the `itemNotes` field contains a valid file path (not JSON data).

```json
{
  "ChainedFilterField": "itemNotes",
  "ChainedFilterType": "IsFilePath",
  "ChainedFilterValue": null
}
```

**How It Works:**
- Checks if field contains `.html` extension
- Rejects JSON objects (starts with `{` or `[`)
- Ensures valid file path format

**Example Matches:**
- `"/custom/forms/personalized-form.html"` ✓
- `"https://mj.3plnext.com/custom/form.html"` ✓
- `"{\"orderId\":\"123\"}"` ✗ (JSON object)
- `"[{\"data\":\"value\"}]"` ✗ (JSON array)
- `"regular-text-note"` ✗ (no .html extension)

### Additional Filter: StartsWithAny / NotStartsWithAny

Applied on top of IsFilePath to route by order ID.

**StartsWithAny (Branch 1):**
```json
{
  "AdditionalFilterField": "orderId",
  "AdditionalFilterType": "StartsWithAny",
  "AdditionalFilterValues": ["SS", "SO"]
}
```

**NotStartsWithAny (Branch 2):**
```json
{
  "AdditionalFilterField": "orderId",
  "AdditionalFilterType": "NotStartsWithAny",
  "AdditionalFilterValues": ["SS", "SO"]
}
```

## Combined Filter Logic

### How Combined Filters Work

**Both filters must pass** for an item to be included in a branch.

```
Item Passes Branch 1 IF:
  IsFilePath(itemNotes) = TRUE
  AND
  StartsWithAny(orderId, ["SS", "SO"]) = TRUE

Item Passes Branch 2 IF:
  IsFilePath(itemNotes) = TRUE
  AND
  NotStartsWithAny(orderId, ["SS", "SO"]) = TRUE
```

### Filter Evaluation Order

1. **Primary Filter** (IsFilePath) evaluated first
2. If primary filter fails → Item skipped entirely
3. If primary filter passes → Additional filter evaluated
4. If additional filter fails → Item skipped for this branch
5. If both filters pass → Item processed by this branch

## Data Flow Example

### Sample API Response

```json
{
  "data": [
    {
      "orderDetailsId": 1001,
      "orderId": "SS1234",
      "itemNotes": "/custom/forms/ss-order.html",
      "sku": "CUSTOM-001"
    },
    {
      "orderDetailsId": 1002,
      "orderId": "SO5678",
      "itemNotes": "/custom/forms/so-order.html",
      "sku": "CUSTOM-002"
    },
    {
      "orderDetailsId": 1003,
      "orderId": "SM9999",
      "itemNotes": "/custom/forms/sm-order.html",
      "sku": "CUSTOM-003"
    },
    {
      "orderDetailsId": 1004,
      "orderId": "SS7777",
      "itemNotes": "{\"customData\": \"json-value\"}",
      "sku": "CUSTOM-004"
    },
    {
      "orderDetailsId": 1005,
      "orderId": "AB1111",
      "itemNotes": "/custom/forms/ab-order.html",
      "sku": "CUSTOM-005"
    }
  ]
}
```

### Branch 1 Processing (SS/SO Orders)

**Item 1:**
- orderId: "SS1234"
- itemNotes: "/custom/forms/ss-order.html"
- IsFilePath: ✓ (has .html)
- StartsWithAny ["SS", "SO"]: ✓ (starts with "SS")
- **Result**: Navigate → Save as personalized-ss-so-1001.pdf → Print to Printer 1

**Item 2:**
- orderId: "SO5678"
- itemNotes: "/custom/forms/so-order.html"
- IsFilePath: ✓ (has .html)
- StartsWithAny ["SS", "SO"]: ✓ (starts with "SO")
- **Result**: Navigate → Save as personalized-ss-so-1002.pdf → Print to Printer 1

**Item 3:**
- orderId: "SM9999"
- itemNotes: "/custom/forms/sm-order.html"
- IsFilePath: ✓ (has .html)
- StartsWithAny ["SS", "SO"]: ✗ (starts with "SM")
- **Result**: Skipped by Branch 1

**Item 4:**
- orderId: "SS7777"
- itemNotes: "{\"customData\": \"json-value\"}"
- IsFilePath: ✗ (JSON object)
- **Result**: Skipped by Branch 1 (primary filter failed)

**Item 5:**
- orderId: "AB1111"
- itemNotes: "/custom/forms/ab-order.html"
- IsFilePath: ✓ (has .html)
- StartsWithAny ["SS", "SO"]: ✗ (starts with "AB")
- **Result**: Skipped by Branch 1

**Branch 1 Total**: 2 items processed (Items 1, 2)

### Branch 2 Processing (Non-SS/SO Orders)

**Item 1:**
- orderId: "SS1234"
- NotStartsWithAny ["SS", "SO"]: ✗ (starts with "SS")
- **Result**: Skipped by Branch 2

**Item 2:**
- orderId: "SO5678"
- NotStartsWithAny ["SS", "SO"]: ✗ (starts with "SO")
- **Result**: Skipped by Branch 2

**Item 3:**
- orderId: "SM9999"
- itemNotes: "/custom/forms/sm-order.html"
- IsFilePath: ✓ (has .html)
- NotStartsWithAny ["SS", "SO"]: ✓ (doesn't start with "SS" or "SO")
- **Result**: Navigate → Save as personalized-other-1003.pdf → Print to Printer 2

**Item 4:**
- orderId: "SS7777"
- itemNotes: "{\"customData\": \"json-value\"}"
- IsFilePath: ✗ (JSON object)
- **Result**: Skipped by Branch 2 (primary filter failed)

**Item 5:**
- orderId: "AB1111"
- itemNotes: "/custom/forms/ab-order.html"
- IsFilePath: ✓ (has .html)
- NotStartsWithAny ["SS", "SO"]: ✓ (doesn't start with "SS" or "SO")
- **Result**: Navigate → Save as personalized-other-1005.pdf → Print to Printer 2

**Branch 2 Total**: 2 items processed (Items 3, 5)

### Summary

| Item | Order ID | Item Notes | IsFilePath | Branch 1 | Branch 2 | Final Destination |
|------|----------|------------|------------|----------|----------|-------------------|
| 1 | SS1234 | /custom/.../ss-order.html | ✓ | ✓ | ✗ | Printer 1 |
| 2 | SO5678 | /custom/.../so-order.html | ✓ | ✓ | ✗ | Printer 1 |
| 3 | SM9999 | /custom/.../sm-order.html | ✓ | ✗ | ✓ | Printer 2 |
| 4 | SS7777 | {"customData": "json"} | ✗ | ✗ | ✗ | Skipped |
| 5 | AB1111 | /custom/.../ab-order.html | ✓ | ✗ | ✓ | Printer 2 |

## Sub-Action Configuration

### Navigate Action (Branch 1)

```json
{
  "ActionNumber": 1,
  "ActionName": "Navigate - SS/SO Custom Forms",
  "ActionType": "NavigateOnly",
  "Configuration": {
    // Array handling
    "ChainedArrayJsonPath": "data",
    "UseChainedInput": true,
    "ChainedItemFieldPath": "itemNotes",
    "IdJsonPath": "orderDetailsId",

    // Navigation
    "Endpoint": "https://mj.3plnext.com/{itemNotes}",
    "Method": "GET",
    "WaitForNetworkIdleMs": 3000,
    "MakeHiddenVisible": false,

    // Primary filter (IsFilePath)
    "ChainedFilterField": "itemNotes",
    "ChainedFilterType": "IsFilePath",
    "ChainedFilterValue": null,

    // Additional filter (Order ID)
    "AdditionalFilterField": "orderId",
    "AdditionalFilterType": "StartsWithAny",
    "AdditionalFilterValues": ["SS", "SO"],

    // Error handling
    "ContinueOnError": true
  }
}
```

### Save PDF Action (Branch 1)

```json
{
  "ActionNumber": 2,
  "ActionName": "Save PDF - SS/SO Orders",
  "ActionType": "SaveCapturedHtml",
  "Configuration": {
    "ContinueOnError": true,
    "UseChainedInput": true,
    "ChainedFromActionNumber": 1,
    "OutputFilePrefix": "personalized-ss-so"
  }
}
```

### Print PDF Action (Branch 1)

```json
{
  "ActionNumber": 3,
  "ActionName": "Print PDF - SS/SO Orders (Printer 1)",
  "ActionType": "PrintSavedPdf",
  "Configuration": {
    "ContinueOnError": true,
    "UseChainedInput": true,
    "ChainedFromActionNumber": 2,
    "PrinterName": "NPI84BD10 (HP LaserJet M607)",
    "OutputFilePrefix": "personalized-ss-so"
  }
}
```

### Navigate Action (Branch 2)

```json
{
  "ActionNumber": 4,
  "ActionName": "Navigate - Non-SS/SO Custom Forms",
  "ActionType": "NavigateOnly",
  "Configuration": {
    // Array handling
    "ChainedArrayJsonPath": "data",
    "UseChainedInput": true,
    "ChainedItemFieldPath": "itemNotes",
    "IdJsonPath": "orderDetailsId",

    // Navigation
    "Endpoint": "https://mj.3plnext.com/{itemNotes}",
    "Method": "GET",
    "WaitForNetworkIdleMs": 3000,
    "MakeHiddenVisible": false,

    // Primary filter (IsFilePath)
    "ChainedFilterField": "itemNotes",
    "ChainedFilterType": "IsFilePath",
    "ChainedFilterValue": null,

    // Additional filter (Order ID)
    "AdditionalFilterField": "orderId",
    "AdditionalFilterType": "NotStartsWithAny",
    "AdditionalFilterValues": ["SS", "SO"],

    // Error handling
    "ContinueOnError": true
  }
}
```

## Migration Instructions

### Before Running Migration

1. **Backup Database:**
   ```bash
   copy "data\api_config.db" "data\api_config.db.backup-api3-$(Get-Date -Format 'yyyyMMdd-HHmmss')"
   ```

2. **Verify Current State:**
   ```sql
   SELECT p.ApiNumber, p.ApiName, s.ActionNumber, s.ActionName
   FROM PrimaryApi p
   LEFT JOIN SubAction s ON p.Id = s.PrimaryApiId
   WHERE p.ApiNumber = 3;
   ```

### Run Migration

```bash
sqlite3 "data\api_config.db" < "scripts\consolidate-api3-with-chained-filters.sql"
```

### Verify Migration

```sql
-- Check API 3 configuration
SELECT ApiNumber, ApiName, PrinterName FROM PrimaryApi WHERE ApiNumber = 3;

-- Check all sub-actions
SELECT
    ActionNumber,
    ActionName,
    ActionType,
    json_extract(Configuration, '$.ChainedFilterType') AS PrimaryFilter,
    json_extract(Configuration, '$.AdditionalFilterType') AS AdditionalFilter,
    json_extract(Configuration, '$.PrinterName') AS PrinterName
FROM SubAction
WHERE PrimaryApiId = (SELECT Id FROM PrimaryApi WHERE ApiNumber = 3)
ORDER BY ExecutionOrder;
```

## Common Operations

### Update Printer Names

```sql
-- Update Printer 1 (SS/SO orders)
UPDATE SubAction
SET Configuration = json_set(Configuration, '$.PrinterName', 'New-Printer-1')
WHERE PrimaryApiId = (SELECT Id FROM PrimaryApi WHERE ApiNumber = 3)
  AND ActionNumber = 3;

-- Update Printer 2 (Other orders)
UPDATE SubAction
SET Configuration = json_set(Configuration, '$.PrinterName', 'New-Printer-2')
WHERE PrimaryApiId = (SELECT Id FROM PrimaryApi WHERE ApiNumber = 3)
  AND ActionNumber = 6;
```

### Change Filter Criteria

```sql
-- Add "SR" prefix to Branch 1 filter
UPDATE SubAction
SET Configuration = json_set(
    Configuration,
    '$.AdditionalFilterValues',
    json('["SS", "SO", "SR"]')
)
WHERE PrimaryApiId = (SELECT Id FROM PrimaryApi WHERE ApiNumber = 3)
  AND ActionNumber = 1;
```

### Change Output File Prefixes

```sql
-- Update prefix for SS/SO orders (both Save and Print actions)
UPDATE SubAction
SET Configuration = json_set(Configuration, '$.OutputFilePrefix', 'custom-ss-orders')
WHERE PrimaryApiId = (SELECT Id FROM PrimaryApi WHERE ApiNumber = 3)
  AND ActionNumber IN (2, 3);
```

### Add Third Branch (Example: RUSH Orders)

```sql
-- Action 7: Navigate RUSH orders
INSERT INTO SubAction (PrimaryApiId, ActionNumber, ActionName, ActionType, Configuration, ExecutionOrder, IsEnabled)
VALUES (
  (SELECT Id FROM PrimaryApi WHERE ApiNumber = 3),
  7,
  'Navigate - RUSH Custom Forms',
  'NavigateOnly',
  json('{
    "ChainedArrayJsonPath": "data",
    "ChainedFilterField": "itemNotes",
    "ChainedFilterType": "IsFilePath",
    "ChainedItemFieldPath": "itemNotes",
    "Endpoint": "https://mj.3plnext.com/{itemNotes}",
    "Method": "GET",
    "WaitForNetworkIdleMs": 3000,
    "UseChainedInput": true,
    "IdJsonPath": "orderDetailsId",
    "AdditionalFilterField": "orderId",
    "AdditionalFilterType": "StartsWith",
    "AdditionalFilterValue": "RUSH",
    "ContinueOnError": true
  }'),
  7,
  1
);

-- Action 8: Save RUSH PDFs
INSERT INTO SubAction (PrimaryApiId, ActionNumber, ActionName, ActionType, Configuration, ExecutionOrder, IsEnabled)
VALUES (
  (SELECT Id FROM PrimaryApi WHERE ApiNumber = 3),
  8,
  'Save PDF - RUSH Orders',
  'SaveCapturedHtml',
  json('{
    "ContinueOnError": true,
    "UseChainedInput": true,
    "ChainedFromActionNumber": 7,
    "OutputFilePrefix": "personalized-rush"
  }'),
  8,
  1
);

-- Action 9: Print RUSH to Priority Printer
INSERT INTO SubAction (PrimaryApiId, ActionNumber, ActionName, ActionType, Configuration, ExecutionOrder, IsEnabled)
VALUES (
  (SELECT Id FROM PrimaryApi WHERE ApiNumber = 3),
  9,
  'Print PDF - RUSH Orders (Priority Printer)',
  'PrintSavedPdf',
  json('{
    "ContinueOnError": true,
    "UseChainedInput": true,
    "ChainedFromActionNumber": 8,
    "PrinterName": "Priority-Printer-Name",
    "OutputFilePrefix": "personalized-rush"
  }'),
  9,
  1
);
```

## Troubleshooting

### No Items Being Processed

**Check Primary Filter:**
```sql
-- Verify IsFilePath filter is configured
SELECT
    ActionNumber,
    ActionName,
    json_extract(Configuration, '$.ChainedFilterField') as Field,
    json_extract(Configuration, '$.ChainedFilterType') as Type
FROM SubAction
WHERE PrimaryApiId = (SELECT Id FROM PrimaryApi WHERE ApiNumber = 3)
  AND ActionType = 'NavigateOnly';
```

**Check Additional Filter:**
```sql
-- Verify additional filter values
SELECT
    ActionNumber,
    ActionName,
    json_extract(Configuration, '$.AdditionalFilterType') as Type,
    json_extract(Configuration, '$.AdditionalFilterValues') as Values
FROM SubAction
WHERE PrimaryApiId = (SELECT Id FROM PrimaryApi WHERE ApiNumber = 3)
  AND ActionType = 'NavigateOnly';
```

### Items Going to Wrong Printer

```sql
-- Check printer assignments
SELECT
    ActionNumber,
    ActionName,
    json_extract(Configuration, '$.PrinterName') as PrinterName,
    json_extract(Configuration, '$.ChainedFromActionNumber') as ChainedFrom
FROM SubAction
WHERE PrimaryApiId = (SELECT Id FROM PrimaryApi WHERE ApiNumber = 3)
  AND ActionType = 'PrintSavedPdf';
```

### PDFs Not Saving

```sql
-- Check save action configuration
SELECT
    ActionNumber,
    ActionName,
    json_extract(Configuration, '$.OutputFilePrefix') as FilePrefix,
    json_extract(Configuration, '$.ChainedFromActionNumber') as ChainedFrom
FROM SubAction
WHERE PrimaryApiId = (SELECT Id FROM PrimaryApi WHERE ApiNumber = 3)
  AND ActionType = 'SaveCapturedHtml';
```

## Best Practices

1. **Always Maintain IsFilePath Filter**: This prevents processing JSON data as file paths
2. **Use Descriptive File Prefixes**: Helps identify which branch created the PDF
3. **Test Filters Incrementally**: Enable one branch at a time initially
4. **Monitor Logs**: Check for "Item filtered out" messages
5. **Backup Before Changes**: Always backup database before running migrations
6. **Update Both Save and Print**: When changing file prefix, update both actions

## Future Enhancements

1. **Dynamic Filter Values**: Load filter criteria from configuration table
2. **Complex Combined Filters**: AND/OR combinations across multiple fields
3. **Conditional Routing**: Route based on SKU, customer, or other fields
4. **Filter Metrics Dashboard**: Track items processed by each branch
5. **Dry-Run Mode**: Test filters without actually printing
