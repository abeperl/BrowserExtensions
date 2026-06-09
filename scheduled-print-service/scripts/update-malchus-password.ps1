# Script to update the password for malchus.3plnext.com in the ApiAuth table
# Usage: .\update-malchus-password.ps1

param(
    [Parameter(Mandatory=$false)]
    [string]$DbPath = "..\publish\api_config.db",

    [Parameter(Mandatory=$false)]
    [string]$BaseUrl = "https://malchus.3plnext.com",

    [Parameter(Mandatory=$false)]
    [string]$Username = "abeperl"
)

Write-Host "Updating password for $BaseUrl (username: $Username)" -ForegroundColor Cyan
Write-Host ""

# Prompt for password securely
$securePassword = Read-Host "Enter password for $Username" -AsSecureString
$BSTR = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($securePassword)
$password = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto($BSTR)

if ([string]::IsNullOrWhiteSpace($password)) {
    Write-Host "Error: Password cannot be empty" -ForegroundColor Red
    exit 1
}

# Check if database exists
if (-not (Test-Path $DbPath)) {
    Write-Host "Error: Database not found at $DbPath" -ForegroundColor Red
    exit 1
}

# Update the password
Write-Host "Updating database..." -ForegroundColor Yellow

$query = "UPDATE ApiAuth SET Password = '$password', UpdatedAt = CURRENT_TIMESTAMP WHERE BaseUrl = '$BaseUrl'"

try {
    & sqlite3 $DbPath $query

    # Verify the update
    $result = & sqlite3 $DbPath "SELECT BaseUrl, Username, CASE WHEN Password IS NULL OR Password = '' THEN 'EMPTY' ELSE 'SET' END FROM ApiAuth WHERE BaseUrl = '$BaseUrl'"

    Write-Host ""
    Write-Host "Success! Updated credentials:" -ForegroundColor Green
    Write-Host "  Base URL: $BaseUrl"
    Write-Host "  Username: $Username"
    Write-Host "  Password: SET"
    Write-Host ""
    Write-Host "You can now restart the service for the changes to take effect." -ForegroundColor Cyan
}
catch {
    Write-Host "Error updating database: $_" -ForegroundColor Red
    exit 1
}
finally {
    # Clear password from memory
    [System.Runtime.InteropServices.Marshal]::ZeroFreeBSTR($BSTR)
    $password = $null
}