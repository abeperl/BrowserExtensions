#Requires -Version 5.1
<#
.SYNOPSIS
    Downloads and installs sqlite3.exe for database querying

.DESCRIPTION
    Downloads sqlite3.exe from SQLite official website and places it in the current directory
    or a specified location accessible in PATH.

.PARAMETER InstallPath
    Where to install sqlite3.exe (default: current directory)

.PARAMETER AddToPath
    Add the installation directory to system PATH

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File install-sqlite3.ps1
#>

param(
    [string]$InstallPath = $PSScriptRoot,
    [switch]$AddToPath = $false
)

Write-Host "SQLite3 Installation Tool" -ForegroundColor Cyan
Write-Host "=========================" -ForegroundColor Cyan
Write-Host ""

# SQLite download URL (Windows 64-bit)
$SqliteUrl = "https://www.sqlite.org/2024/sqlite-tools-win-x64-3460100.zip"
$ZipFile = Join-Path $env:TEMP "sqlite-tools.zip"
$ExtractPath = Join-Path $env:TEMP "sqlite-tools"

try {
    # Check if sqlite3 already exists
    $ExistingSqlite = Get-Command sqlite3 -ErrorAction SilentlyContinue
    if ($ExistingSqlite) {
        Write-Host "[OK] sqlite3 already installed at: $($ExistingSqlite.Source)" -ForegroundColor Green
        Write-Host ""
        & sqlite3 -version
        exit 0
    }

    Write-Host "Downloading SQLite tools..." -ForegroundColor Yellow
    Write-Host "URL: $SqliteUrl" -ForegroundColor Gray

    # Download
    Invoke-WebRequest -Uri $SqliteUrl -OutFile $ZipFile -UseBasicParsing
    Write-Host "[OK] Downloaded to: $ZipFile" -ForegroundColor Green

    # Extract
    Write-Host ""
    Write-Host "Extracting archive..." -ForegroundColor Yellow
    if (Test-Path $ExtractPath) {
        Remove-Item $ExtractPath -Recurse -Force
    }
    Expand-Archive -Path $ZipFile -DestinationPath $ExtractPath -Force
    Write-Host "[OK] Extracted" -ForegroundColor Green

    # Find sqlite3.exe
    $Sqlite3Exe = Get-ChildItem $ExtractPath -Filter "sqlite3.exe" -Recurse | Select-Object -First 1

    if (-not $Sqlite3Exe) {
        Write-Error "Could not find sqlite3.exe in downloaded archive"
        exit 1
    }

    # Ensure install path exists
    if (-not (Test-Path $InstallPath)) {
        New-Item -ItemType Directory -Path $InstallPath -Force | Out-Null
    }

    # Copy to install location
    $Destination = Join-Path $InstallPath "sqlite3.exe"
    Write-Host ""
    Write-Host "Installing sqlite3.exe..." -ForegroundColor Yellow
    Copy-Item $Sqlite3Exe.FullName -Destination $Destination -Force
    Write-Host "[OK] Installed to: $Destination" -ForegroundColor Green

    # Test
    Write-Host ""
    Write-Host "Testing installation..." -ForegroundColor Yellow
    $Version = & $Destination -version
    Write-Host "[OK] SQLite version: $Version" -ForegroundColor Green

    # Add to PATH if requested
    if ($AddToPath) {
        Write-Host ""
        Write-Host "Adding to system PATH..." -ForegroundColor Yellow

        $CurrentPath = [Environment]::GetEnvironmentVariable("Path", "Machine")
        if ($CurrentPath -notlike "*$InstallPath*") {
            $NewPath = "$CurrentPath;$InstallPath"
            [Environment]::SetEnvironmentVariable("Path", $NewPath, "Machine")
            Write-Host "[OK] Added to system PATH" -ForegroundColor Green
            Write-Host "[INFO] You may need to restart PowerShell for PATH changes to take effect" -ForegroundColor Yellow
        } else {
            Write-Host "[INFO] $InstallPath already in PATH" -ForegroundColor Gray
        }
    }

    # Cleanup
    Write-Host ""
    Write-Host "Cleaning up..." -ForegroundColor Yellow
    Remove-Item $ZipFile -Force -ErrorAction SilentlyContinue
    Remove-Item $ExtractPath -Recurse -Force -ErrorAction SilentlyContinue
    Write-Host "[OK] Cleanup complete" -ForegroundColor Green

    Write-Host ""
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "Installation Complete!" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "sqlite3.exe location: $Destination" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "You can now run:" -ForegroundColor Yellow
    Write-Host "  $Destination `"path\to\database.db`" `"SELECT * FROM table;`"" -ForegroundColor Gray
    Write-Host ""
    Write-Host "Or add it to your PATH for system-wide access:" -ForegroundColor Yellow
    Write-Host "  Run this script with -AddToPath switch" -ForegroundColor Gray
    Write-Host ""

} catch {
    Write-Error "Installation failed: $_"
    exit 1
}
