# Add Picklist API to Database
# This script adds API #2: Picklist Datatable API

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$dbPath = Join-Path $scriptDir "api_config.db"

Write-Host "Adding Picklist API to database..." -ForegroundColor Cyan

# Check if sqlite3 is available (check local directory first)
$sqlite3Path = if (Test-Path ".\sqlite3.exe") { ".\sqlite3.exe" } 
               elseif (Get-Command sqlite3 -ErrorAction SilentlyContinue) { "sqlite3" }
               else { $null }

if (-not $sqlite3Path) {
    Write-Host "ERROR: sqlite3 not found" -ForegroundColor Red
    Write-Host "Install with: winget install SQLite.SQLite" -ForegroundColor Yellow
    exit 1
}

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

# Check if API #2 already exists
$existing = & $sqlite3Path $dbPath "SELECT ApiNumber FROM PrimaryApi WHERE ApiNumber = 2;"

if ($existing) {
    Write-Host "`n[!] API #2 already exists - Updating..." -ForegroundColor Yellow
    
    # Update existing API
    $updatePrimaryApiSql = @"
UPDATE PrimaryApi 
SET ApiName = 'Picklist Datatable API',
    IsEnabled = 1,
    BaseUrl = 'https://mj.3plnext.com',
    BearerToken = '$bearerToken',
    PrimaryEndpoint = '/api/Picklist/GetPicklistDatatable',
    PrimaryHttpMethod = 'POST',
    IdJsonPath = 'data[*][0]'
WHERE ApiNumber = 2;
"@
    
    $updatePrimaryApiSql | & $sqlite3Path $dbPath
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "[X] Error updating Primary API" -ForegroundColor Red
        exit 1
    }
    
    Write-Host "[OK] Primary API #2 updated successfully" -ForegroundColor Green
    
    # Delete old sub-actions
    & $sqlite3Path $dbPath "DELETE FROM SubAction WHERE ApiNumber = 2;" | Out-Null
    Write-Host "[OK] Cleared old sub-actions" -ForegroundColor Green
    
} else {
    Write-Host "`nInserting Primary API #2 (Picklist Datatable)..." -ForegroundColor Cyan
    
    $insertPrimaryApiSql = @"
INSERT INTO PrimaryApi (ApiNumber, ApiName, IsEnabled, BaseUrl, BearerToken, PrimaryEndpoint, PrimaryHttpMethod, IdJsonPath)
VALUES (
    2,
    'Picklist Datatable API',
    1,
    'https://mj.3plnext.com',
    '$bearerToken',
    '/api/Picklist/GetPicklistDatatable',
    'POST',
    'data[*][0]'
);
"@
    
    $insertPrimaryApiSql | & $sqlite3Path $dbPath
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "[X] Error inserting Primary API" -ForegroundColor Red
        exit 1
    }
    
    Write-Host "[OK] Primary API #2 inserted successfully" -ForegroundColor Green
}

# Id column no longer exists in PrimaryApi schema; ApiNumber is the PK.
# Log confirmation instead of querying nonexistent Id.
Write-Host "  API #2 record ready (ApiNumber=2)" -ForegroundColor White

# Sub-Action #1: Get URL and Print Manual Picking Page
Write-Host "`nInserting Sub-Action #1 (Get URL and Print)..." -ForegroundColor Cyan

$insertSubAction1Sql = @"
INSERT INTO SubAction (
    ApiNumber, ExecutionOrder, SubActionType, SubActionName, IsEnabled,
    Endpoint, HttpMethod, UseChainedInput, ChainedArrayJsonPath, ChainedItemFieldPath,
    WaitForNetworkIdleMs, MakeHiddenVisible, ContinueOnError
)
VALUES (
    2, 1, 'GetUrlAndPrint', 'Get Manual Picking Page URL', 1,
    'https://mj.3plnext.com/#Outbound/ManualPicking?id={id}', 'GET', 1, 'data', '[0]',
    3000, 1, 1
);
"@

$insertSubAction1Sql | & $sqlite3Path $dbPath

if ($LASTEXITCODE -eq 0) {
    Write-Host "[OK] Sub-Action #1 inserted" -ForegroundColor Green
} else {
    Write-Host "[X] Error inserting Sub-Action #1" -ForegroundColor Red
}

# Display summary
Write-Host "`n================================" -ForegroundColor Cyan
Write-Host "API #2 Added Successfully!" -ForegroundColor Green
Write-Host "================================" -ForegroundColor Cyan

Write-Host "`nAPI Details:" -ForegroundColor Yellow
$apiDetails = & $sqlite3Path $dbPath "SELECT 'API #' || ApiNumber || ': ' || ApiName || ' (' || PrimaryHttpMethod || ' ' || BaseUrl || PrimaryEndpoint || ')' FROM PrimaryApi WHERE ApiNumber = 2;"
Write-Host "  $apiDetails" -ForegroundColor White

Write-Host "`nSub-Actions:" -ForegroundColor Yellow
$subActions = & $sqlite3Path $dbPath "SELECT '  ' || ExecutionOrder || '. ' || SubActionName || ' (' || SubActionType || ')' FROM SubAction WHERE ApiNumber = 2 ORDER BY ExecutionOrder;"
$subActions -split "`n" | ForEach-Object { Write-Host "$_" -ForegroundColor White }

Write-Host "`nYou can now run:" -ForegroundColor Yellow
Write-Host "  powershell -ExecutionPolicy Bypass -File run-api.ps1 -ApiNumber 2 -DryRun" -ForegroundColor White
Write-Host "  powershell -ExecutionPolicy Bypass -File run-api.ps1 -ApiNumber 2" -ForegroundColor White
