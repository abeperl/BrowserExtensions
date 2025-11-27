param(
  [string]$Name = "ScheduledPrintService",
  [string]$DisplayName = "Scheduled Print Service",
  [string]$Description = "Renders HTML to PDF and prints via configured output mode.",
  [string]$ExePath,
  [ValidateSet('Automatic','Manual','Disabled','AutomaticDelayedStart')]
  [string]$StartupType = 'AutomaticDelayedStart'
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path $ExePath)) {
  throw "ExePath not found: $ExePath"
}

# Service already exists?
$existing = Get-Service -Name $Name -ErrorAction SilentlyContinue
if ($existing) {
  Write-Host "Service '$Name' already exists. Stopping and deleting..." -ForegroundColor Yellow
  try { Stop-Service -Name $Name -Force -ErrorAction SilentlyContinue } catch {}
  sc.exe delete $Name | Out-Null
  Start-Sleep -Seconds 2
}

# Create service
$binPath = '"' + $ExePath + '"'
sc.exe create $Name binPath= $binPath start= auto | Out-Null

# Set display name and description
sc.exe config $Name DisplayName= "$DisplayName" | Out-Null
sc.exe description $Name "$Description" | Out-Null

# Delayed auto start if selected
if ($StartupType -eq 'AutomaticDelayedStart') {
  New-ItemProperty -Path "HKLM:\SYSTEM\CurrentControlSet\Services\$Name" -Name DelayedAutostart -Value 1 -PropertyType DWord -Force | Out-Null
}

# Grant service to interact with desktop? Not required.

# Start service
Start-Service -Name $Name
Get-Service -Name $Name
