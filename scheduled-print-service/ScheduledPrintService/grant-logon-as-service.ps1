#Requires -RunAsAdministrator

<#
.SYNOPSIS
    Grants "Log on as a service" permission to a user account
.DESCRIPTION
    Adds the specified user to the "Log on as a service" security policy
#>

param(
    [string]$Username = "abep"
)

Write-Host "=== Grant 'Log on as a service' Permission ===" -ForegroundColor Cyan
Write-Host ""

# Get the user SID
try {
    $user = New-Object System.Security.Principal.NTAccount($Username)
    $sid = $user.Translate([System.Security.Principal.SecurityIdentifier])
    Write-Host "[+] User found: $Username" -ForegroundColor Green
    Write-Host "    SID: $sid" -ForegroundColor Gray
} catch {
    Write-Host "[!] User not found: $Username" -ForegroundColor Red
    Write-Host "    Error: $_" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Granting 'Log on as a service' right to $Username..." -ForegroundColor Yellow

# Export current security policy
$tempPath = "$env:TEMP\secpol.cfg"
$tempPathNew = "$env:TEMP\secpol_new.cfg"

Write-Host "  Exporting current security policy..." -ForegroundColor Gray
secedit /export /cfg $tempPath /quiet

if (-not (Test-Path $tempPath)) {
    Write-Host "[!] Failed to export security policy" -ForegroundColor Red
    exit 1
}

# Read the policy
$content = Get-Content $tempPath

# Find the SeServiceLogonRight line
$found = $false
$newContent = @()

foreach ($line in $content) {
    if ($line -match '^SeServiceLogonRight = (.*)$') {
        $found = $true
        $currentUsers = $matches[1]

        # Check if user already has the right
        if ($currentUsers -match [regex]::Escape($sid.Value)) {
            Write-Host "[+] User already has 'Log on as a service' right" -ForegroundColor Green
            $newContent += $line
        } else {
            # Add the user
            $newLine = "SeServiceLogonRight = $currentUsers,*$($sid.Value)"
            $newContent += $newLine
            Write-Host "[+] Added user to SeServiceLogonRight" -ForegroundColor Green
        }
    } else {
        $newContent += $line
    }
}

# If SeServiceLogonRight doesn't exist, add it
if (-not $found) {
    Write-Host "[+] Creating new SeServiceLogonRight entry" -ForegroundColor Yellow
    $newContent += "SeServiceLogonRight = *$($sid.Value)"
}

# Write new policy
$newContent | Set-Content $tempPathNew -Encoding Unicode

# Import the modified policy
Write-Host "  Importing updated security policy..." -ForegroundColor Gray
secedit /configure /db secedit.sdb /cfg $tempPathNew /quiet

# Clean up
Remove-Item $tempPath -Force -ErrorAction SilentlyContinue
Remove-Item $tempPathNew -Force -ErrorAction SilentlyContinue
Remove-Item "secedit.sdb" -Force -ErrorAction SilentlyContinue

Write-Host ""
Write-Host "[+] Permission granted successfully!" -ForegroundColor Green
Write-Host ""
Write-Host "You can verify the permission in:" -ForegroundColor Yellow
Write-Host "  Local Security Policy > Local Policies > User Rights Assignment > Log on as a service" -ForegroundColor Gray
Write-Host ""
Write-Host "Or run: secpol.msc" -ForegroundColor Gray
Write-Host ""
