# Simple icon generation script
Add-Type -AssemblyName System.Drawing

$icon48Path = "icons/icon48.png"
$icon128Path = "icons/icon128.png"

if (Test-Path $icon48Path) {
    try {
        $src = [System.Drawing.Image]::FromFile($icon48Path)
        $bmp = New-Object System.Drawing.Bitmap(128, 128)
        $g = [System.Drawing.Graphics]::FromImage($bmp)
        $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $g.DrawImage($src, 0, 0, 128, 128)
        $bmp.Save($icon128Path, [System.Drawing.Imaging.ImageFormat]::Png)
        $g.Dispose()
        $bmp.Dispose()
        $src.Dispose()
        Write-Host "Successfully generated icon128.png"
    } catch {
        Write-Host "Error generating icon: $($_.Exception.Message)"
    }
} else {
    Write-Host "Source icon48.png not found"
}