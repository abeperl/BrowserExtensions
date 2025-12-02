-- Migration: Fix API #2 Filter Conflict
-- Date: 2025-12-02
-- Description: Removes the primary API filter from API #2 to allow subaction filters to work correctly
--
-- PROBLEM:
-- API #2 has a primary API filter that keeps ONLY orders starting with "SS" or "SO"
-- But subaction #4 wants to process orders that DON'T start with "SS" or "SO"
-- Result: Primary API filters out 110 orders, keeps 83 SS/SO orders
--         Then subaction #4 skips all 83 because they're SS/SO orders
--         Net result: Nothing gets processed
--
-- SOLUTION:
-- Remove the primary API filter so all 193 orders are kept
-- Let the subaction filters decide which orders to process:
--   - Actions 1-3: Process SS/SO orders only
--   - Actions 4-6: Process Non-SS/SO orders only

-- Before: Show current configuration
SELECT
    'BEFORE' as Status,
    ApiNumber,
    ApiName,
    json_extract(Configuration, '$.ChainedFilterType') as PrimaryFilterType,
    json_extract(Configuration, '$.ChainedFilterValue') as PrimaryFilterValue,
    json_extract(Configuration, '$.ChainedFilterArrayIndex') as PrimaryFilterArrayIndex
FROM PrimaryApi
WHERE ApiNumber = 2;

-- Remove the primary API filter fields from Configuration
UPDATE PrimaryApi
SET Configuration = (
    SELECT json_remove(
        json_remove(
            json_remove(
                Configuration,
                '$.ChainedFilterType'
            ),
            '$.ChainedFilterValue'
        ),
        '$.ChainedFilterArrayIndex'
    )
)
WHERE ApiNumber = 2;

-- After: Show updated configuration
SELECT
    'AFTER' as Status,
    ApiNumber,
    ApiName,
    Configuration,
    json_extract(Configuration, '$.ChainedFilterType') as PrimaryFilterType,
    json_extract(Configuration, '$.ChainedFilterValue') as PrimaryFilterValue,
    json_extract(Configuration, '$.ChainedFilterArrayIndex') as PrimaryFilterArrayIndex
FROM PrimaryApi
WHERE ApiNumber = 2;

-- Verify subaction filters are still in place
SELECT
    ActionNumber,
    ActionName,
    json_extract(Configuration, '$.ChainedFilterType') as FilterType,
    json_extract(Configuration, '$.ChainedFilterValues') as FilterValues,
    json_extract(Configuration, '$.ChainedFilterArrayIndex') as ArrayIndex
FROM SubAction
WHERE PrimaryApiId = (SELECT Id FROM PrimaryApi WHERE ApiNumber = 2)
  AND ActionType = 'NavigateOnly'
ORDER BY ActionNumber;

-- EXPECTED RESULT AFTER FIX:
-- API #2 will fetch all 193 orders
-- Subaction 1-3 (SS/SO Orders): Will process ~83 orders that start with "SS" or "SO"
-- Subaction 4-6 (Non-SS/SO Orders): Will process ~110 orders that DON'T start with "SS" or "SO"
-- Total: All 193 orders will be processed (no skips)

-- NOTES:
-- 1. The primary API filter was redundant because subactions already have the same filters
-- 2. Primary API filters should be used for performance optimization (reduce API payload)
--    or when ALL subactions need the same filter
-- 3. When different subactions need different filters, use subaction-level filters only
-- 4. This change will increase the API response size from 83 to 193 records per call
-- 5. Processing time will increase slightly but all orders will now be handled correctly
