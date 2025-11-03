# Silent Auto Print - Setup & Integration Guide

Quick setup guide for integrating `silent-auto-print-buttons.js` into your project.

## Prerequisites

1. **JSPrintManager Client** must be installed and running on the user's machine
   - Download: https://www.neodynamic.com/downloads/jsprintmanager/
   - Verify it's running (check system tray)

2. **Brother HL-L6200DW series** printer (or configured printer) must be installed

3. **Existing dependencies** must be loaded:
   - jQuery
   - `overlay-manager.js` (for notifications)
   - `auto-print-buttons.js` (for window fallback)

---

## Installation Steps

### Step 1: Add JSPrintManager Library

Add this script tag to your HTML page **before** loading `silent-auto-print-buttons.js`:

```html
<!-- Load JSPrintManager from CDN -->
<script src="https://cdn.neodynamic.com/jsprintmanager/8.0/JSPrintManager.js"></script>
```

**Recommended placement:** In the `<head>` or before closing `</body>` tag

### Step 2: Add Silent Auto Print Script

Add the script after JSPrintManager and other dependencies:

```html
<!-- Dependencies (should already be loaded) -->
<script src="css-js-toinject/overlay-manager.js"></script>
<script src="css-js-toinject/auto-print-buttons.js"></script>

<!-- JSPrintManager -->
<script src="https://cdn.neodynamic.com/jsprintmanager/8.0/JSPrintManager.js"></script>

<!-- Silent Auto Print -->
<script src="css-js-toinject/silent-auto-print-buttons.js"></script>
```

### Step 3: Configure Printer Names

Edit `silent-auto-print-buttons.js` and update the printer configuration:

```javascript
const SILENT_AUTO_PRINT_CONFIG = {
    printMode: 'jsprintmanager',  // or 'windows'

    // UPDATE THESE with your exact printer names
    printerNamePackingSlip: 'Brother HL-L6200DW series',
    printerNameCartonLabel: 'Brother HL-L6200DW series',

    autoClickEnabled: true,
    autoClickDelay: 500,
    // ... rest of config
};
```

**Important:** Printer names are **case-sensitive** and must match Windows exactly!

### Step 4: Integrate with Router

The script is already set up to intercept API calls automatically, but if you need manual integration, add this to your `router.js`:

```javascript
// In your modal observer or after shipment creation succeeds
if (successModal && successModal.offsetParent !== null) {
    // Let silent-auto-print handle it
    window.silentAutoPrint.handleModal();
}
```

**Note:** The script already intercepts `ProcessOutboundShipment` API calls and triggers automatically, so manual integration may not be needed.

---

## Verification & Testing

### 1. Check Installation

Open browser console and run:

```javascript
// Check if script loaded
console.log(window.silentAutoPrint);
// Should output object with methods

// Check JSPrintManager
await window.silentAutoPrint.checkJSPrintManager();
// Should return true if client is running
```

### 2. List Available Printers

```javascript
await window.silentAutoPrint.listPrinters();
// Should list all printers, e.g.:
// ["Brother HL-L6200DW series", "Microsoft Print to PDF", ...]
```

### 3. Test Printer Validation

```javascript
await window.silentAutoPrint.validatePrinter('Brother HL-L6200DW series');
// Should return true if printer exists
```

### 4. Manual Print Test

To test without creating a real shipment:

```javascript
// Manually trigger with a test shipment ID
await window.silentAutoPrint.printAll(950);  // Replace 950 with real shipment ID
```

---

## Configuration Options

### Print Mode Switching

Switch between silent and window-based printing:

```javascript
// Silent mode (JSPrintManager)
window.silentAutoPrint.setPrintMode('jsprintmanager');

// Window mode (existing behavior)
window.silentAutoPrint.setPrintMode('windows');
```

### Change Printer at Runtime

```javascript
// Change Packing Slip printer
window.silentAutoPrint.setPrinter('packingSlip', 'HP LaserJet P3015');

// Change Carton Label printer
window.silentAutoPrint.setPrinter('cartonLabel', 'Zebra ZP450');
```

### Disable Auto-Click

```javascript
// Disable automatic printing
window.silentAutoPrint.setAutoClick(false);

// Re-enable
window.silentAutoPrint.setAutoClick(true);
```

### Configuration Object

Access and modify the config directly:

```javascript
// View current config
console.log(window.silentAutoPrint.config);

// Modify config
window.silentAutoPrint.config.autoClickDelay = 1000;  // Increase delay to 1 second
window.silentAutoPrint.config.debugMode = false;       // Disable debug logs
```

---

## Troubleshooting

### Issue: "JSPrintManager client not available"

**Check:**
1. Is JSPrintManager client app running? (Check system tray)
2. Is the script tag for JSPrintManager.js loaded?
3. Any browser console errors?

