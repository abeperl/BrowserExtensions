-- =====================================================
-- Migration: Consolidate API 3 with Multiple Filter Branches
-- Date: 2025-12-01
-- Description:
--   Current State:
--     - API 3: Single navigation filter (IsFilePath on itemNotes)
--     - 3 sub-actions: Navigate → Save PDF → Print PDF
--     - All items go to same printer
--
--   New State:
--     - Single API call that fetches all personalized order items
--     - Branch 1: Filter IsFilePath AND OrderId starts with "SS" or "SO"
--       → Navigate → Save PDF 1 → Print to Printer 1
--     - Branch 2: Filter IsFilePath AND OrderId does NOT start with "SS" or "SO"
--       → Navigate → Save PDF 2 → Print to Printer 2
--
--   Benefits:
--     - Single API call with multiple filter branches
--     - Different printers for different order types
--     - Maintains IsFilePath filtering for valid custom forms
--     - Clear separation of SS/SO orders vs other orders
-- =====================================================

BEGIN TRANSACTION;

-- =====================================================
-- Step 1: Backup existing API 3 configuration
-- =====================================================
CREATE TEMP TABLE IF NOT EXISTS BackupApi3PrimaryApi AS
SELECT * FROM PrimaryApi WHERE ApiNumber = 3;

CREATE TEMP TABLE IF NOT EXISTS BackupApi3SubAction AS
SELECT s.* FROM SubAction s
JOIN PrimaryApi p ON s.PrimaryApiId = p.Id
WHERE p.ApiNumber = 3;

-- =====================================================
-- Step 2: Get current printer name and other settings
-- =====================================================
-- Store the current printer name for reference
-- We'll use it as Printer 1, and user can update Printer 2 later

-- =====================================================
-- Step 3: Delete existing API 3 sub-actions
-- =====================================================
DELETE FROM SubAction WHERE PrimaryApiId = (SELECT Id FROM PrimaryApi WHERE ApiNumber = 3);

-- =====================================================
-- Step 4: Update API 3 metadata
-- =====================================================
UPDATE PrimaryApi
SET
    ApiName = 'Personalized Orders API (Unified)',
    PrinterName = NULL,  -- Printers now set at sub-action level
    UpdatedAt = CURRENT_TIMESTAMP
WHERE ApiNumber = 3;

-- =====================================================
-- Step 5: Create Branch 1 - SS/SO Orders
-- =====================================================

-- Sub-Action 1: Navigate and Filter for IsFilePath + SS/SO Orders
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
    (SELECT Id FROM PrimaryApi WHERE ApiNumber = 3),
    1,
    'Navigate - SS/SO Custom Forms',
    'NavigateOnly',
    json('{
        "ChainedArrayJsonPath": "data",
        "ChainedFilterField": "itemNotes",
        "ChainedFilterType": "IsFilePath",
        "ChainedFilterValue": null,
        "ChainedItemFieldPath": "itemNotes",
        "Endpoint": "https://mj.3plnext.com/{itemNotes}",
        "Method": "GET",
        "WaitForNetworkIdleMs": 3000,
        "MakeHiddenVisible": false,
        "ContinueOnError": true,
        "UseChainedInput": true,
        "IdJsonPath": "orderDetailsId",
        "AdditionalFilterField": "orderId",
        "AdditionalFilterType": "StartsWithAny",
        "AdditionalFilterValues": ["SS", "SO"]
    }'),
    1,
    1
);

-- Sub-Action 2: Save PDF to Disk for SS/SO Orders
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
    (SELECT Id FROM PrimaryApi WHERE ApiNumber = 3),
    2,
    'Save PDF - SS/SO Orders',
    'SaveCapturedHtml',
    json('{
        "ContinueOnError": true,
        "UseChainedInput": true,
        "ChainedFromActionNumber": 1,
        "OutputFilePrefix": "personalized-ss-so"
    }'),
    2,
    1
);

