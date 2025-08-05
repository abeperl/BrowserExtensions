# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This repository contains two browser extensions:

1. **Word Template Extension** - Extracts data from websites and populates Word templates
2. **Scan Overlay Extension** - Provides scanning workflows with overlays, feedback, and accessibility features

## Development Commands

### Word Template Extension

#### Building Packages
```bash
# Build distribution packages for Chrome/Edge stores
cd word-template-extension
powershell -ExecutionPolicy Bypass -File build-packages.ps1

# Build with specific version
powershell -ExecutionPolicy Bypass -File build-packages.ps1 -Version "1.0.1"

# Build including source code
powershell -ExecutionPolicy Bypass -File build-packages.ps1 -IncludeSource

# Skip native host build
powershell -ExecutionPolicy Bypass -File build-packages.ps1 -BuildNativeHost:$false
```

#### Native Host Development
```bash
cd word-template-extension/native-host

# Install Python dependencies
pip install -r requirements.txt

# Test native host manually
echo '{"action":"ping"}' | python word_updater.py

# Build standalone executable
pip install pyinstaller
pyinstaller --onefile --console --name word_updater word_updater.py
```

#### Installation (Windows)
```bash
cd word-template-extension/native-host
# Run as Administrator
install.bat
```

### Scan Overlay Extension

Enhanced scanning extension with configurable selectors and improved UI:

#### Development Commands
```bash
cd scan-overlay-extension

# Generate audio files (open in browser)
# Open generate-audio.html in browser to create MP3 files
# Download generated files and place in audio/ folder

# No build process required - load unpacked for development
```

#### Configuration
- **Settings Page**: Full settings interface at `settings.html`
- **Configurable Selectors**: XPath/CSS selectors can be customized
- **Audio Generation**: Use `generate-audio.html` to create custom sound files

## Architecture

### Word Template Extension
- **Browser Extension**: Manifest v3 extension with popup, content scripts, and service worker
- **Native Host**: Python application using `python-docx` for Word document processing
- **Communication**: Native messaging protocol between extension and Python host
- **Template System**: Word documents with `{{PLACEHOLDER}}` syntax for dynamic content

Key components:
- `extension/background.js`: Service worker handling native messaging and data extraction
- `extension/popup.js`: UI for template selection and data preview
- `extension/content.js`: Web page data extraction
- `native-host/word_updater.py`: Template processing and document generation

### Scan Overlay Extension
- **Browser Extension**: Enhanced Manifest v3 with configurable selectors and modern UI
- **Settings System**: Comprehensive settings page with field selector configuration
- **Audio Feedback**: Generated MP3 files for scan events with volume control
- **Overlay System**: Dynamic overlays with accessibility features and customizable appearance

Key components:
- `background.js`: Event logging, tab management, and settings synchronization
- `content.js`: Configurable field monitoring, API interception, and scan handling
- `overlay.js`: Dynamic overlay rendering with settings integration
- `popup.js`: Modern popup interface with field detection and quick settings
- `settings.js`: Full settings management page
- `generate-audio.html`: Audio file generation tool

## File Structure

```
BrowserExtensions/
├── word-template-extension/
│   ├── extension/              # Browser extension files
│   ├── native-host/           # Python native messaging host
│   ├── templates/             # Sample Word templates
│   ├── build-packages.ps1     # Build script for distribution
│   └── *.md                   # Documentation files
└── scan-overlay-extension/    # Complete browser extension
    ├── audio/                 # Sound effects
    └── *.js, *.html, *.css   # Extension files
```

## Key Technologies

### Word Template Extension
- **JavaScript**: ES6+ with Chrome extension APIs
- **Python 3.7+**: Native host with `python-docx` library
- **Native Messaging**: JSON protocol for browser-Python communication
- **Word Processing**: Template placeholders like `{{EMAIL}}`, `{{DATE}}`, `{{AMOUNT}}`

### Scan Overlay Extension  
- **JavaScript**: Vanilla JS with Chrome extension APIs
- **CSS**: Custom styling for overlays
- **Web Audio API**: Sound feedback system

## Development Notes

- Both extensions use Manifest v3
- Word Template Extension requires Python runtime and native host registration
- Native messaging host manifest must be registered in Windows registry
- Templates use double-brace syntax: `{{PLACEHOLDER_NAME}}`
- Build script creates packages for Chrome Web Store and Edge Add-ons
- Extension IDs and native host names are hardcoded in configuration files

## Testing

### Word Template Extension
- Test native host: `echo '{"action":"ping"}' | python word_updater.py`
- Load extension unpacked from `extension/` folder
- Verify native messaging registration in browser console
- Test with sample templates in `templates/` folder

### Scan Overlay Extension
- Load unpacked from root folder
- Test overlay rendering on various websites
- Verify audio feedback functionality
- Check accessibility features with screen readers