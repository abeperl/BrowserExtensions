# CSS Override Extension - Test Script

# This script helps validate the extension before submission
# Run this in PowerShell from the extension directory

param(
    [switch]$SkipBuild,
    [switch]$SkipValidation,
    [switch]$CreatePackage
)

$extensionDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$manifestPath = Join-Path $extensionDir "manifest.json"

Write-Host "CSS Override Extension - Test & Validation Script" -ForegroundColor Green
Write-Host "=====================================================" -ForegroundColor Green
Write-Host ""

# Test 1: Validate manifest.json
Write-Host "1. Validating manifest.json..." -ForegroundColor Yellow
if (Test-Path $manifestPath) {
    try {
        $manifest = Get-Content $manifestPath -Raw | ConvertFrom-Json
        Write-Host "✓ manifest.json is valid JSON" -ForegroundColor Green

        # Check required fields
        $requiredFields = @("name", "version", "description", "manifest_version", "permissions", "background", "action", "icons")
        foreach ($field in $requiredFields) {
            if ($manifest.$field -eq $null) {
                Write-Host "✗ Missing required field: $field" -ForegroundColor Red
            } else {
                Write-Host "✓ Required field present: $field" -ForegroundColor Green
            }
        }

        # Check manifest version
        if ($manifest.manifest_version -eq 3) {
            Write-Host "✓ Correct Manifest V3 format" -ForegroundColor Green
        } else {
            Write-Host "✗ Should use Manifest V3" -ForegroundColor Red
        }

    } catch {
        Write-Host "✗ manifest.json contains invalid JSON" -ForegroundColor Red
        Write-Host $_.Exception.Message -ForegroundColor Red
    }
} else {
    Write-Host "✗ manifest.json not found" -ForegroundColor Red
}

Write-Host ""

# Test 2: Check required files
Write-Host "2. Checking required files..." -ForegroundColor Yellow
$requiredFiles = @(
    "manifest.json",
    "background.js",
    "content.js",
    "popup.html",
    "popup.js",
    "settings.html",
    "settings.js",
    "styles.css",
    "README.md",
    "PRIVACY.md"
)

foreach ($file in $requiredFiles) {
    $filePath = Join-Path $extensionDir $file
    if (Test-Path $filePath) {
        Write-Host "✓ $file exists" -ForegroundColor Green
    } else {
        Write-Host "✗ $file missing" -ForegroundColor Red
    }
}

Write-Host ""

# Test 3: Check icons
Write-Host "3. Checking icons..." -ForegroundColor Yellow
$iconsDir = Join-Path $extensionDir "icons"
$requiredIcons = @("icon16.png", "icon32.png", "icon48.png", "icon128.png", "icon.svg")

if (Test-Path $iconsDir) {
    foreach ($icon in $requiredIcons) {
        $iconPath = Join-Path $iconsDir $icon
        if (Test-Path $iconPath) {
            Write-Host "✓ $icon exists" -ForegroundColor Green
        } else {
            Write-Host "⚠ $icon missing (can be generated from icon.svg)" -ForegroundColor Yellow
        }
    }
} else {
    Write-Host "✗ icons directory missing" -ForegroundColor Red
}

Write-Host ""

# Test 4: Validate JavaScript files
Write-Host "4. Validating JavaScript files..." -ForegroundColor Yellow
$jsFiles = @("background.js", "content.js", "popup.js", "settings.js")

foreach ($file in $jsFiles) {
    $filePath = Join-Path $extensionDir $file
    if (Test-Path $filePath) {
        try {
            $content = Get-Content $filePath -Raw
            # Basic syntax check - look for common issues
            if ($content -match "console\.log") {
                Write-Host "⚠ $file contains console.log statements (remove for production)" -ForegroundColor Yellow
            } else {
                Write-Host "✓ $file syntax appears clean" -ForegroundColor Green
            }
        } catch {
            Write-Host "✗ Error reading $file" -ForegroundColor Red
        }
    }
}

Write-Host ""

# Test 5: Check file sizes
Write-Host "5. Checking file sizes..." -ForegroundColor Yellow
$largeFiles = Get-ChildItem $extensionDir -Recurse -File | Where-Object {
    $_.Length -gt 2MB -and $_.Extension -notin @('.zip', '.git')
}

if ($largeFiles) {
    Write-Host "⚠ Large files detected (may impact performance):" -ForegroundColor Yellow
    foreach ($file in $largeFiles) {
        Write-Host "  - $($file.FullName): $([math]::Round($file.Length / 1MB, 2)) MB" -ForegroundColor Yellow
    }
} else {
    Write-Host "✓ No excessively large files found" -ForegroundColor Green
}

Write-Host ""

# Test 6: Create package if requested
if ($CreatePackage) {
    Write-Host "6. Creating extension package..." -ForegroundColor Yellow

    $packageName = "css-override-extension-v$($manifest.version).zip"
    $packagePath = Join-Path $extensionDir $packageName

    # Files to exclude from package
    $excludeFiles = @(
        "*.zip",
        ".git*",
        "*.log",
        "node_modules",
        "*.tmp",
        "*.bak",
        "generate-icons.ps1",
        "test-extension.ps1"
    )

    try {
        # Create ZIP file
        $filesToPackage = Get-ChildItem $extensionDir -File | Where-Object {
            $exclude = $false
            foreach ($pattern in $excludeFiles) {
                if ($_.Name -like $pattern) {
                    $exclude = $true
                    break
                }
            }
            -not $exclude
        }

        if ($filesToPackage) {
            Compress-Archive -Path $filesToPackage.FullName -DestinationPath $packagePath -Force
            Write-Host "✓ Package created: $packageName" -ForegroundColor Green
            Write-Host "  Package size: $([math]::Round((Get-Item $packagePath).Length / 1KB, 2)) KB" -ForegroundColor Green
        } else {
            Write-Host "✗ No files found to package" -ForegroundColor Red
        }
    } catch {
        Write-Host "✗ Failed to create package: $($_.Exception.Message)" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "Validation Summary:" -ForegroundColor Cyan
Write-Host "==================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Next Steps:" -ForegroundColor White
Write-Host "1. Fix any issues marked with ✗ or ⚠" -ForegroundColor White
Write-Host "2. Generate PNG icons from icon.svg if needed" -ForegroundColor White
Write-Host "3. Test extension in browser developer mode" -ForegroundColor White
Write-Host "4. Create screenshots for store listing" -ForegroundColor White
Write-Host "5. Submit to Chrome Web Store and Microsoft Edge Add-ons" -ForegroundColor White
Write-Host ""
Write-Host "For detailed submission guide, see STORE_SUBMISSION_GUIDE.md" -ForegroundColor White