-- Sub-Action 3: Print Saved PDF for SS/SO Orders to Printer 1
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
    (SELECT Id FROM PrimaryApi WHERE ApiNumber = 3),
    3,
    'Print PDF - SS/SO Orders (Printer 1)',
    'PrintSavedPdf',
    json_object(
        'ContinueOnError', 1,
        'UseChainedInput', 1,
        'ChainedFromActionNumber', 2,
        'PrinterName', COALESCE((SELECT PrinterName FROM BackupApi3PrimaryApi LIMIT 1), 'DefaultPrinter'),
        'OutputFilePrefix', 'personalized-ss-so'
    ),
    3,
    1;

-- =====================================================
-- Step 6: Create Branch 2 - Non-SS/SO Orders
-- =====================================================

-- Sub-Action 4: Navigate and Filter for IsFilePath + Non-SS/SO Orders
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
    (SELECT Id FROM PrimaryApi WHERE ApiNumber = 3),
    4,
    'Navigate - Non-SS/SO Custom Forms',
    'NavigateOnly',
    json('{
        "ChainedArrayJsonPath": "data",
        "ChainedFilterField": "itemNotes",
        "ChainedFilterType": "IsFilePath",
        "ChainedFilterValue": null,
        "ChainedItemFieldPath": "itemNotes",
        "Endpoint": "https://mj.3plnext.com/{itemNotes}",
        "Method": "GET",
        "WaitForNetworkIdleMs": 3000,
        "MakeHiddenVisible": false,
        "ContinueOnError": true,
        "UseChainedInput": true,
        "IdJsonPath": "orderDetailsId",
        "AdditionalFilterField": "orderId",
        "AdditionalFilterType": "NotStartsWithAny",
        "AdditionalFilterValues": ["SS", "SO"]
    }'),
    4,
    1
);

-- Sub-Action 5: Save PDF to Disk for Non-SS/SO Orders
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
    (SELECT Id FROM PrimaryApi WHERE ApiNumber = 3),
    5,
    'Save PDF - Non-SS/SO Orders',
    'SaveCapturedHtml',
    json('{
        "ContinueOnError": true,
        "UseChainedInput": true,
        "ChainedFromActionNumber": 4,
        "OutputFilePrefix": "personalized-other"
    }'),
    5,
    1
);

-- Sub-Action 6: Print Saved PDF for Non-SS/SO Orders to Printer 2
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
    (SELECT Id FROM PrimaryApi WHERE ApiNumber = 3),
    6,
    'Print PDF - Non-SS/SO Orders (Printer 2)',
    'PrintSavedPdf',
    json('{
        "ContinueOnError": true,
        "UseChainedInput": true,
        "ChainedFromActionNumber": 5,
        "PrinterName": "4301",
        "OutputFilePrefix": "personalized-other"
    }'),
    6,
    1
);

-- =====================================================
-- Step 7: Commit transaction
-- =====================================================
COMMIT;

-- =====================================================
-- Verification Queries
-- =====================================================

-- 1. Check unified API 3 configuration
SELECT
    ApiNumber,
    ApiName,
    BaseUrl,
    Endpoint,
    IsEnabled,
    PrinterName
FROM PrimaryApi
WHERE ApiNumber = 3;

-- 2. Check all sub-actions for API 3
SELECT
    s.ActionNumber,
    s.ActionName,
    s.ActionType,
    s.ExecutionOrder,
    s.IsEnabled,
    json_extract(s.Configuration, '$.ChainedFilterType') AS PrimaryFilterType,
    json_extract(s.Configuration, '$.ChainedFilterField') AS PrimaryFilterField,
    json_extract(s.Configuration, '$.AdditionalFilterType') AS AdditionalFilterType,
    json_extract(s.Configuration, '$.AdditionalFilterField') AS AdditionalFilterField,
    json_extract(s.Configuration, '$.AdditionalFilterValues') AS AdditionalFilterValues,
    json_extract(s.Configuration, '$.PrinterName') AS PrinterName,
    json_extract(s.Configuration, '$.ChainedFromActionNumber') AS ChainedFrom,
    json_extract(s.Configuration, '$.OutputFilePrefix') AS FilePrefix
FROM SubAction s
JOIN PrimaryApi p ON s.PrimaryApiId = p.Id
WHERE p.ApiNumber = 3
ORDER BY s.ExecutionOrder;

