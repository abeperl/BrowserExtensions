#Requires -RunAsAdministrator

<#
.SYNOPSIS
    Complete fix for ScheduledPrintService startup issues
.DESCRIPTION
    1. Fixes DataRoot path to use local directory
    2. Grants "Log on as a service" permission to service account
    3. Starts the service
#>

param(
    [string]$ServiceName = "ScheduledPrintService",
    [string]$ExeFolder = "C:\Program Files\Malchut\ScheduledPrintService",
    [string]$NewDataRoot = "C:\ProgramData\ScheduledPrintService"
)

Write-Host "=== Complete Service Fix ===" -ForegroundColor Cyan
Write-Host ""

# Get service account
$serviceWmi = Get-WmiObject Win32_Service | Where-Object { $_.Name -eq $ServiceName }
if (-not $serviceWmi) {
    Write-Host "[!] Service not found: $ServiceName" -ForegroundColor Red
    exit 1
}

$serviceAccount = $serviceWmi.StartName
Write-Host "Service Account: $serviceAccount" -ForegroundColor Gray
Write-Host ""

# ===========================================
# STEP 1: Fix DataRoot Path
# ===========================================
Write-Host "Step 1: Fixing DataRoot Path" -ForegroundColor Cyan
Write-Host "----------------------------" -ForegroundColor Gray

# Stop service if running
$service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if ($service -and $service.Status -eq "Running") {
    Write-Host "  Stopping service..." -ForegroundColor Yellow
    Stop-Service -Name $ServiceName -Force
    Start-Sleep -Seconds 2
}

# Create folders
Write-Host "  Creating local data root: $NewDataRoot" -ForegroundColor Yellow
if (-not (Test-Path $NewDataRoot)) {
    New-Item -Path $NewDataRoot -ItemType Directory -Force | Out-Null
}

$logsFolder = Join-Path $NewDataRoot "logs"
if (-not (Test-Path $logsFolder)) {
    New-Item -Path $logsFolder -ItemType Directory -Force | Out-Null
}
Write-Host "  [+] Folders created" -ForegroundColor Green

# Grant permissions
Write-Host "  Granting permissions..." -ForegroundColor Yellow
if ($serviceAccount -eq "LocalSystem") {
    icacls "$NewDataRoot" /grant "NT AUTHORITY\SYSTEM:(OI)(CI)F" /T | Out-Null
} elseif ($serviceAccount -like ".\*") {
    $localUser = $serviceAccount -replace '^\.\\'
    icacls "$NewDataRoot" /grant "${env:COMPUTERNAME}\${localUser}:(OI)(CI)F" /T | Out-Null
} else {
    icacls "$NewDataRoot" /grant "${serviceAccount}:(OI)(CI)F" /T | Out-Null
}
Write-Host "  [+] Permissions granted" -ForegroundColor Green

# Update appsettings.json
$appsettingsPath = Join-Path $ExeFolder "appsettings.json"
Write-Host "  Updating appsettings.json..." -ForegroundColor Yellow

Copy-Item $appsettingsPath "$appsettingsPath.backup" -Force

try {
    $json = Get-Content $appsettingsPath -Raw | ConvertFrom-Json
    $oldDataRoot = $json.DataRoot
    $json.DataRoot = $NewDataRoot
    $json | ConvertTo-Json -Depth 10 | Set-Content $appsettingsPath -Force

    Write-Host "  [+] Changed: $oldDataRoot" -ForegroundColor Gray
    Write-Host "  [+] To:      $NewDataRoot" -ForegroundColor Green
} catch {
    Write-Host "  [!] Error: $_" -ForegroundColor Red
    Copy-Item "$appsettingsPath.backup" $appsettingsPath -Force
    exit 1
}

Write-Host ""

# ===========================================
# STEP 2: Grant "Log on as a service"
# ===========================================
Write-Host "Step 2: Granting 'Log on as a service' Permission" -ForegroundColor Cyan
Write-Host "--------------------------------------------------" -ForegroundColor Gray

