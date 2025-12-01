-- Migration Script: Create API #6 as a copy of API #3 with orderRefNumber filters
--
-- This script:
-- 1. Creates API #6 as a full copy of API #3 (PrimaryApi + all SubActions)
-- 2. Adds filter to API #3: Include only orders where orderRefNumber starts with "SS" or "SO"
-- 3. Adds filter to API #6: Include orders where orderRefNumber does NOT start with "SS" or "SO",
--    OR orderRefNumber is null/empty
--
-- Created: 2025-11-30
-- Purpose: Split personalized orders processing by order reference prefix

BEGIN TRANSACTION;

-- ============================================================================
-- STEP 1: Copy API #3 to create API #6
-- ============================================================================

-- Copy PrimaryApi record
INSERT INTO PrimaryApi (
    ApiNumber,
    ApiName,
    BaseUrl,
    Endpoint,
    HttpMethod,
    Headers,
    Params,
    Payload,
    IsEnabled,
    PrinterName,
    Configuration
)
SELECT
    6 as ApiNumber,                                    -- New API number
    'Personalized Orders API (Non-SS/SO)' as ApiName,  -- Descriptive name for filtered API
    BaseUrl,
    Endpoint,
    HttpMethod,
    Headers,
    Params,
    Payload,
    IsEnabled,
    PrinterName,
    Configuration
FROM PrimaryApi
WHERE ApiNumber = 3;

-- Get the new PrimaryApi ID for API #6
-- (Will use this to create sub-actions)

-- Copy SubActions for API #6
INSERT INTO SubAction (
    PrimaryApiId,
    ActionNumber,
    ActionName,
    ActionType,
    Configuration,
    ExecutionOrder,
    IsEnabled
)
SELECT
    (SELECT Id FROM PrimaryApi WHERE ApiNumber = 6) as PrimaryApiId,  -- Reference new API #6
    ActionNumber,
    ActionName,
    ActionType,
    Configuration,
    ExecutionOrder,
    IsEnabled
FROM SubAction
WHERE PrimaryApiId = (SELECT Id FROM PrimaryApi WHERE ApiNumber = 3)
ORDER BY ExecutionOrder;

-- ============================================================================
-- STEP 2: Add filter to API #3 - Include only SS or SO orders
-- ============================================================================

-- Update API #3 Configuration to filter for orderRefNumber starting with "SS" or "SO"
UPDATE PrimaryApi
SET
    Configuration = json_set(
        COALESCE(Configuration, '{}'),
        '$.FilterField', 'orderRefNumber',
        '$.FilterType', 'StartsWithAny',
        '$.FilterValue', 'SS,SO'
    ),
    ApiName = 'Personalized Orders API (SS/SO Only)',
    UpdatedAt = CURRENT_TIMESTAMP
WHERE ApiNumber = 3;

-- ============================================================================
-- STEP 3: Add filter to API #6 - Include non-SS/SO or null/empty orderRefNumber
-- ============================================================================

-- Update API #6 Configuration to filter for orderRefNumber NOT starting with "SS" or "SO"
-- This also includes records where orderRefNumber is null or empty
UPDATE PrimaryApi
SET
    Configuration = json_set(
        COALESCE(Configuration, '{}'),
        '$.FilterField', 'orderRefNumber',
        '$.FilterType', 'NotStartsWithAny',
        '$.FilterValue', 'SS,SO'
    ),
    UpdatedAt = CURRENT_TIMESTAMP
WHERE ApiNumber = 6;

-- ============================================================================
-- VERIFICATION QUERIES
-- ============================================================================

-- View API #3 configuration
SELECT
    ApiNumber,
    ApiName,
    Configuration
FROM PrimaryApi
WHERE ApiNumber = 3;

-- View API #6 configuration
SELECT
    ApiNumber,
    ApiName,
    Configuration
FROM PrimaryApi
WHERE ApiNumber = 6;

-- View API #3 sub-actions count
SELECT
    'API #3' as Api,
    COUNT(*) as SubActionCount
FROM SubAction
WHERE PrimaryApiId = (SELECT Id FROM PrimaryApi WHERE ApiNumber = 3);

-- View API #6 sub-actions count
SELECT
    'API #6' as Api,
    COUNT(*) as SubActionCount
FROM SubAction
WHERE PrimaryApiId = (SELECT Id FROM PrimaryApi WHERE ApiNumber = 6);

-- View both APIs side by side
SELECT
    pa.ApiNumber,
    pa.ApiName,
    pa.IsEnabled,
    json_extract(pa.Configuration, '$.FilterField') as FilterField,
    json_extract(pa.Configuration, '$.FilterType') as FilterType,
    json_extract(pa.Configuration, '$.FilterValue') as FilterValue,
    COUNT(sa.Id) as SubActionCount
FROM PrimaryApi pa
LEFT JOIN SubAction sa ON sa.PrimaryApiId = pa.Id
WHERE pa.ApiNumber IN (3, 6)
GROUP BY pa.ApiNumber
ORDER BY pa.ApiNumber;

COMMIT;

-- ============================================================================
-- ROLLBACK (if needed)
-- ============================================================================

-- To undo this migration, run:
-- BEGIN TRANSACTION;
-- DELETE FROM SubAction WHERE PrimaryApiId = (SELECT Id FROM PrimaryApi WHERE ApiNumber = 6);
-- DELETE FROM PrimaryApi WHERE ApiNumber = 6;
-- UPDATE PrimaryApi SET
--     ApiName = 'Personalized Orders API',
--     Configuration = json_remove(Configuration, '$.FilterField', '$.FilterType', '$.FilterValue'),
--     UpdatedAt = CURRENT_TIMESTAMP
-- WHERE ApiNumber = 3;
-- COMMIT;

-- ============================================================================
-- IMPLEMENTATION NOTES
-- ============================================================================

-- The filter types referenced above (StartsWithAny, NotStartsWithAny) need to be
-- implemented in the code if they don't exist yet. Here's the expected behavior:
--
-- StartsWithAny:
--   - FilterValue: "SS,SO" (comma-separated prefixes)
--   - Matches if orderRefNumber starts with any of the listed prefixes
--   - Example: "SS123" matches, "SO456" matches, "XX789" does NOT match
--
-- NotStartsWithAny:
--   - FilterValue: "SS,SO" (comma-separated prefixes)
--   - Matches if orderRefNumber does NOT start with any of the listed prefixes
--   - Also matches if orderRefNumber is null or empty
--   - Example: "XX789" matches, "SS123" does NOT match, null/empty matches
--
-- These filters should be applied in the ParseOrderRecords method in SubActionExecutor.cs
-- after fetching the API response and before processing individual orders.
