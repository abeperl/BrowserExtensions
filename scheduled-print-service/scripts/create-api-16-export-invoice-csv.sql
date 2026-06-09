-- Migration: Create API #16 to export combined GL invoices to CSV
-- Date: 2026-01-12
-- Description:
--   - Reads combined invoice JSON files produced by API #15 (CombineOrderItems)
--   - Source folder: C:\ProgramData\ScheduledPrintService\out\combine_glinvoices
--   - Each JSON file is named {customerId}.json
--   - Outputs a CSV with the same name (e.g., 43247.csv) using the ExportInvoicesCsv action
--   - Columns include the provided mapping plus Order Number

-- ============================================================================
-- Primary API (folder-backed - reads all JSON files from combine_glinvoices)
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
    16,
    'Export Combined GL Invoices to CSV',
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
        'Description', 'Exports combine_glinvoices JSON files to CSV (Excel-friendly)'
    ),
    NULL, -- Not using single file
    'C:\\ProgramData\\ScheduledPrintService\\out\\combine_glinvoices'
);

-- ============================================================================
-- Sub-Action 1: Export to CSV
-- Uses the new ExportInvoicesCsv action type to flatten invoice items
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
    (SELECT Id FROM PrimaryApi WHERE ApiNumber = 16),
    1,
    'Export Combined Invoices to CSV',
    'ExportInvoicesCsv',
    json_object(
        'Endpoint', 'combine_glinvoices'
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
WHERE ApiNumber = 16
UNION ALL
SELECT 'Sub-Action' AS Type,
       ActionNumber AS Number,
       ActionName AS Name,
       ActionType AS Detail1,
       Configuration AS Detail2,
       IsEnabled,
       NULL AS LocalJsonFolderPath
FROM SubAction
WHERE PrimaryApiId = (SELECT Id FROM PrimaryApi WHERE ApiNumber = 16)
ORDER BY Type DESC, Number;

-- ============================================================================
-- Notes:
-- ============================================================================
-- 1) Enable the API when ready:
--    UPDATE PrimaryApi SET IsEnabled = 1 WHERE ApiNumber = 16;
--
-- 2) Run manually with:
--    ScheduledPrintService.exe -m -a 16
--
-- 3) Output files:
--    C:\ProgramData\ScheduledPrintService\out\combine_glinvoices\{customerId}.csv
--
-- 4) Mapping applied:
--    Lookup Code -> sku
--    Item Name -> title
--    Price -> salePrice or rate
--    Quantity Ordered -> saleQty
--    Quantity To Supply -> shippedQty or remainingQty
--    MSRP -> listPrice
--    Item Price -> discountedPrice or amount
--    Supplier Item Code -> vendorSKU
--    Order Number -> from invoice.orderNumber (fallback: customerId/file name)
