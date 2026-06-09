-- Migration: Create API #14 for Customer Ledger export from Wholesale Order List
-- Date: 2026-01-12
-- Description:
--   - Uses a local JSON file (Wholesale_Order_List.json) as the primary API source
--   - Each record's index [5] contains the customer account/ID (e.g., "133")
--   - Sub-action 1: Fetch customer ledger by customerId with date range 01/01/1900 to current date
--   - Sub-action 2: Save the customer ledger response to JSON file named {customerId}.json

-- ============================================================================
-- Primary API (file-backed - reads from Wholesale_Order_List.json)
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
    14,
    'Customer Ledger from Wholesale Orders',
    'https://malchus.3plnext.com',
    '/api/CustomerLedger/GetCustomerLedgerById',
    'GET',
    json_object(
        'Authorization', 'Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJVc2VySW5mbyI6IkRsaFREWDFLak5xZGMwaDFDVHozWVVOdU9FSG9DMk1nWE56TFh5SDkxYVA0T1A0VjVVMUI2SFdmeGN1QWE4aUNLekZYS0luUTFadEM3RVVvWmxaazU2Q0R6emE1UGtRdWZNb2xqOW9GTnhMVk5YTXlWRGxDQ1A1bHJPNjVxLzE4WndYY2VLOHhLaVVNa2xpWDlsZkVpck50Q2k0L2V0Y2JSd1V4SXNWNkxoM2M2TGZoQ1JoWkI0SjJvdlNQdUI5MnhYWFU3dHhpQ3RxNysxUGZKdUdvNmdUUWtrSTdNcXVVRi9rZnJQZnk1b2hMYUFrTXlMakkyVjB0NmQxajg5cFQ1WWk1S1B2bGZkRFpiZStEQnJNbWtud3RHWDZoaU9FMVk5SGhNZzBUZnIwVXlqNExxNWY3WmhWWmo4UlRXTTN0ZC95R3dGZG5ENmh3eCtETXFNOC9sT2Ixb1R5Q1FLK29SSzZ3bDZCWHZTR3BDS2ExSnFNU2wzdnJZTVJTbExHT2xRZ28xNSs0V3Y5SFJ1UVE5SVplUmJOZXdoQithdWI3Y013UHN5YUJnaVlXL2J1eWw2MkdiQy9jSXI4U2IwMnNkbUVCQ0U1SDI0dmQwSVIyYjdwakIxcURwbzNtNXJQZmo0VTNqeTlVWTNKWExSN05zUlg2ZkdPTnFvZGE5WjhjQTFYc1dRUTg1TW9KMm83QitiK0x4SWhqREhpYTlncGNUU1dSTDYxRzRpMzExcDJNN01TbDFJdll3c0ErV2FrNjZZQVRsY0VjOEJCcXlCcWNiaTVuQkdNV2t2U280SUZ3VlNxNHVQVHN4Uk5acy9UWWFjN3FHNlJyUkxvcFU4bFZleVpTYS9NMDBZNkI4eGNrVEVqb2pEYmZGOU16TzhhWkFsd281VW9PVm9scTZ4a2VSSm9QTzFwV0RCajg2MlRBOGJDL3k0TjRkRzhCemVQeUZyd0NWMndXYVVRNHFYYmdZZGtZb21lM0NNRDMydk5odVFvazRBMjI1WmdCa2tKS2d1ZjExMG5YclR5MzF1dmVEUXVEemhLR0szdVByZ29nc1kzVExTL2R3Q1E3ZmFJOWJsN1ZtYVlwK1QrR1VQeXBOdWNiOGZ4RFdWSWJKR0Rxak5nRUJlcDQ1dlRBaE5CL1NvZVhoZmZnVWdVPSIsIm5iZiI6MTc2ODE1MzE5NCwiZXhwIjoyMDY4MTUzMTk0LCJpYXQiOjE3NjgxNTMxOTR9.fLRAORcX5Vw1xfRRsu6NstUTs0GJ5F_xUbM9KxdkfJ8',
        'Cookie', 'userData={"userID":33,"userFirstName":"Abe","ipAddress":"","userLastName":"Perl","userEmail":"abeperl@gmail.com","userPhone":"6463166925","userMobile":"","userName":"abeperl","userPassword":"MTE5YXAyMTQAAAAAAAAAAAAAAABcy/W5AITs/7YaM/xHa7fd8RBXNvgTzZEunt+oP5G/+g==","isActive":true,"roleName":"Super Admin","storeName":"","roleId":1,"storeId":1,"isHistoryExist":false,"clientId":1,"printReceiptAuto":false,"userStorePermission":[{"description":"Dynamic Integration Screen","active":true,"storePermissionId":1,"recId":3,"pin":null},{"description":"Sales Return Screen","active":true,"storePermissionId":2,"recId":6,"pin":null}],"repId":null,"userAddress":null}; isRefreshedToken=false; token=eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJVc2VySW5mbyI6IkRsaFREWDFLak5xZGMwaDFDVHozWVVOdU9FSG9DMk1nWE56TFh5SDkxYVA0T1A0VjVVMUI2SFdmeGN1QWE4aUNLekZYS0luUTFadEM3RVVvWmxaazU2Q0R6emE1UGtRdWZNb2xqOW9GTnhMVk5YTXlWRGxDQ1A1bHJPNjVxLzE4WndYY2VLOHhLaVVNa2xpWDlsZkVpck50Q2k0L2V0Y2JSd1V4SXNWNkxoM2M2TGZoQ1JoWkI0SjJvdlNQdUI5MnhYWFU3dHhpQ3RxNysxUGZKdUdvNmdUUWtrSTdNcXVVRi9rZnJQZnk1b2hMYUFrTXlMakkyVjB0NmQxajg5cFQ1WWk1S1B2bGZkRFpiZStEQnJNbWtud3RHWDZoaU9FMVk5SGhNZzBUZnIwVXlqNExxNWY3WmhWWmo4UlRXTTN0ZC95R3dGZG5ENmh3eCtETXFNOC9sT2Ixb1R5Q1FLK29SSzZ3bDZCWHZTR3BDS2ExSnFNU2wzdnJZTVJTbExHT2xRZ28xNSs0V3Y5SFJ1UVE5SVplUmJOZXdoQithdWI3Y013UHN5YUJnaVlXL2J1eWw2MkdiQy9jSXI4U2IwMnNkbUVCQ0U1SDI0dmQwSVIyYjdwakIxcURwbzNtNXJQZmo0VTNqeTlVWTNKWExSN05zUlg2ZkdPTnFvZGE5WjhjQTFYc1dRUTg1TW9KMm83QitiK0x4SWhqREhpYTlncGNUU1dSTDYxRzRpMzExcDJNN01TbDFJdll3c0ErV2FrNjZZQVRsY0VjOEJCcXlCcWNiaTVuQkdNV2t2U280SUZ3VlNxNHVQVHN4Uk5acy9UWWFjN3FHNlJyUkxvcFU4bFZleVpTYS9NMDBZNkI4eGNrVEVqb2pEYmZGOU16TzhhWkFsd281VW9PVm9scTZ4a2VSSm9QTzFwV0RCajg2MlRBOGJDL3k0TjRkRzhCemVQeUZyd0NWMndXYVVRNHFYYmdZZGtZb21lM0NNRDMydk5odVFvazRBMjI1WmdCa2tKS2d1ZjExMG5YclR5MzF1dmVEUXVEemhLR0szdVByZ29nc1kzVExTL2R3Q1E3ZmFJOWJsN1ZtYVlwK1QrR1VQeXBOdWNiOGZ4RFdWSWJKR0Rxak5nRUJlcDQ1dlRBaE5CL1NvZVhoZmZnVWdVPSIsIm5iZiI6MTc2ODE1MzE5NCwiZXhwIjoyMDY4MTUzMTk0LCJpYXQiOjE3NjgxNTMxOTR9.fLRAORcX5Vw1xfRRsu6NstUTs0GJ5F_xUbM9KxdkfJ8; chargingCard=; paymentinprocess=',
        'ClientId', '1',
        'StoreId', '5',
        'Accept', 'application/json',
        'Cache-Control', 'no-cache',
        'Pragma', 'no-cache',
        'X-Requested-With', 'XMLHttpRequest'
    ),
    NULL,
    NULL,
    0, -- Disabled by default; enable when ready
    NULL,
    json_object(
        'IdJsonPath', '[5]',
        'ChainedArrayJsonPath', 'response.result.data',
        'InputBasePath', 'C:\\ProgramData\\ScheduledPrintService\\datafiles',
        'InputFileName', 'Wholesale_Order_List.json'
    ),
    'C:\\ProgramData\\ScheduledPrintService\\datafiles\\Wholesale_Order_List.json'
);

