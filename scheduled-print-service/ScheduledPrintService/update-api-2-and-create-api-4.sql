-- =====================================================
-- Migration: Update API 2 with filtering and create API 4
-- Date: 2025-11-27
-- Description:
--   1. Add PrinterName column to PrimaryApi table
--   2. Update API 2 SubActions with filter for index 17 starts with "SS"
--   3. Create API 4 as copy of API 2 with inverse filter (NOT "SS")
--   4. Set printer "4301" for API 4
-- =====================================================

-- Step 1: Add PrinterName column to PrimaryApi table
ALTER TABLE PrimaryApi ADD COLUMN PrinterName TEXT;

-- Step 2: Update API 2 Sub-Actions with filter
-- First, get the current configuration
-- We'll need to add filter properties to NavigateOnly action

UPDATE SubAction
SET Configuration = json_set(
    json(Configuration),
    '$.ChainedFilterArrayIndex', 17,
    '$.ChainedFilterType', 'StartsWith',
    '$.ChainedFilterValue', 'SS'
)
WHERE PrimaryApiId = (SELECT Id FROM PrimaryApi WHERE ApiNumber = 2)
  AND ActionType = 'NavigateOnly'
  AND ActionNumber = 1;

-- Step 3: Create API 4 as a copy of API 2
-- Insert Primary API for API 4
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
    PrinterName
)
SELECT
    4 AS ApiNumber,
    'Picklist Datatable API (Non-SS)' AS ApiName,
    BaseUrl,
    Endpoint,
    HttpMethod,
    Headers,
    Params,
    Payload,
    IsEnabled,
    '4301' AS PrinterName
FROM PrimaryApi
WHERE ApiNumber = 2;

-- Step 4: Copy Sub-Actions from API 2 to API 4 with inverse filter
-- NavigateOnly action with NOT equals "SS" filter
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
    (SELECT Id FROM PrimaryApi WHERE ApiNumber = 4) AS PrimaryApiId,
    ActionNumber,
    ActionName,
    ActionType,
    -- Update configuration with inverse filter
    json_set(
        json_set(
            json_set(
                json(Configuration),
                '$.ChainedFilterArrayIndex', 17
            ),
            '$.ChainedFilterType', 'NotStartsWith'
        ),
        '$.ChainedFilterValue', 'SS'
    ) AS Configuration,
    ExecutionOrder,
    IsEnabled
FROM SubAction
WHERE PrimaryApiId = (SELECT Id FROM PrimaryApi WHERE ApiNumber = 2)
  AND ActionType = 'NavigateOnly';

-- Copy PrintCapturedHtml action (no filter changes needed)
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
    (SELECT Id FROM PrimaryApi WHERE ApiNumber = 4) AS PrimaryApiId,
    ActionNumber,
    ActionName,
    ActionType,
    Configuration,
    ExecutionOrder,
    IsEnabled
FROM SubAction
WHERE PrimaryApiId = (SELECT Id FROM PrimaryApi WHERE ApiNumber = 2)
  AND ActionType = 'PrintCapturedHtml';

-- Step 5: Create Schedule for API 4 (optional - disabled by default)
INSERT INTO Schedule (
    ScheduleName,
    ScheduleType,
    CronExpression,
    IntervalSeconds,
    IsEnabled,
    Description,
    CreatedAt
)
VALUES (
    'Picklist Non-SS Print Schedule',
    'Interval',
    NULL,
    3600,  -- Every hour
    0,     -- Disabled by default
    'Processes picklist items where reference (index 17) does NOT start with "SS" and prints to printer 4301',
    CURRENT_TIMESTAMP
);

-- Step 6: Link Schedule to API 4
INSERT INTO ScheduleApi (
    ScheduleId,
    ApiNumber,
    ExecutionOrder
)
VALUES (
    (SELECT Id FROM Schedule WHERE ScheduleName = 'Picklist Non-SS Print Schedule'),
    4,
    1
);

-- =====================================================
-- Verification Queries
-- =====================================================
-- Run these to verify the migration was successful:

-- 1. Check PrimaryApi table has PrinterName column
-- PRAGMA table_info(PrimaryApi);

-- 2. Check API 2 configuration
-- SELECT ApiNumber, ApiName, PrinterName FROM PrimaryApi WHERE ApiNumber = 2;

-- 3. Check API 4 configuration
-- SELECT ApiNumber, ApiName, PrinterName FROM PrimaryApi WHERE ApiNumber = 4;

-- 4. Check API 2 SubActions with filter
-- SELECT
--     s.ActionNumber,
--     s.ActionName,
--     s.ActionType,
--     json_extract(s.Configuration, '$.ChainedFilterArrayIndex') AS FilterIndex,
--     json_extract(s.Configuration, '$.ChainedFilterType') AS FilterType,
--     json_extract(s.Configuration, '$.ChainedFilterValue') AS FilterValue
-- FROM SubAction s
-- JOIN PrimaryApi p ON s.PrimaryApiId = p.Id
-- WHERE p.ApiNumber = 2;

-- 5. Check API 4 SubActions with inverse filter
-- SELECT
--     s.ActionNumber,
--     s.ActionName,
--     s.ActionType,
--     json_extract(s.Configuration, '$.ChainedFilterArrayIndex') AS FilterIndex,
--     json_extract(s.Configuration, '$.ChainedFilterType') AS FilterType,
--     json_extract(s.Configuration, '$.ChainedFilterValue') AS FilterValue
-- FROM SubAction s
-- JOIN PrimaryApi p ON s.PrimaryApiId = p.Id
-- WHERE p.ApiNumber = 4;

-- 6. Check Schedule configuration
-- SELECT ScheduleName, IntervalSeconds, IsEnabled FROM Schedule
-- WHERE ScheduleName LIKE '%Non-SS%';

-- =====================================================
-- NOTES:
-- =====================================================
-- 1. API 2 now filters for items where data[x][17] starts with "SS"
-- 2. API 4 filters for items where data[x][17] does NOT start with "SS"
-- 3. API 4 prints to printer "4301"
-- 4. Both schedules are initially disabled - enable manually when ready
--
-- To enable API 4 schedule:
-- UPDATE Schedule SET IsEnabled = 1 WHERE ScheduleName = 'Picklist Non-SS Print Schedule';
--
-- To update printer for API 2 (if needed):
-- UPDATE PrimaryApi SET PrinterName = 'YourPrinterName' WHERE ApiNumber = 2;
-- =====================================================