-- 3. Check action chains
SELECT
    s1.ActionNumber,
    s1.ActionName,
    s1.ActionType,
    json_extract(s1.Configuration, '$.ChainedFromActionNumber') as ChainedFrom,
    s2.ActionName as ParentActionName
FROM SubAction s1
LEFT JOIN SubAction s2 ON
    s2.PrimaryApiId = s1.PrimaryApiId AND
    s2.ActionNumber = json_extract(s1.Configuration, '$.ChainedFromActionNumber')
WHERE s1.PrimaryApiId = (SELECT Id FROM PrimaryApi WHERE ApiNumber = 3)
ORDER BY s1.ExecutionOrder;

-- 4. View complete configuration as JSON
SELECT
    'API 3 Complete Configuration' as Description,
    json_object(
        'ApiNumber', p.ApiNumber,
        'ApiName', p.ApiName,
        'Endpoint', p.Endpoint,
        'SubActions', (
            SELECT json_group_array(
                json_object(
                    'ActionNumber', s.ActionNumber,
                    'ActionName', s.ActionName,
                    'ActionType', s.ActionType,
                    'Configuration', json(s.Configuration)
                )
            )
            FROM SubAction s
            WHERE s.PrimaryApiId = p.Id
            ORDER BY s.ExecutionOrder
        )
    ) as Configuration
FROM PrimaryApi p
WHERE p.ApiNumber = 3;

-- =====================================================
-- NOTES:
-- =====================================================
-- Filter Combination:
--   Both branches apply TWO filters:
--   1. Primary Filter: IsFilePath on itemNotes (ensures valid custom form path)
--   2. Additional Filter: StartsWithAny or NotStartsWithAny on orderId
--
-- Sub-Action Chains:
--   Branch 1: Navigate (1) → Save PDF (2) → Print to Printer 1 (3)
--   Branch 2: Navigate (4) → Save PDF (5) → Print to Printer 2 (6)
--
-- Execution Flow:
--   1. API 3 fetches all personalized order items
--   2. Action 1 filters: itemNotes IsFilePath AND orderId starts with "SS" or "SO"
--   3. Action 2 saves filtered PDFs from Action 1 with prefix "personalized-ss-so"
--   4. Action 3 prints saved PDFs from Action 2 to Printer 1
--   5. Action 4 filters: itemNotes IsFilePath AND orderId does NOT start with "SS" or "SO"
--   6. Action 5 saves filtered PDFs from Action 4 with prefix "personalized-other"
--   7. Action 6 prints saved PDFs from Action 5 to Printer 2
--
-- Example Data Flow:
--   Item 1: orderId="SS1234", itemNotes="/custom/form1.html"
--     → Passes IsFilePath filter ✓
--     → Passes StartsWithAny ["SS","SO"] filter ✓
--     → Navigates → Saves as personalized-ss-so-*.pdf → Prints to Printer 1
--
--   Item 2: orderId="SM5678", itemNotes="/custom/form2.html"
--     → Passes IsFilePath filter ✓
--     → Passes NotStartsWithAny ["SS","SO"] filter ✓
--     → Navigates → Saves as personalized-other-*.pdf → Prints to Printer 2
--
--   Item 3: orderId="SS9999", itemNotes="{\"json\":\"data\"}"
--     → Fails IsFilePath filter ✗ (JSON, not file path)
--     → Skipped by both branches
--
-- To Update Printer Names:
--   -- Update Printer 1 (SS/SO orders)
--   UPDATE SubAction
--   SET Configuration = json_set(Configuration, '$.PrinterName', 'NewPrinterName1')
--   WHERE PrimaryApiId = (SELECT Id FROM PrimaryApi WHERE ApiNumber = 3)
--     AND ActionNumber = 3;
--
--   -- Update Printer 2 (Non-SS/SO orders)
--   UPDATE SubAction
--   SET Configuration = json_set(Configuration, '$.PrinterName', 'NewPrinterName2')
--   WHERE PrimaryApiId = (SELECT Id FROM PrimaryApi WHERE ApiNumber = 3)
--     AND ActionNumber = 6;
--
-- To Add Third Branch (e.g., RUSH orders):
--   -- See QUICK-REFERENCE-API3.md for complete example
-- =====================================================
