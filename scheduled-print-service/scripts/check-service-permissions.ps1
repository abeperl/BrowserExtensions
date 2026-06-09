#Requires -RunAsAdministrator

<#
.SYNOPSIS
    Diagnoses permission issues for ScheduledPrintService Windows service
.DESCRIPTION
    Checks service account permissions on logs folder, exe folder, and database file
#>

param(
    [string]$ServiceName = "ScheduledPrintService",
    [string]$LogFolder = "E:\Share\server\servern\Software\ScheduledPrintService\logs",
    [string]$ExeFolder = "C:\Program Files\Malchut\ScheduledPrintService"
)

Write-Host "=== ScheduledPrintService Permission Checker ===" -ForegroundColor Cyan
Write-Host ""

# Function to test folder permissions
function Test-FolderPermissions {
    param(
        [string]$Path,
        [string]$Account,
        [string]$Description
    )

    Write-Host "Checking: $Description" -ForegroundColor Yellow
    Write-Host "Path: $Path" -ForegroundColor Gray

    if (-not (Test-Path $Path)) {
        Write-Host "  [!] Folder does NOT exist!" -ForegroundColor Red
        Write-Host "  Creating folder..." -ForegroundColor Yellow
        try {
            New-Item -Path $Path -ItemType Directory -Force | Out-Null
            Write-Host "  [+] Folder created successfully" -ForegroundColor Green
        } catch {
            Write-Host "  [!] Failed to create folder: $_" -ForegroundColor Red
            return
        }
    } else {
        Write-Host "  [+] Folder exists" -ForegroundColor Green
    }

    # Get current ACL
    $acl = Get-Acl -Path $Path

    # Check if account has permissions
    $accountAccess = $acl.Access | Where-Object { $_.IdentityReference -like "*$Account*" }

    if ($accountAccess) {
        Write-Host "  [+] Account has explicit permissions:" -ForegroundColor Green
        foreach ($access in $accountAccess) {
            Write-Host "      - $($access.FileSystemRights) ($($access.AccessControlType))" -ForegroundColor Gray
        }
    } else {
        Write-Host "  [!] No explicit permissions found for $Account" -ForegroundColor Yellow
        Write-Host "      (May inherit from parent)" -ForegroundColor Gray
    }

    # Test write access
    $testFile = Join-Path $Path "permission_test_$(Get-Date -Format 'yyyyMMdd_HHmmss').tmp"
    try {
        # This tests as the current user, not the service account
        "test" | Out-File -FilePath $testFile -Force
        Remove-Item $testFile -Force
        Write-Host "  [+] Write test PASSED (as current user)" -ForegroundColor Green
    } catch {
        Write-Host "  [!] Write test FAILED: $_" -ForegroundColor Red
    }

    Write-Host ""
}

# Function to show all permissions
function Show-DetailedPermissions {
    param(
        [string]$Path,
        [string]$Description
    )

    Write-Host "Detailed Permissions: $Description" -ForegroundColor Cyan
    Write-Host "Path: $Path" -ForegroundColor Gray

    if (-not (Test-Path $Path)) {
        Write-Host "  [!] Path does not exist" -ForegroundColor Red
        Write-Host ""
        return
    }

    $acl = Get-Acl -Path $Path
    Write-Host "  Owner: $($acl.Owner)" -ForegroundColor Gray
    Write-Host "  Permissions:" -ForegroundColor Gray

    foreach ($access in $acl.Access) {
        $color = if ($access.AccessControlType -eq "Allow") { "Green" } else { "Red" }
        Write-Host "    $($access.IdentityReference)" -ForegroundColor White
        Write-Host "      Rights: $($access.FileSystemRights)" -ForegroundColor $color
        Write-Host "      Type: $($access.AccessControlType)" -ForegroundColor $color
        Write-Host "      Inherited: $($access.IsInherited)" -ForegroundColor Gray
    }
    Write-Host ""
}

# Check if service exists
Write-Host "Step 1: Checking Service Configuration" -ForegroundColor Cyan
Write-Host "---------------------------------------" -ForegroundColor Gray

$service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue

if (-not $service) {
    Write-Host "[!] Service '$ServiceName' not found!" -ForegroundColor Red
    Write-Host "Please install the service first." -ForegroundColor Yellow
    exit 1
}

Write-Host "[+] Service found" -ForegroundColor Green
Write-Host "    Status: $($service.Status)" -ForegroundColor Gray

# Get service account using WMI
$serviceWmi = Get-WmiObject Win32_Service | Where-Object { $_.Name -eq $ServiceName }
$serviceAccount = $serviceWmi.StartName

Write-Host "    Account: $serviceAccount" -ForegroundColor $(if ($serviceAccount -eq "LocalSystem") { "Yellow" } else { "Green" })
Write-Host "    Start Mode: $($serviceWmi.StartMode)" -ForegroundColor Gray
Write-Host "    Executable: $($serviceWmi.PathName)" -ForegroundColor Gray
Write-Host ""

if ($serviceAccount -eq "LocalSystem") {
    Write-Host "[!] WARNING: Service is running as LocalSystem" -ForegroundColor Yellow
    Write-Host "    This should have full access, but may have network share issues" -ForegroundColor Yellow
    Write-Host ""
}

# Normalize account name
$accountToCheck = $serviceAccount
if ($accountToCheck -eq "LocalSystem") {
    $accountToCheck = "NT AUTHORITY\SYSTEM"
}

