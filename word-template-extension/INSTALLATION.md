# Installation Guide

**Complete setup instructions for the Word Template Extension**

This guide will walk you through installing the Word Template Extension on your computer. The process takes about 10-15 minutes and requires no technical expertise.

## 📋 Before You Start

### System Requirements
- **Windows**: 10 or 11 (recommended)
- **macOS**: 10.14 (Mojave) or later
- **Linux**: Ubuntu 18.04+ or equivalent
- **Browser**: Chrome 88+ or Microsoft Edge 88+
- **Internet**: Required for initial download and browser extension installation

### What You'll Need
- Administrator access to your computer
- About 100MB of free disk space
- Microsoft Word 2016 or later (for viewing generated documents)

## 🪟 Windows Installation (Recommended)

### Step 1: Download the Extension Package
1. Download the Word Template Extension package
2. Extract the ZIP file to a permanent location (e.g., `C:\WordTemplateExtension\`)
3. **Important**: Do not delete this folder after installation

### Step 2: Run the Automated Installer
1. Navigate to the extracted folder
2. Right-click on `install.bat`
3. Select **"Run as administrator"**
4. Click **"Yes"** when prompted by Windows

### Step 3: Follow the Installation Process
The installer will automatically:
- Check for Python (install if needed)
- Install required dependencies
- Create necessary folders
- Register the extension with your browser
- Set up configuration files

**Installation Output Example:**
```
Installing Word Template Extension Native Host...
Installing Python dependencies...
Creating executable...
Updating native messaging manifest...
Registering native messaging host with Chrome...
Registering native messaging host with Edge...
Creating default directories...
Installation completed successfully!
```

### Step 4: Update Extension ID
1. Open the file `native-messaging-host-manifest.json` in a text editor
2. Find the line: `"chrome-extension://YOUR_EXTENSION_ID_HERE/"`
3. Replace `YOUR_EXTENSION_ID_HERE` with your actual extension ID (see Browser Extension Installation below)
4. Save the file

### Step 5: Install Browser Extension
1. Open Chrome or Edge browser
2. Go to the Chrome Web Store or Edge Add-ons store
3. Search for "Word Template Extension"
4. Click **"Add to Chrome"** or **"Get"**
5. Note the extension ID from the URL for Step 4 above

### Step 6: Verify Installation
1. Open your browser
2. Click the extension icon in the toolbar
3. You should see the Word Template Extension popup
4. Check that folders were created:
   - `Documents\Templates\` (for your Word templates)
   - `Documents\Generated\` (for completed documents)

## 🍎 macOS Installation

### Step 1: Install Prerequisites
1. **Install Python** (if not already installed):
   - Download from [python.org](https://python.org)
   - Choose Python 3.7 or later
   - Run the installer and follow prompts

2. **Install Homebrew** (optional, for easier management):
   ```bash
   /bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)"
   ```

### Step 2: Download and Extract
1. Download the Word Template Extension package
2. Extract to a permanent location (e.g., `/Applications/WordTemplateExtension/`)
3. Open Terminal (Applications → Utilities → Terminal)

### Step 3: Install Dependencies
```bash
cd /path/to/WordTemplateExtension/native-host
pip3 install -r requirements.txt
```

### Step 4: Register Native Host
```bash
# For Chrome
mkdir -p ~/.config/google-chrome/NativeMessagingHosts
cp native-messaging-host-manifest.json ~/.config/google-chrome/NativeMessagingHosts/com.wordtemplateextension.nativehost.json

# For Edge
mkdir -p ~/.config/microsoft-edge/NativeMessagingHosts
cp native-messaging-host-manifest.json ~/.config/microsoft-edge/NativeMessagingHosts/com.wordtemplateextension.nativehost.json
```

### Step 5: Update Manifest Path
1. Edit the copied manifest file:
   ```bash
   nano ~/.config/google-chrome/NativeMessagingHosts/com.wordtemplateextension.nativehost.json
   ```
2. Update the `"path"` field to point to your `word_updater.py` file
3. Update the extension ID in `"allowed_origins"`

### Step 6: Create Directories
```bash
mkdir -p ~/Documents/Templates
mkdir -p ~/Documents/Generated
```

### Step 7: Install Browser Extension
Follow the same browser extension installation steps as Windows.

## 🐧 Linux Installation

### Step 1: Install Python and Dependencies
**Ubuntu/Debian:**
```bash
sudo apt update
sudo apt install python3 python3-pip
```

**CentOS/RHEL/Fedora:**
```bash
sudo yum install python3 python3-pip
# or for newer versions:
sudo dnf install python3 python3-pip
```

### Step 2: Download and Setup
1. Download and extract the extension package
2. Navigate to the native-host directory:
   ```bash
   cd /path/to/WordTemplateExtension/native-host
   pip3 install -r requirements.txt
   ```

### Step 3: Register Native Host
```bash
# For Chrome
mkdir -p ~/.config/google-chrome/NativeMessagingHosts
cp native-messaging-host-manifest.json ~/.config/google-chrome/NativeMessagingHosts/com.wordtemplateextension.nativehost.json

