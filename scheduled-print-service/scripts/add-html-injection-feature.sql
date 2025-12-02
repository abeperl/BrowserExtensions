-- Migration: Add HTML Injection Feature
-- Date: 2025-12-02
-- Description: Adds support for injecting dynamic HTML into NavigateOnly pages with placeholder replacement

-- This migration adds HTML injection configuration to SubAction Configuration JSON
-- and Primary API field mapping to PrimaryApi Configuration JSON

-- Example 1: Add HTML injection to API #3 Navigate action
-- This will inject customer name into the personalized modal
UPDATE SubAction
SET Configuration = json_set(
    Configuration,
    '$.HtmlInjections',
    json_array(
        json_object(
            'TargetSelector', '.modal-head',
            'InsertPosition', 'append',
            'HtmlTemplate', '<h4 class="panel-box-title text-center">{customerName}</h4>'
        )
    )
)
WHERE PrimaryApiId = (SELECT Id FROM PrimaryApi WHERE ApiNumber = 3)
  AND ActionType = 'NavigateOnly';

-- Example 2: Add field mapping to Primary API #3
-- This maps API response fields to dictionary keys for placeholder replacement
UPDATE PrimaryApi
SET Configuration = COALESCE(
    json_set(
        CASE
            WHEN Configuration IS NULL OR Configuration = '' THEN '{}'
            ELSE Configuration
        END,
        '$.PrimaryApiFieldMap',
        json_object(
            'customerName', 'Customers',
            'orderNumber', 'OrderNumber',
            'orderDate', 'OrderDate'
        )
    ),
    Configuration
)
WHERE ApiNumber = 3;

-- Verify the changes
SELECT
    p.ApiNumber,
    p.ApiName,
    json_extract(p.Configuration, '$.PrimaryApiFieldMap') as FieldMap,
    s.ActionNumber,
    s.ActionName,
    json_extract(s.Configuration, '$.HtmlInjections') as HtmlInjections
FROM PrimaryApi p
LEFT JOIN SubAction s ON s.PrimaryApiId = p.Id
WHERE p.ApiNumber = 3;

-- NOTES:
-- 1. HtmlInjections is an array of injection configurations, each with:
--    - TargetSelector: CSS selector to find the target element
--    - InsertPosition: Where to insert (append, prepend, after, before, replace)
--    - HtmlTemplate: HTML template with {placeholders} that will be replaced
--
-- 2. PrimaryApiFieldMap maps API response field names to dictionary keys:
--    - Key: Dictionary key name (used in {placeholders})
--    - Value: JSON path or field name in the API response
--
-- 3. Placeholder syntax: {fieldName} will be replaced with values from the dictionary
--
-- 4. Insert positions:
--    - append: Insert as last child inside target element
--    - prepend: Insert as first child inside target element
--    - after: Insert as next sibling after target element
--    - before: Insert as previous sibling before target element
--    - replace: Replace target element's innerHTML
--
-- Example Configuration for other APIs:
--
-- Multiple HTML injections:
-- UPDATE SubAction SET Configuration = json_set(Configuration, '$.HtmlInjections',
--   json_array(
--     json_object('TargetSelector', '.header', 'InsertPosition', 'prepend', 'HtmlTemplate', '<div>{orderNumber}</div>'),
--     json_object('TargetSelector', '.footer', 'InsertPosition', 'append', 'HtmlTemplate', '<span>{orderDate}</span>')
--   )
-- ) WHERE ActionNumber = X;
--
-- No placeholders (static HTML):
-- UPDATE SubAction SET Configuration = json_set(Configuration, '$.HtmlInjections',
--   json_array(
--     json_object('TargetSelector', '.container', 'InsertPosition', 'append', 'HtmlTemplate', '<div class="watermark">DRAFT</div>')
--   )
-- ) WHERE ActionNumber = X;
