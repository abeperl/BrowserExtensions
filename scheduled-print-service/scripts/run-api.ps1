# Run API by Number
# This script executes a Primary API from the database by its ApiNumber
# Usage: .\run-api.ps1 -ApiNumber 1 [-DryRun] [-Verbose]

param(
    [Parameter(Mandatory=$true)]
    [int]$ApiNumber,
    
    [switch]$DryRun,
    
    [switch]$VerboseOutput
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$dbPath = Join-Path $scriptDir "api_config.db"

# Check if database exists
if (-not (Test-Path $dbPath)) {
    Write-Host "ERROR: Database not found at: $dbPath" -ForegroundColor Red
    Write-Host "Please run create-database.ps1 and migrate-config-to-db.ps1 first" -ForegroundColor Yellow
    exit 1
}

# Check if sqlite3 is available
$sqliteCmd = Get-Command sqlite3 -ErrorAction SilentlyContinue
if (-not $sqliteCmd) {
    Write-Host "ERROR: sqlite3 is not installed or not in PATH" -ForegroundColor Red
    Write-Host "Please install SQLite3 from: https://www.sqlite.org/download.html" -ForegroundColor Yellow
    exit 1
}

Write-Host "================================" -ForegroundColor Cyan
Write-Host "API Executor - API Number $ApiNumber" -ForegroundColor Cyan
Write-Host "================================" -ForegroundColor Cyan

# Query the database for API configuration
$apiJson = sqlite3 $dbPath "SELECT * FROM PrimaryApi WHERE ApiNumber = $ApiNumber AND IsEnabled = 1;" -json

if (-not $apiJson) {
    Write-Host "ERROR: API Number $ApiNumber not found or disabled" -ForegroundColor Red
    
    # Show available APIs
    Write-Host "`nAvailable APIs:" -ForegroundColor Yellow
    $availableApis = sqlite3 $dbPath "SELECT ApiNumber, ApiName, IsEnabled FROM PrimaryApi;" -json
    if ($availableApis) {
        $availableApis | ConvertFrom-Json | ForEach-Object {
            $status = if ($_.IsEnabled -eq 1) { "[ENABLED]" } else { "[DISABLED]" }
            Write-Host "  API #$($_.ApiNumber): $($_.ApiName) $status" -ForegroundColor White
        }
    }
    exit 1
}

$api = $apiJson | ConvertFrom-Json

Write-Host "`nAPI Configuration:" -ForegroundColor Green
Write-Host "  Name: $($api.ApiName)" -ForegroundColor White
Write-Host "  Endpoint: $($api.HttpMethod) $($api.BaseUrl)$($api.Endpoint)" -ForegroundColor White
Write-Host "  Enabled: $($api.IsEnabled -eq 1)" -ForegroundColor White

# Parse headers and params from JSON
$headers = $api.Headers | ConvertFrom-Json
$params = $api.Params | ConvertFrom-Json

if ($VerboseOutput) {
    Write-Host "`nHeaders:" -ForegroundColor Yellow
    $headers.PSObject.Properties | ForEach-Object {
        $value = $_.Value
        # Mask sensitive data
        if ($_.Name -eq "Authorization") {
            $value = $value.Substring(0, [Math]::Min(20, $value.Length)) + "..."
        }
        if ($_.Name -eq "Cookie") {
            $value = $value.Substring(0, [Math]::Min(50, $value.Length)) + "..."
        }
        Write-Host "    $($_.Name): $value" -ForegroundColor Gray
    }
    
    Write-Host "`nRequest Parameters:" -ForegroundColor Yellow
    $params.PSObject.Properties | ForEach-Object {
        Write-Host "    $($_.Name): $($_.Value)" -ForegroundColor Gray
    }
}

# Get sub-actions
$subActionsJson = sqlite3 $dbPath "SELECT * FROM SubAction WHERE PrimaryApiId = $($api.Id) ORDER BY ExecutionOrder;" -json

if ($subActionsJson) {
    # Handle single object vs array
    $allSubActions = $subActionsJson | ConvertFrom-Json
    if ($allSubActions -isnot [array]) {
        $allSubActions = @($allSubActions)
    }
} else {
    $allSubActions = @()
}

$enabledSubActions = @($allSubActions | Where-Object { $_.IsEnabled -eq 1 })

Write-Host "`nSub-Actions: $($enabledSubActions.Count) enabled / $($allSubActions.Count) total" -ForegroundColor Cyan
$enabledSubActions | ForEach-Object {
    Write-Host "  [$($_.ActionNumber)] $($_.ActionName) ($($_.ActionType))" -ForegroundColor White
}

if ($DryRun) {
    Write-Host "`n[DRY RUN MODE] - Not executing API call" -ForegroundColor Yellow
    Write-Host "`nWould execute:" -ForegroundColor Yellow
    Write-Host "  1. API Call: $($api.HttpMethod) $($api.BaseUrl)$($api.Endpoint)" -ForegroundColor White
    Write-Host "  2. Process $($enabledSubActions.Count) sub-actions" -ForegroundColor White
    exit 0
}

# Execute the API call
Write-Host "`n================================" -ForegroundColor Cyan
Write-Host "Executing API Call..." -ForegroundColor Cyan
Write-Host "================================" -ForegroundColor Cyan

try {
    $startTime = Get-Date
    
    # Convert headers hashtable for Invoke-RestMethod
    $headersHashtable = @{}
    $headers.PSObject.Properties | ForEach-Object {
        $headersHashtable[$_.Name] = $_.Value
    }
    
    # Prepare request body
    $body = $params | ConvertTo-Json -Depth 10 -Compress
    
    Write-Host "Sending request..." -ForegroundColor White
    
    $response = Invoke-RestMethod `
        -Uri "$($api.BaseUrl)$($api.Endpoint)" `
        -Method $api.HttpMethod `
        -Headers $headersHashtable `
        -Body $body `
        -ContentType "application/json" `
        -TimeoutSec 60
    
    $endTime = Get-Date
    $elapsed = ($endTime - $startTime).TotalSeconds
    
    Write-Host "✓ API call successful ($([Math]::Round($elapsed, 2))s)" -ForegroundColor Green
    
    # Display response summary
    if ($response -is [array]) {
        Write-Host "  Response: Array with $($response.Count) items" -ForegroundColor White
    } elseif ($response -is [PSCustomObject]) {
        $propCount = ($response.PSObject.Properties | Measure-Object).Count
        Write-Host "  Response: Object with $propCount properties" -ForegroundColor White
        
        # Show some common response fields
        if ($response.data) {
            Write-Host "    - data: $($response.data.Count) items" -ForegroundColor Gray
        }
        if ($response.recordsTotal) {
            Write-Host "    - recordsTotal: $($response.recordsTotal)" -ForegroundColor Gray
        }
        if ($response.recordsFiltered) {
            Write-Host "    - recordsFiltered: $($response.recordsFiltered)" -ForegroundColor Gray
        }
    } else {
        Write-Host "  Response: $($response.GetType().Name)" -ForegroundColor White
    }
    
    if ($VerboseOutput) {
        Write-Host "`nFull Response:" -ForegroundColor Yellow
        $response | ConvertTo-Json -Depth 3 | Write-Host -ForegroundColor Gray
    }
    
    # Process sub-actions
    if ($enabledSubActions.Count -gt 0) {
        Write-Host "`n================================" -ForegroundColor Cyan
        Write-Host "Executing Sub-Actions..." -ForegroundColor Cyan
        Write-Host "================================" -ForegroundColor Cyan
        
        $context = @{
            Response = $response
            PrimaryApiResponse = $response
        }
        
        foreach ($subAction in $enabledSubActions) {
            Write-Host "`n[$($subAction.ActionNumber)] $($subAction.ActionName)" -ForegroundColor Cyan
            Write-Host "  Type: $($subAction.ActionType)" -ForegroundColor White
            
            $config = $subAction.Configuration | ConvertFrom-Json
            
            try {
                switch ($subAction.ActionType) {
                    "CreatePicklistBatch" {
                        Write-Host "  Batch Size: $($config.BatchSize)" -ForegroundColor Gray
                        Write-Host "  Endpoint: $($config.Endpoint)" -ForegroundColor Gray
                        Write-Host "  ⚠ Sub-action execution not yet implemented" -ForegroundColor Yellow
                        # TODO: Implement CreatePicklistBatch logic
                    }
                    
                    "GetUrlAndPrint" {
                        Write-Host "  URL: $($config.Endpoint)" -ForegroundColor Gray
                        Write-Host "  ⚠ Sub-action execution not yet implemented" -ForegroundColor Yellow
                        # TODO: Implement GetUrlAndPrint logic
                    }
                    
                    "CallApi" {
                        Write-Host "  Endpoint: $($config.Endpoint)" -ForegroundColor Gray
                        Write-Host "  Method: $($config.Method)" -ForegroundColor Gray
                        Write-Host "  ⚠ Sub-action execution not yet implemented" -ForegroundColor Yellow
                        # TODO: Implement CallApi logic
                    }
                    
                    "Delay" {
                        Write-Host "  Delay: $($config.DelayMilliseconds)ms" -ForegroundColor Gray
                        Start-Sleep -Milliseconds $config.DelayMilliseconds
                        Write-Host "  ✓ Completed" -ForegroundColor Green
                    }
                    
                    "GetHtmlAndPrint" {
                        Write-Host "  Endpoint: $($config.Endpoint)" -ForegroundColor Gray
                        Write-Host "  ⚠ Sub-action execution not yet implemented" -ForegroundColor Yellow
                        # TODO: Implement GetHtmlAndPrint logic
                    }
                    
                    default {
                        Write-Host "  ⚠ Unknown action type: $($subAction.ActionType)" -ForegroundColor Yellow
                    }
                }
            } catch {
                Write-Host "  ✗ Error: $($_.Exception.Message)" -ForegroundColor Red
                if (-not $config.ContinueOnError) {
                    throw
                }
            }
        }
    }
    
    Write-Host "`n================================" -ForegroundColor Cyan
    Write-Host "Execution Complete!" -ForegroundColor Green
    Write-Host "================================" -ForegroundColor Cyan
    
} catch {
    Write-Host "`n✗ API call failed:" -ForegroundColor Red
    Write-Host "  $($_.Exception.Message)" -ForegroundColor Red
    
    if ($VerboseOutput -and $_.Exception.Response) {
        Write-Host "`nResponse Details:" -ForegroundColor Yellow
        Write-Host "  Status: $($_.Exception.Response.StatusCode)" -ForegroundColor Gray
        Write-Host "  StatusDescription: $($_.Exception.Response.StatusDescription)" -ForegroundColor Gray
    }
    
    exit 1
}
