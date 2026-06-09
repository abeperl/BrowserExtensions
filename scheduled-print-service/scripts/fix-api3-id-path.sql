-- Fix API #3 IdJsonPath configuration
-- The API returns items with "orderDetailsId" field, so we need to extract that
-- Current configuration uses "[0]" which treats items as arrays (incorrect)
-- Should be "orderDetailsId" to extract the property value

UPDATE PrimaryApi
SET Configuration = json_set(
    COALESCE(Configuration, '{}'),
    '$.IdJsonPath',
    'orderDetailsId'
)
WHERE ApiNumber = 3;

-- Verify the update
SELECT ApiNumber, ApiName, Configuration
FROM PrimaryApi
WHERE ApiNumber = 3;
