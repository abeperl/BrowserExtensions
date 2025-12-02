# Quick Reference Guide - API 3 (Personalized Orders)

## Quick Commands

### Run Migration

```bash
# Backup first!
copy "data\api_config.db" "data\api_config.db.backup-api3"

# Run migration
sqlite3 "data\api_config.db" < "scripts\consolidate-api3-with-chained-filters.sql"
```

### View Current Configuration

```sql
-- Quick overview
SELECT
    s.ActionNumber,
    s.ActionName,
    s.ActionType,
    json_extract(s.Configuration, '$.ChainedFilterType') AS PrimaryFilter,
    json_extract(s.Configuration, '$.AdditionalFilterType') AS AdditionalFilter,
    json_extract(s.Configuration, '$.AdditionalFilterValues') AS FilterValues,
    json_extract(s.Configuration, '$.PrinterName') AS Printer,
    json_extract(s.Configuration, '$.OutputFilePrefix') AS FilePrefix
FROM SubAction s
WHERE s.PrimaryApiId = (SELECT Id FROM PrimaryApi WHERE ApiNumber = 3)
ORDER BY s.ExecutionOrder;
```

### Visual Chain Diagram

```sql
-- See how actions chain together
SELECT
    s1.ActionNumber || ': ' || s1.ActionName AS Action,
    CASE
        WHEN json_extract(s1.Configuration, '$.ChainedFromActionNumber') IS NULL
        THEN 'ROOT (processes API response)'
        ELSE 'Chained from #' || json_extract(s1.Configuration, '$.ChainedFromActionNumber') || ': ' || s2.ActionName
    END AS Source
FROM SubAction s1
LEFT JOIN SubAction s2 ON
    s2.PrimaryApiId = s1.PrimaryApiId AND
    s2.ActionNumber = json_extract(s1.Configuration, '$.ChainedFromActionNumber')
WHERE s1.PrimaryApiId = (SELECT Id FROM PrimaryApi WHERE ApiNumber = 3)
ORDER BY s1.ExecutionOrder;
```

## Common Operations

### Update Printer Names

```sql
-- Change Printer 1 (SS/SO orders)
UPDATE SubAction
SET Configuration = json_set(Configuration, '$.PrinterName', 'HP LaserJet M404')
WHERE PrimaryApiId = (SELECT Id FROM PrimaryApi WHERE ApiNumber = 3)
  AND ActionNumber = 3;

-- Change Printer 2 (Other orders)
UPDATE SubAction
SET Configuration = json_set(Configuration, '$.PrinterName', 'Canon MF445dw')
WHERE PrimaryApiId = (SELECT Id FROM PrimaryApi WHERE ApiNumber = 3)
  AND ActionNumber = 6;
```

### Change Filter Prefixes

```sql
-- Add "SR" and "SX" to SS/SO filter
UPDATE SubAction
SET Configuration = json_set(
    Configuration,
    '$.AdditionalFilterValues',
    json('["SS", "SO", "SR", "SX"]')
)
WHERE PrimaryApiId = (SELECT Id FROM PrimaryApi WHERE ApiNumber = 3)
  AND ActionNumber = 1;

-- Update the inverse filter accordingly
UPDATE SubAction
SET Configuration = json_set(
    Configuration,
    '$.AdditionalFilterValues',
    json('["SS", "SO", "SR", "SX"]')
)
WHERE PrimaryApiId = (SELECT Id FROM PrimaryApi WHERE ApiNumber = 3)
  AND ActionNumber = 4;
```

### Change Output File Prefixes

```sql
-- Change prefix for SS/SO branch (Actions 2 and 3)
UPDATE SubAction
SET Configuration = json_set(Configuration, '$.OutputFilePrefix', 'special-orders')
WHERE PrimaryApiId = (SELECT Id FROM PrimaryApi WHERE ApiNumber = 3)
  AND ActionNumber IN (2, 3);

-- Change prefix for Other branch (Actions 5 and 6)
UPDATE SubAction
SET Configuration = json_set(Configuration, '$.OutputFilePrefix', 'standard-orders')
WHERE PrimaryApiId = (SELECT Id FROM PrimaryApi WHERE ApiNumber = 3)
  AND ActionNumber IN (5, 6);
```

### Disable/Enable Branches

```sql
-- Disable entire Branch 2 (Non-SS/SO orders)
UPDATE SubAction
SET IsEnabled = 0
WHERE PrimaryApiId = (SELECT Id FROM PrimaryApi WHERE ApiNumber = 3)
  AND ActionNumber IN (4, 5, 6);

-- Re-enable Branch 2
UPDATE SubAction
SET IsEnabled = 1
WHERE PrimaryApiId = (SELECT Id FROM PrimaryApi WHERE ApiNumber = 3)
  AND ActionNumber IN (4, 5, 6);

-- Disable only printing for Branch 1 (keep navigation and save)
UPDATE SubAction
SET IsEnabled = 0
WHERE PrimaryApiId = (SELECT Id FROM PrimaryApi WHERE ApiNumber = 3)
  AND ActionNumber = 3;
```

