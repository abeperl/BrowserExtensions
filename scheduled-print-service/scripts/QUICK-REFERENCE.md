# Quick Reference Guide - Filter and Chain Operations

## Common Operations

### Run the Migration

```bash
# Backup first!
copy "data\api_config.db" "data\api_config.db.backup"

# Run migration
sqlite3 "data\api_config.db" < "scripts\consolidate-apis-with-chained-filters.sql"
```

### View Current Configuration

```sql
-- See all sub-actions for API 2
SELECT
    s.ActionNumber,
    s.ActionName,
    s.ActionType,
    json_extract(s.Configuration, '$.ChainedFilterType') AS FilterType,
    json_extract(s.Configuration, '$.ChainedFilterValues') AS FilterValues,
    json_extract(s.Configuration, '$.PrinterName') AS PrinterName,
    json_extract(s.Configuration, '$.ChainedFromActionNumber') AS ChainedFrom
FROM SubAction s
JOIN PrimaryApi p ON s.PrimaryApiId = p.Id
WHERE p.ApiNumber = 2
ORDER BY s.ExecutionOrder;
```

### Update Printer Names

```sql
-- Change Printer 1 (SS/SO orders)
UPDATE SubAction
SET Configuration = json_set(Configuration, '$.PrinterName', 'HP LaserJet Pro M404')
WHERE PrimaryApiId = (SELECT Id FROM PrimaryApi WHERE ApiNumber = 2)
  AND ActionNumber = 2;

-- Change Printer 2 (Other orders)
UPDATE SubAction
SET Configuration = json_set(Configuration, '$.PrinterName', 'Canon imageRUNNER')
WHERE PrimaryApiId = (SELECT Id FROM PrimaryApi WHERE ApiNumber = 2)
  AND ActionNumber = 4;
```

### Change Filter Criteria

```sql
-- Change to filter for "SS", "SO", and "SR" prefixes
UPDATE SubAction
SET Configuration = json_set(
    Configuration,
    '$.ChainedFilterValues',
    json('["SS", "SO", "SR"]')
)
WHERE PrimaryApiId = (SELECT Id FROM PrimaryApi WHERE ApiNumber = 2)
  AND ActionNumber = 1;
```

### Disable/Enable Sub-Actions

```sql
-- Disable printing to Printer 2
UPDATE SubAction
SET IsEnabled = 0
WHERE PrimaryApiId = (SELECT Id FROM PrimaryApi WHERE ApiNumber = 2)
  AND ActionNumber = 4;

-- Re-enable it
UPDATE SubAction
SET IsEnabled = 1
WHERE PrimaryApiId = (SELECT Id FROM PrimaryApi WHERE ApiNumber = 2)
  AND ActionNumber = 4;
```

### Add New Filter Branch

```sql
-- Example: Add filter for "RUSH" orders to go to Printer 3

-- Step 1: Add filter action
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
    (SELECT Id FROM PrimaryApi WHERE ApiNumber = 2),
    5,
    'Navigate - RUSH Orders',
    'NavigateOnly',
    json('{
        "ChainedArrayJsonPath": "data",
        "UseChainedInput": true,
        "ChainedItemFieldPath": "[0]",
        "Endpoint": "https://mj.3plnext.com/#Outbound/ManualPicking?id={id}",
        "Method": "GET",
        "WaitForNetworkIdleMs": 3000,
        "MakeHiddenVisible": true,
        "ContinueOnError": true,
        "ChainedFilterArrayIndex": 17,
        "ChainedFilterType": "StartsWith",
        "ChainedFilterValue": "RUSH"
    }'),
    5,
    1
);

-- Step 2: Add printer action chained from filter
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
    (SELECT Id FROM PrimaryApi WHERE ApiNumber = 2),
    6,
    'Print RUSH Orders - Printer 3',
    'PrintCapturedHtml',
    json('{
        "ChainedItemFieldPath": "[0]",
        "UseChainedInput": true,
        "Method": "GET",
        "ContinueOnError": true,
        "HtmlJsonPath": "html",
        "Endpoint": "/api/Picklist/GetPicklistHtml/{id}",
        "OutputFilePrefix": "picklist-rush",
        "PrinterName": "Printer3Name",
        "ChainedFromActionNumber": 5
    }'),
    6,
    1
);
```

### Filter Based on Different Column

```sql
-- Change from column 17 to column 10
UPDATE SubAction
SET Configuration = json_set(Configuration, '$.ChainedFilterArrayIndex', 10)
WHERE PrimaryApiId = (SELECT Id FROM PrimaryApi WHERE ApiNumber = 2)
  AND ActionType = 'NavigateOnly';
```

### Change Filter Logic

```sql
-- Switch from "StartsWith SS or SO" to "Contains SPECIAL"
UPDATE SubAction
SET Configuration = json_set(
    json_set(
        Configuration,
        '$.ChainedFilterType',
        'Contains'
    ),
    '$.ChainedFilterValue',
    'SPECIAL'
)
WHERE PrimaryApiId = (SELECT Id FROM PrimaryApi WHERE ApiNumber = 2)
  AND ActionNumber = 1;

-- Note: Single value filters use ChainedFilterValue (singular)
-- Multiple value filters use ChainedFilterValues (plural, JSON array)
```

## Filter Type Reference

### Single Value Filters

```sql
-- StartsWith
'{"ChainedFilterType": "StartsWith", "ChainedFilterValue": "SS"}'

-- NotStartsWith
'{"ChainedFilterType": "NotStartsWith", "ChainedFilterValue": "SS"}'

-- Equals
'{"ChainedFilterType": "Equals", "ChainedFilterValue": "SS1234"}'

-- Contains
'{"ChainedFilterType": "Contains", "ChainedFilterValue": "SPECIAL"}'
```

