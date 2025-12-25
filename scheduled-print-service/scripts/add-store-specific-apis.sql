-- Migration: Create Store-Specific APIs (8-12) based on API 7
-- Date: 2025-12-25
-- Description: Creates API configurations for 5 stores, each with its own StoreId
-- Each store gets its own API and SubActions for Sales Order Export
--
-- Stores:
--   API 8:  StoreId 2 - Boro Park
--   API 9:  StoreId 3 - Monsey
--   API 10: StoreId 4 - Monroe
--   API 11: StoreId 5 - Sales
--   API 12: StoreId 6 - Office
--
-- Note: The SaveJsonToFile action now supports StoreName property which appends
--       the store name to the output directory path

-- ============================================================================
-- API 8: Boro Park (StoreId = 2)
-- ============================================================================
INSERT INTO PrimaryApi (
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
    Configuration
) VALUES (
    8,
    'Sales Order Export API - Boro Park',
    'https://malchus.3plnext.com',
    '/api/SaleOrder/GetSaleOrderList',
    'POST',
    json_object(
        'Authorization', 'Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJVc2VySW5mbyI6IkRsaFREWDFLak5xZGMwaDFDVHozWVVOdU9FSG9DMk1nWE56TFh5SDkxYVA0T1A0VjVVMUI2SFdmeGN1QWE4aUNLekZYS0luUTFadEM3RVVvWmxaazU2Q0R6emE1UGtRdWZNb2xqOW9GTnhMVk5YTXlWRGxDQ1A1bHJPNjVxLzE4WndYY2VLOHhLaVVNa2xpWDlsZkVpck50Q2k0L2V0Y2JSd1V4SXNWNkxoM2M2TGZoQ1JoWkI0SjJvdlNQdUI5MnhYWFU3dHhpQ3RxNysxUGZKdUdvNmdUUWtrSTdNcXVVRi9rZnJQZnk1b2hMYUFrTXlMakkyVjB0NmQxajg5cFQ1WWk1S1B2bGZkRFpiZStEQnJNbWtud3RHWDZoaU9FMVk5SGhNZzBUZnIwVXlqNExxNWY3WmhWWmo4UlRXTTN0ZC95R3dGZG5ENmh3eCtETXFNOC9sT2Ixb1R5Q1FLK29SSzZ3bDZCWHZTR3BDS2ExSnFNU2wzdnJZTVJTbExHT2xRZ28xNSs0V3Y5SFJ1UVE5SVplUmJOZXdoQithdWI3Y013UHN5YUJnaVlXL2J1eWw2MkdiQy9jSXI4U2IwMnNkbUVCQ0U1SDI0dmQwSVIyYjdwakIxcURwbzNtNXJQZmo0VTNqeTlVWTNKWExSN05zUlg2ZkdPTnFvZGE5WjhjQTFYc1dRUTg1TW9KMm83QitiK0x4SWhqREhpYTlncGNUU1dSTDYxRzRpMzExcDJNN01TbDFJdll3c0ErV2FrNjZZQVRsY0VjOEJCcXlCcWNiaTVuQkdNV2t2U280SUZ3VlNxNHVQVHN4Uk5acy9UWWFjN3FHNlJyUkxvcFU4bFZleVpTYS9NMDBZNkI4eGNrVEVqb2pEYmZGOU16TzhhWkFsd281VW9PVm9scTZ4a2VSSm9QTzFwV0RCajg2MlRBOGJDL3k0TjRkRzhCemVQeUZyd0NWMndXYVVRNHFYYmdZZGtZb21lM0NNRDMydk5odVFvazRBMjI1WmdCa2tKS2d1ZjExMG5YclR5MzF1dmVEUXVEemhLR0szdVByZ29nc1kzVExTL2R3Q1E3ZmFJOWJsN1ZtYVlwK1QrR1VQeXBOdWNiOGZ4RFdWSWJKR0Rxak5nRUJlcDQ1dlRBaE5CL1NvZVhoZmZnVWdVPSIsIm5iZiI6MTc2NjM0Njc3OCwiZXhwIjoyMDY2MzQ2Nzc4LCJpYXQiOjE3NjYzNDY3Nzh9.KO41y02CYKQeiaNdfZqUgFa4RFKe0F1or0AfHl61QE8',
        'Cookie', 'token=eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJVc2VySW5mbyI6IkRsaFREWDFLak5xZGMwaDFDVHozWVVOdU9FSG9DMk1nWE56TFh5SDkxYVA0T1A0VjVVMUI2SFdmeGN1QWE4aUNLekZYS0luUTFadEM3RVVvWmxaazU2Q0R6emE1UGtRdWZNb2xqOW9GTnhMVk5YTXlWRGxDQ1A1bHJPNjVxLzE4WndYY2VLOHhLaVVNa2xpWDlsZkVpck50Q2k4L2V0Y2JSd1V4SXNWNkxoM2M2TGZoQ1JoWkI0SjJvdlNQdUI5MnhYWFU3dHhpQ3RxNzsxUGZKdUdvNmdUUWtrSTdNcXVVRi9rZnJQZnk1b2hMYUFrTXlMakkyVjB0NmQxajg5cFQ1WWk1S1B2bGZkRFpiZStEQnJNbWtud3RHWDZoaU9FMVk5SGhNZzBUZnIwVXlqNExxNWY3WmhWWmo4UlRXTTN0ZC95R3dGZG5ENmh3eCtETXFNOC9sT2Ixb1R5Q1FLK29SSzZ3bDZCWHZTR3BDS2ExSnFNU2wzdnJZTVJTbExHT2xRZ28xNSs0V3Y5SFJ1UVE5SVplUmJOZXdoQithdWI3Y013UHN5YUJnaVlXL2J1eWw2MkdiQy9jSXI4U2IwMnNkbUVCQ0U1SDI0dmQwSVIyYjdwakIxcURwbzNtNXJQZmo0VTNqeTlVWTNKWExSN05zUlg2ZkdPTnFvZGE5WjhjQTFYc1dRUTg1TW9KMm83QitiK0x4SWhqREhpYTlncGNUU1dSTDYxRzRpMzExcDJNN01TbDFJdll3c0ErV2FrNjZZQVRsY0VjOEJCcXlCcWNiaTVuQkdNV2t2U280SUZ3VlNxNHVQVHN4Uk5acy9UWWFjN3FHNlJyUkxvcFU4bFZleVpTYS9NMDBZNkI4eGNrVEVqb2pEYmZGOU16TzhhWkFsd281VW9PVm9scTZ4a2VSSm9QTzFwV0RCajg2MlRBOGJDL3k0TjRkRzhCemVQeUZyd0NWMndXYVVRNHFYYmdZZGtZb21lM0NNRDMydk5odVFvazRBMjI1WmdCa2tKS2d1ZjExMG5YclR5MzF1dmVEUXVEemhLR0szdVByZ29nc1kzVExTL2R3Q1E3ZmFJOWJsN1ZtYVlwK1QrR1VQeXBOdWNiOGZ4RFdWSWJKR0Rxak5nRUJlcDQ1dlRBaE5CL1NvZVhoZmZnVWdVPSIsIm5iZiI6MTc2NjM0Njc3OCwiZXhwIjoyMDY2MzQ2Nzc4LCJpYXQiOjE3NjYzNDY3Nzh9.KO41y02CYKQeiaNdfZqUgFa4RFKe0F1or0AfHl61QE8; userData={"userID":33,"userFirstName":"Abe","ipAddress":"","userLastName":"Perl","userEmail":"abeperl@gmail.com","userPhone":"6463166925","userMobile":"","userName":"abeperl","userPassword":"MTE5YXAyMTQAAAAAAAAAAAAAAABcy/W5AITs/7YaM/xHa7fd8RBXNvgTzZEunt+oP5G/+g==","isActive":true,"roleName":"Super Admin","storeName":"","roleId":1,"storeId":2,"isHistoryExist":false,"clientId":1,"printReceiptAuto":false,"userStorePermission":[{"description":"Dynamic Integration Screen","active":true,"storePermissionId":1,"recId":3,"pin":null},{"description":"Sales Return Screen","active":true,"storePermissionId":2,"recId":6,"pin":null}],"repId":null,"userAddress":null}; isRefreshedToken=false; chargingCard=; paymentinprocess=',
        'ClientId', '1',
        'StoreId', '2',
        'WarehouseId', '1'
    ),
    json('{"draw":1,"columns":[{"data":0,"name":"","searchable":true,"orderable":true,"search":{"value":"","regex":false}},{"data":[2],"name":"","searchable":true,"orderable":true,"search":{"value":"","regex":false}},{"data":[24],"name":"","searchable":true,"orderable":true,"search":{"value":"","regex":false}},{"data":[25],"name":"","searchable":true,"orderable":true,"search":{"value":"","regex":false}},{"data":[22],"name":"","searchable":true,"orderable":true,"search":{"value":"","regex":false}},{"data":[1],"name":"","searchable":true,"orderable":true,"search":{"value":"","regex":false}},{"data":[21],"name":"","searchable":true,"orderable":true,"search":{"value":"","regex":false}},{"data":[13],"name":"","searchable":true,"orderable":true,"search":{"value":"","regex":false}},{"data":[20],"name":"","searchable":true,"orderable":true,"search":{"value":"","regex":false}},{"data":[27],"name":"","searchable":true,"orderable":true,"search":{"value":"","regex":false}},{"data":[17],"name":"","searchable":true,"orderable":true,"search":{"value":"","regex":false}},{"data":[16],"name":"","searchable":true,"orderable":true,"search":{"value":"","regex":false}},{"data":12,"name":"","searchable":true,"orderable":false,"search":{"value":"","regex":false}}],"order":[{"column":5,"dir":"asc"}],"start":0,"length":50,"search":{"value":"","regex":false},"dateFrom":null,"dateTo":null,"status":1,"OrderStatusId":-1,"orderType":-1,"CustomStatusId":"0","StoreId":"2"}'),
    NULL,
    0,
    NULL,
    json_object(
        'IdJsonPath', '[5]',
        'ChainedArrayJsonPath', 'response.result.data'
    )
);

