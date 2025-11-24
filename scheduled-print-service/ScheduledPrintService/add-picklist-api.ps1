# Add Picklist API to Database
# This script adds API #2: Picklist Datatable API

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$dbPath = Join-Path $scriptDir "api_config.db"

Write-Host "Adding Picklist API to database..." -ForegroundColor Cyan

# Check if database exists
if (-not (Test-Path $dbPath)) {
    Write-Host "ERROR: Database not found at: $dbPath" -ForegroundColor Red
    exit 1
}

# Bearer token from the curl
$bearerToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJVc2VySW5mbyI6IkRsaFREWDFLak5xYUZpVC91MklsOXhHbHBwSUlrVTl2S1lRQ1lHLzlxcHg0RjB1Zi83eW1sc25INU05NkZFTmI0b3ZPK2lMS2JRaHFGSVJQSVFFVEV4am5FQkZFU09nUTkxUk9pQlQ4WklDMW5TQ24rL1hrSWt1L1YxNUpIMm9iVDQ3Q0gwWkl6WGNJVUkzb1oraGlweDdXME5VQ3hkemJtazFRK2hmVWpuRzF5dU5keDFYVlREdG1iTnFrcXNjeFUyZlBhTHdvYjMzMmVJMGhWKzY3M052OStpOXovTGNEMUp0OGpYeUtobWtlcEQ2Z1o5ajIrWDJlQ1E1UGZHbmdmTHVkbmNQVHZHMjI2bjZZcWgza2tTWGZXdnZGSHd0dHRxbVpJU3kzdGJhNDFnOW1QS1l0Qmw4Rjk4YXNkb1ZzYzZESTZZUlB4RUIwYkJZYUZ1Nm9RcmxRTWlIU1BZc1FyZmJQZHFsN1lpb3dKVnJ6ZzZwOThQR3pZTlk1VC9FSmh5RnFnK2l0YUtGd2hyT2J5TjV2a04rUUorWFcvbEwzTnh2dGJycytRUzlBWmpHRmJxaFNMUldjZnF2bU9BMitmcFpwNmc0aVRVWnpjTFpwc2lwamFpWGYraGpORTBJQSIsIm5iZiI6MTc2MzU3NzY0MiwiZXhwIjoxNzYzNTk1NjQyLCJpYXQiOjE3NjM1Nzc2NDIsImlzcyI6Imh0dHBzOi8vbWouM3BsbmV4dC5jb20iLCJhdWQiOiJodHRwczovL21qLjNwbG5leHQuY29tIn0.jI8C71UiQ3kPMS2I_FcKBp6F8M6EB6a_E77fhINcCDc"

