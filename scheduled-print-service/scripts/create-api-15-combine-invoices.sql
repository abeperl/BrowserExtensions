-- Migration: Create API #15 for Combining Customer Ledger Items into Invoice Files
-- Date: 2026-01-12
-- Description:
--   - Uses a local JSON folder as the primary source (each file = one customer)
--   - Input folder: C:\ProgramData\ScheduledPrintService\out\customerledger\customerledger
--   - Each JSON file is named {customerId}.json (e.g., 32788.json)
--   - Processes the "items" array in each customer ledger file
--   - Filters items where storeId is 5 or 6 AND saleOrderId is not null
--   - Looks up order files in Sales and Office folders
--   - Combines all order items into one file per customer in combine_glinvoices folder

-- ============================================================================
-- Schema Migration: Add LocalJsonFolderPath column if it doesn't exist
-- ============================================================================
-- SQLite doesn't have IF NOT EXISTS for ALTER TABLE, so we use a workaround
-- This will fail silently if column already exists
ALTER TABLE PrimaryApi ADD COLUMN LocalJsonFolderPath TEXT;

-- ============================================================================
-- Primary API (folder-backed - reads all JSON files from customerledger folder)
-- ============================================================================
INSERT OR REPLACE INTO PrimaryApi (
    ApiNumber,
    ApiName,
    BaseUrl,
    Endpoint,
    HttpMethod,
    Headers,
    Payload,
    Params,
    IsEnabled,
    PrinterName,
    Configuration,
    LocalJsonFilePath,
    LocalJsonFolderPath
) VALUES (
    15,
    'Combine Customer Invoices from Ledger',
    'https://malchus.3plnext.com',
    '/api/placeholder',
    'GET',
    json_object(
        'Authorization', 'Bearer placeholder',
        'Accept', 'application/json'
    ),
    NULL,
    NULL,
    0, -- Disabled by default; enable when ready
    NULL,
    json_object(
        'Description', 'Processes customer ledger JSON files and combines order items into invoice files'
    ),
    NULL, -- Not using single file
    'C:\ProgramData\ScheduledPrintService\out\customerledger\customerledger'
);

-- ============================================================================
-- Sub-Action 1: Combine Order Items
-- Processes items array, filters by storeId and saleOrderId, looks up orders,
-- and combines into a single JSON file per customer
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
    1,
    'Combine Order Items into Invoice File',
    'CombineOrderItems',
    json_object(
        'Endpoint', 'combine_glinvoices',
        'ChainedArrayJsonPath', 'response.result.items',
        'ChainedFilterField', 'storeId',
        'ChainedFilterValues', json_array('5', '6'),
        'RequestBody', 'saleOrderId',
        'OutputVariableName', 'C:\ProgramData\ScheduledPrintService\out\sales-orders\Sales,C:\ProgramData\ScheduledPrintService\out\sales-orders\Office'
    ),
    1,
    1
);

-- ============================================================================
-- Verification: view API and sub-actions
-- ============================================================================
SELECT 'Primary API' AS Type,
       ApiNumber AS Number,
       ApiName AS Name,
       BaseUrl AS Detail1,
       COALESCE(LocalJsonFolderPath, LocalJsonFilePath, Endpoint) AS Detail2,
       IsEnabled,
       LocalJsonFolderPath
FROM PrimaryApi
WHERE ApiNumber = 15
UNION ALL
SELECT 'Sub-Action' AS Type,
       ActionNumber AS Number,
       ActionName AS Name,
       ActionType AS Detail1,
       Configuration AS Detail2,
       IsEnabled,
       NULL AS LocalJsonFolderPath
FROM SubAction
WHERE PrimaryApiId = (SELECT Id FROM PrimaryApi WHERE ApiNumber = 15)
ORDER BY Type DESC, Number;

-- ============================================================================
-- Notes:
-- ============================================================================
-- 1) Input folder structure:
--    C:\ProgramData\ScheduledPrintService\out\customerledger\customerledger\
--      - 32788.json
--      - 47894.json
--      - ...
--
--    Each file contains customer ledger data with items at response.result.items:
--    {
--      "response": {
--        "result": {
--          "items": [
--            {
--              "storeId": 5,
--              "saleOrderId": 29431,
--              ...
--            },
--            ...
--          ]
--        }
--      }
--    }
--
--    Note: Duplicate saleOrderIds are automatically skipped (e.g., if 29431 appears twice,
--    it's only processed once)
--
-- 2) Order lookup folders (searched in order):
--    - C:\ProgramData\ScheduledPrintService\out\sales-orders\Sales\order-{saleOrderId}.json
--    - C:\ProgramData\ScheduledPrintService\out\sales-orders\Office\order-{saleOrderId}.json
--
-- 3) Filter criteria:
--    - storeId must be 5 or 6
--    - saleOrderId must not be null or empty
--
-- 4) Output structure:
--    C:\ProgramData\ScheduledPrintService\out\combine_glinvoices\{customerId}.json
--
--    Each file contains:
--    {
--      "customerId": "32788",
--      "generatedAt": "2026-01-12 11:30:45",
--      "invoices": [
--        {
--          "orderNumber": "468",
--          "items": [ ... order items ... ]
--        },
--        {
--          "orderNumber": "469",
--          "items": [ ... order items ... ]
--        }
--      ]
--    }
--
-- 5) If output file already exists, new orders will be merged (avoiding duplicates by orderNumber)
--
-- 6) Enable the API when ready:
--    UPDATE PrimaryApi SET IsEnabled = 1 WHERE ApiNumber = 15;
--
-- 7) Run manually with:
--    ScheduledPrintService.exe -m -a 15