-- Sub-Action 1 for API 8: Get Order Details
INSERT INTO SubAction (
    PrimaryApiId,
    ActionNumber,
    ActionName,
    ActionType,
    Configuration,
    ExecutionOrder,
    IsEnabled
) VALUES (
    (SELECT Id FROM PrimaryApi WHERE ApiNumber = 8),
    1,
    'Get Order Details',
    'CallApi',
    json_object(
        'Endpoint', '/api/SaleOrder/GetSaleOrderById?saleOrderId={id}',
        'Method', 'GET',
        'OutputVariableName', 'orderDetails'
    ),
    1,
    1
);

-- Sub-Action 2 for API 8: Save Order Details as JSON file
INSERT INTO SubAction (
    PrimaryApiId,
    ActionNumber,
    ActionName,
    ActionType,
    Configuration,
    ExecutionOrder,
    IsEnabled
) VALUES (
    (SELECT Id FROM PrimaryApi WHERE ApiNumber = 8),
    2,
    'Save Order Details to JSON File',
    'SaveJsonToFile',
    json_object(
        'Endpoint', 'sales-orders',
        'RequestBody', 'order-{id}.json',
        'StoreName', 'Boro Park'
    ),
    2,
    1
);

-- ============================================================================
-- API 9: Monsey (StoreId = 3)
-- ============================================================================
INSERT INTO PrimaryApi (
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
    Configuration
) VALUES (
    9,
    'Sales Order Export API - Monsey',
    'https://malchus.3plnext.com',
    '/api/SaleOrder/GetSaleOrderList',
    'POST',
    json_object(
        'Authorization', 'Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJVc2VySW5mbyI6IkRsaFREWDFLak5xZGMwaDFDVHozWVVOdU9FSG9DMk1nWE56TFh5SDkxYVA0T1A0VjVVMUI2SFdmeGN1QWE4aUNLekZYS0luUTFadEM3RVVvWmxaazU2Q0R6emE1UGtRdWZNb2xqOW9GTnhMVk5YTXlWRGxDQ1A1bHJPNjVxLzE4WndYY2VLOHhLaVVNa2xpWDlsZkVpck50Q2k0L2V0Y2JSd1V4SXNWNkxoM2M2TGZoQ1JoWkI0SjJvdlNQdUI5MnhYWFU3dHhpQ3RxNysxUGZKdUdvNmdUUWtrSTdNcXVVRi9rZnJQZnk1b2hMYUFrTXlMakkyVjB0NmQxajg5cFQ1WWk1S1B2bGZkRFpiZStEQnJNbWtud3RHWDZoaU9FMVk5SGhNZzBUZnIwVXlqNExxNWY3WmhWWmo4UlRXTTN0ZC95R3dGZG5ENmh3eCtETXFNOC9sT2Ixb1R5Q1FLK29SSzZ3bDZCWHZTR3BDS2ExSnFNU2wzdnJZTVJTbExHT2xRZ28xNSs0V3Y5SFJ1UVE5SVplUmJOZXdoQithdWI3Y013UHN5YUJnaVlXL2J1eWw2MkdiQy9jSXI4U2IwMnNkbUVCQ0U1SDI0dmQwSVIyYjdwakIxcURwbzNtNXJQZmo0VTNqeTlVWTNKWExSN05zUlg2ZkdPTnFvZGE5WjhjQTFYc1dRUTg1TW9KMm83QitiK0x4SWhqREhpYTlncGNUU1dSTDYxRzRpMzExcDJNN01TbDFJdll3c0ErV2FrNjZZQVRsY0VjOEJCcXlCcWNiaTVuQkdNV2t2U280SUZ3VlNxNHVQVHN4Uk5acy9UWWFjN3FHNlJyUkxvcFU4bFZleVpTYS9NMDBZNkI4eGNrVEVqb2pEYmZGOU16TzhhWkFsd281VW9PVm9scTZ4a2VSSm9QTzFwV0RCajg2MlRBOGJDL3k0TjRkRzhCemVQeUZyd0NWMndXYVVRNHFYYmdZZGtZb21lM0NNRDMydk5odVFvazRBMjI1WmdCa2tKS2d1ZjExMG5YclR5MzF1dmVEUXVEemhLR0szdVByZ29nc1kzVExTL2R3Q1E3ZmFJOWJsN1ZtYVlwK1QrR1VQeXBOdWNiOGZ4RFdWSWJKR0Rxak5nRUJlcDQ1dlRBaE5CL1NvZVhoZmZnVWdVPSIsIm5iZiI6MTc2NjM0Njc3OCwiZXhwIjoyMDY2MzQ2Nzc4LCJpYXQiOjE3NjYzNDY3Nzh9.KO41y02CYKQeiaNdfZqUgFa4RFKe0F1or0AfHl61QE8',
        'Cookie', 'token=eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJVc2VySW5mbyI6IkRsaFREWDFLak5xZGMwaDFDVHozWVVOdU9FSG9DMk1nWE56TFh5SDkxYVA0T1A0VjVVMUI2SFdmeGN1QWE4aUNLekZYS0luUTFadEM3RVVvWmxaazU2Q0R6emE1UGtRdWZNb2xqOW9GTnhMVk5YTXlWRGxDQ1A1bHJPNjVxLzE4WndYY2VLOHhLaVVNa2xpWDlsZkVpck50Q2k4L2V0Y2JSd1V4SXNWNkxoM2M2TGZoQ1JoWkI0SjJvdlNQdUI5MnhYWFU3dHhpQ3RxNzsxUGZKdUdvNmdUUWtrSTdNcXVVRi9rZnJQZnk1b2hMYUFrTXlMakkyVjB0NmQxajg5cFQ1WWk1S1B2bGZkRFpiZStEQnJNbWtud3RHWDZoaU9FMVk5SGhNZzBUZnIwVXlqNExxNWY3WmhWWmo4UlRXTTN0ZC95R3dGZG5ENmh3eCtETXFNOC9sT2Ixb1R5Q1FLK29SSzZ3bDZCWHZTR3BDS2ExSnFNU2wzdnJZTVJTbExHT2xRZ28xNSs0V3Y5SFJ1UVE5SVplUmJOZXdoQithdWI3Y013UHN5YUJnaVlXL2J1eWw2MkdiQy9jSXI4U2IwMnNkbUVCQ0U1SDI0dmQwSVIyYjdwakIxcURwbzNtNXJQZmo0VTNqeTlVWTNKWExSN05zUlg2ZkdPTnFvZGE5WjhjQTFYc1dRUTg1TW9KMm83QitiK0x4SWhqREhpYTlncGNUU1dSTDYxRzRpMzExcDJNN01TbDFJdll3c0ErV2FrNjZZQVRsY0VjOEJCcXlCcWNiaTVuQkdNV2t2U280SUZ3VlNxNHVQVHN4Uk5acy9UWWFjN3FHNlJyUkxvcFU4bFZleVpTYS9NMDBZNkI4eGNrVEVqb2pEYmZGOU16TzhhWkFsd281VW9PVm9scTZ4a2VSSm9QTzFwV0RCajg2MlRBOGJDL3k0TjRkRzhCemVQeUZyd0NWMndXYVVRNHFYYmdZZGtZb21lM0NNRDMydk5odVFvazRBMjI1WmdCa2tKS2d1ZjExMG5YclR5MzF1dmVEUXVEemhLR0szdVByZ29nc1kzVExTL2R3Q1E3ZmFJOWJsN1ZtYVlwK1QrR1VQeXBOdWNiOGZ4RFdWSWJKR0Rxak5nRUJlcDQ1dlRBaE5CL1NvZVhoZmZnVWdVPSIsIm5iZiI6MTc2NjM0Njc3OCwiZXhwIjoyMDY2MzQ2Nzc4LCJpYXQiOjE3NjYzNDY3Nzh9.KO41y02CYKQeiaNdfZqUgFa4RFKe0F1or0AfHl61QE8; userData={"userID":33,"userFirstName":"Abe","ipAddress":"","userLastName":"Perl","userEmail":"abeperl@gmail.com","userPhone":"6463166925","userMobile":"","userName":"abeperl","userPassword":"MTE5YXAyMTQAAAAAAAAAAAAAAABcy/W5AITs/7YaM/xHa7fd8RBXNvgTzZEunt+oP5G/+g==","isActive":true,"roleName":"Super Admin","storeName":"","roleId":1,"storeId":3,"isHistoryExist":false,"clientId":1,"printReceiptAuto":false,"userStorePermission":[{"description":"Dynamic Integration Screen","active":true,"storePermissionId":1,"recId":3,"pin":null},{"description":"Sales Return Screen","active":true,"storePermissionId":2,"recId":6,"pin":null}],"repId":null,"userAddress":null}; isRefreshedToken=false; chargingCard=; paymentinprocess=',
        'ClientId', '1',
        'StoreId', '3',
        'WarehouseId', '1'
    ),
    json('{"draw":1,"columns":[{"data":0,"name":"","searchable":true,"orderable":true,"search":{"value":"","regex":false}},{"data":[2],"name":"","searchable":true,"orderable":true,"search":{"value":"","regex":false}},{"data":[24],"name":"","searchable":true,"orderable":true,"search":{"value":"","regex":false}},{"data":[25],"name":"","searchable":true,"orderable":true,"search":{"value":"","regex":false}},{"data":[22],"name":"","searchable":true,"orderable":true,"search":{"value":"","regex":false}},{"data":[1],"name":"","searchable":true,"orderable":true,"search":{"value":"","regex":false}},{"data":[21],"name":"","searchable":true,"orderable":true,"search":{"value":"","regex":false}},{"data":[13],"name":"","searchable":true,"orderable":true,"search":{"value":"","regex":false}},{"data":[20],"name":"","searchable":true,"orderable":true,"search":{"value":"","regex":false}},{"data":[27],"name":"","searchable":true,"orderable":true,"search":{"value":"","regex":false}},{"data":[17],"name":"","searchable":true,"orderable":true,"search":{"value":"","regex":false}},{"data":[16],"name":"","searchable":true,"orderable":true,"search":{"value":"","regex":false}},{"data":12,"name":"","searchable":true,"orderable":false,"search":{"value":"","regex":false}}],"order":[{"column":5,"dir":"asc"}],"start":0,"length":50,"search":{"value":"","regex":false},"dateFrom":null,"dateTo":null,"status":1,"OrderStatusId":-1,"orderType":-1,"CustomStatusId":"0","StoreId":"3"}'),
    NULL,
    0,
    NULL,
    json_object(
        'IdJsonPath', '[5]',
        'ChainedArrayJsonPath', 'response.result.data'
    )
);

