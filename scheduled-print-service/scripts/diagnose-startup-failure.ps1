#Requires -RunAsAdministrator

<#
.SYNOPSIS
    Diagnoses why ScheduledPrintService fails to start
.DESCRIPTION
    Checks Event Viewer, validates config files, and tests manual execution
#>

param(
    [string]$ServiceName = "ScheduledPrintService",
    [string]$ExeFolder = "C:\Program Files\Malchut\ScheduledPrintService"
)

Write-Host "=== Diagnosing Service Startup Failure ===" -ForegroundColor Cyan
Write-Host ""

# Check Event Viewer for recent errors
Write-Host "Step 1: Checking Event Viewer" -ForegroundColor Cyan
Write-Host "------------------------------" -ForegroundColor Gray

try {
    # Check Application log for service-related errors
    $recentErrors = Get-EventLog -LogName Application -After (Get-Date).AddHours(-1) -EntryType Error -ErrorAction SilentlyContinue |
        Where-Object { $_.Source -like "*$ServiceName*" -or $_.Message -like "*$ServiceName*" } |
        Select-Object -First 5

    if ($recentErrors) {
        Write-Host "[!] Found recent errors:" -ForegroundColor Red
        foreach ($error in $recentErrors) {
            Write-Host ""
            Write-Host "Time: $($error.TimeGenerated)" -ForegroundColor Yellow
            Write-Host "Source: $($error.Source)" -ForegroundColor Gray
            Write-Host "Message:" -ForegroundColor Gray
            Write-Host $error.Message -ForegroundColor White
            Write-Host ""
        }
    } else {
        Write-Host "[i] No recent errors in Application log" -ForegroundColor Yellow
    }

    # Check System log
    $systemErrors = Get-EventLog -LogName System -After (Get-Date).AddHours(-1) -EntryType Error -ErrorAction SilentlyContinue |
        Where-Object { $_.Message -like "*$ServiceName*" } |
        Select-Object -First 3

    if ($systemErrors) {
        Write-Host "[!] Found system log errors:" -ForegroundColor Red
        foreach ($error in $systemErrors) {
            Write-Host ""
            Write-Host "Time: $($error.TimeGenerated)" -ForegroundColor Yellow
            Write-Host "Message:" -ForegroundColor Gray
            Write-Host $error.Message -ForegroundColor White
        }
    }

} catch {
    Write-Host "[!] Could not read Event Viewer: $_" -ForegroundColor Red
}

Write-Host ""

# Check appsettings.json validity
Write-Host "Step 2: Validating appsettings.json" -ForegroundColor Cyan
Write-Host "------------------------------------" -ForegroundColor Gray

$appsettingsPath = Join-Path $ExeFolder "appsettings.json"

if (Test-Path $appsettingsPath) {
    Write-Host "[+] File exists: $appsettingsPath" -ForegroundColor Green

    try {
        $json = Get-Content $appsettingsPath -Raw | ConvertFrom-Json
        Write-Host "[+] JSON is valid" -ForegroundColor Green

        # Display current log path
        if ($json.Serilog.WriteTo) {
            foreach ($sink in $json.Serilog.WriteTo) {
                if ($sink.Name -eq "File" -and $sink.Args.path) {
                    Write-Host "    Current log path: $($sink.Args.path)" -ForegroundColor Gray
                }
            }
        }

        # Display DataRootPath if exists
        if ($json.AppSettings -and $json.AppSettings.DataRootPath) {
            Write-Host "    DataRootPath: $($json.AppSettings.DataRootPath)" -ForegroundColor Gray
        }

        Write-Host ""
        Write-Host "Full appsettings.json content:" -ForegroundColor Yellow
        Write-Host "------------------------------" -ForegroundColor Gray
        Get-Content $appsettingsPath | Write-Host -ForegroundColor White
        Write-Host "------------------------------" -ForegroundColor Gray

    } catch {
        Write-Host "[!] JSON is INVALID: $_" -ForegroundColor Red
        Write-Host "    This is likely why the service won't start" -ForegroundColor Yellow
        Write-Host ""
        Write-Host "Current file content:" -ForegroundColor Yellow
        Get-Content $appsettingsPath | Write-Host -ForegroundColor Gray
    }
} else {
    Write-Host "[!] appsettings.json NOT FOUND" -ForegroundColor Red
}

Write-Host ""

# Check executable and dependencies
Write-Host "Step 3: Checking Executable" -ForegroundColor Cyan
Write-Host "---------------------------" -ForegroundColor Gray

$exePath = Join-Path $ExeFolder "ScheduledPrintService.exe"