## Filter Configuration

### Primary Filter (IsFilePath)

**Always Applied** - Ensures itemNotes contains a valid file path:

```json
{
  "ChainedFilterField": "itemNotes",
  "ChainedFilterType": "IsFilePath",
  "ChainedFilterValue": null
}
```

**Matches:**
- `/custom/forms/order.html` ✓
- `https://site.com/form.html` ✓

**Rejects:**
- `{"json": "data"}` ✗
- `regular text` ✗

### Additional Filters

#### StartsWithAny (Branch 1)
```json
{
  "AdditionalFilterField": "orderId",
  "AdditionalFilterType": "StartsWithAny",
  "AdditionalFilterValues": ["SS", "SO"]
}
```

#### NotStartsWithAny (Branch 2)
```json
{
  "AdditionalFilterField": "orderId",
  "AdditionalFilterType": "NotStartsWithAny",
  "AdditionalFilterValues": ["SS", "SO"]
}
```

#### Single Value Filters
```json
// StartsWith (single prefix)
{
  "AdditionalFilterField": "orderId",
  "AdditionalFilterType": "StartsWith",
  "AdditionalFilterValue": "RUSH"
}

// Contains (substring)
{
  "AdditionalFilterField": "orderId",
  "AdditionalFilterType": "Contains",
  "AdditionalFilterValue": "SPECIAL"
}

// Equals (exact match)
{
  "AdditionalFilterField": "sku",
  "AdditionalFilterType": "Equals",
  "AdditionalFilterValue": "CUSTOM-001"
}
```

## Adding New Branches

### Example: Add RUSH Orders Branch

```sql
BEGIN TRANSACTION;

-- Get next available action numbers (current max is 6, so start at 7)

-- Action 7: Navigate RUSH orders
INSERT INTO SubAction (
    PrimaryApiId,
    ActionNumber,
    ActionName,
    ActionType,
    Configuration,
    ExecutionOrder,
    IsEnabled
)
VALUES (
    (SELECT Id FROM PrimaryApi WHERE ApiNumber = 3),
    7,
    'Navigate - RUSH Custom Forms',
    'NavigateOnly',
    json('{
        "ChainedArrayJsonPath": "data",
        "ChainedFilterField": "itemNotes",
        "ChainedFilterType": "IsFilePath",
        "ChainedFilterValue": null,
        "ChainedItemFieldPath": "itemNotes",
        "Endpoint": "https://mj.3plnext.com/{itemNotes}",
        "Method": "GET",
        "WaitForNetworkIdleMs": 3000,
        "MakeHiddenVisible": false,
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
INSERT INTO SubAction (
    PrimaryApiId,
    ActionNumber,
    ActionName,
    ActionType,
    Configuration,
    ExecutionOrder,
    IsEnabled
)
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
INSERT INTO SubAction (
    PrimaryApiId,
    ActionNumber,
    ActionName,
    ActionType,
    Configuration,
    ExecutionOrder,
    IsEnabled
)
VALUES (
    (SELECT Id FROM PrimaryApi WHERE ApiNumber = 3),
    9,
    'Print PDF - RUSH Orders (Priority Printer)',
    'PrintSavedPdf',
    json('{
        "ContinueOnError": true,
        "UseChainedInput": true,
        "ChainedFromActionNumber": 8,
        "PrinterName": "Express-Printer-Name",
        "OutputFilePrefix": "personalized-rush"
    }'),
    9,
    1
);

COMMIT;
```

### Example: Filter by SKU Instead of Order ID

```sql
-- Add branch for specific SKU prefix
INSERT INTO SubAction (PrimaryApiId, ActionNumber, ActionName, ActionType, Configuration, ExecutionOrder, IsEnabled)
VALUES (
    (SELECT Id FROM PrimaryApi WHERE ApiNumber = 3),
    10,
    'Navigate - BMX SKU Custom Forms',
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
        "AdditionalFilterField": "sku",
        "AdditionalFilterType": "StartsWith",
        "AdditionalFilterValue": "BMX",
        "ContinueOnError": true
    }'),
    10,
    1
);

-- Add corresponding Save and Print actions (11, 12)...
```

## Testing Queries

### Simulate Filter Logic