-- Sub-Action 1 for API 9: Get Order Details
INSERT INTO SubAction (
    PrimaryApiId,
    ActionNumber,
    ActionName,
    ActionType,
    Configuration,
    ExecutionOrder,
    IsEnabled
) VALUES (
    (SELECT Id FROM PrimaryApi WHERE ApiNumber = 9),
    1,
    'Get Order Details',
    'CallApi',
    json_object(
        'Endpoint', '/api/SaleOrder/GetSaleOrderById?saleOrderId={id}',
        'Method', 'GET',
        'OutputVariableName', 'orderDetails'
    ),
    1,
    1
);

-- Sub-Action 2 for API 9: Save Order Details as JSON file
INSERT INTO SubAction (
    PrimaryApiId,
    ActionNumber,
    ActionName,
    ActionType,
    Configuration,
    ExecutionOrder,
    IsEnabled
) VALUES (
    (SELECT Id FROM PrimaryApi WHERE ApiNumber = 9),
    2,
    'Save Order Details to JSON File',
    'SaveJsonToFile',
    json_object(
        'Endpoint', 'sales-orders',
        'RequestBody', 'order-{id}.json',
        'StoreName', 'Monsey'
    ),
    2,
    1
);

-- ============================================================================
-- API 10: Monroe (StoreId = 4)
-- ============================================================================
INSERT INTO PrimaryApi (
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
    Configuration
) VALUES (
    10,
    'Sales Order Export API - Monroe',
    'https://malchus.3plnext.com',
    '/api/SaleOrder/GetSaleOrderList',
    'POST',
    json_object(
        'Authorization', 'Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJVc2VySW5mbyI6IkRsaFREWDFLak5xZGMwaDFDVHozWVVOdU9FSG9DMk1nWE56TFh5SDkxYVA0T1A0VjVVMUI2SFdmeGN1QWE4aUNLekZYS0luUTFadEM3RVVvWmxaazU2Q0R6emE1UGtRdWZNb2xqOW9GTnhMVk5YTXlWRGxDQ1A1bHJPNjVxLzE4WndYY2VLOHhLaVVNa2xpWDlsZkVpck50Q2k0L2V0Y2JSd1V4SXNWNkxoM2M2TGZoQ1JoWkI0SjJvdlNQdUI5MnhYWFU3dHhpQ3RxNysxUGZKdUdvNmdUUWtrSTdNcXVVRi9rZnJQZnk1b2hMYUFrTXlMakkyVjB0NmQxajg5cFQ1WWk1S1B2bGZkRFpiZStEQnJNbWtud3RHWDZoaU9FMVk5SGhNZzBUZnIwVXlqNExxNWY3WmhWWmo4UlRXTTN0ZC95R3dGZG5ENmh3eCtETXFNOC9sT2Ixb1R5Q1FLK29SSzZ3bDZCWHZTR3BDS2ExSnFNU2wzdnJZTVJTbExHT2xRZ28xNSs0V3Y5SFJ1UVE5SVplUmJOZXdoQithdWI3Y013UHN5YUJnaVlXL2J1eWw2MkdiQy9jSXI4U2IwMnNkbUVCQ0U1SDI0dmQwSVIyYjdwakIxcURwbzNtNXJQZmo0VTNqeTlVWTNKWExSN05zUlg2ZkdPTnFvZGE5WjhjQTFYc1dRUTg1TW9KMm83QitiK0x4SWhqREhpYTlncGNUU1dSTDYxRzRpMzExcDJNN01TbDFJdll3c0ErV2FrNjZZQVRsY0VjOEJCcXlCcWNiaTVuQkdNV2t2U280SUZ3VlNxNHVQVHN4Uk5acy9UWWFjN3FHNlJyUkxvcFU4bFZleVpTYS9NMDBZNkI4eGNrVEVqb2pEYmZGOU16TzhhWkFsd281VW9PVm9scTZ4a2VSSm9QTzFwV0RCajg2MlRBOGJDL3k0TjRkRzhCemVQeUZyd0NWMndXYVVRNHFYYmdZZGtZb21lM0NNRDMydk5odVFvazRBMjI1WmdCa2tKS2d1ZjExMG5YclR5MzF1dmVEUXVEemhLR0szdVByZ29nc1kzVExTL2R3Q1E3ZmFJOWJsN1ZtYVlwK1QrR1VQeXBOdWNiOGZ4RFdWSWJKR0Rxak5nRUJlcDQ1dlRBaE5CL1NvZVhoZmZnVWdVPSIsIm5iZiI6MTc2NjM0Njc3OCwiZXhwIjoyMDY2MzQ2Nzc4LCJpYXQiOjE3NjYzNDY3Nzh9.KO41y02CYKQeiaNdfZqUgFa4RFKe0F1or0AfHl61QE8',
        'Cookie', 'token=eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJVc2VySW5mbyI6IkRsaFREWDFLak5xZGMwaDFDVHozWVVOdU9FSG9DMk1nWE56TFh5SDkxYVA0T1A0VjVVMUI2SFdmeGN1QWE4aUNLekZYS0luUTFadEM3RVVvWmxaazU2Q0R6emE1UGtRdWZNb2xqOW9GTnhMVk5YTXlWRGxDQ1A1bHJPNjVxLzE4WndYY2VLOHhLaVVNa2xpWDlsZkVpck50Q2k4L2V0Y2JSd1V4SXNWNkxoM2M2TGZoQ1JoWkI0SjJvdlNQdUI5MnhYWFU3dHhpQ3RxNzsxUGZKdUdvNmdUUWtrSTdNcXVVRi9rZnJQZnk1b2hMYUFrTXlMakkyVjB0NmQxajg5cFQ1WWk1S1B2bGZkRFpiZStEQnJNbWtud3RHWDZoaU9FMVk5SGhNZzBUZnIwVXlqNExxNWY3WmhWWmo4UlRXTTN0ZC95R3dGZG5ENmh3eCtETXFNOC9sT2Ixb1R5Q1FLK29SSzZ3bDZCWHZTR3BDS2ExSnFNU2wzdnJZTVJTbExHT2xRZ28xNSs0V3Y5SFJ1UVE5SVplUmJOZXdoQithdWI3Y013UHN5YUJnaVlXL2J1eWw2MkdiQy9jSXI4U2IwMnNkbUVCQ0U1SDI0dmQwSVIyYjdwakIxcURwbzNtNXJQZmo0VTNqeTlVWTNKWExSN05zUlg2ZkdPTnFvZGE5WjhjQTFYc1dRUTg1TW9KMm83QitiK0x4SWhqREhpYTlncGNUU1dSTDYxRzRpMzExcDJNN01TbDFJdll3c0ErV2FrNjZZQVRsY0VjOEJCcXlCcWNiaTVuQkdNV2t2U280SUZ3VlNxNHVQVHN4Uk5acy9UWWFjN3FHNlJyUkxvcFU4bFZleVpTYS9NMDBZNkI4eGNrVEVqb2pEYmZGOU16TzhhWkFsd281VW9PVm9scTZ4a2VSSm9QTzFwV0RCajg2MlRBOGJDL3k0TjRkRzhCemVQeUZyd0NWMndXYVVRNHFYYmdZZGtZb21lM0NNRDMydk5odVFvazRBMjI1WmdCa2tKS2d1ZjExMG5YclR5MzF1dmVEUXVEemhLR0szdVByZ29nc1kzVExTL2R3Q1E3ZmFJOWJsN1ZtYVlwK1QrR1VQeXBOdWNiOGZ4RFdWSWJKR0Rxak5nRUJlcDQ1dlRBaE5CL1NvZVhoZmZnVWdVPSIsIm5iZiI6MTc2NjM0Njc3OCwiZXhwIjoyMDY2MzQ2Nzc4LCJpYXQiOjE3NjYzNDY3Nzh9.KO41y02CYKQeiaNdfZqUgFa4RFKe0F1or0AfHl61QE8; userData={"userID":33,"userFirstName":"Abe","ipAddress":"","userLastName":"Perl","userEmail":"abeperl@gmail.com","userPhone":"6463166925","userMobile":"","userName":"abeperl","userPassword":"MTE5YXAyMTQAAAAAAAAAAAAAAABcy/W5AITs/7YaM/xHa7fd8RBXNvgTzZEunt+oP5G/+g==","isActive":true,"roleName":"Super Admin","storeName":"","roleId":1,"storeId":4,"isHistoryExist":false,"clientId":1,"printReceiptAuto":false,"userStorePermission":[{"description":"Dynamic Integration Screen","active":true,"storePermissionId":1,"recId":3,"pin":null},{"description":"Sales Return Screen","active":true,"storePermissionId":2,"recId":6,"pin":null}],"repId":null,"userAddress":null}; isRefreshedToken=false; chargingCard=; paymentinprocess=',
        'ClientId', '1',
        'StoreId', '4',
        'WarehouseId', '1'
    ),
    json('{"draw":1,"columns":[{"data":0,"name":"","searchable":true,"orderable":true,"search":{"value":"","regex":false}},{"data":[2],"name":"","searchable":true,"orderable":true,"search":{"value":"","regex":false}},{"data":[24],"name":"","searchable":true,"orderable":true,"search":{"value":"","regex":false}},{"data":[25],"name":"","searchable":true,"orderable":true,"search":{"value":"","regex":false}},{"data":[22],"name":"","searchable":true,"orderable":true,"search":{"value":"","regex":false}},{"data":[1],"name":"","searchable":true,"orderable":true,"search":{"value":"","regex":false}},{"data":[21],"name":"","searchable":true,"orderable":true,"search":{"value":"","regex":false}},{"data":[13],"name":"","searchable":true,"orderable":true,"search":{"value":"","regex":false}},{"data":[20],"name":"","searchable":true,"orderable":true,"search":{"value":"","regex":false}},{"data":[27],"name":"","searchable":true,"orderable":true,"search":{"value":"","regex":false}},{"data":[17],"name":"","searchable":true,"orderable":true,"search":{"value":"","regex":false}},{"data":[16],"name":"","searchable":true,"orderable":true,"search":{"value":"","regex":false}},{"data":12,"name":"","searchable":true,"orderable":false,"search":{"value":"","regex":false}}],"order":[{"column":5,"dir":"asc"}],"start":0,"length":50,"search":{"value":"","regex":false},"dateFrom":null,"dateTo":null,"status":1,"OrderStatusId":-1,"orderType":-1,"CustomStatusId":"0","StoreId":"4"}'),
    NULL,
    0,
    NULL,
    json_object(
        'IdJsonPath', '[5]',
        'ChainedArrayJsonPath', 'response.result.data'
    )
);

