-- Migration: Fix API #3 HtmlInjections Format
-- Date: 2025-12-02
-- Description: Fix HtmlInjections from single object to array format
--
-- PROBLEM:
-- HtmlInjections is stored as a single object:
--   "HtmlInjections": { "TargetSelector": "...", ... }
--
-- But the code expects an array of objects:
--   "HtmlInjections": [ { "TargetSelector": "...", ... } ]
--
-- Error: The JSON value could not be converted to System.Collections.Generic.List`1[...HtmlInjection]

-- Before: Show current (broken) format
SELECT
    'BEFORE' as Status,
    ActionNumber,
    ActionName,
    json_extract(Configuration, '$.HtmlInjections') as HtmlInjections_Value,
    json_type(Configuration, '$.HtmlInjections') as HtmlInjections_Type
FROM SubAction
WHERE PrimaryApiId = (SELECT Id FROM PrimaryApi WHERE ApiNumber = 3)
  AND ActionType = 'NavigateOnly';

-- Fix: Wrap the single object in an array
UPDATE SubAction
SET Configuration = json_set(
    Configuration,
    '$.HtmlInjections',
    json_array(
        json_object(
            'TargetSelector', json_extract(Configuration, '$.HtmlInjections.TargetSelector'),
            'InsertPosition', json_extract(Configuration, '$.HtmlInjections.InsertPosition'),
            'HtmlTemplate', json_extract(Configuration, '$.HtmlInjections.HtmlTemplate')
        )
    )
)
WHERE PrimaryApiId = (SELECT Id FROM PrimaryApi WHERE ApiNumber = 3)
  AND ActionType = 'NavigateOnly'
  AND json_type(Configuration, '$.HtmlInjections') = 'object';

-- After: Show fixed format
SELECT
    'AFTER' as Status,
    ActionNumber,
    ActionName,
    json_extract(Configuration, '$.HtmlInjections') as HtmlInjections_Value,
    json_type(Configuration, '$.HtmlInjections') as HtmlInjections_Type
FROM SubAction
WHERE PrimaryApiId = (SELECT Id FROM PrimaryApi WHERE ApiNumber = 3)
  AND ActionType = 'NavigateOnly';

-- Verify the complete configuration
SELECT
    ActionNumber,
    ActionName,
    Configuration
FROM SubAction
WHERE PrimaryApiId = (SELECT Id FROM PrimaryApi WHERE ApiNumber = 3)
  AND ActionType = 'NavigateOnly';

-- EXPECTED RESULT:
-- HtmlInjections should be type "array" with format:
-- [
--   {
--     "TargetSelector": ".modal-head",
--     "InsertPosition": "append",
--     "HtmlTemplate": "<h4 class='panel-box-title text-center'>{customerName}</h4>"
--   }
-- ]