**Test:**
```javascript
await window.silentAutoPrint.checkJSPrintManager();
```

### Issue: "Printer not found"

**Check:**
1. Printer name matches exactly (case-sensitive)
2. Printer is installed in Windows

**Fix:**
```javascript
// List all available printers
await window.silentAutoPrint.listPrinters();

// Copy exact name and update config
window.silentAutoPrint.setPrinter('packingSlip', 'Exact Printer Name');
```

### Issue: "Shipment ID not found"

**Check:**
1. API interceptor is working
2. Modal has data attribute

**Debug:**
```javascript
// Check last captured shipment ID
console.log(window._lastShipmentId);

// Try extracting from modal
window.silentAutoPrint.extractShipmentId();
```

### Issue: Content generation fails

**Check:**
1. API endpoints are accessible
2. CSS files exist (`pages/Outbound/packing-slip.css`, `pages/Outbound/placard.css`)
3. Shipment data is valid

**Test:**
```javascript
// Test Packing Slip generation
await window.silentAutoPrint.generatePackingSlip(950);

// Test Carton Label generation
await window.silentAutoPrint.generateCartonLabel(950);
```

### Issue: Prints look wrong

**Check:**
1. CSS files are loading correctly
2. HTML structure matches expected format

**Debug:**
Enable debug mode to see detailed logs:
```javascript
window.silentAutoPrint.config.debugMode = true;
```

---

## Fallback Behavior

The system has multiple fallback layers:

1. **JSPrintManager** (primary) → Silent printing to configured printers
2. **localhost:8080** (fallback 1) → Existing print service
3. **Window-based** (fallback 2) → Opens print windows like original behavior

### Disable Fallbacks

```javascript
// Disable localhost fallback
window.silentAutoPrint.config.fallbackToLocalhost = false;

// Disable window fallback
window.silentAutoPrint.config.fallbackToWindows = false;
```

### Test Fallbacks

```javascript
// Test localhost fallback directly
await window.silentAutoPrint.fallbackLocalhost(950);

// Test windows fallback directly
await window.silentAutoPrint.fallbackWindows(950);
```

---

## Production Deployment

### Recommended Settings

```javascript
const SILENT_AUTO_PRINT_CONFIG = {
    printMode: 'jsprintmanager',
    printerNamePackingSlip: 'Brother HL-L6200DW series',  // Update for your environment
    printerNameCartonLabel: 'Brother HL-L6200DW series',
    autoClickEnabled: true,
    autoClickDelay: 500,
    jsprintmanagerTimeout: 5000,
    fallbackToLocalhost: true,   // Keep enabled for reliability
    fallbackToWindows: true,     // Keep enabled as last resort
    debugMode: false             // Disable in production
};
```

### Pre-Deployment Checklist

- [ ] JSPrintManager client installed on all workstations
- [ ] Printer names configured correctly
- [ ] JSPrintManager library loading from CDN
- [ ] All dependencies loaded in correct order
- [ ] Tested with real shipment creation workflow
- [ ] Tested all fallback scenarios
- [ ] Debug mode disabled
- [ ] Error notifications working
- [ ] Print output verified (correct content, formatting)

---

## Quick Reference Commands

```javascript
// Check system status
await window.silentAutoPrint.checkJSPrintManager();    // Test JSPrintManager connection
await window.silentAutoPrint.listPrinters();           // List available printers
window.silentAutoPrint.extractShipmentId();            // Get current shipment ID

// Configuration
window.silentAutoPrint.setPrintMode('jsprintmanager'); // Switch print mode
window.silentAutoPrint.setPrinter('packingSlip', 'Printer Name');
window.silentAutoPrint.setAutoClick(false);            // Disable auto-trigger

// Manual operations
await window.silentAutoPrint.printAll(950);            // Manual print with shipment ID
window.silentAutoPrint.handleModal();                  // Manual modal handler trigger

// Testing
window.silentAutoPrint.config.debugMode = true;        // Enable debug logging
await window.silentAutoPrint.generatePackingSlip(950); // Test content generation
await window.silentAutoPrint.generateCartonLabel(950);

// Fallback testing
await window.silentAutoPrint.fallbackLocalhost(950);   // Test localhost fallback
await window.silentAutoPrint.fallbackWindows(950);     // Test windows fallback
```

---

## Support & Documentation

- **Full Specification**: `css-js-toinject/docs/SILENT-AUTO-PRINT-SPEC.md`
- **JSPrintManager Docs**: https://www.neodynamic.com/Products/Help/JSPrintManager8.0/
- **Source Code**: `css-js-toinject/silent-auto-print-buttons.js`

---

## Version History

| Version | Date | Changes |
|---------|------|---------|
| 1.0 | 2025-01-03 | Initial implementation |