-- Sub-Action 1 for API 10: Get Order Details
INSERT INTO SubAction (
    PrimaryApiId,
    ActionNumber,
    ActionName,
    ActionType,
    Configuration,
    ExecutionOrder,
    IsEnabled
) VALUES (
    (SELECT Id FROM PrimaryApi WHERE ApiNumber = 10),
    1,
    'Get Order Details',
    'CallApi',
    json_object(
        'Endpoint', '/api/SaleOrder/GetSaleOrderById?saleOrderId={id}',
        'Method', 'GET',
        'OutputVariableName', 'orderDetails'
    ),
    1,
    1
);

-- Sub-Action 2 for API 10: Save Order Details as JSON file
INSERT INTO SubAction (
    PrimaryApiId,
    ActionNumber,
    ActionName,
    ActionType,
    Configuration,
    ExecutionOrder,
    IsEnabled
) VALUES (
    (SELECT Id FROM PrimaryApi WHERE ApiNumber = 10),
    2,
    'Save Order Details to JSON File',
    'SaveJsonToFile',
    json_object(
        'Endpoint', 'sales-orders',
        'RequestBody', 'order-{id}.json',
        'StoreName', 'Monroe'
    ),
    2,
    1
);

-- ============================================================================
-- API 11: Sales (StoreId = 5)
-- ============================================================================
INSERT INTO PrimaryApi (
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
    Configuration
) VALUES (
    11,
    'Sales Order Export API - Sales',
    'https://malchus.3plnext.com',
    '/api/SaleOrder/GetSaleOrderList',
    'POST',
    json_object(
        'Authorization', 'Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJVc2VySW5mbyI6IkRsaFREWDFLak5xZGMwaDFDVHozWVVOdU9FSG9DMk1nWE56TFh5SDkxYVA0T1A0VjVVMUI2SFdmeGN1QWE4aUNLekZYS0luUTFadEM3RVVvWmxaazU2Q0R6emE1UGtRdWZNb2xqOW9GTnhMVk5YTXlWRGxDQ1A1bHJPNjVxLzE4WndYY2VLOHhLaVVNa2xpWDlsZkVpck50Q2k0L2V0Y2JSd1V4SXNWNkxoM2M2TGZoQ1JoWkI0SjJvdlNQdUI5MnhYWFU3dHhpQ3RxNysxUGZKdUdvNmdUUWtrSTdNcXVVRi9rZnJQZnk1b2hMYUFrTXlMakkyVjB0NmQxajg5cFQ1WWk1S1B2bGZkRFpiZStEQnJNbWtud3RHWDZoaU9FMVk5SGhNZzBUZnIwVXlqNExxNWY3WmhWWmo4UlRXTTN0ZC95R3dGZG5ENmh3eCtETXFNOC9sT2Ixb1R5Q1FLK29SSzZ3bDZCWHZTR3BDS2ExSnFNU2wzdnJZTVJTbExHT2xRZ28xNSs0V3Y5SFJ1UVE5SVplUmJOZXdoQithdWI3Y013UHN5YUJnaVlXL2J1eWw2MkdiQy9jSXI4U2IwMnNkbUVCQ0U1SDI0dmQwSVIyYjdwakIxcURwbzNtNXJQZmo0VTNqeTlVWTNKWExSN05zUlg2ZkdPTnFvZGE5WjhjQTFYc1dRUTg1TW9KMm83QitiK0x4SWhqREhpYTlncGNUU1dSTDYxRzRpMzExcDJNN01TbDFJdll3c0ErV2FrNjZZQVRsY0VjOEJCcXlCcWNiaTVuQkdNV2t2U280SUZ3VlNxNHVQVHN4Uk5acy9UWWFjN3FHNlJyUkxvcFU4bFZleVpTYS9NMDBZNkI4eGNrVEVqb2pEYmZGOU16TzhhWkFsd281VW9PVm9scTZ4a2VSSm9QTzFwV0RCajg2MlRBOGJDL3k0TjRkRzhCemVQeUZyd0NWMndXYVVRNHFYYmdZZGtZb21lM0NNRDMydk5odVFvazRBMjI1WmdCa2tKS2d1ZjExMG5YclR5MzF1dmVEUXVEemhLR0szdVByZ29nc1kzVExTL2R3Q1E3ZmFJOWJsN1ZtYVlwK1QrR1VQeXBOdWNiOGZ4RFdWSWJKR0Rxak5nRUJlcDQ1dlRBaE5CL1NvZVhoZmZnVWdVPSIsIm5iZiI6MTc2NjM0Njc3OCwiZXhwIjoyMDY2MzQ2Nzc4LCJpYXQiOjE3NjYzNDY3Nzh9.KO41y02CYKQeiaNdfZqUgFa4RFKe0F1or0AfHl61QE8',
        'Cookie', 'token=eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJVc2VySW5mbyI6IkRsaFREWDFLak5xZGMwaDFDVHozWVVOdU9FSG9DMk1nWE56TFh5SDkxYVA0T1A0VjVVMUI2SFdmeGN1QWE4aUNLekZYS0luUTFadEM3RVVvWmxaazU2Q0R6emE1UGtRdWZNb2xqOW9GTnhMVk5YTXlWRGxDQ1A1bHJPNjVxLzE4WndYY2VLOHhLaVVNa2xpWDlsZkVpck50Q2k4L2V0Y2JSd1V4SXNWNkxoM2M2TGZoQ1JoWkI0SjJvdlNQdUI5MnhYWFU3dHhpQ3RxNzsxUGZKdUdvNmdUUWtrSTdNcXVVRi9rZnJQZnk1b2hMYUFrTXlMakkyVjB0NmQxajg5cFQ1WWk1S1B2bGZkRFpiZStEQnJNbWtud3RHWDZoaU9FMVk5SGhNZzBUZnIwVXlqNExxNWY3WmhWWmo4UlRXTTN0ZC95R3dGZG5ENmh3eCtETXFNOC9sT2Ixb1R5Q1FLK29SSzZ3bDZCWHZTR3BDS2ExSnFNU2wzdnJZTVJTbExHT2xRZ28xNSs0V3Y5SFJ1UVE5SVplUmJOZXdoQithdWI3Y013UHN5YUJnaVlXL2J1eWw2MkdiQy9jSXI4U2IwMnNkbUVCQ0U1SDI0dmQwSVIyYjdwakIxcURwbzNtNXJQZmo0VTNqeTlVWTNKWExSN05zUlg2ZkdPTnFvZGE5WjhjQTFYc1dRUTg1TW9KMm83QitiK0x4SWhqREhpYTlncGNUU1dSTDYxRzRpMzExcDJNN01TbDFJdll3c0ErV2FrNjZZQVRsY0VjOEJCcXlCcWNiaTVuQkdNV2t2U280SUZ3VlNxNHVQVHN4Uk5acy9UWWFjN3FHNlJyUkxvcFU4bFZleVpTYS9NMDBZNkI4eGNrVEVqb2pEYmZGOU16TzhhWkFsd281VW9PVm9scTZ4a2VSSm9QTzFwV0RCajg2MlRBOGJDL3k0TjRkRzhCemVQeUZyd0NWMndXYVVRNHFYYmdZZGtZb21lM0NNRDMydk5odVFvazRBMjI1WmdCa2tKS2d1ZjExMG5YclR5MzF1dmVEUXVEemhLR0szdVByZ29nc1kzVExTL2R3Q1E3ZmFJOWJsN1ZtYVlwK1QrR1VQeXBOdWNiOGZ4RFdWSWJKR0Rxak5nRUJlcDQ1dlRBaE5CL1NvZVhoZmZnVWdVPSIsIm5iZiI6MTc2NjM0Njc3OCwiZXhwIjoyMDY2MzQ2Nzc4LCJpYXQiOjE3NjYzNDY3Nzh9.KO41y02CYKQeiaNdfZqUgFa4RFKe0F1or0AfHl61QE8; userData={"userID":33,"userFirstName":"Abe","ipAddress":"","userLastName":"Perl","userEmail":"abeperl@gmail.com","userPhone":"6463166925","userMobile":"","userName":"abeperl","userPassword":"MTE5YXAyMTQAAAAAAAAAAAAAAABcy/W5AITs/7YaM/xHa7fd8RBXNvgTzZEunt+oP5G/+g==","isActive":true,"roleName":"Super Admin","storeName":"","roleId":1,"storeId":5,"isHistoryExist":false,"clientId":1,"printReceiptAuto":false,"userStorePermission":[{"description":"Dynamic Integration Screen","active":true,"storePermissionId":1,"recId":3,"pin":null},{"description":"Sales Return Screen","active":true,"storePermissionId":2,"recId":6,"pin":null}],"repId":null,"userAddress":null}; isRefreshedToken=false; chargingCard=; paymentinprocess=',
        'ClientId', '1',
        'StoreId', '5',
        'WarehouseId', '1'
    ),
    json('{"draw":1,"columns":[{"data":0,"name":"","searchable":true,"orderable":true,"search":{"value":"","regex":false}},{"data":[2],"name":"","searchable":true,"orderable":true,"search":{"value":"","regex":false}},{"data":[24],"name":"","searchable":true,"orderable":true,"search":{"value":"","regex":false}},{"data":[25],"name":"","searchable":true,"orderable":true,"search":{"value":"","regex":false}},{"data":[22],"name":"","searchable":true,"orderable":true,"search":{"value":"","regex":false}},{"data":[1],"name":"","searchable":true,"orderable":true,"search":{"value":"","regex":false}},{"data":[21],"name":"","searchable":true,"orderable":true,"search":{"value":"","regex":false}},{"data":[13],"name":"","searchable":true,"orderable":true,"search":{"value":"","regex":false}},{"data":[20],"name":"","searchable":true,"orderable":true,"search":{"value":"","regex":false}},{"data":[27],"name":"","searchable":true,"orderable":true,"search":{"value":"","regex":false}},{"data":[17],"name":"","searchable":true,"orderable":true,"search":{"value":"","regex":false}},{"data":[16],"name":"","searchable":true,"orderable":true,"search":{"value":"","regex":false}},{"data":12,"name":"","searchable":true,"orderable":false,"search":{"value":"","regex":false}}],"order":[{"column":5,"dir":"asc"}],"start":0,"length":50,"search":{"value":"","regex":false},"dateFrom":null,"dateTo":null,"status":1,"OrderStatusId":-1,"orderType":-1,"CustomStatusId":"0","StoreId":"5"}'),
    NULL,
    0,
    NULL,
    json_object(
        'IdJsonPath', '[5]',
        'ChainedArrayJsonPath', 'response.result.data'
    )
);