if ($serviceAccount -eq "LocalSystem") {
    Write-Host "  [i] LocalSystem already has this right" -ForegroundColor Green
} else {
    # Extract username
    $username = $serviceAccount -replace '^\.\\', ''

    Write-Host "  Getting user SID for: $username" -ForegroundColor Yellow

    try {
        $user = New-Object System.Security.Principal.NTAccount($username)
        $sid = $user.Translate([System.Security.Principal.SecurityIdentifier])
        Write-Host "  [+] User found: $username (SID: $sid)" -ForegroundColor Green

        # Export/modify/import security policy
        $tempPath = "$env:TEMP\secpol.cfg"
        $tempPathNew = "$env:TEMP\secpol_new.cfg"

        Write-Host "  Updating security policy..." -ForegroundColor Yellow
        secedit /export /cfg $tempPath /quiet

        $content = Get-Content $tempPath
        $found = $false
        $newContent = @()

        foreach ($line in $content) {
            if ($line -match '^SeServiceLogonRight = (.*)$') {
                $found = $true
                $currentUsers = $matches[1]
                if ($currentUsers -match [regex]::Escape($sid.Value)) {
                    Write-Host "  [+] User already has the right" -ForegroundColor Green
                    $newContent += $line
                } else {
                    $newLine = "SeServiceLogonRight = $currentUsers,*$($sid.Value)"
                    $newContent += $newLine
                    Write-Host "  [+] Added user to SeServiceLogonRight" -ForegroundColor Green
                }
            } else {
                $newContent += $line
            }
        }

        if (-not $found) {
            $newContent += "SeServiceLogonRight = *$($sid.Value)"
            Write-Host "  [+] Created SeServiceLogonRight entry" -ForegroundColor Green
        }

        $newContent | Set-Content $tempPathNew -Encoding Unicode
        secedit /configure /db secedit.sdb /cfg $tempPathNew /quiet

        Remove-Item $tempPath -Force -ErrorAction SilentlyContinue
        Remove-Item $tempPathNew -Force -ErrorAction SilentlyContinue
        Remove-Item "secedit.sdb" -Force -ErrorAction SilentlyContinue

        Write-Host "  [+] Permission granted!" -ForegroundColor Green

    } catch {
        Write-Host "  [!] Error: $_" -ForegroundColor Red
        Write-Host "  You may need to grant this permission manually:" -ForegroundColor Yellow
        Write-Host "    1. Run: secpol.msc" -ForegroundColor Gray
        Write-Host "    2. Go to: Local Policies > User Rights Assignment" -ForegroundColor Gray
        Write-Host "    3. Open: Log on as a service" -ForegroundColor Gray
        Write-Host "    4. Add: $username" -ForegroundColor Gray
    }
}

Write-Host ""

# ===========================================
# STEP 3: Start Service
# ===========================================
Write-Host "Step 3: Starting Service" -ForegroundColor Cyan
Write-Host "------------------------" -ForegroundColor Gray

Write-Host "  Starting $ServiceName..." -ForegroundColor Yellow
Start-Service -Name $ServiceName -ErrorAction SilentlyContinue
Start-Sleep -Seconds 3

$service = Get-Service -Name $ServiceName

if ($service.Status -eq "Running") {
    Write-Host "  [+] Service started successfully!" -ForegroundColor Green
    Write-Host ""
    Write-Host "=== SUCCESS ===" -ForegroundColor Green
    Write-Host ""
    Write-Host "Service is now running!" -ForegroundColor White
    Write-Host "Logs location: $logsFolder" -ForegroundColor Gray
    Write-Host ""
    Write-Host "View logs:" -ForegroundColor Yellow
    Write-Host "  Get-Content '$logsFolder\*.log' -Tail 50 -Wait" -ForegroundColor Gray
} else {
    Write-Host "  [!] Service failed to start. Status: $($service.Status)" -ForegroundColor Red
    Write-Host ""
    Write-Host "Check for errors:" -ForegroundColor Yellow
    Write-Host "  .\diagnose-startup-failure.ps1" -ForegroundColor Gray
}

Write-Host ""
