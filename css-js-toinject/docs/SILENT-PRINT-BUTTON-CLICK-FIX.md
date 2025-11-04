# Silent Print Button Click Fix

## Problem
The packing slip button click wasn't triggering `window.open()` during silent print capture, causing a 15-second timeout. Only the carton label was being captured (1/2 documents).

**Symptom:**
```
📄 Triggering Packing Slip button click (using jQuery)...
📦 Triggering Carton Label button click (using jQuery)...
📋 $.post intercepted: http://localhost:8080/printinvoice
📦 Captured Carton Label HTML from POST
   Progress: 1/2 documents captured
[15 second timeout]
❌ Silent print failed: Error: Timeout waiting for print content
```

## Root Cause
Used jQuery's `$(element).trigger('click')` instead of native `element.click()`.

The fallback code in `auto-print-buttons.js` uses `packingBtn.click()` and works perfectly. We were using a different method that doesn't properly trigger the button's event handlers.

## Solution
Changed `clickPrintButtons()` in `silent-auto-print-buttons.js`:

**Before:**
```javascript
$(packingSlipBtn).trigger('click');  // Doesn't work
$(cartonLabelBtn).trigger('click');  // Doesn't work
```

**After:**
```javascript
packingSlipBtn.click();  // Works!
cartonLabelBtn.click();  // Works!
```

## Expected Behavior After Fix

When the shipment success modal appears, you should see:

```
🖨️🖨️ Silent Print All - Starting...
📋 Setting up window interceptor...
🎯 Window and POST interceptors installed
🖱️ Clicking print buttons...
📄 Clicking Packing Slip button (native .click())...
📄 Packing Slip button clicked
📋 Window.open intercepted (1/2): #outbound/packingSlipdetail?id=XXX
   Hash-based route detected, using polling strategy
   Content loaded after 100ms
📄 Captured Packing Slip HTML
   Progress: 1/2 documents captured
📦 Clicking Carton Label button (native .click())...
📦 Carton Label button clicked
📋 $.post intercepted: http://localhost:8080/printinvoice
📦 Captured Carton Label HTML from POST
   Progress: 2/2 documents captured
✅ Captured all 2 documents
🖨️ Sending to JSPrintManager...
✅ Silent print completed successfully
```

## Testing Steps

1. **Refresh the page** to load the updated code
2. Go to `#outbound/packing`
3. Fill in shipment details and click "Create Shipment"
4. Watch the console for logs
5. **Expected:** Both documents captured without timeout
6. **Expected:** Documents sent to JSPrintManager for silent printing

## Debug Commands

If you encounter issues:

```javascript
// Check if JSPrintManager is available
await window.silentAutoPrint.checkJSPrintManager()

// Test window interception manually
window.silentAutoPrint.interceptWindows()
// Then click the buttons manually in the modal

// Check captured HTML
window.silentAutoPrint
// Look at capturedWindowHTML object

// Switch to window mode for testing
window.silentAutoPrint.setPrintMode('windows')

// Re-enable JSPrintManager mode
window.silentAutoPrint.setPrintMode('jsprintmanager')
```

## Files Changed
- `css-js-toinject/silent-auto-print-buttons.js` (lines 537 and 552)

## Commit
```
5833f5c - Fix silent auto-print: use native .click() instead of jQuery .trigger()
```
