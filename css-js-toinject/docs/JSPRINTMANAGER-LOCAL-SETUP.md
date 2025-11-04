# JSPrintManager Local Setup Guide

## Overview

The `silent-auto-print-buttons.js` requires **JSPrintManager** library to function. This guide covers all options for loading the library.

---

## Current Setup (CDN)

### Primary CDN (Neodynamic)

```html
<script src="https://cdn.neodynamic.com/jsprintmanager/8.0/JSPrintManager.js"></script>
```

**Pros:**
- ✅ Always up-to-date
- ✅ No local files needed
- ✅ Cached across websites

**Cons:**
- ❌ Requires internet connection
- ❌ Dependent on CDN uptime
- ❌ External dependency

---

## Alternative Options

### Option 1: Alternative CDN (jsDelivr)

If the primary CDN is unavailable, use jsDelivr:

```html
<script src="https://cdn.jsdelivr.net/npm/jsprintmanager@8.0.0/JSPrintManager.min.js"></script>
```

**Pros:**
- ✅ Popular, reliable CDN
- ✅ Automatic version management
- ✅ Good uptime

**Fallback Strategy:**
```html
<script src="https://cdn.neodynamic.com/jsprintmanager/8.0/JSPrintManager.js"></script>
<script>
    // Fallback to jsDelivr if primary CDN fails
    if (typeof JSPM === 'undefined') {
        document.write('<script src="https://cdn.jsdelivr.net/npm/jsprintmanager@8.0.0/JSPrintManager.min.js"><\/script>');
    }
</script>
```

---

### Option 2: NPM Installation (Local Files)

For offline environments or corporate networks that block CDNs.

#### Step 1: Install via NPM

```bash
npm install jsprintmanager
```

This will download JSPrintManager to:
```
node_modules/jsprintmanager/
```

#### Step 2: Copy Files to Your Project

**Option A: Copy to project folder**
```bash
# Windows
copy node_modules\jsprintmanager\JSPrintManager.js css-js-toinject\vendor\

# Linux/Mac
cp node_modules/jsprintmanager/JSPrintManager.js css-js-toinject/vendor/
```

**Option B: Use directly from node_modules**
```html
<script src="node_modules/jsprintmanager/JSPrintManager.js"></script>
```

#### Step 3: Update HTML Reference

```html
<!-- Instead of CDN -->
<!-- <script src="https://cdn.neodynamic.com/jsprintmanager/8.0/JSPrintManager.js"></script> -->

<!-- Use local file -->
<script src="css-js-toinject/vendor/JSPrintManager.js"></script>
```

---

### Option 3: Manual Download

If you don't use NPM, download manually.

#### Step 1: Download from NPM Registry

**Direct Download URL:**
```
https://registry.npmjs.org/jsprintmanager/-/jsprintmanager-8.0.0.tgz
```

Or visit:
```
https://www.npmjs.com/package/jsprintmanager
```

#### Step 2: Extract Files

1. Download `jsprintmanager-8.0.0.tgz`
2. Extract the archive (use 7-Zip on Windows)
3. Navigate to `package/` folder
4. Find `JSPrintManager.js`

#### Step 3: Copy to Project

```
css-js-toinject/
└── vendor/
    └── JSPrintManager.js
```

#### Step 4: Update HTML

```html
<script src="css-js-toinject/vendor/JSPrintManager.js"></script>
```

---

### Option 4: Download from GitHub

#### Step 1: Clone Repository

```bash
git clone https://github.com/neodynamic/JSPrintManager.git
```

#### Step 2: Copy Files

```bash
cd JSPrintManager
# Find the distribution file and copy to your project
```

---

## File Structure Recommendation

### Organized Local Setup

```
css-js-toinject/
├── silent-auto-print-buttons.js
├── vendor/
│   └── JSPrintManager.js            ← Local copy
└── docs/
    └── JSPRINTMANAGER-LOCAL-SETUP.md
```

### HTML Load Order

