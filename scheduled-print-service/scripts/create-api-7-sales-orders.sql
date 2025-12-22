-- Migration: Create API #7 for Sales Order List and Details Export
-- Date: 2025-12-22
-- Description: Fetches sales orders from GetSaleOrderList API, gets details for each order, and saves as JSON files
-- This API uses the malchus.3plnext.com domain instead of mj.3plnext.com

-- Insert Primary API Configuration for Sales Order List
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
    PrinterName,
    Configuration
) VALUES (
    7,
    'Sales Order Export API',
    'https://malchus.3plnext.com',
    '/api/SaleOrder/GetSaleOrderList',
    'POST',
    json_object(
        'Authorization', 'Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJVc2VySW5mbyI6IkRsaFREWDFLak5xZGMwaDFDVHozWVVOdU9FSG9DMk1nWE56TFh5SDkxYVA0T1A0VjVVMUI2SFdmeGN1QWE4aUNLekZYS0luUTFadEM3RVVvWmxaazU2Q0R6emE1UGtRdWZNb2xqOW9GTnhMVk5YTXlWRGxDQ1A1bHJPNjVxLzE4WndYY2VLOHhLaVVNa2xpWDlsZkVpck50Q2k0L2V0Y2JSd1V4SXNWNkxoM2M2TGZoQ1JoWkI0SjJvdlNQdUI5MnhYWFU3dHhpQ3RxNysxUGZKdUdvNmdUUWtrSTdNcXVVRi9rZnJQZnk1b2hMYUFrTXlMakkyVjB0NmQxajg5cFQ1WWk1S1B2bGZkRFpiZStEQnJNbWtud3RHWDZoaU9FMVk5SGhNZzBUZnIwVXlqNExxNWY3WmhWWmo4UlRXTTN0ZC95R3dGZG5ENmh3eCtETXFNOC9sT2Ixb1R5Q1FLK29SSzZ3bDZCWHZTR3BDS2ExSnFNU2wzdnJZTVJTbExHT2xRZ28xNSs0V3Y5SFJ1UVE5SVplUmJOZXdoQithdWI3Y013UHN5YUJnaVlXL2J1eWw2MkdiQy9jSXI4U2IwMnNkbUVCQ0U1SDI0dmQwSVIyYjdwakIxcURwbzNtNXJQZmo0VTNqeTlVWTNKWExSN05zUlg2ZkdPTnFvZGE5WjhjQTFYc1dRUTg1TW9KMm83QitiK0x4SWhqREhpYTlncGNUU1dSTDYxRzRpMzExcDJNN01TbDFJdll3c0ErV2FrNjZZQVRsY0VjOEJCcXlCcWNiaTVuQkdNV2t2U280SUZ3VlNxNHVQVHN4Uk5acy9UWWFjN3FHNlJyUkxvcFU4bFZleVpTYS9NMDBZNkI4eGNrVEVqb2pEYmZGOU16TzhhWkFsd281VW9PVm9scTZ4a2VSSm9QTzFwV0RCajg2MlRBOGJDL3k0TjRkRzhCemVQeUZyd0NWMndXYVVRNHFYYmdZZGtZb21lM0NNRDMydk5odVFvazRBMjI1WmdCa2tKS2d1ZjExMG5YclR5MzF1dmVEUXVEemhLR0szdVByZ29nc1kzVExTL2R3Q1E3ZmFJOWJsN1ZtYVlwK1QrR1VQeXBOdWNiOGZ4RFdWSWJKR0Rxak5nRUJlcDQ1dlRBaE5CL1NvZVhoZmZnVWdVPSIsIm5iZiI6MTc2NjM0Njc3OCwiZXhwIjoyMDY2MzQ2Nzc4LCJpYXQiOjE3NjYzNDY3Nzh9.KO41y02CYKQeiaNdfZqUgFa4RFKe0F1or0AfHl61QE8',
        'Cookie', 'token=eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJVc2VySW5mbyI6IkRsaFREWDFLak5xZGMwaDFDVHozWVVOdU9FSG9DMk1nWE56TFh5SDkxYVA0T1A0VjVVMUI2SFdmeGN1QWE4aUNLekZYS0luUTFadEM3RVVvWmxaazU2Q0R6emE1UGtRdWZNb2xqOW9GTnhMVk5YTXlWRGxDQ1A1bHJPNjVxLzE4WndYY2VLOHhLaVVNa2xpWDlsZkVpck50Q2k4L2V0Y2JSd1V4SXNWNkxoM2M2TGZoQ1JoWkI0SjJvdlNQdUI5MnhYWFU3dHhpQ3RxNzsxUGZKdUdvNmdUUWtrSTdNcXVVRi9rZnJQZnk1b2hMYUFrTXlMakkyVjB0NmQxajg5cFQ1WWk1S1B2bGZkRFpiZStEQnJNbWtud3RHWDZoaU9FMVk5SGhNZzBUZnIwVXlqNExxNWY3WmhWWmo4UlRXTTN0ZC95R3dGZG5ENmh3eCtETXFNOC9sT2Ixb1R5Q1FLK29SSzZ3bDZCWHZTR3BDS2ExSnFNU2wzdnJZTVJTbExHT2xRZ28xNSs0V3Y5SFJ1UVE5SVplUmJOZXdoQithdWI3Y013UHN5YUJnaVlXL2J1eWw2MkdiQy9jSXI4U2IwMnNkbUVCQ0U1SDI0dmQwSVIyYjdwakIxcURwbzNtNXJQZmo0VTNqeTlVWTNKWExSN05zUlg2ZkdPTnFvZGE5WjhjQTFYc1dRUTg1TW9KMm83QitiK0x4SWhqREhpYTlncGNUU1dSTDYxRzRpMzExcDJNN01TbDFJdll3c0ErV2FrNjZZQVRsY0VjOEJCcXlCcWNiaTVuQkdNV2t2U280SUZ3VlNxNHVQVHN4Uk5acy9UWWFjN3FHNlJyUkxvcFU4bFZleVpTYS9NMDBZNkI4eGNrVEVqb2pEYmZGOU16TzhhWkFsd281VW9PVm9scTZ4a2VSSm9QTzFwV0RCajg2MlRBOGJDL3k0TjRkRzhCemVQeUZyd0NWMndXYVVRNHFYYmdZZGtZb21lM0NNRDMydk5odVFvazRBMjI1WmdCa2tKS2d1ZjExMG5YclR5MzF1dmVEUXVEemhLR0szdVByZ29nc1kzVExTL2R3Q1E3ZmFJOWJsN1ZtYVlwK1QrR1VQeXBOdWNiOGZ4RFdWSWJKR0Rxak5nRUJlcDQ1dlRBaE5CL1NvZVhoZmZnVWdVPSIsIm5iZiI6MTc2NjM0Njc3OCwiZXhwIjoyMDY2MzQ2Nzc4LCJpYXQiOjE3NjYzNDY3Nzh9.KO41y02CYKQeiaNdfZqUgFa4RFKe0F1or0AfHl61QE8; userData={"userID":33,"userFirstName":"Abe","ipAddress":"","userLastName":"Perl","userEmail":"abeperl@gmail.com","userPhone":"6463166925","userMobile":"","userName":"abeperl","userPassword":"MTE5YXAyMTQAAAAAAAAAAAAAAABcy/W5AITs/7YaM/xHa7fd8RBXNvgTzZEunt+oP5G/+g==","isActive":true,"roleName":"Super Admin","storeName":"","roleId":1,"storeId":1,"isHistoryExist":false,"clientId":1,"printReceiptAuto":false,"userStorePermission":[{"description":"Dynamic Integration Screen","active":true,"storePermissionId":1,"recId":3,"pin":null},{"description":"Sales Return Screen","active":true,"storePermissionId":2,"recId":6,"pin":null}],"repId":null,"userAddress":null}; isRefreshedToken=false; chargingCard=; paymentinprocess=',
        'ClientId', '1',
        'StoreId', '1',
        'WarehouseId', '1'
    ),
    -- Payload for GetSaleOrderList POST request
    json('{"draw":1,"columns":[{"data":0,"name":"","searchable":true,"orderable":true,"search":{"value":"","regex":false}},{"data":[2],"name":"","searchable":true,"orderable":true,"search":{"value":"","regex":false}},{"data":[24],"name":"","searchable":true,"orderable":true,"search":{"value":"","regex":false}},{"data":[25],"name":"","searchable":true,"orderable":true,"search":{"value":"","regex":false}},{"data":[22],"name":"","searchable":true,"orderable":true,"search":{"value":"","regex":false}},{"data":[1],"name":"","searchable":true,"orderable":true,"search":{"value":"","regex":false}},{"data":[21],"name":"","searchable":true,"orderable":true,"search":{"value":"","regex":false}},{"data":[13],"name":"","searchable":true,"orderable":true,"search":{"value":"","regex":false}},{"data":[20],"name":"","searchable":true,"orderable":true,"search":{"value":"","regex":false}},{"data":[27],"name":"","searchable":true,"orderable":true,"search":{"value":"","regex":false}},{"data":[17],"name":"","searchable":true,"orderable":true,"search":{"value":"","regex":false}},{"data":[16],"name":"","searchable":true,"orderable":true,"search":{"value":"","regex":false}},{"data":12,"name":"","searchable":true,"orderable":false,"search":{"value":"","regex":false}}],"order":[{"column":5,"dir":"asc"}],"start":0,"length":50,"search":{"value":"","regex":false},"dateFrom":null,"dateTo":null,"status":1,"OrderStatusId":-1,"orderType":-1,"CustomStatusId":"0","StoreId":"1"}'),
    NULL,  -- Use Payload instead of Params
    0,     -- IsEnabled = 0 (disabled by default)
    NULL,  -- PrinterName (not needed for JSON export)
    json_object(
        'IdJsonPath', '[22]',  -- Extract order ID from response array at index 22
        'ChainedArrayJsonPath', 'data'  -- Response contains array under "data" property
    )
);

