@echo off
echo Installing Word Template Extension Native Host...

REM Get the current directory
set SCRIPT_DIR=%~dp0
set SCRIPT_DIR=%SCRIPT_DIR:~0,-1%

REM Check if Python is installed
python --version >nul 2>&1
if errorlevel 1 (
    echo Error: Python is not installed or not in PATH
    echo Please install Python 3.7 or later from https://python.org
    pause
    exit /b 1
)

REM Install Python dependencies
echo Installing Python dependencies...
pip install -r "%SCRIPT_DIR%\requirements.txt"
if errorlevel 1 (
    echo Error: Failed to install Python dependencies
    pause
    exit /b 1
)

REM Create executable using PyInstaller (optional, fallback to Python script)
echo Checking for PyInstaller...
pip show pyinstaller >nul 2>&1
if errorlevel 1 (
    echo PyInstaller not found. Installing...
    pip install pyinstaller
)

REM Create executable
echo Creating executable...
cd /d "%SCRIPT_DIR%"
pyinstaller --onefile --console --name word_updater word_updater.py
if errorlevel 1 (
    echo Warning: Failed to create executable. Will use Python script directly.
    set USE_PYTHON=1
) else (
    echo Executable created successfully.
    copy "dist\word_updater.exe" "word_updater.exe"
    set USE_PYTHON=0
)

REM Update manifest with correct path
echo Updating native messaging manifest...
powershell -ExecutionPolicy Bypass -File "%SCRIPT_DIR%\update-manifest.ps1"
if errorlevel 1 (
    echo Error: Failed to update manifest
    pause
    exit /b 1
)

REM Register native messaging host with Chrome
echo Registering native messaging host with Chrome...
set REG_KEY=HKEY_CURRENT_USER\Software\Google\Chrome\NativeMessagingHosts\com.wordtemplateextension.nativehost
reg add "%REG_KEY%" /ve /t REG_SZ /d "%SCRIPT_DIR%\native-messaging-host-manifest.json" /f

REM Register with Edge (Chromium)
echo Registering native messaging host with Edge...
set EDGE_REG_KEY=HKEY_CURRENT_USER\Software\Microsoft\Edge\NativeMessagingHosts\com.wordtemplateextension.nativehost
reg add "%EDGE_REG_KEY%" /ve /t REG_SZ /d "%SCRIPT_DIR%\native-messaging-host-manifest.json" /f

REM Create default directories
echo Creating default directories...
set USER_DOCS=%USERPROFILE%\Documents
if not exist "%USER_DOCS%\Templates" mkdir "%USER_DOCS%\Templates"
if not exist "%USER_DOCS%\Generated" mkdir "%USER_DOCS%\Generated"

REM Create sample template
echo Creating sample template...
if not exist "%USER_DOCS%\Templates\template.docx" (
    echo Sample template will be created on first run.
)

echo.
echo Installation completed successfully!
echo.
echo Next steps:
echo 1. Update the extension ID in native-messaging-host-manifest.json
echo 2. Place your Word templates in: %USER_DOCS%\Templates
echo 3. Generated documents will be saved to: %USER_DOCS%\Generated
echo 4. Install and test the browser extension
echo.
echo Note: You may need to restart Chrome/Edge for the native host to be recognized.
echo.
pause