-- Sub-Action 1 for API 11: Get Order Details
INSERT INTO SubAction (
    PrimaryApiId,
    ActionNumber,
    ActionName,
    ActionType,
    Configuration,
    ExecutionOrder,
    IsEnabled
) VALUES (
    (SELECT Id FROM PrimaryApi WHERE ApiNumber = 11),
    1,
    'Get Order Details',
    'CallApi',
    json_object(
        'Endpoint', '/api/SaleOrder/GetSaleOrderById?saleOrderId={id}',
        'Method', 'GET',
        'OutputVariableName', 'orderDetails'
    ),
    1,
    1
);

-- Sub-Action 2 for API 11: Save Order Details as JSON file
INSERT INTO SubAction (
    PrimaryApiId,
    ActionNumber,
    ActionName,
    ActionType,
    Configuration,
    ExecutionOrder,
    IsEnabled
) VALUES (
    (SELECT Id FROM PrimaryApi WHERE ApiNumber = 11),
    2,
    'Save Order Details to JSON File',
    'SaveJsonToFile',
    json_object(
        'Endpoint', 'sales-orders',
        'RequestBody', 'order-{id}.json',
        'StoreName', 'Sales'
    ),
    2,
    1
);

-- ============================================================================
-- API 12: Office (StoreId = 6)
-- ============================================================================
INSERT INTO PrimaryApi (
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
    Configuration
) VALUES (
    12,
    'Sales Order Export API - Office',
    'https://malchus.3plnext.com',
    '/api/SaleOrder/GetSaleOrderList',
    'POST',
    json_object(
        'Authorization', 'Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJVc2VySW5mbyI6IkRsaFREWDFLak5xZGMwaDFDVHozWVVOdU9FSG9DMk1nWE56TFh5SDkxYVA0T1A0VjVVMUI2SFdmeGN1QWE4aUNLekZYS0luUTFadEM3RVVvWmxaazU2Q0R6emE1UGtRdWZNb2xqOW9GTnhMVk5YTXlWRGxDQ1A1bHJPNjVxLzE4WndYY2VLOHhLaVVNa2xpWDlsZkVpck50Q2k0L2V0Y2JSd1V4SXNWNkxoM2M2TGZoQ1JoWkI0SjJvdlNQdUI5MnhYWFU3dHhpQ3RxNysxUGZKdUdvNmdUUWtrSTdNcXVVRi9rZnJQZnk1b2hMYUFrTXlMakkyVjB0NmQxajg5cFQ1WWk1S1B2bGZkRFpiZStEQnJNbWtud3RHWDZoaU9FMVk5SGhNZzBUZnIwVXlqNExxNWY3WmhWWmo4UlRXTTN0ZC95R3dGZG5ENmh3eCtETXFNOC9sT2Ixb1R5Q1FLK29SSzZ3bDZCWHZTR3BDS2ExSnFNU2wzdnJZTVJTbExHT2xRZ28xNSs0V3Y5SFJ1UVE5SVplUmJOZXdoQithdWI3Y013UHN5YUJnaVlXL2J1eWw2MkdiQy9jSXI4U2IwMnNkbUVCQ0U1SDI0dmQwSVIyYjdwakIxcURwbzNtNXJQZmo0VTNqeTlVWTNKWExSN05zUlg2ZkdPTnFvZGE5WjhjQTFYc1dRUTg1TW9KMm83QitiK0x4SWhqREhpYTlncGNUU1dSTDYxRzRpMzExcDJNN01TbDFJdll3c0ErV2FrNjZZQVRsY0VjOEJCcXlCcWNiaTVuQkdNV2t2U280SUZ3VlNxNHVQVHN4Uk5acy9UWWFjN3FHNlJyUkxvcFU4bFZleVpTYS9NMDBZNkI4eGNrVEVqb2pEYmZGOU16TzhhWkFsd281VW9PVm9scTZ4a2VSSm9QTzFwV0RCajg2MlRBOGJDL3k0TjRkRzhCemVQeUZyd0NWMndXYVVRNHFYYmdZZGtZb21lM0NNRDMydk5odVFvazRBMjI1WmdCa2tKS2d1ZjExMG5YclR5MzF1dmVEUXVEemhLR0szdVByZ29nc1kzVExTL2R3Q1E3ZmFJOWJsN1ZtYVlwK1QrR1VQeXBOdWNiOGZ4RFdWSWJKR0Rxak5nRUJlcDQ1dlRBaE5CL1NvZVhoZmZnVWdVPSIsIm5iZiI6MTc2NjM0Njc3OCwiZXhwIjoyMDY2MzQ2Nzc4LCJpYXQiOjE3NjYzNDY3Nzh9.KO41y02CYKQeiaNdfZqUgFa4RFKe0F1or0AfHl61QE8',
        'Cookie', 'token=eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJVc2VySW5mbyI6IkRsaFREWDFLak5xZGMwaDFDVHozWVVOdU9FSG9DMk1nWE56TFh5SDkxYVA0T1A0VjVVMUI2SFdmeGN1QWE4aUNLekZYS0luUTFadEM3RVVvWmxaazU2Q0R6emE1UGtRdWZNb2xqOW9GTnhMVk5YTXlWRGxDQ1A1bHJPNjVxLzE4WndYY2VLOHhLaVVNa2xpWDlsZkVpck50Q2k4L2V0Y2JSd1V4SXNWNkxoM2M2TGZoQ1JoWkI0SjJvdlNQdUI5MnhYWFU3dHhpQ3RxNzsxUGZKdUdvNmdUUWtrSTdNcXVVRi9rZnJQZnk1b2hMYUFrTXlMakkyVjB0NmQxajg5cFQ1WWk1S1B2bGZkRFpiZStEQnJNbWtud3RHWDZoaU9FMVk5SGhNZzBUZnIwVXlqNExxNWY3WmhWWmo4UlRXTTN0ZC95R3dGZG5ENmh3eCtETXFNOC9sT2Ixb1R5Q1FLK29SSzZ3bDZCWHZTR3BDS2ExSnFNU2wzdnJZTVJTbExHT2xRZ28xNSs0V3Y5SFJ1UVE5SVplUmJOZXdoQithdWI3Y013UHN5YUJnaVlXL2J1eWw2MkdiQy9jSXI4U2IwMnNkbUVCQ0U1SDI0dmQwSVIyYjdwakIxcURwbzNtNXJQZmo0VTNqeTlVWTNKWExSN05zUlg2ZkdPTnFvZGE5WjhjQTFYc1dRUTg1TW9KMm83QitiK0x4SWhqREhpYTlncGNUU1dSTDYxRzRpMzExcDJNN01TbDFJdll3c0ErV2FrNjZZQVRsY0VjOEJCcXlCcWNiaTVuQkdNV2t2U280SUZ3VlNxNHVQVHN4Uk5acy9UWWFjN3FHNlJyUkxvcFU4bFZleVpTYS9NMDBZNkI4eGNrVEVqb2pEYmZGOU16TzhhWkFsd281VW9PVm9scTZ4a2VSSm9QTzFwV0RCajg2MlRBOGJDL3k0TjRkRzhCemVQeUZyd0NWMndXYVVRNHFYYmdZZGtZb21lM0NNRDMydk5odVFvazRBMjI1WmdCa2tKS2d1ZjExMG5YclR5MzF1dmVEUXVEemhLR0szdVByZ29nc1kzVExTL2R3Q1E3ZmFJOWJsN1ZtYVlwK1QrR1VQeXBOdWNiOGZ4RFdWSWJKR0Rxak5nRUJlcDQ1dlRBaE5CL1NvZVhoZmZnVWdVPSIsIm5iZiI6MTc2NjM0Njc3OCwiZXhwIjoyMDY2MzQ2Nzc4LCJpYXQiOjE3NjYzNDY3Nzh9.KO41y02CYKQeiaNdfZqUgFa4RFKe0F1or0AfHl61QE8; userData={"userID":33,"userFirstName":"Abe","ipAddress":"","userLastName":"Perl","userEmail":"abeperl@gmail.com","userPhone":"6463166925","userMobile":"","userName":"abeperl","userPassword":"MTE5YXAyMTQAAAAAAAAAAAAAAABcy/W5AITs/7YaM/xHa7fd8RBXNvgTzZEunt+oP5G/+g==","isActive":true,"roleName":"Super Admin","storeName":"","roleId":1,"storeId":6,"isHistoryExist":false,"clientId":1,"printReceiptAuto":false,"userStorePermission":[{"description":"Dynamic Integration Screen","active":true,"storePermissionId":1,"recId":3,"pin":null},{"description":"Sales Return Screen","active":true,"storePermissionId":2,"recId":6,"pin":null}],"repId":null,"userAddress":null}; isRefreshedToken=false; chargingCard=; paymentinprocess=',
        'ClientId', '1',
        'StoreId', '6',
        'WarehouseId', '1'
    ),
    json('{"draw":1,"columns":[{"data":0,"name":"","searchable":true,"orderable":true,"search":{"value":"","regex":false}},{"data":[2],"name":"","searchable":true,"orderable":true,"search":{"value":"","regex":false}},{"data":[24],"name":"","searchable":true,"orderable":true,"search":{"value":"","regex":false}},{"data":[25],"name":"","searchable":true,"orderable":true,"search":{"value":"","regex":false}},{"data":[22],"name":"","searchable":true,"orderable":true,"search":{"value":"","regex":false}},{"data":[1],"name":"","searchable":true,"orderable":true,"search":{"value":"","regex":false}},{"data":[21],"name":"","searchable":true,"orderable":true,"search":{"value":"","regex":false}},{"data":[13],"name":"","searchable":true,"orderable":true,"search":{"value":"","regex":false}},{"data":[20],"name":"","searchable":true,"orderable":true,"search":{"value":"","regex":false}},{"data":[27],"name":"","searchable":true,"orderable":true,"search":{"value":"","regex":false}},{"data":[17],"name":"","searchable":true,"orderable":true,"search":{"value":"","regex":false}},{"data":[16],"name":"","searchable":true,"orderable":true,"search":{"value":"","regex":false}},{"data":12,"name":"","searchable":true,"orderable":false,"search":{"value":"","regex":false}}],"order":[{"column":5,"dir":"asc"}],"start":0,"length":50,"search":{"value":"","regex":false},"dateFrom":null,"dateTo":null,"status":1,"OrderStatusId":-1,"orderType":-1,"CustomStatusId":"0","StoreId":"6"}'),
    NULL,
    0,
    NULL,
    json_object(
        'IdJsonPath', '[5]',
        'ChainedArrayJsonPath', 'response.result.data'
    )
);

