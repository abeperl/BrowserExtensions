-- Fix API #7: Move Payload from Params column to Payload column AND fix response path
-- Date: 2025-12-23
-- Issue 1: The original create-api-7-sales-orders.sql had Params and Payload columns swapped
-- Issue 2: ChainedArrayJsonPath was set to 'data' but should be 'response.result.data'
-- Issue 3: IdJsonPath was set to '[22]' but should be '[5]' (property "5" contains the SaleOrderId)
-- This script fixes existing database installations by moving the data to the correct column

-- Move Params data to Payload column for API #7
UPDATE PrimaryApi
SET
    Payload = Params,
    Params = NULL,
    UpdatedAt = CURRENT_TIMESTAMP
WHERE ApiNumber = 7
  AND (Payload IS NULL OR Payload = '')
  AND Params IS NOT NULL
  AND Params != '';

-- Fix the ChainedArrayJsonPath in Configuration to correct response path
UPDATE PrimaryApi
SET
    Configuration = json_object(
        'IdJsonPath', json_extract(Configuration, '$.IdJsonPath'),
        'ChainedArrayJsonPath', 'response.result.data'
    ),
    UpdatedAt = CURRENT_TIMESTAMP
WHERE ApiNumber = 7
  AND json_extract(Configuration, '$.ChainedArrayJsonPath') = 'data';

-- Fix the IdJsonPath to use property "5" (SaleOrderId) instead of "[22]" or "[0]"
UPDATE PrimaryApi
SET
    Configuration = json_object(
        'IdJsonPath', '[5]',
        'ChainedArrayJsonPath', json_extract(Configuration, '$.ChainedArrayJsonPath')
    ),
    UpdatedAt = CURRENT_TIMESTAMP
WHERE ApiNumber = 7
  AND (json_extract(Configuration, '$.IdJsonPath') = '[22]' OR json_extract(Configuration, '$.IdJsonPath') = '[0]');

-- Verify the fix
SELECT
    ApiNumber,
    ApiName,
    length(Params) as ParamsLength,
    length(Payload) as PayloadLength,
    substr(Payload, 1, 100) as PayloadPreview,
    Configuration
FROM PrimaryApi
WHERE ApiNumber = 7;