# For Chromium
mkdir -p ~/.config/chromium/NativeMessagingHosts
cp native-messaging-host-manifest.json ~/.config/chromium/NativeMessagingHosts/com.wordtemplateextension.nativehost.json
```

### Step 4: Update Configuration
1. Edit the manifest file to update paths and extension ID
2. Make the Python script executable:
   ```bash
   chmod +x word_updater.py
   ```

### Step 5: Create Directories
```bash
mkdir -p ~/Documents/Templates
mkdir -p ~/Documents/Generated
```

## 🔍 Verification Steps

### Test Native Host Connection
1. Open Terminal/Command Prompt
2. Navigate to the native-host directory
3. Run the test command:
   ```bash
   echo '{"action":"ping"}' | python word_updater.py
   ```
4. Expected response: `{"success": true, "message": "pong"}`

### Test Browser Extension
1. Open your browser
2. Navigate to any website
3. Click the Word Template Extension icon
4. Verify the popup appears without errors
5. Check browser console for any error messages

### Test Template Processing
1. Create a simple Word template with `{{TEST}}` placeholder
2. Save it in the Templates folder
3. Use the extension to process the template
4. Verify a document is generated in the Generated folder

## 🚨 Troubleshooting

### Common Installation Issues

#### "Python not found" Error
**Windows:**
- Download Python from [python.org](https://python.org)
- During installation, check "Add Python to PATH"
- Restart Command Prompt and try again

**macOS/Linux:**
- Install Python using your system's package manager
- Verify with: `python3 --version`

#### "Permission denied" Error
**Windows:**
- Run `install.bat` as Administrator
- Check antivirus software isn't blocking installation
- Temporarily disable Windows Defender real-time protection

**macOS/Linux:**
- Use `sudo` for system-wide installations
- Check file permissions: `chmod +x word_updater.py`

#### "Native host not found" Error
1. Restart your browser completely
2. Verify registry entries (Windows) or manifest files (macOS/Linux)
3. Check that file paths in manifest are correct
4. Ensure extension ID matches in manifest file

#### Browser Extension Not Loading
1. Check that extension is enabled in browser settings
2. Verify extension ID in native messaging manifest
3. Look for errors in browser developer console
4. Try reinstalling the browser extension

### Installation Verification Checklist

- [ ] Python 3.7+ installed and accessible
- [ ] Native host dependencies installed (`python-docx`)
- [ ] Native messaging manifest registered with browser
- [ ] Extension ID updated in manifest file
- [ ] Browser extension installed and enabled
- [ ] Templates and Generated folders created
- [ ] Test ping command returns success
- [ ] Extension popup appears without errors

## 📁 File Locations Reference

### Windows
- **Installation**: `C:\path\to\WordTemplateExtension\`
- **Templates**: `%USERPROFILE%\Documents\Templates\`
- **Generated**: `%USERPROFILE%\Documents\Generated\`
- **Logs**: `%USERPROFILE%\AppData\Local\WordTemplateExtension\`
- **Registry**: `HKEY_CURRENT_USER\Software\Google\Chrome\NativeMessagingHosts\`

### macOS
- **Installation**: `/Applications/WordTemplateExtension/` (or chosen location)
- **Templates**: `~/Documents/Templates/`
- **Generated**: `~/Documents/Generated/`
- **Manifest**: `~/.config/google-chrome/NativeMessagingHosts/`

### Linux
- **Installation**: `/opt/WordTemplateExtension/` or `~/WordTemplateExtension/`
- **Templates**: `~/Documents/Templates/`
- **Generated**: `~/Documents/Generated/`
- **Manifest**: `~/.config/google-chrome/NativeMessagingHosts/`

## 🔄 Updating the Extension

### Update Native Host
1. Download the new version
2. Stop the browser
3. Replace files in installation directory
4. Restart browser

### Update Browser Extension
1. Browser extensions typically update automatically
2. Manual update: Remove and reinstall from store
3. Update extension ID in manifest if changed

## 🆘 Getting Additional Help

If you encounter issues not covered here:

1. **Check the logs**: Look in the logs directory for error details
2. **Browser console**: Open developer tools and check for JavaScript errors
3. **Test components**: Use the verification steps to isolate the problem
4. **Reinstall**: Sometimes a clean reinstall resolves complex issues

## 🔒 Security Notes

- The extension only processes data you explicitly extract
- All processing happens locally on your computer
- No data is sent to external servers
- Templates and documents remain on your device
- Review the extension permissions before installation

---

**Installation complete?** Continue to the [User Guide](USER-GUIDE.md) to learn how to use the extension effectively.