-- Sub-Action 1 for API 12: Get Order Details
INSERT INTO SubAction (
    PrimaryApiId,
    ActionNumber,
    ActionName,
    ActionType,
    Configuration,
    ExecutionOrder,
    IsEnabled
) VALUES (
    (SELECT Id FROM PrimaryApi WHERE ApiNumber = 12),
    1,
    'Get Order Details',
    'CallApi',
    json_object(
        'Endpoint', '/api/SaleOrder/GetSaleOrderById?saleOrderId={id}',
        'Method', 'GET',
        'OutputVariableName', 'orderDetails'
    ),
    1,
    1
);

-- Sub-Action 2 for API 12: Save Order Details as JSON file
INSERT INTO SubAction (
    PrimaryApiId,
    ActionNumber,
    ActionName,
    ActionType,
    Configuration,
    ExecutionOrder,
    IsEnabled
) VALUES (
    (SELECT Id FROM PrimaryApi WHERE ApiNumber = 12),
    2,
    'Save Order Details to JSON File',
    'SaveJsonToFile',
    json_object(
        'Endpoint', 'sales-orders',
        'RequestBody', 'order-{id}.json',
        'StoreName', 'Office'
    ),
    2,
    1
);

-- ============================================================================
-- Verification Query
-- ============================================================================
SELECT
    'Store APIs Summary' as Report,
    p.ApiNumber,
    p.ApiName,
    json_extract(p.Headers, '$.StoreId') as HeaderStoreId,
    json_extract(p.Payload, '$.StoreId') as PayloadStoreId,
    p.IsEnabled
