# Update Native Messaging Host Manifest
# This script updates the manifest with the current extension ID and correct paths

param(
    [string]$ExtensionId = "",
    [switch]$Help
)

if ($Help) {
    Write-Host "Usage: .\update-manifest.ps1 -ExtensionId <extension-id>"
    Write-Host ""
    Write-Host "Example:"
    Write-Host "  .\update-manifest.ps1 -ExtensionId 'abcdefghijklmnopqrstuvwxyzabcdef'"
    Write-Host ""
    Write-Host "To find your extension ID:"
    Write-Host "  1. Go to chrome://extensions/"
    Write-Host "  2. Enable Developer mode"
    Write-Host "  3. Find your extension and copy the ID"
    exit 0
}

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ManifestPath = Join-Path $ScriptDir "native-messaging-host-manifest.json"

# Check if manifest exists
if (!(Test-Path $ManifestPath)) {
    Write-Error "Manifest file not found: $ManifestPath"
    exit 1
}

# Read current manifest
try {
    $manifest = Get-Content $ManifestPath -Raw | ConvertFrom-Json
} catch {
    Write-Error "Failed to parse manifest JSON: $_"
    exit 1
}

# Update paths
$exePath = Join-Path $ScriptDir "word_updater.exe"
$pyPath = Join-Path $ScriptDir "word_updater.py"
$launcherPath = Join-Path $ScriptDir "word_updater_launcher.bat"

# Check what executable to use
if (Test-Path $exePath) {
    $manifest.path = $exePath.Replace('\', '\\')
    Write-Host "✓ Using compiled executable: $exePath"
} elseif (Test-Path $pyPath) {
    # Create launcher batch file for Python script
    $launcherContent = @"
@echo off
python "$($pyPath.Replace('\', '\\'))" %*
"@
    $launcherContent | Set-Content $launcherPath -Encoding ASCII
    $manifest.path = $launcherPath.Replace('\', '\\')
    Write-Host "✓ Using Python script with launcher: $launcherPath"
} else {
    Write-Error "Neither word_updater.exe nor word_updater.py found!"
    exit 1
}

# Update extension ID if provided
if ($ExtensionId) {
    if ($ExtensionId -match '^[a-z]{32}$') {
        $manifest.allowed_origins = @("chrome-extension://$ExtensionId/")
        Write-Host "✓ Updated extension ID: $ExtensionId"
    } else {
        Write-Error "Invalid extension ID format. Should be 32 lowercase letters."
        exit 1
    }
} else {
    Write-Warning "No extension ID provided. Current allowed origins:"
    $manifest.allowed_origins | ForEach-Object { Write-Host "  $_" }
    Write-Host ""
    Write-Host "Run with -ExtensionId parameter to update the extension ID"
}

# Save updated manifest
try {
    $manifest | ConvertTo-Json -Depth 10 | Set-Content $ManifestPath -Encoding UTF8
    Write-Host "✓ Manifest updated successfully"
} catch {
    Write-Error "Failed to save manifest: $_"
    exit 1
}

# Display final manifest info
Write-Host ""
Write-Host "=== Updated Manifest ===" -ForegroundColor Cyan
Write-Host "Name: $($manifest.name)"
Write-Host "Path: $($manifest.path)"
Write-Host "Allowed Origins:"
$manifest.allowed_origins | ForEach-Object { Write-Host "  $_" }

Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "1. Run install.bat as Administrator to register the native host"
Write-Host "2. Restart Chrome/Edge"
Write-Host "3. Test the extension"