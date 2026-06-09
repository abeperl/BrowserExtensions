-- Migration: Add SubAction 2 for API #15 - JSON Flattening
-- Date: 2026-01-20
-- Description:
--   Adds the new FlattenJsonToFile action that transforms nested JSON structure
--   from combine_glinvoices_raw folder into flat JSON with top-level context fields.
--
--   Input: JSON files in combine_glinvoices_raw (from SubAction 1 output)
--   Output: Flattened JSON to combine_glinvoices with customerId, customerName, referenceNo, orderNumber, generatedAt, items
--
-- ============================================================================
-- First, update SubAction 1 to output to combine_glinvoices_raw (intermediate folder)
-- ============================================================================
UPDATE SubAction
SET Configuration = json_object(
    'Endpoint', 'combine_glinvoices_raw',
    'ChainedArrayJsonPath', 'response.result.items',
    'ChainedFilterField', 'storeId',
    'ChainedFilterValues', json_array('5'),
    'AdditionalFilterField', 'saleOrderId',
    'AdditionalFilterType', 'NotEmpty',
    'AdditionalFilterValue', '0',
    'RequestBody', 'saleOrderId',
    'OutputVariableName', 'C:\ProgramData\ScheduledPrintService\out\sales-orders\Sales',
    'ChainedFilterField2', 'referenceNo',
    'ChainedFilterType2', 'StartsWithValue',
    'ChainedFilterValue2', 'SS'
)
WHERE PrimaryApiId = (SELECT Id FROM PrimaryApi WHERE ApiNumber = 15)
  AND ActionType = 'CombineOrderItems';

-- ============================================================================
-- Sub-Action 2: Flatten JSON to File
-- Reads order files from combine_glinvoices_raw and outputs to combine_glinvoices
-- ============================================================================
INSERT OR REPLACE INTO SubAction (
    PrimaryApiId,
    ActionNumber,
    ActionName,
    ActionType,
    Configuration,
    ExecutionOrder,
    IsEnabled
) VALUES (
    (SELECT Id FROM PrimaryApi WHERE ApiNumber = 15),
    2,
    'Flatten JSON Structure',
    'FlattenJsonToFile',
    json_object(
        'LocalJsonFolderPath', 'combine_glinvoices_raw',
        'OutputFolder', 'combine_glinvoices',
        'Description', 'Reads from combine_glinvoices_raw and outputs flattened JSON to combine_glinvoices'
    ),
    2,
    1
);

-- ============================================================================
-- Verification: View updated API 15 and all SubActions
-- ============================================================================
SELECT 
    'SubAction #' || s.ActionNumber AS ActionNum,
    s.ActionName,
    s.ActionType,
    s.ExecutionOrder,
    s.IsEnabled,
    CASE 
        WHEN json_extract(s.Configuration, '$.Description') IS NOT NULL 
        THEN json_extract(s.Configuration, '$.Description')
        ELSE json_extract(s.Configuration, '$.Endpoint')
    END AS Details
FROM SubAction s
WHERE s.PrimaryApiId = (SELECT Id FROM PrimaryApi WHERE ApiNumber = 15)
ORDER BY s.ActionNumber;

-- ============================================================================
-- Notes:
-- ============================================================================
-- 1) SubAction 1 (CombineOrderItems) COPIES order files to INTERMEDIATE folder:
--    - Input: sales-orders\Sales\order-{saleOrderId}.json (nested response.result structure)
--    - Output: combine_glinvoices_raw\{customerId}_{saleOrderId}.json (same nested structure, just copied)
--
-- 2) SubAction 2 (FlattenJsonToFile) FLATTENS files and outputs to FINAL folder:
--    - Input: combine_glinvoices_raw\{customerId}_{saleOrderId}.json
--    - Output: combine_glinvoices\{customerId}_{saleOrderId}.json (flattened structure)
--
-- 3) Input file format (SubAction 1 output - nested, copied from sales-orders):
--    {
--      "response": {
--        "result": {
--          "saleOrderId": 15967,
--          "saleOrderNumber": "SS29",
--          "customerId": 33814,
--          "customerLastName": "goldberger",
--          "items": [...]
--        }
--      }
--    }
--
-- 4) Output file format (SubAction 2 output - flattened):
--    {
--      "customerId": "33814",
--      "customerName": "goldberger",
--      "referenceNo": "SS29",
--      "orderNumber": "15967",
--      "generatedAt": "2026-01-20 15:30:03",
--      "items": [...]
--    }
--
-- 5) No filtering applied in SubAction 2 - all files from raw folder are processed
--
-- 6) To enable API 15:
--    UPDATE PrimaryApi SET IsEnabled = 1 WHERE ApiNumber = 15;
--
-- ============================================================================
