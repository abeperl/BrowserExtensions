# Scans common system caches and reports sizes. Optionally runs DISM component store analysis.
param(
    [string]$DriveRoot = 'C:',
    [switch]$IncludeDISM
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

Write-Host "--- System/temp/update caches on $DriveRoot ---"
$targets = @(
    Join-Path $DriveRoot 'Windows\Temp',
    Join-Path $DriveRoot 'Windows\SoftwareDistribution\Download',
    Join-Path $DriveRoot 'Windows\SoftwareDistribution\DataStore',
    Join-Path $DriveRoot 'Windows\Prefetch',
    Join-Path $DriveRoot 'Windows\Logs',
    Join-Path $DriveRoot 'Windows\Panther',
    Join-Path $DriveRoot 'Windows\System32\DriverStore\FileRepository',
    Join-Path $DriveRoot '$Recycle.Bin',
    Join-Path $DriveRoot 'ProgramData\Package Cache',
    Join-Path $DriveRoot 'ProgramData\Microsoft\Windows\WER',
    Join-Path $DriveRoot 'ProgramData\Docker'
)

foreach ($t in $targets) {
    if (Test-Path -LiteralPath $t) {
        $s = Get-FolderSizeBytes -Path $t
        "{0,8} GB  {1}" -f (Format-GB $s), $t
    }
}

Write-Host "`n--- Delivery Optimization cache ---"
try {
    $do = Get-Command Get-DeliveryOptimizationStatus -ErrorAction SilentlyContinue
    if ($do) {
        Get-DeliveryOptimizationStatus | Select-Object -First 10 | Format-Table -AutoSize | Out-String | Write-Host
    } else {
        Write-Host 'Delivery Optimization status cmdlet not available.'
    }
} catch { Write-Host 'Delivery Optimization status not available' }

if ($IncludeDISM) {
    Write-Host "`n--- DISM component store analysis (summary) ---"
    try {
        dism.exe /online /cleanup-image /analyzecomponentstore |
            Select-String -Pattern 'Component Store', 'Cache', 'Reclaimable', 'Recommended' |
            ForEach-Object { $_.Line } | Write-Host
    } catch { Write-Host 'DISM analysis not available' }
}

Write-Host "`nSYS_SCAN_DONE"
