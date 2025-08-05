# Word Template Extension - Store Package Builder
# This script creates distribution packages for Chrome Web Store and Edge Add-ons

param(
    [string]$Version = "1.0.0",
    [switch]$IncludeSource = $false,
    [switch]$BuildNativeHost = $true
)

$ErrorActionPreference = "Stop"

# Script directory
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = $ScriptDir
$ExtensionDir = Join-Path $ProjectRoot "extension"
$NativeHostDir = Join-Path $ProjectRoot "native-host"
$OutputDir = Join-Path $ProjectRoot "dist"

Write-Host "=== Word Template Extension Package Builder ===" -ForegroundColor Cyan
Write-Host "Project Root: $ProjectRoot" -ForegroundColor Gray
Write-Host "Version: $Version" -ForegroundColor Yellow

# Create output directory
if (Test-Path $OutputDir) {
    Remove-Item $OutputDir -Recurse -Force
}
New-Item -ItemType Directory -Path $OutputDir | Out-Null

# Validate extension files
Write-Host "`nValidating extension files..." -ForegroundColor Yellow

$RequiredFiles = @(
    "manifest.json",
    "popup.html", 
    "popup.js",
    "background.js",
    "content.js",
    "styles.css",
    "welcome.html"
)

$MissingFiles = @()
foreach ($file in $RequiredFiles) {
    $filePath = Join-Path $ExtensionDir $file
    if (!(Test-Path $filePath)) {
        $MissingFiles += $file
    }
}

if ($MissingFiles.Count -gt 0) {
    Write-Host "ERROR: Missing required files:" -ForegroundColor Red
    $MissingFiles | ForEach-Object { Write-Host "  - $_" -ForegroundColor Red }
    exit 1
}

# Check icons
$IconsDir = Join-Path $ExtensionDir "icons"
$RequiredIcons = @("icon16.png", "icon32.png", "icon48.png", "icon128.png")
$MissingIcons = @()

foreach ($icon in $RequiredIcons) {
    $iconPath = Join-Path $IconsDir $icon
    if (!(Test-Path $iconPath)) {
        $MissingIcons += $icon
    }
}

if ($MissingIcons.Count -gt 0) {
    Write-Host "WARNING: Missing icon files:" -ForegroundColor Yellow
    $MissingIcons | ForEach-Object { Write-Host "  - $_" -ForegroundColor Yellow }
}

# Update version in manifest.json
Write-Host "Updating manifest version to $Version..." -ForegroundColor Yellow
$ManifestPath = Join-Path $ExtensionDir "manifest.json"
$Manifest = Get-Content $ManifestPath -Raw | ConvertFrom-Json
$Manifest.version = $Version
$Manifest | ConvertTo-Json -Depth 10 | Set-Content $ManifestPath -Encoding UTF8

# Build native host if requested
if ($BuildNativeHost -and (Test-Path $NativeHostDir)) {
    Write-Host "`nBuilding native host..." -ForegroundColor Yellow
    
    $PythonScript = Join-Path $NativeHostDir "word_updater.py"
    if (Test-Path $PythonScript) {
        Push-Location $NativeHostDir
        try {
            # Check if PyInstaller is installed
            $PyInstallerCheck = py -c "import PyInstaller" 2>&1
            if ($LASTEXITCODE -ne 0) {
                Write-Host "Installing PyInstaller..." -ForegroundColor Yellow
                py -m pip install pyinstaller
            }
            
            # Build executable
            Write-Host "Creating executable..." -ForegroundColor Yellow
            py -m PyInstaller --onefile --console --name word_updater word_updater.py
            
            if (Test-Path "dist\word_updater.exe") {
                Copy-Item "dist\word_updater.exe" "word_updater.exe" -Force
                Write-Host "✓ Native host built successfully" -ForegroundColor Green
            } else {
                Write-Host "⚠ Native host build may have failed" -ForegroundColor Yellow
            }
        }
        catch {
            Write-Host "⚠ Error building native host: $_" -ForegroundColor Yellow
        }
        finally {
            Pop-Location
        }
    }
}

# Create Chrome Web Store package
Write-Host "`nCreating Chrome Web Store package..." -ForegroundColor Yellow
$ChromePackage = Join-Path $OutputDir "word-template-extension-chrome-v$Version.zip"

# Copy extension files to temp directory
$TempDir = Join-Path $OutputDir "temp"
New-Item -ItemType Directory -Path $TempDir | Out-Null
Copy-Item "$ExtensionDir\*" $TempDir -Recurse -Force

# Create ZIP package
Add-Type -AssemblyName System.IO.Compression.FileSystem
[System.IO.Compression.ZipFile]::CreateFromDirectory($TempDir, $ChromePackage)
Remove-Item $TempDir -Recurse -Force

Write-Host "✓ Chrome package created: $ChromePackage" -ForegroundColor Green

# Create Edge Add-ons package (same as Chrome)
$EdgePackage = Join-Path $OutputDir "word-template-extension-edge-v$Version.zip"
Copy-Item $ChromePackage $EdgePackage
Write-Host "✓ Edge package created: $EdgePackage" -ForegroundColor Green

# Create source code package if requested
if ($IncludeSource) {
    Write-Host "`nCreating source code package..." -ForegroundColor Yellow
    $SourcePackage = Join-Path $OutputDir "word-template-extension-source-v$Version.zip"
    
    $SourceTemp = Join-Path $OutputDir "source"
    New-Item -ItemType Directory -Path $SourceTemp | Out-Null
    
    # Copy relevant source files
    $SourceItems = @(
        "extension",
        "native-host",
        "templates", 
        "*.md",
        "*.txt"
    )
    
    foreach ($item in $SourceItems) {
        $sourcePath = Join-Path $ProjectRoot $item
        if (Test-Path $sourcePath) {
            Copy-Item $sourcePath "$SourceTemp\" -Recurse -Force
        }
    }
    
    [System.IO.Compression.ZipFile]::CreateFromDirectory($SourceTemp, $SourcePackage)
    Remove-Item $SourceTemp -Recurse -Force
    Write-Host "✓ Source package created: $SourcePackage" -ForegroundColor Green
}

