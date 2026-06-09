-- Migration: Create API #13 for Product Detail ingestion from local data files
-- Date: 2026-01-06
-- Description:
--   - Uses a local JSON file as the primary API source (no HTTP call)
--   - Each record should contain a productId field (used to chain sub-actions)
--   - Adds metadata for the base input path and file name
--   - Sub-action 1: Fetch product detail by clientProductId
--   - Sub-action 2: Save the product detail response to a JSON file

-- ============================================================================
-- Primary API (file-backed)
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
    LocalJsonFilePath
) VALUES (
    13,
    'Product Detail from DataFiles',
    'https://malchus.3plnext.com',
    '/api/Product/GetProductDetailById',
    'GET',
    json_object(
        'Authorization', 'Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJVc2VySW5mbyI6IkRsaFREWDFLak5xZGMwaDFDVHozWVVOdU9FSG9DMk1nWE56TFh5SDkxYVA0T1A0VjVVMUI2SFdmeGN1QWE4aUNLekZYS0luUTFadEM3RVVvWmxaazU2Q0R6emE1UGtRdWZNb2xqOW9GTnhMVk5YTXlWRGxDQ1A1bHJPNjVxLzE4WndYY2VLOHhLaVVNa2xpWDlsZkVpck50Q2k0L2V0Y2JSd1V4SXNWNkxoM2M2TGZoQ1JoWkI0SjJvdlNQdUI5MnhYWFU3dHhpQ3RxNysxUGZKdUdvNmdUUWtrSTdNcXVVRi9rZnJQZnk1b2hMYUFrTXlMakkyVjB0NmQxajg5cFQ1WWk1S1B2bGZkRFpiZStEQnJNbWtud3RHWDZoaU9FMVk5SGhNZzBUZnIwVXlqNExxNWY3WmhWWmo4UlRXTTN0ZC95R3dGZG5ENmh3eCtETXFNOC9sT2Ixb1R5Q1FLK29SSzZ3bDZCWHZTR3BDS2ExSnFNU2wzdnJZTVJTbExHT2xRZ28xNSs0V3Y5SFJ1UVE5SVplUmJOZXdoQithdWI3Y013UHN5YUJnaVlXL2J1eWw2MkdiQy9jSXI4U2IwMnNkbUVCQ0U1SDI0dmQwSVIyYjdwakIxcURwbzNtNXJQZmo0VTNqeTlVWTNKWExSN05zUlg2ZkdPTnFvZGE5WjhjQTFYc1dRUTg1TW9KMm83QitiK0x4SWhqREhpYTlncGNUU1dSTDYxRzRpMzExcDJNN01TbDFJdll3c0ErV2FrNjZZQVRsY0VjOEJCcXlCcWNiaTVuQkdNV2t2U280SUZ3VlNxNHVQVHN4Uk5acy9UWWFjN3FHNlJyUkxvcFU4bFZleVpTYS9NMDBZNkI4eGNrVEVqb2pEYmZGOU16TzhhWkFsd281VW9PVm9scTZ4a2VSSm9QTzFwV0RCajg2MlRBOGJDL3k0TjRkRzhCemVQeUZyd0NWMndXYVVRNHFYYmdZZGtZb21lM0NNRDMydk5odVFvazRBMjI1WmdCa2tKS2d1ZjExMG5YclR5MzF1dmVEUXVEemhLR0szdVByZ29nc1kzVExTL2R3Q1E3ZmFJOWJsN1ZtYVlwK1QrR1VQeXBOdWNiOGZ4RFdWSWJKR0Rxak5nRUJlcDQ1dlRBaE5CL1NvZVhoZmZnVWdVPSIsIm5iZiI6MTc2NzcxOTA5MywiZXhwIjoyMDY3NzE5MDkzLCJpYXQiOjE3Njc3MTkwOTN9.wQyP1g7TwAotkbP5SqkLeyBy0HcqkKk0EeQKXTWmqhQ',
        'Cookie', 'token=eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJVc2VySW5mbyI6IkRsaFREWDFLak5xZGMwaDFDVHozWVVOdU9FSG9DMk1nWE56TFh5SDkxYVA0T1A0VjVVMUI2SFdmeGN1QWE4aUNLekZYS0luUTFadEM3RVVvWmxaazU2Q0R6emE1UGtRdWZNb2xqOW9GTnhMVk5YTXlWRGxDQ1A1bHJPNjVxLzE4WndYY2VLOHhLaVVNa2xpWDlsZkVpck50Q2k0L2V0Y2JSd1V4SXNWNkxoM2M2TGZoQ1JoWkI0SjJvdlNQdUI5MnhYWFU3dHhpQ3RxNysxUGZKdUdvNmdUUWtrSTdNcXVVRi9rZnJQZnk1b2hMYUFrTXlMakkyVjB0NmQxajg5cFQ1WWk1S1B2bGZkRFpiZStEQnJNbWtud3RHWDZoaU9FMVk5SGhNZzBUZnIwVXlqNExxNWY3WmhWWmo4UlRXTTN0ZC95R3dGZG5ENmh3eCtETXFNOC9sT2Ixb1R5Q1FLK29SSzZ3bDZCWHZTR3BDS2ExSnFNU2wzdnJZTVJTbExHT2xRZ28xNSs0V3Y5SFJ1UVE5SVplUmJOZXdoQithdWI3Y013UHN5YUJnaVlXL2J1eWw2MkdiQy9jSXI4U2IwMnNkbUVCQ0U1SDI0dmQwSVIyYjdwakIxcURwbzNtNXJQZmo0VTNqeTlVWTNKWExSN05zUlg2ZkdPTnFvZGE5WjhjQTFYc1dRUTg1TW9KMm83QitiK0x4SWhqREhpYTlncGNUU1dSTDYxRzRpMzExcDJNN01TbDFJdll3c0ErV2FrNjZZQVRsY0VjOEJCcXlCcWNiaTVuQkdNV2t2U280SUZ3VlNxNHVQVHN4Uk5acy9UWWFjN3FHNlJyUkxvcFU4bFZleVpTYS9NMDBZNkI4eGNrVEVqb2pEYmZGOU16TzhhWkFsd281VW9PVm9scTZ4a2VSSm9QTzFwV0RCajg2MlRBOGJDL3k0TjRkRzhCemVQeUZyd0NWMndXYVVRNHFYYmdZZGtZb21lM0NNRDMydk5odVFvazRBMjI1WmdCa2tKS2d1ZjExMG5YclR5MzF1dmVEUXVEemhLR0szdVByZ29nc1kzVExTL2R3Q1E3ZmFJOWJsN1ZtYVlwK1QrR1VQeXBOdWNiOGZ4RFdWSWJKR0Rxak5nRUJlcDQ1dlRBaE5CL1NvZVhoZmZnVWdVPSIsIm5iZiI6MTc2NzcxOTA5MywiZXhwIjoyMDY3NzE5MDkzLCJpYXQiOjE3Njc3MTkwOTN9.wQyP1g7TwAotkbP5SqkLeyBy0HcqkKk0EeQKXTWmqhQ; userData={"userID":33,"userFirstName":"Abe","ipAddress":"","userLastName":"Perl","userEmail":"abeperl@gmail.com","userPhone":"6463166925","userMobile":"","userName":"abeperl","userPassword":"MTE5YXAyMTQAAAAAAAAAAAAAAABcy/W5AITs/7YaM/xHa7fd8RBXNvgTzZEunt+oP5G/+g==","isActive":true,"roleName":"Super Admin","storeName":"","roleId":1,"storeId":1,"isHistoryExist":false,"clientId":1,"printReceiptAuto":false,"userStorePermission":[{"description":"Dynamic Integration Screen","active":true,"storePermissionId":1,"recId":3,"pin":null},{"description":"Sales Return Screen","active":true,"storePermissionId":2,"recId":6,"pin":null}],"repId":null,"userAddress":null}; isRefreshedToken=false',
        'ClientId', '1',
        'StoreId', '1',
        'WarehouseId', '1',
        'Accept', 'application/json'
    ),
    NULL,
    NULL,
    0, -- Disabled by default; enable when ready
    NULL,
    json_object(
        'IdJsonPath', '[0]',
        'ChainedArrayJsonPath', 'response.result.data',
        'InputBasePath', 'C:\\ProgramData\\ScheduledPrintService\\datafiles',
        'InputFileName', 'products.json'
    ),
    'C:\\ProgramData\\ScheduledPrintService\\datafiles\\products.json'
);

