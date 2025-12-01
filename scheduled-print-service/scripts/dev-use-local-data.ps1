# Use the scheduled-print-service folder as runtime data root for local development
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path "$PSScriptRoot").Path
$env:SCHEDULED_PRINT_DATA_ROOT = $root
Write-Host "SCHEDULED_PRINT_DATA_ROOT=$env:SCHEDULED_PRINT_DATA_ROOT" -ForegroundColor Green
Write-Host "Artifacts will be written under this folder (out/logs/chromium-cache/printed-urls.txt)." -ForegroundColor Cyan
