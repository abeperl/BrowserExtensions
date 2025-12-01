# Deploy Scheduled Print Service to Server
# This script copies the published files to the server location

param(
    [string]$SourcePath = ".\publish",
    [string]$DestinationPath = "C:\Program Files\Malchut\ScheduledPrintService",
    [switch]$StopService = $true,
    [switch]$StartService = $true
)

Write-Host "Deploying Scheduled Print Service..." -ForegroundColor Cyan

# Check if running as administrator
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Warning "Not running as Administrator. You may need elevated privileges to stop/start the service or copy to Program Files."
}

# Stop service if requested
if ($StopService) {
    Write-Host "Stopping service..." -ForegroundColor Yellow
    try {
        Stop-Service -Name "ScheduledPrintService" -ErrorAction SilentlyContinue
        Start-Sleep -Seconds 2
        Write-Host "Service stopped." -ForegroundColor Green
    } catch {
        Write-Warning "Could not stop service: $_"
    }
}

# Create destination directory if it doesn't exist
if (-not (Test-Path $DestinationPath)) {
    Write-Host "Creating destination directory: $DestinationPath" -ForegroundColor Yellow
    New-Item -ItemType Directory -Path $DestinationPath -Force | Out-Null
}

# Copy files
Write-Host "Copying files from $SourcePath to $DestinationPath..." -ForegroundColor Yellow
try {
    Copy-Item -Path "$SourcePath\*" -Destination $DestinationPath -Recurse -Force
    Write-Host "Files copied successfully." -ForegroundColor Green
} catch {
    Write-Error "Failed to copy files: $_"
    exit 1
}

# Start service if requested
if ($StartService) {
    Write-Host "Starting service..." -ForegroundColor Yellow
    try {
        Start-Service -Name "ScheduledPrintService"
        Start-Sleep -Seconds 2
        $status = Get-Service -Name "ScheduledPrintService"
        Write-Host "Service status: $($status.Status)" -ForegroundColor Green
    } catch {
        Write-Warning "Could not start service: $_"
    }
}

Write-Host "`nDeployment complete!" -ForegroundColor Cyan
Write-Host "Data/logs location (configured in appsettings.json): E:\Share\server\servern\Software\ScheduledPrintService" -ForegroundColor Cyan
