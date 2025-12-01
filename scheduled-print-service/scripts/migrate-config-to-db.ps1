# Migrate API Configuration from appsettings.json to SQLite Database
# This script reads the Api section from appsettings.json and populates the database

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$dbPath = Join-Path $scriptDir "api_config.db"
$appsettingsPath = Join-Path $scriptDir "appsettings.json"

Write-Host "Migrating API configuration to database..." -ForegroundColor Cyan

# Check if database exists
if (-not (Test-Path $dbPath)) {
    Write-Host "ERROR: Database not found at: $dbPath" -ForegroundColor Red
    Write-Host "Please run create-database.ps1 first" -ForegroundColor Yellow
    exit 1
}

# Read appsettings.json
if (-not (Test-Path $appsettingsPath)) {
    Write-Host "ERROR: appsettings.json not found at: $appsettingsPath" -ForegroundColor Red
    exit 1
}

$appsettings = Get-Content $appsettingsPath -Raw | ConvertFrom-Json
$apiConfig = $appsettings.Api

if (-not $apiConfig) {
    Write-Host "ERROR: Api configuration not found in appsettings.json" -ForegroundColor Red
    exit 1
}

Write-Host "Found API configuration:" -ForegroundColor Green
Write-Host "  Base URL: $($apiConfig.BaseUrl)" -ForegroundColor White
Write-Host "  Sub-Actions: $($apiConfig.SubActions.Count)" -ForegroundColor White

# Prepare headers JSON
$headersObj = @{
    "Authorization" = "Bearer $($apiConfig.BearerToken)"
    "Content-Type" = "application/json"
}

# Prepare cookies as headers
$cookieHeader = ($apiConfig.Cookies.PSObject.Properties | ForEach-Object { "$($_.Name)=$($_.Value)" }) -join "; "
if ($cookieHeader) {
    $headersObj["Cookie"] = $cookieHeader
}

$headersJson = $headersObj | ConvertTo-Json -Compress

# Prepare params JSON (from DefaultRequest)
$paramsJson = $apiConfig.DefaultRequest | ConvertTo-Json -Compress

# Prepare payload JSON (empty for GET requests)
$payloadJson = "{}"

# Escape single quotes for SQL
$headersJsonEscaped = $headersJson -replace "'", "''"
$paramsJsonEscaped = $paramsJson -replace "'", "''"
$payloadJsonEscaped = $payloadJson -replace "'", "''"

# Insert Primary API
$insertPrimaryApiSql = @"
INSERT INTO PrimaryApi (ApiNumber, ApiName, BaseUrl, Endpoint, HttpMethod, Headers, Params, Payload, IsEnabled)
VALUES (
    1,
    'Orders List API',
    '$($apiConfig.BaseUrl)',
    '/api/order/GetOrdersList',
    'POST',
    '$headersJsonEscaped',
    '$paramsJsonEscaped',
    '$payloadJsonEscaped',
    1
);
"@

Write-Host "`nInserting Primary API (ApiNumber=1)..." -ForegroundColor Cyan
$insertPrimaryApiSql | sqlite3 $dbPath

if ($LASTEXITCODE -ne 0) {
    Write-Host "✗ Error inserting Primary API" -ForegroundColor Red
    exit 1
}

Write-Host "✓ Primary API inserted successfully" -ForegroundColor Green

# Get the inserted Primary API ID
$primaryApiId = sqlite3 $dbPath "SELECT Id FROM PrimaryApi WHERE ApiNumber = 1;"

if (-not $primaryApiId) {
    Write-Host "✗ Error retrieving Primary API ID" -ForegroundColor Red
    exit 1
}

Write-Host "  Primary API ID: $primaryApiId" -ForegroundColor White

# Insert Sub-Actions
Write-Host "`nInserting Sub-Actions..." -ForegroundColor Cyan
$actionNumber = 1

