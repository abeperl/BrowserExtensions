# Scans for common development build artifacts and caches under a root folder and prints their sizes.
param(
    [string]$Root = 'C:\\Users\\User\\source\\repos',
    [int]$Top = 30
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

Write-Host "--- Dev artifacts under $Root (top $Top) ---"
$names = @('node_modules','bin','obj','dist','build','out','Debug','Release',
           '.next','.nuxt','.angular','.svelte-kit','.vite','.parcel-cache','.turbo',
           'coverage','.cache')

$rows = @()
Get-ChildItem -LiteralPath $Root -Directory -Recurse -Force -ErrorAction SilentlyContinue |
    Where-Object { $names -contains $_.Name } |
    ForEach-Object {
        $rows += [pscustomobject]@{ Path = $_.FullName; Name = $_.Name; SizeBytes = (Get-FolderSizeBytes -Path $_.FullName) }
    }

$rows | Sort-Object SizeBytes -Descending | Select-Object -First $Top | ForEach-Object {
    "{0,8} GB  {1}" -f (Format-GB $_.SizeBytes), $_.Path
}

Write-Host "`nDEV_SCAN_DONE"