-- ============================================================================
-- Sub-Action 1: Fetch product detail by productId
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
    (SELECT Id FROM PrimaryApi WHERE ApiNumber = 13),
    1,
    'Fetch Product Detail',
    'CallApi',
    json_object(
        'Endpoint', '/api/Product/GetProductDetailById?clientProductId={id}',
        'Method', 'GET',
        'OutputVariableName', 'productDetail'
    ),
    1,
    1
);

-- ============================================================================
-- Sub-Action 2: Save product detail JSON to disk
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
    (SELECT Id FROM PrimaryApi WHERE ApiNumber = 13),
    2,
    'Save Product Detail JSON',
    'SaveJsonToFile',
    json_object(
        'Endpoint', 'product-details',      -- Output directory under DataRoot\\out
        'RequestBody', 'product-{id}.json'  -- File name uses productId
    ),
    2,
    1
);

-- ============================================================================
-- Auth + headers for malchus.3plnext.com (ensures token + header reuse)
-- ============================================================================
INSERT OR REPLACE INTO ApiAuth (
    BaseUrl,
    Username,
    Password,
    BearerToken,
    TokenExpiresAt,
    CreatedAt,
    UpdatedAt
) VALUES (
    'https://malchus.3plnext.com',
    'abeperl',
    '',
    'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJVc2VySW5mbyI6IkRsaFREWDFLak5xZGMwaDFDVHozWVVOdU9FSG9DMk1nWE56TFh5SDkxYVA0T1A0VjVVMUI2SFdmeGN1QWE4aUNLekZYS0luUTFadEM3RVVvWmxaazU2Q0R6emE1UGtRdWZNb2xqOW9GTnhMVk5YTXlWRGxDQ1A1bHJPNjVxLzE4WndYY2VLOHhLaVVNa2xpWDlsZkVpck50Q2k0L2V0Y2JSd1V4SXNWNkxoM2M2TGZoQ1JoWkI0SjJvdlNQdUI5MnhYWFU3dHhpQ3RxNysxUGZKdUdvNmdUUWtrSTdNcXVVRi9rZnJQZnk1b2hMYUFrTXlMakkyVjB0NmQxajg5cFQ1WWk1S1B2bGZkRFpiZStEQnJNbWtud3RHWDZoaU9FMVk5SGhNZzBUZnIwVXlqNExxNWY3WmhWWmo4UlRXTTN0ZC95R3dGZG5ENmh3eCtETXFNOC9sT2Ixb1R5Q1FLK29SSzZ3bDZCWHZTR3BDS2ExSnFNU2wzdnJZTVJTbExHT2xRZ28xNSs0V3Y5SFJ1UVE5SVplUmJOZXdoQithdWI3Y013UHN5YUJnaVlXL2J1eWw2MkdiQy9jSXI4U2IwMnNkbUVCQ0U1SDI0dmQwSVIyYjdwakIxcURwbzNtNXJQZmo0VTNqeTlVWTNKWExSN05zUlg2ZkdPTnFvZGE5WjhjQTFYc1dRUTg1TW9KMm83QitiK0x4SWhqREhpYTlncGNUU1dSTDYxRzRpMzExcDJNN01TbDFJdll3c0ErV2FrNjZZQVRsY0VjOEJCcXlCcWNiaTVuQkdNV2t2U280SUZ3VlNxNHVQVHN4Uk5acy9UWWFjN3FHNlJyUkxvcFU4bFZleVpTYS9NMDBZNkI4eGNrVEVqb2pEYmZGOU16TzhhWkFsd281VW9PVm9scTZ4a2VSSm9QTzFwV0RCajg2MlRBOGJDL3k0TjRkRzhCemVQeUZyd0NWMndXYVVRNHFYYmdZZGtZb21lM0NNRDMydk5odVFvazRBMjI1WmdCa2tKS2d1ZjExMG5YclR5MzF1dmVEUXVEemhLR0szdVByZ29nc1kzVExTL2R3Q1E3ZmFJOWJsN1ZtYVlwK1QrR1VQeXBOdWNiOGZ4RFdWSWJKR0Rxak5nRUJlcDQ1dlRBaE5CL1NvZVhoZmZnVWdVPSIsIm5iZiI6MTc2NzcxOTA5MywiZXhwIjoyMDY3NzE5MDkzLCJpYXQiOjE3Njc3MTkwOTN9.wQyP1g7TwAotkbP5SqkLeyBy0HcqkKk0EeQKXTWmqhQ',
    '2067-01-01T00:00:00Z',
    CURRENT_TIMESTAMP,
    CURRENT_TIMESTAMP
);