### Multiple Value Filters

```sql
-- StartsWithAny (matches if starts with ANY of the values)
'{"ChainedFilterType": "StartsWithAny", "ChainedFilterValues": ["SS", "SO", "SR"]}'

-- NotStartsWithAny (matches if does NOT start with ANY of the values)
'{"ChainedFilterType": "NotStartsWithAny", "ChainedFilterValues": ["SS", "SO"]}'
```

## Testing Queries

### Count Rows by Filter

This helps verify your filter logic before enabling:

```sql
-- Simulate filter: Count how many rows would match "SS" or "SO"
-- (You'll need to run this against your actual API response data)

-- Example with sample data:
WITH sample_data AS (
    SELECT 'SS1234' as orderId UNION ALL
    SELECT 'SO5678' UNION ALL
    SELECT 'SM9999' UNION ALL
    SELECT 'AB1111'
)
SELECT
    COUNT(*) as matches,
    'Starts with SS or SO' as filter_description
FROM sample_data
WHERE orderId LIKE 'SS%' OR orderId LIKE 'SO%';

-- Expected result: 2 matches
```

### View Execution Order

```sql
SELECT
    ActionNumber,
    ActionName,
    ExecutionOrder,
    json_extract(Configuration, '$.ChainedFromActionNumber') as ChainedFrom,
    CASE
        WHEN json_extract(Configuration, '$.ChainedFromActionNumber') IS NULL
        THEN 'Root Action'
        ELSE 'Chained from #' || json_extract(Configuration, '$.ChainedFromActionNumber')
    END as ActionType
FROM SubAction
WHERE PrimaryApiId = (SELECT Id FROM PrimaryApi WHERE ApiNumber = 2)
ORDER BY ExecutionOrder;
```

### Check for Broken Chains

```sql
-- Find chained actions where the parent action doesn't exist
SELECT
    s1.ActionNumber,
    s1.ActionName,
    json_extract(s1.Configuration, '$.ChainedFromActionNumber') as ChainedFrom
FROM SubAction s1
WHERE s1.PrimaryApiId = (SELECT Id FROM PrimaryApi WHERE ApiNumber = 2)
  AND json_extract(s1.Configuration, '$.ChainedFromActionNumber') IS NOT NULL
  AND NOT EXISTS (
      SELECT 1
      FROM SubAction s2
      WHERE s2.PrimaryApiId = s1.PrimaryApiId
        AND s2.ActionNumber = json_extract(s1.Configuration, '$.ChainedFromActionNumber')
  );

-- Should return no rows (empty result = all chains valid)
```

## Troubleshooting Commands

### Reset to Default Configuration

```sql
-- WARNING: This deletes all sub-actions and recreates defaults
-- Backup first!

BEGIN TRANSACTION;

DELETE FROM SubAction WHERE PrimaryApiId = (SELECT Id FROM PrimaryApi WHERE ApiNumber = 2);

-- Re-run the INSERT statements from the migration script
-- (See consolidate-apis-with-chained-filters.sql Step 5)

COMMIT;
```

### Export Current Configuration

```bash
# Export to JSON for backup/review
sqlite3 "data\api_config.db" << EOF
.mode json
SELECT
    p.ApiNumber,
    p.ApiName,
    json_group_array(
        json_object(
            'ActionNumber', s.ActionNumber,
            'ActionName', s.ActionName,
            'ActionType', s.ActionType,
            'Configuration', json(s.Configuration),
            'ExecutionOrder', s.ExecutionOrder
        )
    ) as SubActions
FROM PrimaryApi p
LEFT JOIN SubAction s ON p.Id = s.PrimaryApiId
WHERE p.ApiNumber = 2
GROUP BY p.Id;
EOF
```

### Check SQLite Version (For JSON Support)

```bash
sqlite3 --version
# Need version 3.38+ for full JSON support
```

## Common Patterns

### Pattern 1: Split by Prefix
Route different order prefixes to different printers:
- "SS", "SO" → Printer 1
- Everything else → Printer 2

**Used in:** Default migration configuration

### Pattern 2: Route Special Orders
Catch special order types first, everything else to default:
- "RUSH" → Express Printer
- "BULK" → Bulk Printer
- Everything else → Standard Printer

**Implementation:** Add RUSH filter first, BULK filter second, then NotStartsWithAny ["RUSH", "BULK"] for others

### Pattern 3: Multi-Column Filtering
Filter based on multiple columns:
- Column 17 (OrderId) starts with "SS"
- AND Column 10 (Priority) equals "HIGH"

**Note:** Currently requires multiple chained filters or application-level logic

### Pattern 4: Conditional Printer Selection
Same content, different printer based on criteria:
- Navigate once
- Chain to multiple print actions with different filters
- Each uses different printer

**Benefit:** Navigates only once, prints to appropriate printer based on data

## Performance Tips

1. **Order Filters by Frequency**: Put most common filters first
2. **Use Specific Filters**: "StartsWith" is faster than "Contains"
3. **Enable Only Needed Actions**: Disable unused filter branches
4. **Reduce Wait Times**: Lower `WaitForNetworkIdleMs` if pages load quickly
5. **Batch Similar Orders**: Group by printer to reduce printer switching

## Getting Help

### View Full Schema
```sql
.schema PrimaryApi
.schema SubAction
```

### Check Database Integrity
```bash
sqlite3 "data\api_config.db" "PRAGMA integrity_check;"
```

### View All Configuration as JSON
```sql
.mode json
.output config-backup.json
SELECT * FROM PrimaryApi;
SELECT * FROM SubAction;
.output stdout
```
