# CSS Override Extension - Icon Generation Script
# This script generates PNG icons in various sizes from the SVG source

param(
    [string]$InkscapePath = "C:\Program Files\Inkscape\bin\inkscape.exe",
    [string]$ImageMagickPath = "C:\Program Files\ImageMagick-7.1.1-Q16-HDRI\magick.exe"
)

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$iconsDir = Join-Path $scriptDir "icons"
$svgPath = Join-Path $iconsDir "icon.svg"

$sizes = @(16, 32, 48, 128)

Write-Host "Generating PNG icons from SVG..." -ForegroundColor Green

foreach ($size in $sizes) {
    $outputPath = Join-Path $iconsDir "icon$size.png"

    if (Test-Path $InkscapePath) {
        # Use Inkscape if available (better quality)
        & $InkscapePath --export-type=png --export-filename="$outputPath" --export-width=$size --export-height=$size "$svgPath"
        Write-Host "Generated icon$size.png using Inkscape" -ForegroundColor Yellow
    }
    elseif (Test-Path $ImageMagickPath) {
        # Use ImageMagick as fallback
        & $ImageMagickPath convert "$svgPath" -resize "${size}x${size}" "$outputPath"
        Write-Host "Generated icon$size.png using ImageMagick" -ForegroundColor Yellow
    }
    else {
        Write-Host "Warning: Neither Inkscape nor ImageMagick found. Please install one of them to generate PNG icons." -ForegroundColor Red
        Write-Host "Manual steps:" -ForegroundColor Yellow
        Write-Host "1. Install Inkscape (https://inkscape.org/) or ImageMagick"
        Write-Host "2. Convert $svgPath to PNG at ${size}x${size} pixels"
        Write-Host "3. Save as icon$size.png in the icons directory"
        Write-Host ""
    }
}

Write-Host "Icon generation complete!" -ForegroundColor Green
Write-Host ""
Write-Host "Required icons for store submission:" -ForegroundColor Cyan
foreach ($size in $sizes) {
    $iconPath = Join-Path $iconsDir "icon$size.png"
    if (Test-Path $iconPath) {
        Write-Host "✓ icon$size.png ($size x $size)" -ForegroundColor Green
    } else {
        Write-Host "✗ icon$size.png ($size x $size) - MISSING" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "Next steps for store submission:" -ForegroundColor Cyan
Write-Host "1. Verify all icons are present and look good"
Write-Host "2. Test the extension thoroughly"
Write-Host "3. Create ZIP package for submission"
Write-Host "4. Submit to Chrome Web Store and Microsoft Edge Add-ons"