FROM PrimaryApi p
WHERE p.ApiNumber BETWEEN 7 AND 12
ORDER BY p.ApiNumber;

-- Show SubActions with StoreName
SELECT
    'SubActions with StoreName' as Report,
    p.ApiNumber,
    p.ApiName,
    s.ActionNumber,
    s.ActionName,
    s.ActionType,
    json_extract(s.Configuration, '$.StoreName') as StoreName
FROM PrimaryApi p
JOIN SubAction s ON p.Id = s.PrimaryApiId
WHERE p.ApiNumber BETWEEN 7 AND 12
  AND s.ActionType = 'SaveJsonToFile'
ORDER BY p.ApiNumber;

-- ============================================================================
-- Notes:
-- 1. All APIs are disabled by default (IsEnabled = 0). Enable when ready.
-- 2. To enable a specific store API:
--    UPDATE PrimaryApi SET IsEnabled = 1 WHERE ApiNumber = 8;  -- Boro Park
--    UPDATE PrimaryApi SET IsEnabled = 1 WHERE ApiNumber = 9;  -- Monsey
--    UPDATE PrimaryApi SET IsEnabled = 1 WHERE ApiNumber = 10; -- Monroe
--    UPDATE PrimaryApi SET IsEnabled = 1 WHERE ApiNumber = 11; -- Sales
--    UPDATE PrimaryApi SET IsEnabled = 1 WHERE ApiNumber = 12; -- Office
--
-- 3. Output directories with StoreName will be:
--    C:\ProgramData\ScheduledPrintService\out\sales-orders\Boro Park\
--    C:\ProgramData\ScheduledPrintService\out\sales-orders\Monsey\
--    C:\ProgramData\ScheduledPrintService\out\sales-orders\Monroe\
--    C:\ProgramData\ScheduledPrintService\out\sales-orders\Sales\
--    C:\ProgramData\ScheduledPrintService\out\sales-orders\Office\
--
-- 4. The C# code must be updated to handle the StoreName property
--    in the ExecuteSaveJsonToFileAsync method
-- ============================================================================