# Prepare headers JSON
$headersObj = @{
    "Accept" = "*/*"
    "Accept-Language" = "en-US,en;q=0.9"
    "Authorization" = "Bearer $bearerToken"
    "Cache-Control" = "no-cache"
    "Connection" = "keep-alive"
    "Content-Type" = "application/json"
    "Origin" = "https://mj.3plnext.com"
    "Pragma" = "no-cache"
    "Referer" = "https://mj.3plnext.com/"
    "Sec-Fetch-Dest" = "empty"
    "Sec-Fetch-Mode" = "cors"
    "Sec-Fetch-Site" = "same-origin"
    "User-Agent" = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/144.0.0.0 Safari/537.36 Edg/144.0.0.0"
    "WarehouseId" = "1"
    "X-Requested-With" = "XMLHttpRequest"
    "sec-ch-ua" = "`"Not(A:Brand`";v=`"8`", `"Chromium`";v=`"144`", `"Microsoft Edge`";v=`"144`""
    "sec-ch-ua-mobile" = "?0"
    "sec-ch-ua-platform" = "`"Windows`""
    "Cookie" = "token=$bearerToken; userData={`"userID`":108,`"defaultWarehouseId`":1,`"warehouseId`":4,`"firstName`":`"Abe`",`"lastName`":`"Perl`",`"userEmail`":`"abeperl@gmail.com`",`"roleId`":2,`"role`":`"Administrator`",`"clientId`":0,`"clientName`":null,`"salesRepId`":0,`"customerId`":0,`"channelId`":0,`"channelName`":null,`"isSupportUser`":false,`"isGroupRole`":false}; isRefreshedToken=false"
}

$headersJson = $headersObj | ConvertTo-Json -Compress

# Prepare params JSON (the POST body)
$paramsObj = @{
    "draw" = 1
    "columns" = @(
        @{"data"=0;"name"="";"searchable"=$true;"orderable"=$true;"search"=@{"value"="";"regex"=$false}},
        @{"data"=1;"name"="";"searchable"=$true;"orderable"=$false;"search"=@{"value"="";"regex"=$false}},
        @{"data"=@(1);"name"="";"searchable"=$true;"orderable"=$true;"search"=@{"value"="";"regex"=$false}},
        @{"data"=@(11);"name"="";"searchable"=$true;"orderable"=$true;"search"=@{"value"="";"regex"=$false}},
        @{"data"=@(19);"name"="";"searchable"=$true;"orderable"=$true;"search"=@{"value"="";"regex"=$false}},
        @{"data"=@(20);"name"="";"searchable"=$true;"orderable"=$true;"search"=@{"value"="";"regex"=$false}},
        @{"data"=@(17);"name"="";"searchable"=$true;"orderable"=$true;"search"=@{"value"="";"regex"=$false}},
        @{"data"=@(26);"name"="";"searchable"=$true;"orderable"=$true;"search"=@{"value"="";"regex"=$false}},
        @{"data"=@(29);"name"="";"searchable"=$true;"orderable"=$true;"search"=@{"value"="";"regex"=$false}},
        @{"data"=9;"name"="";"searchable"=$true;"orderable"=$true;"search"=@{"value"="";"regex"=$false}},
        @{"data"=10;"name"="";"searchable"=$true;"orderable"=$true;"search"=@{"value"="";"regex"=$false}},
        @{"data"=@(3);"name"="";"searchable"=$true;"orderable"=$true;"search"=@{"value"="";"regex"=$false}},
        @{"data"=@(4);"name"="";"searchable"=$true;"orderable"=$true;"search"=@{"value"="";"regex"=$false}},
        @{"data"=@(5);"name"="";"searchable"=$true;"orderable"=$true;"search"=@{"value"="";"regex"=$false}},
        @{"data"=@(21);"name"="";"searchable"=$true;"orderable"=$true;"search"=@{"value"="";"regex"=$false}},
        @{"data"=15;"name"="";"searchable"=$true;"orderable"=$true;"search"=@{"value"="";"regex"=$false}},
        @{"data"=@(6);"name"="";"searchable"=$true;"orderable"=$true;"search"=@{"value"="";"regex"=$false}},
        @{"data"=@(7);"name"="";"searchable"=$true;"orderable"=$true;"search"=@{"value"="";"regex"=$false}},
        @{"data"=@(8);"name"="";"searchable"=$true;"orderable"=$true;"search"=@{"value"="";"regex"=$false}},
        @{"data"=@(9);"name"="";"searchable"=$true;"orderable"=$true;"search"=@{"value"="";"regex"=$false}},
        @{"data"=@(14);"name"="";"searchable"=$true;"orderable"=$true;"search"=@{"value"="";"regex"=$false}},
        @{"data"=@(13);"name"="";"searchable"=$true;"orderable"=$true;"search"=@{"value"="";"regex"=$false}},
        @{"data"=@(12);"name"="";"searchable"=$true;"orderable"=$true;"search"=@{"value"="";"regex"=$false}},
        @{"data"=@(10);"name"="";"searchable"=$true;"orderable"=$true;"search"=@{"value"="";"regex"=$false}},
        @{"data"=24;"name"="";"searchable"=$true;"orderable"=$false;"search"=@{"value"="";"regex"=$false}}
    )
    "order" = @(@{"column"=13;"dir"="desc"})
    "start" = 0
    "length" = 25
    "search" = @{"value"="";"regex"=$false}
    "param1" = "-1"
    "param2" = "all"
    "statusName" = "0,2"
    "clientid" = "1"
    "orderType" = ""
    "dateFrom" = $null
    "dateTo" = $null
}

$paramsJson = $paramsObj | ConvertTo-Json -Compress -Depth 10

# Escape single quotes for SQL
$headersJsonEscaped = $headersJson -replace "'", "''"
$paramsJsonEscaped = $paramsJson -replace "'", "''"

# Insert Primary API
Write-Host "`nInserting Primary API #2 (Picklist Datatable)..." -ForegroundColor Cyan

$insertPrimaryApiSql = @"
INSERT INTO PrimaryApi (ApiNumber, ApiName, BaseUrl, Endpoint, HttpMethod, Headers, Params, Payload, IsEnabled)
VALUES (
    2,
    'Picklist Datatable API',
    'https://mj.3plnext.com',
    '/api/Picklist/GetPicklistDatatable',
    'POST',
    '$headersJsonEscaped',
    '$paramsJsonEscaped',
    '{}',
    1
);
"@

$insertPrimaryApiSql | sqlite3 $dbPath

if ($LASTEXITCODE -ne 0) {
    Write-Host "✗ Error inserting Primary API" -ForegroundColor Red
    exit 1
}

Write-Host "✓ Primary API #2 inserted successfully" -ForegroundColor Green

# Get the inserted Primary API ID
$primaryApiId = sqlite3 $dbPath "SELECT Id FROM PrimaryApi WHERE ApiNumber = 2;"

if (-not $primaryApiId) {
    Write-Host "✗ Error retrieving Primary API ID" -ForegroundColor Red
    exit 1
}

Write-Host "  Primary API ID: $primaryApiId" -ForegroundColor White

# Sub-Action #1: Get URL and Print Manual Picking Page
Write-Host "`nInserting Sub-Action #1 (Get URL and Print)..." -ForegroundColor Cyan

$subAction1Config = @{
    Endpoint = "https://mj.3plnext.com/#Outbound/ManualPicking?id={id}"
    Method = "GET"
    UseChainedInput = $true
    ChainedArrayJsonPath = "data"
    ChainedItemFieldPath = "[0]"
    WaitForNetworkIdleMs = 3000
    MakeHiddenVisible = $true
    ContinueOnError = $true
} | ConvertTo-Json -Compress -Depth 10

$subAction1ConfigEscaped = $subAction1Config -replace "'", "''"

$insertSubAction1Sql = @"
INSERT INTO SubAction (PrimaryApiId, ActionNumber, ActionName, ActionType, Configuration, ExecutionOrder, IsEnabled)
VALUES (
    $primaryApiId,
    1,
    'Get Manual Picking Page URL',
    'GetUrlAndPrint',
    '$subAction1ConfigEscaped',
    1,
    1
);
"@

$insertSubAction1Sql | sqlite3 $dbPath

if ($LASTEXITCODE -eq 0) {
    Write-Host "✓ Sub-Action #1 inserted" -ForegroundColor Green
} else {
    Write-Host "✗ Error inserting Sub-Action #1" -ForegroundColor Red
}

# Sub-Action #2: Convert HTML to PDF
Write-Host "`nInserting Sub-Action #2 (Convert HTML to PDF)..." -ForegroundColor Cyan

$subAction2Config = @{
    Endpoint = "/api/Picklist/GetPicklistHtml/{id}"
    Method = "GET"
    HtmlJsonPath = "html"
    UseChainedInput = $true
    ChainedItemFieldPath = "[0]"
    OutputFilePrefix = "picklist"
    ContinueOnError = $true
} | ConvertTo-Json -Compress -Depth 10

$subAction2ConfigEscaped = $subAction2Config -replace "'", "''"

$insertSubAction2Sql = @"
INSERT INTO SubAction (PrimaryApiId, ActionNumber, ActionName, ActionType, Configuration, ExecutionOrder, IsEnabled)
VALUES (
    $primaryApiId,
    2,
    'Convert HTML to PDF and Save',
    'GetHtmlAndPrint',
    '$subAction2ConfigEscaped',
    2,
    1
);
"@

$insertSubAction2Sql | sqlite3 $dbPath

if ($LASTEXITCODE -eq 0) {
    Write-Host "✓ Sub-Action #2 inserted" -ForegroundColor Green
} else {
    Write-Host "✗ Error inserting Sub-Action #2" -ForegroundColor Red
}

# Display summary
Write-Host "`n================================" -ForegroundColor Cyan
Write-Host "API #2 Added Successfully!" -ForegroundColor Green
Write-Host "================================" -ForegroundColor Cyan

Write-Host "`nAPI Details:" -ForegroundColor Yellow
$apiDetails = sqlite3 $dbPath "SELECT 'API #' || ApiNumber || ': ' || ApiName || ' [' || HttpMethod || ' ' || BaseUrl || Endpoint || ']' FROM PrimaryApi WHERE ApiNumber = 2;"
Write-Host "  $apiDetails" -ForegroundColor White

Write-Host "`nSub-Actions:" -ForegroundColor Yellow
$subActions = sqlite3 $dbPath "SELECT '  [' || ActionNumber || '] ' || ActionName || ' (' || ActionType || ')' FROM SubAction WHERE PrimaryApiId = $primaryApiId ORDER BY ExecutionOrder;"
$subActions -split "`n" | ForEach-Object { Write-Host "$_" -ForegroundColor White }

Write-Host "`nYou can now run:" -ForegroundColor Yellow
Write-Host "  powershell -ExecutionPolicy Bypass -File run-api.ps1 -ApiNumber 2 -DryRun" -ForegroundColor White
Write-Host "  powershell -ExecutionPolicy Bypass -File run-api.ps1 -ApiNumber 2" -ForegroundColor White