INSERT OR IGNORE INTO ApiHeaders (ApiAuthId, HeaderName, HeaderValue, IsEnabled)
SELECT Id, 'ClientId', '1', 1 FROM ApiAuth WHERE BaseUrl = 'https://malchus.3plnext.com';

INSERT OR IGNORE INTO ApiHeaders (ApiAuthId, HeaderName, HeaderValue, IsEnabled)
SELECT Id, 'StoreId', '1', 1 FROM ApiAuth WHERE BaseUrl = 'https://malchus.3plnext.com';

INSERT OR IGNORE INTO ApiHeaders (ApiAuthId, HeaderName, HeaderValue, IsEnabled)
SELECT Id, 'WarehouseId', '1', 1 FROM ApiAuth WHERE BaseUrl = 'https://malchus.3plnext.com';

-- ============================================================================
-- Verification: view API and sub-actions
-- ============================================================================
SELECT 'Primary API' AS Type,
       ApiNumber AS Number,
       ApiName AS Name,
       BaseUrl AS Detail1,
       Endpoint AS Detail2,
       IsEnabled,
       LocalJsonFilePath
FROM PrimaryApi
WHERE ApiNumber = 13
UNION ALL
SELECT 'Sub-Action' AS Type,
       ActionNumber AS Number,
       ActionName AS Name,
       ActionType AS Detail1,
       Configuration AS Detail2,
       IsEnabled,
       NULL AS LocalJsonFilePath
FROM SubAction
WHERE PrimaryApiId = (SELECT Id FROM PrimaryApi WHERE ApiNumber = 13)
ORDER BY Type DESC, Number;

-- Notes:
-- 1) Place the product input file at C:\\ProgramData\\ScheduledPrintService\\datafiles\\products.json.
--    Expected structure: { "response": { "result": { "data": [ { "0": "35172", ... }, ... ] } } }
--    Extraction:
--      - Array path: response.result.data
--      - ID path from each item: [0] (reads property "0" which holds clientProductId)
-- 2) Enable the API when ready: UPDATE PrimaryApi SET IsEnabled = 1 WHERE ApiNumber = 13;
-- 3) The SaveJsonToFile action writes to DataRoot\\out\\product-details\\product-{id}.json.