-- Get the PrimaryApiId for the newly inserted API
-- Store it in a variable for use in SubAction inserts
-- SQLite doesn't have variables, so we'll use a subquery in each INSERT

-- Sub-Action 1: Get Order Details for each order
INSERT INTO SubAction (
    PrimaryApiId,
    ActionNumber,
    ActionName,
    ActionType,
    Configuration,
    ExecutionOrder,
    IsEnabled
) VALUES (
    (SELECT Id FROM PrimaryApi WHERE ApiNumber = 7),
    1,
    'Get Order Details',
    'CallApi',
    json_object(
        'Endpoint', '/api/SaleOrder/GetSaleOrderById?saleOrderId={id}',
        'Method', 'GET',
        'OutputVariableName', 'orderDetails'  -- Store response for next action
    ),
    1,
    1  -- Enabled
);

-- Sub-Action 2: Save Order Details as JSON file
INSERT INTO SubAction (
    PrimaryApiId,
    ActionNumber,
    ActionName,
    ActionType,
    Configuration,
    ExecutionOrder,
    IsEnabled
) VALUES (
    (SELECT Id FROM PrimaryApi WHERE ApiNumber = 7),
    2,
    'Save Order Details to JSON File',
    'SaveJsonToFile',
    json_object(
        'Endpoint', 'sales-orders',  -- Directory to save files (relative to app directory)
        'RequestBody', 'order-{id}.json'  -- File name with {id} placeholder
    ),
    2,
    1  -- Enabled
);

