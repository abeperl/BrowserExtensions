# Scans major user folders and key AppData caches, printing sizes in GB and top heavy subfolders.
param(
    [string]$UserRoot = "C:\\Users\\User",
    [int]$TopAppData = 20
)

$ErrorActionPreference = 'SilentlyContinue'
$ProgressPreference = 'SilentlyContinue'

function Get-FolderSizeBytes {
    param([Parameter(Mandatory=$true)][string]$Path)
    try {
        if (-not (Test-Path -LiteralPath $Path)) { return [int64]0 }
        $sum = (Get-ChildItem -LiteralPath $Path -Force -Recurse -ErrorAction SilentlyContinue |
            Measure-Object -Property Length -Sum).Sum
        if ($null -eq $sum) { return [int64]0 }
        return [int64]$sum
    } catch { return [int64]0 }
}

function Format-GB {
    param([Parameter(Mandatory=$true)][double]$Bytes)
    return ([math]::Round(($Bytes/1GB), 2))
}

Write-Host "--- Major user folders ---"
$major = 'Downloads','Videos','Desktop','Documents','Pictures','Music','OneDrive','OneDrive - Personal','source\\repos'
foreach ($name in $major) {
    $p = Join-Path $UserRoot $name
    if (Test-Path -LiteralPath $p) {
        $s = Get-FolderSizeBytes -Path $p
        "{0,8} GB  {1}" -f (Format-GB $s), $p
    }
}

Write-Host "`n--- Key caches under user profile ---"
$cacheRels = @(
    'AppData\\Local\\Temp',
    'AppData\\Local\\Packages',
    'AppData\\Local\\Microsoft\\Windows\\INetCache',
    'AppData\\Local\\Microsoft\\VisualStudio\\Packages',
    'AppData\\Local\\Microsoft\\OneDrive',
    'AppData\\Roaming\\Code\\Cache',
    'AppData\\Roaming\\Code\\CachedData',
    'AppData\\Roaming\\Code\\User\\workspaceStorage',
    'AppData\\Local\\Yarn',
    'AppData\\Local\\npm-cache',
    'AppData\\Roaming\\npm-cache',
    'AppData\\Local\\pnpm-store',
    'AppData\\Local\\pip\\Cache',
    'AppData\\Local\\Android\\Sdk',
    'AppData\\Local\\Docker',
    'AppData\\Local\\Google\\Chrome\\User Data\\Default\\Cache',
    'AppData\\Local\\Microsoft\\Edge\\User Data\\Default\\Cache',
    'AppData\\Roaming\\Mozilla\\Firefox\\Profiles',
    '.nuget\\packages',
    '.cache',
    '.gradle',
    '.m2\\repository'
)

$cacheAbs = $cacheRels | ForEach-Object { Join-Path $UserRoot $_ } | Where-Object { Test-Path -LiteralPath $_ }
$cacheAbs | ForEach-Object {
    $size = Get-FolderSizeBytes -Path $_
    "{0,8} GB  {1}" -f (Format-GB $size), $_
}

Write-Host "`n--- AppData heavy subfolders (top $TopAppData) ---"
$appRoots = 'AppData\\Local','AppData\\Roaming','AppData\\LocalLow' |
    ForEach-Object { Join-Path $UserRoot $_ } | Where-Object { Test-Path -LiteralPath $_ }

$rows = @()
foreach ($root in $appRoots) {
    Get-ChildItem -LiteralPath $root -Directory -Force -ErrorAction SilentlyContinue | ForEach-Object {
        $rows += [pscustomobject]@{ Path = $_.FullName; SizeBytes = (Get-FolderSizeBytes -Path $_.FullName) }
    }
}

$rows | Sort-Object SizeBytes -Descending | Select-Object -First $TopAppData | ForEach-Object {
    "{0,8} GB  {1}" -f (Format-GB $_.SizeBytes), $_.Path
}

Write-Host "`nSCAN_DONE"