```html
<!DOCTYPE html>
<html>
<head>
    <title>Your App</title>
</head>
<body>
    <!-- Your content -->

    <!-- Dependencies (load in order) -->
    <script src="css-js-toinject/overlay-manager.js"></script>
    <script src="css-js-toinject/tab-manager.js"></script>
    <script src="css-js-toinject/auto-print-buttons.js"></script>

    <!-- JSPrintManager (choose one option) -->

    <!-- OPTION 1: Primary CDN -->
    <script src="https://cdn.neodynamic.com/jsprintmanager/8.0/JSPrintManager.js"></script>

    <!-- OPTION 2: Local File -->
    <!-- <script src="css-js-toinject/vendor/JSPrintManager.js"></script> -->

    <!-- OPTION 3: Alternative CDN -->
    <!-- <script src="https://cdn.jsdelivr.net/npm/jsprintmanager@8.0.0/JSPrintManager.min.js"></script> -->

    <!-- Silent Auto Print (loads after JSPrintManager) -->
    <script src="css-js-toinject/silent-auto-print-buttons.js"></script>

    <!-- Router (loads last) -->
    <script src="css-js-toinject/router.js"></script>
</body>
</html>
```

---

## Verification

### Check if JSPrintManager Loaded

Open browser console:

```javascript
// Check if JSPM object exists
typeof JSPM
// Should return: "object"

// Check version
console.log(JSPM.VERSION || 'Version info not available');

// Test connection to client
await JSPM.JSPrintManager.start();
// Should return: Promise that resolves if client app is running
```

---

## Client App Installation (Required)

**IMPORTANT:** JSPrintManager library alone is NOT enough. You MUST also install the **JSPrintManager Client App** on user workstations.

### What is the Client App?

- Desktop application that runs in the background
- Provides the actual printing functionality
- Communicates with the JavaScript library via WebSocket
- Required for silent printing to work

### Download Client App

**Windows, Linux, Mac, Raspberry Pi:**
```
https://www.neodynamic.com/downloads/jspm/
```

**Direct Links:**
- Windows: https://www.neodynamic.com/downloads/jspm/JSPMSetup.exe
- Linux: https://www.neodynamic.com/downloads/jspm/jspm-installer.sh
- Mac: https://www.neodynamic.com/downloads/jspm/JSPMSetup.pkg

### Verify Client App is Running

After installation:

1. **Windows**: Check system tray for JSPM icon
2. **Console test**:
```javascript
await window.silentAutoPrint.checkJSPrintManager();
// Should return: true
```

---

## Complete Local Setup Steps

### For Offline/Corporate Environments

1. **Download JSPrintManager Library**
   - Use NPM: `npm install jsprintmanager`
   - Or download from: https://www.npmjs.com/package/jsprintmanager

2. **Copy to Project**
   ```bash
   mkdir css-js-toinject/vendor
   copy node_modules\jsprintmanager\JSPrintManager.js css-js-toinject\vendor\
   ```

3. **Update HTML**
   ```html
   <script src="css-js-toinject/vendor/JSPrintManager.js"></script>
   ```

4. **Download Client App Installers**
   - For each workstation type (Windows/Linux/Mac)
   - Store on network share or internal server

5. **Install Client App on All Workstations**
   - Run installer with admin rights
   - Verify it starts on system boot

6. **Test Integration**
   ```javascript
   // Open your web app in browser
   await window.silentAutoPrint.checkJSPrintManager();
   await window.silentAutoPrint.listPrinters();
   ```

---

## Troubleshooting Local Setup

### Issue: "JSPM is not defined"

**Cause:** JSPrintManager library didn't load

**Check:**
1. Verify file path is correct
2. Check browser console for 404 error
3. Ensure script tag is before `silent-auto-print-buttons.js`

**Solution:**
```html
<!-- Check path -->
<script src="css-js-toinject/vendor/JSPrintManager.js"></script>

<!-- Verify in console -->
<script>
console.log('JSPM loaded:', typeof JSPM !== 'undefined');
</script>
```

---

### Issue: "Cannot find module 'jsprintmanager'"

**Cause:** NPM package not installed

**Solution:**
```bash
npm install jsprintmanager
```

---

### Issue: Library loads but printing doesn't work

**Cause:** Client app not installed or not running

**Check:**
```javascript
await JSPM.JSPrintManager.start();
// If fails: Client app not running
```

**Solution:**
1. Install client app from: https://www.neodynamic.com/downloads/jspm/
2. Restart computer
3. Check system tray for JSPM icon

