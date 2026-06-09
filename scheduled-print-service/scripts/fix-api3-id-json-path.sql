-- Migration: Fix API #3 IdJsonPath
-- Date: 2025-12-02
-- Description: Change IdJsonPath from "orderDetailsId" to "orderDetailsId" (check actual field name in API response)
--
-- PROBLEM:
-- API #3 is failing to extract IDs with warning:
--   "Could not extract ID from record using path: [0]"
--
-- The API response shows:
--   {"orderId":9146,"orderNumber":"2102500009146",...}
--
-- Current configuration has IdJsonPath set to "orderDetailsId" but that field doesn't exist in the response.
-- Need to determine the correct field name.

-- Before: Show current configuration
SELECT
    'BEFORE' as Status,
    ApiNumber,
    ApiName,
    json_extract(Configuration, '$.IdJsonPath') as IdJsonPath,
    Configuration
FROM PrimaryApi
WHERE ApiNumber = 3;

-- Check what the correct field name should be
-- Based on the response: {"orderId":9146,"orderNumber":"2102500009146",...}
-- The ID field appears to be "orderDetailsId" (as configured)
-- But the warning suggests it's not being found

-- Let's check if ChainedArrayJsonPath is set correctly
SELECT
    ApiNumber,
    ApiName,
    json_extract(Configuration, '$.ChainedArrayJsonPath') as ChainedArrayJsonPath,
    json_extract(Configuration, '$.IdJsonPath') as IdJsonPath
FROM PrimaryApi
WHERE ApiNumber = 3;

-- EXPECTED RESULT:
-- ChainedArrayJsonPath should be "data" (where the array of orders is)
-- IdJsonPath should be "orderDetailsId" (the ID field in each order object)

-- If ChainedArrayJsonPath is missing, add it:
UPDATE PrimaryApi
SET Configuration = json_set(
    Configuration,
    '$.ChainedArrayJsonPath',
    'data'
)
WHERE ApiNumber = 3
  AND json_extract(Configuration, '$.ChainedArrayJsonPath') IS NULL;

-- After: Show updated configuration
SELECT
    'AFTER' as Status,
    ApiNumber,
    ApiName,
    json_extract(Configuration, '$.ChainedArrayJsonPath') as ChainedArrayJsonPath,
    json_extract(Configuration, '$.IdJsonPath') as IdJsonPath,
    Configuration
FROM PrimaryApi
WHERE ApiNumber = 3;

-- NOTES:
-- 1. The API response structure is: {"data": [{"orderDetailsId": 123, ...}, ...]}
-- 2. ChainedArrayJsonPath = "data" tells the code where to find the array
-- 3. IdJsonPath = "orderDetailsId" tells the code which field in each object contains the ID
-- 4. If the field name is wrong, check the actual API response and update accordingly
