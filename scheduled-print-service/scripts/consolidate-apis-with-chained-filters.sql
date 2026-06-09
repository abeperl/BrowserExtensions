-- =====================================================
-- Migration: Consolidate API 2 and 4 into Single API with Chained Sub-Actions
-- Date: 2025-12-01
-- Description:
--   Current State:
--     - API 2: Fetches all rows, filters where OrderId (index 17) starts with "SS" or "SO", prints to Printer 1
--     - API 4: Fetches all rows, filters where OrderId does NOT start with "SS" or "SO", prints to Printer 2
--
--   New State:
--     - Single API call that fetches all rows once
--     - Sub-Action 1: Filter for OrderId starting with "SS" or "SO" → Chain to Printer Sub-Action 1
--     - Sub-Action 2: Filter for OrderId NOT starting with "SS" or "SO" → Chain to Printer Sub-Action 2
--     - Each printer sub-action is chained to its filter sub-action
--
--   Benefits:
--     - Single API call instead of two (reduces load and execution time)
--     - Clear sub-action filtering and chaining logic
--     - Easier to maintain and extend
-- =====================================================

BEGIN TRANSACTION;

-- =====================================================
-- Step 1: Backup existing configurations
-- =====================================================
-- Store current API 2 and API 4 configurations for reference
CREATE TEMP TABLE IF NOT EXISTS BackupPrimaryApi AS
SELECT * FROM PrimaryApi WHERE ApiNumber IN (2, 4);

CREATE TEMP TABLE IF NOT EXISTS BackupSubAction AS
SELECT s.* FROM SubAction s
JOIN PrimaryApi p ON s.PrimaryApiId = p.Id
WHERE p.ApiNumber IN (2, 4);

-- =====================================================
-- Step 2: Delete old API 4 (we'll consolidate into API 2)
-- =====================================================
-- Remove API 4 from schedules first
DELETE FROM ScheduleApi WHERE ApiNumber = 4;

-- Delete API 4's schedule if it exists
DELETE FROM Schedule WHERE ScheduleName = 'Picklist Non-SS Print Schedule';

-- Delete API 4 sub-actions
DELETE FROM SubAction WHERE PrimaryApiId IN (SELECT Id FROM PrimaryApi WHERE ApiNumber = 4);

-- Delete API 4 primary API
DELETE FROM PrimaryApi WHERE ApiNumber = 4;

-- =====================================================
-- Step 3: Update API 2 to remove printer-specific settings
-- =====================================================
-- API 2 will now be the unified API, so remove PrinterName
-- (Printers will be assigned at sub-action level)
UPDATE PrimaryApi
SET
    ApiName = 'Picklist Datatable API (Unified)',
    PrinterName = NULL,
    UpdatedAt = CURRENT_TIMESTAMP
WHERE ApiNumber = 2;

-- =====================================================
-- Step 4: Clear existing API 2 sub-actions
-- =====================================================
DELETE FROM SubAction WHERE PrimaryApiId = (SELECT Id FROM PrimaryApi WHERE ApiNumber = 2);

-- =====================================================
-- Step 5: Create new sub-actions with filters and chained printers
-- =====================================================

-- Get the PrimaryApiId for API 2 (we'll use this multiple times)
-- API 2 Id will be referenced in the INSERT statements below

-- Sub-Action 1: Navigate and Filter for "SS" or "SO" orders
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
    1,
    'Navigate - SS/SO Orders',
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
        "ChainedFilterType": "StartsWithAny",
        "ChainedFilterValues": ["SS", "SO"]
    }'),
    1,
    1
);

-- Sub-Action 2: Print SS/SO Orders to Printer 1
-- This action is chained from Sub-Action 1
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
    2,
    'Print SS/SO Orders - Printer 1',
    'PrintCapturedHtml',
    json('{
        "ChainedItemFieldPath": "[0]",
        "UseChainedInput": true,
        "Method": "GET",
        "ContinueOnError": true,
        "HtmlJsonPath": "html",
        "Endpoint": "/api/Picklist/GetPicklistHtml/{id}",
        "OutputFilePrefix": "picklist-ss-so",
        "PrinterName": "NPI84BD10 (HP LaserJet M607)",
        "ChainedFromActionNumber": 1
    }'),
    2,
    1
);

-- Sub-Action 3: Navigate and Filter for NON "SS" or "SO" orders
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
    3,
    'Navigate - Non SS/SO Orders',
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
        "ChainedFilterType": "NotStartsWithAny",
        "ChainedFilterValues": ["SS", "SO"]
    }'),
    3,
    1
);

