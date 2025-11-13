param(
  [switch]$WhatIf
)
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path $PSScriptRoot -Parent
$targetRoot = Join-Path $PSScriptRoot '.'

$items = @('logs','out','chromium-cache','printed-urls.txt','output.txt','error.txt')

Write-Host "Scanning for artifact items at repo root: $repoRoot" -ForegroundColor Cyan
$found = @()
foreach ($i in $items) {
  $path = Join-Path $repoRoot $i
  if (Test-Path $path) { $found += $path }
}

if (-not $found) { Write-Host "No artifacts to move." -ForegroundColor Yellow; return }

Write-Host "Found artifacts:" -ForegroundColor Green
$found | ForEach-Object { Write-Host " - $_" }

foreach ($src in $found) {
  $name = Split-Path $src -Leaf
  $dest = Join-Path $targetRoot $name
  if (Test-Path $dest) {
    Write-Host "Destination already exists, merging: $dest" -ForegroundColor Yellow
  }
  if ($WhatIf) {
    Write-Host "[WhatIf] Would move '$src' -> '$dest'" -ForegroundColor Magenta
    continue
  }
  Move-Item -Path $src -Destination $dest -Force
  Write-Host "Moved '$src' -> '$dest'" -ForegroundColor Cyan
}

Write-Host "Done. Consider setting $env:SCHEDULED_PRINT_DATA_ROOT to keep future artifacts here." -ForegroundColor Green