foreach ($subAction in $apiConfig.SubActions) {
    # Prepare configuration JSON
    $configObj = @{
        Endpoint = $subAction.Endpoint
        Method = $subAction.Method
        RequestBody = $subAction.RequestBody
        Headers = $subAction.Headers
        HtmlJsonPath = $subAction.HtmlJsonPath
        DelayMilliseconds = $subAction.DelayMilliseconds
        ContinueOnError = $subAction.ContinueOnError
        BatchSize = $subAction.BatchSize
        QuickShip = $subAction.QuickShip
        ForceCreatePicklist = $subAction.ForceCreatePicklist
        OutputVariableName = $subAction.OutputVariableName
        UseChainedInput = $subAction.UseChainedInput
        ChainedInputMapping = $subAction.ChainedInputMapping
        WaitForNetworkIdleMs = $subAction.WaitForNetworkIdleMs
        MakeHiddenVisible = $subAction.MakeHiddenVisible
        ChainedArrayJsonPath = $subAction.ChainedArrayJsonPath
        ChainedItemFieldPath = $subAction.ChainedItemFieldPath
    }
    
    $configJson = $configObj | ConvertTo-Json -Compress -Depth 10
    $configJsonEscaped = $configJson -replace "'", "''"
    
    $isEnabled = if ($subAction.Enabled) { 1 } else { 0 }
    
    $insertSubActionSql = @"
INSERT INTO SubAction (PrimaryApiId, ActionNumber, ActionName, ActionType, Configuration, ExecutionOrder, IsEnabled)
VALUES (
    $primaryApiId,
    $actionNumber,
    '$($subAction.Name)',
    '$($subAction.Type)',
    '$configJsonEscaped',
    $actionNumber,
    $isEnabled
);
"@
    
    Write-Host "  [$actionNumber] $($subAction.Name) ($($subAction.Type)) - Enabled: $($subAction.Enabled)" -ForegroundColor White
    $insertSubActionSql | sqlite3 $dbPath
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "  ✗ Error inserting sub-action: $($subAction.Name)" -ForegroundColor Red
    } else {
        Write-Host "  ✓ Inserted" -ForegroundColor Green
    }
    
    $actionNumber++
}

# Create a default schedule
Write-Host "`nCreating default schedule..." -ForegroundColor Cyan

$insertScheduleSql = @"
INSERT INTO Schedule (ScheduleName, CronExpression, IsEnabled)
VALUES (
    'Default Order Processing Schedule',
    '0 */15 * * * *',
    0
);
"@

$insertScheduleSql | sqlite3 $dbPath

if ($LASTEXITCODE -ne 0) {
    Write-Host "✗ Error creating schedule" -ForegroundColor Red
} else {
    Write-Host "✓ Schedule created (Every 15 minutes, currently disabled)" -ForegroundColor Green
    
    # Link schedule to API
    $scheduleId = sqlite3 $dbPath "SELECT Id FROM Schedule WHERE ScheduleName = 'Default Order Processing Schedule';"
    
    $insertScheduleApiSql = @"
INSERT INTO ScheduleApi (ScheduleId, ApiNumber, ExecutionOrder)
VALUES ($scheduleId, 1, 1);
"@
    
    $insertScheduleApiSql | sqlite3 $dbPath
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✓ Linked API #1 to schedule" -ForegroundColor Green
    }
}

# Display summary
Write-Host "`n================================" -ForegroundColor Cyan
Write-Host "Migration Complete!" -ForegroundColor Green
Write-Host "================================" -ForegroundColor Cyan

$summary = sqlite3 $dbPath @"
SELECT 
    'Primary APIs: ' || COUNT(*) as info 
FROM PrimaryApi
UNION ALL
SELECT 
    'Sub-Actions: ' || COUNT(*) 
FROM SubAction
UNION ALL
SELECT 
    'Schedules: ' || COUNT(*) 
FROM Schedule;
"@

$summary -split "`n" | ForEach-Object { Write-Host "  $_" -ForegroundColor White }

Write-Host "`nYou can now view the configuration using:" -ForegroundColor Yellow
Write-Host "  sqlite3 $dbPath" -ForegroundColor White
Write-Host "`nExample queries:" -ForegroundColor Yellow
Write-Host "  SELECT * FROM PrimaryApi;" -ForegroundColor White
Write-Host "  SELECT * FROM SubAction;" -ForegroundColor White
Write-Host "  SELECT * FROM Schedule;" -ForegroundColor White
