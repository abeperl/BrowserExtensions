# Reset to default ProgramData data root behavior
$ErrorActionPreference = 'Stop'
Remove-Item Env:\SCHEDULED_PRINT_DATA_ROOT -ErrorAction SilentlyContinue
Write-Host "SCHEDULED_PRINT_DATA_ROOT cleared. Service will use %ProgramData%\\ScheduledPrintService." -ForegroundColor Green