if (Test-Path $exePath) {
    Write-Host "[+] Executable exists: $exePath" -ForegroundColor Green

    $exeInfo = Get-Item $exePath
    Write-Host "    Size: $($exeInfo.Length) bytes" -ForegroundColor Gray
    Write-Host "    Modified: $($exeInfo.LastWriteTime)" -ForegroundColor Gray
} else {
    Write-Host "[!] Executable NOT FOUND" -ForegroundColor Red
}

# Check for required DLLs
Write-Host ""
Write-Host "Required files:" -ForegroundColor Yellow
$requiredFiles = @(
    "ScheduledPrintService.dll",
    "ScheduledPrintService.runtimeconfig.json",
    "api_config.db"
)

foreach ($file in $requiredFiles) {
    $filePath = Join-Path $ExeFolder $file
    if (Test-Path $filePath) {
        Write-Host "  [+] $file" -ForegroundColor Green
    } else {
        Write-Host "  [!] $file (MISSING)" -ForegroundColor Red
    }
}

Write-Host ""

# Try to run executable manually
Write-Host "Step 4: Testing Manual Execution" -ForegroundColor Cyan
Write-Host "---------------------------------" -ForegroundColor Gray
Write-Host "Attempting to run service manually for 5 seconds..." -ForegroundColor Yellow
Write-Host ""

try {
    $process = Start-Process -FilePath $exePath -WorkingDirectory $ExeFolder -NoNewWindow -PassThru -RedirectStandardError "$env:TEMP\service_error.txt" -RedirectStandardOutput "$env:TEMP\service_output.txt"

    Start-Sleep -Seconds 5

    if (-not $process.HasExited) {
        Write-Host "[+] Service is running manually!" -ForegroundColor Green
        Write-Host "    This means the issue is service-specific, not the application" -ForegroundColor Yellow
        $process.Kill()
        Start-Sleep -Seconds 1
    } else {
        Write-Host "[!] Service exited immediately with code: $($process.ExitCode)" -ForegroundColor Red
    }

    # Show output
    if (Test-Path "$env:TEMP\service_output.txt") {
        $output = Get-Content "$env:TEMP\service_output.txt" -Raw
        if ($output) {
            Write-Host ""
            Write-Host "Standard Output:" -ForegroundColor Yellow
            Write-Host $output -ForegroundColor White
        }
    }

    if (Test-Path "$env:TEMP\service_error.txt") {
        $errors = Get-Content "$env:TEMP\service_error.txt" -Raw
        if ($errors) {
            Write-Host ""
            Write-Host "Standard Error:" -ForegroundColor Red
            Write-Host $errors -ForegroundColor White
        }
    }

} catch {
    Write-Host "[!] Failed to run manually: $_" -ForegroundColor Red
}

Write-Host ""

# Check service configuration
Write-Host "Step 5: Service Configuration" -ForegroundColor Cyan
Write-Host "-----------------------------" -ForegroundColor Gray

$serviceWmi = Get-WmiObject Win32_Service | Where-Object { $_.Name -eq $ServiceName }

if ($serviceWmi) {
    Write-Host "Service Account: $($serviceWmi.StartName)" -ForegroundColor Gray
    Write-Host "Start Mode: $($serviceWmi.StartMode)" -ForegroundColor Gray
    Write-Host "Path: $($serviceWmi.PathName)" -ForegroundColor Gray
    Write-Host "State: $($serviceWmi.State)" -ForegroundColor Gray
    Write-Host ""

    # Verify path matches
    if ($serviceWmi.PathName -notlike "*$exePath*") {
        Write-Host "[!] WARNING: Service path doesn't match expected executable" -ForegroundColor Red
        Write-Host "    Expected: $exePath" -ForegroundColor Yellow
        Write-Host "    Actual: $($serviceWmi.PathName)" -ForegroundColor Yellow
    }
}

Write-Host ""

# Recommendations
Write-Host "=== RECOMMENDATIONS ===" -ForegroundColor Cyan
Write-Host ""

Write-Host "1. Check Event Viewer errors above for specific failure reason" -ForegroundColor White
Write-Host ""
Write-Host "2. If appsettings.json is corrupted, restore from backup:" -ForegroundColor White
Write-Host "   Get-Content '$appsettingsPath.backup' | Set-Content '$appsettingsPath'" -ForegroundColor Gray
Write-Host ""
Write-Host "3. Try reinstalling the service:" -ForegroundColor White
Write-Host "   .\uninstall-service.ps1" -ForegroundColor Gray
Write-Host "   .\install-service.ps1" -ForegroundColor Gray
Write-Host ""
Write-Host "4. Check database file permissions and location" -ForegroundColor White
Write-Host ""

Write-Host "=== Diagnostic Complete ===" -ForegroundColor Cyan