```sql
-- Test data
WITH test_items AS (
    SELECT 'SS1234' as orderId, '/custom/form1.html' as itemNotes UNION ALL
    SELECT 'SO5678', '/custom/form2.html' UNION ALL
    SELECT 'SM9999', '/custom/form3.html' UNION ALL
    SELECT 'SS7777', '{"json":"data"}' UNION ALL
    SELECT 'AB1111', '/custom/form4.html'
)
SELECT
    orderId,
    itemNotes,
    CASE
        WHEN itemNotes LIKE '%.html' AND itemNotes NOT LIKE '{%' AND itemNotes NOT LIKE '[%'
        THEN 'PASS'
        ELSE 'FAIL'
    END as IsFilePath_Result,
    CASE
        WHEN orderId LIKE 'SS%' OR orderId LIKE 'SO%'
        THEN 'Branch 1'
        ELSE 'Branch 2'
    END as Would_Route_To
FROM test_items
WHERE itemNotes LIKE '%.html'
  AND itemNotes NOT LIKE '{%'
  AND itemNotes NOT LIKE '[%';
```

### Count Items Per Branch (From Logs)

After running the service, analyze which branch processed how many items:

```sql
-- This is conceptual - actual implementation depends on logging
SELECT
    'Branch 1 (SS/SO)' as Branch,
    COUNT(*) as Items_Processed
FROM logs
WHERE action_name LIKE '%SS/SO%'
UNION ALL
SELECT
    'Branch 2 (Other)',
    COUNT(*)
FROM logs
WHERE action_name LIKE '%Non-SS/SO%';
```

### Check for Broken Chains

```sql
-- Find actions with invalid ChainedFromActionNumber
SELECT
    s1.ActionNumber,
    s1.ActionName,
    json_extract(s1.Configuration, '$.ChainedFromActionNumber') as ChainedFrom,
    'BROKEN - Parent action does not exist' as Status
FROM SubAction s1
WHERE s1.PrimaryApiId = (SELECT Id FROM PrimaryApi WHERE ApiNumber = 3)
  AND json_extract(s1.Configuration, '$.ChainedFromActionNumber') IS NOT NULL
  AND NOT EXISTS (
      SELECT 1
      FROM SubAction s2
      WHERE s2.PrimaryApiId = s1.PrimaryApiId
        AND s2.ActionNumber = json_extract(s1.Configuration, '$.ChainedFromActionNumber')
  );

-- Should return no rows (empty = all chains valid)
```

## Troubleshooting

### Check Filter Configuration

```sql
-- Verify all Navigate actions have both filters
SELECT
    ActionNumber,
    ActionName,
    CASE
        WHEN json_extract(Configuration, '$.ChainedFilterType') IS NULL
        THEN '⚠ MISSING PRIMARY FILTER'
        ELSE json_extract(Configuration, '$.ChainedFilterType')
    END as PrimaryFilter,
    CASE
        WHEN json_extract(Configuration, '$.AdditionalFilterType') IS NULL
        THEN '⚠ MISSING ADDITIONAL FILTER'
        ELSE json_extract(Configuration, '$.AdditionalFilterType')
    END as AdditionalFilter,
    json_extract(Configuration, '$.AdditionalFilterValues') as FilterValues
FROM SubAction
WHERE PrimaryApiId = (SELECT Id FROM PrimaryApi WHERE ApiNumber = 3)
  AND ActionType = 'NavigateOnly'
ORDER BY ActionNumber;
```

### Check Printer Assignments

```sql
-- Verify all print actions have printer names
SELECT
    ActionNumber,
    ActionName,
    CASE
        WHEN json_extract(Configuration, '$.PrinterName') IS NULL
        THEN '⚠ NO PRINTER ASSIGNED'
        ELSE json_extract(Configuration, '$.PrinterName')
    END as PrinterName,
    json_extract(Configuration, '$.OutputFilePrefix') as FilePrefix
FROM SubAction
WHERE PrimaryApiId = (SELECT Id FROM PrimaryApi WHERE ApiNumber = 3)
  AND ActionType = 'PrintSavedPdf'
ORDER BY ActionNumber;
```

### Check File Prefix Consistency

