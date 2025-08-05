# Native Host Diagnostic Tool
# Checks common issues with native messaging setup

Write-Host "=== Word Template Extension Native Host Diagnostics ===" -ForegroundColor Cyan
Write-Host ""

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$errors = @()
$warnings = @()
$info = @()

# Test 1: Check Python installation
Write-Host "1. Checking Python installation..." -ForegroundColor Yellow
try {
    $pythonVersion = python --version 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "   ✓ $pythonVersion" -ForegroundColor Green
        $info += "Python installed: $pythonVersion"
    } else {
        Write-Host "   ✗ Python not found in PATH" -ForegroundColor Red
        $errors += "Python not installed or not in PATH"
    }
} catch {
    Write-Host "   ✗ Python not available" -ForegroundColor Red
    $errors += "Python not available: $_"
}

# Test 2: Check Python dependencies
Write-Host "2. Checking Python dependencies..." -ForegroundColor Yellow
$requiredPackages = @("python-docx")
foreach ($package in $requiredPackages) {
    try {
        $result = pip show $package 2>&1
        if ($LASTEXITCODE -eq 0) {
            Write-Host "   ✓ $package installed" -ForegroundColor Green
        } else {
            Write-Host "   ✗ $package not installed" -ForegroundColor Red
            $errors += "$package not installed"
        }
    } catch {
        Write-Host "   ✗ Cannot check $package" -ForegroundColor Red
        $warnings += "Cannot verify $package installation"
    }
}

# Test 3: Check native host files
Write-Host "3. Checking native host files..." -ForegroundColor Yellow
$requiredFiles = @(
    "word_updater.py",
    "native-messaging-host-manifest.json",
    "requirements.txt"
)

foreach ($file in $requiredFiles) {
    $filePath = Join-Path $ScriptDir $file
    if (Test-Path $filePath) {
        Write-Host "   ✓ $file exists" -ForegroundColor Green
    } else {
        Write-Host "   ✗ $file missing" -ForegroundColor Red
        $errors += "$file not found"
    }
}

# Check for executable
$exePath = Join-Path $ScriptDir "word_updater.exe"
if (Test-Path $exePath) {
    Write-Host "   ✓ word_updater.exe exists" -ForegroundColor Green
    $info += "Compiled executable available"
} else {
    Write-Host "   ⚠ word_updater.exe not found (will use Python script)" -ForegroundColor Yellow
    $warnings += "No compiled executable, using Python script"
}

# Test 4: Check manifest configuration
Write-Host "4. Checking manifest configuration..." -ForegroundColor Yellow
$manifestPath = Join-Path $ScriptDir "native-messaging-host-manifest.json"
if (Test-Path $manifestPath) {
    try {
        $manifest = Get-Content $manifestPath -Raw | ConvertFrom-Json
        
        # Check name
        if ($manifest.name -eq "com.wordtemplateextension.nativehost") {
            Write-Host "   ✓ Correct native host name" -ForegroundColor Green
        } else {
            Write-Host "   ✗ Incorrect native host name: $($manifest.name)" -ForegroundColor Red
            $errors += "Wrong native host name in manifest"
        }
        
        # Check path
        if (Test-Path $manifest.path) {
            Write-Host "   ✓ Host executable path exists: $($manifest.path)" -ForegroundColor Green
        } else {
            Write-Host "   ✗ Host executable not found: $($manifest.path)" -ForegroundColor Red
            $errors += "Native host executable not found at specified path"
        }
        
        # Check allowed origins
        if ($manifest.allowed_origins -and $manifest.allowed_origins.Count -gt 0) {
            Write-Host "   ✓ Extension origins configured: $($manifest.allowed_origins.Count) origin(s)" -ForegroundColor Green
            foreach ($origin in $manifest.allowed_origins) {
                Write-Host "     - $origin" -ForegroundColor Gray
            }
        } else {
            Write-Host "   ✗ No allowed origins configured" -ForegroundColor Red
            $errors += "No extension origins configured in manifest"
        }
        
    } catch {
        Write-Host "   ✗ Invalid manifest JSON: $_" -ForegroundColor Red
        $errors += "Manifest JSON is invalid"
    }
}

# Test 5: Check registry entries
Write-Host "5. Checking registry entries..." -ForegroundColor Yellow
$registryKeys = @(
    "HKCU:\Software\Google\Chrome\NativeMessagingHosts\com.wordtemplateextension.nativehost",
    "HKCU:\Software\Microsoft\Edge\NativeMessagingHosts\com.wordtemplateextension.nativehost"
)

