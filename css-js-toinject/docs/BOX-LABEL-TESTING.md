# Box Label Silent Print - Testing Guide

## Overview

Simplified version that ONLY prints box labels (carton labels) silently to a preselected printer.

**File**: `box-label-only-silent.js`

## Configuration

Edit these settings in the file:

```javascript
const BOX_LABEL_CONFIG = {
    // Target printer name (must match Windows printer name exactly)
    printerName: 'Brother HL-L6200DW series',

    // Auto-trigger settings
    autoTriggerEnabled: true,
    autoTriggerDelay: 2000,  // Wait 2s after modal appears

    // Debug logging
    debugMode: true
};
```

## Step-by-Step Testing

### Step 1: Check JSPrintManager

Open browser console and run:

```javascript
window.boxLabelSilent.checkJSPM()
```

**Expected output:**
- ✅ If working: `"✅ JSPrintManager connected"` → Silent printing will work
- ⚠️ If not: `"⚠️ JSPrintManager not available"` → Will fallback to window-based printing

### Step 2: Test Manual Print

Get a shipment ID from your modal or use a known ID:

```javascript
// Manual trigger with specific shipment ID
window.boxLabelSilent.print(12345)
```

**What should happen:**
1. Console shows: `"🏷️ BOX LABEL SILENT PRINT - Starting..."`
2. Console shows: `"📡 STEP 1: Fetching shipment data..."`
3. Console shows: `"📄 STEP 2: Generating label HTML..."`
4. Console shows: `"📊 STEP 3: Generating barcodes..."`
5. Console shows: `"🖨️ STEP 4: Printing to configured printer..."`
6. Either:
   - **Silent print**: `"✅ Print job sent successfully"` (no dialog!)
   - **Window fallback**: Print window opens with labels

### Step 3: Check Printer Configuration

```javascript
// List available printers
JSPM.JSPrintManager.getPrinters().then(printers => {
    console.log('Available printers:', printers.map(p => p.name));
});

// Change printer if needed
window.boxLabelSilent.setPrinter('Your Printer Name');
```

### Step 4: Test Auto-Trigger

Create a shipment and watch the console:

**Expected behavior:**
1. When success modal appears: `"🎯 Modal appearance detected"`
2. After 2 seconds: `"⏳ Auto-triggering print in 2000ms..."`
3. Then print workflow starts automatically

### Step 5: Disable Auto-Trigger (if testing)

```javascript
// Disable auto-trigger for testing
window.boxLabelSilent.setAutoTrigger(false)

// Enable again
window.boxLabelSilent.setAutoTrigger(true)
```

## Troubleshooting

### Problem: "JSPrintManager library not loaded"

**Solution**: Add JSPrintManager script to your page:

```html
<script src="https://cdn.jsdelivr.net/npm/jsprintmanager@8.0.0/JSPrintManager.js"></script>
```

Or use the loader from `silent-auto-print-buttons.js` (lines 34-116).

### Problem: "Configured printer not found"

**Solution**: Check available printers and update configuration:

```javascript
// 1. Check available printers
JSPM.JSPrintManager.getPrinters().then(printers => {
    console.log(printers.map(p => p.name));
});

// 2. Set correct printer name
window.boxLabelSilent.setPrinter('Exact Printer Name from List');
```

### Problem: "Cannot find shipment ID"

**Solution**: Ensure shipment ID is captured. Check:

```javascript
// Check stored ID
console.log('Last shipment ID:', window._lastShipmentId);

// Or get from modal
const modal = document.getElementById('shipment-created');
console.log('Modal ID:', modal?.dataset.shipmentId);
```

## Integration with Router

To auto-trigger when modal appears, add to your router:

```javascript
// In router.js or wherever you detect modal
if (modalDetected) {
    window.boxLabelSilent.handleModal();
}
```

## API Reference

```javascript
// Main functions
window.boxLabelSilent.print(shipmentId)    // Print box labels
window.boxLabelSilent.handleModal()        // Handle modal appearance

// Configuration
window.boxLabelSilent.setPrinter(name)     // Change printer
window.boxLabelSilent.setAutoTrigger(bool) // Enable/disable auto

// Utilities
window.boxLabelSilent.checkJSPM()          // Check JSPrintManager status
window.boxLabelSilent.config               // View current config
```

## Next Steps

Once box labels work reliably:
1. ✅ Verify silent printing works (no dialog)
2. ✅ Verify correct printer is used
3. ✅ Verify labels print correctly
4. Then we can add packing slip printing

## Key Differences from Original

| Aspect | Original Button | This Version |
|--------|----------------|--------------|
| Data fetch | ✅ Same | ✅ Same |
| HTML generation | ✅ Same | ✅ Same |
| Barcode generation | ✅ Same | ✅ Same |
| Printing | ⚠️ Opens window + dialog | ✅ Silent to configured printer |
| Fallback | ❌ None | ✅ Window-based if JSPM unavailable |