-- ============================================================================
-- Sub-Action 1: Fetch customer ledger by customerId
-- Uses dateFrom=01/01/1900 and dateTo=current date (dynamic via {currentDate})
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
    (SELECT Id FROM PrimaryApi WHERE ApiNumber = 14),
    1,
    'Fetch Customer Ledger',
    'CallApi',
    json_object(
        'Endpoint', '/api/CustomerLedger/GetCustomerLedgerById?customerId={id}&dateFrom=01/01/1900&dateTo={currentDate}',
        'Method', 'GET',
        'OutputVariableName', 'customerLedger'
    ),
    1,
    1
);

-- ============================================================================
-- Sub-Action 2: Save customer ledger JSON to customerledger folder
-- File name is {id}.json where {id} is the customerId
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
    (SELECT Id FROM PrimaryApi WHERE ApiNumber = 14),
    2,
    'Save Customer Ledger JSON',
    'SaveJsonToFile',
    json_object(
        'Endpoint', 'customerledger',    -- Output directory under DataRoot\out
        'RequestBody', '{id}.json'       -- File name uses customerId
    ),
    2,
    1
);

-- ============================================================================
-- Update ApiAuth with new token if needed (uses existing entry for malchus.3plnext.com)
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
    'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJVc2VySW5mbyI6IkRsaFREWDFLak5xZGMwaDFDVHozWVVOdU9FSG9DMk1nWE56TFh5SDkxYVA0T1A0VjVVMUI2SFdmeGN1QWE4aUNLekZYS0luUTFadEM3RVVvWmxaazU2Q0R6emE1UGtRdWZNb2xqOW9GTnhMVk5YTXlWRGxDQ1A1bHJPNjVxLzE4WndYY2VLOHhLaVVNa2xpWDlsZkVpck50Q2k0L2V0Y2JSd1V4SXNWNkxoM2M2TGZoQ1JoWkI0SjJvdlNQdUI5MnhYWFU3dHhpQ3RxNysxUGZKdUdvNmdUUWtrSTdNcXVVRi9rZnJQZnk1b2hMYUFrTXlMakkyVjB0NmQxajg5cFQ1WWk1S1B2bGZkRFpiZStEQnJNbWtud3RHWDZoaU9FMVk5SGhNZzBUZnIwVXlqNExxNWY3WmhWWmo4UlRXTTN0ZC95R3dGZG5ENmh3eCtETXFNOC9sT2Ixb1R5Q1FLK29SSzZ3bDZCWHZTR3BDS2ExSnFNU2wzdnJZTVJTbExHT2xRZ28xNSs0V3Y5SFJ1UVE5SVplUmJOZXdoQithdWI3Y013UHN5YUJnaVlXL2J1eWw2MkdiQy9jSXI4U2IwMnNkbUVCQ0U1SDI0dmQwSVIyYjdwakIxcURwbzNtNXJQZmo0VTNqeTlVWTNKWExSN05zUlg2ZkdPTnFvZGE5WjhjQTFYc1dRUTg1TW9KMm83QitiK0x4SWhqREhpYTlncGNUU1dSTDYxRzRpMzExcDJNN01TbDFJdll3c0ErV2FrNjZZQVRsY0VjOEJCcXlCcWNiaTVuQkdNV2t2U280SUZ3VlNxNHVQVHN4Uk5acy9UWWFjN3FHNlJyUkxvcFU4bFZleVpTYS9NMDBZNkI4eGNrVEVqb2pEYmZGOU16TzhhWkFsd281VW9PVm9scTZ4a2VSSm9QTzFwV0RCajg2MlRBOGJDL3k0TjRkRzhCemVQeUZyd0NWMndXYVVRNHFYYmdZZGtZb21lM0NNRDMydk5odVFvazRBMjI1WmdCa2tKS2d1ZjExMG5YclR5MzF1dmVEUXVEemhLR0szdVByZ29nc1kzVExTL2R3Q1E3ZmFJOWJsN1ZtYVlwK1QrR1VQeXBOdWNiOGZ4RFdWSWJKR0Rxak5nRUJlcDQ1dlRBaE5CL1NvZVhoZmZnVWdVPSIsIm5iZiI6MTc2ODE1MzE5NCwiZXhwIjoyMDY4MTUzMTk0LCJpYXQiOjE3NjgxNTMxOTR9.fLRAORcX5Vw1xfRRsu6NstUTs0GJ5F_xUbM9KxdkfJ8',
    '2068-01-01T00:00:00Z',
    CURRENT_TIMESTAMP,
    CURRENT_TIMESTAMP
);