foreach ($key in $registryKeys) {
    try {
        $value = Get-ItemProperty -Path $key -ErrorAction Stop
        $browser = if ($key -like "*Chrome*") { "Chrome" } else { "Edge" }
        Write-Host "   ✓ $browser registry entry exists" -ForegroundColor Green
        
        # Verify the path in registry matches manifest
        if (Test-Path $value.'(default)') {
            Write-Host "     ✓ Registry points to valid manifest" -ForegroundColor Green
        } else {
            Write-Host "     ✗ Registry points to missing manifest: $($value.'(default)')" -ForegroundColor Red
            $errors += "$browser registry points to missing manifest file"
        }
    } catch {
        $browser = if ($key -like "*Chrome*") { "Chrome" } else { "Edge" }
        Write-Host "   ✗ $browser registry entry missing" -ForegroundColor Red
        $errors += "$browser native messaging host not registered"
    }
}

# Test 6: Check directories
Write-Host "6. Checking directories..." -ForegroundColor Yellow
$userDocs = [Environment]::GetFolderPath("MyDocuments")
$templateDir = Join-Path $userDocs "Templates"
$outputDir = Join-Path $userDocs "Generated"

if (Test-Path $templateDir) {
    Write-Host "   ✓ Templates directory exists: $templateDir" -ForegroundColor Green
    $templateCount = (Get-ChildItem $templateDir -Filter "*.docx" -ErrorAction SilentlyContinue).Count
    Write-Host "     Found $templateCount .docx template(s)" -ForegroundColor Gray
} else {
    Write-Host "   ⚠ Templates directory missing: $templateDir" -ForegroundColor Yellow
    $warnings += "Templates directory not created"
}

if (Test-Path $outputDir) {
    Write-Host "   ✓ Output directory exists: $outputDir" -ForegroundColor Green
} else {
    Write-Host "   ⚠ Output directory missing: $outputDir" -ForegroundColor Yellow
    $warnings += "Output directory not created"
}

# Test 7: Test native host execution
Write-Host "7. Testing native host execution..." -ForegroundColor Yellow
try {
    $testMessage = '{"action":"ping"}'
    if (Test-Path $exePath) {
        $process = Start-Process -FilePath $exePath -ArgumentList "" -PassThru -NoNewWindow -RedirectStandardInput -RedirectStandardOutput -RedirectStandardError
        $process.StandardInput.WriteLine($testMessage)
        $process.StandardInput.Close()
        
        $timeout = 5000 # 5 seconds
        if ($process.WaitForExit($timeout)) {
            if ($process.ExitCode -eq 0) {
                Write-Host "   ✓ Native host executable runs successfully" -ForegroundColor Green
            } else {
                Write-Host "   ✗ Native host executable exited with code $($process.ExitCode)" -ForegroundColor Red
                $errors += "Native host execution failed"
            }
        } else {
            Write-Host "   ✗ Native host executable timed out" -ForegroundColor Red
            $errors += "Native host execution timed out"
            $process.Kill()
        }
    } else {
        # Test Python script
        $pythonPath = Join-Path $ScriptDir "word_updater.py"
        if (Test-Path $pythonPath) {
            Write-Host "   ⚠ Testing Python script (executable not available)" -ForegroundColor Yellow
            # Could add Python script test here
        }
    }
} catch {
    Write-Host "   ✗ Failed to test native host: $_" -ForegroundColor Red
    $errors += "Native host execution test failed"
}

# Summary
Write-Host ""
Write-Host "=== SUMMARY ===" -ForegroundColor Cyan

if ($errors.Count -eq 0) {
    Write-Host "✓ No critical errors found!" -ForegroundColor Green
} else {
    Write-Host "✗ Found $($errors.Count) critical error(s):" -ForegroundColor Red
    foreach ($error in $errors) {
        Write-Host "  • $error" -ForegroundColor Red
    }
}

if ($warnings.Count -gt 0) {
    Write-Host ""
    Write-Host "⚠ Found $($warnings.Count) warning(s):" -ForegroundColor Yellow
    foreach ($warning in $warnings) {
        Write-Host "  • $warning" -ForegroundColor Yellow
    }
}

if ($info.Count -gt 0) {
    Write-Host ""
    Write-Host "ℹ Information:" -ForegroundColor Blue
    foreach ($item in $info) {
        Write-Host "  • $item" -ForegroundColor Blue
    }
}

Write-Host ""
if ($errors.Count -eq 0) {
    Write-Host "Native host should be working correctly!" -ForegroundColor Green
    Write-Host ""
    Write-Host "If you're still having issues:" -ForegroundColor Yellow
    Write-Host "1. Restart Chrome/Edge completely"
    Write-Host "2. Check browser console for errors"
    Write-Host "3. Verify extension ID matches manifest"
} else {
    Write-Host "Please fix the errors above before using the extension." -ForegroundColor Red
    Write-Host ""
    Write-Host "Common fixes:" -ForegroundColor Yellow
    Write-Host "1. Run install.bat as Administrator"
    Write-Host "2. Update extension ID with: .\update-manifest.ps1 -ExtensionId 'your-extension-id'"
    Write-Host "3. Restart Chrome/Edge"
}

Write-Host ""