---

## File Size Comparison

| Source | Size (approx) | Notes |
|--------|---------------|-------|
| CDN (minified) | ~80 KB | Compressed, cached |
| Local (full) | ~200 KB | Uncompressed |
| Local (minified) | ~80 KB | Use JSPrintManager.min.js if available |

---

## Version Management

### Current Version: 8.0.0

### Checking for Updates

```bash
# NPM
npm outdated jsprintmanager

# Check NPM registry
# https://www.npmjs.com/package/jsprintmanager
```

### Updating Local Files

```bash
# Update via NPM
npm update jsprintmanager

# Copy new version
copy node_modules\jsprintmanager\JSPrintManager.js css-js-toinject\vendor\
```

---

## Production Recommendations

### Best Practices

1. **Use CDN as Primary**
   - Fastest loading
   - Cached across sites
   - Automatic updates

2. **Include Local Fallback**
   - For offline scenarios
   - Corporate network restrictions
   - CDN downtime

3. **Version Pin**
   - Use specific version (8.0.0) not "latest"
   - Test before upgrading
   - Document version in code

### Recommended Setup

```html
<!-- Primary: Neodynamic CDN -->
<script src="https://cdn.neodynamic.com/jsprintmanager/8.0/JSPrintManager.js"></script>

<!-- Fallback: Local copy -->
<script>
if (typeof JSPM === 'undefined') {
    console.warn('CDN failed, loading local copy...');
    document.write('<script src="css-js-toinject/vendor/JSPrintManager.js"><\/script>');
}
</script>
```

---

## License Information

JSPrintManager is a **commercial product** by Neodynamic.

- **Free Trial**: Available for development/testing
- **License Required**: For production use
- **Pricing**: Check https://www.neodynamic.com/products/printing/js-print-manager/

### License Types

1. **Developer License** - Single developer
2. **Team License** - Multiple developers
3. **Enterprise License** - Unlimited developers

**Note:** The JavaScript library is free to use, but production deployment may require a license. Check Neodynamic's terms.

---

## Support Resources

- **Official Docs**: https://www.neodynamic.com/Products/Help/JSPrintManager8.0/
- **NPM Package**: https://www.npmjs.com/package/jsprintmanager
- **GitHub**: https://github.com/neodynamic/JSPrintManager
- **Downloads**: https://www.neodynamic.com/downloads/jspm/
- **Support**: https://www.neodynamic.com/support/

---

## Quick Reference

### CDN URLs

```html
<!-- Primary -->
<script src="https://cdn.neodynamic.com/jsprintmanager/8.0/JSPrintManager.js"></script>

<!-- Alternative (jsDelivr) -->
<script src="https://cdn.jsdelivr.net/npm/jsprintmanager@8.0.0/JSPrintManager.min.js"></script>

<!-- Alternative (unpkg) -->
<script src="https://unpkg.com/jsprintmanager@8.0.0/JSPrintManager.js"></script>
```

### NPM Commands

```bash
# Install
npm install jsprintmanager

# Install specific version
npm install jsprintmanager@8.0.0

# Update
npm update jsprintmanager

# Check version
npm list jsprintmanager
```

### File Locations After NPM Install

```
node_modules/jsprintmanager/
├── JSPrintManager.js           ← Main file (use this)
├── JSPrintManager.min.js       ← Minified version
├── package.json
└── README.md
```

---

## Summary

### Two Components Required

1. **JavaScript Library** (choose one):
   - ✅ CDN: `https://cdn.neodynamic.com/jsprintmanager/8.0/JSPrintManager.js`
   - ✅ NPM: `npm install jsprintmanager`
   - ✅ Manual download from NPM registry

2. **Client App** (required on every workstation):
   - Download: https://www.neodynamic.com/downloads/jspm/
   - Install on Windows/Linux/Mac
   - Must be running for printing to work

### Both are needed for silent printing to function!

---

## Next Steps

1. Choose library source (CDN vs Local)
2. Download and install client app on workstations
3. Update HTML script tags if using local files
4. Test with: `await window.silentAutoPrint.checkJSPrintManager()`
5. Configure printer names in `silent-auto-print-buttons.js`