```sql
-- Ensure Save and Print actions in same branch use same prefix
SELECT
    'Branch 1' as Branch,
    (SELECT json_extract(Configuration, '$.OutputFilePrefix')
     FROM SubAction
     WHERE PrimaryApiId = (SELECT Id FROM PrimaryApi WHERE ApiNumber = 3)
       AND ActionNumber = 2) as Save_Prefix,
    (SELECT json_extract(Configuration, '$.PrinterName')
     FROM SubAction
     WHERE PrimaryApiId = (SELECT Id FROM PrimaryApi WHERE ApiNumber = 3)
       AND ActionNumber = 3) as Print_Prefix,
    CASE
        WHEN (SELECT json_extract(Configuration, '$.OutputFilePrefix')
              FROM SubAction WHERE PrimaryApiId = (SELECT Id FROM PrimaryApi WHERE ApiNumber = 3) AND ActionNumber = 2)
             =
             (SELECT json_extract(Configuration, '$.OutputFilePrefix')
              FROM SubAction WHERE PrimaryApiId = (SELECT Id FROM PrimaryApi WHERE ApiNumber = 3) AND ActionNumber = 3)
        THEN '✓ Consistent'
        ELSE '⚠ MISMATCH'
    END as Status
UNION ALL
SELECT
    'Branch 2',
    (SELECT json_extract(Configuration, '$.OutputFilePrefix')
     FROM SubAction WHERE PrimaryApiId = (SELECT Id FROM PrimaryApi WHERE ApiNumber = 3) AND ActionNumber = 5),
    (SELECT json_extract(Configuration, '$.OutputFilePrefix')
     FROM SubAction WHERE PrimaryApiId = (SELECT Id FROM PrimaryApi WHERE ApiNumber = 3) AND ActionNumber = 6),
    CASE
        WHEN (SELECT json_extract(Configuration, '$.OutputFilePrefix')
              FROM SubAction WHERE PrimaryApiId = (SELECT Id FROM PrimaryApi WHERE ApiNumber = 3) AND ActionNumber = 5)
             =
             (SELECT json_extract(Configuration, '$.OutputFilePrefix')
              FROM SubAction WHERE PrimaryApiId = (SELECT Id FROM PrimaryApi WHERE ApiNumber = 3) AND ActionNumber = 6)
        THEN '✓ Consistent'
        ELSE '⚠ MISMATCH'
    END;
```

## Export/Import Configuration

### Export Configuration to JSON

```bash
sqlite3 "data\api_config.db" << 'EOF'
.mode json
.output api3-config-backup.json
SELECT
    json_object(
        'ApiNumber', p.ApiNumber,
        'ApiName', p.ApiName,
        'Endpoint', p.Endpoint,
        'SubActions', (
            SELECT json_group_array(
                json_object(
                    'ActionNumber', s.ActionNumber,
                    'ActionName', s.ActionName,
                    'ActionType', s.ActionType,
                    'ExecutionOrder', s.ExecutionOrder,
                    'IsEnabled', s.IsEnabled,
                    'Configuration', json(s.Configuration)
                )
            )
            FROM SubAction s
            WHERE s.PrimaryApiId = p.Id
            ORDER BY s.ExecutionOrder
        )
    ) as config
FROM PrimaryApi p
WHERE p.ApiNumber = 3;
.output stdout
EOF
```

### Reset to Default Configuration

```sql
-- WARNING: Deletes all sub-actions and recreates defaults
-- Backup first!

BEGIN TRANSACTION;

DELETE FROM SubAction WHERE PrimaryApiId = (SELECT Id FROM PrimaryApi WHERE ApiNumber = 3);

-- Re-run the INSERT statements from consolidate-api3-with-chained-filters.sql
-- (See migration script Step 5 and Step 6)

COMMIT;
```

## Performance Tips

1. **Reduce WaitForNetworkIdleMs**: If custom forms load quickly, reduce wait time
2. **Disable Unused Branches**: If you don't have certain order types, disable those actions
3. **Use Specific Filters**: More specific filters process fewer items
4. **Monitor PDF Sizes**: Large PDFs take longer to save and print
5. **Optimize ContinueOnError**: Set to false for critical actions if you want to catch errors

## Common Patterns

### Pattern 1: Split by Order Prefix
- SS/SO → Printer 1
- Everything else → Printer 2
- **Used in:** Default migration

### Pattern 2: Split by SKU
- Certain SKU prefixes → Specialized printer
- Other SKUs → Standard printer

### Pattern 3: Priority Routing
- RUSH prefix → Express printer (fast)
- BULK prefix → Bulk printer (slow, high capacity)
- Standard → Normal printer

### Pattern 4: Customer-Specific Routing
- Filter by customer field
- Route to customer-dedicated printer

## Getting Help

### View Full Schema
```sql
.schema SubAction
PRAGMA table_info(SubAction);
```

### Check Database Version
```bash
sqlite3 --version
# Need 3.38+ for JSON functions
```

### Validate JSON Configuration
```sql
-- Check if all configurations are valid JSON
SELECT
    ActionNumber,
    ActionName,
    CASE
        WHEN json_valid(Configuration) = 1
        THEN '✓ Valid JSON'
        ELSE '⚠ INVALID JSON'
    END as JSON_Status
FROM SubAction
WHERE PrimaryApiId = (SELECT Id FROM PrimaryApi WHERE ApiNumber = 3);
```