-- Sub-Action 4: Print Non SS/SO Orders to Printer 2
-- This action is chained from Sub-Action 3
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
    4,
    'Print Non SS/SO Orders - Printer 2',
    'PrintCapturedHtml',
    json('{
        "ChainedItemFieldPath": "[0]",
        "UseChainedInput": true,
        "Method": "GET",
        "ContinueOnError": true,
        "HtmlJsonPath": "html",
        "Endpoint": "/api/Picklist/GetPicklistHtml/{id}",
        "OutputFilePrefix": "picklist-other",
        "PrinterName": "4301",
        "ChainedFromActionNumber": 3
    }'),
    4,
    1
);

-- =====================================================
-- Step 6: Commit transaction
-- =====================================================
COMMIT;

-- =====================================================
-- Verification Queries
-- =====================================================
-- Run these to verify the migration was successful:

-- 1. Check unified API 2 configuration
SELECT
    ApiNumber,
    ApiName,
    BaseUrl,
    Endpoint,
    IsEnabled
FROM PrimaryApi
WHERE ApiNumber = 2;

-- 2. Check all sub-actions for API 2
SELECT
    s.ActionNumber,
    s.ActionName,
    s.ActionType,
    s.ExecutionOrder,
    s.IsEnabled,
    json_extract(s.Configuration, '$.ChainedFilterType') AS FilterType,
    json_extract(s.Configuration, '$.ChainedFilterValues') AS FilterValues,
    json_extract(s.Configuration, '$.ChainedFilterArrayIndex') AS FilterIndex,
    json_extract(s.Configuration, '$.PrinterName') AS PrinterName,
    json_extract(s.Configuration, '$.ChainedFromActionNumber') AS ChainedFrom
FROM SubAction s
JOIN PrimaryApi p ON s.PrimaryApiId = p.Id
WHERE p.ApiNumber = 2
ORDER BY s.ExecutionOrder;

-- 3. Verify API 4 has been deleted
SELECT COUNT(*) as API4_Count FROM PrimaryApi WHERE ApiNumber = 4;
-- Should return 0

-- 4. Check schedule associations
SELECT
    s.ScheduleName,
    s.CronExpression,
    s.IsEnabled,
    sa.ApiNumber,
    p.ApiName
FROM Schedule s
JOIN ScheduleApi sa ON s.Id = sa.ScheduleId
JOIN PrimaryApi p ON sa.ApiNumber = p.ApiNumber
WHERE sa.ApiNumber = 2;

-- =====================================================
-- NOTES:
-- =====================================================
-- Filter Types Used:
--   - StartsWithAny: Checks if value starts with ANY of the provided values ["SS", "SO"]
--   - NotStartsWithAny: Checks if value does NOT start with ANY of the provided values
--
-- Sub-Action Chaining:
--   - Action 1 (Navigate SS/SO) → Action 2 (Print to Printer 1)
--   - Action 3 (Navigate Non-SS/SO) → Action 4 (Print to Printer 2)
--
-- Execution Flow:
--   1. API 2 is called and fetches all rows from datatable
--   2. Action 1 filters for rows where data[x][17] starts with "SS" or "SO"
--   3. Action 2 receives filtered results from Action 1 and prints to Printer 1
--   4. Action 3 filters for rows where data[x][17] does NOT start with "SS" or "SO"
--   5. Action 4 receives filtered results from Action 3 and prints to Printer 2
--
-- Example Data Flow:
--   Row 16: OrderId = "SS1234" → Filtered by Action 1 → Printed by Action 2 to Printer 1
--   Row 17: OrderId = "SM1234" → Filtered by Action 3 → Printed by Action 4 to Printer 2
--
-- To Update Printer Names:
--   UPDATE SubAction
--   SET Configuration = json_set(Configuration, '$.PrinterName', 'NewPrinterName')
--   WHERE PrimaryApiId = (SELECT Id FROM PrimaryApi WHERE ApiNumber = 2)
--     AND ActionNumber = 2;  -- For Printer 1 (SS/SO orders)
--
--   UPDATE SubAction
--   SET Configuration = json_set(Configuration, '$.PrinterName', 'NewPrinterName')
--   WHERE PrimaryApiId = (SELECT Id FROM PrimaryApi WHERE ApiNumber = 2)
--     AND ActionNumber = 4;  -- For Printer 2 (Non-SS/SO orders)
-- =====================================================