-- Ensure headers exist for malchus.3plnext.com
INSERT OR IGNORE INTO ApiHeaders (ApiAuthId, HeaderName, HeaderValue, IsEnabled)
SELECT Id, 'ClientId', '1', 1 FROM ApiAuth WHERE BaseUrl = 'https://malchus.3plnext.com';

INSERT OR IGNORE INTO ApiHeaders (ApiAuthId, HeaderName, HeaderValue, IsEnabled)
SELECT Id, 'StoreId', '5', 1 FROM ApiAuth WHERE BaseUrl = 'https://malchus.3plnext.com';

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
WHERE ApiNumber = 14
UNION ALL
SELECT 'Sub-Action' AS Type,
       ActionNumber AS Number,
       ActionName AS Name,
       ActionType AS Detail1,
       Configuration AS Detail2,
       IsEnabled,
       NULL AS LocalJsonFilePath
FROM SubAction
WHERE PrimaryApiId = (SELECT Id FROM PrimaryApi WHERE ApiNumber = 14)
ORDER BY Type DESC, Number;

-- ============================================================================
-- Notes:
-- ============================================================================
-- 1) Place the wholesale order list file at:
--    C:\ProgramData\ScheduledPrintService\datafiles\Wholesale_Order_List.json
--
--    Expected structure:
--    {
--      "response": {
--        "responseCode": 0,
--        "responseMessage": "Success",
--        "responseType": "Success",
--        "result": {
--          "draw": 1,
--          "recordsTotal": 3691,
--          "recordsFiltered": 3691,
--          "data": [
--            { "0": "34096", "1": "01/12/2026...", ..., "5": "133", ... },
--            { "0": "34094", "1": "01/11/2026...", ..., "5": "47894", ... },
--            ...
--          ]
--        }
--      }
--    }
--
--    Extraction:
--      - Array path: response.result.data
--      - ID path from each item: [5] (reads property "5" which holds the customer account ID)
--
-- 2) The Customer Ledger API call uses:
--    - customerId: from index [5] of each data row
--    - dateFrom: always "01/01/1900"
--    - dateTo: current date (dynamic placeholder {currentDate})
--
-- 3) Enable the API when ready:
--    UPDATE PrimaryApi SET IsEnabled = 1 WHERE ApiNumber = 14;
--
-- 4) The SaveJsonToFile action writes to:
--    C:\ProgramData\ScheduledPrintService\out\customerledger\{customerId}.json
--
-- 5) Each unique customerId from Wholesale_Order_List.json will create one JSON file
--    containing that customer's complete ledger history.