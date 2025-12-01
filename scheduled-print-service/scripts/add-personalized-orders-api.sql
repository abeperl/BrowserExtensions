-- =====================================================
-- Migration: Add Personalized Orders API Configuration
-- Date: 2025-11-27
-- Description: Adds API configuration for fetching personalized order items
--              and printing custom forms (HTML to PDF)
-- =====================================================

-- Delete existing data for idempotency
DELETE FROM ScheduleApi WHERE ApiNumber = 3;
DELETE FROM Schedule WHERE ScheduleName = 'Personalized Orders Print Schedule';
DELETE FROM SubAction WHERE PrimaryApiId IN (SELECT Id FROM PrimaryApi WHERE ApiNumber = 3);
DELETE FROM PrimaryApi WHERE ApiNumber = 3;

-- 1. Insert Primary API configuration for GetPersonalizedOrderItems
INSERT INTO PrimaryApi (
    ApiNumber,
    ApiName,
    BaseUrl,
    Endpoint,
    HttpMethod,
    Headers,
    Params,
    Payload,
    IsEnabled
) VALUES (
    3,  -- ApiNumber 3 (next available)
    'Personalized Orders API',
    'https://mj.3plnext.com',
    '/api/Order/GetPersonalizedOrderItems',
    'POST',
    -- Headers JSON (token will be updated by token renewal service)
    '{"Authorization":"Bearer YOUR_TOKEN_HERE","Content-Type":"application/json","Accept":"*/*","Accept-Language":"en-US,en;q=0.9","Cache-Control":"no-cache","Connection":"keep-alive","Origin":"https://mj.3plnext.com","Pragma":"no-cache","Referer":"https://mj.3plnext.com/","Sec-Fetch-Dest":"empty","Sec-Fetch-Mode":"cors","Sec-Fetch-Site":"same-origin","User-Agent":"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/144.0.0.0 Safari/537.36 Edg/144.0.0.0","X-Requested-With":"XMLHttpRequest","sec-ch-ua":"\"Not(A:Brand\";v=\"8\", \"Chromium\";v=\"144\", \"Microsoft Edge\";v=\"144\"","sec-ch-ua-mobile":"?0","sec-ch-ua-platform":"\"Windows\"","WarehouseId":"1","Cookie":"token=YOUR_TOKEN_HERE; userData=YOUR_USER_DATA_HERE; isRefreshedToken=false"}',
    -- Params (contains the request payload structure)
    '{"SKu":"","ClientId":"1","searchvalue":""}',
    -- Payload (actual payload to send)
    '{"SKu":"","ClientId":"1","searchvalue":""}',
    1  -- IsEnabled
);

-- 2. Insert Sub-Action: Print Custom Forms from itemNotes field
--    This action iterates through the response data array, filters for non-empty itemNotes
--    containing file paths, and prints each custom form HTML file
INSERT INTO SubAction (
    PrimaryApiId,
    ActionNumber,
    ActionName,
    ActionType,
    Configuration,
    ExecutionOrder,
    IsEnabled
) VALUES (
    (SELECT Id FROM PrimaryApi WHERE ApiNumber = 3),  -- Links to PrimaryApi.Id where ApiNumber=3
    1,  -- First sub-action
    'Print Custom Forms',
    'GetUrlAndPrint',  -- Uses Puppeteer to fetch URL and print to PDF
    -- Configuration JSON with filter to only process file paths
    '{"ChainedArrayJsonPath":"data","UseChainedInput":true,"ChainedItemFieldPath":"itemNotes","ChainedFilterField":"itemNotes","ChainedFilterType":"IsFilePath","ChainedFilterValue":null,"Endpoint":"https://mj.3plnext.com/{itemNotes}","Method":"GET","WaitForNetworkIdleMs":3000,"MakeHiddenVisible":false,"ContinueOnError":true,"Headers":null,"RequestBody":null,"DelayMilliseconds":null,"OutputVariableName":null,"ChainedInputMapping":{},"HtmlJsonPath":null,"BatchSize":null,"QuickShip":null,"ForceCreatePicklist":null}',
    1,  -- ExecutionOrder
    1   -- IsEnabled
);

-- 3. Create corresponding Schedule entry (optional - can be enabled/disabled as needed)
INSERT INTO Schedule (
    ScheduleName,
    CronExpression,
    IsEnabled,
    CreatedAt,
    UpdatedAt
) VALUES (
    'Personalized Orders Print Schedule',
    '0 * * * *',  -- Run every hour (cron format)
    0,            -- Disabled by default - enable manually when ready
    CURRENT_TIMESTAMP,
    CURRENT_TIMESTAMP
);

-- 4. Link the Schedule to the API
INSERT INTO ScheduleApi (
    ScheduleId,
    ApiNumber,
    ExecutionOrder
) VALUES (
    (SELECT Id FROM Schedule WHERE ScheduleName = 'Personalized Orders Print Schedule'),
    3,  -- ApiNumber for Personalized Orders API
    1   -- ExecutionOrder
);

-- =====================================================
-- NOTES:
-- =====================================================
-- 1. Update the Bearer token in the Headers JSON after token renewal
-- 2. The itemNotes field is expected to contain file paths like:
--    "Store\\CustomForms\\SW4425\\SW4425_63725.html"
-- 3. The endpoint will be constructed as:
--    https://mj.3plnext.com/Store/CustomForms/SW4425/SW4425_63725.html
-- 4. The service will automatically filter for non-empty itemNotes
-- 5. Default schedule is '0 * * * *' (every hour). Adjust CronExpression as needed:
--    - Every 30 min: '*/30 * * * *'
--    - Every 15 min: '*/15 * * * *'
--    - Every 5 min:  '*/5 * * * *'
-- 6. Enable the schedule by setting IsEnabled=1 when ready to activate
--
-- To enable the schedule:
-- UPDATE Schedule SET IsEnabled = 1, UpdatedAt = CURRENT_TIMESTAMP
-- WHERE ScheduleName = 'Personalized Orders Print Schedule';
--
-- To check current configuration:
-- SELECT * FROM PrimaryApi WHERE ApiNumber = 3;
-- SELECT s.* FROM SubAction s JOIN PrimaryApi p ON s.PrimaryApiId = p.Id WHERE p.ApiNumber = 3;
-- SELECT * FROM Schedule WHERE ScheduleName = 'Personalized Orders Print Schedule';
-- =====================================================
