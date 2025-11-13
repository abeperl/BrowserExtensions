param(
  [string]$Name = "ScheduledPrintService"
)

$ErrorActionPreference = 'Stop'

$svc = Get-Service -Name $Name -ErrorAction SilentlyContinue
if (-not $svc) {
  Write-Host "Service '$Name' not found." -ForegroundColor Yellow
  return
}

try { Stop-Service -Name $Name -Force -ErrorAction SilentlyContinue } catch {}
sc.exe delete $Name | Out-Null
Write-Host "Service '$Name' deleted." -ForegroundColor Green