-- Add auth credentials for malchus.3plnext.com
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
    'abeperl',  -- Update with actual username if needed
    '',  -- Password (empty if using long-lived token)
    'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJVc2VySW5mbyI6IkRsaFREWDFLak5xZGMwaDFDVHozWVVOdU9FSG9DMk1nWE56TFh5SDkxYVA0T1A0VjVVMUI2SFdmeGN1QWE4aUNLekZYS0luUTFadEM3RVVvWmxaazU2Q0R6emE1UGtRdWZNb2xqOW9GTnhMVk5YTXlWRGxDQ1A1bHJPNjVxLzE4WndYY2VLOHhLaVVNa2xpWDlsZkVpck50Q2k0L2V0Y2JSd1V4SXNWNkxoM2M2TGZoQ1JoWkI0SjJvdlNQdUI5MnhYWFU3dHhpQ3RxNzsxUGZKdUdvNmdUUWtrSTdNcXVVRi9rZnJQZnk1b2hMYUFrTXlMakkyVjB0NmQxajg5cFQ1WWk1S1B2bGZkRFpiZStEQnJNbWtud3RHWDZoaU9FMVk5SGhNZzBUZnIwVXlqNExxNWY3WmhWWmo4UlRXTTN0ZC95R3dGZG5ENmh3eCtETXFNOC9sT2Ixb1R5Q1FLK29SSzZ3bDZCWHZTR3BDS2ExSnFNU2wzdnJZTVJTbExHT2xRZ28xNSs0V3Y5SFJ1UVE5SVplUmJOZXdoQithdWI3Y013UHN5YUJnaVlXL2J1eWw2MkdiQy9jSXI4U2IwMnNkbUVCQ0U1SDI0dmQwSVIyYjdwakIxcURwbzNtNXJQZmo0VTNqeTlVWTNKWExSN05zUlg2ZkdPTnFvZGE5WjhjQTFYc1dRUTg1TW9KMm83QitiK0x4SWhqREhpYTlncGNUU1dSTDYxRzRpMzExcDJNN01TbDFJdll3c0ErV2FrNjZZQVRsY0VjOEJCcXlCcWNiaTVuQkdNV2t2U280SUZ3VlNxNHVQVHN4Uk5acy9UWWFjN3FHNlJyUkxvcFU4bFZleVpTYS9NMDBZNkI4eGNrVEVqb2pEYmZGOU16TzhhWkFsd281VW9PVm9scTZ4a2VSSm9QTzFwV0RCajg2MlRBOGJDL3k0TjRkRzhCemVQeUZyd0NWMndXYVVRNHFYYmdZZGtZb21lM0NNRDMydk5odVFvazRBMjI1WmdCa2tKS2d1ZjExMG5YclR5MzF1dmVEUXVEemhLR0szdVByZ29nc1kzVExTL2R3Q1E3ZmFJOWJsN1ZtYVlwK1QrR1VQeXBOdWNiOGZ4RFdWSWJKR0Rxak5nRUJlcDQ1dlRBaE5CL1NvZVhoZmZnVWdVPSIsIm5iZiI6MTc2NjM0Njc3OCwiZXhwIjoyMDY2MzQ2Nzc4LCJpYXQiOjE3NjYzNDY3Nzh9.KO41y02CYKQeiaNdfZqUgFa4RFKe0F1or0AfHl61QE8',
    '2066-12-22T00:00:00Z',  -- Long-lived token (300 years)
    CURRENT_TIMESTAMP,
    CURRENT_TIMESTAMP
);

