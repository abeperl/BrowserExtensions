# Testing Setup - Box Label Only Silent Print

## What Was Changed

### 1. Router Updated (`router.js`)
**Lines 865-924**: Changed to use ONLY `box-label-only-silent.js`

**Before**: Used `window.silentAutoPrint` (from `silent-auto-print-buttons.js`)
**After**: Uses `window.boxLabelSilent` (from `box-label-only-silent.js`)

**What it does now**:
- Detects when the shipment success modal appears
- Calls `window.boxLabelSilent.handleModal()`
- Shows clear error messages if box-label-only-silent.js is not loaded

### 2. New Files Created
- ✅ `box-label-only-silent.js` - Main script (ONLY prints box labels)
- ✅ `BOX-LABEL-TESTING.md` - Testing guide
- ✅ `test-box-label.html` - Standalone test page
- ✅ `TESTING-SETUP.md` - This file

## How to Test on Your 3PL Website

### Method 1: Browser Console Injection (Quick Test)

1. **Open your 3PL website** and navigate to `#outbound/packing`

2. **Open browser console** (F12)

3. **Inject the script manually**:
```javascript
// Load JSPrintManager first (if not already loaded)
var script1 = document.createElement('script');
script1.src = 'path/to/JSPrintManager.js';
document.head.appendChild(script1);

// Wait a moment, then load box-label-only-silent.js
setTimeout(() => {
    var script2 = document.createElement('script');
    script2.src = 'path/to/box-label-only-silent.js';
    document.head.appendChild(script2);

    // Load router
    setTimeout(() => {
        var script3 = document.createElement('script');
        script3.src = 'path/to/router.js';
        document.head.appendChild(script3);
    }, 1000);
}, 1000);
```

4. **Check if loaded**:
```javascript
// Should see configuration
console.log(window.boxLabelSilent.config);
```

5. **Create a shipment** and watch the console for:
```
🏷️ Box Label Silent Print feature enabled (TESTING MODE)
💡 Auto-trigger: ENABLED
💡 Configured printer: Brother HL-L6200DW series
```

### Method 2: Use Test HTML Page

1. **Open** `test-box-label.html` in your browser

2. **Check status** - should see:
   - ✅ JSPrintManager loaded
   - ✅ box-label-only-silent.js loaded
   - ✅ Router loaded

3. **Click "Check JSPrintManager"** to verify client is running

4. **Click "List Printers"** to see available printers

5. **Enter a shipment ID** and click "Test Print"

**Note**: This won't work fully without the 3PL site's dependencies (`tf.service`, `common`, `JsBarcode`, etc.)

### Method 3: Browser Extension (If you have one)

If you have a browser extension that loads these scripts:

1. **Update manifest.json** to include:
```json
{
  "content_scripts": [{
    "matches": ["*://your-3pl-site.com/*"],
    "js": [
      "JSPrintManager.js",
      "overlay-manager.js",
      "box-label-only-silent.js",
      "router.js"
    ]
  }]
}
```

2. **Reload extension**

3. **Navigate to** `#outbound/packing`

## Expected Console Output

When you navigate to `#outbound/packing`, you should see:

```
🚀 Matched #outbound/packing route
🏷️ Box Label Silent Print feature enabled (TESTING MODE)
💡 Auto-trigger: ENABLED
💡 Auto-trigger delay: 2000ms
💡 Configured printer: Brother HL-L6200DW series
💡 Debug mode: ON
✅ Box Label Silent Print observer set up
💡 Manual trigger with: window.boxLabelSilent.print(shipmentId)
💡 Check JSPM with: window.boxLabelSilent.checkJSPM()
```

When you create a shipment:

```
🎯 Modal appearance detected
📌 Found shipment ID: 12345
⏳ Auto-triggering print in 2000ms...
═══════════════════════════════════════════════════
🏷️ BOX LABEL SILENT PRINT - Starting...
   Shipment ID: 12345
═══════════════════════════════════════════════════

📡 STEP 1: Fetching shipment data...
✅ Data fetched successfully

📄 STEP 2: Generating label HTML...
✅ Generated HTML for 2 box(es)

📊 STEP 3: Generating barcodes...
  ✅ Barcode generated: barcode0
  ✅ Barcode generated: barcode1
✅ All barcodes generated

🖨️ STEP 4: Printing to configured printer...
✅ JSPrintManager connected
🖨️ Preparing print job...
   Target printer: Brother HL-L6200DW series
   ✅ Using configured printer
✅ Print job sent successfully
```

## Troubleshooting

### ❌ "Box Label Silent Print NOT LOADED"

**Problem**: Router can't find `window.boxLabelSilent`

**Solution**: Make sure `box-label-only-silent.js` is loaded BEFORE `router.js`

### ❌ "JSPrintManager library not loaded"

**Problem**: JSPM not available

**Solutions**:
1. Ensure JSPrintManager.js is loaded first
2. Check browser console for load errors
3. Try loading from CDN: `https://cdn.jsdelivr.net/npm/jsprintmanager@8.0.0/JSPrintManager.js`

### ❌ "JSPrintManager not available"

**Problem**: JSPM client not running

**Solutions**:
1. Download and install JSPrintManager client from https://neodynamic.com/downloads/jspm
2. Start the JSPrintManager service
3. Verify it's running (should see system tray icon)

### ❌ "Configured printer not found"

**Problem**: Printer name doesn't match Windows printer name

**Solution**:
```javascript
// 1. List printers
JSPM.JSPrintManager.getPrinters().then(printers => {
    console.log(printers.map(p => p.name));
});

// 2. Update configuration with exact name
window.boxLabelSilent.setPrinter('Exact Printer Name from List');
```

### ⚠️ "Cannot find shipment ID"

**Problem**: Shipment ID not captured from modal

**Solutions**:
1. Check if modal has ID: `document.getElementById('shipment-created')?.dataset.shipmentId`
2. Check stored ID: `window._lastShipmentId`
3. Manually trigger: `window.boxLabelSilent.print(12345)`

## Manual Testing Commands

```javascript
// Check if loaded
window.boxLabelSilent

// View configuration
window.boxLabelSilent.config

// Check JSPrintManager
window.boxLabelSilent.checkJSPM()

// List printers
JSPM.JSPrintManager.getPrinters().then(printers => {
    console.log(printers.map(p => p.name));
});

// Change printer
window.boxLabelSilent.setPrinter('Your Printer Name')

// Disable auto-trigger (for testing)
window.boxLabelSilent.setAutoTrigger(false)

// Manual print test
window.boxLabelSilent.print(12345)

// View router status
window.extensionRouter
```

## What's Disabled

The following scripts are NO LONGER used (disabled in router):
- ❌ `silent-auto-print-buttons.js` (old version that printed both documents)
- ❌ `auto-print-buttons.js` (legacy version)
- ❌ Any fallback mechanisms

This ensures we're ONLY testing the new `box-label-only-silent.js` script.

## Next Steps

Once box label printing works reliably:

1. ✅ Verify silent printing works (no dialog)
2. ✅ Verify correct printer is used
3. ✅ Verify labels print with correct formatting
4. ✅ Test with multiple boxes
5. ✅ Test barcode generation
6. Then we can add packing slip functionality

## Need Help?

**Check console for errors**: All steps have detailed logging with emoji markers:
- 🏷️ = Box label feature
- 📡 = API calls
- 📄 = HTML generation
- 📊 = Barcode generation
- 🖨️ = Printing
- ✅ = Success
- ❌ = Error
- ⚠️ = Warning

**Debug API available at**: `window.boxLabelSilent`
