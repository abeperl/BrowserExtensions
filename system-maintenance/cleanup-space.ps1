# Clean up disk space by removing safe caches and build artifacts.
# Supports DryRun (preview) and optional inclusion of system caches (admin required).

[CmdletBinding(SupportsShouldProcess=$true, ConfirmImpact='Medium')]
param(
    [string]$UserRoot = "C:\\Users\\User",
    [string]$ReposRoot = "C:\\Users\\User\\source\\repos",
    [switch]$ClearNuGet,
    [switch]$ClearNodeCaches,
    [switch]$ClearPipCache,
    [switch]$ClearUvCache,
    [switch]$ClearVsCodeCaches,
    [switch]$ClearBrowserCaches,
    [switch]$ClearUserTemp,
    [switch]$ClearUwpLocalCache,
    [switch]$ClearDevArtifacts,
    [switch]$IncludeSystemCaches,
    [switch]$ForceCloseApps,
    [switch]$DryRun
)

$ErrorActionPreference = 'SilentlyContinue'
$ProgressPreference = 'SilentlyContinue'

function Is-Admin {
    $cur = [Security.Principal.WindowsIdentity]::GetCurrent()
    $pr = New-Object Security.Principal.WindowsPrincipal($cur)
    return $pr.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Get-SizeBytes {
    param([string]$Path)
    try {
        if (-not (Test-Path -LiteralPath $Path)) { return [int64]0 }
        $sum = (Get-ChildItem -LiteralPath $Path -Force -Recurse -ErrorAction SilentlyContinue | Measure-Object Length -Sum).Sum
        if ($null -eq $sum) { return [int64]0 }
        return [int64]$sum
    } catch { return [int64]0 }
}

function Format-GB { param([double]$Bytes) ([math]::Round(($Bytes/1GB),2)) }

function Remove-PathSafe {
    param([string]$Path)
    if (-not (Test-Path -LiteralPath $Path)) { return [int64]0 }
    $size = Get-SizeBytes $Path
    if ($DryRun) {
        Write-Host ("DRYRUN would remove {0,8} GB  {1}" -f (Format-GB $size), $Path)
        return $size
    }
    try {
        if ($PSCmdlet.ShouldProcess($Path, 'Remove-Item -Recurse -Force')) {
            Remove-Item -LiteralPath $Path -Recurse -Force -ErrorAction SilentlyContinue
        }
    } catch {}
    return $size
}

function Clear-DirectoryContents {
    param([string]$Path)
    if (-not (Test-Path -LiteralPath $Path)) { return [int64]0 }
    $size = Get-SizeBytes $Path
    if ($DryRun) {
        Write-Host ("DRYRUN would clear contents of {0,8} GB  {1}" -f (Format-GB $size), $Path)
        return $size
    }
    try {
        if ($PSCmdlet.ShouldProcess($Path, 'Clear directory contents')) {
            Get-ChildItem -LiteralPath $Path -Force -ErrorAction SilentlyContinue | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
        }
    } catch {}
    return $size
}

$freed = [int64]0

Write-Host "Starting cleanup... (DryRun=$DryRun)"

# Optional: Force-close common apps to release locks
if ($ForceCloseApps -and -not $DryRun) {
    foreach ($p in 'Code - Insiders','Code','chrome','msedge','postman') {
        Get-Process $p -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
    }
}

# 1) NuGet global packages
if ($ClearNuGet) {
    $nugetPath = Join-Path $UserRoot '.nuget\packages'
    if (Test-Path $nugetPath) {
        Write-Host "Clearing NuGet global-packages..."
        $freed += Remove-PathSafe -Path $nugetPath
    }
    # Also clear via CLI if available (will re-create folders)
    $dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
    if ($dotnet -and -not $DryRun) {
        try { dotnet nuget locals global-packages --clear | Out-Null } catch {}
        try { dotnet nuget locals http-cache --clear | Out-Null } catch {}
        try { dotnet nuget locals temp --clear | Out-Null } catch {}
    }
}

# 2) Node-related caches
if ($ClearNodeCaches) {
    $paths = @(
        Join-Path $UserRoot 'AppData\Local\npm-cache',
        Join-Path $UserRoot 'AppData\Roaming\npm-cache',
        Join-Path $UserRoot 'AppData\Local\Yarn',
        Join-Path $UserRoot 'AppData\Local\pnpm-store'
    ) | Where-Object { Test-Path -LiteralPath $_ }
    foreach ($p in $paths) { $freed += Remove-PathSafe -Path $p }
    if (-not $DryRun) {
        if (Get-Command npm -ErrorAction SilentlyContinue) { try { npm cache clean --force | Out-Null } catch {} }
        if (Get-Command yarn -ErrorAction SilentlyContinue) { try { yarn cache clean | Out-Null } catch {} }
        if (Get-Command pnpm -ErrorAction SilentlyContinue) { try { pnpm store prune | Out-Null } catch {} }
    }
}

# 3) Python/uv caches
if ($ClearPipCache) {
    $pipCache = Join-Path $UserRoot 'AppData\Local\pip\Cache'
    if (Test-Path $pipCache) { $freed += Remove-PathSafe -Path $pipCache }
    if (Get-Command pip -ErrorAction SilentlyContinue -and -not $DryRun) { try { pip cache purge | Out-Null } catch {} }
}
if ($ClearUvCache) {
    $uvPath = Join-Path $UserRoot 'AppData\Local\uv'
    if (Test-Path $uvPath) {
        # Prefer uv CLI if available
        if (Get-Command uv -ErrorAction SilentlyContinue -and -not $DryRun) {
            try { uv cache prune | Out-Null } catch {}
        } else {
            $freed += Remove-PathSafe -Path $uvPath
        }
    }
}

# 4) VS Code / VS Code Insiders caches
if ($ClearVsCodeCaches) {
    $codePaths = @(
        Join-Path $UserRoot 'AppData\Roaming\Code\Cache',
        Join-Path $UserRoot 'AppData\Roaming\Code\CachedData',
        Join-Path $UserRoot 'AppData\Roaming\Code\User\workspaceStorage',
        Join-Path $UserRoot 'AppData\Roaming\Code - Insiders\Cache',
        Join-Path $UserRoot 'AppData\Roaming\Code - Insiders\CachedData',
        Join-Path $UserRoot 'AppData\Roaming\Code - Insiders\User\workspaceStorage'
    ) | Where-Object { Test-Path -LiteralPath $_ }
    foreach ($p in $codePaths) { $freed += Clear-DirectoryContents -Path $p }
}

# 5) Browser caches (Chrome/Edge)
if ($ClearBrowserCaches) {
    $chrome = Join-Path $UserRoot 'AppData\Local\Google\Chrome\User Data'
    $edge   = Join-Path $UserRoot 'AppData\Local\Microsoft\Edge\User Data'
    $targets = @()
    if (Test-Path $chrome) { $targets += (Get-ChildItem -LiteralPath $chrome -Directory -Force -ErrorAction SilentlyContinue | Where-Object { Test-Path (Join-Path $_.FullName 'Cache') } | ForEach-Object { Join-Path $_.FullName 'Cache' }) }
    if (Test-Path $edge)   { $targets += (Get-ChildItem -LiteralPath $edge   -Directory -Force -ErrorAction SilentlyContinue | Where-Object { Test-Path (Join-Path $_.FullName 'Cache') } | ForEach-Object { Join-Path $_.FullName 'Cache' }) }
    foreach ($p in $targets) { $freed += Clear-DirectoryContents -Path $p }
}

# 6) User Temp and SquirrelTemp
if ($ClearUserTemp) {
    $temp = Join-Path $UserRoot 'AppData\Local\Temp'
    if (Test-Path $temp) { $freed += Clear-DirectoryContents -Path $temp }
    $squirrel = Join-Path $UserRoot 'AppData\Local\SquirrelTemp'
    if (Test-Path $squirrel) { $freed += Remove-PathSafe -Path $squirrel }
}

# 7) UWP LocalCache folders
if ($ClearUwpLocalCache) {
    $packages = Join-Path $UserRoot 'AppData\Local\Packages'
    if (Test-Path $packages) {
        Get-ChildItem -LiteralPath $packages -Directory -Force -ErrorAction SilentlyContinue |
            ForEach-Object {
                $lc = Join-Path $_.FullName 'LocalCache'
                if (Test-Path $lc) { $freed += Clear-DirectoryContents -Path $lc }
            }
    }
}

# 8) Dev artifacts under repos (bin/obj/node_modules etc.)
if ($ClearDevArtifacts -and (Test-Path $ReposRoot)) {
    $artifactNames = 'node_modules','bin','obj','dist','build','out','Debug','Release',
                     '.next','.nuxt','.angular','.svelte-kit','.vite','.parcel-cache','.turbo','coverage','.cache'
    Get-ChildItem -LiteralPath $ReposRoot -Directory -Recurse -Force -ErrorAction SilentlyContinue |
        Where-Object { $artifactNames -contains $_.Name } |
        ForEach-Object { $freed += Remove-PathSafe -Path $_.FullName }
}

# 9) Optional system caches (admin)
if ($IncludeSystemCaches) {
    if (-not (Is-Admin)) { Write-Warning 'System cache cleanup requires running PowerShell as Administrator. Skipping.' }
    else {
        $targets = @(
            'C:\\Windows\\Temp',
            'C:\\Windows\\SoftwareDistribution\\Download',
            'C:\\Windows\\Prefetch',
            'C:\\Windows\\Logs',
            'C:\\ProgramData\\Package Cache'
        ) | Where-Object { Test-Path -LiteralPath $_ }
        foreach ($p in $targets) { $freed += Clear-DirectoryContents -Path $p }
    }
}

Write-Host ("\nEstimated space freed: {0} GB" -f (Format-GB $freed))
Write-Host "CLEANUP_DONE"