-- Verify the configuration
SELECT
    'Primary API' as Type,
    p.ApiNumber,
    p.ApiName,
    p.BaseUrl,
    p.Endpoint,
    p.IsEnabled
FROM PrimaryApi p
WHERE p.ApiNumber = 7

UNION ALL

SELECT
    'Sub-Action' as Type,
    s.ActionNumber,
    s.ActionName,
    s.ActionType,
    s.Configuration,
    s.IsEnabled
FROM SubAction s
WHERE s.PrimaryApiId = (SELECT Id FROM PrimaryApi WHERE ApiNumber = 7)
ORDER BY Type DESC, ActionNumber;

-- NOTES:
-- 1. API is disabled by default (IsEnabled = 0). Enable it when ready to use.
-- 2. To enable the API: UPDATE PrimaryApi SET IsEnabled = 1 WHERE ApiNumber = 7;
-- 3. The IdJsonPath '[22]' extracts the order ID from index 22 of the data array response
-- 4. The first sub-action fetches detailed order data using GetSaleOrderById
-- 5. The second sub-action saves the order details to a JSON file in the sales-orders directory
-- 6. Files will be named like: order-12345.json (where 12345 is the order ID)
-- 7. The SaveJsonToFile action type needs to be implemented in SubActionExecutor.cs
-- 8. Bearer token is a long-lived JWT token (expires in year 2066)