Write-Host "Step 2: Checking Folder Permissions" -ForegroundColor Cyan
Write-Host "------------------------------------" -ForegroundColor Gray
Write-Host ""

# Check Log Folder
Test-FolderPermissions -Path $LogFolder -Account $accountToCheck -Description "Log Folder"

# Check Exe Folder
Test-FolderPermissions -Path $ExeFolder -Account $accountToCheck -Description "Exe/DB Folder"

# Check specific database file
$dbFile = Join-Path $ExeFolder "api_config.db"
if (Test-Path $dbFile) {
    Write-Host "Checking: Database File" -ForegroundColor Yellow
    Write-Host "Path: $dbFile" -ForegroundColor Gray
    $dbAcl = Get-Acl -Path $dbFile
    $dbAccess = $dbAcl.Access | Where-Object { $_.IdentityReference -like "*$accountToCheck*" }

    if ($dbAccess) {
        Write-Host "  [+] Account has database file permissions" -ForegroundColor Green
        foreach ($access in $dbAccess) {
            Write-Host "      - $($access.FileSystemRights) ($($access.AccessControlType))" -ForegroundColor Gray
        }
    } else {
        Write-Host "  [!] No explicit permissions on database file" -ForegroundColor Yellow
    }
    Write-Host ""
}

Write-Host "Step 3: Detailed Permission Listings" -ForegroundColor Cyan
Write-Host "-------------------------------------" -ForegroundColor Gray
Write-Host ""

Show-DetailedPermissions -Path $LogFolder -Description "Log Folder"
Show-DetailedPermissions -Path $ExeFolder -Description "Exe/DB Folder"

# Check if log folder is on network share
if ($LogFolder -like "E:\*" -or $LogFolder -like "\\*") {
    Write-Host "Step 4: Network Share Detection" -ForegroundColor Cyan
    Write-Host "--------------------------------" -ForegroundColor Gray
    Write-Host "[!] WARNING: Log folder appears to be on a network share (E: drive or UNC path)" -ForegroundColor Yellow
    Write-Host "    Network shares can cause issues with Windows services" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "    Recommendations:" -ForegroundColor Yellow
    Write-Host "    1. Use a local path like C:\ProgramData\ScheduledPrintService\logs" -ForegroundColor White
    Write-Host "    2. If network share is required, run service as domain account with share access" -ForegroundColor White
    Write-Host "    3. Ensure E: is a mapped drive accessible to the service account" -ForegroundColor White
    Write-Host ""
}

Write-Host "Step 5: Recommendations" -ForegroundColor Cyan
Write-Host "-----------------------" -ForegroundColor Gray
Write-Host ""

if ($serviceAccount -eq "LocalSystem") {
    Write-Host "[!] Network Share Issue Likely" -ForegroundColor Yellow
    Write-Host "    LocalSystem cannot access network shares or mapped drives" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "    Solution Options:" -ForegroundColor White
    Write-Host "    1. Move logs to local path: C:\ProgramData\ScheduledPrintService\logs" -ForegroundColor Green
    Write-Host "    2. Change service account to domain user with network access" -ForegroundColor Green
    Write-Host "       Run: sc.exe config $ServiceName obj= DOMAIN\Username password= PASSWORD" -ForegroundColor Gray
    Write-Host ""
}

Write-Host "Step 6: Grant Permissions (if needed)" -ForegroundColor Cyan
Write-Host "--------------------------------------" -ForegroundColor Gray
Write-Host ""
Write-Host "To grant full permissions to service account:" -ForegroundColor Yellow
Write-Host ""
Write-Host "For Log Folder:" -ForegroundColor White
Write-Host "  icacls `"$LogFolder`" /grant `"${accountToCheck}:(OI)(CI)F`" /T" -ForegroundColor Gray
Write-Host ""
Write-Host "For Exe/DB Folder:" -ForegroundColor White
Write-Host "  icacls `"$ExeFolder`" /grant `"${accountToCheck}:(OI)(CI)F`" /T" -ForegroundColor Gray
Write-Host ""

# Offer to fix permissions
Write-Host "Would you like to automatically grant permissions? (Y/N)" -ForegroundColor Yellow
$response = Read-Host

if ($response -eq "Y" -or $response -eq "y") {
    Write-Host ""
    Write-Host "Granting permissions..." -ForegroundColor Cyan

    try {
        # Grant permissions to log folder
        Write-Host "  Granting permissions to log folder..." -ForegroundColor Gray
        icacls "$LogFolder" /grant "${accountToCheck}:(OI)(CI)F" /T
        Write-Host "  [+] Log folder permissions granted" -ForegroundColor Green

        # Grant permissions to exe folder
        Write-Host "  Granting permissions to exe/db folder..." -ForegroundColor Gray
        icacls "$ExeFolder" /grant "${accountToCheck}:(OI)(CI)F" /T
        Write-Host "  [+] Exe/DB folder permissions granted" -ForegroundColor Green

        Write-Host ""
        Write-Host "[+] Permissions granted successfully!" -ForegroundColor Green
        Write-Host "    Restart the service for changes to take effect:" -ForegroundColor Yellow
        Write-Host "    Restart-Service -Name $ServiceName" -ForegroundColor Gray

    } catch {
        Write-Host "[!] Error granting permissions: $_" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "=== Permission Check Complete ===" -ForegroundColor Cyan
