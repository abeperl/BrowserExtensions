-- =====================================================
-- Migration: Split PrintCapturedHtml into SaveCapturedHtml + PrintSavedPdf
-- Date: 2025-12-02
-- Description: Splits the PrintCapturedHtml action for API 3 into two separate actions:
--              1. SaveCapturedHtml - Saves HTML content as PDF file
--              2. PrintSavedPdf - Sends the saved PDF to printer
--              This mirrors the pattern used in API 7 for better separation of concerns
-- =====================================================

-- NOTE: This script assumes you have a "PrintCapturedHtml" action that needs to be split.
-- If your database already has the split structure (SaveCapturedHtml + PrintSavedPdf),
-- this script will serve as documentation of the correct pattern.

-- =====================================================
-- BACKUP: Create backup tables for rollback
-- =====================================================
DROP TABLE IF EXISTS BackupApi3SubActions;
CREATE TABLE BackupApi3SubActions AS
SELECT * FROM SubAction
WHERE PrimaryApiId = (SELECT Id FROM PrimaryApi WHERE ApiNumber = 3);

-- =====================================================
-- Step 1: Delete existing PrintCapturedHtml action if it exists
-- =====================================================
DELETE FROM SubAction
WHERE PrimaryApiId = (SELECT Id FROM PrimaryApi WHERE ApiNumber = 3)
  AND ActionType = 'PrintCapturedHtml';

-- =====================================================
-- Step 2: Insert SaveCapturedHtml action for SS/SO Orders
-- =====================================================
-- This action captures HTML from the NavigateOnly action and saves it as a PDF file
INSERT OR REPLACE INTO SubAction (
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

-- =====================================================
-- Step 3: Insert PrintSavedPdf action for SS/SO Orders
-- =====================================================
-- This action takes the saved PDF from action 2 and sends it to the printer
INSERT OR REPLACE INTO SubAction (
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
    3,
    'Print PDF - SS/SO Orders (Printer 1)',
    'PrintSavedPdf',
    json_object(
        'ContinueOnError', 1,
        'UseChainedInput', 1,
        'ChainedFromActionNumber', 2,
        'PrinterName', COALESCE(
            (SELECT json_extract(Configuration, '$.PrinterName')
             FROM BackupApi3SubActions
             WHERE ActionType = 'PrintCapturedHtml'
             LIMIT 1),
            'DefaultPrinter'
        ),
        'OutputFilePrefix', 'personalized-ss-so'
    ),
    3,
    1
);

-- =====================================================
-- Step 4: Insert SaveCapturedHtml action for Non-SS/SO Orders
-- =====================================================
-- This action captures HTML from the NavigateOnly action (4) and saves it as a PDF file
INSERT OR REPLACE INTO SubAction (
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

-- =====================================================
-- Step 5: Insert PrintSavedPdf action for Non-SS/SO Orders
-- =====================================================
-- This action takes the saved PDF from action 5 and sends it to the printer
INSERT OR REPLACE INTO SubAction (
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
-- VERIFICATION QUERIES
-- =====================================================
-- Verify the new structure
SELECT '=== API 3 SubActions After Migration ===' AS Section;
SELECT
    s.ActionNumber,
    s.ActionName,
    s.ActionType,
    s.ExecutionOrder,
    s.IsEnabled,
    CASE
        WHEN s.ActionType = 'SaveCapturedHtml' THEN
            json_extract(s.Configuration, '$.ChainedFromActionNumber') || ' -> ' || json_extract(s.Configuration, '$.OutputFilePrefix')
        WHEN s.ActionType = 'PrintSavedPdf' THEN
            json_extract(s.Configuration, '$.ChainedFromActionNumber') || ' -> ' || json_extract(s.Configuration, '$.PrinterName')
        ELSE 'N/A'
    END AS ChainInfo
FROM SubAction s
JOIN PrimaryApi p ON s.PrimaryApiId = p.Id
WHERE p.ApiNumber = 3
ORDER BY s.ExecutionOrder, s.ActionNumber;

-- =====================================================
-- ROLLBACK INSTRUCTIONS
-- =====================================================
-- To rollback this migration:
--
-- DELETE FROM SubAction WHERE PrimaryApiId = (SELECT Id FROM PrimaryApi WHERE ApiNumber = 3);
-- INSERT INTO SubAction SELECT * FROM BackupApi3SubActions;
-- DROP TABLE BackupApi3SubActions;
-- =====================================================

-- =====================================================
-- PATTERN EXPLANATION
-- =====================================================
-- The split pattern provides several benefits:
--
-- 1. SEPARATION OF CONCERNS
--    - SaveCapturedHtml: Handles HTML capture, rendering, and PDF generation
--    - PrintSavedPdf: Handles printer communication and job submission
--
-- 2. FLEXIBILITY
--    - Can save PDFs without printing (disable PrintSavedPdf)
--    - Can print to multiple printers from same PDF (duplicate PrintSavedPdf actions)
--    - Can archive PDFs before/after printing
--
-- 3. ERROR HANDLING
--    - If save fails, no print attempt is made
--    - If print fails, PDF is still saved for manual retry
--    - Each action can have independent ContinueOnError settings
--
-- 4. DEBUGGING
--    - Saved PDFs can be inspected if print output is incorrect
--    - File paths are logged separately from print jobs
--    - Easier to isolate rendering vs printing issues
--
-- CONFIGURATION KEYS:
--
-- SaveCapturedHtml:
--   - ChainedFromActionNumber: Which action provides the HTML content
--   - OutputFilePrefix: Prefix for saved PDF filename
--   - UseChainedInput: true to use output from previous action
--   - ContinueOnError: Whether to continue if save fails
--
-- PrintSavedPdf:
--   - ChainedFromActionNumber: Which action provides the PDF file path
--   - PrinterName: Target printer name (from Windows printer list)
--   - OutputFilePrefix: Prefix used to locate the PDF file
--   - UseChainedInput: true to use output from previous action
--   - ContinueOnError: Whether to continue if print fails
-- =====================================================

-- =====================================================
-- EXAMPLE: Adding a third printer for SS/SO orders
-- =====================================================
-- Uncomment to add another printer for SS/SO orders:
--
-- INSERT INTO SubAction (
--     PrimaryApiId,
--     ActionNumber,
--     ActionName,
--     ActionType,
--     Configuration,
--     ExecutionOrder,
--     IsEnabled
-- )
-- VALUES (
--     (SELECT Id FROM PrimaryApi WHERE ApiNumber = 3),
--     7,  -- Next available action number
--     'Print PDF - SS/SO Orders (Printer 3)',
--     'PrintSavedPdf',
--     json('{
--         "ContinueOnError": true,
--         "UseChainedInput": true,
--         "ChainedFromActionNumber": 2,
--         "PrinterName": "AnotherPrinterName",
--         "OutputFilePrefix": "personalized-ss-so"
--     }'),
--     7,  -- After action 6
--     1
-- );
-- =====================================================