# Create installer package
Write-Host "`nCreating installer package..." -ForegroundColor Yellow
$InstallerDir = Join-Path $OutputDir "installer"
New-Item -ItemType Directory -Path $InstallerDir | Out-Null

# Copy native host files
if (Test-Path $NativeHostDir) {
    Copy-Item "$NativeHostDir\*" $InstallerDir -Recurse -Force
}

# Copy extension package
Copy-Item $ChromePackage $InstallerDir

# Create installation script
$InstallScript = @"
@echo off
echo Installing Word Template Extension...
echo.

REM Check if Python is installed
python --version >nul 2>&1
if errorlevel 1 (
    echo ERROR: Python is not installed or not in PATH
    echo Please install Python 3.7+ from https://python.org
    pause
    exit /b 1
)

REM Install Python dependencies
echo Installing Python dependencies...
pip install python-docx pywin32

REM Register native messaging host
echo Registering native messaging host...
if exist "native-messaging-host-manifest.json" (
    copy "native-messaging-host-manifest.json" "%LOCALAPPDATA%\Google\Chrome\User Data\NativeMessagingHosts\com.wordtemplate.extension.json"
    copy "native-messaging-host-manifest.json" "%LOCALAPPDATA%\Microsoft\Edge\User Data\NativeMessagingHosts\com.wordtemplate.extension.json"
    echo Native host registered successfully
) else (
    echo WARNING: Native host manifest not found
)

REM Create templates directory
if not exist "%USERPROFILE%\Documents\Word Template Extension\" (
    mkdir "%USERPROFILE%\Documents\Word Template Extension"
    mkdir "%USERPROFILE%\Documents\Word Template Extension\Templates"
    mkdir "%USERPROFILE%\Documents\Word Template Extension\Output"
    echo Created templates directory in Documents
)

echo.
echo Installation complete!
echo.
echo Next steps:
echo 1. Install the extension in Chrome/Edge from the ZIP file
echo 2. Place your Word templates in: %USERPROFILE%\Documents\Word Template Extension\Templates
echo 3. Use placeholders like {{EMAIL}}, {{PHONE}}, {{DATE}} in your templates
echo.
pause
"@

$InstallScript | Set-Content (Join-Path $InstallerDir "install.bat") -Encoding ASCII

# Create README for installer
$InstallerReadme = @"
# Word Template Extension Installer

This package contains everything needed to install and use the Word Template Extension.

## Installation Steps

1. **Run install.bat** - This will:
   - Check Python installation
   - Install required Python packages
   - Register the native messaging host
   - Create template directories

2. **Install Browser Extension**:
   - Open Chrome or Edge
   - Go to Extensions page (chrome://extensions/ or edge://extensions/)
   - Enable "Developer mode"
   - Click "Load unpacked" and select the extension folder
   - OR drag the ZIP file onto the extensions page

3. **Add Templates**:
   - Place Word templates in: `Documents\Word Template Extension\Templates\`
   - Use placeholders like {{EMAIL}}, {{PHONE}}, {{DATE}} in your templates

## System Requirements

- Windows 10/11
- Python 3.7+
- Microsoft Word
- Chrome or Edge browser

## Support

For help and documentation: https://github.com/your-repo/word-template-extension
"@

$InstallerReadme | Set-Content (Join-Path $InstallerDir "README.txt") -Encoding UTF8

$InstallerPackage = Join-Path $OutputDir "word-template-extension-installer-v$Version.zip"
[System.IO.Compression.ZipFile]::CreateFromDirectory($InstallerDir, $InstallerPackage)
Remove-Item $InstallerDir -Recurse -Force

Write-Host "✓ Installer package created: $InstallerPackage" -ForegroundColor Green

# Generate summary
Write-Host "`n=== Package Build Summary ===" -ForegroundColor Cyan
Write-Host "Version: $Version" -ForegroundColor White
Write-Host "Output Directory: $OutputDir" -ForegroundColor Gray
Write-Host ""
Write-Host "Created Packages:" -ForegroundColor Yellow
Get-ChildItem $OutputDir -Filter "*.zip" | ForEach-Object {
    $size = [math]::Round($_.Length / 1KB, 2)
    Write-Host "  📦 $($_.Name) ($size KB)" -ForegroundColor Green
}

Write-Host "`nNext Steps:" -ForegroundColor Yellow
Write-Host "1. Test the extension locally using 'Load unpacked'" -ForegroundColor White
Write-Host "2. Create developer accounts for Chrome Web Store and Edge Add-ons" -ForegroundColor White  
Write-Host "3. Prepare store assets (screenshots, promotional images)" -ForegroundColor White
Write-Host "4. Upload packages to respective stores" -ForegroundColor White

Write-Host "`nStore Submission Files Needed:" -ForegroundColor Yellow
Write-Host "  - Extension ZIP (created ✓)" -ForegroundColor Green
Write-Host "  - Screenshots (5x 1280x800px)" -ForegroundColor Red
Write-Host "  - Promotional images (440x280px, 920x680px, 1400x560px)" -ForegroundColor Red
Write-Host "  - Privacy policy (created ✓)" -ForegroundColor Green
Write-Host "  - Store description (created ✓)" -ForegroundColor Green

Write-Host "`nPackage build completed successfully! 🎉" -ForegroundColor Green
